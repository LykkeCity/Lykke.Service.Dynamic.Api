using System.Threading.Tasks;

namespace Lykke.Service.Dynamic.Job.Services
{
    public interface IPeriodicalService
    {
        Task UpdateBalances();
        Task UpdateBroadcasts();
    }
}
