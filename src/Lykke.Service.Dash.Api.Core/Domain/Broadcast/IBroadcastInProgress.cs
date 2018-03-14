using System;

namespace Lykke.Service.Dash.Api.Core.Domain.Broadcast
{
    public interface IBroadcastInProgress
    {
        Guid OperationId { get; }
        string Hash { get; }
    }
}
