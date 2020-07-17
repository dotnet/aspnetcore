// Copyright (c) .NET Foundation. All rights reserved. See License.txt in the project root for license information.

using System.Linq;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xunit;

namespace Microsoft.AspNetCore.Authentication
{
    public class AuthenticationBuilderTests
    {
        [Fact]
        public void OnlyInvokesCanHandleRequestHandlers()
        {
            // Arrange
            var services = new ServiceCollection();
            var builder = new AuthenticationBuilder(services);

            // Act
            builder.AddScheme<TestOptions, TestHandler, AuthenticationBuilderTests>("Microsoft", (options, service) => { });

            // Assert
            var serviceRegistery = Assert.Single(services.Where(x => x.ServiceType == typeof(IConfigureOptions<TestOptions>)));
            Assert.Equal(ServiceLifetime.Singleton, serviceRegistery.Lifetime);
        }

        private class TestOptions : AuthenticationSchemeOptions
        {

        }

        private class TestHandler : AuthenticationHandler<TestOptions>
        {
            public TestHandler(IOptionsMonitor<TestOptions> options, ILoggerFactory logger, UrlEncoder encoder, ISystemClock clock)
                : base(options, logger, encoder, clock)
            {
            }

            protected override Task<AuthenticateResult> HandleAuthenticateAsync()
            {
                return Task.FromResult(AuthenticateResult.NoResult());
            }
        }
    }
}
