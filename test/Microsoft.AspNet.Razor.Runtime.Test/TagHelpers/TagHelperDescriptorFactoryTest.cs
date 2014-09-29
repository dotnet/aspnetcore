// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Razor.TagHelpers;
using Xunit;

namespace Microsoft.AspNet.Razor.Runtime.TagHelpers
{
    public class TagHelperDescriptorFactoryTest
    {
        [Fact]
        public void DescriptorFactory_BuildsDescriptorsFromSimpleTypes()
        {
            // Arrange
            var expectedDescriptor = new TagHelperDescriptor("Object", "System.Object", ContentBehavior.None);

            // Act
            var descriptor = TagHelperDescriptorFactory.CreateDescriptor(typeof(object));

            // Assert
            Assert.Equal(descriptor, expectedDescriptor, CompleteTagHelperDescriptorComparer.Default);
        }

        [Fact]
        public void DescriptorFactory_BuildsDescriptorsWithConventionNames()
        {
            // Arrange
            var intProperty = typeof(SingleAttributeTagHelper).GetProperty(nameof(SingleAttributeTagHelper.IntAttribute));
            var expectedDescriptor = new TagHelperDescriptor(
                "SingleAttribute",
                typeof(SingleAttributeTagHelper).FullName,
                ContentBehavior.None,
                new[] {
                    new TagHelperAttributeDescriptor(nameof(SingleAttributeTagHelper.IntAttribute), intProperty)
                });

            // Act
            var descriptor = TagHelperDescriptorFactory.CreateDescriptor(typeof(SingleAttributeTagHelper));

            // Assert
            Assert.Equal(descriptor, expectedDescriptor, CompleteTagHelperDescriptorComparer.Default);
        }

        [Fact]
        public void DescriptorFactory_OnlyAcceptsPropertiesWithGetAndSet()
        {
            // Arrange
            var validProperty = typeof(MissingAccessorTagHelper).GetProperty(
                nameof(MissingAccessorTagHelper.ValidAttribute));
            var expectedDescriptor = new TagHelperDescriptor(
                "MissingAccessor",
                typeof(MissingAccessorTagHelper).FullName,
                ContentBehavior.None,
                new[] {
                    new TagHelperAttributeDescriptor(nameof(MissingAccessorTagHelper.ValidAttribute), validProperty)
                });

            // Act
            var descriptor = TagHelperDescriptorFactory.CreateDescriptor(typeof(MissingAccessorTagHelper));

            // Assert
            Assert.Equal(descriptor, expectedDescriptor, CompleteTagHelperDescriptorComparer.Default);
        }

        [Fact]
        public void DescriptorFactory_OnlyAcceptsPropertiesWithPublicGetAndSet()
        {
            // Arrange
            var validProperty = typeof(PrivateAccessorTagHelper).GetProperty(
                nameof(PrivateAccessorTagHelper.ValidAttribute));
            var expectedDescriptor = new TagHelperDescriptor(
                "PrivateAccessor",
                typeof(PrivateAccessorTagHelper).FullName,
                ContentBehavior.None,
                new[] {
                    new TagHelperAttributeDescriptor(
                        nameof(PrivateAccessorTagHelper.ValidAttribute), validProperty)
                });

            // Act
            var descriptor = TagHelperDescriptorFactory.CreateDescriptor(typeof(PrivateAccessorTagHelper));

            // Assert
            Assert.Equal(descriptor, expectedDescriptor, CompleteTagHelperDescriptorComparer.Default);
        }
    }
}