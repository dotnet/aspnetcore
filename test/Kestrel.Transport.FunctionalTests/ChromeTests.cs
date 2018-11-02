// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#if NETCOREAPP2_2

using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.FunctionalTests;
using Microsoft.AspNetCore.Testing;
using Microsoft.AspNetCore.Testing.xunit;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
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
        private string ChromeArgs { get; set; }

        private void InitializeArgs()
        {
            NetLogPath = Path.Combine(ResolvedLogOutputDirectory, $"{ResolvedTestMethodName}.nl.json");
            StartupLogPath = Path.Combine(ResolvedLogOutputDirectory, $"{ResolvedTestMethodName}.su.json");
            ShutdownLogPath = Path.Combine(ResolvedLogOutputDirectory, $"{ResolvedTestMethodName}.sd.json");

            ChromeArgs = $"--headless " +
                $"--no-sandbox " +
                $"--disable-gpu " +
                $"--allow-insecure-localhost " +
                $"--ignore-certificate-errors --enable-features=NetworkService " +
                $"--enable-logging " +
                $"--dump-dom " +
                $"--virtual-time-budget=10000 " +
                $"--log-net-log={NetLogPath} " +
                $"--trace-startup --trace-startup-file={StartupLogPath} " +
                $"--trace-shutdown --trace-shutdown-file={ShutdownLogPath}";
        }

        [ConditionalTheory]
        [OSSkipCondition(OperatingSystems.MacOSX, SkipReason = "Missing SslStream ALPN support: https://github.com/dotnet/corefx/issues/30492")]
        [MinimumOSVersion(OperatingSystems.Windows, WindowsVersions.Win81, SkipReason = "Missing Windows ALPN support: https://en.wikipedia.org/wiki/Application-Layer_Protocol_Negotiation#Support")]
        [InlineData("", "Interop HTTP/2 GET")]
        [InlineData("?TestMethod=POST", "Interop HTTP/2 POST")]
        public async Task Http2(string requestSuffix, string expectedResponse)
        {
            InitializeArgs();

            using (var server = new TestServer(async context =>
            {
                if (string.Equals(context.Request.Query["TestMethod"], "POST", StringComparison.OrdinalIgnoreCase))
                {
                    await context.Response.WriteAsync(_postHtml);
                }
                else
                {
                    await context.Response.WriteAsync($"Interop {context.Request.Protocol} {context.Request.Method}");
                }
            },
            new TestServiceContext(LoggerFactory),
            options => options.Listen(IPAddress.Loopback, 0, listenOptions =>
            {
                listenOptions.Protocols = HttpProtocols.Http2;
                listenOptions.UseHttps(TestResources.GetTestCertificate());
            })))
            {
                var chromeOutput = await RunHeadlessChrome($"https://localhost:{server.Port}/{requestSuffix}");

                AssertExpectedResponseOrShowDebugInstructions(expectedResponse, chromeOutput);
            }
        }

        private async Task<string> RunHeadlessChrome(string testUrl)
        {
            var chromeArgs = $"{ChromeArgs} {testUrl}";
            var chromeStartInfo = new ProcessStartInfo
            {
                FileName = ChromeConstants.ExecutablePath,
                Arguments = chromeArgs,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardError = true,
                RedirectStandardOutput = true
            };

            Logger.LogInformation($"Staring chrome: {ChromeConstants.ExecutablePath} {chromeArgs}");

            var headlessChromeProcess = Process.Start(chromeStartInfo);
            var chromeOutput = await headlessChromeProcess.StandardOutput.ReadToEndAsync();
            var chromeError = await headlessChromeProcess.StandardError.ReadToEndAsync();
            Logger.LogInformation($"Standard output: {chromeOutput}");
            Logger.LogInformation($"Standard error: {chromeError}");

            headlessChromeProcess.WaitForExit();

            return chromeOutput;
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

#elif NET461 // No ALPN support
#else
#error TFMs need updating
#endif
