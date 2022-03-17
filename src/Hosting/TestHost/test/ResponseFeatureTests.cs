// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.TestHost;

public class ResponseFeatureTests
{
    [Fact]
    public async Task StatusCode_DefaultsTo200()
    {
        // Arrange & Act
        var responseInformation = CreateResponseFeature();

        // Assert
        Assert.Equal(200, responseInformation.StatusCode);
        Assert.False(responseInformation.HasStarted);

        await responseInformation.FireOnSendingHeadersAsync();

        Assert.True(responseInformation.HasStarted);
        Assert.True(responseInformation.Headers.IsReadOnly);
    }

    [Fact]
    public async Task StartAsync_StartsResponse()
    {
        // Arrange & Act
        var responseInformation = CreateResponseFeature();

        // Assert
        Assert.Equal(200, responseInformation.StatusCode);
        Assert.False(responseInformation.HasStarted);

        await responseInformation.StartAsync();

        Assert.True(responseInformation.HasStarted);
        Assert.True(responseInformation.Headers.IsReadOnly);
    }

    [Fact]
    public void OnStarting_ThrowsWhenHasStarted()
    {
        // Arrange
        var responseInformation = CreateResponseFeature();
        responseInformation.HasStarted = true;

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() =>
        {
            responseInformation.OnStarting((status) =>
            {
                return Task.FromResult(string.Empty);
            }, state: "string");
        });
    }

    [Fact]
    public void StatusCode_ThrowsWhenHasStarted()
    {
        var responseInformation = CreateResponseFeature();
        responseInformation.HasStarted = true;

        Assert.Throws<InvalidOperationException>(() => responseInformation.StatusCode = 400);
        Assert.Throws<InvalidOperationException>(() => responseInformation.ReasonPhrase = "Hello World");
    }

    [Fact]
    public void StatusCode_MustBeGreaterThan99()
    {
        var responseInformation = CreateResponseFeature();

        Assert.Throws<ArgumentOutOfRangeException>(() => responseInformation.StatusCode = 99);
        Assert.Throws<ArgumentOutOfRangeException>(() => responseInformation.StatusCode = 0);
        Assert.Throws<ArgumentOutOfRangeException>(() => responseInformation.StatusCode = -200);
        responseInformation.StatusCode = 100;
        responseInformation.StatusCode = 200;
        responseInformation.StatusCode = 1000;
    }

    private ResponseFeature CreateResponseFeature()
    {
        return new ResponseFeature(ex => { });
    }
}
