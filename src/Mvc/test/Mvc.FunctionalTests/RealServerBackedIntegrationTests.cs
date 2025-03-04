// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using System.Net.Http.Headers;

namespace Microsoft.AspNetCore.Mvc.FunctionalTests;

public class RealServerBackedIntegrationTests : IClassFixture<KestrelBasedWapFactory>
{
    public KestrelBasedWapFactory Factory { get; }

    public RealServerBackedIntegrationTests(KestrelBasedWapFactory factory)
    {
        Factory = factory;
    }

    [Fact]
    public async Task RetrievesDataFromRealServer()
    {
        // Arrange
        var expectedMediaType = MediaTypeHeaderValue.Parse("application/json; charset=utf-8");

        // Act
        var client = Factory.CreateClient();
        var response = await client.GetAsync("/");
        var responseContent = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(expectedMediaType, response.Content.Headers.ContentType);

        Assert.Equal(5000, client.BaseAddress.Port);

        Assert.Contains("first", responseContent);
        Assert.Contains("second", responseContent);
        Assert.Contains("wall", responseContent);
        Assert.Contains("floor", responseContent);
    }
}
