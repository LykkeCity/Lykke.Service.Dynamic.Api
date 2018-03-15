namespace Lykke.Service.Dash.Api.Core.Settings.ServiceSettings
{
    public class DashApiSettings
    {
        public DbSettings Db { get; set; }
        public string Network { get; set; }
        public string InsightApiUrl { get; set; }
        public decimal Fee { get; set; }
        public int MinConfirmations { get; set; }
        public int BalanceCheckerIntervalMs { get; set; }
        public int BroadcastCheckerIntervalMs { get; set; }
    }
}
