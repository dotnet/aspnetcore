// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.IIS.FunctionalTests.Utilities;
using Microsoft.AspNetCore.Server.IntegrationTesting;
using Microsoft.AspNetCore.Server.IntegrationTesting.IIS;
using Microsoft.AspNetCore.Testing.xunit;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Server.IISIntegration.FunctionalTests
{
    [Collection(PublishedSitesCollection.Name)]
    public class NtlmAuthenticationTests : IISFunctionalTestBase
    {
        // Test only runs on IISExpress today as our CI machines do not have
        // Windows auth installed globally.
        // TODO either enable windows auth on our CI or use containers to test this
        // behavior

        private readonly PublishedSitesFixture _fixture;

        public NtlmAuthenticationTests(PublishedSitesFixture fixture)
        {
            _fixture = fixture;
        }

        public static TestMatrix TestVariants
            => TestMatrix.ForServers(DeployerSelector.ServerType)
                .WithTfms(Tfm.NetCoreApp30)
                .WithAllAncmVersions();

        [ConditionalTheory]
        [RequiresIIS(IISCapability.WindowsAuthentication)]
        [MemberData(nameof(TestVariants))]
        public async Task NtlmAuthentication(TestVariant variant)
        {
            var deploymentParameters = _fixture.GetBaseDeploymentParameters(variant);
            deploymentParameters.ApplicationBaseUriHint = $"https://localhost:0/";

            deploymentParameters.SetWindowsAuth(enabled: true);

            var result = await DeployAsync(deploymentParameters);
            var response = await result.HttpClient.GetAsync("/HelloWorld");

            var responseText = await response.Content.ReadAsStringAsync();
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("Hello World", responseText);

            var httpClient = result.HttpClient;
            response = await httpClient.GetAsync("/Anonymous");
            responseText = await response.Content.ReadAsStringAsync();
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("Anonymous?True", responseText);

            response = await httpClient.GetAsync("/Restricted");
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            Assert.Contains("NTLM", response.Headers.WwwAuthenticate.ToString());
            Assert.Contains("Negotiate", response.Headers.WwwAuthenticate.ToString());

            response = await httpClient.GetAsync("/RestrictedNTLM");
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            Assert.Contains("NTLM", response.Headers.WwwAuthenticate.ToString());
            // Note we can't restrict a challenge to a specific auth type, the native auth modules always add themselves.
            Assert.Contains("Negotiate", response.Headers.WwwAuthenticate.ToString());

            response = await httpClient.GetAsync("/Forbidden");
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

            var httpClientHandler = new HttpClientHandler() { UseDefaultCredentials = true };
            httpClient = result.CreateClient(httpClientHandler);

            response = await httpClient.GetAsync("/Anonymous");
            responseText = await response.Content.ReadAsStringAsync();
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("Anonymous?True", responseText);

            response = await httpClient.GetAsync("/Restricted");
            responseText = await response.Content.ReadAsStringAsync();
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotEmpty(responseText);
        }
    }
}
