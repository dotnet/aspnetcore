// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net.Http;
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
                .WithTfms(Tfm.NetCoreApp30)
                .WithApplicationTypes(ApplicationType.Portable)
                .WithAllAncmVersions()
                .WithAllHostingModels();

        [ConditionalTheory]
        [RequiresIIS(IISCapability.WindowsAuthentication)]
        [MemberData(nameof(TestVariants))]
        public async Task WindowsAuthTest(TestVariant variant)
        {
            var deploymentParameters = _fixture.GetBaseDeploymentParameters(variant);
            deploymentParameters.SetAnonymousAuth(enabled: false);
            deploymentParameters.SetWindowsAuth();

            // The default in hosting sets windows auth to true.
            var deploymentResult = await DeployAsync(deploymentParameters);

            var client = deploymentResult.CreateClient(new HttpClientHandler { UseDefaultCredentials = true });
            var response = await client.GetAsync("/Auth");
            var responseText = await response.Content.ReadAsStringAsync();

            Assert.StartsWith("Windows:", responseText);
            Assert.Contains(Environment.UserName, responseText);
        }
    }
}
