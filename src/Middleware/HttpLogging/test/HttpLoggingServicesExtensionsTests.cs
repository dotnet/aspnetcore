using System;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Microsoft.AspNetCore.HttpLogging
{
    public class HttpLoggingServicesExtensionsTests
    {
        [Fact]
        public void AddHttpLogging_NullOptions_Throws()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new ServiceCollection().AddHttpLogging(null));
        }

        [Fact]
        public void AddW3CLogging_NullOptions_Throws()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new ServiceCollection().AddW3CLogging(null));
        }
    }
}
