// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.InternalTesting;

namespace Microsoft.AspNetCore.Components.WebAssembly.Hosting;

public class RootComponentMappingTest
{
    [Fact]
    public void Constructor_ValidatesComponentType_Success()
    {
        // Arrange
        // Act
        var mapping = new RootComponentMapping(typeof(Router), "test");

        // Assert (does not throw)
        GC.KeepAlive(mapping);
    }

    [Fact]
    public void Constructor_ValidatesComponentType_Failure()
    {
        // Arrange
        // Act & Assert
        ExceptionAssert.ThrowsArgument(
            () => new RootComponentMapping(typeof(StringBuilder), "test"),
            "componentType",
            $"The type '{nameof(StringBuilder)}' must implement IComponent to be used as a root component.");
    }
}
