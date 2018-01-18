using System.Collections.Generic;
using System.Threading.Tasks;
using Lykke.Service.Dash.Api.Core.Domain.Balance;

namespace Lykke.Service.Dash.Api.Core.Repositories
{
    public interface IBalancePositiveRepository
    {
        Task SaveAsync(string address, decimal amount);
        Task DeleteAsync(string address);
        Task<(IEnumerable<IBalancePositive> Items, string Continuation)> GetAsync(int take, string continuation);
    }
}
