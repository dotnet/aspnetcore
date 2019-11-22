// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Xunit;

namespace Microsoft.AspNetCore.Cors.Infrastructure
{
    public class CorsResultTest
    {
        [Fact]
        public void Default_Constructor()
        {
            // Arrange & Act
            var result = new CorsResult();

            // Assert
            Assert.Empty(result.AllowedHeaders);
            Assert.Empty(result.AllowedExposedHeaders);
            Assert.Empty(result.AllowedMethods);
            Assert.False(result.SupportsCredentials);
            Assert.Null(result.AllowedOrigin);
            Assert.Null(result.PreflightMaxAge);
        }

        [Fact]
        public void SettingNegativePreflightMaxAge_Throws()
        {
            // Arrange
            var result = new CorsResult();

            // Act
            var exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                result.PreflightMaxAge = TimeSpan.FromSeconds(-1);
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
            var corsResult = new CorsResult
            {
                SupportsCredentials = true,
                PreflightMaxAge = TimeSpan.FromSeconds(30),
                AllowedOrigin = "*"
            };
            corsResult.AllowedExposedHeaders.Add("foo");
            corsResult.AllowedHeaders.Add("bar");
            corsResult.AllowedHeaders.Add("baz");
            corsResult.AllowedMethods.Add("GET");

            // Act
            var result = corsResult.ToString();

            // Assert
            Assert.Equal(
                @"AllowCredentials: True, PreflightMaxAge: 30, AllowOrigin: *," +
                " AllowExposedHeaders: {foo}, AllowHeaders: {bar,baz}, AllowMethods: {GET}",
                result);
        }
    }
}