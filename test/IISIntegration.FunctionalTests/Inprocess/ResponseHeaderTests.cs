// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Testing.xunit;
using Microsoft.Net.Http.Headers;
using Xunit;

namespace Microsoft.AspNetCore.Server.IISIntegration.FunctionalTests
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
    }
}
