using Common.Log;
using Lykke.Service.Dash.Api.Services;
using Xunit;

namespace Lykke.Service.Dash.Api.Tests
{
    public class DashServiceTests
    {
        private ILog _log;

        private void Init()
        {
            _log = new LogToMemory();
        }
    }
}
