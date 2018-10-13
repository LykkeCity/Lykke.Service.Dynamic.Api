using Lykke.Service.BlockchainApi.Contract;
using Lykke.Service.BlockchainApi.Contract.Assets;
using Lykke.Service.Dynamic.Api.Core.Domain;
using Lykke.Service.Dynamic.Api.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System;
using System.IO;
using Lykke.Common.Api.Contract.Responses;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Lykke.Service.Dynamic.Api.Controllers
{
    [Route("api/assets")]
    public class AssetsController : Controller
    {        
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(PaginationResponse<AssetResponse>[]))]      
        public IActionResult Get([Required, FromQuery] string take, [FromQuery] string continuation)
        {
            if (take != null)
            {
                var emptyList = new List<string>();
                int num1;
                string testTake = take.ToString();
                bool res = int.TryParse(testTake, out num1);
                if (res == false)
                {
                    return BadRequest(ErrorResponse.Create($"{nameof(take)} is not a valid number"));
                }

                var assets = new AssetResponse[] { Asset.Dynamic.ToAssetResponse() };
                return Ok(PaginationResponse.From("", assets));
            }
            else
            {
                return BadRequest(ErrorResponse.Create($"{nameof(take)} is not a valid number"));
            }            
           
        }

        [HttpGet("{assetId}")]
        [ProducesResponseType(typeof(AssetResponse), StatusCodes.Status200OK)]      
        public IActionResult GetAsset([Required] string assetId)
        {
            if(Asset.Dynamic.Id != assetId)
            {
                //mark schroeder20181005 modifying to match Lykke expetedcodes
                //return NotFound();
                return NoContent();
            }

            return Ok(Asset.Dynamic.ToAssetResponse());
        }      
    }
}
