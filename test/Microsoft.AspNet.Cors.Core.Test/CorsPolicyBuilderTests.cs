// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Microsoft.AspNet.Cors.Core.Test
{
    public class CorsPolicyBuilderTests
    {
        [Fact]
        public void Constructor_WithPolicy_AddsTheGivenPolicy()
        {
            // Arrange
            var policy = new CorsPolicy();
            policy.Origins.Add("http://existing.com");
            policy.Headers.Add("Existing");
            policy.Methods.Add("GET");
            policy.ExposedHeaders.Add("ExistingExposed");
            policy.SupportsCredentials = true;
            policy.PreflightMaxAge = TimeSpan.FromSeconds(12);

            // Act
            var builder = new CorsPolicyBuilder(policy);

            // Assert
            var corsPolicy = builder.Build();

            Assert.False(corsPolicy.AllowAnyHeader);
            Assert.False(corsPolicy.AllowAnyMethod);
            Assert.False(corsPolicy.AllowAnyOrigin);
            Assert.True(corsPolicy.SupportsCredentials);
            Assert.Equal(policy.Headers, corsPolicy.Headers);
            Assert.Equal(policy.Methods, corsPolicy.Methods);
            Assert.Equal(policy.Origins, corsPolicy.Origins);
            Assert.Equal(policy.ExposedHeaders, corsPolicy.ExposedHeaders);
            Assert.Equal(TimeSpan.FromSeconds(12), corsPolicy.PreflightMaxAge);
        }

        [Fact]
        public void Constructor_WithNoOrigin()
        {
            // Arrange & Act
            var builder = new CorsPolicyBuilder();

            // Assert
            var corsPolicy = builder.Build();
            Assert.False(corsPolicy.AllowAnyHeader);
            Assert.False(corsPolicy.AllowAnyMethod);
            Assert.False(corsPolicy.AllowAnyOrigin);
            Assert.False(corsPolicy.SupportsCredentials);
            Assert.Empty(corsPolicy.ExposedHeaders);
            Assert.Empty(corsPolicy.Headers);
            Assert.Empty(corsPolicy.Methods);
            Assert.Empty(corsPolicy.Origins);
            Assert.Null(corsPolicy.PreflightMaxAge);
        }

        [Theory]
        [InlineData("")]
        [InlineData("http://example.com,http://example2.com")]
        public void Constructor_WithParamsOrigin_InitializesOrigin(string origin)
        {
            // Arrange
            var origins = origin.Split(',');

            // Act
            var builder = new CorsPolicyBuilder(origins);

            // Assert
            var corsPolicy = builder.Build();
            Assert.False(corsPolicy.AllowAnyHeader);
            Assert.False(corsPolicy.AllowAnyMethod);
            Assert.False(corsPolicy.AllowAnyOrigin);
            Assert.False(corsPolicy.SupportsCredentials);
            Assert.Empty(corsPolicy.ExposedHeaders);
            Assert.Empty(corsPolicy.Headers);
            Assert.Empty(corsPolicy.Methods);
            Assert.Equal(origins.ToList(), corsPolicy.Origins);
            Assert.Null(corsPolicy.PreflightMaxAge);
        }

        [Fact]
        public void WithOrigins_AddsOrigins()
        {
            // Arrange
            var builder = new CorsPolicyBuilder();

            // Act
            builder.WithOrigins("http://example.com", "http://example2.com");

            // Assert
            var corsPolicy = builder.Build();
            Assert.False(corsPolicy.AllowAnyOrigin);
            Assert.Equal(new List<string>() { "http://example.com", "http://example2.com" }, corsPolicy.Origins);
        }

        [Fact]
        public void AllowAnyOrigin_AllowsAny()
        {
            // Arrange
            var builder = new CorsPolicyBuilder();

            // Act
            builder.AllowAnyOrigin();

            // Assert
            var corsPolicy = builder.Build();
            Assert.True(corsPolicy.AllowAnyOrigin);
            Assert.Equal(new List<string>() { "*" }, corsPolicy.Origins);
        }


        [Fact]
        public void WithMethods_AddsMethods()
        {
            // Arrange
            var builder = new CorsPolicyBuilder();

            // Act
            builder.WithMethods("PUT", "GET");

            // Assert
            var corsPolicy = builder.Build();
            Assert.False(corsPolicy.AllowAnyOrigin);
            Assert.Equal(new List<string>() { "PUT", "GET" }, corsPolicy.Methods);
        }

        [Fact]
        public void AllowAnyMethod_AllowsAny()
        {
            // Arrange
            var builder = new CorsPolicyBuilder();

            // Act
            builder.AllowAnyMethod();

            // Assert
            var corsPolicy = builder.Build();
            Assert.True(corsPolicy.AllowAnyMethod);
            Assert.Equal(new List<string>() { "*" }, corsPolicy.Methods);
        }

        [Fact]
        public void WithHeaders_AddsHeaders()
        {
            // Arrange
            var builder = new CorsPolicyBuilder();

            // Act
            builder.WithHeaders("example1", "example2");

            // Assert
            var corsPolicy = builder.Build();
            Assert.False(corsPolicy.AllowAnyHeader);
            Assert.Equal(new List<string>() { "example1", "example2" }, corsPolicy.Headers);
        }

        [Fact]
        public void AllowAnyHeaders_AllowsAny()
        {
            // Arrange
            var builder = new CorsPolicyBuilder();

            // Act
            builder.AllowAnyHeader();

            // Assert
            var corsPolicy = builder.Build();
            Assert.True(corsPolicy.AllowAnyHeader);
            Assert.Equal(new List<string>() { "*" }, corsPolicy.Headers);
        }

        [Fact]
        public void WithExposedHeaders_AddsExposedHeaders()
        {
            // Arrange
            var builder = new CorsPolicyBuilder();

            // Act
            builder.WithExposedHeaders("exposed1", "exposed2");

            // Assert
            var corsPolicy = builder.Build();
            Assert.Equal(new List<string>() { "exposed1", "exposed2" }, corsPolicy.ExposedHeaders);
        }

        [Fact]
        public void SetPreFlightMaxAge_SetsThePreFlightAge()
        {
            // Arrange
            var builder = new CorsPolicyBuilder();

            // Act
            builder.SetPreflightMaxAge(TimeSpan.FromSeconds(12));

            // Assert
            var corsPolicy = builder.Build();
            Assert.Equal(TimeSpan.FromSeconds(12), corsPolicy.PreflightMaxAge);
        }

        [Fact]
        public void AllowCredential_SetsSupportsCredentials_ToTrue()
        {
            // Arrange
            var builder = new CorsPolicyBuilder();

            // Act
            builder.AllowCredentials();

            // Assert
            var corsPolicy = builder.Build();
            Assert.True(corsPolicy.SupportsCredentials);
        }


        [Fact]
        public void DisallowCredential_SetsSupportsCredentials_ToFalse()
        {
            // Arrange
            var builder = new CorsPolicyBuilder();

            // Act
            builder.DisallowCredentials();

            // Assert
            var corsPolicy = builder.Build();
            Assert.False(corsPolicy.SupportsCredentials);
        }
    }
}