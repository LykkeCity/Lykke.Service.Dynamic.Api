using Lykke.Service.Dash.Api.Core.Settings.ServiceSettings;
using Lykke.Service.Dash.Api.Core.Settings.SlackNotifications;

namespace Lykke.Service.Dash.Api.Core.Settings
{
    public class AppSettings
    {
        public DashApiSettings DashApiService { get; set; }
        public SlackNotificationsSettings SlackNotifications { get; set; }
    }
}
