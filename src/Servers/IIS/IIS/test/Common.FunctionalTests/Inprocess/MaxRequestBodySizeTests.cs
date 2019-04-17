// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.IIS.FunctionalTests.Utilities;
using Microsoft.AspNetCore.Server.IntegrationTesting.IIS;
using Microsoft.AspNetCore.Testing.xunit;
using Xunit;

namespace Microsoft.AspNetCore.Server.IISIntegration.FunctionalTests
{
    [Collection(PublishedSitesCollection.Name)]
    public class MaxRequestBodySizeTests : IISFunctionalTestBase
    {
        public MaxRequestBodySizeTests(PublishedSitesFixture fixture) : base(fixture)
        {
        }

        [ConditionalFact]
        [RequiresNewHandler]
        public async Task MaxRequestBodySizeE2EWorks()
        {
            var deploymentParameters = Fixture.GetBaseDeploymentParameters();
            deploymentParameters.TransformArguments((a, _) => $"{a} DecreaseRequestLimit");

            var deploymentResult = await DeployAsync(deploymentParameters);

            var result = await deploymentResult.HttpClient.PostAsync("/ReadRequestBody", new StringContent("test"));
            Assert.Equal(HttpStatusCode.RequestEntityTooLarge, result.StatusCode);
        }
    }
}
