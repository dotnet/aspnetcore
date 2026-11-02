// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using System.Net.Http;

namespace Microsoft.AspNetCore.OpenApi.Tests.Integration;

public class OpenApiDocumentConcurrentRequestTests(SampleAppFixture fixture) : IClassFixture<SampleAppFixture>
{
    [Fact]
    public async Task MapOpenApi_HandlesConcurrentRequests()
    {
        // Arrange
        var client = fixture.CreateClient();
        var requests = new List<Task<HttpResponseMessage>>
        {
            client.GetAsync("/openapi/v1.json"),
            client.GetAsync("/openapi/v1.json"),
            client.GetAsync("/openapi/v1.json")
        };

        // Act
        var results = await Task.WhenAll(requests);

        // Assert
        foreach (var result in results)
        {
            Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        }
    }
}
