// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.IIS.FunctionalTests.Utilities;
using Microsoft.AspNetCore.Server.IISIntegration.FunctionalTests;
using Microsoft.AspNetCore.Server.IntegrationTesting;
using Microsoft.AspNetCore.Server.IntegrationTesting.Common;
using Microsoft.AspNetCore.Server.IntegrationTesting.IIS;
using Microsoft.AspNetCore.Testing.xunit;
using Xunit;

namespace Microsoft.AspNetCore.Server.IIS.FunctionalTests
{
    [Collection(PublishedSitesCollection.Name)]
    [SkipIfNotAdmin]
    public class ClientCertificateTests : IISFunctionalTestBase
    {
        private readonly PublishedSitesFixture _fixture;
        private readonly ClientCertificateFixture _certFixture;

        public ClientCertificateTests(PublishedSitesFixture fixture, ClientCertificateFixture certFixture)
        {
            _fixture = fixture;
            _certFixture = certFixture;
        }

        public static TestMatrix TestVariants
            => TestMatrix.ForServers(DeployerSelector.ServerType)
                .WithTfms(Tfm.NetCoreApp22, Tfm.Net461)
                .WithAllApplicationTypes()
                .WithAllAncmVersions()
                .WithAllHostingModels();

        [ConditionalTheory]
        [MemberData(nameof(TestVariants))]
        public Task HttpsNoClientCert_NoClientCert(TestVariant variant)
        {
            return ClientCertTest(variant, sendClientCert: false);
        }

        [ConditionalTheory]
        [MemberData(nameof(TestVariants))]
        public Task HttpsClientCert_GetCertInformation(TestVariant variant)
        {
            return ClientCertTest(variant, sendClientCert: true);
        }

        private async Task ClientCertTest(TestVariant variant, bool sendClientCert)
        {
            var port = TestPortHelper.GetNextSSLPort();
            var deploymentParameters = _fixture.GetBaseDeploymentParameters(variant);
            deploymentParameters.ApplicationBaseUriHint = $"https://localhost:{port}/";
            deploymentParameters.AddHttpsToServerConfig();

            var deploymentResult = await DeployAsync(deploymentParameters);
            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (a, b, c, d) => true,
                ClientCertificateOptions = ClientCertificateOption.Manual,
            };

            if (sendClientCert)
            {
                Assert.NotNull(_certFixture.Certificate);
                handler.ClientCertificates.Add(_certFixture.Certificate);
            }

            var client = deploymentResult.CreateClient(handler);
            var response = await client.GetAsync("GetClientCert");

            var responseText = await response.Content.ReadAsStringAsync();

            if (sendClientCert)
            {
                Assert.Equal($"Enabled;{_certFixture.Certificate.GetCertHashString()}", responseText);
            }
            else
            {
                Assert.Equal("Disabled", responseText);
            }
        }
    }
}
