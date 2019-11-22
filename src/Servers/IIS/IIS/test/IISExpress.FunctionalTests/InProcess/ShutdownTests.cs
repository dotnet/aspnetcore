// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.IIS.FunctionalTests.Utilities;
using Microsoft.AspNetCore.Server.IntegrationTesting;
using Microsoft.AspNetCore.Testing.xunit;
using Xunit;

namespace Microsoft.AspNetCore.Server.IISIntegration.FunctionalTests
{
    [Collection(PublishedSitesCollection.Name)]
    public class ShutdownTests : IISFunctionalTestBase
    {
        private readonly PublishedSitesFixture _fixture;

        public ShutdownTests(PublishedSitesFixture fixture)
        {
            _fixture = fixture;
        }

        [ConditionalFact]
        public async Task ServerShutsDownWhenMainExits()
        {
            var parameters = _fixture.GetBaseDeploymentParameters(publish: true);
            var deploymentResult = await DeployAsync(parameters);
            try
            {
                await deploymentResult.HttpClient.GetAsync("/Shutdown");
            }
            catch (HttpRequestException ex) when (ex.InnerException is IOException)
            {
                // Server might close a connection before request completes
            }

            deploymentResult.AssertWorkerProcessStop();
        }


        [ConditionalFact]
        public async Task ServerShutsDownWhenMainExitsStress()
        {
            var parameters = _fixture.GetBaseDeploymentParameters(publish: true);
            var deploymentResult = await StartAsync(parameters);

            var load = Helpers.StressLoad(deploymentResult.HttpClient, "/HelloWorld", response => {
                var statusCode = (int)response.StatusCode;
                Assert.True(statusCode == 200 || statusCode == 503, "Status code was " + statusCode);
            });

            try
            {
                await deploymentResult.HttpClient.GetAsync("/Shutdown");
                await load;
            }
            catch (HttpRequestException ex) when (ex.InnerException is IOException | ex.InnerException is SocketException)
            {
                // Server might close a connection before request completes
            }

            deploymentResult.AssertWorkerProcessStop();
        }

        [ConditionalFact]
        public async Task GracefulShutdown_DoesNotCrashProcess()
        {
            var parameters = _fixture.GetBaseDeploymentParameters(publish: true);
            var result = await DeployAsync(parameters);

            var response = await result.HttpClient.GetAsync("/HelloWorld");
            StopServer(gracefulShutdown: true);
            Assert.True(result.HostProcess.ExitCode == 0);
        }

        [ConditionalFact]
        public async Task ForcefulShutdown_DoesCrashProcess()
        {
            var parameters = _fixture.GetBaseDeploymentParameters(publish: true);
            var result = await DeployAsync(parameters);

            var response = await result.HttpClient.GetAsync("/HelloWorld");
            StopServer(gracefulShutdown: false);
            Assert.True(result.HostProcess.ExitCode == 1);
        }
    }
}
