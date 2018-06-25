using System;

namespace Lykke.Service.Dynamic.Api.Core.Domain.Build
{
    public interface IBuild
    {
        Guid OperationId { get; }
        string TransactionContext { get; }
    }
}
