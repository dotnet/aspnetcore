// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Routing.TestObjects;
using Xunit;

namespace Microsoft.AspNetCore.Routing
{
    public class DataSourceDependentCacheTest
    {
        [Fact]
        public void Cache_Initializes_WhenEnsureInitializedCalled()
        {
            // Arrange
            var called = false;

            var dataSource = new DynamicEndpointDataSource();
            var cache = new DataSourceDependentCache<string>(dataSource, (endpoints) =>
            {
                called = true;
                return "hello, world!";
            });

            // Act
            cache.EnsureInitialized();

            // Assert
            Assert.True(called);
            Assert.Equal("hello, world!", cache.Value);
        }

        [Fact]
        public void Cache_DoesNotInitialize_WhenValueCalled()
        {
            // Arrange
            var called = false;

            var dataSource = new DynamicEndpointDataSource();
            var cache = new DataSourceDependentCache<string>(dataSource, (endpoints) =>
            {
                called = true;
                return "hello, world!";
            });

            // Act
            GC.KeepAlive(cache.Value);

            // Assert
            Assert.False(called);
            Assert.Null(cache.Value);
        }

        [Fact]
        public void Cache_Reinitializes_WhenDataSourceChanges()
        {
            // Arrange
            var count = 0;

            var dataSource = new DynamicEndpointDataSource();
            var cache = new DataSourceDependentCache<string>(dataSource, (endpoints) =>
            {
                count++;
                return $"hello, {count}!";
            });

            cache.EnsureInitialized();
            Assert.Equal("hello, 1!", cache.Value);

            // Act
            dataSource.AddEndpoint(null);

            // Assert
            Assert.Equal(2, count);
            Assert.Equal("hello, 2!", cache.Value);
        }
    }
}
