// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Mvc.ApplicationParts;

public class AssemblyPartTest
{
    [Fact]
    public void AssemblyPart_Name_ReturnsAssemblyName()
    {
        // Arrange
        var part = new AssemblyPart(typeof(AssemblyPartTest).Assembly);

        // Act
        var name = part.Name;

        // Assert
        Assert.Equal("Microsoft.AspNetCore.Mvc.Core.Test", name);
    }

    [Fact]
    public void AssemblyPart_Types_ReturnsDefinedTypes()
    {
        // Arrange
        var assembly = typeof(AssemblyPartTest).Assembly;
        var part = new AssemblyPart(assembly);

        // Act
        var types = part.Types;

        // Assert
        Assert.Equal(assembly.DefinedTypes, types);
        Assert.NotSame(assembly.DefinedTypes, types);
    }

    [Fact]
    public void AssemblyPart_Assembly_ReturnsAssembly()
    {
        // Arrange
        var assembly = typeof(AssemblyPartTest).Assembly;
        var part = new AssemblyPart(assembly);

        // Act & Assert
        Assert.Equal(part.Assembly, assembly);
    }
}
