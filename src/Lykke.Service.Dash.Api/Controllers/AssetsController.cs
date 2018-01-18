using Lykke.Service.BlockchainApi.Contract;
using Lykke.Service.BlockchainApi.Contract.Assets;
using Lykke.Service.Dash.Api.Core.Domain;
using Lykke.Service.Dash.Api.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace Lykke.Service.Dash.Api.Controllers
{
    [Route("api/assets")]
    public class AssetsController : Controller
    {
        public AssetsController()
        {
        }

        [HttpGet]
        public PaginationResponse<AssetResponse> Get([Required, FromQuery] int take, [FromQuery] string continuation)
        {
            var assets = new AssetResponse[] { Asset.Dash.ToAssetResponse() };

            return PaginationResponse.From("", assets);
        }

        [HttpGet("{assetId}")]
        [ProducesResponseType(typeof(AssetResponse), StatusCodes.Status200OK)]
        public IActionResult GetAsset([Required] string assetId)
        {
            if(Asset.Dash.Id != assetId)
            {
                return NotFound();
            }

            return Ok(Asset.Dash.ToAssetResponse());
        }
    }
}
