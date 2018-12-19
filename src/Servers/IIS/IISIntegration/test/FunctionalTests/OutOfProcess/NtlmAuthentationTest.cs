// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#if NET461
// Per https://github.com/dotnet/corefx/issues/5045, HttpClientHandler.UseDefaultCredentials does not work correctly in CoreFx.
// We'll require the desktop HttpClient to run these tests.

using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.IntegrationTesting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Microsoft.AspNetCore.Server.IISIntegration.FunctionalTests
{
    public class NtlmAuthenticationTests : LoggedTest
    {
        public NtlmAuthenticationTests(ITestOutputHelper output) : base(output)
        {
        }

        [Fact]
        public Task NtlmAuthentication_Clr_X64_Portable()
        {
            return NtlmAuthentication(RuntimeFlavor.Clr, ApplicationType.Portable, port: 5051, "V1");
        }

        [Fact]
        public Task NtlmAuthentication_CoreClr_X64_Portable()
        {
            return NtlmAuthentication(RuntimeFlavor.CoreClr, ApplicationType.Portable, port: 5052, "V1");
        }

        private async Task NtlmAuthentication(RuntimeFlavor runtimeFlavor, ApplicationType applicationType, int port, string ancmVersion)
        {
            var serverType = ServerType.IISExpress;
            var architecture = RuntimeArchitecture.x64;
            var testName = $"NtlmAuthentication_{runtimeFlavor}";
            using (StartLog(out var loggerFactory, testName))
            {
                var logger = loggerFactory.CreateLogger("NtlmAuthenticationTest");

                var windowsRid = architecture == RuntimeArchitecture.x64
                    ? "win7-x64"
                    : "win7-x86";
                var additionalPublishParameters = $" /p:ANCMVersion={ancmVersion}";
                if (ApplicationType.Standalone == applicationType && RuntimeFlavor.CoreClr == runtimeFlavor)
                {
                    additionalPublishParameters += " -r " + windowsRid;

                }

                var deploymentParameters = new DeploymentParameters(Helpers.GetOutOfProcessTestSitesPath(), serverType, runtimeFlavor, architecture)
                {
                    ApplicationBaseUriHint = $"http://localhost:{port}",
                    EnvironmentName = "NtlmAuthentication", // Will pick the Start class named 'StartupNtlmAuthentication'
                    ServerConfigTemplateContent = (serverType == ServerType.IISExpress) ? File.ReadAllText("AppHostConfig/NtlmAuthentation.config") : null,
                    SiteName = "NtlmAuthenticationTestSite", // This is configured in the NtlmAuthentication.config
                    TargetFramework = runtimeFlavor == RuntimeFlavor.Clr ? "net461" : "netcoreapp2.1",
                    ApplicationType = applicationType,
                    AdditionalPublishParameters = additionalPublishParameters,
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
                    httpClient.Timeout = TimeSpan.FromSeconds(5);

                    // Request to base address and check if various parts of the body are rendered & measure the cold startup time.
                    var response = await RetryHelper.RetryRequest(() =>
                    {
                        return httpClient.GetAsync(string.Empty);
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
