using System.Threading.Tasks;
using Lykke.Service.Dash.Api.Core.Domain.InsightClient;

namespace Lykke.Service.Dash.Api.Core.Services
{
    public interface IDashInsightClient
    {
        Task<decimal> GetBalance(string address);

        Task<Tx> GetTx(string txid);

        Task<TxUnspent[]> GetTxsUnspentAsync(string address);

        Task<TxBroadcast> BroadcastTxAsync(string transactionHex);
    }
}
