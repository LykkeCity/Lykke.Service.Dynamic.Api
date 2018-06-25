using System.Threading.Tasks;

namespace Lykke.Service.Dynamic.Api.Core.Services
{
    public interface IShutdownManager
    {
        Task StopAsync();
    }
}