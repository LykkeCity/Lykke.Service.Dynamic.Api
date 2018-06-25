using Lykke.AzureStorage.Tables;
using Lykke.AzureStorage.Tables.Entity.Annotation;
using Lykke.AzureStorage.Tables.Entity.ValueTypesMerging;
using Lykke.Service.Dynamic.Api.Core.Domain.Balance;

namespace Lykke.Service.Dynamic.Api.AzureRepositories.BalancePositive
{
    [ValueTypeMergingStrategy(ValueTypeMergingStrategy.UpdateAlways)]
    internal class BalancePositiveEntity : AzureTableEntity, IBalancePositive
    {
        public string Address
        {
            get => RowKey;
        }

        public decimal Amount { get; set; }

        public long Block { get; set; }
    }
}
