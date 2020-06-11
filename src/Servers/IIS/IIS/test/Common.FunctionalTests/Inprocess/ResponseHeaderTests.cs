// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Testing;
using Microsoft.Net.Http.Headers;
using Xunit;

namespace Microsoft.AspNetCore.Server.IIS.FunctionalTests.InProcess
{
    [Collection(IISTestSiteCollection.Name)]
    public class ResponseHeaders
    {
        private readonly IISTestSiteFixture _fixture;

        public ResponseHeaders(IISTestSiteFixture fixture)
        {
            _fixture = fixture;
        }

        [ConditionalFact]
        public async Task AddEmptyHeaderSkipped()
        {
            var response = await _fixture.Client.GetAsync("ResponseEmptyHeaders");
            var responseText = await response.Content.ReadAsStringAsync();
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.False(response.Headers.TryGetValues("EmptyHeader", out var headerValues));
        }

        [ConditionalFact]
        public async Task AddResponseHeaders_HeaderValuesAreSetCorrectly()
        {
            var response = await _fixture.Client.GetAsync("ResponseHeaders");
            var responseText = await response.Content.ReadAsStringAsync();
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("Request Complete", responseText);

            Assert.True(response.Headers.TryGetValues("UnknownHeader", out var headerValues));
            Assert.Equal("test123=foo", headerValues.First());

            Assert.True(response.Content.Headers.TryGetValues(HeaderNames.ContentType, out headerValues));
            Assert.Equal("text/plain", headerValues.First());

            Assert.True(response.Headers.TryGetValues("MultiHeader", out headerValues));
            Assert.Equal(2, headerValues.Count());
            Assert.Equal("1", headerValues.First());
            Assert.Equal("2", headerValues.Last());
        }

        [ConditionalFact]
        public async Task ErrorCodeIsSetForExceptionDuringRequest()
        {
            var response = await _fixture.Client.GetAsync("Throw");
            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
            Assert.Equal("Internal Server Error", response.ReasonPhrase);
        }

        [ConditionalTheory]
        [InlineData(200, "custom", "custom", null)]
        [InlineData(200, "custom", "custom", "Custom body")]
        [InlineData(200, "custom", "custom", "")]


        [InlineData(500, "", "Internal Server Error", null)]
        [InlineData(500, "", "Internal Server Error", "Custom body")]
        [InlineData(500, "", "Internal Server Error", "")]

        [InlineData(400, "custom", "custom", null)]
        [InlineData(400, "", "Bad Request", "Custom body")]
        [InlineData(400, "", "Bad Request", "")]

        [InlineData(999, "", "", null)]
        [InlineData(999, "", "", "Custom body")]
        [InlineData(999, "", "", "")]
        public async Task CustomErrorCodeWorks(int code, string reason, string expectedReason, string body)
        {
            var response = await _fixture.Client.GetAsync($"SetCustomErorCode?code={code}&reason={reason}&writeBody={body != null}&body={body}");
            Assert.Equal((HttpStatusCode)code, response.StatusCode);
            Assert.Equal(expectedReason, response.ReasonPhrase);

            // ReadAsStringAsync returns empty string for empty results
            Assert.Equal(body ?? string.Empty, await response.Content.ReadAsStringAsync());
        }

        [ConditionalTheory]
        [RequiresNewHandler]
        [InlineData(204, "GET")]
        [InlineData(304, "GET")]
        public async Task TransferEncodingNotSetForStatusCodes(int code, string method)
        {
            var request = new HttpRequestMessage(new HttpMethod(method), _fixture.Client.BaseAddress + $"SetCustomErorCode?code={code}");
            var response = await _fixture.Client.SendAsync(request);
            Assert.Equal((HttpStatusCode)code, response.StatusCode);
            Assert.DoesNotContain(response.Headers, h => h.Key.Equals("transfer-encoding", StringComparison.InvariantCultureIgnoreCase));
        }

        [ConditionalFact]
        public async Task ServerHeaderIsOverriden()
        {
            var response = await _fixture.Client.GetAsync("OverrideServer");
            Assert.Equal("MyServer/7.8", response.Headers.Server.Single().Product.ToString());
        }
    }
}
