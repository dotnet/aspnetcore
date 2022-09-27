// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Json;

namespace Microsoft.AspNetCore.Diagnostics.FunctionalTests;

public class DeveloperExceptionPageSampleTest : IClassFixture<TestFixture<DeveloperExceptionPageSample.Startup>>
{
    public DeveloperExceptionPageSampleTest(TestFixture<DeveloperExceptionPageSample.Startup> fixture)
    {
        Client = fixture.Client;
    }

    public HttpClient Client { get; }

    [Fact]
    public async Task DeveloperExceptionPage_ShowsError()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost/");

        // Act
        var response = await Client.SendAsync(request);

        // Assert
        var body = await response.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Contains("Exception: Demonstration exception.", body);
    }

    [Fact]
    public async Task DeveloperExceptionPage_ShowsProblemDetails_WhenHtmlNotAccepted()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost/");
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        // Act
        var response = await Client.SendAsync(request);

        // Assert
        var body = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.NotNull(body);
        Assert.Equal(500, body.Status);
        Assert.Contains("Demonstration exception", body.Detail);
    }
}
