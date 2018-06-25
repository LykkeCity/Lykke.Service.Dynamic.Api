using NBitcoin;

namespace Lykke.Service.Dynamic.Api.Core.Domain
{
    public class Asset
    {
        public Asset(string id, int accuracy, MoneyUnit unit) => (Id, Accuracy, Unit) = (id, accuracy, unit);

        public string Id { get; }
        public int Accuracy { get; }
        public MoneyUnit Unit { get; }

        public static Asset Dynamic { get; } = new Asset("DYN", 8, MoneyUnit.BTC);
    }
}
