namespace Lykke.Service.Dash.Api.Core.Settings.ServiceSettings
{
    public class DashApiSettings
    {
        public DbSettings Db { get; set; }
        public string Network { get; set; }
        public string InsightApiUrl { get; set; }
        public ulong MinFeeSatoshis { get; set; }
        public ulong MaxFeeSatoshis { get; set; }
        public ulong FeePerByteSatoshis { get; set; }
        public int MinConfirmations { get; set; }
        public int BalanceCheckerIntervalMs { get; set; }
        public int BroadcastCheckerIntervalMs { get; set; }
    }
}
