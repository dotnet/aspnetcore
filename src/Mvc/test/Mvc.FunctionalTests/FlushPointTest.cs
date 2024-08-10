// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http;
using System.Reflection;
using Microsoft.AspNetCore.InternalTesting;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Mvc.FunctionalTests;

public class FlushPointTest : LoggedTest
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
    public async Task FlushPointsAreExecutedForPagesWithLayouts()
    {
        var expected = @"<title>Page With Layout</title>

RenderBody content


    <span>Content that takes time to produce</span>

";

        // Act
        var body = await Client.GetStringAsync("http://localhost/FlushPoint/PageWithLayout");

        // Assert
        Assert.Equal(expected, body, ignoreLineEndingDifferences: true);
    }

    [Fact]
    public async Task FlushFollowedByLargeContent()
    {
        // Arrange
        var expected = new string('a', 1024 * 1024);

        // Act
        var document = await Client.GetHtmlDocumentAsync("http://localhost/FlushPoint/FlushFollowedByLargeContent");

        // Assert
        var largeContent = document.RequiredQuerySelector("#large-content");
        Assert.StartsWith(expected, largeContent.TextContent);
    }

    [Fact]
    public async Task FlushInvokedInComponent()
    {
        var expected = new string('a', 1024 * 1024);

        // Act
        var document = await Client.GetHtmlDocumentAsync("http://localhost/FlushPoint/FlushInvokedInComponent");

        // Assert
        var largeContent = document.RequiredQuerySelector("#large-content");
        Assert.StartsWith(expected, largeContent.TextContent);
    }

    [Fact]
    public async Task FlushPointsAreExecutedForPagesWithoutLayouts()
    {
        var expected = @"Initial content

Secondary content

Inside partial

After flush inside partial<form action=""/FlushPoint/PageWithoutLayout"" method=""post"">" +
            @"<input id=""Name1"" name=""Name1"" type=""text"" value="""" />" +
            @"<input id=""Name2"" name=""Name2"" type=""text"" value="""" /></form>";

        // Act
        var body = await Client.GetStringAsync("http://localhost/FlushPoint/PageWithoutLayout");

        // Assert
        Assert.Equal(expected, body, ignoreLineEndingDifferences: true);
    }

    [Theory]
    [InlineData("PageWithPartialsAndViewComponents", "FlushAsync invoked inside RenderSection")]
    [InlineData("PageWithRenderSection", "FlushAsync invoked inside RenderSectionAsync")]
    public async Task FlushPointsAreExecutedForPagesWithComponentsPartialsAndSections(string action, string title)
    {
        var expected = $@"<title>{ title }</title>
RenderBody content


partial-content

Value from TaskReturningString
<p>section-content</p>
    component-content
    <span>Content that takes time to produce</span>

More content from layout
";

        // Act
        var body = await Client.GetStringAsync("http://localhost/FlushPoint/" + action);

        // Assert
        Assert.Equal(expected, body, ignoreLineEndingDifferences: true);
    }

    [Fact]
    public async Task FlushPointsNestedLayout()
    {
        // Arrange
        var expected = @"Inside Nested Layout
<title>Nested Page With Layout</title>



    <span>Nested content that takes time to produce</span>
";

        // Act
        var body = await Client.GetStringAsync("http://localhost/FlushPoint/PageWithNestedLayout");

        // Assert
        Assert.Equal(expected, body, ignoreLineEndingDifferences: true);
    }
}
