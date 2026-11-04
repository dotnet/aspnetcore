// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Reflection;
using Microsoft.AspNetCore.InternalTesting;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Mvc.FunctionalTests;

public class RazorPagesWithBasePathTest : LoggedTest
{
    protected override void Initialize(TestContext context, MethodInfo methodInfo, object[] testMethodArguments, ITestOutputHelper testOutputHelper)
    {
        base.Initialize(context, methodInfo, testMethodArguments, testOutputHelper);
        Factory = new MvcTestFixture<RazorPagesWebSite.StartupWithBasePath>(LoggerFactory);
        Client = Factory.CreateDefaultClient();
    }

    public override void Dispose()
    {
        Factory.Dispose();
        base.Dispose();
    }

    public MvcTestFixture<RazorPagesWebSite.StartupWithBasePath> Factory { get; private set; }
    public HttpClient Client { get; private set; }

    [Fact]
    public async Task PageOutsideBasePath_IsNotRouteable()
    {
        // Act
        var response = await Client.GetAsync("/HelloWorld");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task IndexAtBasePath_IsRouteableAtRoot()
    {
        // Act
        var response = await Client.GetAsync("/");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        Assert.Equal("Hello from /Index", content.Trim());
    }

    [Fact]
    public async Task IndexAtBasePath_IsRouteableViaIndex()
    {
        // Act
        var response = await Client.GetAsync("/Index");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        Assert.Equal("Hello from /Index", content.Trim());
    }

    [Fact]
    public async Task IndexInSubdirectory_IsRouteableViaDirectoryName()
    {
        // Act
        var response = await Client.GetAsync("/Admin/Index");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        Assert.Equal("Hello from /Admin/Index", content.Trim());
    }

    [Fact]
    public async Task PageWithRouteTemplateInSubdirectory_IsRouteable()
    {
        // Act
        var response = await Client.GetAsync("/Admin/RouteTemplate/1/MyRouteSuffix/");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        Assert.Equal("Hello from /Admin/RouteTemplate 1", content.Trim());
    }

    [Fact]
    public async Task PageWithRouteTemplateInSubdirectory_IsRouteable_WithOptionalParameters()
    {
        // Act
        var response = await Client.GetAsync("/Admin/RouteTemplate/my-user-id/MyRouteSuffix/4");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        Assert.Equal("Hello from /Admin/RouteTemplate my-user-id 4", content.Trim());
    }

    [Fact]
    public async Task AuthConvention_IsAppliedOnBasePathRelativePaths_ForFiles()
    {
        // Act
        var response = await Client.GetAsync("/Conventions/Auth");

        // Assert
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/Login?ReturnUrl=%2FConventions%2FAuth", response.Headers.Location.PathAndQuery);
    }

    [Fact]
    public async Task AuthConvention_IsAppliedOnBasePathRelativePaths_For_Folders()
    {
        // Act
        var response = await Client.GetAsync("/Conventions/AuthFolder");

        // Assert
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/Login?ReturnUrl=%2FConventions%2FAuthFolder", response.Headers.Location.PathAndQuery);
    }

    [Fact]
    public async Task AuthConvention_AppliedToFolders_CanByOverridenByFiltersOnModel()
    {
        // Act
        var response = await Client.GetStringAsync("/Conventions/AuthFolder/AnonymousViaModel");

        // Assert
        Assert.Equal("Hello from Anonymous", response.Trim());
    }

    [Fact]
    public async Task ViewStart_IsDiscoveredWhenRootDirectoryIsSpecified()
    {
        // Test for https://github.com/aspnet/Mvc/issues/5915
        //Arrange
        var expected = @"Hello from _ViewStart
Hello from /Pages/WithViewStart/Index.cshtml!";

        // Act
        var response = await Client.GetStringAsync("/WithViewStart");

        // Assert
        Assert.Equal(expected, response, ignoreLineEndingDifferences: true);
    }

    [Fact]
    public async Task ViewStart_IsDiscoveredForFilesOutsidePageRoot()
    {
        //Arrange
        var expected = @"Hello from _ViewStart at root
Hello from _ViewStart
Hello from page";

        // Act
        var response = await Client.GetStringAsync("/WithViewStart/ViewStartAtRoot");

        // Assert
        Assert.Equal(expected, response.Trim(), ignoreLineEndingDifferences: true);
    }

    [Fact]
    public async Task ViewImport_IsDiscoveredWhenRootDirectoryIsSpecified()
    {
        // Test for https://github.com/aspnet/Mvc/issues/5915
        //Arrange
        var expected = "Hello from CustomService!";

        // Act
        var response = await Client.GetStringAsync("/WithViewImport");

        // Assert
        Assert.Equal(expected, response.Trim());
    }

    [Fact]
    public async Task FormTagHelper_WithPage_GeneratesLinksToSelf()
    {
        //Arrange
        var expected = "<form method=\"POST\" action=\"/TagHelper/SelfPost/10\">";

        // Act
        var response = await Client.GetStringAsync("/TagHelper/SelfPost");

        // Assert
        Assert.Contains(expected, response.Trim());
    }

    [Fact]
    public async Task FormTagHelper_WithPageHandler_AllowsPostingToSelf()
    {
        //Arrange
        var expected =
@"<form action=""/TagHelper/PostWithHandler/Edit"" method=""post""><input name=""__RequestVerificationToken"" type=""hidden"" value=""{0}"" /></form>
<form method=""post"" action=""/TagHelper/PostWithHandler/Edit""><input name=""__RequestVerificationToken"" type=""hidden"" value=""{0}"" /></form>
<form method=""post"" action=""/TagHelper/PostWithHandler/Edit/10""></form>";

        // Act
        var response = await Client.GetStringAsync("/TagHelper/PostWithHandler");

        // Assert
        var responseContent = response.Trim();
        var forgeryToken = AntiforgeryTestHelper.RetrieveAntiforgeryToken(responseContent, "/TagHelper/PostWithHandler");
        var expectedContent = string.Format(CultureInfo.InvariantCulture, expected, forgeryToken);

        Assert.Equal(expectedContent, responseContent, ignoreLineEndingDifferences: true);
    }

    [Fact]
    public async Task FormTagHelper_WithPage_AllowsPostingToAnotherPage()
    {
        //Arrange
        var expected =
@"<form method=""POST"" action=""/TagHelper/SelfPost/10""></form>
<form method=""POST"" action=""/TagHelper/PostWithHandler/Delete/10""></form>";

        // Act
        var response = await Client.GetStringAsync("/TagHelper/CrossPost");

        // Assert
        Assert.Equal(expected, response.Trim(), ignoreLineEndingDifferences: true);
    }

    [Fact]
    public async Task FormActionTagHelper_WithPage_AllowsPostingToAnotherPage()
    {
        //Arrange
        var expected =
@"<button formaction=""/TagHelper/CrossPost/10"" />
<input type=""submit"" formaction=""/TagHelper/CrossPost/10"" />
<input type=""image"" formaction=""/TagHelper/CrossPost/10"" />
<button formaction=""/TagHelper/PostWithHandler/Edit/11"" />
<input type=""submit"" formaction=""/TagHelper/PostWithHandler/Edit/11"" />
<input type=""image"" formaction=""/TagHelper/PostWithHandler/Delete/11"" />";

        // Act
        var response = await Client.GetStringAsync("/TagHelper/FormAction");

        // Assert
        Assert.Equal(expected, response, ignoreLineEndingDifferences: true);
    }

    [Fact]
    public async Task RedirectFromPage_RedirectsToPathWithoutIndexSegment()
    {
        //Arrange
        var expected = "/Redirects";

        // Act
        var response = await Client.GetAsync("/Redirects/Index");

        // Assert
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal(expected, response.Headers.Location.ToString());
    }

    [Fact]
    public async Task RedirectFromPage_ToIndex_RedirectsToPathWithoutIndexSegment()
    {
        //Arrange
        var expected = "/Redirects";

        // Act
        var response = await Client.GetAsync("/Redirects/RedirectToIndex");

        // Assert
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal(expected, response.Headers.Location.ToString());
    }

    [Fact]
    public async Task PageRoute_UsingDefaultPageNameToRoute()
    {
        // Arrange
        var expected = @"<a href=""/Routes/Sibling/10"">Link</a>";

        // Act
        var response = await Client.GetStringAsync("/Routes/RouteUsingDefaultName");

        // Assert
        Assert.Equal(expected, response.Trim());
    }

    [Fact]
    public async Task Pages_ReturnsFromPagesSharedDirectory()
    {
        // Arrange
        var expected = "Hello from /Pages/Shared/";

        // Act
        var response = await Client.GetStringAsync("/SearchInPages");

        // Assert
        Assert.Equal(expected, response.Trim());
    }

    [Fact]
    public async Task PagesInAreas_Work()
    {
        // Arrange
        var expected = "Hello from a page in Accounts area";

        // Act
        var response = await Client.GetStringAsync("/Accounts/About");

        // Assert
        Assert.Equal(expected, response.Trim());
    }

    [Fact]
    public async Task PagesInAreas_CanHaveRouteTemplates()
    {
        // Arrange
        var expected = "The id is 42";

        // Act
        var response = await Client.GetStringAsync("/Accounts/PageWithRouteTemplate/42");

        // Assert
        Assert.Equal(expected, response.Trim());
    }

    [Fact]
    public async Task PagesInAreas_CanGenerateLinksToControllersAndPages()
    {
        // Arrange
        var expected =
@"<a href=""/Accounts/Manage/RenderPartials"">Link inside area</a>
<a href=""/Products/List/old/20"">Link to external area</a>
<a href=""/Accounts"">Link to area action</a>
<a href=""/Admin"">Link to non-area page</a>";

        // Act
        var response = await Client.GetStringAsync("/Accounts/PageWithLinks");

        // Assert
        Assert.Equal(expected, response.Trim(), ignoreLineEndingDifferences: true);
    }

    [Fact]
    public async Task PagesInAreas_CanGenerateRelativeLinks()
    {
        // Arrange
        var expected =
@"<a href=""/Accounts/PageWithRouteTemplate/1"">Parent directory</a>
<a href=""/Accounts/Manage/RenderPartials"">Sibling directory</a>
<a href=""/Products/List"">Go back to root of different area</a>";

        // Act
        var response = await Client.GetStringAsync("/Accounts/RelativeLinks");

        // Assert
        Assert.Equal(expected, response.Trim(), ignoreLineEndingDifferences: true);
    }

    [Fact]
    public async Task PagesInAreas_CanDiscoverViewsFromAreaAndSharedDirectories()
    {
        // Arrange
        var expected =
@"Layout in /Views/Shared
Partial in /Areas/Accounts/Pages/Manage/

Partial in /Areas/Accounts/Pages/

Partial in /Areas/Accounts/Pages/

Partial in /Areas/Accounts/Views/Shared/

Hello from /Pages/Shared/";

        // Act
        var response = await Client.GetStringAsync("/Accounts/Manage/RenderPartials");

        // Assert
        Assert.Equal(expected, response.Trim(), ignoreLineEndingDifferences: true);
    }

    [Fact]
    public async Task AuthorizeFolderConvention_CanBeAppliedToAreaPages()
    {
        // Act
        var response = await Client.GetAsync("/Accounts/RequiresAuth");

        // Assert
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/Login?ReturnUrl=%2FAccounts%2FRequiresAuth", response.Headers.Location.PathAndQuery);
    }

    [Fact]
    public async Task AllowAnonymousToPageConvention_CanBeAppliedToAreaPages()
    {
        // Act
        var response = await Client.GetStringAsync("/Accounts/RequiresAuth/AllowAnonymous");

        // Assert
        Assert.Equal("Hello from AllowAnonymous", response.Trim());
    }

    // These test is important as it covers a feature that allows razor pages to use a different
    // model at runtime that wasn't known at compile time. Like a non-generic model used at compile
    // time and overriden at runtime with a closed-generic model that performs the actual implementation.
    // An example of this is how the Identity UI library defines a base page model in their views,
    // like how the Register.cshtml view defines its model as RegisterModel and then, at runtime it replaces
    // that model with RegisterModel<TUser> where TUser is the type of the user used to configure identity.
    [Fact]
    public async Task PageConventions_CanBeUsedToCustomizeTheModelType()
    {
        // Act
        var response = await Client.GetAsync("/CustomModelTypeModel");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("<h2>User</h2>", content);
    }

    [Fact]
    public async Task PageConventions_CustomizedModelCanPostToHandlers()
    {
        // Arrange
        var getPage = await Client.GetAsync("/CustomModelTypeModel");
        var token = AntiforgeryTestHelper.RetrieveAntiforgeryToken(await getPage.Content.ReadAsStringAsync(), "");
        var cookie = AntiforgeryTestHelper.RetrieveAntiforgeryCookie(getPage);

        var message = new HttpRequestMessage(HttpMethod.Post, "/CustomModelTypeModel")
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = token,
                ["ConfirmPassword"] = "",
                ["Password"] = "",
                ["Email"] = ""
            })
        };
        message.Headers.TryAddWithoutValidation("Cookie", $"{cookie.Key}={cookie.Value}");

        // Act
        var response = await Client.SendAsync(message);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("is required.", content);
    }

    [Fact]
    public async Task PageConventions_CustomizedModelCanWorkWithModelState()
    {
        // Arrange
        var getPage = await Client.GetAsync("/CustomModelTypeModel?Attempts=0");
        var token = AntiforgeryTestHelper.RetrieveAntiforgeryToken(await getPage.Content.ReadAsStringAsync(), "");
        var cookie = AntiforgeryTestHelper.RetrieveAntiforgeryCookie(getPage);

        var message = new HttpRequestMessage(HttpMethod.Post, "/CustomModelTypeModel?Attempts=3")
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = token,
                ["Email"] = "javi@example.com",
                ["Password"] = "[PLACEHOLDER]-1a",
                ["ConfirmPassword"] = "[PLACEHOLDER]-1a",
            })
        };
        message.Headers.TryAddWithoutValidation("Cookie", $"{cookie.Key}={cookie.Value}");

        // Act
        var response = await Client.SendAsync(message);

        // Assert
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/", response.Headers.Location.ToString());
    }

    [Fact]
    public async Task PageConventions_CustomizedModelCanWorkWithModelState_EnforcesBindRequired()
    {
        // Arrange
        var getPage = await Client.GetAsync("/CustomModelTypeModel?Attempts=0");
        var token = AntiforgeryTestHelper.RetrieveAntiforgeryToken(await getPage.Content.ReadAsStringAsync(), "");
        var cookie = AntiforgeryTestHelper.RetrieveAntiforgeryCookie(getPage);

        var message = new HttpRequestMessage(HttpMethod.Post, "/CustomModelTypeModel")
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = token,
                ["Email"] = "javi@example.com",
                ["Password"] = "[PLACEHOLDER]-1a",
                ["ConfirmPassword"] = "[PLACEHOLDER]-1a",
            })
        };
        message.Headers.TryAddWithoutValidation("Cookie", $"{cookie.Key}={cookie.Value}");

        // Act
        var response = await Client.SendAsync(message);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var responseText = await response.Content.ReadAsStringAsync();
        Assert.Contains(
            "A value for the &#x27;Attempts&#x27; parameter or property was not provided.",
            responseText);
    }

    [Fact]
    public async Task ValidationAttributes_OnTopLevelProperties()
    {
        // Act
        var response = await Client.GetStringAsync("/Validation/PageWithValidation?age=71");

        // Assert
        Assert.Contains("Name is required", response);
        Assert.Contains("18 &#x2264; Age &#x2264; 60", response);
    }

    [Fact]
    public async Task CompareValidationAttributes_OnTopLevelProperties()
    {
        // Act
        var response = await Client.GetStringAsync("/Validation/PageWithCompareValidation?password=[PlaceHolder]-1a&comparePassword=[PlaceHolder]-1b");

        // Assert
        Assert.Contains("User name is required", response);
        Assert.Contains("Password and confirm password do not match.", response);
    }

    [Fact]
    public async Task ValidationAttributes_OnHandlerParameters()
    {
        // Act
        var response = await Client.GetStringAsync("/Validation/PageHandlerWithValidation");

        // Assert
        Assert.Contains("Name is required", response);
    }

    [Fact]
    public async Task PagesFromClassLibraries_CanBeServed()
    {
        // Act
        var response = await Client.GetStringAsync("/ClassLibraryPages/Served");

        // Assert
        Assert.Contains("This page is served from RazorPagesClassLibrary", response);
    }

    [Fact]
    public async Task PagesFromClassLibraries_CanBeOverriden()
    {
        // Act
        var response = await Client.GetStringAsync("/ClassLibraryPages/Overriden");

        // Assert
        Assert.Contains("This page is overriden by RazorPagesWebSite", response);
    }

    [Fact]
    public async Task ViewDataAttributes_SetInPageModel_AreTransferredToLayout()
    {
        // Arrange
        var document = await Client.GetHtmlDocumentAsync("/ViewData/ViewDataInPage");

        // Assert
        var description = document.QuerySelector("meta[name='description']").Attributes["content"];
        Assert.Equal("Description set in handler", description.Value);

        var keywords = document.QuerySelector("meta[name='keywords']").Attributes["content"];
        Assert.Equal("Value set in filter", keywords.Value);

        var author = document.QuerySelector("meta[name='author']").Attributes["content"];
        Assert.Equal("Property with key", author.Value);

        var title = document.QuerySelector("title").TextContent;
        Assert.Equal("Title with default value", title);
    }

    [Fact]
    public async Task ViewDataAttributes_SetInPageWithoutModel_AreTransferredToLayout()
    {
        // Arrange
        var document = await Client.GetHtmlDocumentAsync("/ViewData/ViewDataInPageWithoutModel");

        // Assert
        var description = document.QuerySelector("meta[name='description']").Attributes["content"];
        Assert.Equal("Description set in page handler", description.Value);

        var title = document.QuerySelector("title").TextContent;
        Assert.Equal("Default value", title);
    }

    [Fact]
    public async Task ViewDataProperties_SetInPageModel_AreTransferredToViewComponents()
    {
        // Act
        var document = await Client.GetHtmlDocumentAsync("ViewData/ViewDataToViewComponentPage");

        // Assert
        var message = document.QuerySelector("#message").TextContent;
        Assert.Equal("Message set in handler", message);

        var title = document.QuerySelector("title").TextContent;
        Assert.Equal("View Data in Pages", title);
    }

    [Fact]
    public async Task Antiforgery_RequestWithoutAntiforgeryToken_Returns200ForHeadRequests()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Head, "/Antiforgery/AntiforgeryDefault");

        // Act
        var response = await Client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Antiforgery_RequestWithoutAntiforgeryToken_Returns400BadRequest()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Post, "/Antiforgery/AntiforgeryDefault");

        // Act
        var response = await Client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Antiforgery_RequestWithAntiforgeryToken_Succeeds()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Post, "/Antiforgery/AntiforgeryDefault");
        await AddAntiforgeryHeadersAsync(request);

        // Act
        var response = await Client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Antiforgery_IgnoreAntiforgeryTokenAppliedToModelWorks()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Post, "/Antiforgery/IgnoreAntiforgery");
        await AddAntiforgeryHeadersAsync(request);

        // Act
        var response = await Client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ViewDataSetInViewStart_IsAvailableToPage()
    {
        // Arrange & Act
        var document = await Client.GetHtmlDocumentAsync("/ViewData/ViewDataSetInViewStart");

        // Assert
        var valueSetInViewStart = document.RequiredQuerySelector("#valuefromviewstart").TextContent;
        var valueSetInPageModel = document.RequiredQuerySelector("#valuefrompagemodel").TextContent;
        var valueSetInPage = document.RequiredQuerySelector("#valuefrompage").TextContent;

        Assert.Equal("Value from _ViewStart", valueSetInViewStart);
        Assert.Equal("Value from Page Model", valueSetInPageModel);
        Assert.Equal("Value from Page", valueSetInPage);
    }

    [Fact]
    public async Task RoundTrippingFormFileInputWorks()
    {
        // Arrange
        var url = "/PropertyBinding/BindFormFile";
        var response = await Client.GetAsync(url);
        await response.AssertStatusCodeAsync(HttpStatusCode.OK);

        var document = await response.GetHtmlDocumentAsync();

        var property1 = document.RequiredQuerySelector("#property1").GetAttribute("name");
        var file1 = document.RequiredQuerySelector("#file1").GetAttribute("name");
        var file2 = document.RequiredQuerySelector("#file2").GetAttribute("name");
        var file3 = document.RequiredQuerySelector("#file3").GetAttribute("name");
        var antiforgeryToken = document.RetrieveAntiforgeryToken();

        var cookie = AntiforgeryTestHelper.RetrieveAntiforgeryCookie(response);

        var content = new MultipartFormDataContent
            {
                { new StringContent("property1-value"), property1 },
                { new StringContent("test-value1"), file1, "test1.txt" },
                { new StringContent("test-value2"), file3, "test2.txt" }
            };

        var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = content,
        };
        request.Headers.Add("Cookie", cookie.Key + "=" + cookie.Value);
        request.Headers.Add("RequestVerificationToken", antiforgeryToken);

        response = await Client.SendAsync(request);

        await response.AssertStatusCodeAsync(HttpStatusCode.OK);
    }

    [Fact]
    public async Task AuthAttribute_AppliedOnPageWorks()
    {
        // Act
        using var response = await Client.GetAsync("/Filters/AuthFilterOnPage");

        // Assert
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/Login?ReturnUrl=%2FFilters%2FAuthFilterOnPage", response.Headers.Location.PathAndQuery);
    }

    [Fact]
    public async Task AuthAttribute_AppliedOnPageWithModelWorks()
    {
        // Act
        using var response = await Client.GetAsync("/Filters/AuthFilterOnPageWithModel");

        // Assert
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/Login?ReturnUrl=%2FFilters%2FAuthFilterOnPageWithModel", response.Headers.Location.PathAndQuery);
    }

    [Fact]
    public async Task FiltersAppliedToPageAndPageModelAreExecuted()
    {
        // Act
        using var response = await Client.GetAsync("/Filters/FiltersAppliedToPageAndPageModel");

        // Assert
        await response.AssertStatusCodeAsync(HttpStatusCode.OK);
        Assert.Equal(new[] { "PageModelFilterValue" }, response.Headers.GetValues("PageModelFilterKey"));
        Assert.Equal(new[] { "PageFilterValue" }, response.Headers.GetValues("PageFilterKey"));
    }

    private async Task AddAntiforgeryHeadersAsync(HttpRequestMessage request)
    {
        var response = await Client.GetAsync(request.RequestUri);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var responseBody = await response.Content.ReadAsStringAsync();
        var formToken = AntiforgeryTestHelper.RetrieveAntiforgeryToken(responseBody);
        var cookie = AntiforgeryTestHelper.RetrieveAntiforgeryCookie(response);

        request.Headers.Add("Cookie", cookie.Key + "=" + cookie.Value);
        request.Headers.Add("RequestVerificationToken", formToken);
    }
}
