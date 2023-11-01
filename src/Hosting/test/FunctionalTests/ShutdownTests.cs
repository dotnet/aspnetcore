// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Globalization;
using Microsoft.AspNetCore.Server.IntegrationTesting;
using Microsoft.AspNetCore.InternalTesting;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Hosting.FunctionalTests;

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

    [QuarantinedTest("https://github.com/dotnet/aspnetcore/issues/27371")]
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
                RuntimeArchitectures.Current)
            {
                EnvironmentName = "Shutdown",
                TargetFramework = Tfm.Default,
                ApplicationType = ApplicationType.Portable,
                PublishApplicationBeforeDeployment = true,
                StatusMessagesEnabled = false
            };

            deploymentParameters.EnvironmentVariables["ASPNETCORE_STARTMECHANIC"] = shutdownMechanic;

            using (var deployer = new SelfHostDeployer(deploymentParameters, loggerFactory))
            {
                var startedTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                var completedTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                var output = string.Empty;

                deployer.ProcessOutputListener = (data) =>
                {
                    if (!string.IsNullOrEmpty(data) && data.StartsWith(StartedMessage, StringComparison.Ordinal))
                    {
                        startedTcs.TrySetResult();
                        output += data.Substring(StartedMessage.Length) + '\n';
                    }
                    else
                    {
                        output += data + '\n';
                    }

                    if (output.Contains(CompletionMessage))
                    {
                        completedTcs.TrySetResult();
                    }
                };

                await deployer.DeployAsync();

                try
                {
                    await startedTcs.Task.TimeoutAfter(TimeSpan.FromMinutes(1));
                }
                catch (TimeoutException ex)
                {
                    throw new InvalidOperationException("Timeout while waiting for host process to output started message.", ex);
                }

                SendSIGINT(deployer.HostProcess.Id);

                WaitForExitOrKill(deployer.HostProcess);

                try
                {
                    await completedTcs.Task.TimeoutAfter(TimeSpan.FromMinutes(1));
                }
                catch (TimeoutException ex)
                {
                    throw new InvalidOperationException($"Timeout while waiting for host process to output completion message. The received output is: {output}", ex);
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
            Arguments = processId.ToString(CultureInfo.InvariantCulture),
            RedirectStandardOutput = true,
            UseShellExecute = false
        };

        var process = Process.Start(startInfo);
        WaitForExitOrKill(process);
    }

    private static void WaitForExitOrKill(Process process)
    {
        process.WaitForExit(5 * 1000);
        if (!process.HasExited)
        {
            process.Kill();
        }

        Assert.Equal(0, process.ExitCode);
    }
}
