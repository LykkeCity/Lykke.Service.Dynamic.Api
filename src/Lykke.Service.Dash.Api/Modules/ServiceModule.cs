using Autofac;
using Common.Log;
using Lykke.Service.Dash.Api.AzureRepositories.BroadcastInProgress;
using Lykke.Service.Dash.Api.AzureRepositories.Transaction;
using Lykke.Service.Dash.Api.Core.Services;
using Lykke.Service.Dash.Api.Core.Repositories;
using Lykke.Service.Dash.Api.Core.Settings.ServiceSettings;
using Lykke.Service.Dash.Api.Services;
using Lykke.SettingsReader;
using Lykke.Service.Dash.Api.AzureRepositories.Balance;
using Lykke.Service.Dash.Api.AzureRepositories.BalancePositive;

namespace Lykke.Service.Dash.Api.Modules
{
    public class ServiceModule : Module
    {
        private readonly IReloadingManager<DashApiSettings> _settings;
        private readonly ILog _log;

        public ServiceModule(IReloadingManager<DashApiSettings> settings, ILog log)
        {
            _settings = settings;
            _log = log;
        }

        protected override void Load(ContainerBuilder builder)
        {
            var connectionStringManager = _settings.ConnectionString(x => x.Db.DataConnString);

            builder.RegisterInstance(_log)
                .As<ILog>()
                .SingleInstance();

            builder.RegisterType<HealthService>()
                .As<IHealthService>()
                .SingleInstance();

            builder.RegisterType<StartupManager>()
                .As<IStartupManager>();

            builder.RegisterType<ShutdownManager>()
                .As<IShutdownManager>();

            builder.RegisterType<BroadcastRepository>()
                .As<IBroadcastRepository>()
                .WithParameter(TypedParameter.From(connectionStringManager))
                .SingleInstance();

            builder.RegisterType<BroadcastInProgressRepository>()
                .As<IBroadcastInProgressRepository>()
                .WithParameter(TypedParameter.From(connectionStringManager))
                .SingleInstance();

            builder.RegisterType<BalanceRepository>()
                .As<IBalanceRepository>()
                .WithParameter(TypedParameter.From(connectionStringManager))
                .SingleInstance();

            builder.RegisterType<BalancePositiveRepository>()
                .As<IBalancePositiveRepository>()
                .WithParameter(TypedParameter.From(connectionStringManager))
                .SingleInstance();            

            builder.RegisterType<DashInsightClient>()
                .As<IDashInsightClient>()
                .WithParameter("url", _settings.CurrentValue.InsightApiUrl)
                .SingleInstance();

            builder.RegisterType<DashService>()
                .As<IDashService>()
                .WithParameter(TypedParameter.From(_settings.CurrentValue))
                .SingleInstance();
        }
    }
}
