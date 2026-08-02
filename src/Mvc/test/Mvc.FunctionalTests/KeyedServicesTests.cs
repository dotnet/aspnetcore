// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http;
using System.Reflection;
using Microsoft.AspNetCore.InternalTesting;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Mvc.FunctionalTests;

public class KeyedServicesTests : LoggedTest
{
    protected override void Initialize(TestContext context, MethodInfo methodInfo, object[] testMethodArguments, ITestOutputHelper testOutputHelper)
    {
        base.Initialize(context, methodInfo, testMethodArguments, testOutputHelper);
        Factory = new MvcTestFixture<BasicWebSite.StartupWithoutEndpointRouting>(LoggerFactory);
        Client = Factory.CreateDefaultClient();
    }

    public override void Dispose()
    {
        Factory.Dispose();
        base.Dispose();
    }

    public MvcTestFixture<BasicWebSite.StartupWithoutEndpointRouting> Factory { get; private set; }
    public HttpClient Client { get; private set; }

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
    }

    [Fact]
    public async Task ExplicitSingleFromKeyedServiceAttributeWithNullKey()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Get, "/services/GetKeyNull");

        // Act
        var response = await Client.SendAsync(request);

        // Assert
        Assert.True(response.IsSuccessStatusCode);
        Assert.Equal("DEFAULT", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task ExplicitSingleFromKeyedServiceAttributeOptionalNotRegistered()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Get, "/services/GetOptionalNotRegistered");

        // Act
        var response = await Client.SendAsync(request);

        // Assert
        Assert.True(response.IsSuccessStatusCode);
        Assert.Equal(string.Empty, await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task ExplicitSingleFromKeyedServiceAttributeRequiredNotRegistered()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Get, "/services/GetRequiredNotRegistered");

        // Act
        var response = await Client.SendAsync(request);

        // Assert
        Assert.False(response.IsSuccessStatusCode);
    }
}
