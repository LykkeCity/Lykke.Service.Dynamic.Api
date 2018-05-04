using Autofac;
using Common.Log;
using Lykke.Service.Dash.Api.AzureRepositories.BroadcastInProgress;
using Lykke.Service.Dash.Api.Core.Services;
using Lykke.Service.Dash.Api.Core.Repositories;
using Lykke.Service.Dash.Api.Services;
using Lykke.SettingsReader;
using Lykke.Service.Dash.Api.AzureRepositories.Balance;
using Lykke.Service.Dash.Api.AzureRepositories.BalancePositive;
using Lykke.Service.Dash.Job.PeriodicalHandlers;
using Lykke.Service.Dash.Api.AzureRepositories.Broadcast;
using Lykke.Service.Dash.Job.Settings;
using Lykke.Service.Dash.Job.Services;
using Lykke.Common.Chaos;

namespace Lykke.Service.Dash.Job.Modules
{
    public class JobModule : Module
    {
        private readonly IReloadingManager<DashJobSettings> _settings;
        private readonly ILog _log;

        public JobModule(IReloadingManager<DashJobSettings> settings, ILog log)
        {
            _settings = settings;
            _log = log;
        }

        protected override void Load(ContainerBuilder builder)
        {
            var connectionStringManager = _settings.ConnectionString(x => x.Db.DataConnString);

            builder.RegisterChaosKitty(_settings.CurrentValue.ChaosKitty);

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

            builder.RegisterType<PeriodicalService>()
                .As<IPeriodicalService>()
                .WithParameter(TypedParameter.From(_settings.CurrentValue.MinConfirmations))
                .SingleInstance();            

            builder.RegisterType<BalanceHandler>()
                .As<IStartable>()
                .AutoActivate()
                .WithParameter("period", _settings.CurrentValue.BalanceCheckerInterval)
                .SingleInstance();

            builder.RegisterType<BroadcastHandler>()
                .As<IStartable>()
                .AutoActivate()
                .WithParameter("period", _settings.CurrentValue.BroadcastCheckerInterval)
                .SingleInstance();
        }
    }
}
