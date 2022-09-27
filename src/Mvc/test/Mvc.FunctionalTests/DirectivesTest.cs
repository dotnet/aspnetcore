// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http;

namespace Microsoft.AspNetCore.Mvc.FunctionalTests;

public class DirectivesTest : IClassFixture<MvcTestFixture<RazorWebSite.Startup>>
{
    public DirectivesTest(MvcTestFixture<RazorWebSite.Startup> fixture)
    {
        Client = fixture.CreateDefaultClient();
    }

    public HttpClient Client { get; }

    [Fact]
    public async Task ViewsInheritsUsingsAndInjectDirectivesFromViewStarts()
    {
        // Arrange
        var expected = "Hello Person1";

        // Act
        var body = await Client.GetStringAsync(
            "http://localhost/Directives/ViewInheritsInjectAndUsingsFromViewImports");

        // Assert
        Assert.Equal(expected, body.Trim());
    }

    [Fact]
    public async Task ViewInheritsBasePageFromViewStarts()
    {
        // Arrange
        var expected = "WriteLiteral says:layout:Write says:Write says:Hello Person2";

        // Act
        var body = await Client.GetStringAsync("http://localhost/Directives/ViewInheritsBasePageFromViewImports");

        // Assert
        Assert.Equal(expected, body.Trim());
    }

    [Fact]
    public async Task ViewAndViewComponentsReplaceTModelTokenFromInheritedBasePages()
    {
        // Arrange
        var expected =
@"WriteLiteral says:<h1>Write says:BobWriteLiteral says:</h1>
Write says:WriteLiteral says:<strong>Write says:98052WriteLiteral says:</strong>";

        // Act
        var body = await Client.GetStringAsync("Directives/ViewReplacesTModelTokenFromInheritedBasePages");

        // Assert
        Assert.Equal(expected, body.Trim(), ignoreLineEndingDifferences: true);
    }
}
