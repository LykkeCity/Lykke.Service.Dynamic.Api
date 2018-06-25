using System.Collections.Generic;
using System.Threading.Tasks;
using Lykke.Service.Dynamic.Api.Core.Domain.Balance;

namespace Lykke.Service.Dynamic.Api.Core.Repositories
{
    public interface IBalanceRepository
    {
        Task AddAsync(string address);
        Task DeleteAsync(string address);
        Task<(IEnumerable<IBalance> Entities, string ContinuationToken)> GetAsync(int take, string continuation);
        Task<IBalance> GetAsync(string address);
    }
}
