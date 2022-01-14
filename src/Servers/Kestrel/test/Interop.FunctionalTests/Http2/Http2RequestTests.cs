// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using System.Net.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Testing;
using Microsoft.Extensions.Hosting;

namespace Interop.FunctionalTests.Http2;

public class Http2RequestTests : LoggedTest
{
    [Fact]
    public async Task GET_NoTLS_Http11RequestToHttp2Endpoint_400Result()
    {
        // Arrange
        var builder = CreateHostBuilder(c => Task.CompletedTask, protocol: HttpProtocols.Http2, plaintext: true);

        using (var host = builder.Build())
        using (var client = HttpHelpers.CreateClient())
        {
            await host.StartAsync().DefaultTimeout();

            var request = new HttpRequestMessage(HttpMethod.Get, $"http://127.0.0.1:{host.GetPort()}/");
            request.Version = HttpVersion.Version11;
            request.VersionPolicy = HttpVersionPolicy.RequestVersionExact;

            // Act
            var responseMessage = await client.SendAsync(request, CancellationToken.None).DefaultTimeout();

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, responseMessage.StatusCode);
            Assert.Equal("An HTTP/1.x request was sent to an HTTP/2 only endpoint.", await responseMessage.Content.ReadAsStringAsync());
        }
    }

    private IHostBuilder CreateHostBuilder(RequestDelegate requestDelegate, HttpProtocols? protocol = null, Action<KestrelServerOptions> configureKestrel = null, bool? plaintext = null)
    {
        return HttpHelpers.CreateHostBuilder(AddTestLogging, requestDelegate, protocol, configureKestrel, plaintext);
    }
}
