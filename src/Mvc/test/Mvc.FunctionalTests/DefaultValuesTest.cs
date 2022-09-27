// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http;

namespace Microsoft.AspNetCore.Mvc.FunctionalTests;

public class DefaultValuesTest : IClassFixture<MvcTestFixture<BasicWebSite.StartupWithoutEndpointRouting>>
{
    public DefaultValuesTest(MvcTestFixture<BasicWebSite.StartupWithoutEndpointRouting> fixture)
    {
        Client = fixture.CreateDefaultClient();
    }

    public HttpClient Client { get; }

    [Fact]
    public async Task Controller_WithDefaultValueAttribute_ReturnsDefault()
    {
        // Arrange
        var expected = "hello";
        var url = "http://localhost/DefaultValues/EchoValue_DefaultValueAttribute";

        // Act
        var response = await Client.GetStringAsync(url);

        // Assert
        Assert.Equal(expected, response);
    }

    [Fact]
    public async Task Controller_WithDefaultValueAttribute_ReturnsModelBoundValues()
    {
        // Arrange
        var expected = "cool";
        var url = "http://localhost/DefaultValues/EchoValue_DefaultValueAttribute?input=cool";

        // Act
        var response = await Client.GetStringAsync(url);

        // Assert
        Assert.Equal(expected, response);
    }

    [Fact]
    public async Task Controller_WithDefaultParameterValue_ReturnsDefault()
    {
        // Arrange
        var expected = "world";
        var url = "http://localhost/DefaultValues/EchoValue_DefaultParameterValue";

        // Act
        var response = await Client.GetStringAsync(url);

        // Assert
        Assert.Equal(expected, response);
    }

    [Fact]
    public async Task Controller_WithDefaultParameterValue_ReturnsModelBoundValues()
    {
        // Arrange
        var expected = "cool";
        var url = "http://localhost/DefaultValues/EchoValue_DefaultParameterValue?input=cool";

        // Act
        var response = await Client.GetStringAsync(url);

        // Assert
        Assert.Equal(expected, response);
    }

    [Fact]
    public async Task Controller_WithDefaultParameterValues_ForStructs_ReturnsDefaults()
    {
        // Arrange
        var expected = $"{default(Guid)}, {default(TimeSpan)}";
        var url = "http://localhost/DefaultValues/EchoValue_DefaultParameterValue_ForStructs";

        // Act
        var response = await Client.GetStringAsync(url);

        // Assert
        Assert.Equal(expected, response);
    }

    [Fact]
    public async Task Controller_WithDefaultParameterValues_ForStructs_ReturnsBoundValues()
    {
        // Arrange
        Guid guid = Guid.NewGuid();
        TimeSpan timeSpan = new TimeSpan(10, 10, 10);
        var expected = $"{guid}, {timeSpan}";
        var url = $"http://localhost/DefaultValues/EchoValue_DefaultParameterValue_ForStructs?guid={guid}&timespan={timeSpan}";

        // Act
        var response = await Client.GetStringAsync(url);

        // Assert
        Assert.Equal(expected, response);
    }

    [Fact]
    public async Task EchoValue_DefaultParameterValue_ForGlobbedPath()
    {
        // Arrange
        var expected = $"index.html";
        var url = "http://localhost/DefaultValues/EchoValue_DefaultParameterValue_ForGlobbedPath";

        // Act
        var response = await Client.GetStringAsync(url);

        // Assert
        Assert.Equal(expected, response);
    }
}
