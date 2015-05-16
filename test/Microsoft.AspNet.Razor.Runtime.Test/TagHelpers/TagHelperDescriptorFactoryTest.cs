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
                                TagHelperDescriptorProvider.CatchAllDescriptorTarget,
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
                                TagHelperDescriptorProvider.CatchAllDescriptorTarget,
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
                                TagHelperDescriptorProvider.CatchAllDescriptorTarget,
                                typeof(MultiAttributeAttributeTargetingTagHelper).FullName,
                                AssemblyName,
                                attributes,
                                requiredAttributes: new[] { "custom" }),
                            new TagHelperDescriptor(
                                TagHelperDescriptorProvider.CatchAllDescriptorTarget,
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
                                TagHelperDescriptorProvider.CatchAllDescriptorTarget,
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
                errorSink);

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
        public void CreateDescriptor_HtmlCasesTagNameAndAttributeName(
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
                errorSink);

            // Assert
            Assert.Empty(errorSink.Errors);
            var descriptor = Assert.Single(descriptors);
            Assert.Equal(expectedTagName, descriptor.TagName, StringComparer.Ordinal);
            var attributeDescriptor = Assert.Single(descriptor.Attributes);
            Assert.Equal(expectedAttributeName, attributeDescriptor.Name);
        }

        [Fact]
        public void CreateDescriptor_OverridesAttributeNameFromAttribute()
        {
            // Arrange
            var errorSink = new ErrorSink();
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
            var descriptors = TagHelperDescriptorFactory.CreateDescriptors(
                AssemblyName,
                typeof(OverriddenAttributeTagHelper),
                errorSink);

            // Assert
            Assert.Empty(errorSink.Errors);
            Assert.Equal(expectedDescriptors, descriptors.ToArray(), CaseSensitiveTagHelperDescriptorComparer.Default);
        }

        [Fact]
        public void CreateDescriptor_DoesNotInheritOverridenAttributeName()
        {
            // Arrange
            var errorSink = new ErrorSink();
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
                        new TagHelperAttributeDescriptor("valid-attribute1", validProperty1),
                        new TagHelperAttributeDescriptor("Something-Else", validProperty2)
                    })
            };

            // Act
            var descriptors = TagHelperDescriptorFactory.CreateDescriptors(
                AssemblyName,
                typeof(InheritedOverriddenAttributeTagHelper),
                errorSink);

            // Assert
            Assert.Empty(errorSink.Errors);
            Assert.Equal(expectedDescriptors, descriptors.ToArray(), CaseSensitiveTagHelperDescriptorComparer.Default);
        }

        [Fact]
        public void CreateDescriptor_AllowsOverridenAttributeNameOnUnimplementedVirtual()
        {
            // Arrange
            var errorSink = new ErrorSink();
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
            var descriptors = TagHelperDescriptorFactory.CreateDescriptors(
                AssemblyName,
                typeof(InheritedNotOverriddenAttributeTagHelper),
                errorSink);

            // Assert
            Assert.Empty(errorSink.Errors);
            Assert.Equal(expectedDescriptors, descriptors, CaseSensitiveTagHelperDescriptorComparer.Default);
        }

        [Fact]
        public void CreateDescriptor_BuildsDescriptorsFromSimpleTypes()
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
                errorSink);

            // Assert
            Assert.Empty(errorSink.Errors);
            var descriptor = Assert.Single(descriptors);
            Assert.Equal(expectedDescriptor, descriptor, CaseSensitiveTagHelperDescriptorComparer.Default);
        }

        [Fact]
        public void CreateDescriptor_BuildsDescriptorsWithInheritedProperties()
        {
            // Arrange
            var errorSink = new ErrorSink();

            // Also confirm isStringProperty is calculated correctly.
            var expectedDescriptor = new TagHelperDescriptor(
                "inherited-single-attribute",
                typeof(InheritedSingleAttributeTagHelper).FullName,
                AssemblyName,
                new[] {
                    new TagHelperAttributeDescriptor(
                        "int-attribute",
                        nameof(InheritedSingleAttributeTagHelper.IntAttribute),
                        typeof(int).FullName,
                        isStringProperty: false)
                });

            // Act
            var descriptors = TagHelperDescriptorFactory.CreateDescriptors(
                AssemblyName,
                typeof(InheritedSingleAttributeTagHelper),
                errorSink);

            // Assert
            Assert.Empty(errorSink.Errors);
            var descriptor = Assert.Single(descriptors);
            Assert.Equal(expectedDescriptor, descriptor, CaseSensitiveTagHelperDescriptorComparer.Default);
        }

        [Fact]
        public void CreateDescriptor_BuildsDescriptorsWithConventionNames()
        {
            // Arrange
            var errorSink = new ErrorSink();
            var intProperty = typeof(SingleAttributeTagHelper).GetProperty(nameof(SingleAttributeTagHelper.IntAttribute));
            var expectedDescriptor = new TagHelperDescriptor(
                "single-attribute",
                typeof(SingleAttributeTagHelper).FullName,
                AssemblyName,
                new[] {
                    new TagHelperAttributeDescriptor("int-attribute", intProperty)
                });

            // Act
            var descriptors = TagHelperDescriptorFactory.CreateDescriptors(
                AssemblyName,
                typeof(SingleAttributeTagHelper),
                new ErrorSink());

            // Assert
            Assert.Empty(errorSink.Errors);
            var descriptor = Assert.Single(descriptors);
            Assert.Equal(expectedDescriptor, descriptor, CaseSensitiveTagHelperDescriptorComparer.Default);
        }

        [Fact]
        public void CreateDescriptor_OnlyAcceptsPropertiesWithGetAndSet()
        {
            // Arrange
            var errorSink = new ErrorSink();
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
            var descriptors = TagHelperDescriptorFactory.CreateDescriptors(
                AssemblyName,
                typeof(MissingAccessorTagHelper),
                errorSink);

            // Assert
            Assert.Empty(errorSink.Errors);
            var descriptor = Assert.Single(descriptors);
            Assert.Equal(expectedDescriptor, descriptor, CaseSensitiveTagHelperDescriptorComparer.Default);
        }

        [Fact]
        public void CreateDescriptor_OnlyAcceptsPropertiesWithPublicGetAndSet()
        {
            // Arrange
            var errorSink = new ErrorSink();
            var validProperty = typeof(NonPublicAccessorTagHelper).GetProperty(
                nameof(NonPublicAccessorTagHelper.ValidAttribute));
            var expectedDescriptor = new TagHelperDescriptor(
                "non-public-accessor",
                typeof(NonPublicAccessorTagHelper).FullName,
                AssemblyName,
                new[] {
                    new TagHelperAttributeDescriptor("valid-attribute", validProperty)
                });

            // Act
            var descriptors = TagHelperDescriptorFactory.CreateDescriptors(
                AssemblyName,
                typeof(NonPublicAccessorTagHelper),
                errorSink);

            // Assert
            Assert.Empty(errorSink.Errors);
            var descriptor = Assert.Single(descriptors);
            Assert.Equal(expectedDescriptor, descriptor, CaseSensitiveTagHelperDescriptorComparer.Default);
        }

        [Fact]
        public void CreateDescriptor_DoesNotIncludePropertiesWithNotBound()
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
                        isStringProperty: false)
                });

            // Act
            var descriptors = TagHelperDescriptorFactory.CreateDescriptors(
                AssemblyName,
                typeof(NotBoundAttributeTagHelper),
                errorSink);

            // Assert
            Assert.Empty(errorSink.Errors);
            var descriptor = Assert.Single(descriptors);
            Assert.Equal(expectedDescriptor, descriptor, CaseSensitiveTagHelperDescriptorComparer.Default);
        }

        [Fact(Skip = "#364")]
        public void CreateDescriptor_AddsErrorForTagHelperWithDuplicateAttributeNames()
        {
            // Arrange
            var errorSink = new ErrorSink();

            // Act
            var descriptors = TagHelperDescriptorFactory.CreateDescriptors(
                AssemblyName,
                typeof(DuplicateAttributeNameTagHelper),
                errorSink);

            // Assert
            Assert.Empty(descriptors);
            var error = Assert.Single(errorSink.Errors);
        }

        [Fact]
        public void CreateDescriptor_ResolvesMultipleTagHelperDescriptorsFromSingleType()
        {
            // Arrange
            var errorSink = new ErrorSink();

            // Also confirm isStringProperty is calculated correctly.
            var expectedDescriptors = new[] {
                new TagHelperDescriptor(
                    "div",
                    typeof(MultiTagTagHelper).FullName,
                    AssemblyName,
                    new[] {
                        new TagHelperAttributeDescriptor(
                            "valid-attribute",
                            nameof(MultiTagTagHelper.ValidAttribute),
                            typeof(string).FullName,
                            isStringProperty: true)
                    }),
                new TagHelperDescriptor(
                    "p",
                    typeof(MultiTagTagHelper).FullName,
                    AssemblyName,
                    new[] {
                        new TagHelperAttributeDescriptor(
                            "valid-attribute",
                            nameof(MultiTagTagHelper.ValidAttribute),
                            typeof(string).FullName,
                            isStringProperty: true)
                    })
            };

            // Act
            var descriptors = TagHelperDescriptorFactory.CreateDescriptors(
                AssemblyName,
                typeof(MultiTagTagHelper),
                errorSink);

            // Assert
            Assert.Empty(errorSink.Errors);

            // We don't care about order. Mono returns reflected attributes differently so we need to ensure order
            // doesn't matter by sorting.
            descriptors = descriptors.OrderBy(descriptor => descriptor.TagName).ToArray();

            Assert.Equal(expectedDescriptors, descriptors, CaseSensitiveTagHelperDescriptorComparer.Default);
        }

        [Fact]
        public void CreateDescriptor_DoesntResolveInheritedTagNames()
        {
            // Arrange
            var errorSink = new ErrorSink();
            var validProp = typeof(InheritedMultiTagTagHelper).GetProperty(nameof(InheritedMultiTagTagHelper.ValidAttribute));
            var expectedDescriptor = new TagHelperDescriptor(
                    "inherited-multi-tag",
                    typeof(InheritedMultiTagTagHelper).FullName,
                    AssemblyName,
                    new[] {
                        new TagHelperAttributeDescriptor("valid-attribute", validProp)
                    });

            // Act
            var descriptors = TagHelperDescriptorFactory.CreateDescriptors(
                AssemblyName,
                typeof(InheritedMultiTagTagHelper),
                errorSink);

            // Assert
            Assert.Empty(errorSink.Errors);
            var descriptor = Assert.Single(descriptors);
            Assert.Equal(expectedDescriptor, descriptor, CaseSensitiveTagHelperDescriptorComparer.Default);
        }

        [Fact]
        public void CreateDescriptor_IgnoresDuplicateTagNamesFromAttribute()
        {
            // Arrange
            var errorSink = new ErrorSink();
            var expectedDescriptors = new[] {
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
                errorSink);

            // Assert
            Assert.Empty(errorSink.Errors);

            // We don't care about order. Mono returns reflected attributes differently so we need to ensure order
            // doesn't matter by sorting.
            descriptors = descriptors.OrderBy(descriptor => descriptor.TagName).ToArray();

            Assert.Equal(expectedDescriptors, descriptors, CaseSensitiveTagHelperDescriptorComparer.Default);
        }

        [Fact]
        public void CreateDescriptor_OverridesTagNameFromAttribute()
        {
            // Arrange
            var errorSink = new ErrorSink();
            var expectedDescriptors = new[] {
                new TagHelperDescriptor("data-condition",
                                        typeof(OverrideNameTagHelper).FullName,
                                        AssemblyName),
            };

            // Act
            var descriptors = TagHelperDescriptorFactory.CreateDescriptors(
                AssemblyName,
                typeof(OverrideNameTagHelper),
                errorSink);

            // Assert
            Assert.Empty(errorSink.Errors);
            Assert.Equal(expectedDescriptors, descriptors.ToArray(), CaseSensitiveTagHelperDescriptorComparer.Default);
        }

        public static TheoryData InvalidNameData
        {
            get
            {
                var invalidNameError =
                    "Tag helpers cannot target {0} name '{1}' because it contains a '{2}' character.";
                var nullOrWhitespaceNameError =
                    "{0} name cannot be null or whitespace.";
                Func<string, string, string> onNameError = (invalidText, invalidCharacter) =>
                    string.Format(invalidNameError, "tag", invalidText, invalidCharacter);

                // name, expectedErrorMessages
                return new TheoryData<string, string[]>
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
                            onNameError("!he!lo!", "!")
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
                            onNameError("@he@lo@", "@")
                        }
                    },
                    { "/", new[] { onNameError("/", "/") } },
                    { "hello/", new[] { onNameError("hello/", "/") } },
                    { "/hello", new[] { onNameError("/hello", "/") } },
                    { "he/lo", new[] { onNameError("he/lo", "/") } },
                    {
                        "/he/lo/",
                        new[] {
                            onNameError("/he/lo/", "/"),
                            onNameError("/he/lo/", "/"),
                            onNameError("/he/lo/", "/")
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
                            onNameError("<he<lo<", "<")
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
                            onNameError("?he?lo?", "?")
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
                            onNameError("[he[lo[", "[")
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
                            onNameError(">he>lo>", ">")
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
                            onNameError("]he]lo]", "]")
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
                            onNameError("=he=lo=", "=")
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
                            onNameError("\"he\"lo\"", "\"")
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
                            onNameError("'he'lo'", "'")
                        }
                    },
                    { string.Empty, new[] { string.Format(nullOrWhitespaceNameError, "Tag") } },
                    { Environment.NewLine, new[] { string.Format(nullOrWhitespaceNameError, "Tag") } },
                    { "\t", new[] { string.Format(nullOrWhitespaceNameError, "Tag") } },
                    { " \t ", new[] { string.Format(nullOrWhitespaceNameError, "Tag") } },
                    { " ", new[] { string.Format(nullOrWhitespaceNameError, "Tag") } },
                    { Environment.NewLine + " ", new[] { string.Format(nullOrWhitespaceNameError, "Tag") } },
                    {
                        "! \t\r\n@/<>?[]=\"'",
                        new[]
                        {
                            onNameError("! \t\r\n@/<>?[]=\"'", "!"),
                            onNameError("! \t\r\n@/<>?[]=\"'", " "),
                            onNameError("! \t\r\n@/<>?[]=\"'", "\t"),
                            onNameError("! \t\r\n@/<>?[]=\"'", "\r"),
                            onNameError("! \t\r\n@/<>?[]=\"'", "\n"),
                            onNameError("! \t\r\n@/<>?[]=\"'", "@"),
                            onNameError("! \t\r\n@/<>?[]=\"'", "/"),
                            onNameError("! \t\r\n@/<>?[]=\"'", "<"),
                            onNameError("! \t\r\n@/<>?[]=\"'", ">"),
                            onNameError("! \t\r\n@/<>?[]=\"'", "?"),
                            onNameError("! \t\r\n@/<>?[]=\"'", "["),
                            onNameError("! \t\r\n@/<>?[]=\"'", "]"),
                            onNameError("! \t\r\n@/<>?[]=\"'", "="),
                            onNameError("! \t\r\n@/<>?[]=\"'", "\""),
                            onNameError("! \t\r\n@/<>?[]=\"'", "'"),
                        }
                    },
                    {
                        "! \tv\ra\nl@i/d<>?[]=\"'",
                        new[]
                        {
                            onNameError("! \tv\ra\nl@i/d<>?[]=\"'", "!"),
                            onNameError("! \tv\ra\nl@i/d<>?[]=\"'", " "),
                            onNameError("! \tv\ra\nl@i/d<>?[]=\"'", "\t"),
                            onNameError("! \tv\ra\nl@i/d<>?[]=\"'", "\r"),
                            onNameError("! \tv\ra\nl@i/d<>?[]=\"'", "\n"),
                            onNameError("! \tv\ra\nl@i/d<>?[]=\"'", "@"),
                            onNameError("! \tv\ra\nl@i/d<>?[]=\"'", "/"),
                            onNameError("! \tv\ra\nl@i/d<>?[]=\"'", "<"),
                            onNameError("! \tv\ra\nl@i/d<>?[]=\"'", ">"),
                            onNameError("! \tv\ra\nl@i/d<>?[]=\"'", "?"),
                            onNameError("! \tv\ra\nl@i/d<>?[]=\"'", "["),
                            onNameError("! \tv\ra\nl@i/d<>?[]=\"'", "]"),
                            onNameError("! \tv\ra\nl@i/d<>?[]=\"'", "="),
                            onNameError("! \tv\ra\nl@i/d<>?[]=\"'", "\""),
                            onNameError("! \tv\ra\nl@i/d<>?[]=\"'", "'"),
                        }
                    },
                };
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
            for (var i = 0; i < errors.Length; i++)
            {
                Assert.Equal(expectedErrorMessages[i], errors[i].Message);
                Assert.Equal(SourceLocation.Zero, errors[i].Location);
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
                    "attributes beginning with 'data-'.";

                // type, expectedAttributeDescriptors, expectedErrors
                return new TheoryData<Type, IEnumerable<TagHelperAttributeDescriptor>, string[]>
                {
                    {
                        typeof(InvalidBoundAttribute),
                        Enumerable.Empty<TagHelperAttributeDescriptor>(),
                        new[] {
                            string.Format(
                                errorFormat,
                                nameof(InvalidBoundAttribute.DataSomething),
                                typeof(InvalidBoundAttribute).FullName)
                        }
                    },
                    {
                        typeof(InvalidBoundAttributeWithValid),
                        new[] {
                            new TagHelperAttributeDescriptor(
                                "int-attribute",
                                typeof(InvalidBoundAttributeWithValid)
                                    .GetProperty(nameof(InvalidBoundAttributeWithValid.IntAttribute)))
                        },
                        new[] {
                            string.Format(
                                errorFormat,
                                nameof(InvalidBoundAttributeWithValid.DataSomething),
                                typeof(InvalidBoundAttributeWithValid).FullName)
                        }
                    },
                    {
                        typeof(OverriddenInvalidBoundAttributeWithValid),
                        new[] {
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
                        new[] {
                            string.Format(
                                errorFormat,
                                nameof(OverriddenValidBoundAttributeWithInvalid.ValidSomething),
                                typeof(OverriddenValidBoundAttributeWithInvalid).FullName)
                        }
                    },
                    {
                        typeof(OverriddenValidBoundAttributeWithInvalidUpperCase),
                        Enumerable.Empty<TagHelperAttributeDescriptor>(),
                        new[] {
                            string.Format(
                                errorFormat,
                                nameof(OverriddenValidBoundAttributeWithInvalidUpperCase.ValidSomething),
                                typeof(OverriddenValidBoundAttributeWithInvalidUpperCase).FullName)
                        }
                    },
                };
            }
        }

        [Theory]
        [MemberData(nameof(InvalidTagHelperAttributeDescriptorData))]
        public void CreateDescriptor_DoesNotAllowDataDashAttributes(
            Type type,
            IEnumerable<TagHelperAttributeDescriptor> expectedAttributeDescriptors,
            string[] expectedErrors)
        {
            // Arrange
            var errorSink = new ErrorSink();

            // Act
            var descriptors = TagHelperDescriptorFactory.CreateDescriptors(AssemblyName, type, errorSink);

            // Assert
            var actualErrors = errorSink.Errors.ToArray();
            Assert.Equal(expectedErrors.Length, actualErrors.Length);

            for (var i = 0; i < actualErrors.Length; i++)
            {
                var actualError = actualErrors[i];
                Assert.Equal(1, actualError.Length);
                Assert.Equal(SourceLocation.Zero, actualError.Location);
                Assert.Equal(expectedErrors[i], actualError.Message);
            }

            var actualDescriptor = Assert.Single(descriptors);
            Assert.Equal(
                expectedAttributeDescriptors,
                actualDescriptor.Attributes,
                CaseSensitiveTagHelperAttributeDescriptorComparer.Default);
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
    }
}