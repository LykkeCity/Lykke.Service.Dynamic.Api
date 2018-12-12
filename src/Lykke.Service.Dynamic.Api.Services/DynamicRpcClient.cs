using Common.Log;
using Lykke.Service.Dynamic.Api.Core.Domain.InsightClient;
using Lykke.Service.Dynamic.Api.Core.Services;
using NBitcoin.RPC;
using NBitcoin.Dynamic.RPC;
using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Collections.Generic;
using NBitcoin;
using Lykke.Service.Dynamic.Api.Core.Settings.ServiceSettings;

namespace Lykke.Service.Dynamic.Api.Services
{
    public class DynamicRpcClient : IDynamicRpcClient
    {
        private readonly ILog _log;

        private const decimal COIN = 100000000;
        private DynamicRPCClient rpc;
        private readonly string _userName;
        private readonly string _password;
        private readonly uint _rpcPort = 33350;
        private readonly string _url;
        private readonly string _network;
        private int nBlockHeight = 0;

        public DynamicRpcClient(ILog log, RpcSettings rpcSettings, string network)
        {
            _log = log;
            _network = network;
            _userName = rpcSettings.UserName;
            _password = rpcSettings.Password;
            _rpcPort = rpcSettings.Port;
            _url = rpcSettings.Url;
            InitRPC();
        }

        private void InitRPC()
        {
            var credential = new NetworkCredential(_userName, _password);
            var net = Network.GetNetwork(_network);
            var creds = new RPCCredentialString
            {
                Server = _url + Convert.ToUInt32(_rpcPort),
                UserPassword = credential
            };
            rpc = new DynamicRPCClient(creds, net);
        }

        public async Task<decimal> GetBalance(string address, int minConfirmations)
        {
            var utxos = await GetTxsUnspentAsync(address, minConfirmations); 
            return utxos.Sum(f => f.Amount);
        }

        public async Task<long> GetLatestBlockHeight()
        {
            int height = 0;
            try
            {
                height = await rpc.GetBlockCountAsync();
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to get last block height.", ex);
            }
            nBlockHeight = height;
            return height;
        }

        bool IsLockTimeEpoch(int nLockTime)
        {
            if (nLockTime > 1536300350)
                return true;

            return false;
        }

        int GetCurrentEpochTime()
        {
            TimeSpan t = DateTime.UtcNow - new DateTime(1970, 1, 1);
            return (int)t.TotalSeconds;
        }

        public async Task<Tx> GetTx(string txid)
        {
            Tx tx;
            try
            {
                JsonTransaction jsonTx = await rpc.GetTransactionAsync(txid);
                int height = await rpc.GetBlockCountAsync();
                tx = LoadTxFromRPCJson(jsonTx, height);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to get transaction '{txid}'.", ex);
            }
            return tx;
        }

        private Tx LoadTxFromRPCJson(JsonTransaction jsonResponse, int blockHeight)
        {
            var tx = new Tx();
            NBitcoin.Dynamic.RPC.Transaction rpcTransaction = jsonResponse.result;
            tx.BlockHeight = rpcTransaction.height;
            tx.Confirmations = blockHeight - rpcTransaction.height;
            
            tx.Time = rpcTransaction.time;
            tx.Txid = rpcTransaction.txid;
            if (IsLockTimeEpoch(rpcTransaction.locktime)) 
            {
                // locktime = epoch time
                tx.TxLock = rpcTransaction.locktime > GetCurrentEpochTime() ? true : false;
            }
            else
            {
                // locktime = block height
                tx.TxLock = rpcTransaction.locktime > blockHeight ? true : false;
            }
            
            // popluate vin
            double inSatoshis = 0;
            List<TxVin> listTxVin = new List<TxVin>();
            foreach (Vin input in rpcTransaction.vin)
            {
                TxVin txIn = new TxVin
                {
                    Addr = input.address,
                    Txid = input.txid,
                    Value = (decimal)input.value
                };
                inSatoshis = inSatoshis + input.value;
                listTxVin.Add(txIn);
            }
            tx.Vin = listTxVin.ToArray();
            // popluate vout
            double outSatoshis = 0;
            List<TxVout> listTxVout = new List<TxVout>();
            foreach (Vout output in rpcTransaction.vout)
            {
                TxVout txOut = new TxVout();
                List<string> addresses = new List<string>();
                foreach (string address in output.scriptPubKey.addresses)
                {
                    addresses.Add(address);
                }
                if (addresses.Count() > 0) {
                    txOut.ScriptPubKey = new Core.Domain.InsightClient.ScriptPubKey();
                    txOut.ScriptPubKey.Addresses = addresses.ToArray();
                    txOut.Txid = output.scriptPubKey.hex;
                    txOut.Value = (decimal)output.value;
                    outSatoshis = outSatoshis + output.value;
                    listTxVout.Add(txOut);
                }
            }
            tx.Vout = listTxVout.ToArray();
            tx.Fees = (decimal)(inSatoshis - outSatoshis);
            return tx;
        }

        public async Task<Tx[]> GetAddressTxs(string address, int continuation)
        {
            int counter = 1;
            List<Tx> listTxs = new List<Tx>();
            try
            {
                int height = await rpc.GetBlockCountAsync();
                JsonAddressTxIDs jsonAddressTxIDs = await rpc.GetAddressTxIDsAsync(address);
                foreach (string txid in jsonAddressTxIDs.result)
                {
                    if (counter > continuation)
                    {
                        JsonTransaction jsonTx = await rpc.GetTransactionAsync(txid);
                        Tx tx = LoadTxFromRPCJson(jsonTx, height);
                        listTxs.Add(tx);
                    }
                    counter++;
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to get address transaction ids for '{address}'.", ex);
            }
            return listTxs.ToArray();
        }

        private TxUnspent[] LoadTxUnspentFromRPCJson(JsonUTXOs jsonResponse, int blockHeight)
        {
            List<TxUnspent> listTxUnspent = new List<TxUnspent>();
            foreach(UTXO utxo in jsonResponse.result)
            {
                TxUnspent TxUnspent = new TxUnspent();
                TxUnspent.Amount = (decimal)utxo.satoshis / COIN;
                TxUnspent.Confirmations = blockHeight - utxo.height;
                TxUnspent.ScriptPubKey = utxo.script;
                TxUnspent.Satoshis = utxo.satoshis;
                TxUnspent.Txid = utxo.txid;
                TxUnspent.Vout = utxo.outputIndex;
                listTxUnspent.Add(TxUnspent);
            }
            return listTxUnspent.ToArray();
        }

        public async Task<TxUnspent[]> GetTxsUnspentAsync(string address, int minConfirmations)
        {
            long nCurrentHeight = await GetLatestBlockHeight();
            TxUnspent[] txsUnspent;
            try
            {
                JsonUTXOs jsonResponse = await rpc.GetAddressUTXOsAsync(address);
                txsUnspent = LoadTxUnspentFromRPCJson(jsonResponse, (int)nCurrentHeight);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to get {nameof(TxUnspent)}[] for address='{address}'", ex);
            }

            if (txsUnspent == null)
            {
                return new List<TxUnspent>().ToArray();
            }

            return txsUnspent.Where(f => f.Confirmations >= minConfirmations).ToArray();
        }

        public async Task<TxBroadcast> BroadcastTxAsync(string transactionHex)
        {
            try
            {
                NBitcoin.Transaction tx = new NBitcoin.Transaction(transactionHex);
                await rpc.SendRawTransactionAsync(tx);
                var txBroadcast = new TxBroadcast();
                txBroadcast.Txid = tx.GetHash().ToString();
                return txBroadcast;
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to get {nameof(TxUnspent)}[] for url='{transactionHex}'", ex);
            }
        }
    }
}
