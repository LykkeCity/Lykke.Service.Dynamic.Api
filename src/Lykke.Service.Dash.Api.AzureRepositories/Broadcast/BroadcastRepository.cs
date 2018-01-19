using System;
using System.Threading.Tasks;
using AzureStorage;
using AzureStorage.Tables;
using Common.Log;
using Lykke.SettingsReader;
using Lykke.Service.Dash.Api.Core.Domain.Broadcast;
using Lykke.Service.Dash.Api.Core.Repositories;

namespace Lykke.Service.Dash.Api.AzureRepositories.Transaction
{
    public class BroadcastRepository : IBroadcastRepository
    {
        private INoSQLTableStorage<BroadcastEntity> _table;
        private static string GetPartitionKey() => "";
        private static string GetRowKey(Guid operationId) => operationId.ToString();

        public BroadcastRepository(IReloadingManager<string> connectionStringManager, ILog log)
        {
            _table = AzureTableStorage<BroadcastEntity>.Create(connectionStringManager, "Broadcasts", log);
        }

        public async Task<IBroadcast> GetAsync(Guid operationId)
        {
            return await _table.GetDataAsync(GetPartitionKey(), GetRowKey(operationId));
        }

        public async Task AddAsync(Guid operationId, string hash)
        {
            await _table.InsertOrReplaceAsync(new BroadcastEntity
            {
                PartitionKey = GetPartitionKey(),
                RowKey = GetRowKey(operationId),
                BroadcastedUtc = DateTime.UtcNow,
                State = BroadcastState.Broadcasted,
                Hash = hash
            });
        }

        public async Task AddFailedAsync(Guid operationId, string hash, string error)
        {
            await _table.InsertOrReplaceAsync(new BroadcastEntity
            {
                PartitionKey = GetPartitionKey(),
                RowKey = GetRowKey(operationId),
                FailedUtc = DateTime.UtcNow,
                State = BroadcastState.Failed,
                Hash = hash,
                Error = error
            });
        }

        public async Task SaveAsCompletedAsync(Guid operationId, decimal amount, decimal fee)
        {
            await _table.ReplaceAsync(GetPartitionKey(), GetRowKey(operationId), x =>
            {
                x.State = BroadcastState.Completed;
                x.CompletedUtc = DateTime.UtcNow;
                x.Amount = amount;
                x.Fee = fee;

                return x;
            });
        }

        public async Task DeleteAsync(Guid operationId)
        {
            await _table.DeleteAsync(GetPartitionKey(), GetRowKey(operationId));
        }
    }
}
