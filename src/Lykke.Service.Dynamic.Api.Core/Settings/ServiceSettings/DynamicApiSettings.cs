namespace Lykke.Service.Dynamic.Api.Core.Settings.ServiceSettings
{
    public class DynamicApiSettings
    {
        public DbSettings Db { get; set; }
        public RpcSettings Rpc { get; set; }
        public string Network { get; set; }
        public decimal Fee { get; set; }
        public int MinConfirmations { get; set; }
    }
}
