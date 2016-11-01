// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Xunit;

namespace Microsoft.AspNetCore.Cors.Infrastructure
{
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
}