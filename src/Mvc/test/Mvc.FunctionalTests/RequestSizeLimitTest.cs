// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Mvc.FunctionalTests;

public class RequestSizeLimitTest : LoggedTest
{
    private static void ConfigureWebHostBuilder(IWebHostBuilder builder) =>
        builder.UseStartup<BasicWebSite.StartupRequestLimitSize>();

    protected override void Initialize(TestContext context, MethodInfo methodInfo, object[] testMethodArguments, ITestOutputHelper testOutputHelper)
    {
        base.Initialize(context, methodInfo, testMethodArguments, testOutputHelper);
        Factory = new MvcTestFixture<BasicWebSite.StartupRequestLimitSize>(LoggerFactory).WithWebHostBuilder(ConfigureWebHostBuilder);
        Client = Factory.CreateDefaultClient();
    }

    public override void Dispose()
    {
        Factory.Dispose();
        base.Dispose();
    }

    public WebApplicationFactory<BasicWebSite.StartupRequestLimitSize> Factory { get; private set; }
    public HttpClient Client { get; private set; }

    [Fact]
    public async Task RequestSizeLimitCheckHappens_BeforeAntiforgeryTokenValidation()
    {
        // Arrange
        var request = new HttpRequestMessage();
        var kvps = new List<KeyValuePair<string, string>>();
        kvps.Add(new KeyValuePair<string, string>("SampleString", new string('p', 1024)));
        kvps.Add(new KeyValuePair<string, string>("RequestVerificationToken", "invalid-data"));

        // Act
        var response = await Client.PostAsync(
            "RequestSizeLimit/RequestSizeLimitCheckBeforeAntiforgeryValidation",
            new FormUrlEncodedContent(kvps));

        // Assert
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        var result = await response.Content.ReadAsStringAsync();
        Assert.Contains(
            "InvalidOperationException: Request content size is greater than the limit size",
            result);
    }

    [Fact]
    public async Task AntiforgeryTokenValidationHappens_AfterRequestSizeLimitCheck()
    {
        // Arrange
        var request = new HttpRequestMessage();
        var kvps = new List<KeyValuePair<string, string>>();
        kvps.Add(new KeyValuePair<string, string>("SampleString", "string"));
        kvps.Add(new KeyValuePair<string, string>("RequestVerificationToken", "invalid-data"));

        // Act
        var response = await Client.PostAsync(
            "RequestSizeLimit/RequestSizeLimitCheckBeforeAntiforgeryValidation",
            new FormUrlEncodedContent(kvps));

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task DisableRequestSizeLimitOnAction_OverridesControllerLevelSettings()
    {
        // Arrange
        var expected = $"{{\"sampleInt\":10,\"sampleString\":\"{new string('p', 1024)}\"}}";
        var request = new HttpRequestMessage();
        request.Method = HttpMethod.Post;
        request.Content = new StringContent(expected, Encoding.UTF8, "text/json");
        request.RequestUri = new Uri("http://localhost/RequestSizeLimit/DisableRequestSizeLimit");

        // Act
        var response = await Client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var actual = await response.Content.ReadAsStringAsync();
        Assert.Equal(expected, actual);
    }
}
