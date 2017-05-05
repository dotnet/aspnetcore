// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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

        [ConditionalTheory(Skip = "No test configuration enabled")]
        [OSSkipCondition(OperatingSystems.Linux | OperatingSystems.MacOSX)]
        //[InlineData(RuntimeArchitecture.x86, "https://localhost:44394/", ApplicationType.Portable)]
        [InlineData(RuntimeArchitecture.x64, "https://localhost:44395/", ApplicationType.Standalone)]
        public Task Https_HelloWorld(RuntimeArchitecture architecture, string applicationBaseUrl, ApplicationType applicationType)
        {
            return HttpsHelloWorld(ServerType.IISExpress, architecture, applicationBaseUrl, applicationType);
        }

        public async Task HttpsHelloWorld(ServerType serverType, RuntimeArchitecture architecture, string applicationBaseUrl, ApplicationType applicationType)
        {
            var testName = $"HttpsHelloWorld_{serverType}_{architecture}";
            using (StartLog(out var loggerFactory, testName))
            {
                var logger = loggerFactory.CreateLogger("HttpsHelloWorldTest");

                var deploymentParameters = new DeploymentParameters(Helpers.GetTestSitesPath(), serverType, RuntimeFlavor.CoreClr, architecture)
                {
                    ApplicationBaseUriHint = applicationBaseUrl,
                    EnvironmentName = "HttpsHelloWorld", // Will pick the Start class named 'StartupHttpsHelloWorld',
                    ServerConfigTemplateContent = (serverType == ServerType.IISExpress) ? File.ReadAllText("Https.config") : null,
                    SiteName = "HttpsTestSite", // This is configured in the Https.config
                    TargetFramework = "netcoreapp2.0",
                    ApplicationType = applicationType
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

        [ConditionalTheory]
        [OSSkipCondition(OperatingSystems.Linux | OperatingSystems.MacOSX)]
        //[InlineData(RuntimeArchitecture.x86, "https://localhost:44399/", ApplicationType.Standalone)]
        [InlineData(RuntimeArchitecture.x64, "https://localhost:44398/", ApplicationType.Portable)]
        public Task Https_HelloWorld_NoClientCert(RuntimeArchitecture architecture, string applicationBaseUrl, ApplicationType applicationType)
        {
            return HttpsHelloWorldCerts(ServerType.IISExpress, architecture, applicationBaseUrl, applicationType, sendClientCert: false);
        }

        [ConditionalTheory(Skip = "Manual test only, selecting a client cert is non-determanistic on different machines.")]
        [OSSkipCondition(OperatingSystems.Linux | OperatingSystems.MacOSX)]
        [InlineData(RuntimeArchitecture.x64, "https://localhost:44301/", ApplicationType.Portable)]
        public Task Https_HelloWorld_ClientCert(RuntimeArchitecture architecture, string applicationBaseUrl, ApplicationType applicationType)
        {
            return HttpsHelloWorldCerts(ServerType.IISExpress, architecture, applicationBaseUrl, applicationType, sendClientCert: true);
        }

        public async Task HttpsHelloWorldCerts(ServerType serverType, RuntimeArchitecture architecture, string applicationBaseUrl, ApplicationType applicationType, bool sendClientCert)
        {
            var testName = $"HttpsHelloWorldCerts_{serverType}_{architecture}";
            using (StartLog(out var loggerFactory, testName))
            {
                var logger = loggerFactory.CreateLogger("HttpsHelloWorldTest");

                var deploymentParameters = new DeploymentParameters(Helpers.GetTestSitesPath(), serverType, RuntimeFlavor.CoreClr, architecture)
                {
                    ApplicationBaseUriHint = applicationBaseUrl,
                    EnvironmentName = "HttpsHelloWorld", // Will pick the Start class named 'StartupHttpsHelloWorld',
                    ServerConfigTemplateContent = (serverType == ServerType.IISExpress) ? File.ReadAllText("Https.config") : null,
                    SiteName = "HttpsTestSite", // This is configured in the Https.config
                    TargetFramework = "netcoreapp2.0",
                    ApplicationType = applicationType
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
