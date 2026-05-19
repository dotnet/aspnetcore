// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Server.IntegrationTesting;
using Microsoft.AspNetCore.InternalTesting;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Hosting.FunctionalTests;

public class WebHostBuilderTests : LoggedTest
{
    public WebHostBuilderTests(ITestOutputHelper output) : base(output) { }

    public static TestMatrix TestVariants => TestMatrix.ForServers(ServerType.Kestrel)
            .WithTfms(Tfm.Default);

    [QuarantinedTest("https://github.com/dotnet/aspnetcore/issues/52429")]
    [ConditionalTheory]
    [MemberData(nameof(TestVariants))]
    public async Task InjectedStartup_DefaultApplicationNameIsEntryAssembly(TestVariant variant)
    {
        using (StartLog(out var loggerFactory))
        {
            var logger = loggerFactory.CreateLogger(nameof(InjectedStartup_DefaultApplicationNameIsEntryAssembly));

            // https://github.com/dotnet/aspnetcore/issues/8247
#pragma warning disable 0618
            var applicationPath = Path.Combine(TestPathUtilities.GetSolutionRootDirectory("Hosting"), "test", "testassets", "IStartupInjectionAssemblyName");
#pragma warning restore 0618

            var deploymentParameters = new DeploymentParameters(variant)
            {
                ApplicationPath = applicationPath,
                StatusMessagesEnabled = false
            };

            using (var deployer = new SelfHostDeployer(deploymentParameters, loggerFactory))
            {
                var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                var output = string.Empty;

                deployer.ProcessOutputListener = (data) =>
                {
                    if (!string.IsNullOrWhiteSpace(data))
                    {
                        output += data + '\n';
                    }
                };

                var deploymentResult = await deployer.DeployAsync();
                deploymentResult.HostShutdownToken.Register(tcs.SetResult);

                try
                {
                    await tcs.Task.TimeoutAfter(TimeSpan.FromMinutes(1));
                }
                catch (TimeoutException ex)
                {
                    throw new InvalidOperationException("Timeout while waiting for output from host process.", ex);
                }

                output = output.Trim('\n');

                Assert.Equal($"IStartupInjectionAssemblyName", output);
            }
        }
    }
}
