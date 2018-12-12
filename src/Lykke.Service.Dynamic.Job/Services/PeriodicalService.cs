using Common.Log;
using Lykke.Service.Dynamic.Api.Core.Services;
using Lykke.Service.Dynamic.Api.Core.Repositories;
using Lykke.Service.Dynamic.Api.Services.Helpers;
using Lykke.Service.Dynamic.Api.Core.Domain.InsightClient;
using System.Threading.Tasks;
using System.Linq;
using Lykke.Common.Chaos;
using Common;

namespace Lykke.Service.Dynamic.Job.Services
{
    public class PeriodicalService : IPeriodicalService
    {
        private ILog _log;
        private readonly IChaosKitty _chaosKitty;
        private readonly IDynamicRpcClient _dynamicRpcClient;
        private readonly IBroadcastRepository _broadcastRepository;
        private readonly IBroadcastInProgressRepository _broadcastInProgressRepository;
        private readonly IBalanceRepository _balanceRepository;
        private readonly IBalancePositiveRepository _balancePositiveRepository;
        private readonly int _minConfirmations;

        public PeriodicalService(ILog log,
            IChaosKitty chaosKitty,
            IDynamicRpcClient dynamicRpcClient,
            IBroadcastRepository broadcastRepository,
            IBroadcastInProgressRepository broadcastInProgressRepository,
            IBalanceRepository balanceRepository,
            IBalancePositiveRepository balancePositiveRepository,
            int minConfirmations)
        {
            _log = log.CreateComponentScope(nameof(PeriodicalService));
            _chaosKitty = chaosKitty;
            _dynamicRpcClient = dynamicRpcClient;
            _broadcastRepository = broadcastRepository;
            _broadcastInProgressRepository = broadcastInProgressRepository;
            _balanceRepository = balanceRepository;
            _balancePositiveRepository = balancePositiveRepository;
            _minConfirmations = minConfirmations;
        }

        public async Task UpdateBroadcasts()
        {
            var list = await _broadcastInProgressRepository.GetAllAsync();

            foreach (var item in list)
            {
                var tx = await _dynamicRpcClient.GetTx(item.Hash);
                if (tx != null && tx.Confirmations >= _minConfirmations)
                {
                    _log.WriteInfo(nameof(UpdateBroadcasts),
                        new { item.OperationId, amount = tx.GetAmount(), tx.Fees, tx.BlockHeight }, 
                        $"Brodcast update is detected");

                    await _broadcastRepository.SaveAsCompletedAsync(item.OperationId, tx.GetAmount(),
                        tx.Fees, tx.BlockHeight);

                    _chaosKitty.Meow(item.OperationId);

                    await _broadcastInProgressRepository.DeleteAsync(item.OperationId);

                    _chaosKitty.Meow(item.OperationId);

                    await RefreshBalances(tx);
                }
            }
        }

        public async Task UpdateBalances()
        {
            var positiveBalances = await _balancePositiveRepository.GetAllAsync();
            var continuation = "";

            while (true)
            {
                var balances = await _balanceRepository.GetAsync(100, continuation);

                foreach (var balance in balances.Entities)
                {
                    var deleteZeroBalance = positiveBalances.Any(f => f.Address == balance.Address);

                    await RefreshAddressBalance(balance.Address, deleteZeroBalance);
                }

                if (string.IsNullOrEmpty(balances.ContinuationToken))
                {
                    break;
                }

                continuation = balances.ContinuationToken;
            }
        }

        private async Task RefreshBalances(Tx tx)
        {
            foreach (var address in tx.GetAddresses())
            {
                var balance = await _balanceRepository.GetAsync(address);
                if (balance != null)
                {
                    await RefreshAddressBalance(address, true);
                }
            }
        }

        private async Task<decimal> RefreshAddressBalance(string address, bool deleteZeroBalance)
        {
            var balance = await _dynamicRpcClient.GetBalance(address, _minConfirmations);

            if (balance > 0)
            {
                var block = await _dynamicRpcClient.GetLatestBlockHeight();

                var balancePositive = await _balancePositiveRepository.GetAsync(address);
                if (balancePositive == null)
                {
                    _log.WriteInfo(nameof(RefreshAddressBalance), 
                        new { address, balance, block },
                        $"Positive balance is detected");
                }
                if (balancePositive != null && balancePositive.Amount != balance)
                {
                    _log.WriteInfo(nameof(RefreshAddressBalance),
                        new { address, balance, oldBalance = balancePositive.Amount, block }, 
                        $"Change in positive balance is detected");
                }

                await _balancePositiveRepository.SaveAsync(address, balance, block);

                _chaosKitty.Meow(new { address, balance, block }.ToJson());
            }

            if (balance == 0 && deleteZeroBalance)
            {
                _log.WriteInfo(nameof(RefreshAddressBalance), new { address }, 
                    $"Zero balance is detected");

                await _balancePositiveRepository.DeleteAsync(address);

                _chaosKitty.Meow(address);
            }

            return balance;
        }
    }
}
