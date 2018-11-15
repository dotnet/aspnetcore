// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.IntegrationTesting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Xunit;
using Xunit.Abstractions;

namespace E2ETests
{
    public class DotnetRunTestRunner : LoggedTest
    {
        public DotnetRunTestRunner(ITestOutputHelper output)
            : base(output)
        {
        }

        public async Task RunTests(
            ServerType serverType,
            RuntimeFlavor runtimeFlavor,
            ApplicationType applicationType,
            RuntimeArchitecture runtimeArchitecture)
        {
            var testName = $"DotnetRunTests_{serverType}_{runtimeFlavor}_{applicationType}";
            using (StartLog(out var loggerFactory, testName))
            {
                var logger = loggerFactory.CreateLogger("DotnetRunTests");
                var musicStoreDbName = DbUtils.GetUniqueName();
                var applicationPath = Helpers.GetApplicationPath();
                var deploymentParameters = new DeploymentParameters(
                    applicationPath, serverType, runtimeFlavor, runtimeArchitecture)
                {
                    PublishApplicationBeforeDeployment = false,
                    TargetFramework = Helpers.GetTargetFramework(runtimeFlavor),
                    Configuration = Helpers.GetCurrentBuildConfiguration(),
                    EnvironmentName = "Development",
                    ApplicationType = applicationType,
                    UserAdditionalCleanup = parameters =>
                    {
                        DbUtils.DropDatabase(musicStoreDbName, logger);
                    },
                    EnvironmentVariables =
                    {
                        { MusicStoreConfig.ConnectionStringKey, DbUtils.CreateConnectionString(musicStoreDbName) },
                    },
                };

                using (var deployer = ApplicationDeployerFactory.Create(deploymentParameters, loggerFactory))
                {
                    var deploymentResult = await deployer.DeployAsync();
                    var httpClientHandler = new HttpClientHandler { UseDefaultCredentials = true };
                    var httpClient = deploymentResult.CreateHttpClient(httpClientHandler);

                    var response = await RetryHelper.RetryRequest(
                        () => httpClient.GetAsync(string.Empty), logger, deploymentResult.HostShutdownToken);

                    Assert.False(response == null, "Response object is null because the client could not " +
                        "connect to the server after multiple retries");

                    var validator = new Validator(httpClient, httpClientHandler, logger, deploymentResult);

                    logger.LogInformation("Verifying home page");
                    // Verify HomePage should validate that we're using precompiled views.
                    await validator.VerifyHomePage(response);

                    // Verify developer exception page
                    logger.LogInformation("Verifying developer exception page");
                    response = await RetryHelper.RetryRequest(
                        () => httpClient.GetAsync("PageThatThrows"), logger, cancellationToken: deploymentResult.HostShutdownToken);
                    await validator.VerifyDeveloperExceptionPage(response);

                    logger.LogInformation("Variation completed successfully.");
                }
            }
        }
    }
}
