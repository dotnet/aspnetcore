// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Reflection;
using System.Text.Json;
using BasicWebSite.Models;
using Microsoft.AspNetCore.InternalTesting;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Mvc.FunctionalTests;

public class BasicTests : LoggedTest
{
    // Some tests require comparing the actual response body against an expected response baseline
    // so they require a reference to the assembly on which the resources are located, in order to
    // make the tests less verbose, we get a reference to the assembly with the resources and we
    // use it on all the rest of the tests.
    private static readonly Assembly _resourcesAssembly = typeof(BasicTests).GetTypeInfo().Assembly;

    protected override void Initialize(TestContext context, MethodInfo methodInfo, object[] testMethodArguments, ITestOutputHelper testOutputHelper)
    {
        base.Initialize(context, methodInfo, testMethodArguments, testOutputHelper);
        Factory = new MvcTestFixture<BasicWebSite.StartupWithoutEndpointRouting>(LoggerFactory);
        Client = Factory.CreateDefaultClient();
    }

    public override void Dispose()
    {
        Factory.Dispose();
        base.Dispose();
    }

    public MvcTestFixture<BasicWebSite.StartupWithoutEndpointRouting> Factory { get; private set; }
    public HttpClient Client { get; private set; }

    [Fact]
    public async Task CanRender_CSharp7Views()
    {
        // Arrange
        var expectedMediaType = MediaTypeHeaderValue.Parse("text/html; charset=utf-8");
        var outputFile = "compiler/resources/BasicWebSite.Home.CSharp7View.html";
        var expectedContent =
            await ResourceFile.ReadResourceAsync(_resourcesAssembly, outputFile, sourceFile: false);

        // Act
        var response = await Client.GetAsync("Home/CSharp7View");
        var responseContent = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(expectedMediaType, response.Content.Headers.ContentType);

        ResourceFile.UpdateOrVerify(_resourcesAssembly, outputFile, expectedContent, responseContent);
    }

    [Fact]
    public async Task CanRender_ViewComponentWithArgumentsFromController()
    {
        // Arrange
        var expectedMediaType = MediaTypeHeaderValue.Parse("text/html; charset=utf-8");
        var outputFile = "compiler/resources/BasicWebSite.PassThrough.Index.html";
        var expectedContent =
            await ResourceFile.ReadResourceAsync(_resourcesAssembly, outputFile, sourceFile: false);

        // Act
        var response = await Client.GetAsync("PassThrough/Index?value=123");
        var responseContent = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(expectedMediaType, response.Content.Headers.ContentType);

        ResourceFile.UpdateOrVerify(_resourcesAssembly, outputFile, expectedContent, responseContent);
    }

    [Theory]
    [InlineData("")]
    [InlineData("Home")]
    [InlineData("Home/Index")]
    public async Task CanRender_ViewsWithLayout(string url)
    {
        // Arrange
        var expectedMediaType = MediaTypeHeaderValue.Parse("text/html; charset=utf-8");
        var outputFile = "compiler/resources/BasicWebSite.Home.Index.html";
        var expectedContent =
            await ResourceFile.ReadResourceAsync(_resourcesAssembly, outputFile, sourceFile: false);

        // Act
        // The host is not important as everything runs in memory and tests are isolated from each other.
        var response = await Client.GetAsync(url);
        var responseContent = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(expectedMediaType, response.Content.Headers.ContentType);

        ResourceFile.UpdateOrVerify(_resourcesAssembly, outputFile, expectedContent, responseContent);
    }

    [Fact]
    public async Task CanRender_SimpleViews()
    {
        // Arrange
        var expectedMediaType = MediaTypeHeaderValue.Parse("text/html; charset=utf-8");
        var outputFile = "compiler/resources/BasicWebSite.Home.PlainView.html";
        var expectedContent =
            await ResourceFile.ReadResourceAsync(_resourcesAssembly, outputFile, sourceFile: false);

        // Act
        var response = await Client.GetAsync("http://localhost/Home/PlainView");
        var responseContent = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(expectedMediaType, response.Content.Headers.ContentType);

        ResourceFile.UpdateOrVerify(_resourcesAssembly, outputFile, expectedContent, responseContent);
    }

    [Fact]
    public async Task ViewWithAttributePrefix_RendersWithoutIgnoringPrefix()
    {
        // Arrange
        var outputFile = "compiler/resources/BasicWebSite.Home.ViewWithPrefixedAttributeValue.html";
        var expectedContent =
            await ResourceFile.ReadResourceAsync(_resourcesAssembly, outputFile, sourceFile: false);

        // Act
        var response = await Client.GetAsync("Home/ViewWithPrefixedAttributeValue");
        var responseContent = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        ResourceFile.UpdateOrVerify(_resourcesAssembly, outputFile, expectedContent, responseContent);
    }

    [Fact]
    public async Task CanReturn_ResultsWithoutContent()
    {
        // Act
        var response = await Client.GetAsync("Home/NoContentResult");
        var responseContent = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Null(response.Content.Headers.ContentType);
        Assert.Equal(0, response.Content.Headers.ContentLength);
        Assert.Equal(0, responseContent.Length);
    }

    [Fact]
    public async Task ReturningTaskFromAction_ProducesEmptyResult()
    {
        // Act
        var response = await Client.GetAsync("Home/ActionReturningTask");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Hello, World!", Assert.Single(response.Headers.GetValues("Message")));
        Assert.Empty(await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task ActionDescriptors_CreatedOncePerRequest()
    {
        // Arrange
        var expectedContent = "1";

        // Act and Assert
        for (var i = 0; i < 3; i++)
        {
            var result = await Client.GetAsync("Monitor/CountActionDescriptorInvocations");
            Assert.Equal(HttpStatusCode.OK, result.StatusCode);
            var responseContent = await result.Content.ReadAsStringAsync();

            Assert.Equal(expectedContent, responseContent);
        }
    }

    [Fact]
    public async Task ActionWithRequireHttps_RedirectsToSecureUrl_ForNonHttpsGetRequests()
    {
        // Act
        var response = await Client.GetAsync("Home/HttpsOnlyAction");

        // Assert
        Assert.Equal(HttpStatusCode.Found, response.StatusCode);
        Assert.NotNull(response.Headers.Location);
        Assert.Equal("https://localhost/Home/HttpsOnlyAction", response.Headers.Location.ToString());
        Assert.Equal(0, response.Content.Headers.ContentLength);

        var responseBytes = await response.Content.ReadAsByteArrayAsync();
        Assert.Empty(responseBytes);
    }

    [Fact]
    public async Task ActionWithRequireHttps_ReturnsBadRequestResponse_ForNonHttpsNonGetRequests()
    {
        // Act
        var response = await Client.SendAsync(new HttpRequestMessage(
            HttpMethod.Post,
            "http://localhost/Home/HttpsOnlyAction"));

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal(0, response.Content.Headers.ContentLength);

        var responseBytes = await response.Content.ReadAsByteArrayAsync();
        Assert.Empty(responseBytes);
    }

    [Theory]
    [InlineData("GET")]
    [InlineData("POST")]
    public async Task ActionWithRequireHttps_AllowsHttpsRequests(string method)
    {
        // Act
        var response = await Client.SendAsync(new HttpRequestMessage(
            new HttpMethod(method),
            "https://localhost/Home/HttpsOnlyAction"));

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task JsonHelper_RendersJson_WithCamelCaseNames()
    {
        // Arrange
        var expectedBody =
@"<script type=""text/javascript"">
    var json = {""id"":9000,""fullName"":""John \u003cb\u003eSmith\u003c/b\u003e""};
</script>";

        // Act
        var response = await Client.GetAsync("Home/JsonHelperInView");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/html", response.Content.Headers.ContentType.MediaType);

        var actualBody = await response.Content.ReadAsStringAsync();
        Assert.Equal(expectedBody, actualBody, ignoreLineEndingDifferences: true);
    }

    [Fact]
    public async Task JsonHelperWithSettings_RendersJson_WithNamesUnchanged()
    {
        // Arrange
        var json = "{\"id\":9000,\"FullName\":\"John \\u003cb\\u003eSmith\\u003c/b\\u003e\"}";
        var expectedBody = string.Format(
            CultureInfo.InvariantCulture,
            @"<script type=""text/javascript"">
    var json = {0};
</script>",
            json);

        // Act
        var response = await Client.GetAsync("Home/JsonHelperWithSettingsInView?snakeCase=false");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/html", response.Content.Headers.ContentType.MediaType);

        var actualBody = await response.Content.ReadAsStringAsync();
        Assert.Equal(expectedBody, actualBody.Trim(), ignoreLineEndingDifferences: true);
    }

    [Fact]
    public async Task JsonHelperWithSettings_RendersJson_WithSnakeCaseNames()
    {
        // Arrange
        var json = "{\"id\":9000,\"full_name\":\"John \\u003cb\\u003eSmith\\u003c/b\\u003e\"}";
        var expectedBody = string.Format(
            CultureInfo.InvariantCulture,
            @"<script type=""text/javascript"">
    var json = {0};
</script>",
            json);

        // Act
        var response = await Client.GetAsync("Home/JsonHelperWithSettingsInView?snakeCase=true");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/html", response.Content.Headers.ContentType.MediaType);

        var actualBody = await response.Content.ReadAsStringAsync();
        Assert.Equal(expectedBody, actualBody.Trim(), ignoreLineEndingDifferences: true);
    }

    public static IEnumerable<object[]> HtmlHelperLinkGenerationData
    {
        get
        {
            yield return new[] {
                    "ActionLink_ActionOnSameController",
                    @"<a href=""/Links/Details"">linktext</a>" };
            yield return new[] {
                    "ActionLink_ActionOnOtherController",
                    @"<a href=""/Products/Details?print=true"">linktext</a>"
                };
            yield return new[] {
                    "ActionLink_SecurePage_ImplicitHostName",
                    @"<a href=""https://localhost/Products/Details?print=true"">linktext</a>"
                };
            yield return new[] {
                    "ActionLink_HostNameFragmentAttributes",
                    // note: attributes are alphabetically ordered
                    @"<a href=""https://www.contoso.com:9000/Products/Details?print=true#details"" p1=""p1-value"">linktext</a>"
                };
            yield return new[] {
                    "RouteLink_RestLinkToOtherController",
                    @"<a href=""/api/orders/10"">linktext</a>"
                };
            yield return new[] {
                    "RouteLink_SecureApi_ImplicitHostName",
                    @"<a href=""https://localhost/api/orders/10"">linktext</a>"
                };
            yield return new[] {
                    "RouteLink_HostNameFragmentAttributes",
                    @"<a href=""https://www.contoso.com:9000/api/orders/10?print=True#details"" p1=""p1-value"">linktext</a>"
                };
        }
    }

    [Theory]
    [MemberData(nameof(HtmlHelperLinkGenerationData))]
    public async Task HtmlHelperLinkGeneration(string viewName, string expectedLink)
    {
        // Act
        var response = await Client.GetAsync("Links/Index?view=" + viewName);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var responseData = await response.Content.ReadAsStringAsync();
        Assert.Contains(expectedLink, responseData, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ConfigureMvc_AddsOptionsProperly()
    {
        // Act
        var response = await Client.GetAsync("Home/GetApplicationDescription");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var responseData = await response.Content.ReadAsStringAsync();
        Assert.Equal("This is a basic website.", responseData);
    }

    [Fact]
    public async Task TypesMarkedAsNonAction_AreInaccessible()
    {
        // Act
        var response = await Client.GetAsync("SqlData/TruncateAllDbRecords");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UsingPageRouteParameterInConventionalRouteWorks()
    {
        // Arrange
        var expected = "ConventionalRoute - Hello from mypage";

        // Act
        var response = await Client.GetStringAsync("/PageRoute/ConventionalRouteView/mypage");

        // Assert
        Assert.Equal(expected, response.Trim());
    }

    [Fact]
    public async Task UsingPageRouteParameterInAttributeRouteWorks()
    {
        // Arrange
        var expected = "AttributeRoute - Hello from test-page";

        // Act
        var response = await Client.GetStringAsync("/PageRoute/AttributeView/test-page");

        // Assert
        Assert.Equal(expected, response.Trim());
    }

    [Fact]
    public async Task RedirectToAction_WithEmptyActionName_UsesAmbientValue()
    {
        // Arrange
        var product = new Dictionary<string, string>
            {
                { "SampleInt", "20" }
            };

        // Act
        var response = await Client.PostAsync("/Home/Product", new FormUrlEncodedContent(product));

        // Assert
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.NotNull(response.Headers.Location);
        Assert.Equal("/Home/Product", response.Headers.Location.ToString());

        var responseBody = await Client.GetStringAsync("/Home/Product");
        Assert.Equal("Get Product", responseBody);
    }
    [Fact]
    public async Task ActionMethod_ReturningActionMethodOfT_WithBadRequest()
    {
        // Arrange
        var url = "ActionResultOfT/GetProduct";

        // Act
        var response = await Client.GetAsync(url);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ActionMethod_ReturningActionMethodOfT()
    {
        // Arrange
        var url = "ActionResultOfT/GetProduct?productId=10";

        // Act
        var response = await Client.GetStringAsync(url);

        // Assert
        var result = JsonSerializer.Deserialize<Product>(response, TestJsonSerializerOptionsProvider.Options);
        Assert.Equal(10, result.SampleInt);
    }

    [Fact]
    public async Task ActionMethod_ReturningSequenceOfObjectsWrappedInActionResultOfT()
    {
        // Arrange
        var url = "ActionResultOfT/GetProducts";

        // Act
        var response = await Client.GetStringAsync(url);

        // Assert
        var result = JsonSerializer.Deserialize<Product[]>(response, TestJsonSerializerOptionsProvider.Options);
        Assert.Equal(2, result.Length);
    }

    [Fact]
    public async Task TestingInfrastructure_InvokesCreateDefaultBuilder()
    {
        // Act
        var response = await Client.GetStringAsync("Testing/Builder");

        // Assert
        Assert.Equal("true", response);
    }

    [Fact]
    public async Task ApplicationAssemblyPartIsListedAsFirstAssembly()
    {
        // Act
        var response = await Client.GetStringAsync("Home/GetAssemblyPartData");
        var assemblyParts = JsonSerializer.Deserialize<IList<string>>(response, TestJsonSerializerOptionsProvider.Options);
        var expected = new[]
        {
                "BasicWebSite",
                "Microsoft.AspNetCore.Components.Server",
                "Microsoft.AspNetCore.SpaServices",
                "Microsoft.AspNetCore.SpaServices.Extensions",
                "Microsoft.AspNetCore.Mvc.TagHelpers",
                "Microsoft.AspNetCore.Mvc.Razor",
            };

        // Assert
        //
        // We don't keep track the explicit list of assemblies that show up here
        // because this can change as we work on the product. All we care about is
        // that BasicWebSite is first, and that everything after it is a Microsoft.
        Assert.True(assemblyParts.Count > 2);
        Assert.Equal("BasicWebSite", assemblyParts[0]);
        for (var i = 1; i < assemblyParts.Count; i++)
        {
            Assert.StartsWith("Microsoft.", assemblyParts[i]);
        }
    }

    [Fact]
    public async Task ViewDataProperties_AreTransferredToViews()
    {
        // Act
        var document = await Client.GetHtmlDocumentAsync("ViewDataProperty/ViewDataPropertyToView");

        // Assert
        var message = document.QuerySelector("#message").TextContent;
        Assert.Equal("Message set in action", message);

        var filterMessage = document.QuerySelector("#filter-message").TextContent;
        Assert.Equal("Value set in OnActionExecuting", filterMessage);

        var title = document.QuerySelector("title").TextContent;
        Assert.Equal("View Data Property Sample", title);
    }

    [Fact]
    public async Task ViewDataProperties_AreTransferredToViewComponents()
    {
        // Act
        var document = await Client.GetHtmlDocumentAsync("ViewDataProperty/ViewDataPropertyToViewComponent");

        // Assert
        var message = document.QuerySelector("#message").TextContent;
        Assert.Equal("Message set in action", message);

        var title = document.QuerySelector("title").TextContent;
        Assert.Equal("View Data Property Sample", title);
    }

    [Fact]
    public async Task BindPropertiesAttribute_CanBeAppliedToControllers()
    {
        // Arrange
        var formContent = new Dictionary<string, string>
            {
                { "Name", "TestName" },
                { "Id", "10" },
            };

        // Act
        var response = await Client.PostAsync("BindProperties/Action", new FormUrlEncodedContent(formContent));

        // Assert
        await response.AssertStatusCodeAsync(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        var data = JsonSerializer.Deserialize<BindPropertyControllerData>(content, TestJsonSerializerOptionsProvider.Options);

        Assert.Equal("TestName", data.Name);
        Assert.Equal(10, data.Id);
    }

    [Fact]
    public async Task BindPropertiesAttribute_DoesNotApplyToPropertiesWithBindingInfo()
    {
        // Arrange
        var formContent = new Dictionary<string, string>
            {
                { "Id", "10" },
                { "FromRoute", "12" },
                { "CustomBound", "Test" },
            };

        // Act
        var response = await Client.PostAsync("BindProperties/Action", new FormUrlEncodedContent(formContent));

        // Assert
        await response.AssertStatusCodeAsync(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        var data = JsonSerializer.Deserialize<BindPropertyControllerData>(content, TestJsonSerializerOptionsProvider.Options);

        Assert.Equal(10, data.Id);
        Assert.Null(data.IdFromRoute);
        Assert.Equal("CustomBoundValue", data.CustomBound);
    }

    [Fact]
    public async Task BindPropertiesAttribute_DoesNotCausePropertiesWithBindNeverAttributeToBeModelBound()
    {
        // Arrange
        var formContent = new Dictionary<string, string>
            {
                { "BindNeverProperty", "Hello world" },
            };

        // Act
        var response = await Client.PostAsync("BindProperties/Action", new FormUrlEncodedContent(formContent));

        // Assert
        await response.AssertStatusCodeAsync(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        var data = JsonSerializer.Deserialize<BindPropertyControllerData>(content, TestJsonSerializerOptionsProvider.Options);

        Assert.Null(data.BindNeverProperty);
    }

    [Fact]
    public async Task BindPropertiesAttributeWithSupportsGet_BindsOnNonGet()
    {
        // Arrange
        var formContent = new Dictionary<string, string>
            {
                {  "Name", "TestName" },
            };

        // Act
        var response = await Client.PostAsync("BindPropertiesSupportsGet/Action", new FormUrlEncodedContent(formContent));

        // Assert
        await response.AssertStatusCodeAsync(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Equal("TestName", content);
    }

    [Fact]
    public async Task BindPropertiesAttributeWithSupportsGet_BindsOnGet()
    {
        // Act
        var response = await Client.GetAsync("BindPropertiesSupportsGet/Action?Name=OnGetTestName");

        // Assert
        await response.AssertStatusCodeAsync(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Equal("OnGetTestName", content);
    }

    [Fact]
    public async Task BindPropertiesAppliesValidation()
    {
        // Act
        var response = await Client.GetAsync("BindPropertiesWithValidation/Action?Password=Test&ConfirmPassword=different");

        // Assert
        await response.AssertStatusCodeAsync(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();

        Assert.Collection(
            problem.Errors.OrderBy(e => e.Key),
            kvp =>
            {
                Assert.Equal("ConfirmPassword", kvp.Key);
                Assert.Equal("Password and confirm password do not match.", Assert.Single(kvp.Value));
            },
            kvp =>
            {
                Assert.Equal("UserName", kvp.Key);
                Assert.Equal("User name is required.", Assert.Single(kvp.Value));
            });
    }

    [Fact]
    public async Task InvalidForm_ResultsInModelError()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Post, "/Home/Product");
        request.Content = new MultipartFormDataContent();

        var response = await Client.SendAsync(request);

        await response.AssertStatusCodeAsync(HttpStatusCode.BadRequest);
        var content = await response.Content.ReadAsStringAsync();
        var problemDetails = JsonSerializer.Deserialize<ValidationProblemDetails>(content, TestJsonSerializerOptionsProvider.Options);
        Assert.Collection(
            problemDetails.Errors,
            kvp =>
            {
                Assert.Empty(kvp.Key);
                Assert.Equal("Failed to read the request form. Form section has invalid Content-Disposition value: ", string.Join(" ", kvp.Value));
            });
    }

    public class BindPropertyControllerData
    {
        public string Name { get; set; }

        public int? Id { get; set; }

        public int? IdFromRoute { get; set; }

        public string CustomBound { get; set; }

        public string BindNeverProperty { get; set; }
    }
}
