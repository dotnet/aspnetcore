// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Razor.Evolution;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.Razor.Host.Test
{
    public class ViewComponentTagHelperDescriptorConventionsTest
    {
        [Fact]
        public void IsViewComponentDescriptor_ReturnsFalseForInvalidDescriptor()
        {
            //Arrange
            var tagHelperDescriptor = CreateTagHelperDescriptor();

            // Act
            var isViewComponentDescriptor = ViewComponentTagHelperDescriptorConventions
                .IsViewComponentDescriptor(tagHelperDescriptor);

            // Assert
            Assert.False(isViewComponentDescriptor);
        }

        [Fact]
        public void IsViewComponentDescriptor_ReturnsTrueForValidDescriptor()
        {
            // Arrange
            var descriptor = CreateViewComponentTagHelperDescriptor();

            // Act
            var isViewComponentDescriptor = ViewComponentTagHelperDescriptorConventions
                .IsViewComponentDescriptor(descriptor);

            // Assert
            Assert.True(isViewComponentDescriptor);
        }

        private static TagHelperDescriptor CreateTagHelperDescriptor()
        {
            var descriptor = new TagHelperDescriptor
            {
                TagName = "tag-name",
                TypeName = "TypeName",
                AssemblyName = "AssemblyName",
            };

            return descriptor;
        }

        private static TagHelperDescriptor CreateViewComponentTagHelperDescriptor()
        {
            var descriptor = CreateTagHelperDescriptor();
            descriptor.PropertyBag.Add(
                ViewComponentTagHelperDescriptorConventions.ViewComponentNameKey,
                "ViewComponentName");

            return descriptor;
        }
    }
}