using System.Threading.Tasks;
using Lykke.Service.Dash.Api.Core.Domain.InsightClient;

namespace Lykke.Service.Dash.Api.Core.Services
{
    public interface IDashInsightClient
    {
        Task<ulong> GetBalanceSatoshis(string address);

        Task<long> GetLatestBlockHeight();

        Task<Tx> GetTx(string txid);

        Task<Tx[]> GetAddressTxs(string address, int continuation);

        Task<TxUnspent[]> GetTxsUnspentAsync(string address);

        Task<TxBroadcast> BroadcastTxAsync(string transactionHex);
    }
}
