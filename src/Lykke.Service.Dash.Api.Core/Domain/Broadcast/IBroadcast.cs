using System;

namespace Lykke.Service.Dash.Api.Core.Domain.Broadcast
{
    public interface IBroadcast
    {
        Guid OperationId { get; }
        BroadcastState State { get; }
        string Hash { get; }
        decimal? Amount { get; }
        decimal? Fee { get; }
        string Error { get; }
        DateTime? BroadcastedUtc { get; }
        DateTime? CompletedUtc { get; }
        DateTime? FailedUtc { get; }
    }
}
