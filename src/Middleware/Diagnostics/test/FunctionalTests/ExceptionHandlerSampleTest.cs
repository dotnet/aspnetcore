// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using System.Net.Http;

namespace Microsoft.AspNetCore.Diagnostics.FunctionalTests;

public class ExceptionHandlerSampleTest : IClassFixture<TestFixture<ExceptionHandlerSample.Startup>>
{
    public ExceptionHandlerSampleTest(TestFixture<ExceptionHandlerSample.Startup> fixture)
    {
        Client = fixture.Client;
    }

    public HttpClient Client { get; }

    [Fact]
    public async Task ExceptionHandlerPage_ShowsError()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost/throw");

        // Act
        var response = await Client.SendAsync(request);

        // Assert
        var body = await response.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Contains("we encountered an un-expected issue with your application.", body);
    }
}
