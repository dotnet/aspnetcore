// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Reflection;
using Microsoft.AspNet.Razor.TagHelpers;
using Xunit;

namespace Microsoft.AspNet.Razor.Runtime.TagHelpers
{
    public class TagHelperDescriptorFactoryTest
    {
        private static readonly string AssemblyName =
            typeof(TagHelperDescriptorFactoryTest).GetTypeInfo().Assembly.GetName().Name;

        public static TheoryData HtmlCaseData
        {
            get
            {
                // tagHelperType, expectedTagName, expectedAttributeName
                return new TheoryData<Type, string, string>
                {
                    { typeof(SingleAttributeTagHelper), "single-attribute", "int-attribute" },
                    { typeof(ALLCAPSTAGHELPER), "allcaps", "allcapsattribute" },
                    { typeof(CAPSOnOUTSIDETagHelper), "caps-on-outside", "caps-on-outsideattribute" },
                    { typeof(capsONInsideTagHelper), "caps-on-inside", "caps-on-insideattribute" },
                    { typeof(One1Two2Three3TagHelper), "one1-two2-three3", "one1-two2-three3-attribute" },
                    { typeof(ONE1TWO2THREE3TagHelper), "one1two2three3", "one1two2three3-attribute" },
                    { typeof(First_Second_ThirdHiTagHelper), "first_second_third-hi", "first_second_third-attribute" },
                    { typeof(UNSuffixedCLASS), "un-suffixed-class", "un-suffixed-attribute" },
                };
            }
        }

        [Theory]
        [MemberData(nameof(HtmlCaseData))]
        public void CreateDescriptor_HtmlCasesTagNameAndAttributeName(
            Type tagHelperType,
            string expectedTagName,
            string expectedAttributeName)
        {
            // Arrange & Act
            var descriptors = TagHelperDescriptorFactory.CreateDescriptors(tagHelperType);

            // Assert
            var descriptor = Assert.Single(descriptors);
            Assert.Equal(expectedTagName, descriptor.TagName, StringComparer.Ordinal);
            var attributeDescriptor = Assert.Single(descriptor.Attributes);
            Assert.Equal(expectedAttributeName, attributeDescriptor.Name);
        }

        [Fact]
        public void CreateDescriptor_OverridesAttributeNameFromAttribute()
        {
            // Arrange
            var validProperty1 = typeof(OverriddenAttributeTagHelper).GetProperty(
                nameof(OverriddenAttributeTagHelper.ValidAttribute1));
            var validProperty2 = typeof(OverriddenAttributeTagHelper).GetProperty(
                nameof(OverriddenAttributeTagHelper.ValidAttribute2));
            var expectedDescriptors = new[] {
                new TagHelperDescriptor(
                    "overridden-attribute",
                    typeof(OverriddenAttributeTagHelper).FullName,
                    AssemblyName,
                    new[] {
                        new TagHelperAttributeDescriptor("SomethingElse", validProperty1),
                        new TagHelperAttributeDescriptor("Something-Else", validProperty2)
                    })
            };

            // Act
            var descriptors = TagHelperDescriptorFactory.CreateDescriptors(typeof(OverriddenAttributeTagHelper));

            // Assert
            Assert.Equal(expectedDescriptors, descriptors, CompleteTagHelperDescriptorComparer.Default);
        }

        [Fact]
        public void CreateDescriptor_DoesNotInheritOverridenAttributeName()
        {
            // Arrange
            var validProperty1 = typeof(InheritedOverriddenAttributeTagHelper).GetProperty(
                nameof(InheritedOverriddenAttributeTagHelper.ValidAttribute1));
            var validProperty2 = typeof(InheritedOverriddenAttributeTagHelper).GetProperty(
                nameof(InheritedOverriddenAttributeTagHelper.ValidAttribute2));
            var expectedDescriptors = new[] {
                new TagHelperDescriptor(
                    "inherited-overridden-attribute",
                    typeof(InheritedOverriddenAttributeTagHelper).FullName,
                    AssemblyName,
                    new[] {
                        new TagHelperAttributeDescriptor("valid-attribute1",
                                                         validProperty1),
                        new TagHelperAttributeDescriptor("Something-Else", validProperty2)
                    })
            };

            // Act
            var descriptors = TagHelperDescriptorFactory.CreateDescriptors(typeof(InheritedOverriddenAttributeTagHelper));

            // Assert
            Assert.Equal(expectedDescriptors, descriptors, CompleteTagHelperDescriptorComparer.Default);
        }

        [Fact]
        public void CreateDescriptor_AllowsOverridenAttributeNameOnUnimplementedVirtual()
        {
            // Arrange
            var validProperty1 = typeof(InheritedNotOverriddenAttributeTagHelper).GetProperty(
                nameof(InheritedNotOverriddenAttributeTagHelper.ValidAttribute1));
            var validProperty2 = typeof(InheritedNotOverriddenAttributeTagHelper).GetProperty(
                nameof(InheritedNotOverriddenAttributeTagHelper.ValidAttribute2));
            var expectedDescriptors = new[] {
                new TagHelperDescriptor(
                    "inherited-not-overridden-attribute",
                    typeof(InheritedNotOverriddenAttributeTagHelper).FullName,
                    AssemblyName,
                    new[] {
                        new TagHelperAttributeDescriptor("SomethingElse", validProperty1),
                        new TagHelperAttributeDescriptor("Something-Else", validProperty2)
                    })
            };

            // Act
            var descriptors = TagHelperDescriptorFactory.CreateDescriptors(typeof(InheritedNotOverriddenAttributeTagHelper));

            // Assert
            Assert.Equal(expectedDescriptors, descriptors, CompleteTagHelperDescriptorComparer.Default);
        }

        [Fact]
        public void CreateDescriptor_BuildsDescriptorsFromSimpleTypes()
        {
            // Arrange
            var objectAssemblyName = typeof(object).GetTypeInfo().Assembly.GetName().Name;
            var expectedDescriptor =
                new TagHelperDescriptor("object", "System.Object", objectAssemblyName);

            // Act
            var descriptors = TagHelperDescriptorFactory.CreateDescriptors(typeof(object));

            // Assert
            var descriptor = Assert.Single(descriptors);
            Assert.Equal(expectedDescriptor, descriptor, CompleteTagHelperDescriptorComparer.Default);
        }

        [Fact]
        public void CreateDescriptor_BuildsDescriptorsWithInheritedProperties()
        {
            // Arrange
            var intProperty = typeof(InheritedSingleAttributeTagHelper).GetProperty(
                nameof(InheritedSingleAttributeTagHelper.IntAttribute));
            var expectedDescriptor = new TagHelperDescriptor(
                "inherited-single-attribute",
                typeof(InheritedSingleAttributeTagHelper).FullName,
                AssemblyName,
                new[] {
                    new TagHelperAttributeDescriptor("int-attribute", intProperty)
                });

            // Act
            var descriptors = TagHelperDescriptorFactory.CreateDescriptors(typeof(InheritedSingleAttributeTagHelper));

            // Assert
            var descriptor = Assert.Single(descriptors);
            Assert.Equal(expectedDescriptor, descriptor, CompleteTagHelperDescriptorComparer.Default);
        }

        [Fact]
        public void CreateDescriptor_BuildsDescriptorsWithConventionNames()
        {
            // Arrange
            var intProperty = typeof(SingleAttributeTagHelper).GetProperty(nameof(SingleAttributeTagHelper.IntAttribute));
            var expectedDescriptor = new TagHelperDescriptor(
                "single-attribute",
                typeof(SingleAttributeTagHelper).FullName,
                AssemblyName,
                new[] {
                    new TagHelperAttributeDescriptor("int-attribute", intProperty)
                });

            // Act
            var descriptors = TagHelperDescriptorFactory.CreateDescriptors(typeof(SingleAttributeTagHelper));

            // Assert
            var descriptor = Assert.Single(descriptors);
            Assert.Equal(expectedDescriptor, descriptor, CompleteTagHelperDescriptorComparer.Default);
        }

        [Fact]
        public void CreateDescriptor_OnlyAcceptsPropertiesWithGetAndSet()
        {
            // Arrange
            var validProperty = typeof(MissingAccessorTagHelper).GetProperty(
                nameof(MissingAccessorTagHelper.ValidAttribute));
            var expectedDescriptor = new TagHelperDescriptor(
                "missing-accessor",
                typeof(MissingAccessorTagHelper).FullName,
                AssemblyName,
                new[] {
                    new TagHelperAttributeDescriptor("valid-attribute", validProperty)
                });

            // Act
            var descriptors = TagHelperDescriptorFactory.CreateDescriptors(typeof(MissingAccessorTagHelper));

            // Assert
            var descriptor = Assert.Single(descriptors);
            Assert.Equal(expectedDescriptor, descriptor, CompleteTagHelperDescriptorComparer.Default);
        }

        [Fact]
        public void CreateDescriptor_OnlyAcceptsPropertiesWithPublicGetAndSet()
        {
            // Arrange
            var validProperty = typeof(PrivateAccessorTagHelper).GetProperty(
                nameof(PrivateAccessorTagHelper.ValidAttribute));
            var expectedDescriptor = new TagHelperDescriptor(
                "private-accessor",
                typeof(PrivateAccessorTagHelper).FullName,
                AssemblyName,
                new[] {
                    new TagHelperAttributeDescriptor(
                        "valid-attribute", validProperty)
                });

            // Act
            var descriptors = TagHelperDescriptorFactory.CreateDescriptors(typeof(PrivateAccessorTagHelper));

            // Assert
            var descriptor = Assert.Single(descriptors);
            Assert.Equal(expectedDescriptor, descriptor, CompleteTagHelperDescriptorComparer.Default);
        }

        [Fact]
        public void CreateDescriptor_ResolvesMultipleTagHelperDescriptorsFromSingleType()
        {
            // Arrange
            var validProp = typeof(MultiTagTagHelper).GetProperty(nameof(MultiTagTagHelper.ValidAttribute));
            var expectedDescriptors = new[] {
                new TagHelperDescriptor(
                    "div",
                    typeof(MultiTagTagHelper).FullName,
                    AssemblyName,
                    new[] {
                        new TagHelperAttributeDescriptor("valid-attribute", validProp)
                    }),
                new TagHelperDescriptor(
                    "p",
                    typeof(MultiTagTagHelper).FullName,
                    AssemblyName,
                    new[] {
                        new TagHelperAttributeDescriptor("valid-attribute", validProp)
                    })
            };

            // Act
            var descriptors = TagHelperDescriptorFactory.CreateDescriptors(typeof(MultiTagTagHelper));

            // Assert
            Assert.Equal(expectedDescriptors, descriptors, CompleteTagHelperDescriptorComparer.Default);
        }

        [Fact]
        public void CreateDescriptor_DoesntResolveInheritedTagNames()
        {
            // Arrange
            var validProp = typeof(InheritedMultiTagTagHelper).GetProperty(nameof(InheritedMultiTagTagHelper.ValidAttribute));
            var expectedDescriptor = new TagHelperDescriptor(
                    "inherited-multi-tag",
                    typeof(InheritedMultiTagTagHelper).FullName,
                    AssemblyName,
                    new[] {
                        new TagHelperAttributeDescriptor("valid-attribute", validProp)
                    });

            // Act
            var descriptors = TagHelperDescriptorFactory.CreateDescriptors(typeof(InheritedMultiTagTagHelper));

            // Assert
            var descriptor = Assert.Single(descriptors);
            Assert.Equal(expectedDescriptor, descriptor, CompleteTagHelperDescriptorComparer.Default);
        }

        [Fact]
        public void CreateDescriptor_IgnoresDuplicateTagNamesFromAttribute()
        {
            // Arrange
            var expectedDescriptors = new[] {
                new TagHelperDescriptor(
                    "p",
                    typeof(DuplicateTagNameTagHelper).FullName,
                    AssemblyName),
                new TagHelperDescriptor(
                    "div",
                    typeof(DuplicateTagNameTagHelper).FullName,
                    AssemblyName)
            };

            // Act
            var descriptors = TagHelperDescriptorFactory.CreateDescriptors(typeof(DuplicateTagNameTagHelper));

            // Assert
            Assert.Equal(expectedDescriptors, descriptors, CompleteTagHelperDescriptorComparer.Default);
        }

        [Fact]
        public void CreateDescriptor_OverridesTagNameFromAttribute()
        {
            // Arrange
            var expectedDescriptors = new[] {
                new TagHelperDescriptor("data-condition",
                                        typeof(OverrideNameTagHelper).FullName,
                                        AssemblyName),
            };

            // Act
            var descriptors = TagHelperDescriptorFactory.CreateDescriptors(typeof(OverrideNameTagHelper));

            // Assert
            Assert.Equal(expectedDescriptors, descriptors, CompleteTagHelperDescriptorComparer.Default);
        }

        [Fact]
        public void CreateDescriptor_GetsTagNamesFromMultipleAttributes()
        {
            // Arrange
            var expectedDescriptors = new[] {
                new TagHelperDescriptor(
                    "span",
                    typeof(MultipleAttributeTagHelper).FullName,
                    AssemblyName),
                new TagHelperDescriptor(
                    "p",
                    typeof(MultipleAttributeTagHelper).FullName,
                    AssemblyName),
                new TagHelperDescriptor(
                    "div",
                    typeof(MultipleAttributeTagHelper).FullName,
                    AssemblyName)
            };

            // Act
            var descriptors = TagHelperDescriptorFactory.CreateDescriptors(typeof(MultipleAttributeTagHelper));

            // Assert
            Assert.Equal(expectedDescriptors, descriptors, CompleteTagHelperDescriptorComparer.Default);
        }

        [HtmlElementName("p", "div")]
        private class MultiTagTagHelper
        {
            public string ValidAttribute { get; set; }
        }

        private class InheritedMultiTagTagHelper : MultiTagTagHelper
        {
        }

        [HtmlElementName("p", "p", "div", "div")]
        private class DuplicateTagNameTagHelper
        {
        }

        [HtmlElementName("data-condition")]
        private class OverrideNameTagHelper
        {
        }

        [HtmlElementName("span")]
        [HtmlElementName("div", "p")]
        private class MultipleAttributeTagHelper
        {
        }

        private class InheritedSingleAttributeTagHelper : SingleAttributeTagHelper
        {
        }

        private class OverriddenAttributeTagHelper
        {
            [HtmlAttributeName("SomethingElse")]
            public virtual string ValidAttribute1 { get; set; }

            [HtmlAttributeName("Something-Else")]
            public string ValidAttribute2 { get; set; }
        }

        private class InheritedOverriddenAttributeTagHelper : OverriddenAttributeTagHelper
        {
            public override string ValidAttribute1 { get; set; }
        }

        private class InheritedNotOverriddenAttributeTagHelper : OverriddenAttributeTagHelper
        {
        }

        private class ALLCAPSTAGHELPER : TagHelper
        {
            public int ALLCAPSATTRIBUTE { get; set; }
        }

        private class CAPSOnOUTSIDETagHelper : TagHelper
        {
            public int CAPSOnOUTSIDEATTRIBUTE { get; set; }
        }

        private class capsONInsideTagHelper : TagHelper
        {
            public int capsONInsideattribute { get; set; }
        }

        private class One1Two2Three3TagHelper : TagHelper
        {
            public int One1Two2Three3Attribute { get; set; }
        }

        private class ONE1TWO2THREE3TagHelper : TagHelper
        {
            public int ONE1TWO2THREE3Attribute { get; set; }
        }

        private class First_Second_ThirdHiTagHelper : TagHelper
        {
            public int First_Second_ThirdAttribute { get; set; }
        }

        private class UNSuffixedCLASS : TagHelper
        {
            public int UNSuffixedATTRIBUTE { get; set; }

        }
    }
}