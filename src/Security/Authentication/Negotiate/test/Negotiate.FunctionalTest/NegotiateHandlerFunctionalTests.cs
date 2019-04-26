// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Testing.xunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Net.Http.Headers;
using Xunit;

namespace Microsoft.AspNetCore.Authentication.Negotiate
{
    // In theory this would work on Linux and Mac, but the client would require explicit credentials.
    [OSSkipCondition(OperatingSystems.Linux | OperatingSystems.MacOSX)]
    public class NegotiateHandlerFunctionalTests
    {
        [ConditionalFact]
        public async Task Anonymous_NoChallenge_NoOps()
        {
            using var host = await CreateHostAsync();
            using var client = CreateClient(host);
            var result = await client.GetAsync("/expectanonymous");
            Assert.Equal(HttpStatusCode.OK, result.StatusCode);
            Assert.False(result.Headers.Contains(HeaderNames.WWWAuthenticate));
        }

        // TODO: Ensure this is using HTTP2. Needs SSL.
        [ConditionalFact]
        public async Task Anonymous_Http2_NoOps()
        {
            using var host = await CreateHostAsync();
            using var client = CreateClient(host);
            var result = await client.GetAsync("/expectanonymous");
            Assert.Equal(HttpStatusCode.OK, result.StatusCode);
            Assert.False(result.Headers.Contains(HeaderNames.WWWAuthenticate));
        }

        [ConditionalFact]
        public async Task Anonymous_Challenge_401Negotiate()
        {
            using var host = await CreateHostAsync();
            using var client = CreateClient(host);
            var result = await client.GetAsync("/requireauth");
            Assert.Equal(HttpStatusCode.Unauthorized, result.StatusCode);
            Assert.Equal("Negotiate", result.Headers.WwwAuthenticate.ToString());
        }

        [ConditionalFact]
        // TODO: Ensure this is using HTTP2. Needs SSL.
        // TODO: Verify clients will downgrade to HTTP/1? Or do we need to send HTTP_1_1_REQUIRED?
        public async Task Anonymous_ChallengeHttp2_401Negotiate()
        {
            using var host = await CreateHostAsync();
            using var client = CreateClient(host);
            var result = await client.GetAsync("/requireauth");
            Assert.Equal(HttpStatusCode.Unauthorized, result.StatusCode);
            Assert.Equal("Negotiate", result.Headers.WwwAuthenticate.ToString());
        }

        [ConditionalFact]
        public async Task DefautCredentials_Success()
        {
            using var host = await CreateHostAsync();
            using var client = CreateClient(host, useDefaultCredentials: true);
            var result = await client.GetAsync("/requireauth");
            Assert.Equal(HttpStatusCode.OK, result.StatusCode);
            Assert.False(result.Headers.Contains(HeaderNames.WWWAuthenticate)); // TODO: Why is this empty
        }

        [ConditionalFact]
        public async Task AnonymousAfterAuthenticated_Success()
        {
            using var host = await CreateHostAsync();
            using var client = CreateClient(host, useDefaultCredentials: true);
            var result = await client.GetAsync("/requireauth");
            Assert.Equal(HttpStatusCode.OK, result.StatusCode);
            Assert.False(result.Headers.Contains(HeaderNames.WWWAuthenticate)); // TODO: Why is this empty

            result = await client.GetAsync("/requireauth");
            Assert.Equal(HttpStatusCode.OK, result.StatusCode);
            Assert.False(result.Headers.Contains(HeaderNames.WWWAuthenticate));
        }

        [ConditionalFact]
        public async Task Unauthorized_401Negotiate()
        {
            using var host = await CreateHostAsync();
            using var client = CreateClient(host, useDefaultCredentials: true);

            var result = await client.GetAsync("/unauthorized");
            Assert.Equal(HttpStatusCode.Unauthorized, result.StatusCode);
            Assert.Equal("Negotiate", result.Headers.WwwAuthenticate.ToString());
        }

        [ConditionalFact(Skip = "Client bug? It tries to re-authenticate when the connection is already authenticated.")]
        public async Task UnauthorizedAfterAuthenticated_Success()
        {
            using var host = await CreateHostAsync();
            using var client = CreateClient(host, useDefaultCredentials: true);
            var result = await client.GetAsync("/requireauth");
            Assert.Equal(HttpStatusCode.OK, result.StatusCode);
            Assert.False(result.Headers.Contains(HeaderNames.WWWAuthenticate)); // TODO: Why is this empty

            result = await client.GetAsync("/unauthorized");
            Assert.Equal(HttpStatusCode.Unauthorized, result.StatusCode);
            Assert.Equal("Negotiate", result.Headers.WwwAuthenticate.ToString());
        }

        private static Task<IHost> CreateHostAsync(Action<NegotiateOptions> configureOptions = null)
        {
            var builder = new HostBuilder()
                .ConfigureServices(services => services
                    .AddAuthentication(NegotiateDefaults.AuthenticationScheme)
                    .AddNegotiate(configureOptions))
                .ConfigureWebHost(webHostBuilder =>
                {
                    webHostBuilder.UseKestrel(options =>
                    {
                        options.Listen(IPAddress.Loopback, 0);
                    });
                    webHostBuilder.Configure(app =>
                    {
                        app.UseAuthentication();
                        app.Run(TestApp);

                    });                
                });

            return builder.StartAsync();
        }

        private static async Task TestApp(HttpContext context)
        {
            if (context.Request.Path == new PathString("/expectanonymous"))
            {
                Assert.False(context.User.Identity.IsAuthenticated, "Anonymous");
                return;
            }
            else if (context.Request.Path == new PathString("/requireauth"))
            {
                if (!context.User.Identity.IsAuthenticated)
                {
                    await context.ChallengeAsync();
                    return;
                }

                var name = context.User.Identity.Name;
                Assert.False(string.IsNullOrEmpty(name), "name");
                await context.Response.WriteAsync(name);
            }
            else if (context.Request.Path == new PathString("/unauthorized"))
            {
                // Simulate Authorization failure 
                var result = await context.AuthenticateAsync();
                await context.ChallengeAsync();
            }
            else
            {
                throw new NotImplementedException(context.Request.Path);
            }
        }

        private static HttpClient CreateClient(IHost host, bool useDefaultCredentials = false)
        {
            var address = host.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>().Addresses.First();
            return new HttpClient(new HttpClientHandler()
                {
                    UseDefaultCredentials = useDefaultCredentials
                })
            {
                BaseAddress = new Uri(address)
            };
        }
    }
}
