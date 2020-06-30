// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language
{
    public class TagHelperDescriptorExtensionsTest
    {
        [Fact]
        public void GetTypeName_ReturnsTypeName()
        {
            // Arrange
            var expectedTypeName = "TestTagHelper";
            var descriptor = TagHelperDescriptorBuilder.Create(expectedTypeName, "TestAssembly").TypeName(expectedTypeName).Build();

            // Act
            var typeName = descriptor.GetTypeName();

            // Assert
            Assert.Equal(expectedTypeName, typeName);
        }

        [Fact]
        public void GetTypeName_ReturnsNullIfNoTypeName()
        {
            // Arrange
            var descriptor = TagHelperDescriptorBuilder.Create("Test", "TestAssembly").Build();

            // Act
            var typeName = descriptor.GetTypeName();

            // Assert
            Assert.Null(typeName);
        }

        [Fact]
        public void IsDefaultKind_ReturnsTrue_IfKindIsDefault()
        {
            // Arrange
            var descriptor = TagHelperDescriptorBuilder.Create("TestTagHelper", "TestAssembly").Build();

            // Act
            var isDefault = descriptor.IsDefaultKind();

            // Assert
            Assert.True(isDefault);
        }

        [Fact]
        public void IsDefaultKind_ReturnsFalse_IfKindIsNotDefault()
        {
            // Arrange
            var descriptor = TagHelperDescriptorBuilder.Create("other-kind", "TestTagHelper", "TestAssembly").Build();

            // Act
            var isDefault = descriptor.IsDefaultKind();

            // Assert
            Assert.False(isDefault);
        }
    }
}
