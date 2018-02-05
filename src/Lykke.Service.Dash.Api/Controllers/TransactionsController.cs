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
using Common.Log;

namespace Lykke.Service.Dash.Api.Controllers
{
    [Route("api/transactions")]
    public class TransactionsController : Controller
    {
        private readonly IDashService _dashService;

        public TransactionsController(ILog log, IDashService dashService)
        {
            _dashService = dashService;
        }

        [HttpPost("single")]
        [ProducesResponseType(typeof(BuildTransactionResponse), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> Build([Required, FromBody] BuildSingleTransactionRequest request)
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
            var fromAddressBalance = await _dashService.GetAddressBalance(request.FromAddress);
            var fee = _dashService.GetFee();
            var requiredBalance = request.IncludeFee ? amount : amount + fee;

            if (amount < fee)
            {
                return Ok(new BuildTransactionResponse()
                {
                    ErrorCode = TransactionExecutionError.AmountIsTooSmall
                });
            }
            if (requiredBalance > fromAddressBalance)
            {
                return Ok(new BuildTransactionResponse()
                {
                    ErrorCode = TransactionExecutionError.NotEnoughtBalance
                });
            }

            var transactionContext = await _dashService.BuildTransactionAsync(request.OperationId, fromAddress, 
                toAddress, amount, request.IncludeFee);

            return Ok(new BuildTransactionResponse()
            {
                TransactionContext = transactionContext
            });
        }

        [HttpPut]
        [ProducesResponseType(StatusCodes.Status501NotImplemented)]
        public IActionResult Rebuild()
        {
            return new StatusCodeResult(StatusCodes.Status501NotImplemented);
        }

        [HttpPost("broadcast")]
        [ProducesResponseType(typeof(BroadcastTransactionResponse), StatusCodes.Status200OK)]
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

            return Ok(new BroadcastTransactionResponse());
        }

        [HttpPost("broadcast/batched")]
        [ProducesResponseType(StatusCodes.Status501NotImplemented)]
        public IActionResult BroadcastBatched()
        {
            return new StatusCodeResult(StatusCodes.Status501NotImplemented);
        }

        [HttpGet("broadcast/single/{operationId}")]
        [ProducesResponseType(typeof(BroadcastedSingleTransactionResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetBroadcast([Required] Guid operationId)
        {
            var broadcast = await _dashService.GetBroadcastAsync(operationId);
            if (broadcast == null)
            {
                return NoContent();
            }

            var amount = broadcast.Amount.HasValue ?
                Conversions.CoinsToContract(broadcast.Amount.Value, Asset.Dash.Accuracy) : "";

            var fee = broadcast.Fee.HasValue ?
                Conversions.CoinsToContract(broadcast.Fee.Value, Asset.Dash.Accuracy) : "";

            return Ok(new BroadcastedSingleTransactionResponse
            {
                OperationId = broadcast.OperationId,
                Hash = broadcast.Hash,
                State = broadcast.State.ToBroadcastedTransactionState(),
                Amount = amount,
                Fee = fee,
                Error = broadcast.Error,
                Timestamp = broadcast.GetTimestamp(),
                Block = broadcast.Block
            });
        }

        [HttpDelete("broadcast/{operationId}")]
        public async Task<IActionResult> DeleteBroadcast([Required] Guid operationId)
        {
            var broadcast = await _dashService.GetBroadcastAsync(operationId);
            if (broadcast == null)
            {
                return NoContent();
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
    }
}
