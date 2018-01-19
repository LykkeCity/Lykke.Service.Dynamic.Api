using Lykke.Service.Dash.Api.Core.Domain.Balance;
using Lykke.Service.Dash.Api.Core.Domain.Broadcast;
using NBitcoin;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Lykke.Service.Dash.Api.Core.Services
{
    public interface IDashService
    {
        BitcoinAddress GetBitcoinAddress(string address);

        Transaction GetTransaction(string transactionHex);

        Task<string> BuildTransactionAsync(BitcoinAddress fromAddress, BitcoinAddress toAddress, 
            decimal amount, bool includeFee);

        Task BroadcastAsync(Transaction transaction, Guid operationId);

        Task<IBroadcast> GetBroadcastAsync(Guid operationId);

        Task DeleteBroadcastAsync(IBroadcast broadcast);

        Task UpdateBroadcasts();

        Task UpdateBalances();

        Task<decimal> RefreshAddressBalance(string address);
    }
}
