// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Testing;
using Microsoft.Extensions.Logging.Testing;
using Xunit;

namespace Microsoft.AspNetCore.Server.IIS.FunctionalTests;

[SkipIfHostableWebCoreNotAvailable]
[MinimumOSVersion(OperatingSystems.Windows, WindowsVersions.Win8, SkipReason = "https://github.com/aspnet/IISIntegration/issues/866")]
public class TestServerTest : StrictTestServerTests
{
    [ConditionalFact]
    public async Task SingleProcessTestServer_HelloWorld()
    {
        var helloWorld = "Hello World";
        var expectedPath = "/Path";

        string path = null;
        using (var testServer = await TestServer.Create(ctx =>
        {
            path = ctx.Request.Path.ToString();
            return ctx.Response.WriteAsync(helloWorld);
        }, LoggerFactory))
        {
            var result = await testServer.HttpClient.GetAsync(expectedPath);
            Assert.Equal(helloWorld, await result.Content.ReadAsStringAsync());
            Assert.Equal(expectedPath, path);
        }
    }
}
