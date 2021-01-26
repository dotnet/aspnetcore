// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Testing;
using Microsoft.Extensions.Logging.Testing;
using Xunit;

namespace Microsoft.AspNetCore.Server.IIS.FunctionalTests
{
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
}
