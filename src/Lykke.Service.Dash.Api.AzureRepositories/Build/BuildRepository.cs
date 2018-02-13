using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Lykke.Service.Dash.Api.Core.Repositories;
using Lykke.Service.Dash.Api.Core.Domain.Build;
using Lykke.SettingsReader;
using AzureStorage;
using AzureStorage.Tables;
using Common.Log;

namespace Lykke.Service.Dash.Api.AzureRepositories.Build
{
    public class BuildRepository : IBuildRepository
    {
        private INoSQLTableStorage<BuildEntity> _table;
        private static string GetPartitionKey() => "";
        private static string GetRowKey(Guid operationId) => operationId.ToString();

        public BuildRepository(IReloadingManager<string> connectionStringManager, ILog log)
        {
            _table = AzureTableStorage<BuildEntity>.Create(connectionStringManager, "Builds", log);
        }

        public async Task<IBuild> GetAsync(Guid operationId)
        {
            return await _table.GetDataAsync(GetPartitionKey(), GetRowKey(operationId));
        }

        public async Task AddAsync(Guid operationId, string transactionContext)
        {
            await _table.InsertOrReplaceAsync(new BuildEntity
            {
                PartitionKey = GetPartitionKey(),
                RowKey = GetRowKey(operationId),
                TransactionContext = transactionContext
            });
        }

        public async Task DeleteAsync(Guid operationId)
        {
            await _table.DeleteIfExistAsync(GetPartitionKey(), GetRowKey(operationId));
        }
    }
}
