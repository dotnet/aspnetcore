// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using System.Net.Http;
using System.Reflection;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Net.Http.Headers;
using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Mvc.FunctionalTests;

public class ViewEngineTests : LoggedTest
{
    private static readonly Assembly _assembly = typeof(ViewEngineTests).GetTypeInfo().Assembly;

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

    public static IEnumerable<object[]> RazorView_ExecutesPageAndLayoutData
    {
        get
        {
            yield return new[] { "ViewWithoutLayout", @"ViewWithoutLayout-Content" };
            yield return new[]
            {
                    "ViewWithLayout",
@"<layout>
ViewWithLayout-Content
</layout>"
                };
            yield return new[]
            {
                    "ViewWithFullPath",
@"<layout>
ViewWithFullPath-content
</layout>"
                };
            yield return new[]
            {
                    "ViewWithNestedLayout",
@"<layout>
<nested-layout>
/ViewEngine/ViewWithNestedLayout
ViewWithNestedLayout-Content
</nested-layout>
</layout>"
                };

            yield return new[]
            {
                    "ViewWithDataFromController",
                    "<h1>hello from controller</h1>"
                };
        }
    }

    [Theory]
    [MemberData(nameof(RazorView_ExecutesPageAndLayoutData))]
    public async Task RazorView_ExecutesPageAndLayout(string actionName, string expected)
    {
        // Arrange & Act
        var response = await Client.GetAsync("http://localhost/ViewEngine/" + actionName);

        // Assert
        await response.AssertStatusCodeAsync(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(expected, body.Trim(), ignoreLineEndingDifferences: true);
    }

    [Fact]
    public async Task RazorView_ExecutesPartialPagesWithCorrectContext()
    {
        // Arrange
        var expected = @"<partial>98052

</partial>
<partial2>98052

</partial2>
test-value";

        // Act
        var body = await Client.GetStringAsync("http://localhost/ViewEngine/ViewWithPartial");

        // Assert
        Assert.Equal(expected, body.Trim(), ignoreLineEndingDifferences: true);
    }

    [Fact]
    public async Task RazorView_DoesNotThrow_PartialViewWithEnumerableModel()
    {
        // Arrange
        var expected = "HelloWorld";

        // Act
        var body = await Client.GetStringAsync(
            "http://localhost/ViewEngine/ViewWithPartialTakingModelFromIEnumerable");

        // Assert
        Assert.Equal(expected, body.Trim());
    }

    [Fact]
    public async Task RazorView_PassesViewContextBetweenViewAndLayout()
    {
        // Arrange
        var expected =
@"<title>Page title</title>
partial-contentcomponent-content";

        // Act
        var body = await Client.GetStringAsync("http://localhost/ViewEngine/ViewPassesViewDataToLayout");

        // Assert
        Assert.Equal(expected, body.Trim(), ignoreLineEndingDifferences: true);
    }

    public static IEnumerable<object[]> RazorViewEngine_UsesAllExpandedPathsToLookForViewsData
    {
        get
        {
            var expected1 = @"expander-index
gb-partial";
            yield return new[] { "en-GB", expected1 };

            var expected2 = @"fr-index
fr-partial";
            yield return new[] { "fr", expected2 };

            if (!TestPlatformHelper.IsMono)
            {
                // https://github.com/aspnet/Mvc/issues/2759
                var expected3 = @"expander-index
expander-partial";
                yield return new[] { "!-invalid-!", expected3 };
            }
        }
    }

    [Theory]
    [MemberData(nameof(RazorViewEngine_UsesAllExpandedPathsToLookForViewsData))]
    public async Task RazorViewEngine_UsesViewExpandersForViewsAndPartials(string value, string expected)
    {
        // Arrange
        var cultureCookie = "c=" + value + "|uic=" + value;
        var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost/TemplateExpander");
        request.Headers.Add("Cookie",
            new CookieHeaderValue(CookieRequestCultureProvider.DefaultCookieName, cultureCookie).ToString());

        // Act
        var response = await Client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.Equal(expected, body.Trim(), ignoreLineEndingDifferences: true);
    }

    public static TheoryData ViewLocationExpanders_GetIsMainPageFromContextData
    {
        get
        {
            return new TheoryData<string, string>
                {
                    {
                        "Index",
                        "<expander-view><shared-views>/Shared-Views/ExpanderViews/_ExpanderPartial.cshtml</shared-views></expander-view>"
                    },
                    {
                        "Partial",
                        "<shared-views>/Shared-Views/ExpanderViews/_ExpanderPartial.cshtml</shared-views>"
                    },
                };
        }
    }

    [Theory]
    [MemberData(nameof(ViewLocationExpanders_GetIsMainPageFromContextData))]
    public async Task ViewLocationExpanders_GetIsMainPageFromContext(string action, string expected)
    {
        // Arrange & Act
        var body = await Client.GetStringAsync($"http://localhost/ExpanderViews/{action}");

        // Assert
        Assert.Equal(expected, body.Trim());
    }

    public static IEnumerable<object[]> RazorViewEngine_RendersPartialViewsData
    {
        get
        {
            yield return new[]
            {
                    "ViewWithoutLayout", "ViewWithoutLayout-Content"
                };
            yield return new[]
            {
                    "PartialViewWithNamePassedIn",
@"<layout>
ViewWithLayout-Content
</layout>"
                };
            yield return new[]
            {
                    "ViewWithFullPath",
@"<layout>
ViewWithFullPath-content
</layout>"
                };
            yield return new[]
            {
                    "ViewWithNestedLayout",
@"<layout>
<nested-layout>
/PartialViewEngine/ViewWithNestedLayout
ViewWithNestedLayout-Content
</nested-layout>
</layout>"
                };
            yield return new[]
            {
                    "PartialWithDataFromController", "<h1>hello from controller</h1>"
                };
            yield return new[]
            {
                    "PartialWithModel",
                    @"my name is judge
<partial>98052
</partial>"
                };
        }
    }

    [Theory]
    [MemberData(nameof(RazorViewEngine_RendersPartialViewsData))]
    public async Task RazorViewEngine_RendersPartialViews(string actionName, string expected)
    {
        // Arrange & Act
        var response = await Client.GetAsync("http://localhost/PartialViewEngine/" + actionName);

        // Assert
        await response.AssertStatusCodeAsync(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Equal(expected, body.Trim(), ignoreLineEndingDifferences: true);
    }

    [Fact]
    public async Task LayoutValueIsPassedBetweenNestedViewStarts()
    {
        // Arrange
        var expected = @"<title>viewstart-value</title>
~/Views/NestedViewStarts/NestedViewStarts/Layout.cshtml
index-content";

        // Act
        var body = await Client.GetStringAsync("http://localhost/NestedViewStarts");

        // Assert
        Assert.Equal(expected, body.Trim(), ignoreLineEndingDifferences: true);
    }

    public static IEnumerable<object[]> RazorViewEngine_UsesExpandersForLayoutsData
    {
        get
        {
            var expected1 =
@"<language-layout>View With Layout
</language-layout>";

            yield return new[] { "en-GB", expected1 };

            if (!TestPlatformHelper.IsMono)
            {
                // https://github.com/aspnet/Mvc/issues/2759
                yield return new[] { "!-invalid-!", expected1 };
            }

            var expected2 =
@"<fr-language-layout>View With Layout
</fr-language-layout>";
            yield return new[] { "fr", expected2 };
        }
    }

    [Theory]
    [MemberData(nameof(RazorViewEngine_UsesExpandersForLayoutsData))]
    public async Task RazorViewEngine_UsesExpandersForLayouts(string value, string expected)
    {
        // Arrange
        var cultureCookie = "c=" + value + "|uic=" + value;
        var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost/TemplateExpander/ViewWithLayout");
        request.Headers.Add("Cookie",
            new CookieHeaderValue(CookieRequestCultureProvider.DefaultCookieName, cultureCookie).ToString());

        // Act
        var response = await Client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.Equal(expected, body.Trim(), ignoreLineEndingDifferences: true);
    }

    [Fact]
    public async Task ViewStartsCanUseDirectivesInjectedFromParentGlobals()
    {
        // Arrange
        var expected =
@"<view-start>Hello Controller-Person</view-start>
<page>Hello Controller-Person</page>";
        var target = "http://localhost/NestedViewImports";

        // Act
        var body = await Client.GetStringAsync(target);

        // Assert
        Assert.Equal(expected, body.Trim(), ignoreLineEndingDifferences: true);
    }

    [Fact]
    public async Task ViewComponentsExecuteLayout()
    {
        // Arrange
        var expected =
@"<title>View With Component With Layout</title>
Page Content
<component-title>ViewComponent With Title</component-title>
<component-body>Component With Layout</component-body>";

        // Act
        var body = await Client.GetStringAsync("http://localhost/ViewEngine/ViewWithComponentThatHasLayout");

        // Assert
        Assert.Equal(expected, body.Trim(), ignoreLineEndingDifferences: true);
    }

    [Fact]
    public async Task RelativePathsWorkAsExpected()
    {
        // Arrange
        var expected =
@"<layout>
<nested-layout>
/ViewEngine/ViewWithRelativePath
ViewWithRelativePath-content
<partial>partial-content</partial>
<component-title>View with relative path title</component-title>
<component-body>Component with Relative Path
<label><strong>Name:</strong> Fred</label>
WriteLiteral says:<strong>Write says:98052WriteLiteral says:</strong></component-body>
</nested-layout>
</layout>";

        // Act
        var body = await Client.GetStringAsync("http://localhost/ViewEngine/ViewWithRelativePath");

        // Assert
        Assert.Equal(expected, body.Trim(), ignoreLineEndingDifferences: true);
    }

    [Fact]
    public async Task ViewComponentsDoNotExecuteViewStarts()
    {
        // Arrange
        var expected = @"<page-content>ViewComponent With ViewStart</page-content>";

        // Act
        var body = await Client.GetStringAsync("http://localhost/ViewEngine/ViewWithComponentThatHasViewStart");

        // Assert
        Assert.Equal(expected, body.Trim());
    }

    [Fact]
    public async Task PartialDoNotExecuteViewStarts()
    {
        // Arrange
        var expected = "Partial that does not specify Layout";

        // Act
        var body = await Client.GetStringAsync("http://localhost/PartialsWithLayout/PartialDoesNotExecuteViewStarts");

        // Assert
        Assert.Equal(expected, body.Trim());
    }

    [Fact]
    public async Task PartialsRenderedViaRenderPartialAsync_CanRenderLayouts()
    {
        // Arrange
        var expected =
@"<layout-for-viewstart-with-layout><layout-for-viewstart-with-layout>Partial that specifies Layout
</layout-for-viewstart-with-layout>Partial that does not specify Layout</layout-for-viewstart-with-layout>";

        // Act
        var body = await Client.GetStringAsync("http://localhost/PartialsWithLayout/PartialsRenderedViaRenderPartial");

        // Assert
        Assert.Equal(expected, body.Trim(), ignoreLineEndingDifferences: true);
    }

    [Fact]
    public async Task PartialsRenderedViaPartialAsync_CanRenderLayouts()
    {
        // Arrange
        var expected =
@"<layout-for-viewstart-with-layout><layout-for-viewstart-with-layout>Partial that specifies Layout
</layout-for-viewstart-with-layout>
Partial that does not specify Layout
</layout-for-viewstart-with-layout>";

        // Act
        var response = await Client.GetAsync("http://localhost/PartialsWithLayout/PartialsRenderedViaPartial");
        await response.AssertStatusCodeAsync(HttpStatusCode.OK);

        var body = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.Equal(expected, body.Trim(), ignoreLineEndingDifferences: true);
    }

    [Fact]
    public async Task RazorView_SetsViewPathAndExecutingPagePath()
    {
        // Arrange
        var outputFile = "compiler/resources/ViewEngineController.ViewWithPaths.txt";
        var expectedContent = await ResourceFile.ReadResourceAsync(_assembly, outputFile, sourceFile: false);

        // Act
        var responseContent = await Client.GetStringAsync("http://localhost/ViewWithPaths");

        // Assert
        responseContent = responseContent.Trim();
        ResourceFile.UpdateOrVerify(_assembly, outputFile, expectedContent, responseContent);
    }

    [Fact]
    public async Task ViewEngine_NormalizesPathsReturnedByViewLocationExpanders()
    {
        // Arrange
        var expected =
@"Layout
Page
Partial";

        // Act
        var responseContent = await Client.GetStringAsync("/BackSlash");

        // Assert
        Assert.Equal(expected, responseContent, ignoreLineEndingDifferences: true);
    }

    [Fact]
    public async Task ViewEngine_DiscoversViewsFromPagesSharedDirectory()
    {
        // Arrange
        var expected = "Hello from Pages/Shared";

        // Act
        var responseContent = await Client.GetStringAsync("/ViewEngine/SearchInPages");

        // Assert
        Assert.Equal(expected, responseContent.Trim());
    }
}
