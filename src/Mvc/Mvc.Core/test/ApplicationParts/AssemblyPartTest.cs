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
    public class AssemblyPartTest
    {
        [Fact]
        public void AssemblyPart_Name_ReturnsAssemblyName()
        {
            // Arrange
            var part = new AssemblyPart(typeof(AssemblyPartTest).GetTypeInfo().Assembly);

            // Act
            var name = part.Name;

            // Assert
            Assert.Equal("Microsoft.AspNetCore.Mvc.Core.Test", name);
        }

        [Fact]
        public void AssemblyPart_Types_ReturnsDefinedTypes()
        {
            // Arrange
            var assembly = typeof(AssemblyPartTest).GetTypeInfo().Assembly;
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
            var assembly = typeof(AssemblyPartTest).GetTypeInfo().Assembly;
            var part = new AssemblyPart(assembly);

            // Act & Assert
            Assert.Equal(part.Assembly, assembly);
        }
    }
}
