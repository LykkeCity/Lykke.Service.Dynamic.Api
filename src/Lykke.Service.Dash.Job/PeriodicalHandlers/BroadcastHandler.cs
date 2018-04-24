using System;
using System.Threading.Tasks;
using Common;
using Common.Log;
using Lykke.Service.Dash.Job.Services;

namespace Lykke.Service.Dash.Job.PeriodicalHandlers
{
    public class BroadcastHandler : TimerPeriod
    {
        private ILog _log;
        private IPeriodicalService _periodicalService;

        public BroadcastHandler(int period, ILog log, IPeriodicalService periodicalService) :
            base(nameof(BroadcastHandler), period, log)
        {
            _log = log;
            _periodicalService = periodicalService;
        }

        public override async Task Execute()
        {
            try
            {
                await _periodicalService.UpdateBroadcasts();
            }
            catch (Exception ex)
            {
                await _log.WriteErrorAsync(nameof(BroadcastHandler), nameof(Execute),
                    "Failed to update broadcasts", ex);
            }
        }
    }
}
