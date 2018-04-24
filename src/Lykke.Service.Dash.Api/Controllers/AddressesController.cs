using Lykke.Service.BlockchainApi.Contract.Addresses;
using Lykke.Service.Dash.Api.Services;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Net;

namespace Lykke.Service.Dash.Api.Controllers
{
    [Route("api/addresses")]
    public class AddressesController : Controller
    {
        private readonly IDashService _dashService;

        public AddressesController(IDashService dashService)
        {
            _dashService = dashService;
        }

        [HttpGet("{address}/validity")]
        [ProducesResponseType(typeof(AddressValidationResponse), (int)HttpStatusCode.OK)]
        public IActionResult GetAddressValidity([Required] string address)
        {
            return Ok(new AddressValidationResponse()
            {
                IsValid = _dashService.GetBitcoinAddress(address) != null
            });
        }
    }
}
