using Lykke.Service.Dash.Api.Models;
using Lykke.Service.BlockchainApi.Contract.Balances;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Threading.Tasks;
using Lykke.Service.Dash.Api.Core.Services;
using Lykke.Service.Dash.Api.Core.Repositories;
using Microsoft.AspNetCore.Http;
using System.Linq;
using Lykke.Service.Dash.Api.Helpers;
using Lykke.Service.BlockchainApi.Contract;

namespace Lykke.Service.Dash.Api.Controllers
{
    [Route("api/balances")]
    public class BalancesController : Controller
    {
        private readonly IDashService _dashService;
        private readonly IBalanceRepository _balanceRepository;
        private readonly IBalancePositiveRepository _balancePositiveRepository;

        public BalancesController(IDashService dashService,
            IBalanceRepository balanceRepository,
            IBalancePositiveRepository balancePositiveRepository)
        {
            _dashService = dashService;
            _balanceRepository = balanceRepository;
            _balancePositiveRepository = balancePositiveRepository;
        }

        [HttpGet]
        public async Task<PaginationResponse<WalletBalanceContract>> Get([Required, FromQuery] int take, [FromQuery] string continuation)
        {
            var result = await _balancePositiveRepository.GetAsync(take, continuation);
            
            return PaginationResponse.From(
                result.Continuation, 
                result.Items.Select(f => f.ToWalletBalanceContract()).ToArray());
       }

        [HttpPost("{address}/observation")]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        public async Task<IActionResult> AddToObservations([Required] string address)
        {
            if (string.IsNullOrEmpty(address))
            {
                return BadRequest(ErrorResponse.Create($"{nameof(address)} is null or empty"));
            }

            var validAddress = _dashService.GetBitcoinAddress(address) != null;
            if (!validAddress)
            {
                return BadRequest(ErrorResponse.Create($"{nameof(address)} is not valid"));
            }

            var balance = await _balanceRepository.GetAsync(address);
            if (balance != null)
            {
                return new StatusCodeResult(StatusCodes.Status409Conflict);
            }

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

            var balance = await _balanceRepository.GetAsync(address);
            if (balance == null)
            {
                return new StatusCodeResult(StatusCodes.Status204NoContent);
            }

            await _balanceRepository.DeleteAsync(address);
            await _balancePositiveRepository.DeleteAsync(address);

            return Ok();
        }
    }
}
