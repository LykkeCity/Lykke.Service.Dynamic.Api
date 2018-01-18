using System.Threading.Tasks;

namespace Lykke.Service.Dash.Api.Core.Services
{
    public interface IShutdownManager
    {
        Task StopAsync();
    }
}