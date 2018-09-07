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
        private const string _testTxIndexOff = "txindex=0";
        private const string _testAddressIndexOn = "addressindex=1";
        private const string _testTimeStampIndexOn = "timestampindex=1";
        private const string _testSpentIndexOn = "spentindex=1";
        private const string _testServerOn = "server=1";
        private const string _testDaemonOn = "daemon=1";
        private DynamicRPCClient rpc;
        private string _userName;
        private string _password;
        private uint _rpcPort;
        private string _network;
        private int nBlockHeight = 0;
        private bool TxIndexOff = false;
        private bool AddressIndexOn = false;
        private bool TimeStampIndexOn = false;
        private bool SpentIndexOn = false;
        private bool ServerOn = false;
        private bool DaemonOn = false;
        /*
         * TODO:
         * make sure tx, address, timestamp and spent indexing are turned on.
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
                            else if (configLine.Length == _testAddressIndexOn.Length && configLine == _testAddressIndexOn)
                            {
                                AddressIndexOn = true;
                            }
                            else if (configLine.Length == _testTimeStampIndexOn.Length && configLine == _testTimeStampIndexOn)
                            {
                                TimeStampIndexOn = true;
                            }
                            else if (configLine.Length == _testSpentIndexOn.Length && configLine == _testSpentIndexOn)
                            {
                                SpentIndexOn = true;
                            }
                            else if (configLine.Length == _testServerOn.Length && configLine == _testServerOn)
                            {
                                ServerOn = true;
                            }
                            else if (configLine.Length == _testDaemonOn.Length && configLine == _testDaemonOn)
                            {
                                DaemonOn = true;
                            }
                            else if (configLine.Length == _testTxIndexOff.Length && configLine == _testTxIndexOff)
                            {
                                TxIndexOff = true;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to get read configuration file.", ex);
            }
            if (TxIndexOff)
            {
                throw new Exception($"Dynamic wallet needs to have txindex on in the config file (txindex=1).");
            }
            if (!AddressIndexOn)
            {
                throw new Exception($"Dynamic wallet needs to have addressindex on in the config file (addressindex=1).");
            }
            if (!TimeStampIndexOn)
            {
                throw new Exception($"Dynamic wallet needs to have timestampindex on in the config file (timestampindex=1).");
            }
            if (!SpentIndexOn)
            {
                throw new Exception($"Dynamic wallet needs to have spendindex on in the config file (spendindex=1).");
            }
            if (!ServerOn)
            {
                throw new Exception($"Dynamic wallet needs to have server on in the config file (server=1).");
            }
            if (!DaemonOn)
            {
                throw new Exception($"Dynamic wallet needs to have daemon on in the config file (daemon=1).");
            }
        }

        private void InitRPC()
        {
            NetworkCredential credential = new NetworkCredential(_userName, _password);
            Network net = Network.GetNetwork(_network);
            RPCCredentialString creds = new RPCCredentialString();
            creds.Server = "http://127.0.0.1:" + Convert.ToUInt32(_rpcPort);
            creds.UserPassword = credential;
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
            Tx tx = new Tx();
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
                TxVin txIn = new TxVin();
                txIn.Addr = input.address;
                txIn.Txid = input.txid;
                txIn.Value = (decimal)input.value; 
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
                txOut.ScriptPubKey.Addresses = addresses.ToArray();
                txOut.Txid = output.scriptPubKey.hex;
                txOut.Value = (decimal)output.value;
                outSatoshis = outSatoshis + output.value;
                listTxVout.Add(txOut);
            }
            tx.Vout = listTxVout.ToArray();
            tx.Fees = (decimal)(inSatoshis - outSatoshis);
            return tx;
        }

        public async Task<Tx[]> GetAddressTxs(string address, int continuation)
        {
            // TODO: implement continuation
            List<Tx> listTxs = new List<Tx>();
            try
            {
                int height = await rpc.GetBlockCountAsync();
                JsonAddressTxIDs jsonAddressTxIDs = await rpc.GetAddressTxIDsAsync(address);
                foreach (string txid in jsonAddressTxIDs.result)
                {
                    JsonTransaction jsonTx = await rpc.GetTransactionAsync(txid);
                    Tx tx = LoadTxFromRPCJson(jsonTx, height);
                    listTxs.Add(tx);
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
                TxBroadcast txBroadcast = new TxBroadcast();
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
