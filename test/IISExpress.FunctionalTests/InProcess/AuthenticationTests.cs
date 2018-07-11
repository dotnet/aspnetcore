// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.IIS.FunctionalTests.Utilities;
using Microsoft.AspNetCore.Testing.xunit;
using Xunit;

namespace Microsoft.AspNetCore.Server.IISIntegration.FunctionalTests
{
    public class AuthenticationTests : IISFunctionalTestBase
    {

        [ConditionalFact]
        public async Task Authentication_InProcess()
        {
            var deploymentParameters = Helpers.GetBaseDeploymentParameters(publish: true);
            deploymentParameters.ServerConfigTemplateContent = GetWindowsAuthConfig();

            var deploymentResult = await DeployAsync(deploymentParameters);

            var response = await deploymentResult.RetryingHttpClient.GetAsync("/AuthenticationAnonymous");

            var responseText = await response.Content.ReadAsStringAsync();
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("Anonymous?True", responseText);

            response = await deploymentResult.RetryingHttpClient.GetAsync("/AuthenticationRestricted");
            responseText = await response.Content.ReadAsStringAsync();
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            Assert.Contains("NTLM", response.Headers.WwwAuthenticate.ToString());
            Assert.Contains("Negotiate", response.Headers.WwwAuthenticate.ToString());

            response = await deploymentResult.RetryingHttpClient.GetAsync("/AuthenticationRestrictedNTLM");
            responseText = await response.Content.ReadAsStringAsync();
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            Assert.Contains("NTLM", response.Headers.WwwAuthenticate.ToString());
            // Note we can't restrict a challenge to a specific auth type, the native auth modules always add themselves.
            Assert.Contains("Negotiate", response.Headers.WwwAuthenticate.ToString());

            response = await deploymentResult.RetryingHttpClient.GetAsync("/AuthenticationForbidden");
            responseText = await response.Content.ReadAsStringAsync();
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

            var httpClientHandler = new HttpClientHandler() { UseDefaultCredentials = true };
            var httpClient = deploymentResult.DeploymentResult.CreateHttpClient(httpClientHandler);

            response = await httpClient.GetAsync("/AuthenticationAnonymous");
            responseText = await response.Content.ReadAsStringAsync();
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("Anonymous?True", responseText);

            response = await httpClient.GetAsync("/AuthenticationRestricted");
            responseText = await response.Content.ReadAsStringAsync();
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotEmpty(responseText);
        }
    }
}
