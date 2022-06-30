// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using System.Net.Http;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Json;

namespace Microsoft.AspNetCore.Diagnostics.FunctionalTests;

public class ProblemDetailsExceptionHandlerSampleTest : IClassFixture<TestFixture<ExceptionHandlerSample.StartupWithProblemDetails>>
{
    public ProblemDetailsExceptionHandlerSampleTest(TestFixture<ExceptionHandlerSample.StartupWithProblemDetails> fixture)
    {
        Client = fixture.Client;
    }

    public HttpClient Client { get; }

    [Fact]
    public async Task ExceptionHandlerPage_ProducesProblemDetails()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost/throw");

        // Act
        var response = await Client.SendAsync(request);

        // Assert
        var body = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.NotNull(body);
        Assert.Equal(500, body.Status);
    }
}
