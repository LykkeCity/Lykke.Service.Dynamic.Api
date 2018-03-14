using System;
using System.Threading.Tasks;
using AzureStorage;
using AzureStorage.Tables;
using Common.Log;
using Lykke.SettingsReader;
using Lykke.Service.Dash.Api.Core.Domain.Broadcast;
using Lykke.Service.Dash.Api.Core.Repositories;

namespace Lykke.Service.Dash.Api.AzureRepositories.Broadcast
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

        public async Task AddAsync(Guid operationId, string hash, long block)
        {
            await _table.InsertOrReplaceAsync(new BroadcastEntity
            {
                PartitionKey = GetPartitionKey(),
                RowKey = GetRowKey(operationId),
                BroadcastedUtc = DateTime.UtcNow,
                State = BroadcastState.Broadcasted,
                Hash = hash,
                Block = block
            });
        }

        public async Task AddFailedAsync(Guid operationId, string hash, string error, long block)
        {
            await _table.InsertOrReplaceAsync(new BroadcastEntity
            {
                PartitionKey = GetPartitionKey(),
                RowKey = GetRowKey(operationId),
                FailedUtc = DateTime.UtcNow,
                State = BroadcastState.Failed,
                Hash = hash,
                Error = error,
                Block = block
            });
        }

        public async Task SaveAsCompletedAsync(Guid operationId, decimal amount, decimal fee, long block)
        {
            await _table.ReplaceAsync(GetPartitionKey(), GetRowKey(operationId), x =>
            {
                x.State = BroadcastState.Completed;
                x.CompletedUtc = DateTime.UtcNow;
                x.Amount = amount;
                x.Fee = fee;
                x.Block = block;

                return x;
            });
        }

        public async Task DeleteAsync(Guid operationId)
        {
            await _table.DeleteIfExistAsync(GetPartitionKey(), GetRowKey(operationId));
        }
    }
}
