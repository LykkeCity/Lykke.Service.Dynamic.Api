using System;
using System.Threading.Tasks;
using Common;
using Common.Log;
using Lykke.Service.Dash.Api.Core.Services;

namespace Lykke.Service.Dash.Api.PeriodicalHandlers
{
    public class BalanceHandler : TimerPeriod
    {
        private ILog _log;
        private IDashService _dashService;

        public BalanceHandler(int period, ILog log, IDashService dashService) :
            base(nameof(BalanceHandler), period, log)
        {
            _log = log;
            _dashService = dashService;
        }

        public override async Task Execute()
        {
            try
            {
                await _dashService.UpdateBalances();
            }
            catch (Exception ex)
            {
                await _log.WriteErrorAsync(nameof(BalanceHandler), nameof(Execute), 
                    "Failed to update balances", ex);
            }
        }
    }
}
