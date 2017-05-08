using System;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;

namespace E2ETests
{
    public class BaseStoreSetupFixture : IDisposable
    {
        private readonly IDisposable _logToken;
        private readonly ILogger<BaseStoreSetupFixture> _logger;
        private readonly Store _store;

        public BaseStoreSetupFixture(bool createInDefaultLocation, string loggerName)
        {
            if (!Store.IsEnabled())
            {
                return;
            }

            var testLog = AssemblyTestLog.ForAssembly(typeof(BaseStoreSetupFixture).Assembly);
            ILoggerFactory loggerFactory;
            _logToken = testLog.StartTestLog(null, loggerName, out loggerFactory, testName: loggerName);
            _logger = loggerFactory.CreateLogger<BaseStoreSetupFixture>();

            CreateStoreInDefaultLocation = createInDefaultLocation;

            _logger.LogInformation(
                "Setting up store in the location: {location}",
                createInDefaultLocation ? "default" : "custom");

            _store = new Store(loggerFactory);

            StoreDirectory = _store.CreateStore(createInDefaultLocation);
        }

        public bool CreateStoreInDefaultLocation { get; }

        public string StoreDirectory { get; }

        public void Dispose()
        {
            if (_store != null)
            {
                _store.Dispose();
            }

            if (_logToken != null)
            {
                _logToken.Dispose();
            }
        }
    }
}