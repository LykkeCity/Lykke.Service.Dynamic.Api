namespace Lykke.Service.Dash.Api.Core.Domain.InsightClient
{
    public class TxUnspent
    {
        public string Txid { get; set; }
        public uint Vout { get; set; }
        public string ScriptPubKey { get; set; }
        public decimal Amount { get; set; }
        public ulong Satoshis { get; set; }
        public int Confirmations { get; set; }
    }
}
