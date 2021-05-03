using System;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Microsoft.Extensions.Logging.W3C.Tests
{
    public class W3CLoggerFactoryExtensionsTests
    {
        [Fact]
        public void AddW3CLogger_NullOptions_Throws()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new ServiceCollection()
                    .AddLogging(builder =>
                    {
                        builder.AddW3CLogger(null);
                    }));
        }
    }
}
