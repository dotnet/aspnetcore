// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.IntegrationTesting;
using Microsoft.AspNetCore.Testing;
using Microsoft.Extensions.Logging.Testing;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Server.HttpSys.FunctionalTests
{
    public class DelegateOutOfProcTests : LoggedTest
    {
        private static readonly string StartedMessage = "Started";
        private static readonly string CompletionMessage = "Stopping firing\n" +
                                                            "Stopping end\n" +
                                                            "Stopped firing\n" +
                                                            "Stopped end";

        public DelegateOutOfProcTests(ITestOutputHelper output) : base(output) { }

        [ConditionalFact]
        [DelegateSupportedCondition(true)]
        public async Task CanDelegateOutOfProcess()
        {
            using (StartLog(out var loggerFactory))
            {
                var logger = loggerFactory.CreateLogger("CanDelegateOutOfProcess");

                // https://github.com/dotnet/aspnetcore/issues/8247
#pragma warning disable 0618
                var applicationPath = Path.Combine(TestPathUtilities.GetSolutionRootDirectory("HttpSys"), "test", "testassets",
                    "DelegationSite");
#pragma warning restore 0618

                var deploymentParameters = new DeploymentParameters(
                    applicationPath,
                    ServerType.HttpSys,
                    RuntimeFlavor.CoreClr,
                    RuntimeArchitecture.x64)
                {
                    EnvironmentName = "Testing",
                    TargetFramework = Tfm.Default,
                    ApplicationType = ApplicationType.Portable,
                    PublishApplicationBeforeDeployment = true,
                    StatusMessagesEnabled = false
                };

                using var deployer = new SelfHostDeployer(deploymentParameters, loggerFactory);
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

                var result = await deployer.DeployAsync();

                try
                {
                    await startedTcs.Task.TimeoutAfter(TimeSpan.FromMinutes(1));
                }
                catch (TimeoutException ex)
                {
                    throw new InvalidOperationException("Timeout while waiting for host process to output started message.", ex);
                }

                output = output.Trim('\n');

                Assert.Equal(CompletionMessage, output);
            }
        }
    }
}
