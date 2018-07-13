// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.IIS.FunctionalTests.Utilities;
using Microsoft.AspNetCore.Testing.xunit;
using Xunit;

namespace Microsoft.AspNetCore.Server.IISIntegration.FunctionalTests
{
    public class StartupExceptionTests : IISFunctionalTestBase
    {
        [ConditionalTheory]
        [InlineData("CheckLogFile")]
        [InlineData("CheckErrLogFile")]
        public async Task CheckStdoutWithRandomNumber(string path)
        {
            // Forcing publish for now to have parity between IIS and IISExpress
            // Reason is because by default for IISExpress, we expect there to not be a web.config file.
            // However, for IIS, we need a web.config file because the default on generated on publish
            // doesn't include V2. We can remove the publish flag once IIS supports non-publish running
            var deploymentParameters = Helpers.GetBaseDeploymentParameters("StartupExceptionWebsite", publish: true);

            var randomNumberString = new Random(Guid.NewGuid().GetHashCode()).Next(10000000).ToString();

            var environmentVariablesInWebConfig = new Dictionary<string, string>
            {
                ["ASPNETCORE_INPROCESS_STARTUP_VALUE"] = path,
                ["ASPNETCORE_INPROCESS_RANDOM_VALUE"] = randomNumberString
            };

            var deploymentResult = await DeployAsync(deploymentParameters);
            Helpers.AddEnvironmentVariablesToWebConfig(deploymentResult.DeploymentResult.ContentRoot, environmentVariablesInWebConfig);

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
            var deploymentParameters = Helpers.GetBaseDeploymentParameters("StartupExceptionWebsite", publish: true);

            var environmentVariablesInWebConfig = new Dictionary<string, string>
            {
                ["ASPNETCORE_INPROCESS_STARTUP_VALUE"] = path
            };

            var deploymentResult = await DeployAsync(deploymentParameters);

            Helpers.AddEnvironmentVariablesToWebConfig(deploymentResult.DeploymentResult.ContentRoot, environmentVariablesInWebConfig);

            var response = await deploymentResult.RetryingHttpClient.GetAsync(path);

            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);

            StopServer();

            Assert.Contains(TestSink.Writes, context => context.Message.Contains(new string('a', 4096)));
        }

        [ConditionalFact]
        public async Task Gets500_30_ErrorPage()
        {
            var deploymentParameters = Helpers.GetBaseDeploymentParameters("StartupExceptionWebsite", publish: true);

            var deploymentResult = await DeployAsync(deploymentParameters);

            var response = await deploymentResult.HttpClient.GetAsync("/");
            Assert.False(response.IsSuccessStatusCode);
            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);

            var responseText = await response.Content.ReadAsStringAsync();
            Assert.Contains("500.30 - ANCM In-Process Start Failure", responseText);
        }
    }
}
