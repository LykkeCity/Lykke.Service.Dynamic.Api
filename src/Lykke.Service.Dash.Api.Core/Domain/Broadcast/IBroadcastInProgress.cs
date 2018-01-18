using System;
using System.Collections.Generic;
using System.Text;

namespace Lykke.Service.Dash.Api.Core.Domain.BroadcastInProgress
{
    public interface IBroadcastInProgress
    {
        string Hash { get; }
        Guid OperationId { get; }
    }
}
