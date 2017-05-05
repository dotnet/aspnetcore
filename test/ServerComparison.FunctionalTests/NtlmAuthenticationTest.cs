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
        [OSSkipCondition(OperatingSystems.Linux)]
        [OSSkipCondition(OperatingSystems.MacOSX)]
        [InlineData(ServerType.IISExpress, RuntimeArchitecture.x86, ApplicationType.Portable, Skip = "https://github.com/aspnet/Hosting/issues/601")]
        [InlineData(ServerType.IISExpress, RuntimeArchitecture.x64, ApplicationType.Portable, Skip = "https://github.com/aspnet/IISIntegration/issues/1")]
        [InlineData(ServerType.WebListener, RuntimeArchitecture.x64, ApplicationType.Portable)]
        [InlineData(ServerType.WebListener, RuntimeArchitecture.x64, ApplicationType.Standalone)]
        public async Task NtlmAuthentication(ServerType serverType, RuntimeArchitecture architecture, ApplicationType applicationType)
        {
            var testName = $"NtlmAuthentication_{serverType}_{architecture}_{applicationType}";
            using (StartLog(out var loggerFactory, testName))
            {
                var logger = loggerFactory.CreateLogger("NtlmAuthenticationTest");

                var deploymentParameters = new DeploymentParameters(Helpers.GetApplicationPath(applicationType), serverType, RuntimeFlavor.CoreClr, architecture)
                {
                    EnvironmentName = "NtlmAuthentication", // Will pick the Start class named 'StartupNtlmAuthentication'
                    ServerConfigTemplateContent = Helpers.GetConfigContent(serverType, "NtlmAuthentication.config", nginxConfig: null),
                    SiteName = "NtlmAuthenticationTestSite", // This is configured in the NtlmAuthentication.config
                    TargetFramework = "netcoreapp2.0",
                    ApplicationType = applicationType
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
                        /* https://github.com/aspnet/ServerTests/issues/82
                        logger.LogInformation("Testing /Restricted");
                        response = await httpClient.GetAsync("/Restricted");
                        responseText = await response.Content.ReadAsStringAsync();
                        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
                        Assert.Contains("NTLM", response.Headers.WwwAuthenticate.ToString());
                        Assert.Contains("Negotiate", response.Headers.WwwAuthenticate.ToString());

                        logger.LogInformation("Testing /RestrictedNTLM");
                        response = await httpClient.GetAsync("/RestrictedNTLM");
                        responseText = await response.Content.ReadAsStringAsync();
                        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
                        Assert.Contains("NTLM", response.Headers.WwwAuthenticate.ToString());
                        // Note IIS can't restrict a challenge to a specific auth type, the native auth modules always add themselves.
                        // However WebListener can.
                        if (serverType == ServerType.WebListener)
                        {
                            Assert.DoesNotContain("Negotiate", response.Headers.WwwAuthenticate.ToString());
                        }
                        else if (serverType == ServerType.IISExpress)
                        {
                            Assert.Contains("Negotiate", response.Headers.WwwAuthenticate.ToString());
                        }

                        logger.LogInformation("Testing /Forbidden");
                        response = await httpClient.GetAsync("/Forbidden");
                        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
                        */
                        logger.LogInformation("Enabling Default Credentials");

                        // Change the http client to one that uses default credentials
                        httpClient = deploymentResult.CreateHttpClient(new HttpClientHandler() { UseDefaultCredentials = true });

                        logger.LogInformation("Testing /AutoForbid");
                        response = await httpClient.GetAsync("/AutoForbid");
                        responseText = await response.Content.ReadAsStringAsync();
                        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

                        logger.LogInformation("Testing /Restricted");
                        response = await httpClient.GetAsync("/Restricted");
                        responseText = await response.Content.ReadAsStringAsync();
                        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                        Assert.Equal("Negotiate", responseText);

                        logger.LogInformation("Testing /RestrictedNegotiate");
                        response = await httpClient.GetAsync("/RestrictedNegotiate");
                        responseText = await response.Content.ReadAsStringAsync();
                        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                        Assert.Equal("Negotiate", responseText);

                        logger.LogInformation("Testing /RestrictedNTLM");
                        if (serverType == ServerType.WebListener)
                        {
                            response = await httpClient.GetAsync("/RestrictedNTLM");
                            responseText = await response.Content.ReadAsStringAsync();
                            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                            Assert.Equal("NTLM", responseText);
                        }
                        else if (serverType == ServerType.IISExpress)
                        {
                            response = await httpClient.GetAsync("/RestrictedNTLM");
                            responseText = await response.Content.ReadAsStringAsync();
                            // This isn't a Forbidden because we authenticate with Negotiate and challenge for NTLM.
                            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
                            // Note IIS can't restrict a challenge to a specific auth type, the native auth modules always add themselves,
                            // so both Negotiate and NTLM get sent again.
                        }
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