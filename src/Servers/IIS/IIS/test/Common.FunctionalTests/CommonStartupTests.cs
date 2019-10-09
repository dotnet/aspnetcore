// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.IIS.FunctionalTests.Utilities;
using Microsoft.AspNetCore.Server.IntegrationTesting;
using Microsoft.AspNetCore.Testing;
using Xunit;

namespace Microsoft.AspNetCore.Server.IIS.FunctionalTests
{
    [Collection(PublishedSitesCollection.Name)]
    public class CommonStartupTests : IISFunctionalTestBase
    {
        public CommonStartupTests(PublishedSitesFixture fixture) : base(fixture)
        {
        }

        public static TestMatrix TestVariants
            => TestMatrix.ForServers(DeployerSelector.ServerType)
                .WithTfms(Tfm.NetCoreApp50)
                .WithAllApplicationTypes()
                .WithAllHostingModels();

        [ConditionalTheory]
        [MemberData(nameof(TestVariants))]
        public async Task StartupStress(TestVariant variant)
        {
            var deploymentParameters = Fixture.GetBaseDeploymentParameters(variant);

            var deploymentResult = await DeployAsync(deploymentParameters);

            await Helpers.StressLoad(deploymentResult.HttpClient, "/HelloWorld", response => {
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                Assert.Equal("Hello World", response.Content.ReadAsStringAsync().GetAwaiter().GetResult());
            });
        }
    }
}
