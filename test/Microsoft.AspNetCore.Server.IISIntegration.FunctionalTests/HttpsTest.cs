// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.Testing;
using Microsoft.AspNetCore.Testing.xunit;
using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Sdk;

namespace Microsoft.AspNetCore.Server.IISIntegration.FunctionalTests
{
    // IisExpress preregisteres 44300-44399 ports.
    public class HttpsTest
    {
        [ConditionalTheory]
        [OSSkipCondition(OperatingSystems.Linux)]
        [OSSkipCondition(OperatingSystems.MacOSX)]
        //[InlineData(RuntimeFlavor.CoreClr, RuntimeArchitecture.x86, "https://localhost:44395/", ApplicationType.Standalone)]
        [InlineData(RuntimeFlavor.Clr, RuntimeArchitecture.x64, "https://localhost:44396/", ApplicationType.Standalone)]
        public Task Https_HelloWorld(RuntimeFlavor runtimeFlavor, RuntimeArchitecture architecture, string applicationBaseUrl, ApplicationType applicationType)
        {
            return HttpsHelloWorld(ServerType.IISExpress, runtimeFlavor, architecture, applicationBaseUrl, applicationType);
        }

        public async Task HttpsHelloWorld(ServerType serverType, RuntimeFlavor runtimeFlavor, RuntimeArchitecture architecture, string applicationBaseUrl, ApplicationType applicationType)
        {
            var logger = new LoggerFactory()
                            .AddConsole()
                            .CreateLogger($"HttpsHelloWorld:{serverType}:{runtimeFlavor}:{architecture}");

            using (logger.BeginScope("HttpsHelloWorldTest"))
            {
                var deploymentParameters = new DeploymentParameters(Helpers.GetTestSitesPath(applicationType), serverType, runtimeFlavor, architecture)
                {
                    ApplicationBaseUriHint = applicationBaseUrl,
                    EnvironmentName = "HttpsHelloWorld", // Will pick the Start class named 'StartupHttpsHelloWorld',
                    ServerConfigTemplateContent = (serverType == ServerType.IISExpress) ? File.ReadAllText("Https.config") : null,
                    SiteName = "HttpsTestSite", // This is configured in the Https.config
                    PublishTargetFramework = runtimeFlavor == RuntimeFlavor.Clr ? "net451" : "netcoreapp1.0",
                    ApplicationType = applicationType
                };

                using (var deployer = ApplicationDeployerFactory.Create(deploymentParameters, logger))
                {
                    var deploymentResult = deployer.Deploy();
                    var handler = new WebRequestHandler();
                    handler.ServerCertificateValidationCallback = (a, b, c, d) => true;
                    var httpClient = new HttpClient(handler)
                    {
                        BaseAddress = new Uri(deploymentResult.ApplicationBaseUri),
                        Timeout = TimeSpan.FromSeconds(5),
                    };

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
        [OSSkipCondition(OperatingSystems.Linux)]
        [OSSkipCondition(OperatingSystems.MacOSX)]
        [InlineData(RuntimeFlavor.CoreClr, RuntimeArchitecture.x64, "https://localhost:44397/", ApplicationType.Standalone)]
        [InlineData(RuntimeFlavor.CoreClr, RuntimeArchitecture.x64, "https://localhost:44398/", ApplicationType.Portable)]
        //[InlineData(RuntimeFlavor.Clr, RuntimeArchitecture.x86, "https://localhost:44399/", ApplicationType.Standalone)]
        public Task Https_HelloWorld_NoClientCert(RuntimeFlavor runtimeFlavor, RuntimeArchitecture architecture, string applicationBaseUrl, ApplicationType applicationType)
        {
            return HttpsHelloWorldCerts(ServerType.IISExpress, runtimeFlavor, architecture, applicationBaseUrl, applicationType, sendClientCert: false);
        }

        [ConditionalTheory(Skip = "Manual test only, selecting a client cert is non-determanistic on different machines.")]
        [OSSkipCondition(OperatingSystems.Linux)]
        [OSSkipCondition(OperatingSystems.MacOSX)]
        //[InlineData(RuntimeFlavor.CoreClr, RuntimeArchitecture.x86, "https://localhost:44400/", ApplicationType.Standalone)]
        [InlineData(RuntimeFlavor.Clr, RuntimeArchitecture.x64, "https://localhost:44401/", ApplicationType.Standalone)]
        public Task Https_HelloWorld_ClientCert(RuntimeFlavor runtimeFlavor, RuntimeArchitecture architecture, string applicationBaseUrl, ApplicationType applicationType)
        {
            return HttpsHelloWorldCerts(ServerType.IISExpress, runtimeFlavor, architecture, applicationBaseUrl, applicationType, sendClientCert: true);
        }

        public async Task HttpsHelloWorldCerts(ServerType serverType, RuntimeFlavor runtimeFlavor, RuntimeArchitecture architecture, string applicationBaseUrl, ApplicationType applicationType, bool sendClientCert)
        {
            var logger = new LoggerFactory()
                            .AddConsole()
                            .CreateLogger($"HttpsHelloWorldCerts:{serverType}:{runtimeFlavor}:{architecture}");

            using (logger.BeginScope("HttpsHelloWorldTest"))
            {
                var deploymentParameters = new DeploymentParameters(Helpers.GetTestSitesPath(applicationType), serverType, runtimeFlavor, architecture)
                {
                    ApplicationBaseUriHint = applicationBaseUrl,
                    EnvironmentName = "HttpsHelloWorld", // Will pick the Start class named 'StartupHttpsHelloWorld',
                    ServerConfigTemplateContent = (serverType == ServerType.IISExpress) ? File.ReadAllText("Https.config") : null,
                    SiteName = "HttpsTestSite", // This is configured in the Https.config
                    PublishTargetFramework = runtimeFlavor == RuntimeFlavor.Clr ? "net451" : "netcoreapp1.0",
                    ApplicationType = applicationType
                };

                using (var deployer = ApplicationDeployerFactory.Create(deploymentParameters, logger))
                {
                    var deploymentResult = deployer.Deploy();
                    var handler = new WebRequestHandler();
                    handler.ServerCertificateValidationCallback = (a, b, c, d) => true;
                    handler.ClientCertificateOptions = ClientCertificateOption.Manual;
                    if (sendClientCert)
                    {
                        X509Certificate2 clientCert = FindClientCert();
                        Assert.NotNull(clientCert);
                        handler.ClientCertificates.Add(clientCert);
                    }
                    var httpClient = new HttpClient(handler) { BaseAddress = new Uri(deploymentResult.ApplicationBaseUri) };

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
