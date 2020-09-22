// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.AspNetCore.Server.IIS.FunctionalTests.Utilities;
using Microsoft.AspNetCore.Server.IntegrationTesting;
using Microsoft.AspNetCore.Server.IntegrationTesting.Common;
using Microsoft.AspNetCore.Server.IntegrationTesting.IIS;
using Microsoft.AspNetCore.Testing;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Microsoft.AspNetCore.Server.IIS.FunctionalTests
{
    [Collection(IISHttpsTestSiteCollection.Name)]
    public class HttpsTests
    {
        private readonly ClientCertificateFixture _certFixture;

        public HttpsTests(IISTestSiteFixture fixture, ClientCertificateFixture certFixture)
        {
            var port = TestPortHelper.GetNextSSLPort();
            fixture.DeploymentParameters.ApplicationBaseUriHint = $"https://localhost:{port}/";
            fixture.DeploymentParameters.AddHttpsToServerConfig();
            fixture.DeploymentParameters.SetWindowsAuth(false);
            Fixture = fixture;
            _certFixture = certFixture;
        }

        public IISTestSiteFixture Fixture { get; }

        public static TestMatrix TestVariants
            => TestMatrix.ForServers(DeployerSelector.ServerType)
                .WithTfms(Tfm.Net50)
                .WithApplicationTypes(ApplicationType.Portable)
                .WithAllHostingModels();

        [ConditionalTheory]
        [MemberData(nameof(TestVariants))]
        [MinimumOSVersion(OperatingSystems.Windows, WindowsVersions.Win8)]
        public Task HttpsNoClientCert_NoClientCert(TestVariant variant)
        {
            return ClientCertTest(variant, sendClientCert: false);
        }

        [ConditionalTheory]
        [MemberData(nameof(TestVariants))]
        [MinimumOSVersion(OperatingSystems.Windows, WindowsVersions.Win8)]
        public Task HttpsClientCert_GetCertInformation(TestVariant variant)
        {
            return ClientCertTest(variant, sendClientCert: true);
        }

        private async Task ClientCertTest(TestVariant variant, bool sendClientCert)
        {

            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (a, b, c, d) => true,
                ClientCertificateOptions = ClientCertificateOption.Manual,
            };

            X509Certificate2 cert = null;
            if (sendClientCert)
            {
                cert = _certFixture.GetOrCreateCertificate();
                handler.ClientCertificates.Add(cert);
            }

            var client = new HttpClient(handler);
            client.BaseAddress = Fixture.Client.BaseAddress;
            var response = await client.GetAsync("GetClientCert");

            var responseText = await response.Content.ReadAsStringAsync();

            if (sendClientCert)
            {
                Assert.Equal($"Enabled;{cert.GetCertHashString()}", responseText);
            }
            else
            {
                Assert.Equal("Disabled", responseText);
            }
        }
    }
}
