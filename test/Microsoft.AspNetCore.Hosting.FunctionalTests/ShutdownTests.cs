// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Diagnostics;
using System.IO;
using Microsoft.AspNetCore.Server.IntegrationTesting;
using Microsoft.AspNetCore.Server.IntegrationTesting.xunit;
using Microsoft.AspNetCore.Testing.xunit;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Microsoft.AspNetCore.Hosting.FunctionalTests
{
    public class ShutdownTests
    {
        [ConditionalFact]
        [OSSkipCondition(OperatingSystems.Windows)]
        [OSSkipCondition(OperatingSystems.MacOSX)]
        public void ShutdownTest()
        {
            var logger = new LoggerFactory()
                .AddConsole()
                .CreateLogger(nameof(ShutdownTest));

            var applicationPath = Path.Combine(TestProjectHelpers.GetSolutionRoot(), "test",
                "Microsoft.AspNetCore.Hosting.TestSites");

            var deploymentParameters = new DeploymentParameters(
                applicationPath,
                ServerType.Kestrel,
                RuntimeFlavor.CoreClr,
                RuntimeArchitecture.x64)
            {
                EnvironmentName = "Shutdown",
                TargetFramework = "netcoreapp1.1",
                ApplicationType = ApplicationType.Portable,
                PublishApplicationBeforeDeployment = true
            };

            using (var deployer = new SelfHostDeployer(deploymentParameters, logger))
            {
                deployer.Deploy();

                // Wait for application to start
                System.Threading.Thread.Sleep(1000);

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
