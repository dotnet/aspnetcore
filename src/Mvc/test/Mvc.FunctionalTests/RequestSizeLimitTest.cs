// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using System.Net.Http;
using System.Text;
using Microsoft.AspNetCore.Hosting;

namespace Microsoft.AspNetCore.Mvc.FunctionalTests;

public class RequestSizeLimitTest : IClassFixture<MvcTestFixture<BasicWebSite.StartupRequestLimitSize>>
{
    public RequestSizeLimitTest(MvcTestFixture<BasicWebSite.StartupRequestLimitSize> fixture)
    {
        var factory = fixture.Factories.FirstOrDefault() ?? fixture.WithWebHostBuilder(ConfigureWebHostBuilder);
        Client = factory.CreateDefaultClient();
    }

    private static void ConfigureWebHostBuilder(IWebHostBuilder builder) =>
        builder.UseStartup<BasicWebSite.StartupRequestLimitSize>();

    public HttpClient Client { get; }

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
