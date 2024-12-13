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

public class RazorPageModelTest : LoggedTest
{
    private static void ConfigureWebHostBuilder(IWebHostBuilder builder) =>
        builder.UseStartup<RazorPagesWebSite.StartupWithoutEndpointRouting>();

    protected override void Initialize(TestContext context, MethodInfo methodInfo, object[] testMethodArguments, ITestOutputHelper testOutputHelper)
    {
        base.Initialize(context, methodInfo, testMethodArguments, testOutputHelper);
        Factory = new MvcTestFixture<RazorPagesWebSite.StartupWithoutEndpointRouting>(LoggerFactory).WithWebHostBuilder(ConfigureWebHostBuilder);
        Client = Factory.CreateDefaultClient();
    }

    public override void Dispose()
    {
        Factory.Dispose();
        base.Dispose();
    }

    public WebApplicationFactory<RazorPagesWebSite.StartupWithoutEndpointRouting> Factory { get; private set; }
    public HttpClient Client { get; private set; }

    [Fact]
    public async Task Page_TryUpdateModelAsync_Success()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Post, "Pages/TryUpdateModel/10")
        {
            Content = new FormUrlEncodedContent(new KeyValuePair<string, string>[]
            {
                    new KeyValuePair<string, string>("Name", "Overriden"),
                    new KeyValuePair<string, string>("Age", "25"),
            })
        };

        await AddAntiforgeryHeaders(request);

        // Act
        var response = await Client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Updated: True", content);
        Assert.Contains("Name = Overriden", content);
    }

    [Fact]
    public async Task Page_TryValidateModel_Success()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Post, "Pages/TryValidateModel/10")
        {
            Content = new FormUrlEncodedContent(new KeyValuePair<string, string>[]
            {
                    new KeyValuePair<string, string>("Name", "Foo"),
                    new KeyValuePair<string, string>("Age", "25"),
            })
        };

        await AddAntiforgeryHeaders(request);

        // Act
        var response = await Client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();

        Assert.Contains("Validation: success", content.Trim());
    }

    [Fact]
    public async Task Page_TryValidateModel_TooLong()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Post, "Pages/TryValidateModel/10")
        {
            Content = new FormUrlEncodedContent(new KeyValuePair<string, string>[]
            {
                    new KeyValuePair<string, string>("Name", "Foo"),
                    new KeyValuePair<string, string>("Age", "200"),
            })
        };

        await AddAntiforgeryHeaders(request);

        // Act
        var response = await Client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();

        Assert.Contains("Validation: fail", content);
        Assert.Contains("The field Age must be between 0 and 99.", content);
    }

    [Fact]
    public async Task PageModel_TryUpdateModelAsync_Success()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Post, "Pages/TryUpdateModelPageModel/10")
        {
            Content = new FormUrlEncodedContent(new KeyValuePair<string, string>[]
            {
                    new KeyValuePair<string, string>("Name", "Overriden"),
                    new KeyValuePair<string, string>("Age", "25"),
            })
        };

        await AddAntiforgeryHeaders(request);

        // Act
        var response = await Client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Updated: True", content);
        Assert.Contains("Name = Overriden", content);
    }

    [Fact]
    public async Task PageModel_TryValidateModel_Success()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Post, "Pages/TryValidateModelPageModel/10")
        {
            Content = new FormUrlEncodedContent(new KeyValuePair<string, string>[]
            {
                    new KeyValuePair<string, string>("Name", "Foo"),
                    new KeyValuePair<string, string>("Age", "25"),
            })
        };

        await AddAntiforgeryHeaders(request);

        // Act
        var response = await Client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();

        Assert.Contains("Validation: success", content.Trim());
    }

    [Fact]
    public async Task PageModel_TryValidateModel_TooLong()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Post, "Pages/TryValidateModelPageModel/10")
        {
            Content = new FormUrlEncodedContent(new KeyValuePair<string, string>[]
            {
                    new KeyValuePair<string, string>("Name", "Foo"),
                    new KeyValuePair<string, string>("Age", "200"),
            })
        };

        await AddAntiforgeryHeaders(request);

        // Act
        var response = await Client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();

        Assert.Contains("Validation: fail", content);
        Assert.Contains("The field Age must be between 0 and 99.", content);
    }

    private async Task AddAntiforgeryHeaders(HttpRequestMessage request)
    {
        var getResponse = await Client.GetAsync(request.RequestUri);
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        var getResponseBody = await getResponse.Content.ReadAsStringAsync();
        var formToken = AntiforgeryTestHelper.RetrieveAntiforgeryToken(getResponseBody, "");
        var cookie = AntiforgeryTestHelper.RetrieveAntiforgeryCookie(getResponse);

        request.Headers.Add("Cookie", cookie.Key + "=" + cookie.Value);
        request.Headers.Add("RequestVerificationToken", formToken);
    }
}
