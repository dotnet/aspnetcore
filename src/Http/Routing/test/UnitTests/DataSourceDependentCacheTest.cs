// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Routing.TestObjects;

namespace Microsoft.AspNetCore.Routing;

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

    [Fact]
    public void Cache_CanDispose_WhenUninitialized()
    {
        // Arrange
        var count = 0;

        var dataSource = new DynamicEndpointDataSource();
        var cache = new DataSourceDependentCache<string>(dataSource, (endpoints) =>
        {
            count++;
            return $"hello, {count}!";
        });

        // Act
        cache.Dispose();

        // Assert
        dataSource.AddEndpoint(null);
        Assert.Null(cache.Value);
    }

    [Fact]
    public void Cache_CanDispose_WhenInitialized()
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
        cache.Dispose();

        // Assert
        dataSource.AddEndpoint(null);
        Assert.Equal("hello, 1!", cache.Value); // Ignores update
    }
}
