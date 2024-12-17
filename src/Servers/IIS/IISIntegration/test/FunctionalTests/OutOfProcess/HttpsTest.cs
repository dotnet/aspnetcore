// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
#if NETCOREAPP2_1

using System;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.IntegrationTesting;
using Microsoft.AspNetCore.Testing.xunit;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Microsoft.AspNetCore.Server.IISIntegration.FunctionalTests
{
    // IIS Express preregisteres 44300-44399 ports with SSL bindings.
    // So these tests always have to use ports in this range, and we can't rely on OS-allocated ports without a whole lot of ceremony around
    // creating self-signed certificates and registering SSL bindings with HTTP.sys
    public class HttpsTest : LoggedTest
    {
        public HttpsTest(ITestOutputHelper output) : base(output)
        {
        }

        [ConditionalFact]
        public Task Https_HelloWorld_CLR_X64()
        {
            return HttpsHelloWorld(RuntimeFlavor.Clr, ApplicationType.Portable, port: 44396, "V1");
        }

        [ConditionalFact]
        public Task Https_HelloWorld_CoreCLR_X64_Portable()
        {
            return HttpsHelloWorld(RuntimeFlavor.CoreClr, ApplicationType.Portable, port: 44394, "V1");
        }

        private async Task HttpsHelloWorld(RuntimeFlavor runtimeFlavor, ApplicationType applicationType, int port, string ancmVersion)
        {
            var serverType = ServerType.IISExpress;
            var architecture = RuntimeArchitecture.x64;

            var applicationBaseUrl = $"https://localhost:{port}/";
            var testName = $"HttpsHelloWorld_{runtimeFlavor}";
            using (StartLog(out var loggerFactory, testName))
            {
                var logger = loggerFactory.CreateLogger("HttpsHelloWorldTest");

                var deploymentParameters = new DeploymentParameters(Helpers.GetOutOfProcessTestSitesPath(), serverType, runtimeFlavor, architecture)
                {
                    ApplicationBaseUriHint = applicationBaseUrl,
                    EnvironmentName = "HttpsHelloWorld", // Will pick the Start class named 'StartupHttpsHelloWorld',
                    ServerConfigTemplateContent = (serverType == ServerType.IISExpress) ? File.ReadAllText("AppHostConfig/Https.config") : null,
                    SiteName = "HttpsTestSite", // This is configured in the Https.config
                    TargetFramework = runtimeFlavor == RuntimeFlavor.Clr ? "net461" : "netcoreapp2.1",
                    ApplicationType = applicationType,
                    Configuration =
#if DEBUG
                        "Debug",
#else
                        "Release",
#endif
                    AdditionalPublishParameters = $" /p:ANCMVersion={ancmVersion}"

                };

                using (var deployer = ApplicationDeployerFactory.Create(deploymentParameters, loggerFactory))
                {
                    var deploymentResult = await deployer.DeployAsync();
                    var handler = new HttpClientHandler();
                    handler.ServerCertificateCustomValidationCallback = (a, b, c, d) => true;
                    var httpClient = deploymentResult.CreateHttpClient(handler);
                    httpClient.Timeout = TimeSpan.FromSeconds(5);

                    // Request to base address and check if various parts of the body are rendered & measure the cold startup time.
                    var response = await RetryHelper.RetryRequest(() =>
                    {
                        return httpClient.GetAsync(string.Empty);
                    }, logger, deploymentResult.HostShutdownToken, retryCount: 30);

                    var responseText = await response.Content.ReadAsStringAsync();
                    try
                    {
                        Assert.Equal("Scheme:https; Original:http", responseText);
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

        [ConditionalFact]
        public Task Https_HelloWorld_NoClientCert_CoreCLR_X64_Portable()
        {
            return HttpsHelloWorldCerts(RuntimeFlavor.CoreClr, ApplicationType.Portable , port: 44397, sendClientCert: false, "V1");
        }

        [ConditionalFact]
        public Task Https_HelloWorld_NoClientCert_Clr_X64()
        {
            return HttpsHelloWorldCerts(RuntimeFlavor.Clr, ApplicationType.Portable, port: 44398, sendClientCert: false, "V1");
        }

#pragma warning disable xUnit1004 // Test methods should not be skipped
        [Fact(Skip = "Manual test only, selecting a client cert is non-determanistic on different machines.")]
#pragma warning restore xUnit1004 // Test methods should not be skipped
        public Task Https_HelloWorld_ClientCert_Clr_X64()
        {
            return HttpsHelloWorldCerts(RuntimeFlavor.Clr, ApplicationType.Portable, port: 44301, sendClientCert: true, "V1");
        }

#pragma warning disable xUnit1004 // Test methods should not be skipped
        [Fact(Skip = "Manual test only, selecting a client cert is non-determanistic on different machines.")]
#pragma warning restore xUnit1004 // Test methods should not be skipped
        public Task Https_HelloWorld_ClientCert_CoreCLR_X64_Portable()
        {
            return HttpsHelloWorldCerts(RuntimeFlavor.CoreClr, ApplicationType.Portable, port: 44302, sendClientCert: true, "V1");
        }

        private async Task HttpsHelloWorldCerts(RuntimeFlavor runtimeFlavor, ApplicationType applicationType, int port, bool sendClientCert, string ancmVersion)
        {
            var serverType = ServerType.IISExpress;
            var architecture = RuntimeArchitecture.x64;
            var applicationBaseUrl = $"https://localhost:{port}/";
            var testName = $"HttpsHelloWorldCerts_{runtimeFlavor}";
            using (StartLog(out var loggerFactory, testName))
            {
                var logger = loggerFactory.CreateLogger("HttpsHelloWorldTest");

                var deploymentParameters = new DeploymentParameters(Helpers.GetOutOfProcessTestSitesPath(), serverType, runtimeFlavor, architecture)
                {
                    ApplicationBaseUriHint = applicationBaseUrl,
                    EnvironmentName = "HttpsHelloWorld", // Will pick the Start class named 'StartupHttpsHelloWorld',
                    ServerConfigTemplateContent = (serverType == ServerType.IISExpress) ? File.ReadAllText("AppHostConfig/Https.config") : null,
                    SiteName = "HttpsTestSite", // This is configured in the Https.config
                    TargetFramework = runtimeFlavor == RuntimeFlavor.Clr ? "net461" : "netcoreapp2.1",
                    ApplicationType = applicationType,
                    Configuration =
#if DEBUG
                        "Debug",
#else
                        "Release",
#endif
                    AdditionalPublishParameters = $" /p:ANCMVersion={ancmVersion}"
                };

                using (var deployer = ApplicationDeployerFactory.Create(deploymentParameters, loggerFactory))
                {
                    var deploymentResult = await deployer.DeployAsync();
                    var handler = new HttpClientHandler();
                    handler.ServerCertificateCustomValidationCallback = (a, b, c, d) => true;
                    handler.ClientCertificateOptions = ClientCertificateOption.Manual;
                    if (sendClientCert)
                    {
                        X509Certificate2 clientCert = FindClientCert();
                        Assert.NotNull(clientCert);
                        handler.ClientCertificates.Add(clientCert);
                    }
                    var httpClient = deploymentResult.CreateHttpClient(handler);

                    // Request to base address and check if various parts of the body are rendered & measure the cold startup time.
                    var response = await RetryHelper.RetryRequest(() =>
                    {
                        return httpClient.GetAsync("checkclientcert");
                    }, logger, deploymentResult.HostShutdownToken);

                    var responseText = await response.Content.ReadAsStringAsync();
                    try
                    {
                        if (sendClientCert)
                        {
                            Assert.Equal("Scheme:https; Original:http; has cert? True", responseText);
                        }
                        else
                        {
                            Assert.Equal("Scheme:https; Original:http; has cert? False", responseText);
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

        private X509Certificate2 FindClientCert()
        {
            var store = new X509Store();
            store.Open(OpenFlags.ReadOnly);

            foreach (var cert in store.Certificates)
            {
                bool isClientAuth = false;
                bool isSmartCard = false;
                foreach (var extension in cert.Extensions)
                {
                    var eku = extension as X509EnhancedKeyUsageExtension;
                    if (eku != null)
                    {
                        foreach (var oid in eku.EnhancedKeyUsages)
                        {
                            if (oid.FriendlyName == "Client Authentication")
                            {
                                isClientAuth = true;
                            }
                            else if (oid.FriendlyName == "Smart Card Logon")
                            {
                                isSmartCard = true;
                                break;
                            }
                        }
                    }
                }

                if (isClientAuth && !isSmartCard)
                {
                    return cert;
                }
            }
            return null;
        }
    }
}
#elif NET462
#else
#error Target frameworks need to be updated
#endif
