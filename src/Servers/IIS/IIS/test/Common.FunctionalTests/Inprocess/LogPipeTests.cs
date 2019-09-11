// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.IIS.FunctionalTests.Utilities;
using Microsoft.AspNetCore.Server.IntegrationTesting;
using Microsoft.AspNetCore.Server.IntegrationTesting.IIS;
using Microsoft.AspNetCore.Testing.xunit;
using Xunit;

namespace Microsoft.AspNetCore.Server.IISIntegration.FunctionalTests
{
    [Collection(PublishedSitesCollection.Name)]
    public class LogPipeTests : IISFunctionalTestBase
    {
        private readonly PublishedSitesFixture _fixture;

        public LogPipeTests(PublishedSitesFixture fixture)
        {
            _fixture = fixture;
        }

        [ConditionalTheory]
        [InlineData("ConsoleErrorWrite")]
        [InlineData("ConsoleWrite")]
        public async Task CheckStdoutLoggingToPipe_DoesNotCrashProcess(string path)
        {
            var deploymentParameters = _fixture.GetBaseDeploymentParameters(publish: true);
            var deploymentResult = await DeployAsync(deploymentParameters);

            await Helpers.AssertStarts(deploymentResult, path);

            StopServer();

            if (deploymentParameters.ServerType == ServerType.IISExpress)
            {
                Assert.Contains(TestSink.Writes, context => context.Message.Contains("TEST MESSAGE"));
            }
        }

        [ConditionalTheory]
        [InlineData("ConsoleErrorWriteStartServer")]
        [InlineData("ConsoleWriteStartServer")]
        public async Task CheckStdoutLoggingToPipeWithFirstWrite(string path)
        {
            var deploymentParameters = _fixture.GetBaseDeploymentParameters(publish: true);

            var firstWriteString = "TEST MESSAGE";

            deploymentParameters.TransformArguments((a, _) => $"{a} {path}");

            var deploymentResult = await DeployAsync(deploymentParameters);

            await Helpers.AssertStarts(deploymentResult);

            StopServer();

            if (deploymentParameters.ServerType == ServerType.IISExpress)
            {
                // We can't read stdout logs from IIS as they aren't redirected.
                Assert.Contains(TestSink.Writes, context => context.Message.Contains(firstWriteString));
            }
        }

        [ConditionalFact]
        public async Task CheckUnicodePipe()
        {
            var path = "CheckConsoleFunctions";
            var deploymentParameters = _fixture.GetBaseDeploymentParameters(_fixture.InProcessTestSite, publish: true);
            deploymentParameters.TransformArguments((a, _) => $"{a} {path}");

            var deploymentResult = await DeployAsync(deploymentParameters);

            var response = await deploymentResult.HttpClient.GetAsync(path);

            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);

            StopServer();
            EventLogHelpers.VerifyEventLogEvent(deploymentResult, EventLogHelpers.InProcessThreadExitStdOut(deploymentResult, "12", "(.*)彡⾔(.*)"));
        }
    }
}
