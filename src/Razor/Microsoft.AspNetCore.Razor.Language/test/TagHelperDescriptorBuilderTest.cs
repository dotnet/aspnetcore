// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Xunit;

namespace Microsoft.AspNetCore.Razor.Language
{
    public class TagHelperDescriptorBuilderTest
    {
        [Fact]
        public void DisplayName_SetsDescriptorsDisplayName()
        {
            // Arrange
            var expectedDisplayName = "ExpectedDisplayName";
            var builder = TagHelperDescriptorBuilder.Create("TestTagHelper", "TestAssembly");

            // Act
            var descriptor = builder.DisplayName(expectedDisplayName).Build();

            // Assert
            Assert.Equal(expectedDisplayName, descriptor.DisplayName);
        }

        [Fact]
        public void DisplayName_DefaultsToTypeName()
        {
            // Arrange
            var expectedDisplayName = "TestTagHelper";
            var builder = TagHelperDescriptorBuilder.Create("TestTagHelper", "TestAssembly");

            // Act
            var descriptor = builder.Build();

            // Assert
            Assert.Equal(expectedDisplayName, descriptor.DisplayName);
        }
    }
}
