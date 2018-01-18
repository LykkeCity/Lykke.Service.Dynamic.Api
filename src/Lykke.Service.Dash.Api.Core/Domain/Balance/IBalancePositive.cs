namespace Lykke.Service.Dash.Api.Core.Domain.Balance
{
    public interface IBalancePositive
    {
        string Address { get; }
        decimal Amount { get; }
    }
}
