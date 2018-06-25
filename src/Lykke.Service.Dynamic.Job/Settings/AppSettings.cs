using Lykke.Service.Dynamic.Api.Core.Settings.SlackNotifications;

namespace Lykke.Service.Dynamic.Job.Settings
{
    public class AppSettings
    {
        public DynamicJobSettings DynamicJob { get; set; }
        public SlackNotificationsSettings SlackNotifications { get; set; }
    }
}
