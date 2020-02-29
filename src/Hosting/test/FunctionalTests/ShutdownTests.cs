// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.IntegrationTesting;
using Microsoft.AspNetCore.Testing;
using Microsoft.Extensions.Logging.Testing;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Hosting.FunctionalTests
{
    public class ShutdownTests : LoggedTest
    {
        private static readonly string StartedMessage = "Started";
        private static readonly string CompletionMessage = "Stopping firing\n" +
                                                            "Stopping end\n" +
                                                            "Stopped firing\n" +
                                                            "Stopped end";

        public ShutdownTests(ITestOutputHelper output) : base(output) { }

        [ConditionalFact]
        [OSSkipCondition(OperatingSystems.Windows)]
        [OSSkipCondition(OperatingSystems.MacOSX)]
        public async Task ShutdownTestRun()
        {
            await ExecuteShutdownTest(nameof(ShutdownTestRun), "Run");
        }

        [ConditionalFact]
        [OSSkipCondition(OperatingSystems.Windows)]
        [OSSkipCondition(OperatingSystems.MacOSX)]
        public async Task ShutdownTestWaitForShutdown()
        {
            await ExecuteShutdownTest(nameof(ShutdownTestWaitForShutdown), "WaitForShutdown");
        }

        private async Task ExecuteShutdownTest(string testName, string shutdownMechanic)
        {
            using (StartLog(out var loggerFactory))
            {
                var logger = loggerFactory.CreateLogger(testName);

// https://github.com/dotnet/aspnetcore/issues/8247
#pragma warning disable 0618
                var applicationPath = Path.Combine(TestPathUtilities.GetSolutionRootDirectory("Hosting"), "test", "testassets",
                    "Microsoft.AspNetCore.Hosting.TestSites");
#pragma warning restore 0618

                var deploymentParameters = new DeploymentParameters(
                    applicationPath,
                    ServerType.Kestrel,
                    RuntimeFlavor.CoreClr,
                    RuntimeArchitecture.x64)
                {
                    EnvironmentName = "Shutdown",
                    TargetFramework = Tfm.NetCoreApp50,
                    ApplicationType = ApplicationType.Portable,
                    PublishApplicationBeforeDeployment = true,
                    StatusMessagesEnabled = false
                };

                deploymentParameters.EnvironmentVariables["ASPNETCORE_STARTMECHANIC"] = shutdownMechanic;

                using (var deployer = new SelfHostDeployer(deploymentParameters, loggerFactory))
                {
                    await deployer.DeployAsync();

                    var started = new ManualResetEventSlim();
                    var completed = new ManualResetEventSlim();
                    var output = string.Empty;
                    deployer.HostProcess.OutputDataReceived += (sender, args) =>
                    {
                        if (!string.IsNullOrEmpty(args.Data) && args.Data.StartsWith(StartedMessage))
                        {
                            started.Set();
                            output += args.Data.Substring(StartedMessage.Length) + '\n';
                        }
                        else
                        {
                            output += args.Data + '\n';
                        }

                        if (output.Contains(CompletionMessage))
                        {
                            completed.Set();
                        }
                    };

                    started.Wait(50000);

                    if (!started.IsSet)
                    {
                        throw new InvalidOperationException("Application did not start successfully");
                    }

                    SendSIGINT(deployer.HostProcess.Id);

                    WaitForExitOrKill(deployer.HostProcess);

                    completed.Wait(50000);

                    if (!started.IsSet)
                    {
                        throw new InvalidOperationException($"Application did not write the expected output. The received output is: {output}");
                    }

                    output = output.Trim('\n');

                    Assert.Equal(CompletionMessage, output);
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
