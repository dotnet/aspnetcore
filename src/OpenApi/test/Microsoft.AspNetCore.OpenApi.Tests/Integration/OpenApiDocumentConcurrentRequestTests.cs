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

        // Act
        await Parallel.ForAsync(0, 150, async (_, ctx) =>
        {
            var response = await client.GetAsync("/openapi/v1.json", ctx);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        });
    }
}
