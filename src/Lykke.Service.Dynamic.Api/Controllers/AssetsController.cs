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

namespace Lykke.Service.Dynamic.Api.Controllers
{
    [Route("api/assets")]
    public class AssetsController : Controller
    {
        [HttpGet]
        //public PaginationResponse<AssetResponse> Get([Required, FromQuery] int take, [FromQuery] string continuation)
        //[ProducesResponseType(typeof(PaginationResponse<AssetResponse>), (int)HttpStatusCode.OK)]
        //[ProducesResponseType(typeof(AssetResponse), StatusCodes.Status400BadRequest)]
        //[ProducesResponseType(typeof(AssetResponse), StatusCodes.Status204NoContent)]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        public PaginationResponse<AssetResponse> Get([Required, FromQuery] string take, [FromQuery] string continuation)
        {
            //mark 20181005 adding take catch per lykke automated testing. Take should be int
            //try
            //{

            //    var emptyList = new List<string>();
            //    int num1;
            //    string testTake = take.ToString();
            //    bool res = int.TryParse(testTake, out num1);
            //    if (res == false)
            //    {
            //        //mark schroeder20181005 modifying to match Lykke expetedcodes
            //        //return NotFound();

            //        // return BadRequest(ErrorResponse.Create($"{nameof(Asset.ToString())} is not a valid"));
            //        //ThrowBadRequest();
            //        //return StatusCode(204);
            //        //////var assets = new AssetResponse[] { Asset.Dynamic.BadAssetResponse() };
            //        //////this.StatusCode(204);
            //        //////return PaginationResponse.From("", assets);
            //        //return PaginationResponse.From("", assets);
            //        //return false;
            //        //var nullassets = new AssetResponse[] { Asset.Dynamic.ToAssetResponse() };
            //        //nullassets
            //        //return PaginationResponse.From("", emptyList);              
            //        //ErrorResponse(400);
            //        var assets = new AssetResponse[] { Asset.Dynamic.BadAssetResponse() };

            //        return BadRequest;
            //    }
            //    else
            //    {
                    var assets = new AssetResponse[] { Asset.Dynamic.ToAssetResponse() };
                    return PaginationResponse.From("", assets);
            //    }

            //}
            //catch 
            //{
            //    ThrowNoContent();
            //    return null;
               
            //}
                      
        }

        [HttpGet("{assetId}")]
        [ProducesResponseType(typeof(AssetResponse), StatusCodes.Status200OK)]
       // [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.NoContent)]
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

        public StatusCodeResult ThrowNoContent()
        {
            return new StatusCodeResult(StatusCodes.Status204NoContent);           
        }
        public StatusCodeResult ThrowBadRequest()
        {
           return new StatusCodeResult(StatusCodes.Status400BadRequest);
        }
    }
}
