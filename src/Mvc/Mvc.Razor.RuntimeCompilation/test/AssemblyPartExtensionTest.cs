// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.ApplicationParts
{
    public class AssemblyPartExtensionTest
    {
        [Fact]
        public void GetReferencePaths_ReturnsReferencesFromDependencyContext_IfPreserveCompilationContextIsSet()
        {
            // Arrange
            var assembly = GetType().GetTypeInfo().Assembly;
            var part = new AssemblyPart(assembly);

            // Act
            var references = part.GetReferencePaths().ToList();

            // Assert
            Assert.Contains(assembly.Location, references);
            Assert.Contains(
                typeof(AssemblyPart).GetTypeInfo().Assembly.GetName().Name,
                references.Select(Path.GetFileNameWithoutExtension));
        }

        [Fact]
        public void GetReferencePaths_ReturnsAssemblyLocation_IfPreserveCompilationContextIsNotSet()
        {
            // Arrange
            // src projects do not have preserveCompilationContext specified.
            var assembly = typeof(AssemblyPart).GetTypeInfo().Assembly;
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
}
