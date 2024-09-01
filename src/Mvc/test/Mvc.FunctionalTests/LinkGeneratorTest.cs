// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using System.Net.Http;
using System.Reflection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Mvc.FunctionalTests;

// Functional tests for MVC's scenarios with LinkGenerator (2.2+ only)
public class LinkGeneratorTest : LoggedTest
{
    private static void ConfigureWebHostBuilder(IWebHostBuilder builder) =>
        builder.UseStartup<RoutingWebSite.StartupForLinkGenerator>();

    protected override void Initialize(TestContext context, MethodInfo methodInfo, object[] testMethodArguments, ITestOutputHelper testOutputHelper)
    {
        base.Initialize(context, methodInfo, testMethodArguments, testOutputHelper);
        Factory = new MvcTestFixture<RoutingWebSite.StartupForLinkGenerator>(LoggerFactory).WithWebHostBuilder(ConfigureWebHostBuilder)
            .WithWebHostBuilder(ConfigureWebHostBuilder);
        Client = Factory.CreateDefaultClient();
    }

    public override void Dispose()
    {
        Factory.Dispose();
        base.Dispose();
    }

    public WebApplicationFactory<RoutingWebSite.StartupForLinkGenerator> Factory { get; private set; }
    public HttpClient Client { get; private set; }

    [Fact]
    public async Task GetPathByAction_CanGeneratePathToSelf()
    {
        // Act
        var response = await Client.GetAsync("LG1/LinkToSelf");
        var responseContent = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("/LG1/LinkToSelf", responseContent);
    }

    [Fact]
    public async Task GetPathByAction_CanGeneratePathToSelf_PreserveAmbientValues()
    {
        // Act
        var response = await Client.GetAsync("LG1/LinkToSelf/17?another-value=5");
        var responseContent = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("/LG1/LinkToSelf/17?another-value=5", responseContent);
    }

    [Fact]
    public async Task GetPathByAction_CanGeneratePathToAnotherAction_RemovesAmbientValues()
    {
        // Act
        var response = await Client.GetAsync("LG1/LinkToAnotherAction/17?another-value=5");
        var responseContent = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("/LG1/LinkToSelf?another-value=5", responseContent);
    }

    [Fact]
    public async Task GetPathByAction_CanGeneratePathToAnotherController_RemovesAmbientValues()
    {
        // Act
        var response = await Client.GetAsync("LG1/LinkToAnotherController/17?another-value=5");
        var responseContent = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("/LG2/SomeAction?another-value=5", responseContent);
    }

    [Fact]
    public async Task GetPathByAction_CanGeneratePathToAnotherControllerInArea_RemovesAmbientValues()
    {
        // Act
        var response = await Client.GetAsync("LG1/LinkToAnArea/17?another-value=5");
        var responseContent = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("/Admin/LG3/SomeAction?another-value=5", responseContent);
    }

    [Fact]
    public async Task GetPathByAction_CanGeneratePathWithinArea()
    {
        // Act
        var response = await Client.GetAsync("Admin/LG3/LinkInsideOfArea/17");
        var responseContent = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("/Admin/LG3/SomeAction", responseContent);
    }

    // This will fallback to the non-area route because the calling code relies on ambient values, but doesn't pass
    // the HttpContext.
    [Fact]
    public async Task GetPathByAction_FailsToGenerateLinkInsideArea()
    {
        // Act
        var response = await Client.GetAsync("Admin/LG3/LinkInsideOfAreaFail/17?another-value=5");
        var responseContent = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("/LG3/SomeAction", responseContent);
    }

    [Fact]
    public async Task GetPathByAction_CanGeneratePathOutsideOfArea()
    {
        // Act
        var response = await Client.GetAsync("Admin/LG3/LinkOutsideOfArea/17?another-value=5");
        var responseContent = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("/LG1/SomeAction", responseContent);
    }

    [Fact]
    public async Task GetPathByAction_CanGeneratePathFromPath()
    {
        // Act
        var response = await Client.GetAsync("LGAnotherPage/17");
        var responseContent = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("/LG2/SomeAction", responseContent);
    }

    [Fact]
    public async Task GetPathByPage_FromPage_CanGeneratePathWithRelativePageName()
    {
        // Act
        var response = await Client.GetAsync("LGPage/17");
        var responseContent = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("/LGAnotherPage", responseContent);
    }

    [Fact]
    public async Task GetPathByPage_CanGeneratePathToPage()
    {
        // Act
        var response = await Client.GetAsync("LG1/LinkToPage/17?another-value=4");
        var responseContent = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("/LGPage?another-value=4", responseContent);
    }

    [Fact]
    public async Task GetPathByPage_CanGeneratePathToPage_PathTransformed()
    {
        // Act
        var response = await Client.GetAsync("LG1/LinkToPageWithTransformedPath?id=HelloWorld");
        var responseContent = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("/page-route-transformer/test-page/ExtraPath/HelloWorld", responseContent);
    }

    [Fact]
    public async Task GetPathByPage_CanGeneratePathToPageInArea()
    {
        // Act
        var response = await Client.GetAsync("LG1/LinkToPageInArea/17?another-value=4");
        var responseContent = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("/Admin/LGAreaPage?another-value=4&handler=a-handler", responseContent);
    }

    [Fact]
    public async Task GetUriByAction_CanGenerateFullUri()
    {
        // Act
        var response = await Client.GetAsync("LG1/LinkWithFullUri/17");
        var responseContent = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("http://localhost/LG1/LinkWithFullUri/17#hi", responseContent);
    }

    [Fact]
    public async Task GetUriByAction_CanGenerateFullUri_WithoutHttpContext()
    {
        // Act
        var response = await Client.GetAsync("LG1/LinkWithFullUriWithoutHttpContext/17");
        var responseContent = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("https://www.example.com/LG1/LinkWithFullUri#hi", responseContent);
    }

    [Fact]
    public async Task GetUriByPage_CanGenerateFullUri()
    {
        // Act
        var response = await Client.GetAsync("LG1/LinkToPageWithFullUri/17");
        var responseContent = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("http://localhost/LGPage", responseContent);
    }

    [Fact]
    public async Task GetUriByPage_CanGenerateFullUri_WithoutHttpContext()
    {
        // Act
        var response = await Client.GetAsync("LG1/LinkToPageWithFullUriWithoutHttpContext/17");
        var responseContent = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("https://www.example.com/Admin/LGAreaPage?handler=a-handler", responseContent);
    }

    [Fact]
    public async Task GetUriByRouteValues_CanGenerateUriToRouteWithoutMvcParameters()
    {
        // Act
        var response = await Client.GetAsync("LG1/LinkToRouteWithNoMvcParameters?custom=17");
        var responseContent = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("https://www.example.com/routewithnomvcparameters/17", responseContent);
    }
}
