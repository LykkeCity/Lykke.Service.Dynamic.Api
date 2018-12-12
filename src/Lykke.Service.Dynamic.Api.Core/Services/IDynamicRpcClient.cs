using System.Threading.Tasks;
using Lykke.Service.Dynamic.Api.Core.Domain.InsightClient;

namespace Lykke.Service.Dynamic.Api.Core.Services
{
    public interface IDynamicRpcClient
    {
        Task<decimal> GetBalance(string address, int minConfirmations);

        Task<long> GetLatestBlockHeight();

        Task<Tx> GetTx(string txid);

        Task<Tx[]> GetAddressTxs(string address, int continuation);

        Task<TxUnspent[]> GetTxsUnspentAsync(string address, int minConfirmations);

        Task<TxBroadcast> BroadcastTxAsync(string transactionHex);
    }
}
