// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.IIS.FunctionalTests.Utilities;
using Microsoft.AspNetCore.Server.IntegrationTesting;
using Microsoft.AspNetCore.Testing.xunit;
using Xunit;

namespace Microsoft.AspNetCore.Server.IISIntegration.FunctionalTests
{
    [Collection(PublishedSitesCollection.Name)]
    public class CommonStartupTests : IISFunctionalTestBase
    {
        private readonly PublishedSitesFixture _fixture;

        public CommonStartupTests(PublishedSitesFixture fixture)
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
        public async Task StartupStress(TestVariant variant)
        {
            var deploymentParameters = _fixture.GetBaseDeploymentParameters(variant, publish: true);

            var deploymentResult = await DeployAsync(deploymentParameters);

            await Helpers.StressLoad(deploymentResult.HttpClient, "/HelloWorld", response => {
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                Assert.Equal("Hello World", response.Content.ReadAsStringAsync().GetAwaiter().GetResult());
            });
        }
    }
}
