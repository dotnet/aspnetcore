// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Xunit;

namespace Microsoft.AspNetCore.Razor.Language
{
    public class DefaultBoundAttributeDescriptorBuilderTest
    {
        [Fact]
        public void DisplayName_SetsDescriptorsDisplayName()
        {
            // Arrange
            var expectedDisplayName = "ExpectedDisplayName";

            var tagHelperBuilder = new DefaultTagHelperDescriptorBuilder(TagHelperConventions.DefaultKind, "TestTagHelper", "Test");

            var builder = new DefaultBoundAttributeDescriptorBuilder(tagHelperBuilder, TagHelperConventions.DefaultKind);
            builder.DisplayName(expectedDisplayName);

            // Act
            var descriptor = builder.Build();

            // Assert
            Assert.Equal(expectedDisplayName, descriptor.DisplayName);
        }

        [Fact]
        public void DisplayName_DefaultsToPropertyLookingDisplayName()
        {
            // Arrange
            var tagHelperBuilder = new DefaultTagHelperDescriptorBuilder(TagHelperConventions.DefaultKind, "TestTagHelper", "Test");
            tagHelperBuilder.TypeName("TestTagHelper");

            var builder = new DefaultBoundAttributeDescriptorBuilder(tagHelperBuilder, TagHelperConventions.DefaultKind);
            builder
                .TypeName(typeof(int).FullName)
                .PropertyName("SomeProperty");

            // Act
            var descriptor = builder.Build();

            // Assert
            Assert.Equal("int TestTagHelper.SomeProperty", descriptor.DisplayName);
        }
    }
}
