// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.IntegrationTesting;
using Microsoft.AspNetCore.Testing;
using Microsoft.AspNetCore.Testing.xunit;
using Microsoft.Extensions.Logging.Testing;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Hosting.FunctionalTests
{
    public class WebHostBuilderTests : LoggedTest
    {
        public WebHostBuilderTests(ITestOutputHelper output) : base(output) { }

        [ConditionalFact]
        [OSSkipCondition(OperatingSystems.MacOSX)]
        [OSSkipCondition(OperatingSystems.Linux)]
        public async Task InjectedStartup_DefaultApplicationNameIsEntryAssembly_Clr()
            => await InjectedStartup_DefaultApplicationNameIsEntryAssembly(RuntimeFlavor.Clr);

        private async Task InjectedStartup_DefaultApplicationNameIsEntryAssembly(RuntimeFlavor runtimeFlavor)
        {
            using (StartLog(out var loggerFactory))
            {
                var logger = loggerFactory.CreateLogger(nameof(InjectedStartup_DefaultApplicationNameIsEntryAssembly));

                var applicationPath = Path.Combine(TestPathUtilities.GetSolutionRootDirectory("Hosting"), "test", "testassets", "IStartupInjectionAssemblyName");

                var deploymentParameters = new DeploymentParameters(
                    applicationPath,
                    ServerType.Kestrel,
                    runtimeFlavor,
                    RuntimeArchitecture.x64)
                {
                    TargetFramework = runtimeFlavor == RuntimeFlavor.Clr ? "net461" : "netcoreapp2.1",
                    ApplicationType = ApplicationType.Portable,
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
