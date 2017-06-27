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

            var tagHelperBuilder = new DefaultTagHelperDescriptorBuilder(TagHelperConventions.DefaultKind, "TestTagHelper", "Test");
            tagHelperBuilder.TypeName("TestTagHelper");

            var builder = new DefaultBoundAttributeDescriptorBuilder(tagHelperBuilder, TagHelperConventions.DefaultKind);
            builder
                .Name("test")
                .PropertyName(expectedPropertyName)
                .TypeName(typeof(int).FullName);

            var descriptor = builder.Build();

            // Act
            var propertyName = descriptor.GetPropertyName();

            // Assert
            Assert.Equal(expectedPropertyName, propertyName);
        }

        [Fact]
        public void GetPropertyName_ReturnsNullIfNoPropertyName()
        {
            // Arrange
            var tagHelperBuilder = new DefaultTagHelperDescriptorBuilder(TagHelperConventions.DefaultKind, "TestTagHelper", "Test");
            tagHelperBuilder.TypeName("TestTagHelper");

            var builder = new DefaultBoundAttributeDescriptorBuilder(tagHelperBuilder, TagHelperConventions.DefaultKind);
            builder
                .Name("test")
                .TypeName(typeof(int).FullName);

            var descriptor = builder.Build();

            // Act
            var propertyName = descriptor.GetPropertyName();

            // Assert
            Assert.Null(propertyName);
        }

        [Fact]
        public void IsDefaultKind_ReturnsTrue_IfKindIsDefault()
        {
            // Arrange
            var tagHelperBuilder = new DefaultTagHelperDescriptorBuilder(TagHelperConventions.DefaultKind, "TestTagHelper", "Test");
            tagHelperBuilder.TypeName("TestTagHelper");

            var builder = new DefaultBoundAttributeDescriptorBuilder(tagHelperBuilder, TagHelperConventions.DefaultKind);
            builder
                .Name("test")
                .PropertyName("IntProperty")
                .TypeName(typeof(int).FullName);

            var descriptor = builder.Build();

            // Act
            var isDefault = descriptor.IsDefaultKind();

            // Assert
            Assert.True(isDefault);
        }

        [Fact]
        public void IsDefaultKind_ReturnsFalse_IfKindIsNotDefault()
        {
            // Arrange
            var tagHelperBuilder = new DefaultTagHelperDescriptorBuilder("other-kind", "TestTagHelper", "Test");
            tagHelperBuilder.TypeName("TestTagHelper");

            var builder = new DefaultBoundAttributeDescriptorBuilder(tagHelperBuilder, "other-kind");
            builder
                .Name("test")
                .PropertyName("IntProperty")
                .TypeName(typeof(int).FullName);

            var descriptor = builder.Build();

            // Act
            var isDefault = descriptor.IsDefaultKind();

            // Assert
            Assert.False(isDefault);
        }
    }
}
