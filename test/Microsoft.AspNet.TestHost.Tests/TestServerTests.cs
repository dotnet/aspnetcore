using System;
using System.IO;
using System.Runtime.Versioning;
using System.Threading.Tasks;
using Microsoft.AspNet.Abstractions;
using Microsoft.AspNet.DependencyInjection;
using Microsoft.AspNet.DependencyInjection.Fallback;
using Microsoft.Net.Runtime;
using Xunit;

namespace Microsoft.AspNet.TestHost.Tests
{
    public class TestServerTests
    {
        [Fact]
        public void CreateWithDelegate()
        {
            // Arrange
            var services = new ServiceCollection()
                .AddSingleton<IApplicationEnvironment, TestApplicationEnvironment>()
                .BuildServiceProvider();

            // Act & Assert
            Assert.DoesNotThrow(() => TestServer.Create(services, app => { }));
        }

        [Fact]
        public async Task CreateWithGeneric()
        {
            // Arrange
            var services = new ServiceCollection()
                .AddSingleton<IApplicationEnvironment, TestApplicationEnvironment>()
                .BuildServiceProvider();

            var server = TestServer.Create<Startup>(services);
            var client = server.Handler;

            // Act
            var response = await client.GetAsync("http://any");

            // Assert
            Assert.Equal("Startup", new StreamReader(response.Body).ReadToEnd());
        }

        [Fact]
        public void ThrowsIfNoApplicationEnvironmentIsRegisteredWithTheProvider()
        {
            // Arrange
            var services = new ServiceCollection()
                .BuildServiceProvider();

            // Act & Assert
            Assert.Throws<ArgumentException>(
                "serviceProvider",
                () => TestServer.Create<Startup>(services));
        }

        public class Startup
        {
            public void Configuration(IBuilder builder)
            {
                builder.Run(ctx => ctx.Response.WriteAsync("Startup"));
            }
        }

        public class AnotherStartup
        {
            public void Configuration(IBuilder builder)
            {
                builder.Run(ctx => ctx.Response.WriteAsync("Another Startup"));
            }
        }
    }
}
