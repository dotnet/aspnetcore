// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http;
using System.Reflection;
using Microsoft.AspNetCore.InternalTesting;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Mvc.FunctionalTests;

public class DirectivesTest : LoggedTest
{
    protected override void Initialize(TestContext context, MethodInfo methodInfo, object[] testMethodArguments, ITestOutputHelper testOutputHelper)
    {
        base.Initialize(context, methodInfo, testMethodArguments, testOutputHelper);
        Factory = new MvcTestFixture<RazorWebSite.Startup>(LoggerFactory);
        Client = Factory.CreateDefaultClient();
    }

    public override void Dispose()
    {
        Factory.Dispose();
        base.Dispose();
    }

    public MvcTestFixture<RazorWebSite.Startup> Factory { get; private set; }
    public HttpClient Client { get; private set; }

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
