// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.IIS.FunctionalTests.Utilities;
using Microsoft.AspNetCore.Server.IntegrationTesting;
using Microsoft.AspNetCore.Testing;
using Xunit;

namespace Microsoft.AspNetCore.Server.IIS.FunctionalTests
{
    [Collection(PublishedSitesCollection.Name)]
    public class CompressionTests : IISFunctionalTestBase
    {
        public CompressionTests(PublishedSitesFixture fixture) : base(fixture)
        {
        }

        [ConditionalFact]
        public async Task PassesThroughCompressionOutOfProcess()
        {
            var deploymentParameters = Fixture.GetBaseDeploymentParameters(HostingModel.OutOfProcess);

            var deploymentResult = await DeployAsync(deploymentParameters);

            var request = new HttpRequestMessage(HttpMethod.Get, "/CompressedData");

            request.Headers.AcceptEncoding.Add(new StringWithQualityHeaderValue("gzip"));

            var response = await deploymentResult.HttpClient.SendAsync(request);
            Assert.Equal("gzip", response.Content.Headers.ContentEncoding.Single());
            Assert.Equal(
                new byte[] {
                    0x1F, 0x8B, 0x08, 0x00, 0x00, 0x00, 0x00, 0x00,
                    0x04, 0x0A, 0x63, 0x60, 0xA0, 0x3D, 0x00, 0x00,
                    0xCA, 0xC6, 0x88, 0x99, 0x64, 0x00, 0x00, 0x00 },
                await response.Content.ReadAsByteArrayAsync());
        }

        [ConditionalFact]
        public async Task PassesThroughCompressionInProcess()
        {
            var deploymentParameters = Fixture.GetBaseDeploymentParameters(HostingModel.InProcess);

            var deploymentResult = await DeployAsync(deploymentParameters);

            var request = new HttpRequestMessage(HttpMethod.Get, "/CompressedData");

            request.Headers.AcceptEncoding.Add(new StringWithQualityHeaderValue("gzip"));

            var response = await deploymentResult.HttpClient.SendAsync(request);
            Assert.Equal("gzip", response.Content.Headers.ContentEncoding.Single());
            Assert.Equal(
                new byte[] {
                    0x1F, 0x8B, 0x08, 0x00, 0x00, 0x00, 0x00, 0x00,
                    0x04, 0x0A, 0x63, 0x60, 0xA0, 0x3D, 0x00, 0x00,
                    0xCA, 0xC6, 0x88, 0x99, 0x64, 0x00, 0x00, 0x00 },
                await response.Content.ReadAsByteArrayAsync());
        }
    }
}
