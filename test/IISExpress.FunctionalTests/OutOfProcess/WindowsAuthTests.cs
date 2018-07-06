// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.IIS.FunctionalTests.Utilities;
using Microsoft.AspNetCore.Server.IntegrationTesting;
using Microsoft.AspNetCore.Testing.xunit;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Server.IISIntegration.FunctionalTests
{
    public class WindowsAuthTests : IISFunctionalTestBase
    {
        public WindowsAuthTests(ITestOutputHelper output = null) : base(output)
        {
        }

        public static TestMatrix TestVariants
            => TestMatrix.ForServers(DeployerSelector.ServerType)
                .WithTfms(Tfm.NetCoreApp22, Tfm.Net461)
                .WithAllApplicationTypes()
                .WithAllAncmVersions();

        [ConditionalTheory]
        [MemberData(nameof(TestVariants))]
        public async Task WindowsAuthTest(TestVariant variant)
        {
            var deploymentParameters = new DeploymentParameters(variant)
            {
                ApplicationPath = Helpers.GetOutOfProcessTestSitesPath(),
                ServerConfigTemplateContent = GetWindowsAuthConfig()
            };

            // The default in hosting sets windows auth to true.
            var deploymentResult = await DeployAsync(deploymentParameters);

            var response = await deploymentResult.HttpClient.GetAsync("/Auth");
            var responseText = await response.Content.ReadAsStringAsync();

            Assert.True("backcompat;Windows".Equals(responseText) || "latest;Windows".Equals(responseText), "Auth");
        }
    }
}
