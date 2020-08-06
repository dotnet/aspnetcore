// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.IIS.FunctionalTests.Utilities;
using Microsoft.AspNetCore.Server.IntegrationTesting.Common;
using Microsoft.AspNetCore.Server.IntegrationTesting.IIS;
using Microsoft.AspNetCore.Testing;
using Microsoft.Net.Http.Headers;
using Xunit;

namespace Microsoft.AspNetCore.Server.IIS.FunctionalTests
{
    [Collection(PublishedSitesCollection.Name)]
    public class ResponseTrailersTests : IISFunctionalTestBase
    {
        private const string WindowsVersionForTrailers = "10.0.20180";

        public ResponseTrailersTests(PublishedSitesFixture fixture) : base(fixture)
        {
        }

        [ConditionalFact]
        [MinimumOSVersion(OperatingSystems.Windows, WindowsVersionForTrailers)]
        public async Task ResponseTrailers_HTTP2_TrailersAvailable()
        {
            var version = System.Environment.OSVersion.Version;

            var deploymentParameters = GetHttpsDeploymentParameters();
            var deploymentResult = await DeployAsync(deploymentParameters);

            var response = await SendRequestAsync(deploymentResult.HttpClient.BaseAddress.ToString() + "ResponseTrailers_HTTP2_TrailersAvailable");

            response.EnsureSuccessStatusCode();
            Assert.Equal(HttpVersion.Version20, response.Version);
            Assert.Empty(response.TrailingHeaders);
        }

        [ConditionalFact]
        [MinimumOSVersion(OperatingSystems.Windows, WindowsVersionForTrailers)]
        public async Task ResponseTrailers_HTTP1_TrailersNotAvailable()
        {
            var deploymentParameters = Fixture.GetBaseDeploymentParameters(hostingModel: IntegrationTesting.HostingModel.OutOfProcess);

            var deploymentResult = await DeployAsync(deploymentParameters);

            var response = await SendRequestAsync(deploymentResult.HttpClient.BaseAddress.ToString() + "ResponseTrailers_HTTP1_TrailersNotAvailable");

            response.EnsureSuccessStatusCode();
            Assert.Equal(HttpVersion.Version11, response.Version);
            Assert.Empty(response.TrailingHeaders);
        }

        [ConditionalFact]
        [MinimumOSVersion(OperatingSystems.Windows, WindowsVersionForTrailers)]
        public async Task ResponseTrailers_ProhibitedTrailers_Blocked()
        {
            var deploymentParameters = GetHttpsDeploymentParameters();
            var deploymentResult = await DeployAsync(deploymentParameters);

            var response = await SendRequestAsync(deploymentResult.HttpClient.BaseAddress.ToString() + "ResponseTrailers_ProhibitedTrailers_Blocked");

            response.EnsureSuccessStatusCode();
            Assert.Equal(HttpVersion.Version20, response.Version);
            Assert.Empty(response.TrailingHeaders);
        }

        [ConditionalFact]
        [MinimumOSVersion(OperatingSystems.Windows, WindowsVersionForTrailers)]
        public async Task ResponseTrailers_NoBody_TrailersSent()
        {
            var deploymentParameters = GetHttpsDeploymentParameters();
            var deploymentResult = await DeployAsync(deploymentParameters);

            var response = await SendRequestAsync(deploymentResult.HttpClient.BaseAddress.ToString() + "ResponseTrailers_NoBody_TrailersSent");

            response.EnsureSuccessStatusCode();
            Assert.Equal(HttpVersion.Version20, response.Version);
            Assert.NotEmpty(response.TrailingHeaders);
            Assert.Equal("TrailerValue", response.TrailingHeaders.GetValues("TrailerName").Single());
        }

        [ConditionalFact]
        [MinimumOSVersion(OperatingSystems.Windows, WindowsVersionForTrailers)]
        public async Task ResponseTrailers_WithBody_TrailersSent()
        {
            var deploymentParameters = GetHttpsDeploymentParameters();
            var deploymentResult = await DeployAsync(deploymentParameters);

            var response = await SendRequestAsync(deploymentResult.HttpClient.BaseAddress.ToString() + "ResponseTrailers_WithBody_TrailersSent");

            response.EnsureSuccessStatusCode();
            Assert.Equal(HttpVersion.Version20, response.Version);
            Assert.Equal("Hello World", await response.Content.ReadAsStringAsync());
            Assert.NotEmpty(response.TrailingHeaders);
            Assert.Equal("Trailer Value", response.TrailingHeaders.GetValues("TrailerName").Single());
        }

        [ConditionalFact]
        [MinimumOSVersion(OperatingSystems.Windows, WindowsVersionForTrailers)]
        public async Task ResponseTrailers_WithContentLengthBody_TrailersSent()
        {
            var body = "Hello World";

            var deploymentParameters = GetHttpsDeploymentParameters();
            var deploymentResult = await DeployAsync(deploymentParameters);

            var response = await SendRequestAsync(deploymentResult.HttpClient.BaseAddress.ToString() + "ResponseTrailers_WithContentLengthBody_TrailersSent");

            response.EnsureSuccessStatusCode();
            Assert.Equal(HttpVersion.Version20, response.Version);
            Assert.Equal(body, await response.Content.ReadAsStringAsync());
            Assert.NotEmpty(response.TrailingHeaders);
            Assert.Equal("Trailer Value", response.TrailingHeaders.GetValues("TrailerName").Single());
        }

        [ConditionalFact]
        [MinimumOSVersion(OperatingSystems.Windows, WindowsVersionForTrailers)]
        public async Task ResponseTrailers_WithTrailersBeforeContentLengthBody_TrailersSent()
        {
            var body = "Hello World";

            var deploymentParameters = GetHttpsDeploymentParameters();
            var deploymentResult = await DeployAsync(deploymentParameters);

            var response = await SendRequestAsync(deploymentResult.HttpClient.BaseAddress.ToString() + "ResponseTrailers_WithTrailersBeforeContentLengthBody_TrailersSent");

            response.EnsureSuccessStatusCode();
            Assert.Equal(HttpVersion.Version20, response.Version);
            // Avoid HttpContent's automatic content-length calculation.
            Assert.True(response.Content.Headers.TryGetValues(HeaderNames.ContentLength, out var contentLength), HeaderNames.ContentLength);
            Assert.Equal((2 * body.Length).ToString(CultureInfo.InvariantCulture), contentLength.First());
            Assert.Equal(body + body, await response.Content.ReadAsStringAsync());
            Assert.NotEmpty(response.TrailingHeaders);
            Assert.Equal("Trailer Value", response.TrailingHeaders.GetValues("TrailerName").Single());
        }

        [ConditionalFact]
        [MinimumOSVersion(OperatingSystems.Windows, WindowsVersionForTrailers)]
        public async Task ResponseTrailers_WithContentLengthBodyAndDeclared_TrailersSent()
        {
            var body = "Hello World";

            var deploymentParameters = GetHttpsDeploymentParameters();
            var deploymentResult = await DeployAsync(deploymentParameters);

            var response = await SendRequestAsync(deploymentResult.HttpClient.BaseAddress.ToString() + "ResponseTrailers_WithContentLengthBodyAndDeclared_TrailersSent");

            response.EnsureSuccessStatusCode();
            Assert.Equal(HttpVersion.Version20, response.Version);
            // Avoid HttpContent's automatic content-length calculation.
            Assert.True(response.Content.Headers.TryGetValues(HeaderNames.ContentLength, out var contentLength), HeaderNames.ContentLength);
            Assert.Equal(body.Length.ToString(CultureInfo.InvariantCulture), contentLength.First());
            Assert.Equal("TrailerName", response.Headers.Trailer.Single());
            Assert.Equal(body, await response.Content.ReadAsStringAsync());
            Assert.NotEmpty(response.TrailingHeaders);
            Assert.Equal("Trailer Value", response.TrailingHeaders.GetValues("TrailerName").Single());
        }

        [ConditionalFact]
        [MinimumOSVersion(OperatingSystems.Windows, WindowsVersionForTrailers)]
        public async Task ResponseTrailers_MultipleValues_SentAsSeparateHeaders()
        {
            var deploymentParameters = GetHttpsDeploymentParameters();
            var deploymentResult = await DeployAsync(deploymentParameters);

            var response = await SendRequestAsync(deploymentResult.HttpClient.BaseAddress.ToString() + "ResponseTrailers_MultipleValues_SentAsSeparateHeaders");

            response.EnsureSuccessStatusCode();
            Assert.Equal(HttpVersion.Version20, response.Version);
            Assert.NotEmpty(response.TrailingHeaders);

            Assert.Equal(new[] { "TrailerValue0", "TrailerValue1" }, response.TrailingHeaders.GetValues("TrailerName"));
        }

        private IISDeploymentParameters GetHttpsDeploymentParameters()
        {
            var port = TestPortHelper.GetNextSSLPort();
            var deploymentParameters = Fixture.GetBaseDeploymentParameters();
            deploymentParameters.ApplicationBaseUriHint = $"https://localhost:{port}/";
            deploymentParameters.AddHttpsToServerConfig();
            return deploymentParameters;
        }

        private async Task<HttpResponseMessage> SendRequestAsync(string uri, bool http2 = true)
        {
            var handler = new HttpClientHandler();
            handler.MaxResponseHeadersLength = 128;
            handler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
            using var client = new HttpClient(handler);
            client.DefaultRequestVersion = http2 ? HttpVersion.Version20 : HttpVersion.Version11;
            return await client.GetAsync(uri);
        }
    }
}
