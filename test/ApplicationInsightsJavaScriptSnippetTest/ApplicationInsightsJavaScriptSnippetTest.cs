// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.IntegrationTesting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Xunit;
using Xunit.Abstractions;

namespace ApplicationInsightsJavaScriptSnippetTest
{
    public class ApplicationInsightsJavaScriptSnippetTest : LoggedTest
    {
        public ApplicationInsightsJavaScriptSnippetTest(ITestOutputHelper output) : base(output)
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
                var logger = loggerFactory.CreateLogger(nameof(ApplicationInsightsJavaScriptSnippetTest));
                var deploymentParameters = new DeploymentParameters(GetApplicationPath(applicationType), ServerType.Kestrel, RuntimeFlavor.CoreClr, RuntimeArchitecture.x64)
                {
                    PublishApplicationBeforeDeployment = true,
                    PreservePublishedApplicationForDebugging = PreservePublishedApplicationForDebugging,
                    TargetFramework = "netcoreapp2.0",
                    Configuration = GetCurrentBuildConfiguration(),
                    ApplicationType = applicationType,
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
                        return await httpClient.GetAsync("\\Home\\ScriptCheck");
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

        private static string GetApplicationPath(ApplicationType applicationType)
        {
            var current = new DirectoryInfo(AppContext.BaseDirectory);
            while (current != null)
            {
                if (File.Exists(Path.Combine(current.FullName, "AzureIntegration.sln")))
                {
                    break;
                }
                current = current.Parent;
            }

            if (current == null)
            {
                throw new InvalidOperationException("Could not find the solution directory");
            }

            return Path.GetFullPath(Path.Combine(current.FullName, "sample", "ApplicationInsightsHostingStartupSample"));
        }

        private static bool PreservePublishedApplicationForDebugging
        {
            get
            {
                var deletePublishedFolder = Environment.GetEnvironmentVariable("ASPNETCORE_DELETEPUBLISHEDFOLDER");

                if (string.Equals("false", deletePublishedFolder, StringComparison.OrdinalIgnoreCase)
                    || string.Equals("0", deletePublishedFolder, StringComparison.OrdinalIgnoreCase))
                {
                    // preserve the published folder and do not delete it
                    return true;
                }

                // do not preserve the published folder and delete it
                return false;
            }
        }

        private static string GetCurrentBuildConfiguration()
        {
            var configuration = "Debug";
            if (string.Equals(Environment.GetEnvironmentVariable("Configuration"), "Release", StringComparison.OrdinalIgnoreCase))
            {
                configuration = "Release";
            }

            return configuration;
        }
    }
}
