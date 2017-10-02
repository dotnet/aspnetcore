// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Microsoft.AspNetCore.Http.Tests
{
    public class HttpServiceCollectionExtensionsTests
    {
        [Fact]
        public void AddHttpContextAccessor_AddsWithCorrectLifetime()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services.AddHttpContextAccessor();

            // Assert
            var descriptor = services[0];
            Assert.Equal(ServiceLifetime.Singleton, descriptor.Lifetime);
            Assert.Equal(typeof(HttpContextAccessor), descriptor.ImplementationType);
        }

        [Fact]
        public void AddHttpContextAccessor_ThrowsWithoutServices()
        {
            Assert.Throws<ArgumentNullException>("services", () => HttpServiceCollectionExtensions.AddHttpContextAccessor(null));
        }
    }
}
