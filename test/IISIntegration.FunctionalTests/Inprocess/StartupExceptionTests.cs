// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using IISIntegration.FunctionalTests.Utilities;
using Xunit;
using System.Net;

namespace Microsoft.AspNetCore.Server.IISIntegration.FunctionalTests
{
    public class StartupExceptionTests : IISFunctionalTestBase
    {

        [Theory]
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

            Assert.Contains(TestSink.Writes, context => context.Message.Contains($"Random number: {randomNumberString}"));
        }

        [Theory]
        [InlineData("CheckLargeStdErrWrites")]
        [InlineData("CheckLargeStdOutWrites")]
        [InlineData("CheckOversizedStdErrWrites")]
        [InlineData("CheckOversizedStdOutWrites")]
        public async Task CheckStdoutWithLargeWrites(string path)
        {
            var deploymentParameters = Helpers.GetBaseDeploymentParameters("StartupExceptionWebsite");
            deploymentParameters.EnvironmentVariables["ASPNETCORE_INPROCESS_STARTUP_VALUE"] = path;
            var deploymentResult = await DeployAsync(deploymentParameters);

            var response = await deploymentResult.RetryingHttpClient.GetAsync(path);

            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);

            Assert.Contains(TestSink.Writes, context => context.Message.Contains(new string('a', 4096)));
        }
    }
}
