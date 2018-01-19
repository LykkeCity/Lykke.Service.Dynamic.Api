using Common.Log;
using Flurl.Http;
using Lykke.Service.Dash.Api.Core.Domain.InsightClient;
using Lykke.Service.Dash.Api.Core.Services;
using Lykke.Service.Dash.Api.Services.Helpers;
using System;
using System.Net;
using System.Threading.Tasks;

namespace Lykke.Service.Dash.Api.Services
{
    public class DashInsightClient : IDashInsightClient
    {
        private readonly ILog _log;
        private readonly string _url;

        public DashInsightClient(ILog log, string url)
        {
            _log = log;
            _url = url;
        }

        public async Task<decimal> GetBalance(string address)
        {
            var url = $"{_url}/addr/{address}?noTxList=1&uid={Guid.NewGuid()}";

            try
            {
                var addr = await GetJson<Address>(url);

                return addr.Balance - addr.UnconfirmedBalance;
            }
            catch (FlurlHttpException ex) when (ex.Call.Response.StatusCode == HttpStatusCode.NotFound)
            {
                return 0;
            }
            catch (Exception ex)
            {
                await _log.WriteErrorAsync(nameof(DashInsightClient), nameof(GetTxsUnspentAsync),
                    $"Failed to get json for url='{url}'", ex);

                throw;
            }
        }

        public async Task<Tx> GetTx(string txid)
        {
            var url = $"{_url}/tx/{txid}";

            try
            {
                return await GetJson<Tx>(url);
            }
            catch (FlurlHttpException ex) when (ex.Call.Response.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }
            catch (Exception ex)
            {
                await _log.WriteErrorAsync(nameof(DashInsightClient), nameof(GetTxsUnspentAsync),
                    $"Failed to get json for url='{url}'", ex);

                throw;
            }
        }

        public async Task<TxUnspent[]> GetTxsUnspentAsync(string address)
        {
            var url = $"{_url}/addr/{address}/utxo";

            try
            {
                return await GetJson<TxUnspent[]>(url);
            }
            catch (FlurlHttpException ex) when (ex.Call.Response.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }
            catch (Exception ex)
            {
                await _log.WriteErrorAsync(nameof(DashInsightClient), nameof(GetTxsUnspentAsync),
                    $"Failed to get json for url='{url}'", ex);

                throw;
            }
        }

        public async Task<TxBroadcast> BroadcastTxAsync(string transactionHex)
        {
            var url = $"{_url}/tx/send";
            var data = new { rawtx = transactionHex };

            try
            {
                return await url
                    .PostJsonAsync(data)
                    .ReceiveJson<TxBroadcast>();
            }
            catch (Exception ex)
            {
                await _log.WriteErrorAsync(nameof(DashInsightClient), nameof(BroadcastTxAsync),
                    $"Failed to post json for url='{url}' and data='{data}'", ex);

                throw;
            }
        }

        private async Task<T> GetJson<T>(string url, int tryCount = 3)
        {
            bool NeedToRetryException(Exception ex)
            {
                if (!(ex is FlurlHttpException flurlException))
                {
                    return false;
                }

                var isTimeout = flurlException is FlurlHttpTimeoutException;
                if (isTimeout)
                {
                    return true;
                }

                if (flurlException.Call.HttpStatus == HttpStatusCode.ServiceUnavailable ||
                    flurlException.Call.HttpStatus == HttpStatusCode.InternalServerError)
                {
                    return true;
                }

                return false;
            }

            return await Retry.Try(() => url.GetJsonAsync<T>(), NeedToRetryException, tryCount, _log);
        }
    }
}
