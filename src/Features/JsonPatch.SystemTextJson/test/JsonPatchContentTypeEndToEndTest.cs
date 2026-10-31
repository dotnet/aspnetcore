// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Microsoft.AspNetCore.JsonPatch.SystemTextJson;

public class JsonPatchContentTypeEndToEndTest : LoggedTest
{
    [Theory]
    [InlineData("/untyped", "application/json-patch+json", true)]
    [InlineData("/typed", "application/json-patch+json", true)]
    [InlineData("/untyped", "application/json", false)]
    [InlineData("/typed", "application/json", false)]
    [InlineData("/untyped", "text/plain", false)]
    [InlineData("/typed", "text/plain", false)]
    public async Task PatchContentTypes_AreHandledAsExpected(string route, string contentType, bool shouldBeAccepted)
    {
        using var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                    .UseTestServer()
                    .ConfigureServices(services => services.AddRouting().AddSingleton(LoggerFactory))
                    .Configure(app =>
                    {
                        app.UseRouting();
                        app.UseEndpoints(endpoints =>
                        {
                            endpoints.MapPatch("/untyped", (JsonPatchDocument patch) => { });
                            endpoints.MapPatch("/typed", (JsonPatchDocument<SimpleObject> patch) => { });
                        });
                    });
            })
            .Build();

        await host.StartAsync();

        using var client = host.GetTestClient();
        using var content = new StringContent("[]", Encoding.UTF8, contentType);

        using var response = await client.PatchAsync(route, content);

        var expectedStatusCode = shouldBeAccepted ? HttpStatusCode.OK : HttpStatusCode.UnsupportedMediaType;
        Assert.Equal(expectedStatusCode, response.StatusCode);

        await host.StopAsync();
    }
}
