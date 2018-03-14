using System;
using System.Threading.Tasks;
using Lykke.Service.Dash.Api.Core.Domain.Broadcast;

namespace Lykke.Service.Dash.Api.Core.Repositories
{
    public interface IBroadcastRepository
    {
        Task<IBroadcast> GetAsync(Guid operationId);
        Task AddAsync(Guid operationId, string hash, long block);
        Task SaveAsCompletedAsync(Guid operationId, decimal amount, decimal fee, long block);
        Task DeleteAsync(Guid operationId);
    }
}
