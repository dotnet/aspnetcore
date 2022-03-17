// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http;

namespace Microsoft.AspNetCore.Mvc.FunctionalTests;

public class SimpleTests : IClassFixture<MvcTestFixture<SimpleWebSite.Startup>>
{
    public SimpleTests(MvcTestFixture<SimpleWebSite.Startup> fixture)
    {
        Client = fixture.CreateDefaultClient();
    }

    public HttpClient Client { get; }

    [Fact]
    public async Task JsonSerializeFormatted()
    {
        // Arrange
        var expected = "{" + Environment.NewLine
             + "  \"first\": \"wall\"," + Environment.NewLine
             + "  \"second\": \"floor\"" + Environment.NewLine
             + "}";

        // Act
        var content = await Client.GetStringAsync("http://localhost/Home/Index");

        // Assert
        Assert.Equal(expected, content);
    }
}
