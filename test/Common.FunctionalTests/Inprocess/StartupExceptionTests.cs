// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.IIS.FunctionalTests.Utilities;
using Microsoft.AspNetCore.Server.IntegrationTesting.IIS;
using Microsoft.AspNetCore.Testing.xunit;
using Xunit;

namespace Microsoft.AspNetCore.Server.IISIntegration.FunctionalTests
{
    [Collection(PublishedSitesCollection.Name)]
    public class StartupExceptionTests : IISFunctionalTestBase
    {
        private readonly PublishedSitesFixture _fixture;

        public StartupExceptionTests(PublishedSitesFixture fixture)
        {
            _fixture = fixture;
        }

        [ConditionalTheory]
        [InlineData("CheckLogFile")]
        [InlineData("CheckErrLogFile")]
        public async Task CheckStdoutWithRandomNumber(string mode)
        {
            var deploymentParameters = _fixture.GetBaseDeploymentParameters(_fixture.StartupExceptionWebsite, publish: true);

            var randomNumberString = new Random(Guid.NewGuid().GetHashCode()).Next(10000000).ToString();
            deploymentParameters.TransformArguments((a, _) => $"{a} {mode} {randomNumberString}");

            await AssertFailsToStart(deploymentParameters);

            Assert.Contains(TestSink.Writes, context => context.Message.Contains($"Random number: {randomNumberString}"));
        }

        [ConditionalTheory]
        [InlineData("CheckLargeStdErrWrites")]
        [InlineData("CheckLargeStdOutWrites")]
        [InlineData("CheckOversizedStdErrWrites")]
        [InlineData("CheckOversizedStdOutWrites")]
        public async Task CheckStdoutWithLargeWrites(string mode)
        {
            var deploymentParameters = _fixture.GetBaseDeploymentParameters(_fixture.StartupExceptionWebsite, publish: true);
            deploymentParameters.TransformArguments((a, _) => $"{a} {mode}");

            await AssertFailsToStart(deploymentParameters);

            Assert.Contains(TestSink.Writes, context => context.Message.Contains(new string('a', 30000)));
        }

        [ConditionalFact]
        public async Task CheckValidConsoleFunctions()
        {
            var deploymentParameters = _fixture.GetBaseDeploymentParameters(_fixture.StartupExceptionWebsite, publish: true);
            deploymentParameters.TransformArguments((a, _) => $"{a} CheckConsoleFunctions");

            await AssertFailsToStart(deploymentParameters);

            Assert.Contains(TestSink.Writes, context => context.Message.Contains("Is Console redirection: True"));
        }

        private async Task AssertFailsToStart(IntegrationTesting.IIS.IISDeploymentParameters deploymentParameters)
        {
            var deploymentResult = await DeployAsync(deploymentParameters);

            var response = await deploymentResult.HttpClient.GetAsync("/");

            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);

            StopServer();
        }

        [ConditionalFact]
        public async Task Gets500_30_ErrorPage()
        {
            var deploymentParameters = _fixture.GetBaseDeploymentParameters(_fixture.StartupExceptionWebsite, publish: true);

            var deploymentResult = await DeployAsync(deploymentParameters);

            var response = await deploymentResult.HttpClient.GetAsync("/");
            Assert.False(response.IsSuccessStatusCode);
            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);

            var responseText = await response.Content.ReadAsStringAsync();
            Assert.Contains("500.30 - ANCM In-Process Start Failure", responseText);
        }

    }
}
