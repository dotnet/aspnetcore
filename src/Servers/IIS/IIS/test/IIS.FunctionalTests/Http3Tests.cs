// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.IIS.FunctionalTests;
using Microsoft.AspNetCore.Server.IntegrationTesting.Common;
using Microsoft.AspNetCore.Server.IntegrationTesting.IIS;
using Microsoft.AspNetCore.Testing;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;
using Microsoft.Win32;
using Xunit;

namespace Microsoft.AspNetCore.Server.IIS.FunctionalTests
{
    [MsQuicSupported]
    [Collection(IISHttpsTestSiteCollection.Name)]
    public class Http3Tests
    {
        public Http3Tests(IISTestSiteFixture fixture)
        {
            var port = TestPortHelper.GetNextSSLPort();
            fixture.DeploymentParameters.ApplicationBaseUriHint = $"https://localhost:{port}/";
            fixture.DeploymentParameters.AddHttpsToServerConfig();
            fixture.DeploymentParameters.SetWindowsAuth(false);
            Fixture = fixture;
        }

        public IISTestSiteFixture Fixture { get; }

        [ConditionalFact]
        public async Task Http3_Direct()
        {
            var response = await SendRequestAsync(Fixture.Client.BaseAddress.ToString() + "Http3_Direct");

            response.EnsureSuccessStatusCode();
            Assert.Equal(HttpVersion.Version30, response.Version);
        }

        private async Task<HttpResponseMessage> SendRequestAsync(string uri)
        {
            var handler = new HttpClientHandler();
            handler.MaxResponseHeadersLength = 128;
            handler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
            using var client = new HttpClient(handler);
            client.DefaultRequestVersion = HttpVersion.Version30;
            client.DefaultVersionPolicy = HttpVersionPolicy.RequestVersionExact;
            return await client.GetAsync(uri);
        }
    }
}
