// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if NET
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Microsoft.AspNetCore.JsonPatch;

public class JsonPatchContentTypeEndToEndTest : LoggedTest
{
    [Theory]
    [InlineData("/untyped", "application/json-patch+json", true)]
    [InlineData("/typed", "application/json-patch+json", true)]
    [InlineData("/untyped", "application/json", true)]
    [InlineData("/typed", "application/json", true)]
    [InlineData("/untyped", "text/plain", false)]
    [InlineData("/typed", "text/plain", false)]
    public async Task PatchContentTypes_AreHandledAsExpected(string route, string contentType, bool shouldBeAccepted)
    {
        using var factory = new JsonPatchWebApplicationFactory(LoggerFactory);
        using var client = factory.CreateClient();

        using var content = new StringContent("""{ "operations": [] }""", Encoding.UTF8, contentType);

        using var response = await client.PatchAsync(route, content);

        var expectedStatusCode = shouldBeAccepted ? HttpStatusCode.OK : HttpStatusCode.UnsupportedMediaType;
        Assert.Equal(expectedStatusCode, response.StatusCode);
    }

    private sealed class JsonPatchWebApplicationFactory(ILoggerFactory loggerFactory) : WebApplicationFactory<JsonPatchContentTypeEndToEndTest>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder) => builder.UseContentRoot(Directory.GetCurrentDirectory());

        protected override IHostBuilder CreateHostBuilder()
        {
            return new HostBuilder().ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                    .UseTestServer()
                    .ConfigureServices(services => services.AddRouting().AddSingleton(loggerFactory))
                    .Configure(app =>
                    {
                        app.UseRouting();
                        app.UseEndpoints(endpoints =>
                        {
                            endpoints.MapPatch("/untyped", (JsonPatchDocument patch) => { });
                            endpoints.MapPatch("/typed", (JsonPatchDocument<SimpleObject> patch) => { });
                        });
                    });
            });
        }
    }
}
#endif
