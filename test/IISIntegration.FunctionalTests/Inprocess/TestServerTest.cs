// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using IISIntegration.FunctionalTests.Utilities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Testing.xunit;
using Microsoft.Extensions.Logging.Testing;
using Xunit;
using Xunit.Abstractions;

namespace IISIntegration.FunctionalTests.Inprocess
{
    [SkipIfHostableWebCoreNotAvailible]
    public class TestServerTest: LoggedTest
    {
        public TestServerTest(ITestOutputHelper output = null) : base(output)
        {
        }

        [ConditionalFact]
        [OSSkipCondition(OperatingSystems.Windows, WindowsVersions.Win7, "https://github.com/aspnet/IISIntegration/issues/866")]
        public async Task SingleProcessTestServer_HelloWorld()
        {
            var helloWorld = "Hello World";
            var expectedPath = "/Path";

            string path = null;
            using (var testServer = await TestServer.Create(ctx => {
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
