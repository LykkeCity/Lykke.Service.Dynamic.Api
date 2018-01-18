using System.Collections.Generic;
using System.Threading.Tasks;
using Lykke.Service.Dash.Api.Core.Domain.Balance;

namespace Lykke.Service.Dash.Api.Core.Repositories
{
    public interface IBalanceRepository
    {
        Task AddAsync(string address);
        Task DeleteAsync(string address);
        Task<IEnumerable<IBalance>> GetAllAsync();
        Task<IBalance> GetAsync(string address);
    }
}
