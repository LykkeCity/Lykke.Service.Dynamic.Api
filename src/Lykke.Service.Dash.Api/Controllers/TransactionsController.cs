using Lykke.Service.BlockchainApi.Contract.Transactions;
using Lykke.Service.Dash.Api.Models;
using Lykke.Service.Dash.Api.Core.Services;
using Lykke.Service.BlockchainApi.Contract;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using System;
using Lykke.Service.Dash.Api.Helpers;
using Lykke.Service.Dash.Api.Core.Domain;

namespace Lykke.Service.Dash.Api.Controllers
{
    [Route("api/transactions")]
    public class TransactionsController : Controller
    {
        private readonly IDashService _dashService;

        public TransactionsController(IDashService dashService)
        {
            _dashService = dashService;
        }

        [HttpPost]
        [ProducesResponseType(typeof(BuildTransactionResponse), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> Build([Required, FromBody] BuildTransactionRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ErrorResponse.Create("ValidationError", ModelState));
            }

            var fromAddress = _dashService.GetBitcoinAddress(request.FromAddress);
            if (fromAddress == null)
            {
                return BadRequest(ErrorResponse.Create($"{nameof(request.FromAddress)} is not a valid"));
            }

            var toAddress = _dashService.GetBitcoinAddress(request.ToAddress);
            if (toAddress == null)
            {
                return BadRequest(ErrorResponse.Create($"{nameof(request.ToAddress)} is not a valid"));
            }

            if (request.AssetId != Asset.Dash.Id)
            {
                return BadRequest(ErrorResponse.Create($"{nameof(request.AssetId)} was not found"));
            }

            var amount = Conversions.CoinsFromContract(request.Amount, Asset.Dash.Accuracy);

            var transactionContext = await _dashService.BuildTransactionAsync(fromAddress, toAddress, 
                amount, request.IncludeFee);

            return Ok(new BuildTransactionResponse()
            {
                TransactionContext = transactionContext
            });
        }

        [HttpPost("broadcast")]
        public async Task<IActionResult> Broadcast([Required, FromBody] BroadcastTransactionRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ErrorResponse.Create("ValidationError", ModelState));
            }

            var broadcast = await _dashService.GetBroadcastAsync(request.OperationId);
            if (broadcast != null)
            {
                return new StatusCodeResult(StatusCodes.Status409Conflict);
            }

            var transaction = _dashService.GetTransaction(request.SignedTransaction);
            if (transaction == null)
            {
                return BadRequest(ErrorResponse.Create($"{nameof(request.SignedTransaction)} is not a valid"));
            }

            await _dashService.BroadcastAsync(transaction, request.OperationId);

            return Ok();
        }

        [HttpGet("broadcast/{operationId}")]
        [ProducesResponseType(typeof(BroadcastedTransactionResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetBroadcast([Required] Guid operationId)
        {
            var broadcast = await _dashService.GetBroadcastAsync(operationId);
            if (broadcast == null)
            {
                return new StatusCodeResult(StatusCodes.Status204NoContent);
            }

            return Ok(new BroadcastedTransactionResponse
            {
                OperationId = broadcast.OperationId,
                Hash = broadcast.Hash,
                State = broadcast.State.ToBroadcastedTransactionState(),
                Amount = broadcast.Amount?.ToString(),
                Fee = broadcast.Fee?.ToString(),
                Error = broadcast.Error,
                Timestamp = broadcast.GetTimestamp(),
            });
        }

        [HttpDelete("broadcast/{operationId}")]
        public async Task<IActionResult> DeleteBroadcast([Required] Guid operationId)
        {
            var broadcast = await _dashService.GetBroadcastAsync(operationId);
            if (broadcast == null)
            {
                return new StatusCodeResult(StatusCodes.Status204NoContent);
            }

            await _dashService.DeleteBroadcastAsync(broadcast);

            return Ok();
        }

        //[HttpPost("history/from/{address}/observation")]
        //[ProducesResponseType(typeof(BroadcastedTransactionResponse), StatusCodes.Status200OK)]
        //public async Task<IActionResult> AddObservationFromAddress([Required] string address)
        //{
        //    var dashAddress = _dashService.GetBitcoinAddress(address);
        //    if (dashAddress == null)
        //    {
        //        return BadRequest(ErrorResponse.Create($"{nameof(address)} is not a valid"));
        //    }

        //    return Ok();
        //}

        [HttpGet("broadcast/update")]
        public async Task UpdateBroadcasts()
        {
            await _dashService.UpdateBroadcasts();
        }
    }
}
