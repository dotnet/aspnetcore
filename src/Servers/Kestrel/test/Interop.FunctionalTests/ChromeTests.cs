// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Testing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using OpenQA.Selenium.Chrome;
using Xunit;

namespace Interop.FunctionalTests
{
    [SkipIfChromeUnavailable]
    public class ChromeTests : LoggedTest
    {
        private static readonly string _postHtml =
@"<!DOCTYPE html>
<html>
 <head>
   <script type=""text/javascript"">
    function dosubmit() { document.forms[0].submit(); }
   </script>
 </head>
 <body onload=""dosubmit();"">
   <form action=""/"" method=""POST"" accept-charset=""utf-8"">
   </form>
 </body>
</html>";

        private string NetLogPath { get; set; }
        private string StartupLogPath { get; set; }
        private string ShutdownLogPath { get; set; }
        private string[] ChromeArgs { get; set; }

        private void InitializeArgs()
        {
            NetLogPath = Path.Combine(ResolvedLogOutputDirectory, $"{ResolvedTestMethodName}.nl.json");
            StartupLogPath = Path.Combine(ResolvedLogOutputDirectory, $"{ResolvedTestMethodName}.su.json");
            ShutdownLogPath = Path.Combine(ResolvedLogOutputDirectory, $"{ResolvedTestMethodName}.sd.json");

            ChromeArgs = new [] {
                $"--headless",
                $"--no-sandbox",
                $"--disable-gpu",
                $"--allow-insecure-localhost",
                $"--ignore-certificate-errors",
                $"--enable-features=NetworkService",
                $"--enable-logging",
                $"--log-net-log={NetLogPath}",
                $"--trace-startup",
                $"--trace-startup-file={StartupLogPath}",
                $"--trace-shutdown",
                $"--trace-shutdown-file={ShutdownLogPath}"
            };
        }

        [ConditionalTheory(Skip="Disabling while debugging. https://github.com/dotnet/aspnetcore-internal/issues/1363")]
        [OSSkipCondition(OperatingSystems.MacOSX, SkipReason = "Missing SslStream ALPN support: https://github.com/dotnet/corefx/issues/30492")]
        [MinimumOSVersion(OperatingSystems.Windows, WindowsVersions.Win81, SkipReason = "Missing Windows ALPN support: https://en.wikipedia.org/wiki/Application-Layer_Protocol_Negotiation#Support")]
        [InlineData("", "Interop HTTP/2 GET")]
        [InlineData("?TestMethod=POST", "Interop HTTP/2 POST")]
        public async Task Http2(string requestSuffix, string expectedResponse)
        {
            InitializeArgs();

            var hostBuilder = new WebHostBuilder()
                .UseKestrel(options =>
                {
                    options.Listen(IPAddress.Loopback, 0, listenOptions =>
                    {
                        listenOptions.Protocols = HttpProtocols.Http2;
                        listenOptions.UseHttps(TestResources.GetTestCertificate());
                    });
                })
                .ConfigureServices(AddTestLogging)
                .Configure(app => app.Run(async context =>
                {
                    if (HttpMethods.IsPost(context.Request.Query["TestMethod"]))
                    {
                        await context.Response.WriteAsync(_postHtml);
                    }
                    else
                    {
                        await context.Response.WriteAsync($"Interop {context.Request.Protocol} {context.Request.Method}");
                    }
                }));

            using (var host = hostBuilder.Build())
            {
                await host.StartAsync();
                var chromeOutput = RunHeadlessChrome($"https://localhost:{host.GetPort()}/{requestSuffix}");

                AssertExpectedResponseOrShowDebugInstructions(expectedResponse, chromeOutput);

                await host.StopAsync();
            }
        }

        private string RunHeadlessChrome(string testUrl)
        {
            var chromeOptions = new ChromeOptions();
            chromeOptions.AddArguments(ChromeArgs);

            using (var driver = new ChromeDriver(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), chromeOptions))
            {
                driver.Navigate().GoToUrl(testUrl);

                return driver.PageSource;
            }
        }

        private void AssertExpectedResponseOrShowDebugInstructions(string expectedResponse, string actualResponse)
        {
            try
            {
                Assert.Contains(expectedResponse, actualResponse);
            }
            catch
            {
                Logger.LogError("Chrome interop tests failed. Please consult the following logs:");
                Logger.LogError($"Network logs: {NetLogPath}");
                Logger.LogError($"Startup logs: {StartupLogPath}");
                Logger.LogError($"Shutdown logs: {ShutdownLogPath}");
                throw;
            }
        }
    }
}
