using Lykke.Service.Dynamic.Api.Core.Settings.ServiceSettings;
using Lykke.Service.Dynamic.Api.Core.Settings.SlackNotifications;

namespace Lykke.Service.Dynamic.Api.Core.Settings
{
    public class AppSettings
    {
        public DynamicApiSettings DynamicApiService { get; set; }
        public SlackNotificationsSettings SlackNotifications { get; set; }
    }
}
