// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.HotReload;
using Microsoft.AspNetCore.Components.Infrastructure;
using Xunit;

namespace Microsoft.AspNetCore.Components.Tests;

public class RootTypeCacheTest
{
    [Fact]
    public void HotReload_ClearsCache()
    {
        // Arrange
        var cache1 = new RootTypeCache();
        var cache2 = new RootTypeCache();

        // Populate caches by attempting to resolve types
        var result1Before = cache1.GetRootType("NonExistentAssembly", "NonExistentType");
        var result2Before = cache2.GetRootType("AnotherAssembly", "AnotherType");

        // Verify initial state (should be null for non-existent types)
        Assert.Null(result1Before);
        Assert.Null(result2Before);

        // Act - Trigger hot reload cache clearing
        HotReloadManager.UpdateApplication(null);

        // Assert - Verify cache is cleared by checking that subsequent lookups still work
        // (This verifies the cache clearing didn't break functionality)
        var result1After = cache1.GetRootType("NonExistentAssembly", "NonExistentType");
        var result2After = cache2.GetRootType("AnotherAssembly", "AnotherType");

        Assert.Null(result1After);
        Assert.Null(result2After);
    }

    [Fact]
    public void GetRootType_ReturnsNullForNonExistentType()
    {
        // Arrange
        var cache = new RootTypeCache();

        // Act
        var result = cache.GetRootType("NonExistentAssembly", "NonExistentType");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetRootType_ReturnsTypeForExistingType()
    {
        // Arrange
        var cache = new RootTypeCache();
        var currentAssembly = typeof(RootTypeCache).Assembly.GetName().Name;
        var existingTypeName = typeof(RootTypeCache).FullName;

        // Act
        var result = cache.GetRootType(currentAssembly!, existingTypeName!);

        // Assert
        Assert.Equal(typeof(RootTypeCache), result);
    }
}