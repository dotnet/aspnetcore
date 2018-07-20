// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.IIS.FunctionalTests.Utilities;
using Microsoft.AspNetCore.Server.IntegrationTesting;
using Microsoft.AspNetCore.Testing.xunit;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Server.IISIntegration.FunctionalTests
{
    public class ShutdownTests : IISFunctionalTestBase
    {

        public ShutdownTests(ITestOutputHelper output) : base(output)
        {
        }

        [ConditionalFact]
        public async Task ServerShutsDownWhenMainExits()
        {
            var parameters = Helpers.GetBaseDeploymentParameters(publish: true);
            var result = await DeployAsync(parameters);

            var response = await result.RetryingHttpClient.GetAsync("/Shutdown");
            Assert.True(result.HostShutdownToken.WaitHandle.WaitOne(TimeoutExtensions.DefaultTimeout));
        }

        [ConditionalFact]
        public async Task GracefulShutdown_DoesNotCrashProcess()
        {
            var parameters = Helpers.GetBaseDeploymentParameters(publish: true);
            parameters.GracefulShutdown = true;
            var result = await DeployAsync(parameters);

            var response = await result.RetryingHttpClient.GetAsync("/HelloWorld");
            StopServer();
            Assert.True(result.HostProcess.ExitCode == 0);
        }

        [ConditionalFact]
        public async Task ForcefulShutdown_DoesrashProcess()
        {
            var parameters = Helpers.GetBaseDeploymentParameters(publish: true);
            var result = await DeployAsync(parameters);

            var response = await result.RetryingHttpClient.GetAsync("/HelloWorld");
            StopServer();
            Assert.True(result.HostProcess.ExitCode == 1);
        }
    }
}
