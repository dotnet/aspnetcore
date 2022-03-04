// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Xunit;

namespace Microsoft.AspNetCore.Cors.Infrastructure
{
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
                @"AllowAnyHeader: False, AllowAnyMethod: False, AllowAnyOrigin: False, PreflightMaxAge: 12,"+
                " SupportsCredentials: True, Origins: {http://example.com,http://example.org}, Methods: {GET},"+
                " Headers: {foo,bar}, ExposedHeaders: {}",
                policyString);
        }
    }
}