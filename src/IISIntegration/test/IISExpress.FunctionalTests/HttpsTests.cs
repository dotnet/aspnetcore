// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.IIS.FunctionalTests.Utilities;
using Microsoft.AspNetCore.Server.IntegrationTesting;
using Microsoft.AspNetCore.Server.IntegrationTesting.Common;
using Microsoft.AspNetCore.Server.IntegrationTesting.IIS;
using Microsoft.AspNetCore.Testing.xunit;
using Xunit;

namespace Microsoft.AspNetCore.Server.IISIntegration.FunctionalTests
{
    [Collection(PublishedSitesCollection.Name)]
    public class HttpsTests : IISFunctionalTestBase
    {
        private readonly PublishedSitesFixture _fixture;

        public HttpsTests(PublishedSitesFixture fixture)
        {
            _fixture = fixture;
        }

        public static TestMatrix TestVariants
            => TestMatrix.ForServers(DeployerSelector.ServerType)
                .WithTfms(Tfm.NetCoreApp30)
                .WithAllApplicationTypes()
                .WithAllAncmVersions()
                .WithAllHostingModels();

        [ConditionalTheory]
        [MemberData(nameof(TestVariants))]
        public async Task HttpsHelloWorld(TestVariant variant)
        {
            var port = TestPortHelper.GetNextSSLPort();
            var deploymentParameters = _fixture.GetBaseDeploymentParameters(variant);
            deploymentParameters.ApplicationBaseUriHint = $"https://localhost:{port}/";
            deploymentParameters.AddHttpsToServerConfig();

            var deploymentResult = await DeployAsync(deploymentParameters);

            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (a, b, c, d) => true
            };
            var client = deploymentResult.CreateClient(handler);
            var response = await client.GetAsync("HttpsHelloWorld");
            var responseText = await response.Content.ReadAsStringAsync();
            if (variant.HostingModel == HostingModel.OutOfProcess)
            {
                Assert.Equal("Scheme:https; Original:http", responseText);
            }
            else
            {
                Assert.Equal("Scheme:https; Original:", responseText);
            }
        }
    }
}
