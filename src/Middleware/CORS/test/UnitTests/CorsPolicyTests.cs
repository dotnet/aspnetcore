// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Cors.Infrastructure;

public class CorsPolicyTest
{
    [Fact]
    public void Default_Constructor()
    {
        // Arrange & Act
        var corsPolicy = new CorsPolicy();

        // Assert
        Assert.False(corsPolicy.AllowAnyHeader);
        Assert.False(corsPolicy.AllowAnyMethod);
        Assert.False(corsPolicy.AllowAnyOrigin);
        Assert.False(corsPolicy.SupportsCredentials);
        Assert.Empty(corsPolicy.ExposedHeaders);
        Assert.Empty(corsPolicy.Headers);
        Assert.Empty(corsPolicy.Methods);
        Assert.Empty(corsPolicy.Origins);
        Assert.Null(corsPolicy.PreflightMaxAge);
        Assert.NotNull(corsPolicy.IsOriginAllowed);
        Assert.True(corsPolicy.IsDefaultIsOriginAllowed);
    }

    [Fact]
    public void IsDefaultIsOriginAllowed_IsFalseAfterSettingIsOriginAllowed()
    {
        // Arrange
        var policy = new CorsPolicy();

        // Act
        policy.IsOriginAllowed = origin => true;

        // Assert
        Assert.False(policy.IsDefaultIsOriginAllowed);
    }

    [Fact]
    public void SettingNegativePreflightMaxAge_Throws()
    {
        // Arrange
        var policy = new CorsPolicy();

        // Act
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
        {
            policy.PreflightMaxAge = TimeSpan.FromSeconds(-12);
        });

        // Assert
        Assert.Equal(
            $"PreflightMaxAge must be greater than or equal to 0. (Parameter 'value')",
            exception.Message);
    }

    [Fact]
    public void ToString_ReturnsThePropertyValues()
    {
        // Arrange
        var corsPolicy = new CorsPolicy
        {
            PreflightMaxAge = TimeSpan.FromSeconds(12),
            SupportsCredentials = true
        };
        corsPolicy.Headers.Add("foo");
        corsPolicy.Headers.Add("bar");
        corsPolicy.Origins.Add("http://example.com");
        corsPolicy.Origins.Add("http://example.org");
        corsPolicy.Methods.Add("GET");

        // Act
        var policyString = corsPolicy.ToString();

        // Assert
        Assert.Equal(
            @"AllowAnyHeader: False, AllowAnyMethod: False, AllowAnyOrigin: False, PreflightMaxAge: 12," +
            " SupportsCredentials: True, Origins: {http://example.com,http://example.org}, Methods: {GET}," +
            " Headers: {foo,bar}, ExposedHeaders: {}",
            policyString);
    }
}
