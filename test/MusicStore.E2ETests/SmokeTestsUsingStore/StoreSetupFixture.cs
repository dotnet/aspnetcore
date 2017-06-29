using System;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;

namespace E2ETests
{
    public class StoreSetupFixture : IDisposable
    {
        private readonly IDisposable _logToken;
        private readonly ILogger<StoreSetupFixture> _logger;
        private readonly Store _store;

        public StoreSetupFixture()
        {
            if (!Store.IsEnabled())
            {
                return;
            }

            var loggerName = nameof(StoreSetupFixture);
            var testLog = AssemblyTestLog.ForAssembly(typeof(StoreSetupFixture).Assembly);
            ILoggerFactory loggerFactory;
            _logToken = testLog.StartTestLog(null, loggerName, out loggerFactory, testName: loggerName);
            _logger = loggerFactory.CreateLogger<StoreSetupFixture>();
            
            _store = new Store(loggerFactory);

            StoreDirectory = _store.CreateStore();

            _logger.LogInformation($"Store was setup at {StoreDirectory}");
        }

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