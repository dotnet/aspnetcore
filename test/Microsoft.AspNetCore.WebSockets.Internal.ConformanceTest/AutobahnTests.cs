// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.IntegrationTesting;
using Microsoft.AspNetCore.Testing.xunit;
using Microsoft.AspNetCore.WebSockets.Internal.ConformanceTest.Autobahn;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.PlatformAbstractions;

namespace Microsoft.AspNetCore.WebSockets.Internal.ConformanceTest
{
    public class AutobahnTests
    {
        [ConditionalFact]
        [SkipIfWsTestNotPresent]
        public async Task AutobahnTestSuite()
        {
            var reportDir = Environment.GetEnvironmentVariable("AUTOBAHN_SUITES_REPORT_DIR");
            var outDir = !string.IsNullOrEmpty(reportDir) ?
                reportDir :
                Path.Combine(PlatformServices.Default.Application.ApplicationBasePath, "autobahnreports");

            if (Directory.Exists(outDir))
            {
                Directory.Delete(outDir, recursive: true);
            }

            outDir = outDir.Replace("\\", "\\\\");

            // 9.* is Limits/Performance which is VERY SLOW; 12.*/13.* are compression which we don't implement
            var spec = new AutobahnSpec(outDir)
                .IncludeCase("*")
                .ExcludeCase("9.*", "12.*", "13.*");

            var loggerFactory = new LoggerFactory(); // No logging by default! It's very loud...

            if (string.Equals(Environment.GetEnvironmentVariable("AUTOBAHN_SUITES_LOG"), "1", StringComparison.Ordinal))
            {
                loggerFactory.AddConsole();
            }

            AutobahnResult result;
            using (var tester = new AutobahnTester(loggerFactory, spec))
            {
                await tester.DeployTestAndAddToSpec(ServerType.Kestrel, ssl: false, expectationConfig: expect => expect
                    .NonStrict("6.4.3", "6.4.4"));

                result = await tester.Run();
                tester.Verify(result);
            }
        }

        private bool IsWindows8OrHigher()
        {
            const string WindowsName = "Microsoft Windows ";
            const int VersionOffset = 18;

            if (RuntimeInformation.OSDescription.StartsWith(WindowsName))
            {
                var versionStr = RuntimeInformation.OSDescription.Substring(VersionOffset);
                Version version;
                if (Version.TryParse(versionStr, out version))
                {
                    return version.Major > 6 || (version.Major == 6 && version.Minor >= 2);
                }
            }

            return false;
        }

        private bool IsIISExpress10Installed()
        {
            var pf = Environment.GetEnvironmentVariable("PROGRAMFILES");
            var iisExpressExe = Path.Combine(pf, "IIS Express", "iisexpress.exe");
            return File.Exists(iisExpressExe) && FileVersionInfo.GetVersionInfo(iisExpressExe).FileMajorPart >= 10;
        }
    }
}
