// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http;

namespace Microsoft.AspNetCore.Mvc.FunctionalTests;

public class KeyedServicesTests : IClassFixture<MvcTestFixture<BasicWebSite.StartupWithoutEndpointRouting>>
{
    public KeyedServicesTests(MvcTestFixture<BasicWebSite.StartupWithoutEndpointRouting> fixture)
    {
        Client = fixture.CreateDefaultClient();
    }

    public HttpClient Client { get; }

    [Fact]
    public async Task ExplicitSingleFromKeyedServiceAttribute()
    {
        // Arrange
        var okRequest = new HttpRequestMessage(HttpMethod.Get, "/services/GetOk");
        var notokRequest = new HttpRequestMessage(HttpMethod.Get, "/services/GetNotOk");

        // Act
        var okResponse = await Client.SendAsync(okRequest);
        var notokResponse = await Client.SendAsync(notokRequest);

        // Assert
        Assert.True(okResponse.IsSuccessStatusCode);
        Assert.True(notokResponse.IsSuccessStatusCode);
        Assert.Equal("OK", await okResponse.Content.ReadAsStringAsync());
        Assert.Equal("NOT OK", await notokResponse.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task ExplicitMultipleFromKeyedServiceAttribute()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Get, "/services/GetBoth");

        // Act
        var response = await Client.SendAsync(request);

        // Assert
        Assert.True(response.IsSuccessStatusCode);
        Assert.Equal("OK,NOT OK", await response.Content.ReadAsStringAsync());

        var response2 = await Client.SendAsync(new HttpRequestMessage(HttpMethod.Get, "/services/GetBoth"));
        Assert.True(response2.IsSuccessStatusCode);
    }
}
