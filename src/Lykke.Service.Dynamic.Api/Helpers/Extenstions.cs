using Common;
using Lykke.Common.Api.Contract.Responses;
using Lykke.Service.BlockchainApi.Contract;
using Lykke.Service.BlockchainApi.Contract.Assets;
using Lykke.Service.BlockchainApi.Contract.Balances;
using Lykke.Service.BlockchainApi.Contract.Transactions;
using Lykke.Service.Dynamic.Api.Core.Domain;
using Lykke.Service.Dynamic.Api.Core.Domain.Balance;
using Lykke.Service.Dynamic.Api.Core.Domain.Broadcast;
using Lykke.Service.Dynamic.Api.Core.Domain.InsightClient;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Lykke.Service.Dynamic.Api.Helpers
{
    public static class Extenstions
    {
        public static ErrorResponse ToErrorResponse(this ModelStateDictionary modelState)
        {
            var response = new ErrorResponse();

            foreach (var state in modelState)
            {
                var messages = state.Value.Errors
                    .Where(e => !string.IsNullOrWhiteSpace(e.ErrorMessage))
                    .Select(e => e.ErrorMessage)
                    .Concat(state.Value.Errors
                        .Where(e => string.IsNullOrWhiteSpace(e.ErrorMessage))
                        .Select(e => e.Exception.Message))
                    .ToList();

                response.ModelErrors.Add(state.Key, messages);
            }

            return response;
        }

        public static AssetResponse ToAssetResponse(this Asset self)
        {
            return new AssetResponse
            {
                Accuracy = self.Accuracy,
                Address = string.Empty,
                AssetId = self.Id,
                Name = self.Id
            };
        }

        //public static AssetResponse BadAssetResponse(this Asset self)
        //{
        //    return new AssetResponse(
           
        //}

        public static BroadcastedTransactionState ToBroadcastedTransactionState(this BroadcastState self)
        {
            switch (self)
            {
                case BroadcastState.Broadcasted:
                    return BroadcastedTransactionState.InProgress;
                case BroadcastState.Completed:
                    return BroadcastedTransactionState.Completed;
                case BroadcastState.Failed:
                    return BroadcastedTransactionState.Failed;
                default:
                    throw new ArgumentException($"Failed to convert " +
                        $"{nameof(BroadcastState)}.{Enum.GetName(typeof(BroadcastState), self)} " +
                        $"to {nameof(BroadcastedTransactionState)}");
            }
        }

        public static DateTime GetTimestamp(this IBroadcast self)
        {
            switch (self.State)
            {
                case BroadcastState.Broadcasted:
                    return self.BroadcastedUtc.Value;
                case BroadcastState.Completed:
                    return self.CompletedUtc.Value;
                case BroadcastState.Failed:
                    return self.FailedUtc.Value;
                default:
                    throw new ArgumentException($"Unsupported IBroadcast.State={Enum.GetName(typeof(BroadcastState), self.State)}");
            }
        }

        public static WalletBalanceContract ToWalletBalanceContract(this IBalancePositive self)
        {
            return new WalletBalanceContract
            {
                Address = self.Address,
                AssetId = Asset.Dynamic.Id,
                Balance = Conversions.CoinsToContract(self.Amount, Asset.Dynamic.Accuracy),
                Block = self.Block
            };
        }

        public static HistoricalTransactionContract ToHistoricalTransactionContract(this Tx self, string address, 
            bool isFrom)
        {
            var fromAddress = "";
            var toAddress = "";
            var amount = 0M;

            if (isFrom)
            {
                var vouts = self.Vout.Where(f => !f.ScriptPubKey.Addresses.Contains(address));
                var toAddresses = new List<string>();               

                foreach (var vout in vouts)
                {
                    foreach (var voutAddress in vout.ScriptPubKey.Addresses)
                    {
                        if (!toAddresses.Contains(voutAddress))
                        {
                            toAddresses.Add(voutAddress);
                        }
                    }
                }

                fromAddress = address;
                //toAddress = $"{{ {string.Join(",", toAddresses)} }}";
                toAddress = toAddresses.FirstOrDefault();
                amount = vouts.Sum(f => f.Value);
                //mark 20181005 add fee to total amount
                amount = amount + self.Fees;
            }
            else
            {
               
            var vins = self.Vin.Where(f => !f.Addr.Contains(address));
            var toAddresses = new List<string>();

            foreach (var vin in vins)
            {
                    if (!toAddresses.Contains(vin.Addr))
                    {
                        toAddresses.Add(vin.Addr);
                    }
            }
               
                fromAddress = toAddresses.FirstOrDefault();
                //toAddress = $"{{ {string.Join(",", toAddresses)} }}";
                toAddress = address;  
                amount = vins.Sum(f => f.Value);
                //mark 20181005 subtract fee from total amount
                amount = amount - self.Fees;
            }

            return new HistoricalTransactionContract
            {
                Amount = Conversions.CoinsToContract(amount, Asset.Dynamic.Accuracy),
                AssetId = Asset.Dynamic.Id,
                FromAddress = fromAddress,
                ToAddress = toAddress,
                Hash = self.Txid,
               // OperationId = Guid.Empty,
                //mark schroeder 20181002 Dyn uses seconds and not milliseconds. 
                //Timestamp = DateTimeOffset.FromUnixTimeMilliseconds(self.Time).DateTime.ToUniversalTime()
                Timestamp = DateTimeOffset.FromUnixTimeSeconds(self.Time).DateTime.ToUniversalTime()
            };
        }
    }
}
