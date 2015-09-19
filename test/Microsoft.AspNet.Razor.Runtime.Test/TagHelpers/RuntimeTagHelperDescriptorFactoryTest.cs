// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.AspNet.Razor.TagHelpers;
using Microsoft.AspNet.Razor.Test.Internal;
using Xunit;

namespace Microsoft.AspNet.Razor.Runtime.TagHelpers
{
    public class RuntimeTagHelperDescriptorFactoryTest : TagHelperDescriptorFactoryTest
    {
        public override ITypeInfo GetTypeInfo(Type tagHelperType) =>
            new RuntimeTypeInfo(tagHelperType.GetTypeInfo());

        [Fact]
        public void CreateDescriptors_BuildsDescriptorsFromSimpleTypes()
        {
            // Arrange
            var errorSink = new ErrorSink();
            var objectAssemblyName = typeof(object).GetTypeInfo().Assembly.GetName().Name;
            var expectedDescriptor =
                CreateTagHelperDescriptor("object", "System.Object", objectAssemblyName);

            // Act
            var descriptors = TagHelperDescriptorFactory.CreateDescriptors(
                objectAssemblyName,
                GetTypeInfo(typeof(object)),
                designTime: false,
                errorSink: errorSink);

            // Assert
            Assert.Empty(errorSink.Errors);
            var descriptor = Assert.Single(descriptors);
            Assert.Equal(expectedDescriptor, descriptor, CaseSensitiveTagHelperDescriptorComparer.Default);
        }

        [Theory]
        [MemberData(nameof(TagHelperWithPrefixData))]
        public void CreateDescriptors_WithPrefixes_ReturnsExpectedAttributeDescriptors(
            Type tagHelperType,
            IEnumerable<TagHelperAttributeDescriptor> expectedAttributeDescriptors,
            string[] expectedErrorMessages)
        {
            // Arrange
            var errorSink = new ErrorSink();

            // Act
            var descriptors = TagHelperDescriptorFactory.CreateDescriptors(
                AssemblyName,
                GetTypeInfo(tagHelperType),
                designTime: false,
                errorSink: errorSink);

            // Assert
            var errors = errorSink.Errors.ToArray();
            Assert.Equal(expectedErrorMessages.Length, errors.Length);

            for (var i = 0; i < errors.Length; i++)
            {
                Assert.Equal(0, errors[i].Length);
                Assert.Equal(SourceLocation.Zero, errors[i].Location);
                Assert.Equal(expectedErrorMessages[i], errors[i].Message, StringComparer.Ordinal);
            }

            var descriptor = Assert.Single(descriptors);
            Assert.Equal(
                expectedAttributeDescriptors,
                descriptor.Attributes,
                TagHelperAttributeDescriptorComparer.Default);
        }

        // TagHelperDesignTimeDescriptors are not created in CoreCLR.
#if !DNXCORE50
        public static TheoryData OutputElementHintData
        {
            get
            {
                // tagHelperType, expectedDescriptors
                return new TheoryData<Type, TagHelperDescriptor[]>
                {
                    {
                        typeof(OutputElementHintTagHelper),
                        new[]
                        {
                            new TagHelperDescriptor
                            {
                                TagName = "output-element-hint",
                                TypeName = typeof(OutputElementHintTagHelper).FullName,
                                AssemblyName = AssemblyName,
                                DesignTimeDescriptor = new TagHelperDesignTimeDescriptor
                                {
                                    OutputElementHint = "strong"
                                }
                            }
                        }
                    },
                    {
                        typeof(MulitpleDescriptorTagHelperWithOutputElementHint),
                        new[]
                        {
                            new TagHelperDescriptor
                            {
                                TagName = "a",
                                TypeName = typeof(MulitpleDescriptorTagHelperWithOutputElementHint).FullName,
                                AssemblyName = AssemblyName,
                                DesignTimeDescriptor = new TagHelperDesignTimeDescriptor
                                {
                                    OutputElementHint = "div"
                                }
                            },
                            new TagHelperDescriptor
                            {
                                TagName = "p",
                                TypeName = typeof(MulitpleDescriptorTagHelperWithOutputElementHint).FullName,
                                AssemblyName = AssemblyName,
                                DesignTimeDescriptor = new TagHelperDesignTimeDescriptor
                                {
                                    OutputElementHint = "div"
                                }
                            }
                        }
                    }
                };
            }
        }

        [Theory]
        [MemberData(nameof(OutputElementHintData))]
        public void CreateDescriptors_CreatesDesignTimeDescriptorsWithOutputElementHint(
            Type tagHelperType,
            TagHelperDescriptor[] expectedDescriptors)
        {
            // Arrange
            var errorSink = new ErrorSink();

            // Act
            var descriptors = TagHelperDescriptorFactory.CreateDescriptors(
                AssemblyName,
                GetTypeInfo(tagHelperType),
                designTime: true,
                errorSink: errorSink);

            // Assert
            Assert.Empty(errorSink.Errors);

            // We don't care about order. Mono returns reflected attributes differently so we need to ensure order
            // doesn't matter by sorting.
            descriptors = descriptors.OrderBy(descriptor => descriptor.TagName);

            Assert.Equal(expectedDescriptors, descriptors, CaseSensitiveTagHelperDescriptorComparer.Default);
        }
#endif
    }
}