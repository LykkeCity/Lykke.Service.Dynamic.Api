using Lykke.Service.Dash.Api.Core.Settings.SlackNotifications;

namespace Lykke.Service.Dash.Job.Settings
{
    public class AppSettings
    {
        public DashJobSettings DashJob { get; set; }
        public SlackNotificationsSettings SlackNotifications { get; set; }
    }
}
