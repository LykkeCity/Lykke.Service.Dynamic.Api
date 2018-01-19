using Microsoft.AspNetCore.Mvc;

namespace Lykke.Service.Dash.Api.Controllers
{
    [Route("api/capabilities")]
    public class CapabilitiesController : Controller
    {
        [HttpGet]
        public IActionResult Get()
        {
            var capabilities = new
            {
                isTransactionsRebuildingSupported = false,
                isBatchedTransactionsSupported = false
            };

            return Ok(capabilities);
        }
    }
}
