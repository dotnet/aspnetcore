using System;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Microsoft.AspNetCore.Diagnostics.EntityFrameworkCore.Tests
{
#if NET451
    public class DataStoreErrorLoggerTest
    {
        private const string _name = "test";

        private static DataStoreErrorLogger SetUp(Func<string, LogLevel, bool> filter = null, string name = null)
        {
            // Arrange
            var provider = new DataStoreErrorLoggerProvider();
            var logger = (DataStoreErrorLogger)provider.CreateLogger(name ?? _name);

            return logger;
        }

        private static void DomainFunc()
        {
            var logger = SetUp();
            logger.StartLoggingForCurrentCallContext();
            logger.LogInformation(0, "Test");
        }

        [Fact]
        public void ScopeWithChangingAppDomains_DoesNotAccessUnloadedAppDomain()
        {
            // Arrange
            var logger = SetUp();
            var domain = AppDomain.CreateDomain("newDomain");

            // Act
            logger.StartLoggingForCurrentCallContext();
            domain.DoCallBack(DomainFunc);
            AppDomain.Unload(domain);
            logger.LogInformation(0, "Testing");

            // Assert
            Assert.NotNull(logger.LastError);
        }
    }
#endif
}
