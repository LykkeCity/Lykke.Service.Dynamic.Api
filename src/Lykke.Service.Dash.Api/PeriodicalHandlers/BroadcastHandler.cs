using System;
using System.Threading.Tasks;
using Common;
using Common.Log;
using Lykke.Service.Dash.Api.Core.Services;

namespace Lykke.Service.Dash.Api.PeriodicalHandlers
{
    public class BroadcastHandler : TimerPeriod
    {
        private ILog _log;
        private IDashService _dashService;

        public BroadcastHandler(int period, ILog log, IDashService dashService) :
            base(nameof(BroadcastHandler), period, log)
        {
            _log = log;
            _dashService = dashService;
        }

        public override async Task Execute()
        {
            try
            {
                await _dashService.UpdateBroadcasts();
            }
            catch (Exception ex)
            {
                await _log.WriteErrorAsync(nameof(BroadcastHandler), nameof(Execute),
                    "Failed to update broadcasts", ex);
            }
        }
    }
}
