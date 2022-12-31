// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using System.Net.Http;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Json;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Net.Http.Headers;
using System.Net.Http.Headers;

namespace Microsoft.AspNetCore.Diagnostics.FunctionalTests;

public class StatusCodeSampleTest : IClassFixture<TestFixture<StatusCodePagesSample.Startup>>
{
    public StatusCodeSampleTest(TestFixture<StatusCodePagesSample.Startup> fixture)
    {
        Client = fixture.Client;
    }

    public HttpClient Client { get; }

    [Fact]
    public async Task StatusCodePage_ShowsError()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost/errors/417");

        // Act
        var response = await Client.SendAsync(request);

        // Assert
        var body = await response.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Status Code: 417", body);
    }

    [Fact]
    public async Task StatusCodePageOptions_ExcludesSemicolon_WhenReasonPhrase_IsUnknown()
    {
        //Arrange
        var httpStatusCode = 541;
        var request = new HttpRequestMessage(HttpMethod.Get, $"http://localhost/?statuscode={httpStatusCode}");

        //Act
        var response = await Client.SendAsync(request);

        var responseBody = await response.Content.ReadAsStringAsync();

        //Assert
        Assert.Equal((HttpStatusCode)httpStatusCode, response.StatusCode);
        Assert.DoesNotContain(";", responseBody);
    }

    [Fact]
    public async Task StatusCodePageOptions_IncludesSemicolon__AndReasonPhrase_WhenReasonPhrase_IsKnown()
    {
        //Arrange
        var httpStatusCode = 400;
        var request = new HttpRequestMessage(HttpMethod.Get, $"http://localhost/?statuscode={httpStatusCode}");
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/plain"));

        //Act
        var response = await Client.SendAsync(request);

        var statusCodeReasonPhrase = ReasonPhrases.GetReasonPhrase(httpStatusCode);

        var responseBody = await response.Content.ReadAsStringAsync();

        //Assert
        Assert.Equal((HttpStatusCode)httpStatusCode, response.StatusCode);
        Assert.Contains(";", responseBody);
        Assert.Contains(statusCodeReasonPhrase, responseBody);
    }

    [Fact]
    public async Task StatusCodePage_ProducesProblemDetails()
    {
        // Arrange
        var httpStatusCode = 400;
        var request = new HttpRequestMessage(HttpMethod.Get, $"http://localhost?statuscode={httpStatusCode}");
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        // Act
        var response = await Client.SendAsync(request);

        // Assert
        var body = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.NotNull(body);
        Assert.Equal(400, body.Status);
    }

    [Fact]
    public async Task StatusCodePage_ProducesProblemDetails_WithoutAcceptHeader()
    {
        // Arrange
        var httpStatusCode = 400;
        var request = new HttpRequestMessage(HttpMethod.Get, $"http://localhost?statuscode={httpStatusCode}");

        // Act
        var response = await Client.SendAsync(request);

        // Assert
        var body = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.NotNull(body);
        Assert.Equal(400, body.Status);
    }
}
