// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.IntegrationTesting;
using Microsoft.AspNetCore.Testing;
using Microsoft.AspNetCore.Testing.xunit;
using Microsoft.Extensions.Logging.Testing;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Hosting.FunctionalTests
{
    public class ShutdownTests : LoggedTest
    {
        public ShutdownTests(ITestOutputHelper output) : base(output)
        {
        }

        [ConditionalFact]
        [OSSkipCondition(OperatingSystems.Windows)]
        [OSSkipCondition(OperatingSystems.MacOSX)]
        public async Task ShutdownTestRun()
        {
            using (StartLog(out var loggerFactory))
            {
                var logger = loggerFactory.CreateLogger(nameof(ShutdownTestRun));

                var applicationPath = Path.Combine(TestPathUtilities.GetSolutionRootDirectory("Hosting"), "test",
                    "Microsoft.AspNetCore.Hosting.TestSites");

                var deploymentParameters = new DeploymentParameters(
                    applicationPath,
                    ServerType.Kestrel,
                    RuntimeFlavor.CoreClr,
                    RuntimeArchitecture.x64)
                {
                    EnvironmentName = "Shutdown",
                    TargetFramework = "netcoreapp2.0",
                    ApplicationType = ApplicationType.Portable,
                    PublishApplicationBeforeDeployment = true
                };

                deploymentParameters.EnvironmentVariables["ASPNETCORE_STARTMECHANIC"] = "Run";

                using (var deployer = new SelfHostDeployer(deploymentParameters, loggerFactory))
                {
                    await deployer.DeployAsync();

                    string output = string.Empty;
                    deployer.HostProcess.OutputDataReceived += (sender, args) => output += args.Data + '\n';

                    SendSIGINT(deployer.HostProcess.Id);

                    WaitForExitOrKill(deployer.HostProcess);

                    output = output.Trim('\n');

                    Assert.Equal(output, "Application is shutting down...\n" +
                                         "Stopping firing\n" +
                                         "Stopping end\n" +
                                         "Stopped firing\n" +
                                         "Stopped end");
                }
            }
        }

        [ConditionalFact]
        [OSSkipCondition(OperatingSystems.Windows)]
        [OSSkipCondition(OperatingSystems.MacOSX)]
        public async Task ShutdownTestWaitForShutdown()
        {
            using (StartLog(out var loggerFactory))
            {
                var logger = loggerFactory.CreateLogger(nameof(ShutdownTestWaitForShutdown));

                var applicationPath = Path.Combine(TestPathUtilities.GetSolutionRootDirectory("Hosting"), "test",
                    "Microsoft.AspNetCore.Hosting.TestSites");

                var deploymentParameters = new DeploymentParameters(
                    applicationPath,
                    ServerType.Kestrel,
                    RuntimeFlavor.CoreClr,
                    RuntimeArchitecture.x64)
                {
                    EnvironmentName = "Shutdown",
                    TargetFramework = "netcoreapp2.0",
                    ApplicationType = ApplicationType.Portable,
                    PublishApplicationBeforeDeployment = true
                };

                deploymentParameters.EnvironmentVariables["ASPNETCORE_STARTMECHANIC"] = "WaitForShutdown";

                using (var deployer = new SelfHostDeployer(deploymentParameters, loggerFactory))
                {
                    await deployer.DeployAsync();

                    string output = string.Empty;
                    deployer.HostProcess.OutputDataReceived += (sender, args) => output += args.Data + '\n';

                    SendSIGINT(deployer.HostProcess.Id);

                    WaitForExitOrKill(deployer.HostProcess);

                    output = output.Trim('\n');

                    Assert.Equal(output, "Stopping firing\n" +
                                         "Stopping end\n" +
                                         "Stopped firing\n" +
                                         "Stopped end");
                }
            }
        }


        private static void SendSIGINT(int processId)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "kill",
                Arguments = processId.ToString(),
                RedirectStandardOutput = true,
                UseShellExecute = false
            };

            var process = Process.Start(startInfo);
            WaitForExitOrKill(process);
        }

        private static void WaitForExitOrKill(Process process)
        {
            process.WaitForExit(1000);
            if (!process.HasExited)
            {
                process.Kill();
            }

            Assert.Equal(0, process.ExitCode);
        }
    }
}
