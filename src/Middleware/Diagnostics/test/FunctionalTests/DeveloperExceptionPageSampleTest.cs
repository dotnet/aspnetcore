// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Json;
using System.Text.Json;

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

        var exceptionNode = (JsonElement)body.Extensions["exception"];
        Assert.Contains("System.Exception: Demonstration exception.", exceptionNode.GetProperty("details").GetString());
        Assert.Equal("application/json", exceptionNode.GetProperty("headers").GetProperty("Accept")[0].GetString());
        Assert.Equal("localhost", exceptionNode.GetProperty("headers").GetProperty("Host")[0].GetString());
        Assert.Equal("/", exceptionNode.GetProperty("path").GetString());
        Assert.Equal("Endpoint display name", exceptionNode.GetProperty("endpoint").GetString());
        Assert.Equal("Value1", exceptionNode.GetProperty("routeValues").GetProperty("routeValue1").GetString());
        Assert.Equal("Value2", exceptionNode.GetProperty("routeValues").GetProperty("routeValue2").GetString());
    }
}
