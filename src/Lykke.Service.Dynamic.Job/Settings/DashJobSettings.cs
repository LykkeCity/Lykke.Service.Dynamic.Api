using Lykke.Common.Chaos;
using Lykke.Service.Dynamic.Api.Core.Settings.ServiceSettings;
using Lykke.SettingsReader.Attributes;
using System;

namespace Lykke.Service.Dynamic.Job.Settings
{
    public class DynamicJobSettings
    {
        public DbSettings Db { get; set; }
        public string InsightApiUrl { get; set; }
        public int MinConfirmations { get; set; }
        public TimeSpan BalanceCheckerInterval { get; set; }
        public TimeSpan BroadcastCheckerInterval { get; set; }

        [Optional]
        public ChaosSettings ChaosKitty { get; set; }
    }
}
