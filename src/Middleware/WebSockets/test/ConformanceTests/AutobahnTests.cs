// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Microsoft.AspNetCore.Server.IntegrationTesting;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.AspNetCore.WebSockets.ConformanceTest.Autobahn;
using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.WebSockets.ConformanceTest;

public class AutobahnTests : LoggedTest
{
    private static readonly TimeSpan TestTimeout = TimeSpan.FromMinutes(3);

    public AutobahnTests(ITestOutputHelper output) : base(output)
    {
    }

    // Skip if wstest is not installed for now, see https://github.com/aspnet/WebSockets/issues/95
    // We will enable Wstest on every build once we've gotten the necessary infrastructure sorted out :).
    [ConditionalFact(Skip = "https://github.com/dotnet/aspnetcore/issues/4350")]
    [SkipIfWsTestNotPresent]
    public async Task AutobahnTestSuite()
    {
        // If we're on CI, we want to actually fail if WsTest isn't installed, rather than just skipping the test
        // The SkipIfWsTestNotPresent attribute ensures that this test isn't skipped on CI, so we just need to check that Wstest is present
        // And we use Assert.True to provide an error message
        Assert.True(Wstest.Default != null, $"The 'wstest' executable (Autobahn WebSockets Test Suite) could not be found at '{Wstest.DefaultLocation}'. Run the Build Agent setup scripts to install it or see https://github.com/crossbario/autobahn-testsuite for instructions on manual installation.");

        using (StartLog(out var loggerFactory))
        {
            var logger = loggerFactory.CreateLogger<AutobahnTests>();
            var reportDir = Environment.GetEnvironmentVariable("AUTOBAHN_SUITES_REPORT_DIR");
            var outDir = !string.IsNullOrEmpty(reportDir) ?
                reportDir :
                Path.Combine(AppContext.BaseDirectory, "autobahnreports");

            if (Directory.Exists(outDir))
            {
                Directory.Delete(outDir, recursive: true);
            }

            outDir = outDir.Replace("\\", "\\\\");

            // 9.* is Limits/Performance which is VERY SLOW; 12.*/13.* are compression which we don't implement
            var spec = new AutobahnSpec(outDir)
                .IncludeCase("*")
                .ExcludeCase("9.*", "12.*", "13.*");

            var cts = new CancellationTokenSource();
            cts.CancelAfter(TestTimeout); // These tests generally complete in just over 1 minute.

            using (cts.Token.Register(() => logger.LogError("Test run is taking longer than maximum duration of {timeoutMinutes:0.00} minutes. Aborting...", TestTimeout.TotalMinutes)))
            {
                AutobahnResult result;
                using (var tester = new AutobahnTester(loggerFactory, spec))
                {
                    await tester.DeployTestAndAddToSpec(ServerType.Kestrel, ssl: false, environment: "ManagedSockets", cancellationToken: cts.Token);
                    await tester.DeployTestAndAddToSpec(ServerType.Kestrel, ssl: true, environment: "ManagedSockets", cancellationToken: cts.Token);

                    // Windows-only WebListener tests
                    if (IsWindows8OrHigher())
                    {
                        // WebListener occasionally gives a non-strict response on 3.2. IIS Express seems to have the same behavior. Wonder if it's related to HttpSys?
                        // For now, just allow the non-strict response, it's not a failure.
                        await tester.DeployTestAndAddToSpec(ServerType.HttpSys, ssl: false, environment: "ManagedSockets", cancellationToken: cts.Token);
                    }

                    result = await tester.Run(cts.Token);
                    tester.Verify(result);
                }
            }

            // If it hasn't been cancelled yet, cancel the token just to be sure
            cts.Cancel();
        }
    }

    private bool IsWindows8OrHigher() => OperatingSystem.IsWindowsVersionAtLeast(6, 2);

    private bool IsIISExpress10Installed()
    {
        var pf = Environment.GetEnvironmentVariable("PROGRAMFILES");
        var iisExpressExe = Path.Combine(pf, "IIS Express", "iisexpress.exe");
        return File.Exists(iisExpressExe) && FileVersionInfo.GetVersionInfo(iisExpressExe).FileMajorPart >= 10;
    }
}
