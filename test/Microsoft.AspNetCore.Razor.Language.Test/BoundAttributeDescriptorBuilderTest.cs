// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Xunit;

namespace Microsoft.AspNetCore.Razor.Language
{
    public class BoundAttributeDescriptorBuilderTest
    {
        [Fact]
        public void DisplayName_SetsDescriptorsDisplayName()
        {
            // Arrange
            var expectedDisplayName = "ExpectedDisplayName";
            var builder = BoundAttributeDescriptorBuilder.Create("TestTagHelper");

            // Act
            var descriptor = builder.DisplayName(expectedDisplayName).Build();

            // Assert
            Assert.Equal(expectedDisplayName, descriptor.DisplayName);
        }

        [Fact]
        public void DisplayName_DefaultsToPropertyLookingDisplayName()
        {
            // Arrange
            var builder = BoundAttributeDescriptorBuilder.Create("TestTagHelper")
                .TypeName(typeof(int).FullName)
                .PropertyName("SomeProperty");

            // Act
            var descriptor = builder.Build();

            // Assert
            Assert.Equal("int TestTagHelper.SomeProperty", descriptor.DisplayName);
        }
    }
}
