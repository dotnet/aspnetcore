// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.IntegrationTesting;
using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;

namespace ApplicationInsightsJavaScriptSnippetTest
{
    public class JavaScriptSnippetTest : ApplicationInsightsFunctionalTest
    {
        public JavaScriptSnippetTest(ITestOutputHelper output) : base(output)
        {
        }

        [Theory]
        [InlineData(ApplicationType.Portable)]
        [InlineData(ApplicationType.Standalone)]
        public async Task ScriptInjected(ApplicationType applicationType)
        {
            await JavaScriptSnippetInjectionTestSuite(applicationType);
        }

        private async Task JavaScriptSnippetInjectionTestSuite(ApplicationType applicationType)
        {
            var testName = $"ApplicationInsightsJavaScriptSnippetTest_{applicationType}";
            using (StartLog(out var loggerFactory, testName))
            {
                var logger = loggerFactory.CreateLogger(nameof(JavaScriptSnippetTest));
                var deploymentParameters = new DeploymentParameters(GetApplicationPath(), ServerType.Kestrel, RuntimeFlavor.CoreClr, RuntimeArchitecture.x64)
                {
                    PublishApplicationBeforeDeployment = true,
                    PreservePublishedApplicationForDebugging = PreservePublishedApplicationForDebugging,
                    TargetFramework = "netcoreapp2.0",
                    Configuration = GetCurrentBuildConfiguration(),
                    ApplicationType = applicationType,
                    EnvironmentName = "JavaScript",
                    EnvironmentVariables =
                    {
                        new KeyValuePair<string, string>(
                            "ASPNETCORE_HOSTINGSTARTUPASSEMBLIES",
                            "Microsoft.AspNetCore.ApplicationInsights.HostingStartup"),
                    },
                };

                using (var deployer = ApplicationDeployerFactory.Create(deploymentParameters, loggerFactory))
                {
                    var deploymentResult = await deployer.DeployAsync();
                    var httpClientHandler = new HttpClientHandler();
                    var httpClient = deploymentResult.CreateHttpClient(httpClientHandler);

                    // Request to base address and check if various parts of the body are rendered & measure the cold startup time.
                    var response = await RetryHelper.RetryRequest(async () =>
                    {
                        return await httpClient.GetAsync("/Home/ScriptCheck");
                    }, logger: logger, cancellationToken: deploymentResult.HostShutdownToken);

                    Assert.False(response == null, "Response object is null because the client could not " +
                        "connect to the server after multiple retries");

                    var validator = new Validator(httpClient, httpClientHandler, logger, deploymentResult);

                    logger.LogInformation("Verifying layout page");
                    await validator.VerifyLayoutPage(response);

                    logger.LogInformation("Verifying layout page before script");
                    await validator.VerifyLayoutPageBeforeScript(response);

                    logger.LogInformation("Verifying layout page after script");
                    await validator.VerifyLayoutPageAfterScript(response);

                    logger.LogInformation("Variation completed successfully.");
                }
            }
        }
    }
}
