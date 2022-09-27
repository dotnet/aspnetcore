// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using System.Reflection.Emit;

namespace Microsoft.AspNetCore.Mvc.ApplicationParts;

public class AssemblyPartExtensionTest
{
    [Fact]
    public void GetReferencePaths_ReturnsReferencesFromDependencyContext_IfPreserveCompilationContextIsSet()
    {
        // Arrange
        var assembly = GetType().Assembly;
        var part = new AssemblyPart(assembly);

        // Act
        var references = part.GetReferencePaths().ToList();

        // Assert
        Assert.Contains(assembly.Location, references);
        Assert.Contains(
            typeof(AssemblyPart).Assembly.GetName().Name,
            references.Select(Path.GetFileNameWithoutExtension));
    }

    [Fact]
    public void GetReferencePaths_ReturnsAssemblyLocation_IfPreserveCompilationContextIsNotSet()
    {
        // Arrange
        // src projects do not have preserveCompilationContext specified.
        var assembly = typeof(AssemblyPart).Assembly;
        var part = new AssemblyPart(assembly);

        // Act
        var references = part.GetReferencePaths().ToList();

        // Assert
        var actual = Assert.Single(references);
        Assert.Equal(assembly.Location, actual);
    }

    [Fact]
    public void GetReferencePaths_ReturnsEmptySequenceForDynamicAssembly()
    {
        // Arrange
        var name = new AssemblyName($"DynamicAssembly-{Guid.NewGuid()}");
        var assembly = AssemblyBuilder.DefineDynamicAssembly(name,
            AssemblyBuilderAccess.RunAndCollect);

        var part = new AssemblyPart(assembly);

        // Act
        var references = part.GetReferencePaths().ToList();

        // Assert
        Assert.Empty(references);
    }
}
