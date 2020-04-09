// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.IntegrationTesting;
using Microsoft.AspNetCore.Testing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Xunit;
using Xunit.Abstractions;

namespace E2ETests
{
    [Trait("E2Etests", "DotnetRun")]
    public class DotnetRunTests : LoggedTest
    {
        public static TestMatrix TestVariants
            => TestMatrix.ForServers(ServerType.Kestrel)
                .WithTfms(Tfm.NetCoreApp50);

        [ConditionalTheory]
        [MemberData(nameof(TestVariants))]
        public async Task DotnetRun_Tests(TestVariant variant)
        {
            var testName = $"DotnetRunTests_{variant}";
            using (StartLog(out var loggerFactory, testName))
            {
                var logger = loggerFactory.CreateLogger("DotnetRunTests");
                var musicStoreDbName = DbUtils.GetUniqueName();
                var deploymentParameters = new DeploymentParameters(variant)
                {
                    ApplicationPath = Helpers.GetApplicationPath(),
                    PublishApplicationBeforeDeployment = false,
                    EnvironmentName = "Development",
                    UserAdditionalCleanup = parameters =>
                    {
                        DbUtils.DropDatabase(musicStoreDbName, logger);
                    },
                    EnvironmentVariables =
                    {
                        { MusicStoreConfig.ConnectionStringKey, DbUtils.CreateConnectionString(musicStoreDbName) },
                    },
                };

                using (var deployer = IISApplicationDeployerFactory.Create(deploymentParameters, loggerFactory))
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
