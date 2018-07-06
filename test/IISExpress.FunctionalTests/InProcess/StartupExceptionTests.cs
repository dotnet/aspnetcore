// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.IIS.FunctionalTests.Utilities;
using Microsoft.AspNetCore.Testing.xunit;
using Xunit;

namespace Microsoft.AspNetCore.Server.IISIntegration.FunctionalTests
{

    public class StartupExceptionTests : IISFunctionalTestBase
    {
        // TODO FileNotFound here.
        [ConditionalTheory]
        [InlineData("CheckLogFile")]
        [InlineData("CheckErrLogFile")]
        public async Task CheckStdoutWithRandomNumber(string path)
        {
            var deploymentParameters = Helpers.GetBaseDeploymentParameters("StartupExceptionWebsite");
            deploymentParameters.EnvironmentVariables["ASPNETCORE_INPROCESS_STARTUP_VALUE"] = path;
            var randomNumberString = new Random(Guid.NewGuid().GetHashCode()).Next(10000000).ToString();
            deploymentParameters.EnvironmentVariables["ASPNETCORE_INPROCESS_RANDOM_VALUE"] = randomNumberString;

            var deploymentResult = await DeployAsync(deploymentParameters);

            var response = await deploymentResult.RetryingHttpClient.GetAsync(path);

            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);

            StopServer();

            Assert.Contains(TestSink.Writes, context => context.Message.Contains($"Random number: {randomNumberString}"));
        }

        [ConditionalTheory]
        [InlineData("CheckLargeStdErrWrites")]
        [InlineData("CheckLargeStdOutWrites")]
        [InlineData("CheckOversizedStdErrWrites")]
        [InlineData("CheckOversizedStdOutWrites")]
        public async Task CheckStdoutWithLargeWrites(string path)
        {
            // Need a web.config
            // Also publish issues.
            var deploymentParameters = Helpers.GetBaseDeploymentParameters("StartupExceptionWebsite");
            deploymentParameters.EnvironmentVariables["ASPNETCORE_INPROCESS_STARTUP_VALUE"] = path;

            var deploymentResult = await DeployAsync(deploymentParameters);

            var response = await deploymentResult.RetryingHttpClient.GetAsync(path);

            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);

            StopServer();

            Assert.Contains(TestSink.Writes, context => context.Message.Contains(new string('a', 4096)));
        }

        [ConditionalFact]
        public async Task Gets500_30_ErrorPage()
        {
            var deploymentParameters = Helpers.GetBaseDeploymentParameters("StartupExceptionWebsite");

            var deploymentResult = await DeployAsync(deploymentParameters);

            var response = await deploymentResult.HttpClient.GetAsync("/");
            Assert.False(response.IsSuccessStatusCode);
            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);

            var responseText = await response.Content.ReadAsStringAsync();
            Assert.Contains("500.30 - ANCM In-Process Start Failure", responseText);
        }
    }
}
