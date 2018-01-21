using Common.Log;
using Lykke.Service.Dash.Api.Core.Domain;
using Lykke.Service.Dash.Api.Core.Domain.Broadcast;
using Lykke.Service.Dash.Api.Core.Services;
using Lykke.Service.Dash.Api.Core.Repositories;
using Lykke.Service.Dash.Api.Core.Settings.ServiceSettings;
using Lykke.Service.Dash.Api.Services.Helpers;
using NBitcoin;
using NBitcoin.Dash;
using NBitcoin.JsonConverters;
using NBitcoin.Policy;
using System;
using System.Linq;
using System.Threading.Tasks;
using Lykke.Service.Dash.Api.Core.Domain.InsightClient;

namespace Lykke.Service.Dash.Api.Services
{
    public class DashService : IDashService
    {
        private readonly ILog _log;
        private readonly IDashInsightClient _dashInsightClient;
        private readonly IBroadcastRepository _broadcastRepository;
        private readonly IBroadcastInProgressRepository _broadcastInProgressRepository;
        private readonly IBalanceRepository _balanceRepository;
        private readonly IBalancePositiveRepository _balancePositiveRepository;
        private readonly Network _network;
        private readonly DashApiSettings _dashApiSettings;
        private readonly FeeRate _feeRate;

        public DashService(ILog log,
            IDashInsightClient dashInsightClient,
            IBroadcastRepository broadcastRepository,
            IBroadcastInProgressRepository broadcastInProgressRepository,
            IBalanceRepository balanceRepository,
            IBalancePositiveRepository balancePositiveRepository,
            DashApiSettings dashApiSettings)
        {
            DashNetworks.Register();

            _log = log;
            _dashInsightClient = dashInsightClient;
            _broadcastRepository = broadcastRepository;
            _broadcastInProgressRepository = broadcastInProgressRepository;
            _balanceRepository = balanceRepository;
            _balancePositiveRepository = balancePositiveRepository;
            _dashApiSettings = dashApiSettings;
            _network = Network.GetNetwork(_dashApiSettings.Network);
            _feeRate = new FeeRate(_dashApiSettings.FeePerByteSatoshis * 1024);
        }

        public BitcoinAddress GetBitcoinAddress(string address)
        {
            try
            {
                return BitcoinAddress.Create(address, _network);
            }
            catch
            {
                return null;
            }            
        }

        public Transaction GetTransaction(string transactionHex)
        {
            try
            {
                return Transaction.Parse(transactionHex);
            }
            catch
            {
                return null;
            }
        }

        public async Task<string> BuildTransactionAsync(Guid operationId, BitcoinAddress fromAddress, 
            BitcoinAddress toAddress, decimal amount, bool includeFee)
        {
            var sendAmount = Money.FromUnit(amount, Asset.Dash.Unit);

            var txsUnspent = await _dashInsightClient.GetTxsUnspentAsync(fromAddress.ToString());
            if (txsUnspent == null || !txsUnspent.Any())
            {
                throw new Exception($"There are no assets in {nameof(fromAddress)} address");
            }

            var availableAmount = txsUnspent.Sum(f => f.Amount);
            if (availableAmount < amount)
            {
                throw new Exception($"There are no enough assets in {nameof(fromAddress)} address: " +
                    $"available={availableAmount}, required: {amount}");
            }

            var builder = new TransactionBuilder()
                .Send(toAddress, sendAmount)
                .SetChange(fromAddress)
                .SetTransactionPolicy(new StandardTransactionPolicy
                {
                    CheckFee = false
                });

            if (includeFee)
            {
                builder.SubtractFees();
            }

            foreach (var txUnspent in txsUnspent)
            {
                var coin = new Coin(
                    fromTxHash: uint256.Parse(txUnspent.Txid),
                    fromOutputIndex: txUnspent.Vout,
                    amount: Money.Coins(txUnspent.Amount),
                    scriptPubKey: fromAddress.ScriptPubKey);

                builder.AddCoins(coin);
            }

            var feeMoney = CalculateFee(builder);

            var tx = builder
                .SendFees(feeMoney)
                .BuildTransaction(false);

            var coins = builder.FindSpentCoins(tx);

            return Serializer.ToString((tx: tx, coins: coins));
        }

        private Money CalculateFee(TransactionBuilder txBuilder)
        {
            var fee = txBuilder.EstimateFees(_feeRate);
            var min = Money.Satoshis(_dashApiSettings.MinFeeSatoshis);
            var max = Money.Satoshis(_dashApiSettings.MaxFeeSatoshis);

            return Money.Max(Money.Min(fee, max), min);
        }

        public async Task BroadcastAsync(Transaction transaction, Guid operationId)
        {
            try
            {
                var response = await _dashInsightClient.BroadcastTxAsync(transaction.ToHex());

                await _broadcastRepository.AddAsync(operationId, response.Txid);
                await _broadcastInProgressRepository.AddAsync(operationId, response.Txid);
            }
            catch (Exception ex)
            {
                await _log.WriteErrorAsync(nameof(DashService), nameof(BroadcastAsync),
                    $"transaction={transaction.ToString()}, operationId={operationId}", ex);

                await _broadcastRepository.AddFailedAsync(operationId, transaction.GetHash().ToString(), 
                    ex.Message);
            }
        }

        public async Task<IBroadcast> GetBroadcastAsync(Guid operationId)
        {
            return await _broadcastRepository.GetAsync(operationId);
        }

        public async Task DeleteBroadcastAsync(IBroadcast broadcast)
        {
            await _broadcastRepository.DeleteAsync(broadcast.OperationId);

            if (broadcast.State == BroadcastState.Broadcasted)
            {
                await _broadcastInProgressRepository.DeleteAsync(broadcast.OperationId);
            }
        }

        public async Task UpdateBroadcasts()
        {
            var list = await _broadcastInProgressRepository.GetAllAsync();
            if (list == null || !list.Any())
            {
                return;
            }

            foreach (var item in list)
            {
                var tx = await _dashInsightClient.GetTx(item.Hash);
                if (tx != null && tx.Confirmations >= _dashApiSettings.MinConfirmations)
                {
                    await RefreshBalances(tx);

                    await _broadcastRepository.SaveAsCompletedAsync(item.OperationId, tx.GetAmount(), tx.Fees);
                    await _broadcastInProgressRepository.DeleteAsync(item.OperationId);
                }
            }
        }

        private async Task RefreshBalances(Tx tx)
        {
            foreach (var address in tx.GetAddresses())
            {
                var balance = await _balanceRepository.GetAsync(address);
                if (balance != null)
                {
                    var amount = await RefreshAddressBalance(address);

                    await _log.WriteInfoAsync(nameof(DashService), nameof(RefreshBalances),
                        $"New balance of address={address} is {amount}");
                }
            }
        }

        public async Task UpdateBalances()
        {
            var balances = await _balanceRepository.GetAllAsync();

            foreach (var balance in balances)
            {
                var amount = await RefreshAddressBalance(balance.Address);
            }
        }

        public async Task<decimal> RefreshAddressBalance(string address)
        {
            var balanceSatoshis = await _dashInsightClient.GetBalanceSatoshis(address);
            var balance = Money.Satoshis(balanceSatoshis).ToDecimal(Asset.Dash.Unit);

            if (balance > 0)
            {
                await _balancePositiveRepository.SaveAsync(address, balance);
            }
            else
            {
                await _balancePositiveRepository.DeleteAsync(address);
            }

            return balance;
        }
    }
}
