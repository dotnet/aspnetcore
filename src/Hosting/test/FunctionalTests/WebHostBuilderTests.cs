// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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
    public class WebHostBuilderTests : LoggedTest
    {
        public WebHostBuilderTests(ITestOutputHelper output) : base(output) { }

        public static TestMatrix TestVariants => TestMatrix.ForServers(ServerType.Kestrel)
                .WithTfms(Tfm.NetCoreApp50);

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
                    await deployer.DeployAsync();

                    string output = string.Empty;
                    var mre = new ManualResetEventSlim();
                    deployer.HostProcess.OutputDataReceived += (sender, args) =>
                    {
                        if (!string.IsNullOrWhiteSpace(args.Data))
                        {
                            output += args.Data + '\n';
                            mre.Set();
                        }
                    };

                    mre.Wait(50000);

                    output = output.Trim('\n');

                    Assert.Equal($"IStartupInjectionAssemblyName", output);
                }
            }
        }
    }
}
