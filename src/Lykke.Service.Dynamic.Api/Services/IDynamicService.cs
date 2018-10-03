using Lykke.Service.Dynamic.Api.Core.Domain.Broadcast;
using Lykke.Service.Dynamic.Api.Core.Domain.InsightClient;
using NBitcoin;
using System;
using System.Threading.Tasks;

namespace Lykke.Service.Dynamic.Api.Services
{
    public interface IDynamicService
    {
        BitcoinAddress GetBitcoinAddress(string address);

        Transaction GetTransaction(string transactionHex);

        Task<string> BuildTransactionAsync(Guid operationId, BitcoinAddress fromAddress, 
            BitcoinAddress toAddress, decimal amount, bool includeFee);

        Task BroadcastAsync(Transaction transaction, Guid operationId);

        Task<IBroadcast> GetBroadcastAsync(Guid operationId);

        Task DeleteBroadcastAsync(IBroadcast broadcast);

        Task<decimal> GetAddressBalance(string address);

        decimal GetFee();

        Task<Tx[]> GetFromAddressTxs(string fromAddress, int take, string afterHash);
        //public async Task<Tx[]> GetAddressTxs(string address, int continuation)
        Task<Tx[]> GetToAddressTxs(string toAddress, int take, string afterHash);

    }
}
