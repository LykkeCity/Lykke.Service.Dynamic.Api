using System;
using System.Threading.Tasks;
using Common;
using Common.Log;
using Lykke.Service.Dynamic.Job.Services;

namespace Lykke.Service.Dynamic.Job.PeriodicalHandlers
{
    public class BroadcastHandler : TimerPeriod
    {
        private ILog _log;
        private IPeriodicalService _periodicalService;

        public BroadcastHandler(TimeSpan period, ILog log, IPeriodicalService periodicalService) :
            base(nameof(BroadcastHandler), (int)period.TotalMilliseconds, log)
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
