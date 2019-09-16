using Lykke.Service.BlockchainApi.Contract;
using Lykke.Service.BlockchainApi.Contract.Assets;
using Lykke.Service.Dynamic.Api.Core.Domain;
using Lykke.Service.Dynamic.Api.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace Lykke.Service.Dynamic.Api.Controllers
{
    [Route("api/assets")]
    public class AssetsController : Controller
    {
        [HttpGet]
        public IActionResult Get([Required, FromQuery] int take, [FromQuery] string continuation)
        {
            if (take <= 0)
            {
                return BadRequest();
            }

            var assets = new AssetResponse[] { Asset.Dynamic.ToAssetResponse() };

            return Ok(PaginationResponse.From("", assets));
        }

        [HttpGet("{assetId}")]
        [ProducesResponseType(typeof(AssetResponse), StatusCodes.Status200OK)]
        public IActionResult GetAsset([Required] string assetId)
        {
            if(Asset.Dynamic.Id != assetId)
            {
                return NoContent();
            }

            return Ok(Asset.Dynamic.ToAssetResponse());
        }
    }
}
