// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.IIS.FunctionalTests.Utilities;
using Microsoft.AspNetCore.Server.IntegrationTesting;
using Microsoft.AspNetCore.Server.IntegrationTesting.Common;
using Microsoft.AspNetCore.Testing.xunit;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Server.IISIntegration.FunctionalTests
{
    // IIS Express preregisteres 44300-44399 ports with SSL bindings.
    // So these tests always have to use ports in this range, and we can't rely on OS-allocated ports without a whole lot of ceremony around
    // creating self-signed certificates and registering SSL bindings with HTTP.sys
    // Test specific to IISExpress
    public class HttpsTest : IISFunctionalTestBase
    {
        public HttpsTest(ITestOutputHelper output) : base(output)
        {
        }

        public static TestMatrix TestVariants
            => TestMatrix.ForServers(DeployerSelector.ServerType)
                .WithTfms(Tfm.NetCoreApp22, Tfm.Net461)
                .WithAllAncmVersions();

        [ConditionalTheory]
        [MemberData(nameof(TestVariants))]
        public async Task HttpsHelloWorld(TestVariant variant)
        {
            var port = TestPortHelper.GetNextSSLPort();
            var deploymentParameters = new DeploymentParameters(variant)
            {
                ApplicationPath = Helpers.GetOutOfProcessTestSitesPath(),
                ApplicationBaseUriHint = $"https://localhost:{port}/",
                ServerConfigTemplateContent = GetHttpsServerConfig()
            };

            var deploymentResult = await DeployAsync(deploymentParameters);

            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (a, b, c, d) => true
            };
            var client = deploymentResult.CreateRetryClient(handler);
            var response = await client.GetAsync("HttpsHelloWorld");
            var responseText = await response.Content.ReadAsStringAsync();
            Assert.Equal("Scheme:https; Original:http", responseText);
        }

        [ConditionalTheory]
        [MemberData(nameof(TestVariants))]
        public Task HttpsHelloWorld_NoClientCert(TestVariant variant)
        {
            return HttpsHelloWorldCerts(variant, sendClientCert: false);
        }

#pragma warning disable xUnit1004 // Test methods should not be skipped
        [ConditionalTheory(Skip = "Manual test only, selecting a client cert is non-determanistic on different machines.")]
        [MemberData(nameof(TestVariants))]
#pragma warning restore xUnit1004 // Test methods should not be skipped
        public Task HttpsHelloWorld_ClientCert(TestVariant variant)
        {
            return HttpsHelloWorldCerts(variant, sendClientCert: true);
        }

        private async Task HttpsHelloWorldCerts(TestVariant variant, bool sendClientCert)
        {
            var port = TestPortHelper.GetNextSSLPort();
            var deploymentParameters = new DeploymentParameters(variant)
            {
                ApplicationPath = Helpers.GetOutOfProcessTestSitesPath(),
                ApplicationBaseUriHint = $"https://localhost:{port}/",
                ServerConfigTemplateContent = GetHttpsServerConfig()
            };

            var deploymentResult = await DeployAsync(deploymentParameters);

            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (a, b, c, d) => true,
                ClientCertificateOptions = ClientCertificateOption.Manual
            };

            if (sendClientCert)
            {
                X509Certificate2 clientCert = FindClientCert();
                Assert.NotNull(clientCert);
                handler.ClientCertificates.Add(clientCert);
            }

            var client = deploymentResult.CreateRetryClient(handler);

            // Request to base address and check if various parts of the body are rendered & measure the cold startup time.
            var response = await client.GetAsync("checkclientcert");

            var responseText = await response.Content.ReadAsStringAsync();
            if (sendClientCert)
            {
                Assert.Equal("Scheme:https; Original:http; has cert? True", responseText);
            }
            else
            {
                Assert.Equal("Scheme:https; Original:http; has cert? False", responseText);
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
