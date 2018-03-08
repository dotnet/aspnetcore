// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#if NET461

using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Testing.xunit;
using Xunit;

namespace Microsoft.AspNetCore.Server.IISIntegration.FunctionalTests
{
    [Collection(IISTestSiteCollection.Name)]
    public class AuthenticationTests
    {
        private readonly IISTestSiteFixture _fixture;
        public AuthenticationTests(IISTestSiteFixture fixture)
        {
            _fixture = fixture;
        }

        [ConditionalFact]
        public async Task Authentication_InProcess_IISExpressAsync()
        {
            var response = await _fixture.Client.GetAsync("/AuthenticationAnonymous");

            var responseText = await response.Content.ReadAsStringAsync();
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("Anonymous?True", responseText);

            response = await _fixture.Client.GetAsync("/AuthenticationRestricted");
            responseText = await response.Content.ReadAsStringAsync();
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            Assert.Contains("NTLM", response.Headers.WwwAuthenticate.ToString());
            Assert.Contains("Negotiate", response.Headers.WwwAuthenticate.ToString());

            response = await _fixture.Client.GetAsync("/AuthenticationRestrictedNTLM");
            responseText = await response.Content.ReadAsStringAsync();
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            Assert.Contains("NTLM", response.Headers.WwwAuthenticate.ToString());
            // Note we can't restrict a challenge to a specific auth type, the native auth modules always add themselves.
            Assert.Contains("Negotiate", response.Headers.WwwAuthenticate.ToString());

            response = await _fixture.Client.GetAsync("/AuthenticationForbidden");
            responseText = await response.Content.ReadAsStringAsync();
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

            var httpClientHandler = new HttpClientHandler() { UseDefaultCredentials = true };
            var httpClient = _fixture.DeploymentResult.CreateHttpClient(httpClientHandler);

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
#elif NETCOREAPP2_0 || NETCOREAPP2_1
#else
#error Target frameworks need to be updated
#endif
