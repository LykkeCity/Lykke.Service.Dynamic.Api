using Common.Log;
using Flurl.Http;
using Lykke.Service.Dynamic.Api.Core.Domain.InsightClient;
using Lykke.Service.Dynamic.Api.Core.Services;
using Lykke.Service.Dynamic.Api.Services.Helpers;
using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace Lykke.Service.Dynamic.Api.Services
{
    public class DynamicInsightClient : IDynamicInsightClient
    {
        private readonly ILog _log;
        private readonly string _url;

        public DynamicInsightClient(ILog log, string url)
        {
            _log = log;
            _url = url;
        }

        public async Task<decimal> GetBalance(string address, int minConfirmations)
        {
            var utxos = await GetTxsUnspentAsync(address, minConfirmations);
            
            return utxos.Sum(f => f.Amount);
        }

        public async Task<long> GetLatestBlockHeight()
        {
            BlocksInfo blocksInfo; 
            var url = $"{_url}/blocks?limit=1";

            try
            {
                blocksInfo = await GetJson<BlocksInfo>(url);
            }
            catch (FlurlHttpException ex) when (ex.Call.Response.StatusCode == HttpStatusCode.NotFound)
            {
                return 0;
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to get {nameof(BlocksInfo)} for url='{url}'", ex);
            }

            if (blocksInfo == null)
            {
                throw new Exception($"{nameof(blocksInfo)} can not be null");
            }
            if (blocksInfo.Blocks == null)
            {
                throw new Exception($"{nameof(blocksInfo)}{nameof(blocksInfo.Blocks)} can not be null");
            }
            if (blocksInfo.Blocks.Length == 0)
            {
                throw new Exception($"{nameof(blocksInfo)}{nameof(blocksInfo.Blocks)} must have at least one entry");
            }

            return blocksInfo.Blocks[0].Height;
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
                throw new Exception($"Failed to get {nameof(Tx)} for url='{url}'", ex);
            }
        }

        public async Task<Tx[]> GetAddressTxs(string address, int continuation)
        {
            AddressTxs addressTxs;
            var start = continuation;
            var end = start + 50;
            var url = $"{_url}/addrs/{address}/txs?from={start}&to={end}";

            try
            {
                addressTxs = await GetJson<AddressTxs>(url);
            }
            catch (FlurlHttpException ex) when (ex.Call.Response.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to get {nameof(AddressTxs)} for url='{url}'", ex);
            }

            if (addressTxs == null)
            {
                throw new Exception($"{nameof(addressTxs)} can not be null");
            }

            return addressTxs.Items;
        }

        public async Task<TxUnspent[]> GetTxsUnspentAsync(string address, int minConfirmations)
        {
            TxUnspent[] txsUnspent;
            var url = $"{_url}/addr/{address}/utxo";

            try
            {
                txsUnspent =  await GetJson<TxUnspent[]>(url);
            }
            catch (FlurlHttpException ex) when (ex.Call.Response.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to get {nameof(TxUnspent)}[] for url='{url}'", ex);
            }

            if (txsUnspent == null)
            {
                return new TxUnspent[] { };
            }

            return txsUnspent.Where(f => f.Confirmations >= minConfirmations).ToArray();
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
                throw new Exception($"Failed to post and get {nameof(TxBroadcast)} for url='{url}' and data='{data}'", ex);
            }
        }

        private async Task<T> GetJson<T>(string url, int tryCount = 3)
        {
            bool NeedToRetryException(Exception ex)
            {
                if (ex is FlurlHttpException flurlException)
                {
                    return true;
                }

                return false;
            }

            return await Retry.Try(() => url.GetJsonAsync<T>(), NeedToRetryException, tryCount, _log, 100);
        }
    }
}
