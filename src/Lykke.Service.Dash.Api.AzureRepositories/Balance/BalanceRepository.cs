using System.Threading.Tasks;
using AzureStorage;
using AzureStorage.Tables;
using Common.Log;
using Lykke.SettingsReader;
using Lykke.Service.Dash.Api.Core.Domain.Balance;
using System.Collections.Generic;
using Lykke.Service.Dash.Api.Core.Repositories;

namespace Lykke.Service.Dash.Api.AzureRepositories.Balance
{
    public class BalanceRepository : IBalanceRepository
    {
        private INoSQLTableStorage<BalanceEntity> _table;
        private static string GetPartitionKey() => "";
        private static string GetRowKey(string address) => address;

        public BalanceRepository(IReloadingManager<string> connectionStringManager, ILog log)
        {
            _table = AzureTableStorage<BalanceEntity>.Create(connectionStringManager, "Balances", log);
        }

        public async Task<IEnumerable<IBalance>> GetAllAsync()
        {
            return await _table.GetDataAsync(GetPartitionKey());
        }

        public async Task<IBalance> GetAsync(string address)
        {
            return await _table.GetDataAsync(GetPartitionKey(), GetRowKey(address));
        }

        public async Task AddAsync(string address)
        {
            await _table.InsertOrReplaceAsync(new BalanceEntity
            {
                PartitionKey = GetPartitionKey(),
                RowKey = GetRowKey(address)
            });
        }

        public async Task DeleteAsync(string address)
        {
            await _table.DeleteAsync(GetPartitionKey(), GetRowKey(address));
        }
    }
}
