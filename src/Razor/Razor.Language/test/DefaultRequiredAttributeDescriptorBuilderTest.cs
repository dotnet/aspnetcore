// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Xunit;

namespace Microsoft.AspNetCore.Razor.Language
{
    public class DefaultRequiredAttributeDescriptorBuilderTest
    {
        [Fact]
        public void Build_DisplayNameIsName_NameComparisonFullMatch()
        {
            // Arrange
            var builder = new DefaultRequiredAttributeDescriptorBuilder();

            builder
                .Name("asp-action")
                .NameComparisonMode(RequiredAttributeDescriptor.NameComparisonMode.FullMatch);

            // Act
            var descriptor = builder.Build();

            // Assert
            Assert.Equal("asp-action", descriptor.DisplayName);
        }

        [Fact]
        public void Build_DisplayNameIsNameWithDots_NameComparisonPrefixMatch()
        {
            // Arrange
            var builder = new DefaultRequiredAttributeDescriptorBuilder();

            builder
                .Name("asp-route-")
                .NameComparisonMode(RequiredAttributeDescriptor.NameComparisonMode.PrefixMatch);

            // Act
            var descriptor = builder.Build();

            // Assert
            Assert.Equal("asp-route-...", descriptor.DisplayName);
        }
    }
}
