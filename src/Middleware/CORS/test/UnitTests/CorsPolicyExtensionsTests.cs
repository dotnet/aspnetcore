// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Cors.Infrastructure;

public sealed class CorsPolicyExtensionsTest
{
    [Fact]
    public void IsOriginAnAllowedSubdomain_ReturnsTrueIfPolicyContainsOrigin()
    {
        // Arrange
        const string origin = "http://sub.domain";
        var policy = new CorsPolicy();
        policy.Origins.Add(origin);

        // Act
        var actual = policy.IsOriginAnAllowedSubdomain(origin);

        // Assert
        Assert.True(actual);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("null")]
    [InlineData("http://")]
    [InlineData("http://*")]
    [InlineData("http://.domain")]
    [InlineData("http://.domain/hello")]
    public void IsOriginAnAllowedSubdomain_ReturnsFalseIfOriginIsMalformedUri(string malformedOrigin)
    {
        // Arrange
        var policy = new CorsPolicy();
        policy.Origins.Add("http://*.domain");

        // Act
        var actual = policy.IsOriginAnAllowedSubdomain(malformedOrigin);

        // Assert
        Assert.False(actual);
    }

    [Theory]
    [InlineData("http://sub.domain", "http://*.domain")]
    [InlineData("http://sub.sub.domain", "http://*.domain")]
    [InlineData("http://sub.sub.domain", "http://*.sub.domain")]
    [InlineData("http://sub.domain:4567", "http://*.domain:4567")]
    public void IsOriginAnAllowedSubdomain_ReturnsTrue_WhenASubdomain(string origin, string allowedOrigin)
    {
        // Arrange
        var policy = new CorsPolicy();
        policy.Origins.Add(allowedOrigin);

        // Act
        var isAllowed = policy.IsOriginAnAllowedSubdomain(origin);

        // Assert
        Assert.True(isAllowed);
    }

    [Theory]
    [InlineData("http://domain", "http://*.domain")]
    [InlineData("http://sub.domain", "http://domain")]
    [InlineData("http://sub.domain:1234", "http://*.domain:5678")]
    [InlineData("http://sub.domain", "http://domain.*")]
    [InlineData("http://sub.sub.domain", "http://sub.*.domain")]
    [InlineData("http://sub.domain.hacker", "http://*.domain")]
    [InlineData("https://sub.domain", "http://*.domain")]
    public void IsOriginAnAllowedSubdomain_ReturnsFalse_WhenNotASubdomain(string origin, string allowedOrigin)
    {
        // Arrange
        var policy = new CorsPolicy();
        policy.Origins.Add(allowedOrigin);

        // Act
        var isAllowed = policy.IsOriginAnAllowedSubdomain(origin);

        // Assert
        Assert.False(isAllowed);
    }
}
