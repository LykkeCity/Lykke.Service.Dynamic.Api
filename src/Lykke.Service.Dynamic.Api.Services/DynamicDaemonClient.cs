using Common.Log;
using Flurl.Http;
using Lykke.Service.Dynamic.Api.Core.Domain.InsightClient;
using Lykke.Service.Dynamic.Api.Core.Services;
using Lykke.Service.Dynamic.Api.Services.Helpers;
using NBitcoin.RPC;
using NBitcoin.Dynamic.RPC;
using System;
using System.Linq;
using System.Net;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Collections.Generic;
using NBitcoin;

namespace Lykke.Service.Dynamic.Api.Services
{
    public class DynamicDaemonClient : IDynamicDaemonClient
    {
        private readonly ILog _log;
        private readonly string _datadir;
        private readonly string _url;

        private const decimal COIN = 100000000;
        private const string _configFileName = "dynamic.conf";
        private const string _rpcUserPrefix = "rpcuser=";
        private const string _rpcPasswordPrefix = "rpcpassword=";
        private const string _rpcPortPrefix = "rpcport=";
        private const string _testNetPrefix = "testnet=1";
        private string _userName;
        private string _password;
        private uint _rpcPort;
        private string _network;
        private DynamicRPCDynamicClient rpc;
        private int nBlockHeight = 0;
        /*
         * TODO:
         * make sure tx, address, timestamp and spent indexing are turned on.
         * txindex=1
         * addressindex=1
         * timestampindex=1
         * spentindex=1
         * server=1
         * daemon=1
        */
        public DynamicDaemonClient(ILog log, string datadir)
        {
            _log = log;
            _datadir = datadir;
            _url = "";
            _network = "main";
            ReadConfigFile();
            InitRPC();
        }
       
        private void ReadConfigFile()
        {
            try
            {
                string configFile = Path.Combine(_datadir, _configFileName);
                // Open the file to read from.
                using (StreamReader streamConfigReader = File.OpenText(configFile))
                {
                    string configLine = "";
                    while ((configLine = streamConfigReader.ReadLine()) != null)
                    {
                        if (configLine != null)
                        {
                            if (configLine.Length >= _rpcUserPrefix.Length + 1 && configLine.Substring(0, _rpcUserPrefix.Length) == _rpcUserPrefix)
                            {
                                _userName = configLine.Substring(_rpcUserPrefix.Length, configLine.Length - (_rpcUserPrefix.Length));
                            }
                            else if (configLine.Length >= _rpcPasswordPrefix.Length + 1 && configLine.Substring(0, _rpcPasswordPrefix.Length) == _rpcPasswordPrefix)
                            {
                                _password = configLine.Substring(_rpcPasswordPrefix.Length, configLine.Length - (_rpcPasswordPrefix.Length));
                            }
                            else if (configLine.Length >= _rpcPortPrefix.Length + 1 && configLine.Substring(0, _rpcPortPrefix.Length) == _rpcPortPrefix)
                            {
                                string _strRPCPort = configLine.Substring(_rpcPortPrefix.Length, configLine.Length - (_rpcPortPrefix.Length));
                                if (!uint.TryParse(_strRPCPort, out uint port))
                                {
                                    throw new Exception("Can not convert RPC port to an unsigned integer");
                                }
                                _rpcPort = port;
                            }
                            else if (configLine.Length == _testNetPrefix.Length && configLine == _testNetPrefix)
                            {
                                _network = "testnet";
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to get read configuration file.", ex);
            }
        }

        private void InitRPC()
        {
            NetworkCredential credential = new NetworkCredential(_userName, _password);
            NBitcoin.Network net = NBitcoin.Network.GetNetwork(_network);
            RPCCredentialString creds = new RPCCredentialString();
            creds.Server = "http://127.0.0.1:" + Convert.ToUInt32(_rpcPort);
            creds.UserPassword = credential;
            rpc = new DynamicRPCDynamicClient(creds, net);
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

        Tx ConvertTransactionToTx(Transaction transaction)
        {
            if (transaction == null)
                return null;

            Tx tx = new Tx();
            Coin[] spentCoins = null;
            Money amount = transaction.GetFee(spentCoins);
            tx.Fees = amount.ToDecimal(MoneyUnit.BTC);
            //tx.BlockHeight = ;
            //tx.Time = ;
            tx.Confirmations = nBlockHeight - tx.BlockHeight;
            tx.Txid = transaction.GetHash().ToString();
            tx.TxLock = (transaction.LockTime > 0) ? true : false;
            int i = 0;
            foreach(TxIn txIn in transaction.Inputs)
            {
                TxVin vin = new TxVin();
                vin.Txid = txIn.GetHashCode().ToString();
                //vin.Value = txIn.value;
                //vin.Addr = txIn.Address;
                tx.Vin.SetValue(vin, i);
                i++;
            }
            i = 0;
            foreach (TxOut txOut in transaction.Outputs)
            {
                TxVout vout = new TxVout();
                vout.Txid = txOut.GetHashCode().ToString();
                //vout.ScriptPubKey = txOut.ScriptPubKey.ToString();
                vout.Value = txOut.Value.ToDecimal(MoneyUnit.BTC);
                tx.Vout.SetValue(vout, i);
                i++;
            }
            return tx;
        }

        public async Task<Tx> GetTx(string txid)
        {
            Tx tx;
            try
            {
                uint256 uintTxId = new uint256(txid);
                Transaction transaction;
                transaction = await rpc.GetRawTransactionAsync(uintTxId);
                tx = ConvertTransactionToTx(transaction);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to get last block height.", ex);
            }
            return tx;
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

        private TxUnspent[] LoadTxUnspentFromJsonString(string jsonResponse, int blockHeight)
        {
            
            TxUnspent[] txUnspents;
            JsonTextReader reader = new JsonTextReader(new StringReader(jsonResponse));
            bool dataArrayStarted = false;
            string nextValue = "";
            List<TxUnspent> listTxUnspent = new List<TxUnspent>();
            TxUnspent addTxUnspent = new TxUnspent();
            while (reader.Read())
            {
                if (!dataArrayStarted && reader.TokenType == JsonToken.StartArray)
                {
                    dataArrayStarted = true;
                }
                else if (dataArrayStarted && reader.TokenType == JsonToken.PropertyName && reader.Value != null)
                {
                    nextValue = reader.Value.ToString();
                }
                else if (dataArrayStarted && reader.Value != null && (reader.TokenType == JsonToken.String || reader.TokenType == JsonToken.Integer))
                {
                    if (nextValue == "address")
                    {
                        addTxUnspent = new TxUnspent();
                    }
                    else if (nextValue == "txid")
                    {
                        addTxUnspent.Txid = reader.Value.ToString();
                    }
                    else if (nextValue == "outputIndex")
                    {
                        string outputIndex = reader.Value.ToString();
                        uint vout;
                        if (uint.TryParse(outputIndex, out vout))
                        {
                            addTxUnspent.Vout = vout;
                        }
                    }
                    else if (nextValue == "script")
                    {
                        addTxUnspent.ScriptPubKey = reader.Value.ToString();
                    }
                    else if (nextValue == "satoshis")
                    {
                        string strSatoshis = reader.Value.ToString();
                        ulong satoshis;
                        if (ulong.TryParse(strSatoshis, out satoshis))
                        {
                            addTxUnspent.Satoshis = satoshis;
                            addTxUnspent.Amount = (decimal)satoshis/COIN;
                        }
                    }
                    else if (nextValue == "height")
                    {
                        string strHeight = reader.Value.ToString();
                        int height;
                        if (int.TryParse(strHeight, out height))
                        {
                            addTxUnspent.Confirmations = blockHeight - height;
                        }
                        listTxUnspent.Add(addTxUnspent);
                    }
                }
            }
            txUnspents = listTxUnspent.ToArray();
            return txUnspents;
        }

        public async Task<TxUnspent[]> GetTxsUnspentAsync(string address, int minConfirmations)
        {
            long nCurrentHeight = await GetLatestBlockHeight();
            TxUnspent[] txsUnspent;
            try
            {
                string jsonResponse = await rpc.GetAddressUTXOsAsync(address, minConfirmations);
                txsUnspent = LoadTxUnspentFromJsonString(jsonResponse, (int)nCurrentHeight);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to get {nameof(TxUnspent)}[] for address='{address}'", ex);
            }

            if (txsUnspent == null)
            {
                return new TxUnspent[] { };
            }

            return txsUnspent.Where(f => f.Confirmations >= minConfirmations).ToArray();
        }

        public async Task<TxBroadcast> BroadcastTxAsync(string transactionHex)
        {
            try
            {
                NBitcoin.Transaction tx = new NBitcoin.Transaction(transactionHex);
                await rpc.SendRawTransactionAsync(tx);
                TxBroadcast txBroadcast = new TxBroadcast();
                txBroadcast.Txid = tx.GetHash().ToString();
                return txBroadcast;
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to get {nameof(TxUnspent)}[] for url='{transactionHex}'", ex);
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
