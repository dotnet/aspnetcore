// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel.InMemory.FunctionalTests.TestTransport;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.Logging.Testing;
using Xunit;

namespace Microsoft.AspNetCore.Server.Kestrel.InMemory.FunctionalTests;

public class LoggingConnectionMiddlewareTests : LoggedTest
{
    [QuarantinedTest("https://github.com/dotnet/aspnetcore/issues/38086")]
    [Fact]
    public async Task LoggingConnectionMiddlewareCanBeAddedBeforeAndAfterHttps()
    {
        await using (var server = new TestServer(context =>
            {
                context.Response.ContentLength = 12;
                return context.Response.WriteAsync("Hello World!");
            },
            new TestServiceContext(LoggerFactory),
            listenOptions =>
            {
                listenOptions.UseConnectionLogging();
                listenOptions.UseHttps(TestResources.GetTestCertificate());
                listenOptions.UseConnectionLogging();
            }))
        {
            {
                var response = await server.HttpClientSlim.GetStringAsync($"https://localhost:{server.Port}/", validateCertificate: false)
                                                          .DefaultTimeout();
                Assert.Equal("Hello World!", response);
            }
        }
    }
}
