// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.WebAssembly.Rendering;

public class RenderRegistryTest
{
    [Fact]
    public void RendererRegistry_Find_ThrowsErrorOnNonWASM()
    {
        // Act
        Exception ex = Assert.Throws<ArgumentException>(() => RendererRegistry.Find(123));

        // Assert
        Assert.Equal("There is no renderer with ID 123.", ex.Message);
    }
    [Fact]
    public void RendererRegistry_Remove_DoesNothingOnNonWASM()
    {
        // Act
        var result = RendererRegistry.TryRemove(123);

        // Assert
        Assert.False(result);
    }
}
