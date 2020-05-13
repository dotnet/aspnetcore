// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.IntegrationTesting;
using Microsoft.AspNetCore.Testing;
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

        public static TestMatrix TestVariants
            => TestMatrix.ForServers(ServerType.IISExpress, ServerType.HttpSys, ServerType.Kestrel)
                .WithTfms(Tfm.NetCoreApp50)
                .WithAllHostingModels();

        [ConditionalTheory]
        [MemberData(nameof(TestVariants))]
        // In theory it could work on these platforms but the client would need non-default credentials.
        [OSSkipCondition(OperatingSystems.Linux | OperatingSystems.MacOSX)]
        public async Task NtlmAuthentication(TestVariant variant)
        {
            var testName = $"NtlmAuthentication_{variant.Server}_{variant.Tfm}_{variant.Architecture}_{variant.ApplicationType}";
            using (StartLog(out var loggerFactory, testName))
            {
                var logger = loggerFactory.CreateLogger("NtlmAuthenticationTest");

                var deploymentParameters = new DeploymentParameters(variant)
                {
                    ApplicationPath = Helpers.GetApplicationPath(),
                    EnvironmentName = "NtlmAuthentication", // Will pick the Start class named 'StartupNtlmAuthentication'
                };

                using (var deployer = IISApplicationDeployerFactory.Create(deploymentParameters, loggerFactory))
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
                        if (variant.Server == ServerType.Kestrel)
                        {
                            Assert.DoesNotContain("NTLM", response.Headers.WwwAuthenticate.ToString());
                        }
                        else
                        {
                            Assert.Contains("NTLM", response.Headers.WwwAuthenticate.ToString());
                        }
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
