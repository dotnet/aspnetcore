// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http;

namespace Microsoft.AspNetCore.Mvc.FunctionalTests;

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
