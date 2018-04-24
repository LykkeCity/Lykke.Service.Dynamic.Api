using Lykke.Service.Dash.Api.Core.Settings.ServiceSettings;

namespace Lykke.Service.Dash.Job.Settings
{
    public class DashJobSettings
    {
        public DbSettings Db { get; set; }
        public string InsightApiUrl { get; set; }
        public int MinConfirmations { get; set; }
        public int BalanceCheckerIntervalMs { get; set; }
        public int BroadcastCheckerIntervalMs { get; set; }
    }
}
