using Autofac;
using Common.Log;
using Lykke.Service.Dynamic.Api.AzureRepositories.BroadcastInProgress;
using Lykke.Service.Dynamic.Api.Core.Services;
using Lykke.Service.Dynamic.Api.Core.Repositories;
using Lykke.Service.Dynamic.Api.Core.Settings.ServiceSettings;
using Lykke.Service.Dynamic.Api.Services;
using Lykke.SettingsReader;
using Lykke.Service.Dynamic.Api.AzureRepositories.Balance;
using Lykke.Service.Dynamic.Api.AzureRepositories.BalancePositive;
using Lykke.Service.Dynamic.Api.AzureRepositories.Broadcast;
using Lykke.Service.Dynamic.Api.AzureRepositories.Build;

namespace Lykke.Service.Dynamic.Api.Modules
{
    public class ServiceModule : Module
    {
        private readonly IReloadingManager<DynamicApiSettings> _settings;
        private readonly ILog _log;

        public ServiceModule(IReloadingManager<DynamicApiSettings> settings, ILog log)
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

            builder.RegisterType<BuildRepository>()
                .As<IBuildRepository>()
                .WithParameter(TypedParameter.From(connectionStringManager))
                .SingleInstance();

            builder.RegisterType<DynamicInsightClient>()
                .As<IDynamicInsightClient>()
                .WithParameter("url", _settings.CurrentValue.InsightApiUrl)
                .SingleInstance();

            builder.RegisterType<DynamicDaemonClient>()
                .As<IDynamicDaemonClient>()
                .WithParameter("datadir", _settings.CurrentValue.DaemonDataDir)
                .SingleInstance();

            builder.RegisterType<DynamicService>()
                .As<IDynamicService>()
                .WithParameter("network", _settings.CurrentValue.Network)
                .WithParameter("fee", _settings.CurrentValue.Fee)
                .WithParameter("minConfirmations", _settings.CurrentValue.MinConfirmations)
                .SingleInstance();
        }
    }
}
