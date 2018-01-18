using System.Threading.Tasks;
using System.Collections.Generic;
using AzureStorage;
using AzureStorage.Tables;
using Common.Log;
using Lykke.SettingsReader;
using Lykke.Service.Dash.Api.Core.Domain.Balance;
using Lykke.Service.Dash.Api.Core.Repositories;

namespace Lykke.Service.Dash.Api.AzureRepositories.BalancePositive
{
    public class BalancePositiveRepository : IBalancePositiveRepository
    {
        private INoSQLTableStorage<BalancePositiveEntity> _table;
        private static string GetPartitionKey() => "";
        private static string GetRowKey(string address) => address;

        public BalancePositiveRepository(IReloadingManager<string> connectionStringManager, ILog log)
        {
            _table = AzureTableStorage<BalancePositiveEntity>.Create(connectionStringManager, "BalancesPositive", log);
        }

        public async Task<(IEnumerable<IBalancePositive> Items, string Continuation)> GetAsync(int take, string continuation)
        {
            var result = await _table.GetDataWithContinuationTokenAsync(GetPartitionKey(), take, continuation);

            return (result.Entities, result.ContinuationToken);
        }

        public async Task SaveAsync(string address, decimal amount)
        {
            await _table.InsertOrReplaceAsync(new BalancePositiveEntity
            {
                PartitionKey = GetPartitionKey(),
                RowKey = GetRowKey(address),
                Amount = amount
            });
        }

        public async Task DeleteAsync(string address)
        {
            await _table.DeleteIfExistAsync(GetPartitionKey(), GetRowKey(address));
        }
    }
}
