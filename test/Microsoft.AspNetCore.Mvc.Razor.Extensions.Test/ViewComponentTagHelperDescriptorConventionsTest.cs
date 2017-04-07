// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Razor.Language;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.Razor.Extensions
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
            var descriptor = ITagHelperDescriptorBuilder.Create("TypeName", "AssemblyName")
                .TagMatchingRule(rule =>
                    rule.RequireTagName("tag-name"))
                .Build();


            return descriptor;
        }

        private static TagHelperDescriptor CreateViewComponentTagHelperDescriptor()
        {
            var descriptor = ITagHelperDescriptorBuilder.Create("TypeName", "AssemblyName")
                .TagMatchingRule(rule =>
                    rule.RequireTagName("tag-name"))
                .AddMetadata(ViewComponentTagHelperDescriptorConventions.ViewComponentNameKey, "ViewComponentName")
                .Build();

            return descriptor;
        }
    }
}