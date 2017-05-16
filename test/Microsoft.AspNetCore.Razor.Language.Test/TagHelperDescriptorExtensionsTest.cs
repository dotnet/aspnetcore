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
            var descriptor = TagHelperDescriptorBuilder.Create(expectedTypeName, "TestAssembly").Build();

            // Act
            var typeName = descriptor.GetTypeName();

            // Assert
            Assert.Equal(expectedTypeName, typeName);
        }

        [Fact]
        public void GetTypeName_ReturnsNullIfNoTypeName()
        {
            // Arrange
            var descriptor = new CustomTagHelperDescriptor();

            // Act
            var typeName = descriptor.GetTypeName();

            // Assert
            Assert.Null(typeName);
        }

        [Fact]
        public void IsDefaultKind_ReturnsTrueIfFromDefaultBuilder()
        {
            // Arrange
            var descriptor = TagHelperDescriptorBuilder.Create("TestTagHelper", "TestAssembly").Build();

            // Act
            var isDefault = descriptor.IsDefaultKind();

            // Assert
            Assert.True(isDefault);
        }

        [Fact]
        public void IsDefaultKind_ReturnsFalseIfFromCustomBuilder()
        {
            // Arrange
            var descriptor = new CustomTagHelperDescriptor();

            // Act
            var isDefault = descriptor.IsDefaultKind();

            // Assert
            Assert.False(isDefault);
        }

        private class CustomTagHelperDescriptor : TagHelperDescriptor
        {
            public CustomTagHelperDescriptor() : base("custom")
            {
                Metadata = new Dictionary<string, string>();
            }
        }
    }
}
