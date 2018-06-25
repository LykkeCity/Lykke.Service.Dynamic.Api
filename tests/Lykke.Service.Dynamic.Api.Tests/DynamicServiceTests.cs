using Common.Log;
using Lykke.Service.Dynamic.Api.Services;
using Xunit;

namespace Lykke.Service.Dynamic.Api.Tests
{
    public class DynamicServiceTests
    {
        private ILog _log;

        private void Init()
        {
            _log = new LogToMemory();
        }
    }
}
