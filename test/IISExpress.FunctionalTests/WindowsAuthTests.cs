// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.IIS.FunctionalTests.Utilities;
using Microsoft.AspNetCore.Server.IntegrationTesting;
using Microsoft.AspNetCore.Server.IntegrationTesting.IIS;
using Microsoft.AspNetCore.Testing.xunit;
using Xunit;

namespace Microsoft.AspNetCore.Server.IISIntegration.FunctionalTests
{
    [Collection(PublishedSitesCollection.Name)]
    public class WindowsAuthTests : IISFunctionalTestBase
    {
        private readonly PublishedSitesFixture _fixture;

        public WindowsAuthTests(PublishedSitesFixture fixture)
        {
            _fixture = fixture;
        }

        public static TestMatrix TestVariants
            => TestMatrix.ForServers(DeployerSelector.ServerType)
                .WithTfms(Tfm.NetCoreApp22, Tfm.Net461)
                .WithAllApplicationTypes()
                .WithAllAncmVersions()
                .WithAllHostingModels();

        [ConditionalTheory]
        [MemberData(nameof(TestVariants))]
        public async Task WindowsAuthTest(TestVariant variant)
        {
            var deploymentParameters = _fixture.GetBaseDeploymentParameters(variant);
            deploymentParameters.AddWindowsAuthToServerConfig();

            // The default in hosting sets windows auth to true.
            var deploymentResult = await DeployAsync(deploymentParameters);

            var response = await deploymentResult.HttpClient.GetAsync("/Auth");
            var responseText = await response.Content.ReadAsStringAsync();

            Assert.Equal("Windows", responseText);
        }
    }
}
