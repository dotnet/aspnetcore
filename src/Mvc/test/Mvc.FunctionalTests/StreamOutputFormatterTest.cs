// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.FunctionalTests
{
    public class StreamOutputFormatterTest : IClassFixture<MvcTestFixture<FormatterWebSite.Startup>>
    {
        public StreamOutputFormatterTest(MvcTestFixture<FormatterWebSite.Startup> fixture)
        {
            Client = fixture.CreateDefaultClient();
        }

        public HttpClient Client { get; }

        [Theory]
        [InlineData("SimpleMemoryStream", null)]
        [InlineData("MemoryStreamWithContentType", "text/html")]
        [InlineData("MemoryStreamWithContentTypeFromProduces", "text/plain")]
        [InlineData("MemoryStreamWithContentTypeFromProducesWithMultipleValues", "text/html")]
        [InlineData("MemoryStreamOverridesProducesContentTypeWithResponseContentType", "text/plain")]
        public async Task StreamOutputFormatter_ReturnsAppropriateContentAndContentType(string actionName, string contentType)
        {
            // Arrange & Act
            var response = await Client.GetAsync("http://localhost/Stream/" + actionName);
            var body = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal(contentType, response.Content.Headers.ContentType?.ToString());

            Assert.Equal("Sample text from a stream", body);
        }
    }
}
