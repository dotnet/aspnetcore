// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.IIS.FunctionalTests.Utilities;
using Microsoft.AspNetCore.Server.IntegrationTesting;
using Microsoft.AspNetCore.Server.IntegrationTesting.IIS;
using Microsoft.AspNetCore.Testing;
using Xunit;

namespace Microsoft.AspNetCore.Server.IIS.FunctionalTests.InProcess
{
    [Collection(PublishedSitesCollection.Name)]
    public class Latin1Tests : IISFunctionalTestBase
    {
        public Latin1Tests(PublishedSitesFixture fixture) : base(fixture)
        {
        }

        [ConditionalFact]
        [RequiresNewHandler]
        public async Task Latin1Works()
        {
            var deploymentParameters = Fixture.GetBaseDeploymentParameters();
            deploymentParameters.TransformArguments((a, _) => $"{a} AddLatin1");

            var deploymentResult = await DeployAsync(deploymentParameters);

            var client = new HttpClient(new LoggingHandler(new WinHttpHandler(), deploymentResult.Logger));

            var requestMessage = new HttpRequestMessage(HttpMethod.Get, "/Latin1");
            requestMessage.Headers.Add("foo", "£");

            var result = await deploymentResult.HttpClient.SendAsync(requestMessage);
            Assert.Equal(HttpStatusCode.RequestEntityTooLarge, result.StatusCode);
        }
    }
}
