using Lykke.Service.BlockchainApi.Contract.Addresses;
using Lykke.Service.Dynamic.Api.Services;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Net;

namespace Lykke.Service.Dynamic.Api.Controllers
{
    [Route("api/addresses")]
    public class AddressesController : Controller
    {
        private readonly IDynamicService _dynamicService;

        public AddressesController(IDynamicService dynamicService)
        {
            _dynamicService = dynamicService;
        }

        [HttpGet("{address}/validity")]
        [ProducesResponseType(typeof(AddressValidationResponse), (int)HttpStatusCode.OK)]
        public IActionResult GetAddressValidity([Required] string address)
        {
            return Ok(new AddressValidationResponse()
            {
                IsValid = _dynamicService.GetBitcoinAddress(address) != null
            });
        }
    }
}
