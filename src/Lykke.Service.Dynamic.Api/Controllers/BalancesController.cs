using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System.Net;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using Common;
using Common.Log;
using Lykke.Common.Api.Contract.Responses;
using Lykke.Service.BlockchainApi.Contract;
using Lykke.Service.BlockchainApi.Contract.Balances;
using Lykke.Service.Dynamic.Api.Core.Repositories;
using Lykke.Service.Dynamic.Api.Helpers;
using Lykke.Service.Dynamic.Api.Services;

namespace Lykke.Service.Dynamic.Api.Controllers
{
    [Route("api/balances")]
    public class BalancesController : Controller
    {
        private readonly ILog _log;
        private readonly IDynamicService _dynamicService;
        private readonly IBalanceRepository _balanceRepository;
        private readonly IBalancePositiveRepository _balancePositiveRepository;

        public BalancesController(ILog log,
            IDynamicService dynamicService,
            IBalanceRepository balanceRepository,
            IBalancePositiveRepository balancePositiveRepository)
        {
            _log = log;
            _dynamicService = dynamicService;
            _balanceRepository = balanceRepository;
            _balancePositiveRepository = balancePositiveRepository;
        }

        [HttpGet]
        public async Task<IActionResult> Get([Required, FromQuery] int take, [FromQuery] string continuation)
        {
            if (take <= 0)
            {
                return BadRequest();
            }

            var result = await _balancePositiveRepository.GetAsync(take, continuation);
            
            return Ok(PaginationResponse.From(
                result.ContinuationToken,
                result.Entities.Select(f => f.ToWalletBalanceContract()).ToArray()
            ));
        }

        [HttpPost("{address}/observation")]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        public async Task<IActionResult> AddToObservations([Required] string address)
        {
            if (string.IsNullOrEmpty(address))
            {
                return BadRequest(ErrorResponse.Create($"{nameof(address)} is null or empty"));
            }

            var validAddress = _dynamicService.GetBitcoinAddress(address) != null;
            if (!validAddress)
            {
                return BadRequest(ErrorResponse.Create($"{nameof(address)} is not valid"));
            }

            var balance = await _balanceRepository.GetAsync(address);
            if (balance != null)
            {
                return new StatusCodeResult(StatusCodes.Status409Conflict);
            }

            await _log.WriteInfoAsync(nameof(BalancesController), nameof(AddToObservations),
                new { address = address }.ToJson(), "Add address to observations");

            await _balanceRepository.AddAsync(address);

            return Ok();
        }

        [HttpDelete("{address}/observation")]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        public async Task<IActionResult> DeleteFromObservations([Required] string address)
        {
            if (string.IsNullOrEmpty(address))
            {
                return BadRequest(ErrorResponse.Create($"{nameof(address)} is null or empty"));
            }

            var validAddress = _dynamicService.GetBitcoinAddress(address) != null;
            if (!validAddress)
            {
                return BadRequest(ErrorResponse.Create($"{nameof(address)} is not valid"));
            }

            var balance = await _balanceRepository.GetAsync(address);
            if (balance == null)
            {
                return new StatusCodeResult(StatusCodes.Status204NoContent);
            }

            await _log.WriteInfoAsync(nameof(BalancesController), nameof(DeleteFromObservations),
                new { address = address }.ToJson(), "Delete address from observations");

            await _balancePositiveRepository.DeleteAsync(address);
            await _balanceRepository.DeleteAsync(address);

            return Ok();
        }
    }
}
