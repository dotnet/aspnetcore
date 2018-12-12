// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.IntegrationTesting;
using Microsoft.AspNetCore.Testing.xunit;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace ServerComparison.FunctionalTests
{
    public class NtlmAuthenticationTests : LoggedTest
    {
        public NtlmAuthenticationTests(ITestOutputHelper output) : base(output)
        {
        }

        [ConditionalTheory]
        [OSSkipCondition(OperatingSystems.Linux | OperatingSystems.MacOSX)]
        [InlineData(ServerType.IISExpress, RuntimeFlavor.Clr, "net461", RuntimeArchitecture.x64, ApplicationType.Portable, HostingModel.OutOfProcess, "V2", Skip = "Websdk issue with full framework publish. See https://github.com/aspnet/websdk/pull/322")]
        [InlineData(ServerType.IISExpress, RuntimeFlavor.CoreClr, "netcoreapp2.1", RuntimeArchitecture.x64, ApplicationType.Standalone, HostingModel.OutOfProcess, "/p:ANCMVersion=V2")]
        [InlineData(ServerType.IISExpress, RuntimeFlavor.CoreClr, "netcoreapp2.1", RuntimeArchitecture.x64, ApplicationType.Portable, HostingModel.OutOfProcess, "/p:ANCMVersion=V2")]
        [InlineData(ServerType.IISExpress, RuntimeFlavor.CoreClr, "netcoreapp2.1", RuntimeArchitecture.x64, ApplicationType.Standalone, HostingModel.OutOfProcess, "/p:ANCMVersion=V1")]
        [InlineData(ServerType.IISExpress, RuntimeFlavor.CoreClr, "netcoreapp2.1", RuntimeArchitecture.x64, ApplicationType.Portable, HostingModel.OutOfProcess, "/p:ANCMVersion=V1")]
        [InlineData(ServerType.IISExpress, RuntimeFlavor.CoreClr, "netcoreapp2.0", RuntimeArchitecture.x64, ApplicationType.Portable, HostingModel.OutOfProcess, "/p:ANCMVersion=V2")]
        [InlineData(ServerType.IISExpress, RuntimeFlavor.CoreClr, "netcoreapp2.0", RuntimeArchitecture.x64, ApplicationType.Standalone, HostingModel.OutOfProcess, "/p:ANCMVersion=V2")]
        [InlineData(ServerType.IISExpress, RuntimeFlavor.CoreClr, "netcoreapp2.0", RuntimeArchitecture.x64, ApplicationType.Portable, HostingModel.OutOfProcess, "/p:ANCMVersion=V1")]
        [InlineData(ServerType.IISExpress, RuntimeFlavor.CoreClr, "netcoreapp2.0", RuntimeArchitecture.x64, ApplicationType.Standalone, HostingModel.OutOfProcess, "/p:ANCMVersion=V1")]
        [InlineData(ServerType.WebListener, RuntimeFlavor.CoreClr, "netcoreapp2.0", RuntimeArchitecture.x64, ApplicationType.Portable)]
        [InlineData(ServerType.WebListener, RuntimeFlavor.CoreClr, "netcoreapp2.1", RuntimeArchitecture.x64, ApplicationType.Portable)]
        [InlineData(ServerType.WebListener, RuntimeFlavor.CoreClr, "netcoreapp2.0", RuntimeArchitecture.x64, ApplicationType.Standalone)]
        [InlineData(ServerType.WebListener, RuntimeFlavor.CoreClr, "netcoreapp2.1", RuntimeArchitecture.x64, ApplicationType.Standalone)]
        public async Task NtlmAuthentication(ServerType serverType,
            RuntimeFlavor runtimeFlavor,
            string targetFramework,
            RuntimeArchitecture architecture,
            ApplicationType applicationType,
            HostingModel hostingModel = HostingModel.OutOfProcess,
            string additionalPublishParameters = "")
        {
            var testName = $"NtlmAuthentication_{serverType}_{runtimeFlavor}_{architecture}_{applicationType}";
            using (StartLog(out var loggerFactory, testName))
            {
                var logger = loggerFactory.CreateLogger("NtlmAuthenticationTest");

                var deploymentParameters = new DeploymentParameters(Helpers.GetApplicationPath(), serverType, runtimeFlavor, architecture)
                {
                    EnvironmentName = "NtlmAuthentication", // Will pick the Start class named 'StartupNtlmAuthentication'
                    ServerConfigTemplateContent = Helpers.GetConfigContent(serverType, "NtlmAuthentication.config", nginxConfig: null),
                    SiteName = "NtlmAuthenticationTestSite", // This is configured in the NtlmAuthentication.config
                    TargetFramework = targetFramework,
                    ApplicationType = applicationType,
                    HostingModel = hostingModel,
                    AdditionalPublishParameters = additionalPublishParameters
                };

                using (var deployer = ApplicationDeployerFactory.Create(deploymentParameters, loggerFactory))
                {
                    var deploymentResult = await deployer.DeployAsync();
                    var httpClient = deploymentResult.HttpClient;

                    // Request to base address and check if various parts of the body are rendered & measure the cold startup time.
                    var response = await RetryHelper.RetryRequest(() =>
                    {
                        return httpClient.GetAsync(string.Empty);
                    }, logger, deploymentResult.HostShutdownToken);

                    var responseText = await response.Content.ReadAsStringAsync();
                    try
                    {
                        Assert.Equal("Hello World", responseText);

                        logger.LogInformation("Testing /Anonymous");
                        response = await httpClient.GetAsync("/Anonymous");
                        responseText = await response.Content.ReadAsStringAsync();
                        Assert.Equal("Anonymous?True", responseText);

                        logger.LogInformation("Testing /Restricted");
                        response = await httpClient.GetAsync("/Restricted");
                        responseText = await response.Content.ReadAsStringAsync();
                        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
                        Assert.Contains("NTLM", response.Headers.WwwAuthenticate.ToString());
                        Assert.Contains("Negotiate", response.Headers.WwwAuthenticate.ToString());

                        logger.LogInformation("Testing /Forbidden");
                        response = await httpClient.GetAsync("/Forbidden");
                        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

                        logger.LogInformation("Enabling Default Credentials");

                        // Change the http client to one that uses default credentials
                        httpClient = deploymentResult.CreateHttpClient(new HttpClientHandler() { UseDefaultCredentials = true });

                        logger.LogInformation("Testing /Restricted");
                        response = await httpClient.GetAsync("/Restricted");
                        responseText = await response.Content.ReadAsStringAsync();
                        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                        Assert.Equal("Authenticated", responseText);

                        logger.LogInformation("Testing /Forbidden");
                        response = await httpClient.GetAsync("/Forbidden");
                        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
                    }
                    catch (XunitException)
                    {
                        logger.LogWarning(response.ToString());
                        logger.LogWarning(responseText);
                        throw;
                    }
                }
            }
        }
    }
}