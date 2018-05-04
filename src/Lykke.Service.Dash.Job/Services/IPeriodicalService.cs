using System.Threading.Tasks;

namespace Lykke.Service.Dash.Job.Services
{
    public interface IPeriodicalService
    {
        Task UpdateBalances();
        Task UpdateBroadcasts();
    }
}
