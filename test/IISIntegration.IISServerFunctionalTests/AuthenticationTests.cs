// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#if NET461

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.IIS.FunctionalTests;
using Microsoft.AspNetCore.Server.IntegrationTesting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace IISIntegration.IISServerFunctionalTests
{
    public class AuthenticationTests : LoggedTest
    {
        public AuthenticationTests(ITestOutputHelper output) : base(output)
        {
        }

        [Fact(Skip = "See https://github.com/aspnet/IISIntegration/issues/424")]
        public Task Authentication_InProcess_IISExpress()
        {
            return Authentication();
        }

        private async Task Authentication()
        {
            var serverType = ServerType.IISExpress;
            var architecture = RuntimeArchitecture.x64;
            var testName = $"Authentication_{RuntimeFlavor.CoreClr}";
            using (StartLog(out var loggerFactory, testName))
            {
                var logger = loggerFactory.CreateLogger("AuthenticationTest");

                var deploymentParameters = new DeploymentParameters(Helpers.GetTestSitesPath(), serverType, RuntimeFlavor.CoreClr, architecture)
                {
                    ApplicationBaseUriHint = $"http://localhost:5051",
                    EnvironmentName = "Authentication", // Will pick the Start class named 'StartupAuthentication',
                    ServerConfigTemplateContent = (serverType == ServerType.IISExpress) ? File.ReadAllText("Http.config") : null,
                    SiteName = "HttpTestSite", // This is configured in the Http.config
                    TargetFramework = "netcoreapp2.0",
                    ApplicationType = ApplicationType.Portable,
                    Configuration =
#if DEBUG
                        "Debug"
#else
                        "Release"
#endif
                };

                using (var deployer = ApplicationDeployerFactory.Create(deploymentParameters, loggerFactory))
                {
                    var deploymentResult = await deployer.DeployAsync();
                    var httpClient = deploymentResult.HttpClient;
                    deploymentResult.HttpClient.Timeout = TimeSpan.FromSeconds(5);

                    // Request to base address and check if various parts of the body are rendered & measure the cold startup time.
                    var response = await RetryHelper.RetryRequest(() =>
                    {
                        return deploymentResult.HttpClient.GetAsync(string.Empty);
                    }, logger, deploymentResult.HostShutdownToken, retryCount: 30);

                    var responseText = await response.Content.ReadAsStringAsync();
                    try
                    {
                        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                        Assert.Equal("Hello World", responseText);

                        response = await httpClient.GetAsync("/Anonymous");
                        responseText = await response.Content.ReadAsStringAsync();
                        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                        Assert.Equal("Anonymous?True", responseText);

                        response = await httpClient.GetAsync("/Restricted");
                        responseText = await response.Content.ReadAsStringAsync();
                        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
                        Assert.Contains("NTLM", response.Headers.WwwAuthenticate.ToString());
                        Assert.Contains("Negotiate", response.Headers.WwwAuthenticate.ToString());

                        response = await httpClient.GetAsync("/RestrictedNTLM");
                        responseText = await response.Content.ReadAsStringAsync();
                        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
                        Assert.Contains("NTLM", response.Headers.WwwAuthenticate.ToString());
                        // Note we can't restrict a challenge to a specific auth type, the native auth modules always add themselves.
                        Assert.Contains("Negotiate", response.Headers.WwwAuthenticate.ToString());

                        response = await httpClient.GetAsync("/Forbidden");
                        responseText = await response.Content.ReadAsStringAsync();
                        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

                        var httpClientHandler = new HttpClientHandler() { UseDefaultCredentials = true };
                        httpClient = deploymentResult.CreateHttpClient(httpClientHandler);

                        response = await httpClient.GetAsync("/Anonymous");
                        responseText = await response.Content.ReadAsStringAsync();
                        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                        Assert.Equal("Anonymous?True", responseText);

                        response = await httpClient.GetAsync("/Restricted");
                        responseText = await response.Content.ReadAsStringAsync();
                        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                        Assert.NotEmpty(responseText);
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
#elif NETCOREAPP2_0 || NETCOREAPP2_1
#else
#error Target frameworks need to be updated
#endif
