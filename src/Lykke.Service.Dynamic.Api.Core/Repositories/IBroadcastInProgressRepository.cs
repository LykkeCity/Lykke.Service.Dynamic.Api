using Lykke.Service.Dynamic.Api.Core.Domain.Broadcast;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Lykke.Service.Dynamic.Api.Core.Repositories
{
    public interface IBroadcastInProgressRepository
    {
        Task AddAsync(Guid operationId, string hash);
        Task DeleteAsync(Guid operationId);
        Task<IEnumerable<IBroadcastInProgress>> GetAllAsync();
    }
}
