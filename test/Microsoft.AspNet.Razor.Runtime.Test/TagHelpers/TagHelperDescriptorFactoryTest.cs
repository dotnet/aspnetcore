// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.AspNet.Razor.TagHelpers;
using Xunit;

namespace Microsoft.AspNet.Razor.Runtime.TagHelpers
{
    public class TagHelperDescriptorFactoryTest
    {
        private static readonly string AssemblyName =
            typeof(TagHelperDescriptorFactoryTest).GetTypeInfo().Assembly.GetName().Name;

        public static TheoryData AttributeTargetData
        {
            get
            {
                var attributes = Enumerable.Empty<TagHelperAttributeDescriptor>();

                // tagHelperType, expectedDescriptors
                return new TheoryData<Type, IEnumerable<TagHelperDescriptor>>
                {
                    {
                        typeof(AttributeTargetingTagHelper),
                        new[]
                        {
                            new TagHelperDescriptor(
                                TagHelperDescriptorProvider.ElementCatchAllTarget,
                                typeof(AttributeTargetingTagHelper).FullName,
                                AssemblyName,
                                attributes,
                                requiredAttributes: new[] { "class" })
                        }
                    },
                    {
                        typeof(MultiAttributeTargetingTagHelper),
                        new[]
                        {
                            new TagHelperDescriptor(
                                TagHelperDescriptorProvider.ElementCatchAllTarget,
                                typeof(MultiAttributeTargetingTagHelper).FullName,
                                AssemblyName,
                                attributes,
                                requiredAttributes: new[] { "class", "style" })
                        }
                    },
                    {
                        typeof(MultiAttributeAttributeTargetingTagHelper),
                        new[]
                        {
                            new TagHelperDescriptor(
                                TagHelperDescriptorProvider.ElementCatchAllTarget,
                                typeof(MultiAttributeAttributeTargetingTagHelper).FullName,
                                AssemblyName,
                                attributes,
                                requiredAttributes: new[] { "custom" }),
                            new TagHelperDescriptor(
                                TagHelperDescriptorProvider.ElementCatchAllTarget,
                                typeof(MultiAttributeAttributeTargetingTagHelper).FullName,
                                AssemblyName,
                                attributes,
                                requiredAttributes: new[] { "class", "style" })
                        }
                    },
                    {
                        typeof(InheritedAttributeTargetingTagHelper),
                        new[]
                        {
                            new TagHelperDescriptor(
                                TagHelperDescriptorProvider.ElementCatchAllTarget,
                                typeof(InheritedAttributeTargetingTagHelper).FullName,
                                AssemblyName,
                                attributes,
                                requiredAttributes: new[] { "style" })
                        }
                    },
                    {
                        typeof(RequiredAttributeTagHelper),
                        new[]
                        {
                            new TagHelperDescriptor(
                                "input",
                                typeof(RequiredAttributeTagHelper).FullName,
                                AssemblyName,
                                attributes,
                                requiredAttributes: new[] { "class" })
                        }
                    },
                    {
                        typeof(InheritedRequiredAttributeTagHelper),
                        new[]
                        {
                            new TagHelperDescriptor(
                                "div",
                                typeof(InheritedRequiredAttributeTagHelper).FullName,
                                AssemblyName,
                                attributes,
                                requiredAttributes: new[] { "class" })
                        }
                    },
                    {
                        typeof(MultiAttributeRequiredAttributeTagHelper),
                        new[]
                        {
                            new TagHelperDescriptor(
                                "div",
                                typeof(MultiAttributeRequiredAttributeTagHelper).FullName,
                                AssemblyName,
                                attributes,
                                requiredAttributes: new[] { "class" }),
                            new TagHelperDescriptor(
                                "input",
                                typeof(MultiAttributeRequiredAttributeTagHelper).FullName,
                                AssemblyName,
                                attributes,
                                requiredAttributes: new[] { "class" })
                        }
                    },
                    {
                        typeof(MultiAttributeSameTagRequiredAttributeTagHelper),
                        new[]
                        {
                            new TagHelperDescriptor(
                                "input",
                                typeof(MultiAttributeSameTagRequiredAttributeTagHelper).FullName,
                                AssemblyName,
                                attributes,
                                requiredAttributes: new[] { "style" }),
                            new TagHelperDescriptor(
                                "input",
                                typeof(MultiAttributeSameTagRequiredAttributeTagHelper).FullName,
                                AssemblyName,
                                attributes,
                                requiredAttributes: new[] { "class" })
                        }
                    },
                    {
                        typeof(MultiRequiredAttributeTagHelper),
                        new[]
                        {
                            new TagHelperDescriptor(
                                "input",
                                typeof(MultiRequiredAttributeTagHelper).FullName,
                                AssemblyName,
                                attributes,
                                requiredAttributes: new[] { "class", "style" })
                        }
                    },
                    {
                        typeof(MultiTagMultiRequiredAttributeTagHelper),
                        new[]
                        {
                            new TagHelperDescriptor(
                                "div",
                                typeof(MultiTagMultiRequiredAttributeTagHelper).FullName,
                                AssemblyName,
                                attributes,
                                requiredAttributes: new[] { "class", "style" }),
                            new TagHelperDescriptor(
                                "input",
                                typeof(MultiTagMultiRequiredAttributeTagHelper).FullName,
                                AssemblyName,
                                attributes,
                                requiredAttributes: new[] { "class", "style" }),
                        }
                    },
                    {
                        typeof(AttributeWildcardTargetingTagHelper),
                        new[]
                        {
                            new TagHelperDescriptor(
                                TagHelperDescriptorProvider.ElementCatchAllTarget,
                                typeof(AttributeWildcardTargetingTagHelper).FullName,
                                AssemblyName,
                                attributes,
                                requiredAttributes: new[] { "class*" })
                        }
                    },
                    {
                        typeof(MultiAttributeWildcardTargetingTagHelper),
                        new[]
                        {
                            new TagHelperDescriptor(
                                TagHelperDescriptorProvider.ElementCatchAllTarget,
                                typeof(MultiAttributeWildcardTargetingTagHelper).FullName,
                                AssemblyName,
                                attributes,
                                requiredAttributes: new[] { "class*", "style*" })
                        }
                    },
                };
            }
        }

        [Theory]
        [MemberData(nameof(AttributeTargetData))]
        public void CreateDescriptors_ReturnsExpectedDescriptors(
            Type tagHelperType,
            IEnumerable<TagHelperDescriptor> expectedDescriptors)
        {
            // Arrange
            var errorSink = new ErrorSink();

            // Act
            var descriptors = TagHelperDescriptorFactory.CreateDescriptors(
                AssemblyName,
                tagHelperType,
                designTime: false,
                errorSink: errorSink);

            // Assert
            Assert.Empty(errorSink.Errors);

            // We don't care about order. Mono returns reflected attributes differently so we need to ensure order
            // doesn't matter by sorting.
            descriptors = descriptors.OrderBy(
                descriptor => CaseSensitiveTagHelperDescriptorComparer.Default.GetHashCode(descriptor)).ToArray();
            expectedDescriptors = expectedDescriptors.OrderBy(
                descriptor => CaseSensitiveTagHelperDescriptorComparer.Default.GetHashCode(descriptor)).ToArray();

            Assert.Equal(expectedDescriptors, descriptors, CaseSensitiveTagHelperDescriptorComparer.Default);
        }

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
        public void CreateDescriptors_HtmlCasesTagNameAndAttributeName(
            Type tagHelperType,
            string expectedTagName,
            string expectedAttributeName)
        {
            // Arrange
            var errorSink = new ErrorSink();

            // Act
            var descriptors = TagHelperDescriptorFactory.CreateDescriptors(
                AssemblyName,
                tagHelperType,
                designTime: false,
                errorSink: errorSink);

            // Assert
            Assert.Empty(errorSink.Errors);
            var descriptor = Assert.Single(descriptors);
            Assert.Equal(expectedTagName, descriptor.TagName, StringComparer.Ordinal);
            var attributeDescriptor = Assert.Single(descriptor.Attributes);
            Assert.Equal(expectedAttributeName, attributeDescriptor.Name);
        }

        [Fact]
        public void CreateDescriptors_OverridesAttributeNameFromAttribute()
        {
            // Arrange
            var errorSink = new ErrorSink();
            var validProperty1 = typeof(OverriddenAttributeTagHelper).GetProperty(
                nameof(OverriddenAttributeTagHelper.ValidAttribute1));
            var validProperty2 = typeof(OverriddenAttributeTagHelper).GetProperty(
                nameof(OverriddenAttributeTagHelper.ValidAttribute2));
            var expectedDescriptors = new[]
            {
                new TagHelperDescriptor(
                    "overridden-attribute",
                    typeof(OverriddenAttributeTagHelper).FullName,
                    AssemblyName,
                    new[]
                    {
                        new TagHelperAttributeDescriptor("SomethingElse", validProperty1),
                        new TagHelperAttributeDescriptor("Something-Else", validProperty2)
                    })
            };

            // Act
            var descriptors = TagHelperDescriptorFactory.CreateDescriptors(
                AssemblyName,
                typeof(OverriddenAttributeTagHelper),
                designTime: false,
                errorSink: errorSink);

            // Assert
            Assert.Empty(errorSink.Errors);
            Assert.Equal(expectedDescriptors, descriptors, CaseSensitiveTagHelperDescriptorComparer.Default);
        }

        [Fact]
        public void CreateDescriptors_DoesNotInheritOverridenAttributeName()
        {
            // Arrange
            var errorSink = new ErrorSink();
            var validProperty1 = typeof(InheritedOverriddenAttributeTagHelper).GetProperty(
                nameof(InheritedOverriddenAttributeTagHelper.ValidAttribute1));
            var validProperty2 = typeof(InheritedOverriddenAttributeTagHelper).GetProperty(
                nameof(InheritedOverriddenAttributeTagHelper.ValidAttribute2));
            var expectedDescriptors = new[]
            {
                new TagHelperDescriptor(
                    "inherited-overridden-attribute",
                    typeof(InheritedOverriddenAttributeTagHelper).FullName,
                    AssemblyName,
                    new[]
                    {
                        new TagHelperAttributeDescriptor("valid-attribute1", validProperty1),
                        new TagHelperAttributeDescriptor("Something-Else", validProperty2)
                    })
            };

            // Act
            var descriptors = TagHelperDescriptorFactory.CreateDescriptors(
                AssemblyName,
                typeof(InheritedOverriddenAttributeTagHelper),
                designTime: false,
                errorSink: errorSink);

            // Assert
            Assert.Empty(errorSink.Errors);
            Assert.Equal(expectedDescriptors, descriptors, CaseSensitiveTagHelperDescriptorComparer.Default);
        }

        [Fact]
        public void CreateDescriptors_AllowsOverridenAttributeNameOnUnimplementedVirtual()
        {
            // Arrange
            var errorSink = new ErrorSink();
            var validProperty1 = typeof(InheritedNotOverriddenAttributeTagHelper).GetProperty(
                nameof(InheritedNotOverriddenAttributeTagHelper.ValidAttribute1));
            var validProperty2 = typeof(InheritedNotOverriddenAttributeTagHelper).GetProperty(
                nameof(InheritedNotOverriddenAttributeTagHelper.ValidAttribute2));
            var expectedDescriptors = new[]
            {
                new TagHelperDescriptor(
                    "inherited-not-overridden-attribute",
                    typeof(InheritedNotOverriddenAttributeTagHelper).FullName,
                    AssemblyName,
                    new[]
                    {
                        new TagHelperAttributeDescriptor("SomethingElse", validProperty1),
                        new TagHelperAttributeDescriptor("Something-Else", validProperty2)
                    })
            };

            // Act
            var descriptors = TagHelperDescriptorFactory.CreateDescriptors(
                AssemblyName,
                typeof(InheritedNotOverriddenAttributeTagHelper),
                designTime: false,
                errorSink: errorSink);

            // Assert
            Assert.Empty(errorSink.Errors);
            Assert.Equal(expectedDescriptors, descriptors, CaseSensitiveTagHelperDescriptorComparer.Default);
        }

        [Fact]
        public void CreateDescriptors_BuildsDescriptorsFromSimpleTypes()
        {
            // Arrange
            var errorSink = new ErrorSink();
            var objectAssemblyName = typeof(object).GetTypeInfo().Assembly.GetName().Name;
            var expectedDescriptor =
                new TagHelperDescriptor("object", "System.Object", objectAssemblyName);

            // Act
            var descriptors = TagHelperDescriptorFactory.CreateDescriptors(
                objectAssemblyName,
                typeof(object),
                designTime: false,
                errorSink: errorSink);

            // Assert
            Assert.Empty(errorSink.Errors);
            var descriptor = Assert.Single(descriptors);
            Assert.Equal(expectedDescriptor, descriptor, CaseSensitiveTagHelperDescriptorComparer.Default);
        }

        [Fact]
        public void CreateDescriptors_BuildsDescriptorsWithInheritedProperties()
        {
            // Arrange
            var errorSink = new ErrorSink();

            // Also confirm isStringProperty is calculated correctly.
            var expectedDescriptor = new TagHelperDescriptor(
                "inherited-single-attribute",
                typeof(InheritedSingleAttributeTagHelper).FullName,
                AssemblyName,
                new[]
                {
                    new TagHelperAttributeDescriptor(
                        "int-attribute",
                        nameof(InheritedSingleAttributeTagHelper.IntAttribute),
                        typeof(int).FullName,
                        isIndexer: false,
                        isStringProperty: false,
                        designTimeDescriptor: null)
                });

            // Act
            var descriptors = TagHelperDescriptorFactory.CreateDescriptors(
                AssemblyName,
                typeof(InheritedSingleAttributeTagHelper),
                designTime: false,
                errorSink: errorSink);

            // Assert
            Assert.Empty(errorSink.Errors);
            var descriptor = Assert.Single(descriptors);
            Assert.Equal(expectedDescriptor, descriptor, CaseSensitiveTagHelperDescriptorComparer.Default);
        }

        [Fact]
        public void CreateDescriptors_BuildsDescriptorsWithConventionNames()
        {
            // Arrange
            var errorSink = new ErrorSink();
            var intProperty = typeof(SingleAttributeTagHelper).GetProperty(nameof(SingleAttributeTagHelper.IntAttribute));
            var expectedDescriptor = new TagHelperDescriptor(
                "single-attribute",
                typeof(SingleAttributeTagHelper).FullName,
                AssemblyName,
                new[]
                {
                    new TagHelperAttributeDescriptor("int-attribute", intProperty)
                });

            // Act
            var descriptors = TagHelperDescriptorFactory.CreateDescriptors(
                AssemblyName,
                typeof(SingleAttributeTagHelper),
                designTime: false,
                errorSink: new ErrorSink());

            // Assert
            Assert.Empty(errorSink.Errors);
            var descriptor = Assert.Single(descriptors);
            Assert.Equal(expectedDescriptor, descriptor, CaseSensitiveTagHelperDescriptorComparer.Default);
        }

        [Fact]
        public void CreateDescriptors_OnlyAcceptsPropertiesWithGetAndSet()
        {
            // Arrange
            var errorSink = new ErrorSink();
            var validProperty = typeof(MissingAccessorTagHelper).GetProperty(
                nameof(MissingAccessorTagHelper.ValidAttribute));
            var expectedDescriptor = new TagHelperDescriptor(
                "missing-accessor",
                typeof(MissingAccessorTagHelper).FullName,
                AssemblyName,
                new[]
                {
                    new TagHelperAttributeDescriptor("valid-attribute", validProperty)
                });

            // Act
            var descriptors = TagHelperDescriptorFactory.CreateDescriptors(
                AssemblyName,
                typeof(MissingAccessorTagHelper),
                designTime: false,
                errorSink: errorSink);

            // Assert
            Assert.Empty(errorSink.Errors);
            var descriptor = Assert.Single(descriptors);
            Assert.Equal(expectedDescriptor, descriptor, CaseSensitiveTagHelperDescriptorComparer.Default);
        }

        [Fact]
        public void CreateDescriptors_OnlyAcceptsPropertiesWithPublicGetAndSet()
        {
            // Arrange
            var errorSink = new ErrorSink();
            var validProperty = typeof(NonPublicAccessorTagHelper).GetProperty(
                nameof(NonPublicAccessorTagHelper.ValidAttribute));
            var expectedDescriptor = new TagHelperDescriptor(
                "non-public-accessor",
                typeof(NonPublicAccessorTagHelper).FullName,
                AssemblyName,
                new[]
                {
                    new TagHelperAttributeDescriptor("valid-attribute", validProperty)
                });

            // Act
            var descriptors = TagHelperDescriptorFactory.CreateDescriptors(
                AssemblyName,
                typeof(NonPublicAccessorTagHelper),
                designTime: false,
                errorSink: errorSink);

            // Assert
            Assert.Empty(errorSink.Errors);
            var descriptor = Assert.Single(descriptors);
            Assert.Equal(expectedDescriptor, descriptor, CaseSensitiveTagHelperDescriptorComparer.Default);
        }

        [Fact]
        public void CreateDescriptors_DoesNotIncludePropertiesWithNotBound()
        {
            // Arrange
            var errorSink = new ErrorSink();

            // Also confirm isStringProperty is calculated correctly.
            var expectedDescriptor = new TagHelperDescriptor(
                "not-bound-attribute",
                typeof(NotBoundAttributeTagHelper).FullName,
                AssemblyName,
                new[]
                {
                    new TagHelperAttributeDescriptor(
                        "bound-property",
                        nameof(NotBoundAttributeTagHelper.BoundProperty),
                        typeof(object).FullName,
                        isIndexer: false,
                        isStringProperty: false,
                        designTimeDescriptor: null)
                });

            // Act
            var descriptors = TagHelperDescriptorFactory.CreateDescriptors(
                AssemblyName,
                typeof(NotBoundAttributeTagHelper),
                designTime: false,
                errorSink: errorSink);

            // Assert
            Assert.Empty(errorSink.Errors);
            var descriptor = Assert.Single(descriptors);
            Assert.Equal(expectedDescriptor, descriptor, CaseSensitiveTagHelperDescriptorComparer.Default);
        }

        [Fact(Skip = "#364")]
        public void CreateDescriptors_AddsErrorForTagHelperWithDuplicateAttributeNames()
        {
            // Arrange
            var errorSink = new ErrorSink();

            // Act
            var descriptors = TagHelperDescriptorFactory.CreateDescriptors(
                AssemblyName,
                typeof(DuplicateAttributeNameTagHelper),
                designTime: false,
                errorSink: errorSink);

            // Assert
            Assert.Empty(descriptors);
            var error = Assert.Single(errorSink.Errors);
        }

        [Fact]
        public void CreateDescriptors_ResolvesMultipleTagHelperDescriptorsFromSingleType()
        {
            // Arrange
            var errorSink = new ErrorSink();

            // Also confirm isStringProperty is calculated correctly.
            var expectedDescriptors = new[]
            {
                new TagHelperDescriptor(
                    "div",
                    typeof(MultiTagTagHelper).FullName,
                    AssemblyName,
                    new[]
                    {
                        new TagHelperAttributeDescriptor(
                            "valid-attribute",
                            nameof(MultiTagTagHelper.ValidAttribute),
                            typeof(string).FullName,
                            isIndexer: false,
                            isStringProperty: true,
                            designTimeDescriptor: null)
                    }),
                new TagHelperDescriptor(
                    "p",
                    typeof(MultiTagTagHelper).FullName,
                    AssemblyName,
                    new[]
                    {
                        new TagHelperAttributeDescriptor(
                            "valid-attribute",
                            nameof(MultiTagTagHelper.ValidAttribute),
                            typeof(string).FullName,
                            isIndexer: false,
                            isStringProperty: true,
                            designTimeDescriptor: null)
                    })
            };

            // Act
            var descriptors = TagHelperDescriptorFactory.CreateDescriptors(
                AssemblyName,
                typeof(MultiTagTagHelper),
                designTime: false,
                errorSink: errorSink);

            // Assert
            Assert.Empty(errorSink.Errors);

            // We don't care about order. Mono returns reflected attributes differently so we need to ensure order
            // doesn't matter by sorting.
            descriptors = descriptors.OrderBy(descriptor => descriptor.TagName).ToArray();

            Assert.Equal(expectedDescriptors, descriptors, CaseSensitiveTagHelperDescriptorComparer.Default);
        }

        [Fact]
        public void CreateDescriptors_DoesNotResolveInheritedTagNames()
        {
            // Arrange
            var errorSink = new ErrorSink();
            var validProp = typeof(InheritedMultiTagTagHelper).GetProperty(nameof(InheritedMultiTagTagHelper.ValidAttribute));
            var expectedDescriptor = new TagHelperDescriptor(
                    "inherited-multi-tag",
                    typeof(InheritedMultiTagTagHelper).FullName,
                    AssemblyName,
                    new[]
                    {
                        new TagHelperAttributeDescriptor("valid-attribute", validProp)
                    });

            // Act
            var descriptors = TagHelperDescriptorFactory.CreateDescriptors(
                AssemblyName,
                typeof(InheritedMultiTagTagHelper),
                designTime: false,
                errorSink: errorSink);

            // Assert
            Assert.Empty(errorSink.Errors);
            var descriptor = Assert.Single(descriptors);
            Assert.Equal(expectedDescriptor, descriptor, CaseSensitiveTagHelperDescriptorComparer.Default);
        }

        [Fact]
        public void CreateDescriptors_IgnoresDuplicateTagNamesFromAttribute()
        {
            // Arrange
            var errorSink = new ErrorSink();
            var expectedDescriptors = new[]
            {
                new TagHelperDescriptor(
                    "div",
                    typeof(DuplicateTagNameTagHelper).FullName,
                    AssemblyName),
                new TagHelperDescriptor(
                    "p",
                    typeof(DuplicateTagNameTagHelper).FullName,
                    AssemblyName)
            };

            // Act
            var descriptors = TagHelperDescriptorFactory.CreateDescriptors(
                AssemblyName,
                typeof(DuplicateTagNameTagHelper),
                designTime: false,
                errorSink: errorSink);

            // Assert
            Assert.Empty(errorSink.Errors);

            // We don't care about order. Mono returns reflected attributes differently so we need to ensure order
            // doesn't matter by sorting.
            descriptors = descriptors.OrderBy(descriptor => descriptor.TagName).ToArray();

            Assert.Equal(expectedDescriptors, descriptors, CaseSensitiveTagHelperDescriptorComparer.Default);
        }

        [Fact]
        public void CreateDescriptors_OverridesTagNameFromAttribute()
        {
            // Arrange
            var errorSink = new ErrorSink();
            var expectedDescriptors = new[]
            {
                new TagHelperDescriptor("data-condition",
                                        typeof(OverrideNameTagHelper).FullName,
                                        AssemblyName),
            };

            // Act
            var descriptors = TagHelperDescriptorFactory.CreateDescriptors(
                AssemblyName,
                typeof(OverrideNameTagHelper),
                designTime: false,
                errorSink: errorSink);

            // Assert
            Assert.Empty(errorSink.Errors);
            Assert.Equal(expectedDescriptors, descriptors, CaseSensitiveTagHelperDescriptorComparer.Default);
        }

        // name, expectedErrorMessages
        public static TheoryData<string, string[]> InvalidNameData
        {
            get
            {
                Func<string, string, string> onNameError =
                    (invalidText, invalidCharacter) => $"Tag helpers cannot target tag name '{ invalidText }' " +
                        $"because it contains a '{ invalidCharacter }' character.";
                var whitespaceErrorString = "Tag name cannot be null or whitespace.";

                var data = GetInvalidNameOrPrefixData(onNameError, whitespaceErrorString, onDataError: null);
                data.Add(string.Empty, new[] { whitespaceErrorString });

                return data;
            }
        }

        [Theory]
        [MemberData(nameof(InvalidNameData))]
        public void ValidTargetElementAttributeNames_CreatesErrorOnInvalidNames(
            string name, string[] expectedErrorMessages)
        {
            // Arrange
            var errorSink = new ErrorSink();
            var attribute = new TargetElementAttribute(name);

            // Act
            TagHelperDescriptorFactory.ValidTargetElementAttributeNames(attribute, errorSink);

            // Assert
            var errors = errorSink.Errors.ToArray();
            Assert.Equal(expectedErrorMessages.Length, errors.Length);
            for (var i = 0; i < expectedErrorMessages.Length; i++)
            {
                Assert.Equal(1, errors[i].Length);
                Assert.Equal(SourceLocation.Zero, errors[i].Location);
                Assert.Equal(expectedErrorMessages[i], errors[i].Message, StringComparer.Ordinal);
            }
        }

        public static TheoryData ValidNameData
        {
            get
            {
                // name, expectedNames
                return new TheoryData<string, IEnumerable<string>>
                {
                    { "p", new[] { "p" } },
                    { " p", new[] { "p" } },
                    { "p ", new[] { "p" } },
                    { " p ", new[] { "p" } },
                    { "p,div", new[] { "p", "div" } },
                    { " p,div", new[] { "p", "div" } },
                    { "p ,div", new[] { "p", "div" } },
                    { " p ,div", new[] { "p", "div" } },
                    { "p, div", new[] { "p", "div" } },
                    { "p,div ", new[] { "p", "div" } },
                    { "p, div ", new[] { "p", "div" } },
                    { " p, div ", new[] { "p", "div" } },
                    { " p , div ", new[] { "p", "div" } },
                };
            }
        }

        [Theory]
        [MemberData(nameof(ValidNameData))]
        public void GetCommaSeparatedValues_OutputsCommaSeparatedListOfNames(
            string name,
            IEnumerable<string> expectedNames)
        {
            // Act
            var result = TagHelperDescriptorFactory.GetCommaSeparatedValues(name);

            // Assert
            Assert.Equal(expectedNames, result);
        }

        [Fact]
        public void GetCommaSeparatedValues_OutputsEmptyArrayForNullValue()
        {
            // Act
            var result = TagHelperDescriptorFactory.GetCommaSeparatedValues(text: null);

            // Assert
            Assert.Empty(result);
        }

        public static TheoryData InvalidTagHelperAttributeDescriptorData
        {
            get
            {
                var errorFormat = "Invalid tag helper bound property '{0}.{1}'. Tag helpers cannot bind to HTML " +
                    "attributes with name '{2}' because name starts with 'data-'.";

                // type, expectedAttributeDescriptors, expectedErrors
                return new TheoryData<Type, IEnumerable<TagHelperAttributeDescriptor>, string[]>
                {
                    {
                        typeof(InvalidBoundAttribute),
                        Enumerable.Empty<TagHelperAttributeDescriptor>(),
                        new[]
                        {
                            string.Format(
                                errorFormat,
                                typeof(InvalidBoundAttribute).FullName,
                                nameof(InvalidBoundAttribute.DataSomething),
                                "data-something")
                        }
                    },
                    {
                        typeof(InvalidBoundAttributeWithValid),
                        new[]
                        {
                            new TagHelperAttributeDescriptor(
                                "int-attribute",
                                typeof(InvalidBoundAttributeWithValid)
                                    .GetProperty(nameof(InvalidBoundAttributeWithValid.IntAttribute)))
                        },
                        new[]
                        {
                            string.Format(
                                errorFormat,
                                typeof(InvalidBoundAttributeWithValid).FullName,
                                nameof(InvalidBoundAttributeWithValid.DataSomething),
                                "data-something")
                        }
                    },
                    {
                        typeof(OverriddenInvalidBoundAttributeWithValid),
                        new[]
                        {
                            new TagHelperAttributeDescriptor(
                                "valid-something",
                                typeof(OverriddenInvalidBoundAttributeWithValid)
                                    .GetProperty(nameof(OverriddenInvalidBoundAttributeWithValid.DataSomething)))
                        },
                        new string[0]
                    },
                    {
                        typeof(OverriddenValidBoundAttributeWithInvalid),
                        Enumerable.Empty<TagHelperAttributeDescriptor>(),
                        new[]
                        {
                            string.Format(
                                errorFormat,
                                typeof(OverriddenValidBoundAttributeWithInvalid).FullName,
                                nameof(OverriddenValidBoundAttributeWithInvalid.ValidSomething),
                                "data-something")
                        }
                    },
                    {
                        typeof(OverriddenValidBoundAttributeWithInvalidUpperCase),
                        Enumerable.Empty<TagHelperAttributeDescriptor>(),
                        new[]
                        {
                            string.Format(
                                errorFormat,
                                typeof(OverriddenValidBoundAttributeWithInvalidUpperCase).FullName,
                                nameof(OverriddenValidBoundAttributeWithInvalidUpperCase.ValidSomething),
                                "DATA-SOMETHING")
                        }
                    },
                };
            }
        }

        [Theory]
        [MemberData(nameof(InvalidTagHelperAttributeDescriptorData))]
        public void CreateDescriptors_DoesNotAllowDataDashAttributes(
            Type type,
            IEnumerable<TagHelperAttributeDescriptor> expectedAttributeDescriptors,
            string[] expectedErrors)
        {
            // Arrange
            var errorSink = new ErrorSink();

            // Act
            var descriptors = TagHelperDescriptorFactory.CreateDescriptors(
                AssemblyName,
                type,
                designTime: false,
                errorSink: errorSink);

            // Assert
            var actualErrors = errorSink.Errors.ToArray();
            Assert.Equal(expectedErrors.Length, actualErrors.Length);

            for (var i = 0; i < actualErrors.Length; i++)
            {
                var actualError = actualErrors[i];
                Assert.Equal(1, actualError.Length);
                Assert.Equal(SourceLocation.Zero, actualError.Location);
                Assert.Equal(expectedErrors[i], actualError.Message, StringComparer.Ordinal);
            }

            var actualDescriptor = Assert.Single(descriptors);
            Assert.Equal(
                expectedAttributeDescriptors,
                actualDescriptor.Attributes,
                TagHelperAttributeDescriptorComparer.Default);
        }

        // tagTelperType, expectedAttributeDescriptors, expectedErrorMessages
        public static TheoryData<Type, IEnumerable<TagHelperAttributeDescriptor>, string[]> TagHelperWithPrefixData
        {
            get
            {
                Func<string, string, string> onError = (typeName, propertyName) =>
                    $"Invalid tag helper bound property '{ typeName }.{ propertyName }'. " +
                    $"'{ nameof(HtmlAttributeNameAttribute) }." +
                    $"{ nameof(HtmlAttributeNameAttribute.DictionaryAttributePrefix) }' must be null unless " +
                    "property type implements 'IDictionary<string, TValue>'.";

                // tagTelperType, expectedAttributeDescriptors, expectedErrorMessages
                return new TheoryData<Type, IEnumerable<TagHelperAttributeDescriptor>, string[]>
                {
                    {
                        typeof(DefaultValidHtmlAttributePrefix),
                        new[]
                        {
                            new TagHelperAttributeDescriptor(
                                name: "dictionary-property",
                                propertyName: nameof(DefaultValidHtmlAttributePrefix.DictionaryProperty),
                                typeName: typeof(IDictionary<string, string>).FullName,
                                isIndexer: false,
                                isStringProperty: false,
                                designTimeDescriptor: null),
                            new TagHelperAttributeDescriptor(
                                name: "dictionary-property-",
                                propertyName: nameof(DefaultValidHtmlAttributePrefix.DictionaryProperty),
                                typeName: typeof(string).FullName,
                                isIndexer: true,
                                isStringProperty: true,
                                designTimeDescriptor: null),
                        },
                        new string[0]
                    },
                    {
                        typeof(SingleValidHtmlAttributePrefix),
                        new[]
                        {
                            new TagHelperAttributeDescriptor(
                                name: "valid-name",
                                propertyName: nameof(SingleValidHtmlAttributePrefix.DictionaryProperty),
                                typeName: typeof(IDictionary<string, string>).FullName,
                                isIndexer: false,
                                isStringProperty: false,
                                designTimeDescriptor: null),
                            new TagHelperAttributeDescriptor(
                                name: "valid-prefix",
                                propertyName: nameof(SingleValidHtmlAttributePrefix.DictionaryProperty),
                                typeName: typeof(string).FullName,
                                isIndexer: true,
                                isStringProperty: true,
                                designTimeDescriptor: null),
                        },
                        new string[0]
                    },
                    {
                        typeof(MultipleValidHtmlAttributePrefix),
                        new[]
                        {
                            new TagHelperAttributeDescriptor(
                                name: "valid-name1",
                                propertyName: nameof(MultipleValidHtmlAttributePrefix.DictionaryProperty),
                                typeName: typeof(Dictionary<string, object>).FullName,
                                isIndexer: false,
                                isStringProperty: false,
                                designTimeDescriptor: null),
                            new TagHelperAttributeDescriptor(
                                name: "valid-name2",
                                propertyName: nameof(MultipleValidHtmlAttributePrefix.DictionarySubclassProperty),
                                typeName: typeof(DictionarySubclass).FullName,
                                isIndexer: false,
                                isStringProperty: false,
                                designTimeDescriptor: null),
                            new TagHelperAttributeDescriptor(
                                name: "valid-name3",
                                propertyName: nameof(MultipleValidHtmlAttributePrefix.DictionaryWithoutParameterlessConstructorProperty),
                                typeName: typeof(DictionaryWithoutParameterlessConstructor).FullName,
                                isIndexer: false,
                                isStringProperty: false,
                                designTimeDescriptor: null),
                            new TagHelperAttributeDescriptor(
                                name: "valid-name4",
                                propertyName: nameof(MultipleValidHtmlAttributePrefix.GenericDictionarySubclassProperty),
                                typeName: typeof(GenericDictionarySubclass<object>).FullName,
                                isIndexer: false,
                                isStringProperty: false,
                                designTimeDescriptor: null),
                            new TagHelperAttributeDescriptor(
                                name: "valid-name5",
                                propertyName: nameof(MultipleValidHtmlAttributePrefix.SortedDictionaryProperty),
                                typeName: typeof(SortedDictionary<string, int>).FullName,
                                isIndexer: false,
                                isStringProperty: false,
                                designTimeDescriptor: null),
                            new TagHelperAttributeDescriptor(
                                name: "valid-name6",
                                propertyName: nameof(MultipleValidHtmlAttributePrefix.StringProperty),
                                typeName: typeof(string).FullName,
                                isIndexer: false,
                                isStringProperty: true,
                                designTimeDescriptor: null),
                            new TagHelperAttributeDescriptor(
                                name: "valid-prefix1-",
                                propertyName: nameof(MultipleValidHtmlAttributePrefix.DictionaryProperty),
                                typeName: typeof(object).FullName,
                                isIndexer: true,
                                isStringProperty: false,
                                designTimeDescriptor: null),
                            new TagHelperAttributeDescriptor(
                                name: "valid-prefix2-",
                                propertyName: nameof(MultipleValidHtmlAttributePrefix.DictionarySubclassProperty),
                                typeName: typeof(string).FullName,
                                isIndexer: true,
                                isStringProperty: true,
                                designTimeDescriptor: null),
                            new TagHelperAttributeDescriptor(
                                name: "valid-prefix3-",
                                propertyName: nameof(MultipleValidHtmlAttributePrefix.DictionaryWithoutParameterlessConstructorProperty),
                                typeName: typeof(string).FullName,
                                isIndexer: true,
                                isStringProperty: true,
                                designTimeDescriptor: null),
                            new TagHelperAttributeDescriptor(
                                name: "valid-prefix4-",
                                propertyName: nameof(MultipleValidHtmlAttributePrefix.GenericDictionarySubclassProperty),
                                typeName: typeof(object).FullName,
                                isIndexer: true,
                                isStringProperty: false,
                                designTimeDescriptor: null),
                            new TagHelperAttributeDescriptor(
                                name: "valid-prefix5-",
                                propertyName: nameof(MultipleValidHtmlAttributePrefix.SortedDictionaryProperty),
                                typeName: typeof(int).FullName,
                                isIndexer: true,
                                isStringProperty: false,
                                designTimeDescriptor: null),
                        },
                        new string[0]
                    },
                    {
                        typeof(SingleInvalidHtmlAttributePrefix),
                        Enumerable.Empty<TagHelperAttributeDescriptor>(),
                        new[]
                        {
                            onError(
                                typeof(SingleInvalidHtmlAttributePrefix).FullName,
                                nameof(SingleInvalidHtmlAttributePrefix.StringProperty)),
                        }
                    },
                    {
                        typeof(MultipleInvalidHtmlAttributePrefix),
                        new[]
                        {
                            new TagHelperAttributeDescriptor(
                                name: "valid-name1",
                                propertyName: nameof(MultipleInvalidHtmlAttributePrefix.LongProperty),
                                typeName: typeof(long).FullName,
                                isIndexer: false,
                                isStringProperty: false,
                                designTimeDescriptor: null),
                        },
                        new[]
                        {
                            onError(
                                typeof(MultipleInvalidHtmlAttributePrefix).FullName,
                                nameof(MultipleInvalidHtmlAttributePrefix.DictionaryOfIntProperty)),
                            onError(
                                typeof(MultipleInvalidHtmlAttributePrefix).FullName,
                                nameof(MultipleInvalidHtmlAttributePrefix.ReadOnlyDictionaryProperty)),
                            onError(
                                typeof(MultipleInvalidHtmlAttributePrefix).FullName,
                                nameof(MultipleInvalidHtmlAttributePrefix.IntProperty)),
                            onError(
                                typeof(MultipleInvalidHtmlAttributePrefix).FullName,
                                nameof(MultipleInvalidHtmlAttributePrefix.DictionaryOfIntSubclassProperty)),
                        }
                    },
                };
            }
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
                tagHelperType,
                designTime: false,
                errorSink: errorSink);

            // Assert
            var errors = errorSink.Errors.ToArray();
            Assert.Equal(expectedErrorMessages.Length, errors.Length);

            for (var i = 0; i < errors.Length; i++)
            {
                Assert.Equal(1, errors[i].Length);
                Assert.Equal(SourceLocation.Zero, errors[i].Location);
                Assert.Equal(expectedErrorMessages[i], errors[i].Message, StringComparer.Ordinal);
            }

            var descriptor = Assert.Single(descriptors);
            Assert.Equal(
                expectedAttributeDescriptors,
                descriptor.Attributes,
                TagHelperAttributeDescriptorComparer.Default);
        }

        public static TheoryData<string> ValidAttributeNameOrPrefixData
        {
            get
            {
                return new TheoryData<string>
                {
                    null,
                    string.Empty,
                    "data",
                    "dataa-",
                    "ValidName",
                    "valid-name",
                    "--valid--name--",
                    ",,--__..oddly.valid::;;",
                };
            }
        }

        [Theory]
        [MemberData(nameof(ValidAttributeNameOrPrefixData))]
        public void ValidateTagHelperAttributeDescriptor_WithValidName_ReturnsTrue(string name)
        {
            // Arrange
            var descriptor = new TagHelperAttributeDescriptor(
                name,
                propertyName: "ValidProperty",
                typeName: "PropertyType",
                isIndexer: false,
                designTimeDescriptor: null);
            var errorSink = new ErrorSink();

            // Act
            var result = TagHelperDescriptorFactory.ValidateTagHelperAttributeDescriptor(
                descriptor,
                typeof(MultiTagTagHelper),
                errorSink);

            // Assert
            Assert.True(result);
            Assert.Empty(errorSink.Errors);
        }

        [Theory]
        [MemberData(nameof(ValidAttributeNameOrPrefixData))]
        public void ValidateTagHelperAttributeDescriptor_WithValidPrefix_ReturnsTrue(string prefix)
        {
            // Arrange
            var descriptor = new TagHelperAttributeDescriptor(
                name: prefix,
                propertyName: "ValidProperty",
                typeName: "PropertyType",
                isIndexer: true,
                designTimeDescriptor: null);
            var errorSink = new ErrorSink();

            // Act
            var result = TagHelperDescriptorFactory.ValidateTagHelperAttributeDescriptor(
                descriptor,
                typeof(MultiTagTagHelper),
                errorSink);

            // Assert
            Assert.True(result);
            Assert.Empty(errorSink.Errors);
        }

        // name, expectedErrorMessages
        public static TheoryData<string, string[]> InvalidAttributeNameData
        {
            get
            {
                Func<string, string, string> onNameError = (invalidText, invalidCharacter) => "Invalid tag helper " +
                    $"bound property '{ typeof(MultiTagTagHelper).FullName }.InvalidProperty'. Tag helpers cannot " +
                    $"bind to HTML attributes with name '{ invalidText }' because name contains a " +
                    $"'{ invalidCharacter }' character.";
                var whitespaceErrorString = "Invalid tag helper bound property " +
                    $"'{ typeof(MultiTagTagHelper).FullName }.InvalidProperty'. Tag helpers cannot bind to HTML " +
                    "attributes with a whitespace name.";
                Func<string, string> onDataError = invalidText => "Invalid tag helper bound property " +
                    $"'{ typeof(MultiTagTagHelper).FullName }.InvalidProperty'. Tag helpers cannot bind to HTML " +
                    $"attributes with name '{ invalidText }' because name starts with 'data-'.";

                return GetInvalidNameOrPrefixData(onNameError, whitespaceErrorString, onDataError);
            }
        }

        [Theory]
        [MemberData(nameof(InvalidAttributeNameData))]
        public void ValidateTagHelperAttributeDescriptor_WithInvalidName_AddsExpectedErrors(
            string name,
            string[] expectedErrorMessages)
        {
            // Arrange
            var descriptor = new TagHelperAttributeDescriptor(
                name,
                propertyName: "InvalidProperty",
                typeName: "PropertyType",
                isIndexer: false,
                designTimeDescriptor: null);
            var errorSink = new ErrorSink();

            // Act
            var result = TagHelperDescriptorFactory.ValidateTagHelperAttributeDescriptor(
                descriptor,
                typeof(MultiTagTagHelper),
                errorSink);

            // Assert
            Assert.False(result);

            var errors = errorSink.Errors.ToArray();
            Assert.Equal(expectedErrorMessages.Length, errors.Length);
            for (var i = 0; i < expectedErrorMessages.Length; i++)
            {
                Assert.Equal(1, errors[i].Length);
                Assert.Equal(SourceLocation.Zero, errors[i].Location);
                Assert.Equal(expectedErrorMessages[i], errors[i].Message, StringComparer.Ordinal);
            }
        }

        // prefix, expectedErrorMessages
        public static TheoryData<string, string[]> InvalidAttributePrefixData
        {
            get
            {
                Func<string, string, string> onPrefixError = (invalidText, invalidCharacter) => "Invalid tag helper " +
                    $"bound property '{ typeof(MultiTagTagHelper).FullName }.InvalidProperty'. Tag helpers cannot " +
                    $"bind to HTML attributes with prefix '{ invalidText }' because prefix contains a " +
                    $"'{ invalidCharacter }' character.";
                var whitespaceErrorString = "Invalid tag helper bound property " +
                    $"'{ typeof(MultiTagTagHelper).FullName }.InvalidProperty'. Tag helpers cannot bind to HTML " +
                    "attributes with a whitespace prefix.";
                Func<string, string> onDataError = invalidText => "Invalid tag helper bound property " +
                    $"'{ typeof(MultiTagTagHelper).FullName }.InvalidProperty'. Tag helpers cannot bind to HTML " +
                    $"attributes with prefix '{ invalidText }' because prefix starts with 'data-'.";

                return GetInvalidNameOrPrefixData(onPrefixError, whitespaceErrorString, onDataError);
            }
        }

        [Theory]
        [MemberData(nameof(InvalidAttributePrefixData))]
        public void ValidateTagHelperAttributeDescriptor_WithInvalidPrefix_AddsExpectedErrors(
            string prefix,
            string[] expectedErrorMessages)
        {
            // Arrange
            var descriptor = new TagHelperAttributeDescriptor(
                name: prefix,
                propertyName: "InvalidProperty",
                typeName: "ValuesType",
                isIndexer: true,
                designTimeDescriptor: null);
            var errorSink = new ErrorSink();

            // Act
            var result = TagHelperDescriptorFactory.ValidateTagHelperAttributeDescriptor(
                descriptor,
                typeof(MultiTagTagHelper),
                errorSink);

            // Assert
            Assert.False(result);

            var errors = errorSink.Errors.ToArray();
            Assert.Equal(expectedErrorMessages.Length, errors.Length);
            for (var i = 0; i < expectedErrorMessages.Length; i++)
            {
                Assert.Equal(1, errors[i].Length);
                Assert.Equal(SourceLocation.Zero, errors[i].Location);
                Assert.Equal(expectedErrorMessages[i], errors[i].Message, StringComparer.Ordinal);
            }
        }

        private static TheoryData<string, string[]> GetInvalidNameOrPrefixData(
            Func<string, string, string> onNameError,
            string whitespaceErrorString,
            Func<string, string> onDataError)
        {
            // name, expectedErrorMessages
            var data = new TheoryData<string, string[]>
            {
                { "!", new[] {  onNameError("!", "!") } },
                { "hello!", new[] { onNameError("hello!", "!") } },
                { "!hello", new[] { onNameError("!hello", "!") } },
                { "he!lo", new[] { onNameError("he!lo", "!") } },
                {
                    "!he!lo!",
                    new[]
                    {
                        onNameError("!he!lo!", "!"),
                        onNameError("!he!lo!", "!"),
                        onNameError("!he!lo!", "!"),
                    }
                },
                { "@", new[] { onNameError("@", "@") } },
                { "hello@", new[] { onNameError("hello@", "@") } },
                { "@hello", new[] { onNameError("@hello", "@") } },
                { "he@lo", new[] { onNameError("he@lo", "@") } },
                {
                    "@he@lo@",
                    new[]
                    {
                        onNameError("@he@lo@", "@"),
                        onNameError("@he@lo@", "@"),
                        onNameError("@he@lo@", "@"),
                    }
                },
                { "/", new[] { onNameError("/", "/") } },
                { "hello/", new[] { onNameError("hello/", "/") } },
                { "/hello", new[] { onNameError("/hello", "/") } },
                { "he/lo", new[] { onNameError("he/lo", "/") } },
                {
                    "/he/lo/",
                    new[]
                    {
                        onNameError("/he/lo/", "/"),
                        onNameError("/he/lo/", "/"),
                        onNameError("/he/lo/", "/"),
                    }
                },
                { "<", new[] { onNameError("<", "<") } },
                { "hello<", new[] { onNameError("hello<", "<") } },
                { "<hello", new[] { onNameError("<hello", "<") } },
                { "he<lo", new[] { onNameError("he<lo", "<") } },
                {
                    "<he<lo<",
                    new[]
                    {
                        onNameError("<he<lo<", "<"),
                        onNameError("<he<lo<", "<"),
                        onNameError("<he<lo<", "<"),
                    }
                },
                { "?", new[] { onNameError("?", "?") } },
                { "hello?", new[] { onNameError("hello?", "?") } },
                { "?hello", new[] { onNameError("?hello", "?") } },
                { "he?lo", new[] { onNameError("he?lo", "?") } },
                {
                    "?he?lo?",
                    new[]
                    {
                        onNameError("?he?lo?", "?"),
                        onNameError("?he?lo?", "?"),
                        onNameError("?he?lo?", "?"),
                    }
                },
                { "[", new[] { onNameError("[", "[") } },
                { "hello[", new[] { onNameError("hello[", "[") } },
                { "[hello", new[] { onNameError("[hello", "[") } },
                { "he[lo", new[] { onNameError("he[lo", "[") } },
                {
                    "[he[lo[",
                    new[]
                    {
                        onNameError("[he[lo[", "["),
                        onNameError("[he[lo[", "["),
                        onNameError("[he[lo[", "["),
                    }
                },
                { ">", new[] { onNameError(">", ">") } },
                { "hello>", new[] { onNameError("hello>", ">") } },
                { ">hello", new[] { onNameError(">hello", ">") } },
                { "he>lo", new[] { onNameError("he>lo", ">") } },
                {
                    ">he>lo>",
                    new[]
                    {
                        onNameError(">he>lo>", ">"),
                        onNameError(">he>lo>", ">"),
                        onNameError(">he>lo>", ">"),
                    }
                },
                { "]", new[] { onNameError("]", "]") } },
                { "hello]", new[] { onNameError("hello]", "]") } },
                { "]hello", new[] { onNameError("]hello", "]") } },
                { "he]lo", new[] { onNameError("he]lo", "]") } },
                {
                    "]he]lo]",
                    new[]
                    {
                        onNameError("]he]lo]", "]"),
                        onNameError("]he]lo]", "]"),
                        onNameError("]he]lo]", "]"),
                    }
                },
                { "=", new[] { onNameError("=", "=") } },
                { "hello=", new[] { onNameError("hello=", "=") } },
                { "=hello", new[] { onNameError("=hello", "=") } },
                { "he=lo", new[] { onNameError("he=lo", "=") } },
                {
                    "=he=lo=",
                    new[]
                    {
                        onNameError("=he=lo=", "="),
                        onNameError("=he=lo=", "="),
                        onNameError("=he=lo=", "="),
                    }
                },
                { "\"", new[] { onNameError("\"", "\"") } },
                { "hello\"", new[] { onNameError("hello\"", "\"") } },
                { "\"hello", new[] { onNameError("\"hello", "\"") } },
                { "he\"lo", new[] { onNameError("he\"lo", "\"") } },
                {
                    "\"he\"lo\"",
                    new[]
                    {
                        onNameError("\"he\"lo\"", "\""),
                        onNameError("\"he\"lo\"", "\""),
                        onNameError("\"he\"lo\"", "\""),
                    }
                },
                { "'", new[] { onNameError("'", "'") } },
                { "hello'", new[] { onNameError("hello'", "'") } },
                { "'hello", new[] { onNameError("'hello", "'") } },
                { "he'lo", new[] { onNameError("he'lo", "'") } },
                {
                    "'he'lo'",
                    new[]
                    {
                        onNameError("'he'lo'", "'"),
                        onNameError("'he'lo'", "'"),
                        onNameError("'he'lo'", "'"),
                    }
                },
                { "hello*", new[] { onNameError("hello*", "*") } },
                { "*hello", new[] { onNameError("*hello", "*") } },
                { "he*lo", new[] { onNameError("he*lo", "*") } },
                {
                    "*he*lo*",
                    new[]
                    {
                        onNameError("*he*lo*", "*"),
                        onNameError("*he*lo*", "*"),
                        onNameError("*he*lo*", "*"),
                    }
                },
                { Environment.NewLine, new[] { whitespaceErrorString } },
                { "\t", new[] { whitespaceErrorString } },
                { " \t ", new[] { whitespaceErrorString } },
                { " ", new[] { whitespaceErrorString } },
                { Environment.NewLine + " ", new[] { whitespaceErrorString } },
                {
                    "! \t\r\n@/<>?[]=\"'*",
                    new[]
                    {
                        onNameError("! \t\r\n@/<>?[]=\"'*", "!"),
                        onNameError("! \t\r\n@/<>?[]=\"'*", " "),
                        onNameError("! \t\r\n@/<>?[]=\"'*", "\t"),
                        onNameError("! \t\r\n@/<>?[]=\"'*", "\r"),
                        onNameError("! \t\r\n@/<>?[]=\"'*", "\n"),
                        onNameError("! \t\r\n@/<>?[]=\"'*", "@"),
                        onNameError("! \t\r\n@/<>?[]=\"'*", "/"),
                        onNameError("! \t\r\n@/<>?[]=\"'*", "<"),
                        onNameError("! \t\r\n@/<>?[]=\"'*", ">"),
                        onNameError("! \t\r\n@/<>?[]=\"'*", "?"),
                        onNameError("! \t\r\n@/<>?[]=\"'*", "["),
                        onNameError("! \t\r\n@/<>?[]=\"'*", "]"),
                        onNameError("! \t\r\n@/<>?[]=\"'*", "="),
                        onNameError("! \t\r\n@/<>?[]=\"'*", "\""),
                        onNameError("! \t\r\n@/<>?[]=\"'*", "'"),
                        onNameError("! \t\r\n@/<>?[]=\"'*", "*"),
                    }
                },
                {
                    "! \tv\ra\nl@i/d<>?[]=\"'*",
                    new[]
                    {
                        onNameError("! \tv\ra\nl@i/d<>?[]=\"'*", "!"),
                        onNameError("! \tv\ra\nl@i/d<>?[]=\"'*", " "),
                        onNameError("! \tv\ra\nl@i/d<>?[]=\"'*", "\t"),
                        onNameError("! \tv\ra\nl@i/d<>?[]=\"'*", "\r"),
                        onNameError("! \tv\ra\nl@i/d<>?[]=\"'*", "\n"),
                        onNameError("! \tv\ra\nl@i/d<>?[]=\"'*", "@"),
                        onNameError("! \tv\ra\nl@i/d<>?[]=\"'*", "/"),
                        onNameError("! \tv\ra\nl@i/d<>?[]=\"'*", "<"),
                        onNameError("! \tv\ra\nl@i/d<>?[]=\"'*", ">"),
                        onNameError("! \tv\ra\nl@i/d<>?[]=\"'*", "?"),
                        onNameError("! \tv\ra\nl@i/d<>?[]=\"'*", "["),
                        onNameError("! \tv\ra\nl@i/d<>?[]=\"'*", "]"),
                        onNameError("! \tv\ra\nl@i/d<>?[]=\"'*", "="),
                        onNameError("! \tv\ra\nl@i/d<>?[]=\"'*", "\""),
                        onNameError("! \tv\ra\nl@i/d<>?[]=\"'*", "'"),
                        onNameError("! \tv\ra\nl@i/d<>?[]=\"'*", "*"),
                    }
                },
            };

            if (onDataError != null)
            {
                data.Add("data-", new[] { onDataError("data-") });
                data.Add("data-something", new[] { onDataError("data-something") });
                data.Add("Data-Something", new[] { onDataError("Data-Something") });
                data.Add("DATA-SOMETHING", new[] { onDataError("DATA-SOMETHING") });
            }

            return data;
        }

        [TargetElement(Attributes = "class*")]
        private class AttributeWildcardTargetingTagHelper : TagHelper
        {
        }

        [TargetElement(Attributes = "class*,style*")]
        private class MultiAttributeWildcardTargetingTagHelper : TagHelper
        {
        }

        [TargetElement(Attributes = "class")]
        private class AttributeTargetingTagHelper : TagHelper
        {
        }

        [TargetElement(Attributes = "class,style")]
        private class MultiAttributeTargetingTagHelper : TagHelper
        {
        }

        [TargetElement(Attributes = "custom")]
        [TargetElement(Attributes = "class,style")]
        private class MultiAttributeAttributeTargetingTagHelper : TagHelper
        {
        }

        [TargetElement(Attributes = "style")]
        private class InheritedAttributeTargetingTagHelper : AttributeTargetingTagHelper
        {
        }

        [TargetElement("input", Attributes = "class")]
        private class RequiredAttributeTagHelper : TagHelper
        {
        }

        [TargetElement("div", Attributes = "class")]
        private class InheritedRequiredAttributeTagHelper : RequiredAttributeTagHelper
        {
        }

        [TargetElement("div", Attributes = "class")]
        [TargetElement("input", Attributes = "class")]
        private class MultiAttributeRequiredAttributeTagHelper : TagHelper
        {
        }

        [TargetElement("input", Attributes = "style")]
        [TargetElement("input", Attributes = "class")]
        private class MultiAttributeSameTagRequiredAttributeTagHelper : TagHelper
        {
        }

        [TargetElement("input", Attributes = "class,style")]
        private class MultiRequiredAttributeTagHelper : TagHelper
        {
        }

        [TargetElement("div", Attributes = "style")]
        private class InheritedMultiRequiredAttributeTagHelper : MultiRequiredAttributeTagHelper
        {
        }

        [TargetElement("div", Attributes = "class,style")]
        [TargetElement("input", Attributes = "class,style")]
        private class MultiTagMultiRequiredAttributeTagHelper : TagHelper
        {
        }

        [TargetElement("p")]
        [TargetElement("div")]
        private class MultiTagTagHelper
        {
            public string ValidAttribute { get; set; }
        }

        private class InheritedMultiTagTagHelper : MultiTagTagHelper
        {
        }

        [TargetElement("p")]
        [TargetElement("p")]
        [TargetElement("div")]
        [TargetElement("div")]
        private class DuplicateTagNameTagHelper
        {
        }

        [TargetElement("data-condition")]
        private class OverrideNameTagHelper
        {
        }

        private class InheritedSingleAttributeTagHelper : SingleAttributeTagHelper
        {
        }

        private class DuplicateAttributeNameTagHelper
        {
            public string MyNameIsLegion { get; set; }

            [HtmlAttributeName("my-name-is-legion")]
            public string Fred { get; set; }
        }

        private class NotBoundAttributeTagHelper
        {
            public object BoundProperty { get; set; }

            [HtmlAttributeNotBound]
            public string NotBoundProperty { get; set; }

            [HtmlAttributeName("unused")]
            [HtmlAttributeNotBound]
            public string NamedNotBoundProperty { get; set; }
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

        private class InvalidBoundAttribute : TagHelper
        {
            public string DataSomething { get; set; }
        }

        private class InvalidBoundAttributeWithValid : SingleAttributeTagHelper
        {
            public string DataSomething { get; set; }
        }

        private class OverriddenInvalidBoundAttributeWithValid : TagHelper
        {
            [HtmlAttributeName("valid-something")]
            public string DataSomething { get; set; }
        }

        private class OverriddenValidBoundAttributeWithInvalid : TagHelper
        {
            [HtmlAttributeName("data-something")]
            public string ValidSomething { get; set; }
        }

        private class OverriddenValidBoundAttributeWithInvalidUpperCase : TagHelper
        {
            [HtmlAttributeName("DATA-SOMETHING")]
            public string ValidSomething { get; set; }
        }

        private class DefaultValidHtmlAttributePrefix : TagHelper
        {
            public IDictionary<string, string> DictionaryProperty { get; set; }
        }

        private class SingleValidHtmlAttributePrefix : TagHelper
        {
            [HtmlAttributeName("valid-name", DictionaryAttributePrefix = "valid-prefix")]
            public IDictionary<string, string> DictionaryProperty { get; set; }
        }

        private class MultipleValidHtmlAttributePrefix : TagHelper
        {
            [HtmlAttributeName("valid-name1", DictionaryAttributePrefix = "valid-prefix1-")]
            public Dictionary<string, object> DictionaryProperty { get; set; }

            [HtmlAttributeName("valid-name2", DictionaryAttributePrefix = "valid-prefix2-")]
            public DictionarySubclass DictionarySubclassProperty { get; set; }

            [HtmlAttributeName("valid-name3", DictionaryAttributePrefix = "valid-prefix3-")]
            public DictionaryWithoutParameterlessConstructor DictionaryWithoutParameterlessConstructorProperty { get; set; }

            [HtmlAttributeName("valid-name4", DictionaryAttributePrefix = "valid-prefix4-")]
            public GenericDictionarySubclass<object> GenericDictionarySubclassProperty { get; set; }

            [HtmlAttributeName("valid-name5", DictionaryAttributePrefix = "valid-prefix5-")]
            public SortedDictionary<string, int> SortedDictionaryProperty { get; set; }

            [HtmlAttributeName("valid-name6")]
            public string StringProperty { get; set; }
        }

        private class SingleInvalidHtmlAttributePrefix : TagHelper
        {
            [HtmlAttributeName("valid-name", DictionaryAttributePrefix = "valid-prefix")]
            public string StringProperty { get; set; }
        }

        private class MultipleInvalidHtmlAttributePrefix : TagHelper
        {
            [HtmlAttributeName("valid-name1")]
            public long LongProperty { get; set; }

            [HtmlAttributeName("valid-name2", DictionaryAttributePrefix = "valid-prefix2-")]
            public Dictionary<int, string> DictionaryOfIntProperty { get; set; }

            [HtmlAttributeName("valid-name3", DictionaryAttributePrefix = "valid-prefix3-")]
            public IReadOnlyDictionary<string, object> ReadOnlyDictionaryProperty { get; set; }

            [HtmlAttributeName("valid-name4", DictionaryAttributePrefix = "valid-prefix4-")]
            public int IntProperty { get; set; }

            [HtmlAttributeName("valid-name5", DictionaryAttributePrefix = "valid-prefix5-")]
            public DictionaryOfIntSubclass DictionaryOfIntSubclassProperty { get; set; }
        }

        private class DictionarySubclass : Dictionary<string, string>
        {
        }

        private class DictionaryWithoutParameterlessConstructor : Dictionary<string, string>
        {
            public DictionaryWithoutParameterlessConstructor(int count)
                : base()
            {
            }
        }

        private class DictionaryOfIntSubclass : Dictionary<int, string>
        {
        }

        private class GenericDictionarySubclass<TValue> : Dictionary<string, TValue>
        {
        }
    }
}