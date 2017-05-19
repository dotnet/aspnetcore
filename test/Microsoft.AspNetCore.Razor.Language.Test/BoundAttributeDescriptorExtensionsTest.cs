// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Xunit;

namespace Microsoft.AspNetCore.Razor.Language
{
    public class BoundAttributeDescriptorExtensionsTest
    {
        [Fact]
        public void GetPropertyName_ReturnsPropertyName()
        {
            // Arrange
            var expectedPropertyName = "IntProperty";
            var descriptor = BoundAttributeDescriptorBuilder.Create("TestTagHelper")
                .Name("test")
                .PropertyName(expectedPropertyName)
                .TypeName(typeof(int).FullName)
                .Build();

            // Act
            var propertyName = descriptor.GetPropertyName();

            // Assert
            Assert.Equal(expectedPropertyName, propertyName);
        }

        [Fact]
        public void GetPropertyName_ReturnsNullIfNoPropertyName()
        {
            // Arrange
            var descriptor = BoundAttributeDescriptorBuilder.Create("TestTagHelper")
                .Name("test")
                .TypeName(typeof(int).FullName)
                .Build();

            // Act
            var propertyName = descriptor.GetPropertyName();

            // Assert
            Assert.Null(propertyName);
        }

        [Fact]
        public void IsDefaultKind_ReturnsTrueIfFromDefaultBuilder()
        {
            // Arrange
            var descriptor = BoundAttributeDescriptorBuilder.Create("TestTagHelper")
                .Name("test")
                .PropertyName("IntProperty")
                .TypeName(typeof(int).FullName)
                .Build();

            // Act
            var isDefault = descriptor.IsDefaultKind();

            // Assert
            Assert.True(isDefault);
        }

        [Fact]
        public void IsDefaultKind_ReturnsFalseIfFromCustomBuilder()
        {
            // Arrange
            var descriptor = new CustomBoundAttributeDescriptor();

            // Act
            var isDefault = descriptor.IsDefaultKind();

            // Assert
            Assert.False(isDefault);
        }

        private class CustomBoundAttributeDescriptor : BoundAttributeDescriptor
        {
            public CustomBoundAttributeDescriptor() : base("custom")
            {
            }
        }
    }
}
