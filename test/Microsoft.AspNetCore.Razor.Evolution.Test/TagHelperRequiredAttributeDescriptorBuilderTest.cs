// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Xunit;

namespace Microsoft.AspNetCore.Razor.Evolution
{
    public class TagHelperRequiredAttributeDescriptorBuilderTest
    {
        [Fact]
        public void Build_DisplayNameIsName_NameComparisonFullMatch()
        {
            // Arrange
            var descriptorBuilder = RequiredAttributeDescriptorBuilder.Create()
                .Name("asp-action")
                .NameComparisonMode(RequiredAttributeDescriptor.NameComparisonMode.FullMatch);

            // Act
            var descriptor = descriptorBuilder.Build();

            // Assert
            Assert.Equal("asp-action", descriptor.DisplayName);
        }

        [Fact]
        public void Build_DisplayNameIsNameWithDots_NameComparisonPrefixMatch()
        {
            // Arrange
            var descriptorBuilder = RequiredAttributeDescriptorBuilder.Create()
                .Name("asp-route-")
                .NameComparisonMode(RequiredAttributeDescriptor.NameComparisonMode.PrefixMatch);

            // Act
            var descriptor = descriptorBuilder.Build();

            // Assert
            Assert.Equal("asp-route-...", descriptor.DisplayName);
        }
    }
}
