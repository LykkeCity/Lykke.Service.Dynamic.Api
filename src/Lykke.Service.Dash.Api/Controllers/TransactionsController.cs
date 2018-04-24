using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System.Net;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using Common;
using Common.Log;
using Lykke.Common.Api.Contract.Responses;
using Lykke.Service.BlockchainApi.Contract.Transactions;
using Lykke.Service.BlockchainApi.Contract;
using Lykke.Service.Dash.Api.Core.Domain;
using Lykke.Service.Dash.Api.Core.Repositories;
using Lykke.Service.Dash.Api.Services;
using Lykke.Service.Dash.Api.Helpers;

namespace Lykke.Service.Dash.Api.Controllers
{
    [Route("api/transactions")]
    public class TransactionsController : Controller
    {
        private readonly ILog _log;
        private readonly IDashService _dashService;
        private readonly IBuildRepository _buildRepository;

        public TransactionsController(ILog log, 
            IDashService dashService,
            IBuildRepository buildRepository)
        {
            _log = log;
            _dashService = dashService;
            _buildRepository = buildRepository;
        }

        [HttpPost("single")]
        [ProducesResponseType(typeof(BuildTransactionResponse), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> Build([Required, FromBody] BuildSingleTransactionRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState.ToErrorResponse());
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

            var build = await _buildRepository.GetAsync(request.OperationId);
            if (build != null)
            {
                return Ok(new BuildTransactionResponse()
                {
                    TransactionContext = build.TransactionContext
                });
            }

            var amount = Conversions.CoinsFromContract(request.Amount, Asset.Dash.Accuracy);
            var fromAddressBalance = await _dashService.GetAddressBalance(request.FromAddress);
            var fee = _dashService.GetFee();
            var requiredBalance = request.IncludeFee ? amount : amount + fee;

            if (amount < fee)
            {
                return BadRequest(BlockchainErrorResponse.FromKnownError(BlockchainErrorCode.AmountIsTooSmall));
            }
            if (requiredBalance > fromAddressBalance)
            {
                return BadRequest(BlockchainErrorResponse.FromKnownError(BlockchainErrorCode.NotEnoughtBalance));
            }

            await _log.WriteInfoAsync(nameof(TransactionsController), nameof(Build),
                request.ToJson(), "Build transaction");

            var transactionContext = await _dashService.BuildTransactionAsync(request.OperationId, fromAddress, 
                toAddress, amount, request.IncludeFee);

            await _buildRepository.AddAsync(request.OperationId, transactionContext);

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
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> Broadcast([Required, FromBody] BroadcastTransactionRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState.ToErrorResponse());
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

            await _log.WriteInfoAsync(nameof(TransactionsController), nameof(Broadcast),
                request.ToJson(), "Broadcast transaction");

            await _dashService.BroadcastAsync(transaction, request.OperationId);

            return Ok();
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

            await _log.WriteInfoAsync(nameof(TransactionsController), nameof(DeleteBroadcast),
                new { operationId = operationId }.ToJson(), 
                "Delete broadcast");

            await _buildRepository.DeleteAsync(operationId);
            await _dashService.DeleteBroadcastAsync(broadcast);

            return Ok();
        }

        [HttpPost("history/from/{address}/observation")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public IActionResult AddObservationFromAddress([Required] string address)
        {
            var dashAddress = _dashService.GetBitcoinAddress(address);
            if (dashAddress == null)
            {
                return BadRequest(ErrorResponse.Create($"{nameof(address)} is not a valid"));
            }

            return Ok();
        }

        [HttpPost("history/to/{address}/observation")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public IActionResult AddObservationToAddress([Required] string address)
        {
            var dashAddress = _dashService.GetBitcoinAddress(address);
            if (dashAddress == null)
            {
                return BadRequest(ErrorResponse.Create($"{nameof(address)} is not a valid"));
            }

            return Ok();
        }

        [HttpGet("history/from/{address}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(HistoricalTransactionContract[]))]
        public async Task<IActionResult> GetHistoryFromAddress([Required] string address,
            [Required, FromQuery] int take, 
            [FromQuery] string afterHash)
        {
            var dashAddress = _dashService.GetBitcoinAddress(address);
            if (dashAddress == null)
            {
                return BadRequest(ErrorResponse.Create($"{nameof(address)} is not a valid"));
            }

            var txs = await _dashService.GetFromAddressTxs(address, take, afterHash);

            return Ok(txs.Select(f => f.ToHistoricalTransactionContract(address, true)));
        }

        [HttpDelete("history/from/{address}/observation")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public IActionResult DeleteObservationFromAddress([Required] string address)
        {
            var dashAddress = _dashService.GetBitcoinAddress(address);
            if (dashAddress == null)
            {
                return BadRequest(ErrorResponse.Create($"{nameof(address)} is not a valid"));
            }

            return Ok();
        }

        [HttpDelete("history/to/{address}/observation")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public IActionResult DeleteObservationToAddress([Required] string address)
        {
            var dashAddress = _dashService.GetBitcoinAddress(address);
            if (dashAddress == null)
            {
                return BadRequest(ErrorResponse.Create($"{nameof(address)} is not a valid"));
            }

            return Ok();
        }
    }
}
