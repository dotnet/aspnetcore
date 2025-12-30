// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http;
using System.Reflection;
using Microsoft.AspNetCore.InternalTesting;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Mvc.FunctionalTests;

public class StreamOutputFormatterTest : LoggedTest
{
    protected override void Initialize(TestContext context, MethodInfo methodInfo, object[] testMethodArguments, ITestOutputHelper testOutputHelper)
    {
        base.Initialize(context, methodInfo, testMethodArguments, testOutputHelper);
        Factory = new MvcTestFixture<FormatterWebSite.Startup>(LoggerFactory);
        Client = Factory.CreateDefaultClient();
    }

    public override void Dispose()
    {
        Factory.Dispose();
        base.Dispose();
    }

    public MvcTestFixture<FormatterWebSite.Startup> Factory { get; private set; }
    public HttpClient Client { get; private set; }

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
