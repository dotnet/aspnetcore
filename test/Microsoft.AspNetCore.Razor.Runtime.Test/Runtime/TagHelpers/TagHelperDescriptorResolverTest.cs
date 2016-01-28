// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Razor.Compilation.TagHelpers;
using Microsoft.AspNetCore.Razor.Parser;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.AspNetCore.Razor.Test.Internal;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Runtime.TagHelpers
{
    public class TagHelperDescriptorResolverTest : TagHelperTypeResolverTest
    {
        private static readonly string AssemblyName =
            typeof(TagHelperDescriptorFactoryTest).GetTypeInfo().Assembly.GetName().Name;

        private static readonly Type Valid_PlainTagHelperType = typeof(Valid_PlainTagHelper);

        private static readonly Type Valid_InheritedTagHelperType = typeof(Valid_InheritedTagHelper);

        private static TagHelperDescriptor Valid_PlainTagHelperDescriptor
        {
            get
            {
                return new TagHelperDescriptor
                {
                    TagName = "valid_plain",
                    TypeName = Valid_PlainTagHelperType.FullName,
                    AssemblyName = AssemblyName
                };
            }
        }

        private static TagHelperDescriptor Valid_InheritedTagHelperDescriptor
        {
            get
            {
                return new TagHelperDescriptor
                {
                    TagName = "valid_inherited",
                    TypeName = Valid_InheritedTagHelperType.FullName,
                    AssemblyName = AssemblyName
                };
            }
        }

        [Theory]
        [InlineData("foo,assemblyName", 4)]
        [InlineData("foo, assemblyName", 5)]
        [InlineData("   foo, assemblyName", 8)]
        [InlineData("   foo   , assemblyName", 11)]
        [InlineData("foo,    assemblyName", 8)]
        [InlineData("   foo   ,    assemblyName   ", 14)]
        public void Resolve_CalculatesAssemblyLocationInLookupText(string lookupText, int assemblyLocation)
        {
            // Arrange
            var errorSink = new ErrorSink();
            var tagHelperDescriptorResolver = new InspectableTagHelperDescriptorResolver();
            var directiveType = TagHelperDirectiveType.AddTagHelper;
            var resolutionContext = new TagHelperDescriptorResolutionContext(
                new[]
                {
                    new TagHelperDirectiveDescriptor
                    {
                        DirectiveText = lookupText,
                        Location = SourceLocation.Zero,
                        DirectiveType = directiveType
                    }
                },
                errorSink);
            var expectedSourceLocation = new SourceLocation(assemblyLocation, 0, assemblyLocation);

            // Act
            tagHelperDescriptorResolver.Resolve(resolutionContext);

            // Assert
            Assert.Empty(errorSink.Errors);
            Assert.Equal(expectedSourceLocation, tagHelperDescriptorResolver.DocumentLocation);
        }

        public static TheoryData ResolveDirectiveDescriptorsInvalidTagHelperPrefixData
        {
            get
            {
                var assemblyA = AssemblyName;
                var stringType = typeof(string);
                var assemblyB = stringType.GetTypeInfo().Assembly.GetName().Name;
                var defaultAssemblyLookups = new Dictionary<string, IEnumerable<Type>>
                {
                    { assemblyA, new[] { Valid_PlainTagHelperType, Valid_InheritedTagHelperType } },
                    { assemblyB, new[] { stringType } }
                };
                var directiveLocation1 = new SourceLocation(1, 2, 3);
                var directiveLocation2 = new SourceLocation(4, 5, 6);
                var multipleDirectiveError =
                    "Invalid tag helper directive '{0}'. Cannot have multiple '{0}' directives on a page.";
                var invalidTagHelperPrefixValueError =
                    "Invalid tag helper directive '{0}' value. '{1}' is not allowed in prefix '{2}'.";

                return new TheoryData<Dictionary<string, IEnumerable<Type>>, // descriptorAssemblyLookups
                                      IEnumerable<TagHelperDirectiveDescriptor>, // directiveDescriptors
                                      IEnumerable<TagHelperDescriptor>, // expectedDescriptors
                                      IEnumerable<RazorError>> // expectedErrors
                {
                    {
                        defaultAssemblyLookups,
                        new[]
                        {
                            new TagHelperDirectiveDescriptor
                            {
                                DirectiveText = "th:",
                                Location = directiveLocation1,
                                DirectiveType = TagHelperDirectiveType.TagHelperPrefix
                            },
                            new TagHelperDirectiveDescriptor
                            {
                                DirectiveText = "different",
                                Location = directiveLocation2,
                                DirectiveType = TagHelperDirectiveType.TagHelperPrefix
                            }
                        },
                        new TagHelperDescriptor[0],
                        new[]
                        {
                            new RazorError(
                                string.Format(multipleDirectiveError, SyntaxConstants.CSharp.TagHelperPrefixKeyword),
                                directiveLocation2,
                                length: 9)
                        }
                    },
                    {
                        defaultAssemblyLookups,
                        new[]
                        {
                            new TagHelperDirectiveDescriptor
                            {
                                DirectiveText = "th:",
                                Location = directiveLocation1,
                                DirectiveType = TagHelperDirectiveType.TagHelperPrefix
                            },
                            new TagHelperDirectiveDescriptor
                            {
                                DirectiveText = "different",
                                Location = directiveLocation2,
                                DirectiveType = TagHelperDirectiveType.TagHelperPrefix
                            },
                            new TagHelperDirectiveDescriptor
                            {
                                DirectiveText = "Microsoft.AspNetCore.Razor.TagHelpers.Valid_P*, " + assemblyA,
                                Location = directiveLocation1,
                                DirectiveType = TagHelperDirectiveType.AddTagHelper
                            }
                        },
                        new[] { CreatePrefixedValidPlainDescriptor("th:") },
                        new[]
                        {
                            new RazorError(
                                string.Format(multipleDirectiveError, SyntaxConstants.CSharp.TagHelperPrefixKeyword),
                                directiveLocation2,
                                length: 9)
                        }
                    },
                    {
                        defaultAssemblyLookups,
                        new[]
                        {
                            new TagHelperDirectiveDescriptor
                            {
                                DirectiveText = "th:",
                                Location = directiveLocation1,
                                DirectiveType = TagHelperDirectiveType.TagHelperPrefix
                            },
                            new TagHelperDirectiveDescriptor
                            {
                                DirectiveText = "different",
                                Location = directiveLocation2,
                                DirectiveType = TagHelperDirectiveType.TagHelperPrefix
                            },
                            new TagHelperDirectiveDescriptor
                            {
                                DirectiveText = "Microsoft.AspNetCore.Razor.TagHelpers.Valid_Plain*, " + assemblyA,
                                Location = directiveLocation1,
                                DirectiveType = TagHelperDirectiveType.AddTagHelper
                            },
                            new TagHelperDirectiveDescriptor
                            {
                                DirectiveText = "System.String, " + assemblyB,
                                Location = directiveLocation1,
                                DirectiveType = TagHelperDirectiveType.AddTagHelper
                            }
                        },
                        new[] { CreatePrefixedValidPlainDescriptor("th:"), CreatePrefixedStringDescriptor("th:") },
                        new[]
                        {
                            new RazorError(
                                string.Format(multipleDirectiveError, SyntaxConstants.CSharp.TagHelperPrefixKeyword),
                                directiveLocation2,
                                length: 9)
                        }
                    },
                    {
                        defaultAssemblyLookups,
                        new[]
                        {
                            new TagHelperDirectiveDescriptor
                            {
                                DirectiveText = "th ",
                                Location = directiveLocation1,
                                DirectiveType = TagHelperDirectiveType.TagHelperPrefix
                            },
                        },
                        new TagHelperDescriptor[0],
                        new[]
                        {
                            new RazorError(
                                string.Format(
                                    invalidTagHelperPrefixValueError,
                                    SyntaxConstants.CSharp.TagHelperPrefixKeyword,
                                    ' ',
                                    "th "),
                                directiveLocation1,
                                length: 3)
                        }
                    },
                    {
                        defaultAssemblyLookups,
                        new[]
                        {
                            new TagHelperDirectiveDescriptor
                            {
                                DirectiveText = "th\t",
                                Location = directiveLocation1,
                                DirectiveType = TagHelperDirectiveType.TagHelperPrefix
                            }
                        },
                        new TagHelperDescriptor[0],
                        new[]
                        {
                            new RazorError(
                                string.Format(
                                    invalidTagHelperPrefixValueError,
                                    SyntaxConstants.CSharp.TagHelperPrefixKeyword,
                                    '\t',
                                    "th\t"),
                                directiveLocation1,
                                length: 3)
                        }
                    },
                    {
                        defaultAssemblyLookups,
                        new[]
                        {
                            new TagHelperDirectiveDescriptor
                            {
                                DirectiveText = "th" + Environment.NewLine,
                                Location = directiveLocation1,
                                DirectiveType = TagHelperDirectiveType.TagHelperPrefix
                            }
                        },
                        new TagHelperDescriptor[0],
                        new[]
                        {
                            new RazorError(
                                string.Format(
                                    invalidTagHelperPrefixValueError,
                                    SyntaxConstants.CSharp.TagHelperPrefixKeyword,
                                    Environment.NewLine[0],
                                    "th" + Environment.NewLine),
                                directiveLocation1,
                                length: 2 + Environment.NewLine.Length)
                        }
                    },
                    {
                        defaultAssemblyLookups,
                        new[]
                        {
                            new TagHelperDirectiveDescriptor
                            {
                                DirectiveText = " th ",
                                Location = directiveLocation1,
                                DirectiveType = TagHelperDirectiveType.TagHelperPrefix
                            }
                        },
                        new TagHelperDescriptor[0],
                        new[]
                        {
                            new RazorError(
                                string.Format(
                                    invalidTagHelperPrefixValueError,
                                    SyntaxConstants.CSharp.TagHelperPrefixKeyword,
                                    ' ',
                                    " th "),
                                directiveLocation1,
                                length: 4)
                        }
                    },
                    {
                        defaultAssemblyLookups,
                        new[]
                        {
                            new TagHelperDirectiveDescriptor
                            {
                                DirectiveText = "@",
                                Location = directiveLocation1,
                                DirectiveType = TagHelperDirectiveType.TagHelperPrefix
                            }
                        },
                        new TagHelperDescriptor[0],
                        new[]
                        {
                            new RazorError(
                                string.Format(
                                    invalidTagHelperPrefixValueError,
                                    SyntaxConstants.CSharp.TagHelperPrefixKeyword,
                                    '@',
                                    "@"),
                                directiveLocation1,
                                length: 1)
                        }
                    },
                    {
                        defaultAssemblyLookups,
                        new[]
                        {
                            new TagHelperDirectiveDescriptor
                            {
                                DirectiveText = "t@h",
                                Location = directiveLocation1,
                                DirectiveType = TagHelperDirectiveType.TagHelperPrefix
                            }
                        },
                        new TagHelperDescriptor[0],
                        new[]
                        {
                            new RazorError(
                                string.Format(
                                    invalidTagHelperPrefixValueError,
                                    SyntaxConstants.CSharp.TagHelperPrefixKeyword,
                                    '@',
                                    "t@h"),
                                directiveLocation1,
                                length: 3)
                        }
                    },
                    {
                        defaultAssemblyLookups,
                        new[]
                        {
                            new TagHelperDirectiveDescriptor
                            {
                                DirectiveText = "!",
                                Location = directiveLocation1,
                                DirectiveType = TagHelperDirectiveType.TagHelperPrefix
                            }
                        },
                        new TagHelperDescriptor[0],
                        new[]
                        {
                            new RazorError(
                                string.Format(
                                    invalidTagHelperPrefixValueError,
                                    SyntaxConstants.CSharp.TagHelperPrefixKeyword,
                                    '!',
                                    "!"),
                                directiveLocation1,
                                length: 1)
                        }
                    },
                    {
                        defaultAssemblyLookups,
                        new[]
                        {
                            new TagHelperDirectiveDescriptor
                            {
                                DirectiveText = "!th",
                                Location = directiveLocation1,
                                DirectiveType = TagHelperDirectiveType.TagHelperPrefix
                            }
                        },
                        new TagHelperDescriptor[0],
                        new[]
                        {
                            new RazorError(
                                string.Format(
                                    invalidTagHelperPrefixValueError,
                                    SyntaxConstants.CSharp.TagHelperPrefixKeyword,
                                    '!',
                                    "!th"),
                                directiveLocation1,
                                length: 3)
                        }
                    },
                };
            }
        }

        [Theory]
        [MemberData(nameof(ResolveDirectiveDescriptorsInvalidTagHelperPrefixData))]
        public void Resolve_CreatesExpectedErrorsForTagHelperPrefixDirectives(
            Dictionary<string, IEnumerable<Type>> descriptorAssemblyLookups,
            IEnumerable<TagHelperDirectiveDescriptor> directiveDescriptors,
            IEnumerable<TagHelperDescriptor> expectedDescriptors,
            IEnumerable<RazorError> expectedErrors)
        {
            // Arrange
            var tagHelperDescriptorResolver =
                new TestTagHelperDescriptorResolver(
                    new LookupBasedTagHelperTypeResolver(descriptorAssemblyLookups));
            var errorSink = new ErrorSink();
            var resolutionContext = new TagHelperDescriptorResolutionContext(
                directiveDescriptors,
                errorSink);

            // Act
            var descriptors = tagHelperDescriptorResolver.Resolve(resolutionContext);

            // Assert
            Assert.Equal(expectedErrors, errorSink.Errors);
            Assert.Equal(expectedDescriptors.Count(), descriptors.Count());

            foreach (var expectedDescriptor in expectedDescriptors)
            {
                Assert.Contains(expectedDescriptor, descriptors, TagHelperDescriptorComparer.Default);
            }
        }

        public static TheoryData ResolveDirectiveDescriptorsTagHelperPrefixData
        {
            get
            {
                var assemblyA = AssemblyName;
                var stringType = typeof(string);
                var assemblyB = stringType.GetTypeInfo().Assembly.GetName().Name;
                var defaultAssemblyLookups = new Dictionary<string, IEnumerable<Type>>
                {
                    { assemblyA, new[] { Valid_PlainTagHelperType, Valid_InheritedTagHelperType } },
                    { assemblyB, new[] { stringType } }
                };

                return new TheoryData<
                    Dictionary<string, IEnumerable<Type>>, // descriptorAssemblyLookups
                    IEnumerable<TagHelperDirectiveDescriptor>, // directiveDescriptors
                    IEnumerable<TagHelperDescriptor>> // expectedDescriptors
                {
                    {
                        defaultAssemblyLookups,
                        new []
                        {
                            CreateTagHelperDirectiveDescriptor("", TagHelperDirectiveType.TagHelperPrefix),
                            CreateTagHelperDirectiveDescriptor(
                                "Microsoft.AspNetCore.Razor.TagHelpers.Valid_Plain*, " + assemblyA,
                                TagHelperDirectiveType.AddTagHelper),
                        },
                        new [] { Valid_PlainTagHelperDescriptor }
                    },
                    {
                        defaultAssemblyLookups,
                        new []
                        {
                            CreateTagHelperDirectiveDescriptor("th:", TagHelperDirectiveType.TagHelperPrefix),
                            CreateTagHelperDirectiveDescriptor(
                                "Microsoft.AspNetCore.Razor.TagHelpers.Valid_Plain*, " + assemblyA,
                                TagHelperDirectiveType.AddTagHelper),
                        },
                        new [] { CreatePrefixedValidPlainDescriptor("th:") }
                    },
                    {
                        defaultAssemblyLookups,
                        new []
                        {
                            CreateTagHelperDirectiveDescriptor("*, " + assemblyA, TagHelperDirectiveType.AddTagHelper),
                            CreateTagHelperDirectiveDescriptor("th:", TagHelperDirectiveType.TagHelperPrefix)
                        },
                        new []
                        {
                            CreatePrefixedValidPlainDescriptor("th:"),
                            CreatePrefixedValidInheritedDescriptor("th:")
                        }
                    },
                    {
                        defaultAssemblyLookups,
                        new []
                        {
                            CreateTagHelperDirectiveDescriptor("th-", TagHelperDirectiveType.TagHelperPrefix),
                            CreateTagHelperDirectiveDescriptor(
                                "Microsoft.AspNetCore.Razor.TagHelpers.Valid_Plain*, " + assemblyA,
                                TagHelperDirectiveType.AddTagHelper),
                            CreateTagHelperDirectiveDescriptor(
                                "Microsoft.AspNetCore.Razor.TagHelpers.Valid_Inherited*, " + assemblyA,
                                TagHelperDirectiveType.AddTagHelper)
                        },
                        new []
                        {
                            CreatePrefixedValidPlainDescriptor("th-"),
                            CreatePrefixedValidInheritedDescriptor("th-")
                        }
                    },
                    {
                        defaultAssemblyLookups,
                        new []
                        {
                            CreateTagHelperDirectiveDescriptor("", TagHelperDirectiveType.TagHelperPrefix),
                            CreateTagHelperDirectiveDescriptor(
                                "Microsoft.AspNetCore.Razor.TagHelpers.Valid_Plain*, " + assemblyA,
                                TagHelperDirectiveType.AddTagHelper),
                            CreateTagHelperDirectiveDescriptor(
                                "Microsoft.AspNetCore.Razor.TagHelpers.Valid_Inherited*, " + assemblyA,
                                TagHelperDirectiveType.AddTagHelper)
                        },
                        new [] { Valid_PlainTagHelperDescriptor, Valid_InheritedTagHelperDescriptor }
                    },
                    {
                        defaultAssemblyLookups,
                        new []
                        {
                            CreateTagHelperDirectiveDescriptor("th", TagHelperDirectiveType.TagHelperPrefix),
                            CreateTagHelperDirectiveDescriptor(
                                "*, " + assemblyA,
                                TagHelperDirectiveType.AddTagHelper),
                            CreateTagHelperDirectiveDescriptor(
                                "*, " + assemblyB,
                                TagHelperDirectiveType.AddTagHelper),
                        },
                        new []
                        {
                            CreatePrefixedValidPlainDescriptor("th"),
                            CreatePrefixedValidInheritedDescriptor("th"),
                            CreatePrefixedStringDescriptor("th")
                        }
                    },
                    {
                        defaultAssemblyLookups,
                        new []
                        {
                            CreateTagHelperDirectiveDescriptor(
                                "*, " + assemblyA,
                                TagHelperDirectiveType.AddTagHelper),
                            CreateTagHelperDirectiveDescriptor("th:-", TagHelperDirectiveType.TagHelperPrefix),
                            CreateTagHelperDirectiveDescriptor(
                                "*, " + assemblyB,
                                TagHelperDirectiveType.AddTagHelper),
                        },
                        new []
                        {
                            CreatePrefixedValidPlainDescriptor("th:-"),
                            CreatePrefixedValidInheritedDescriptor("th:-"),
                            CreatePrefixedStringDescriptor("th:-")
                        }
                    },
                };
            }
        }

        [Theory]
        [MemberData(nameof(ResolveDirectiveDescriptorsTagHelperPrefixData))]
        public void Resolve_ReturnsPrefixedDescriptorsBasedOnDirectiveDescriptors(
            Dictionary<string, IEnumerable<Type>> descriptorAssemblyLookups,
            IEnumerable<TagHelperDirectiveDescriptor> directiveDescriptors,
            IEnumerable<TagHelperDescriptor> expectedDescriptors)
        {
            // Arrange
            var tagHelperDescriptorResolver =
                new TestTagHelperDescriptorResolver(
                    new LookupBasedTagHelperTypeResolver(descriptorAssemblyLookups));
            var resolutionContext = new TagHelperDescriptorResolutionContext(
                directiveDescriptors,
                new ErrorSink());

            // Act
            var descriptors = tagHelperDescriptorResolver.Resolve(resolutionContext);

            // Assert
            Assert.Equal(expectedDescriptors.Count(), descriptors.Count());

            foreach (var expectedDescriptor in expectedDescriptors)
            {
                Assert.Contains(expectedDescriptor, descriptors, TagHelperDescriptorComparer.Default);
            }
        }

        [Theory]
        [InlineData("MyType, MyAssembly", "MyAssembly")]
        [InlineData("*, MyAssembly2", "MyAssembly2")]
        public void Resolve_AllowsOverridenResolveDescriptorsInAssembly(string lookupText, string expectedAssemblyName)
        {
            // Arrange
            var tagHelperDescriptorResolver = new AssemblyCheckingTagHelperDescriptorResolver();
            var context = new TagHelperDescriptorResolutionContext(
                new[] { CreateTagHelperDirectiveDescriptor(lookupText, TagHelperDirectiveType.AddTagHelper) },
                new ErrorSink());

            // Act
            tagHelperDescriptorResolver.Resolve(context);

            // Assert
            Assert.Equal(expectedAssemblyName, tagHelperDescriptorResolver.CalledWithAssemblyName);
        }

        public static TheoryData ResolveDirectiveDescriptorsData
        {
            get
            {
                var assemblyA = AssemblyName;
                var stringType = typeof(string);

                var assemblyB = stringType.GetTypeInfo().Assembly.GetName().Name;

                // We're treating 'string' as a TagHelper so we can test TagHelpers in multiple assemblies without
                // building a separate assembly with a single TagHelper.
                var stringTagHelperDescriptor =
                    new TagHelperDescriptor
                    {
                        TagName = "string",
                        TypeName = "System.String",
                        AssemblyName = assemblyB
                    };

                return new TheoryData<Dictionary<string, IEnumerable<Type>>, // descriptorAssemblyLookups
                                      IEnumerable<TagHelperDirectiveDescriptor>, // directiveDescriptors
                                      IEnumerable<TagHelperDescriptor>> // expectedDescriptors
                {
                    {
                        new Dictionary<string, IEnumerable<Type>>
                        {
                            { assemblyA, new [] { Valid_PlainTagHelperType } }
                        },
                        new []
                        {
                            CreateTagHelperDirectiveDescriptor("*, " + assemblyA, TagHelperDirectiveType.AddTagHelper)
                        },
                        new [] { Valid_PlainTagHelperDescriptor }
                    },
                    {
                        new Dictionary<string, IEnumerable<Type>>
                        {
                            { assemblyA, new [] { Valid_PlainTagHelperType } },
                            { assemblyB, new [] { stringType } }
                        },
                        new []
                        {
                            CreateTagHelperDirectiveDescriptor("*, " + assemblyA, TagHelperDirectiveType.AddTagHelper),
                            CreateTagHelperDirectiveDescriptor("*, " + assemblyB, TagHelperDirectiveType.AddTagHelper)
                        },
                        new [] { Valid_PlainTagHelperDescriptor, stringTagHelperDescriptor }
                    },
                    {
                        new Dictionary<string, IEnumerable<Type>>
                        {
                            { assemblyA, new [] { Valid_PlainTagHelperType } },
                            { assemblyB, new [] { stringType } }
                        },
                        new []
                        {
                            CreateTagHelperDirectiveDescriptor("*, " + assemblyA, TagHelperDirectiveType.AddTagHelper),
                            CreateTagHelperDirectiveDescriptor("*, " + assemblyB, TagHelperDirectiveType.RemoveTagHelper)
                        },
                        new [] { Valid_PlainTagHelperDescriptor }
                    },
                    {
                        new Dictionary<string, IEnumerable<Type>>
                        {
                            { assemblyA, new [] { Valid_PlainTagHelperType, Valid_InheritedTagHelperType } },
                            { assemblyB, new [] { stringType } }
                        },
                        new []
                        {
                            CreateTagHelperDirectiveDescriptor("*, " + assemblyA, TagHelperDirectiveType.AddTagHelper),
                            CreateTagHelperDirectiveDescriptor("*, " + assemblyB, TagHelperDirectiveType.AddTagHelper),
                            CreateTagHelperDirectiveDescriptor("*, " + assemblyA, TagHelperDirectiveType.RemoveTagHelper)
                        },
                        new [] { stringTagHelperDescriptor }
                    },
                    {
                        new Dictionary<string, IEnumerable<Type>>
                        {
                            { assemblyA, new [] { Valid_PlainTagHelperType, Valid_InheritedTagHelperType } }
                        },
                        new []
                        {
                            CreateTagHelperDirectiveDescriptor(
                                Valid_PlainTagHelperType.FullName + ", " + assemblyA,
                                TagHelperDirectiveType.AddTagHelper),
                            CreateTagHelperDirectiveDescriptor("*, " + assemblyA, TagHelperDirectiveType.AddTagHelper)
                        },
                        new [] { Valid_PlainTagHelperDescriptor, Valid_InheritedTagHelperDescriptor }
                    },
                    {
                        new Dictionary<string, IEnumerable<Type>>
                        {
                            { assemblyA, new [] { Valid_PlainTagHelperType, Valid_InheritedTagHelperType } }
                        },
                        new []
                        {
                            CreateTagHelperDirectiveDescriptor("*, " + assemblyA, TagHelperDirectiveType.AddTagHelper),
                            CreateTagHelperDirectiveDescriptor(
                                Valid_PlainTagHelperType.FullName + ", " + assemblyA,
                                TagHelperDirectiveType.RemoveTagHelper)
                        },
                        new [] { Valid_InheritedTagHelperDescriptor }
                    },
                    {
                        new Dictionary<string, IEnumerable<Type>>
                        {
                            { assemblyA, new [] { Valid_PlainTagHelperType, Valid_InheritedTagHelperType } },
                        },
                        new []
                        {
                            CreateTagHelperDirectiveDescriptor("*, " + assemblyA, TagHelperDirectiveType.AddTagHelper),
                            CreateTagHelperDirectiveDescriptor(
                                Valid_PlainTagHelperType.FullName + ", " + assemblyA,
                                TagHelperDirectiveType.RemoveTagHelper),
                            CreateTagHelperDirectiveDescriptor("*, " + assemblyA, TagHelperDirectiveType.AddTagHelper)
                        },
                        new [] { Valid_InheritedTagHelperDescriptor, Valid_PlainTagHelperDescriptor }
                    },
                    {
                        new Dictionary<string, IEnumerable<Type>>
                        {
                            { assemblyA, new [] { Valid_PlainTagHelperType, Valid_InheritedTagHelperType } },
                        },
                        new []
                        {
                            CreateTagHelperDirectiveDescriptor("*, " + assemblyA, TagHelperDirectiveType.AddTagHelper),
                            CreateTagHelperDirectiveDescriptor("*, " + assemblyA, TagHelperDirectiveType.AddTagHelper),
                        },
                        new [] { Valid_InheritedTagHelperDescriptor, Valid_PlainTagHelperDescriptor }
                    },
                    {
                        new Dictionary<string, IEnumerable<Type>>
                        {
                            { assemblyA, new [] { Valid_PlainTagHelperType, Valid_InheritedTagHelperType } },
                        },
                        new []
                        {
                            CreateTagHelperDirectiveDescriptor(
                                Valid_PlainTagHelperType.Namespace + ".Valid_Plain*, " + assemblyA,
                                TagHelperDirectiveType.AddTagHelper),
                        },
                        new [] { Valid_PlainTagHelperDescriptor }
                    },
                    {
                        new Dictionary<string, IEnumerable<Type>>
                        {
                            { assemblyA, new [] { Valid_PlainTagHelperType, Valid_InheritedTagHelperType } },
                        },
                        new []
                        {
                            CreateTagHelperDirectiveDescriptor(
                                Valid_PlainTagHelperType.Namespace + "*, " + assemblyA,
                                TagHelperDirectiveType.AddTagHelper),
                        },
                        new [] { Valid_PlainTagHelperDescriptor, Valid_PlainTagHelperDescriptor }
                    },
                    {
                        new Dictionary<string, IEnumerable<Type>>
                        {
                            { assemblyA, new [] { Valid_PlainTagHelperType, Valid_InheritedTagHelperType } },
                        },
                        new []
                        {
                            CreateTagHelperDirectiveDescriptor(
                                 Valid_PlainTagHelperType.Namespace + "." + Valid_PlainTagHelperType.Name + ", " + assemblyA,
                                TagHelperDirectiveType.AddTagHelper),
                        },
                        new [] { Valid_PlainTagHelperDescriptor }
                    },
                    {
                        new Dictionary<string, IEnumerable<Type>>
                        {
                            { assemblyA, new [] { Valid_PlainTagHelperType, Valid_InheritedTagHelperType } },
                        },
                        new []
                        {
                            CreateTagHelperDirectiveDescriptor("*, " + assemblyA, TagHelperDirectiveType.AddTagHelper),
                            CreateTagHelperDirectiveDescriptor(
                                Valid_PlainTagHelperType.Namespace + ".Valid_P*, " + assemblyA,
                                TagHelperDirectiveType.RemoveTagHelper),
                            CreateTagHelperDirectiveDescriptor("*, " + assemblyA, TagHelperDirectiveType.AddTagHelper)
                        },
                        new [] { Valid_InheritedTagHelperDescriptor, Valid_PlainTagHelperDescriptor }
                    },
                    {
                        new Dictionary<string, IEnumerable<Type>>
                        {
                            { assemblyA, new [] { Valid_PlainTagHelperType } },
                            { assemblyB, new [] { stringType } }
                        },
                        new []
                        {
                            CreateTagHelperDirectiveDescriptor("*, " + assemblyA, TagHelperDirectiveType.AddTagHelper),
                            CreateTagHelperDirectiveDescriptor("Str*, " + assemblyB, TagHelperDirectiveType.RemoveTagHelper)
                        },
                        new [] { Valid_PlainTagHelperDescriptor }
                    },
                    {
                        new Dictionary<string, IEnumerable<Type>>
                        {
                            { assemblyA, new [] { Valid_PlainTagHelperType } },
                            { assemblyB, new [] { stringType } }
                        },
                        new []
                        {
                            CreateTagHelperDirectiveDescriptor("*, " + assemblyA, TagHelperDirectiveType.AddTagHelper),
                            CreateTagHelperDirectiveDescriptor("*, " + assemblyB, TagHelperDirectiveType.RemoveTagHelper)
                        },
                        new [] { Valid_PlainTagHelperDescriptor }
                    },
                    {
                        new Dictionary<string, IEnumerable<Type>>
                        {
                            { assemblyA, new [] { Valid_PlainTagHelperType } },
                            { assemblyB, new [] { stringType } }
                        },
                        new []
                        {
                            CreateTagHelperDirectiveDescriptor("*, " + assemblyA, TagHelperDirectiveType.AddTagHelper),
                            CreateTagHelperDirectiveDescriptor("System." + stringType.FullName + ", " + assemblyB, TagHelperDirectiveType.AddTagHelper)
                        },
                        new [] { Valid_PlainTagHelperDescriptor }
                    },
                    {
                        new Dictionary<string, IEnumerable<Type>>
                        {
                            { assemblyA, new [] { Valid_PlainTagHelperType, Valid_InheritedTagHelperType } },
                            { assemblyB, new [] { stringType } }
                        },
                        new []
                        {
                            CreateTagHelperDirectiveDescriptor("*, " + assemblyA, TagHelperDirectiveType.AddTagHelper),
                            CreateTagHelperDirectiveDescriptor("*, " + assemblyB, TagHelperDirectiveType.AddTagHelper),
                            CreateTagHelperDirectiveDescriptor("Microsoft.*, " + assemblyA, TagHelperDirectiveType.RemoveTagHelper)
                        },
                        new [] { stringTagHelperDescriptor }
                    },
                    {
                        new Dictionary<string, IEnumerable<Type>>
                        {
                            { assemblyA, new [] { Valid_PlainTagHelperType, Valid_InheritedTagHelperType } },
                            { assemblyB, new [] { stringType } }
                        },
                        new []
                        {
                            CreateTagHelperDirectiveDescriptor(
                                "*, " + assemblyA, TagHelperDirectiveType.AddTagHelper),
                            CreateTagHelperDirectiveDescriptor(
                                "*, " + assemblyB, TagHelperDirectiveType.AddTagHelper),
                            CreateTagHelperDirectiveDescriptor(
                                "?Microsoft*, " + assemblyA, TagHelperDirectiveType.RemoveTagHelper),
                            CreateTagHelperDirectiveDescriptor(
                                "System." + stringType.FullName + ", " + assemblyB, TagHelperDirectiveType.RemoveTagHelper)
                        },
                        new []
                        {
                            Valid_InheritedTagHelperDescriptor,
                            Valid_PlainTagHelperDescriptor,
                            stringTagHelperDescriptor
                        }
                    },
                    {
                        new Dictionary<string, IEnumerable<Type>>
                        {
                            { assemblyA, new [] { Valid_PlainTagHelperType, Valid_InheritedTagHelperType } },
                            { assemblyB, new [] { stringType } }
                        },
                        new []
                        {
                            CreateTagHelperDirectiveDescriptor(
                                "*, " + assemblyA, TagHelperDirectiveType.AddTagHelper),
                            CreateTagHelperDirectiveDescriptor(
                                "*, " + assemblyB, TagHelperDirectiveType.AddTagHelper),
                            CreateTagHelperDirectiveDescriptor(
                                "TagHelper*, " + assemblyA, TagHelperDirectiveType.RemoveTagHelper),
                            CreateTagHelperDirectiveDescriptor(
                                "System." + stringType.FullName + ", " + assemblyB, TagHelperDirectiveType.RemoveTagHelper)
                        },
                        new []
                        {
                            Valid_InheritedTagHelperDescriptor,
                            Valid_PlainTagHelperDescriptor,
                            stringTagHelperDescriptor
                        }
                    },
                };
            }
        }

        [Theory]
        [MemberData(nameof(ResolveDirectiveDescriptorsData))]
        public void Resolve_ReturnsDescriptorsBasedOnDirectiveDescriptors(
            Dictionary<string, IEnumerable<Type>> descriptorAssemblyLookups,
            IEnumerable<TagHelperDirectiveDescriptor> directiveDescriptors,
            IEnumerable<TagHelperDescriptor> expectedDescriptors)
        {
            // Arrange
            var tagHelperDescriptorResolver =
                new TestTagHelperDescriptorResolver(
                    new LookupBasedTagHelperTypeResolver(descriptorAssemblyLookups));
            var resolutionContext = new TagHelperDescriptorResolutionContext(
                directiveDescriptors,
                new ErrorSink());

            // Act
            var descriptors = tagHelperDescriptorResolver.Resolve(resolutionContext);

            // Assert
            Assert.Equal(expectedDescriptors.Count(), descriptors.Count());

            foreach (var expectedDescriptor in expectedDescriptors)
            {
                Assert.Contains(expectedDescriptor, descriptors, TagHelperDescriptorComparer.Default);
            }
        }

        public static TheoryData ResolveDirectiveDescriptorsData_EmptyResult
        {
            get
            {
                var assemblyA = AssemblyName;
                var stringType = typeof(string);

                var assemblyB = stringType.GetTypeInfo().Assembly.GetName().Name;
                var stringTagHelperDescriptor =
                    new TagHelperDescriptor
                    {
                        TagName = "string",
                        TypeName = "System.String",
                        AssemblyName = assemblyB
                    };

                return new TheoryData<Dictionary<string, IEnumerable<Type>>, // descriptorAssemblyLookups
                                      IEnumerable<TagHelperDirectiveDescriptor>> // directiveDescriptors
                {
                    {
                        new Dictionary<string, IEnumerable<Type>>
                        {
                            { assemblyA, new [] { Valid_PlainTagHelperType } },
                        },
                        new []
                        {
                            CreateTagHelperDirectiveDescriptor("*, " + assemblyA, TagHelperDirectiveType.AddTagHelper),
                            CreateTagHelperDirectiveDescriptor("*, " + assemblyA, TagHelperDirectiveType.RemoveTagHelper),
                        }
                    },
                    {
                        new Dictionary<string, IEnumerable<Type>>
                        {
                            { assemblyA, new [] { Valid_PlainTagHelperType, Valid_InheritedTagHelperType } },
                        },
                        new []
                        {
                            CreateTagHelperDirectiveDescriptor("*, " + assemblyA, TagHelperDirectiveType.AddTagHelper),
                            CreateTagHelperDirectiveDescriptor(Valid_PlainTagHelperType.FullName + ", " + assemblyA, TagHelperDirectiveType.RemoveTagHelper),
                            CreateTagHelperDirectiveDescriptor(Valid_InheritedTagHelperType.FullName + ", " + assemblyA, TagHelperDirectiveType.RemoveTagHelper)
                        }
                    },
                    {
                        new Dictionary<string, IEnumerable<Type>>
                        {
                            { assemblyA, new [] { Valid_PlainTagHelperType, Valid_InheritedTagHelperType } },
                            { assemblyB, new [] { stringType } },
                        },
                        new []
                        {
                            CreateTagHelperDirectiveDescriptor("*, " + assemblyA, TagHelperDirectiveType.AddTagHelper),
                            CreateTagHelperDirectiveDescriptor("*, " + assemblyB, TagHelperDirectiveType.AddTagHelper),
                            CreateTagHelperDirectiveDescriptor("*, " + assemblyA, TagHelperDirectiveType.RemoveTagHelper),
                            CreateTagHelperDirectiveDescriptor("*, " + assemblyB, TagHelperDirectiveType.RemoveTagHelper)
                        }
                    },
                    {
                        new Dictionary<string, IEnumerable<Type>>
                        {
                            { assemblyA, new [] { Valid_PlainTagHelperType, Valid_InheritedTagHelperType } },
                            { assemblyB, new [] { stringType } },
                        },
                        new []
                        {
                            CreateTagHelperDirectiveDescriptor("*, " + assemblyA, TagHelperDirectiveType.AddTagHelper),
                            CreateTagHelperDirectiveDescriptor("*, " + assemblyB, TagHelperDirectiveType.AddTagHelper),
                            CreateTagHelperDirectiveDescriptor(Valid_PlainTagHelperType.FullName + ", " + assemblyA, TagHelperDirectiveType.RemoveTagHelper),
                            CreateTagHelperDirectiveDescriptor(Valid_InheritedTagHelperType.FullName + ", " + assemblyA, TagHelperDirectiveType.RemoveTagHelper),
                            CreateTagHelperDirectiveDescriptor(stringType.FullName + ", " + assemblyB, TagHelperDirectiveType.RemoveTagHelper)
                        }
                    },
                    {
                        new Dictionary<string, IEnumerable<Type>>(),
                        new []
                        {
                            CreateTagHelperDirectiveDescriptor("*, " + assemblyA, TagHelperDirectiveType.RemoveTagHelper),
                            CreateTagHelperDirectiveDescriptor(Valid_PlainTagHelperType.FullName + ", " + assemblyA, TagHelperDirectiveType.RemoveTagHelper),
                        }
                    },
                    {
                        new Dictionary<string, IEnumerable<Type>>
                        {
                            { assemblyA, new [] { Valid_PlainTagHelperType } },
                        },
                        new []
                        {
                            CreateTagHelperDirectiveDescriptor("*, " + assemblyA, TagHelperDirectiveType.AddTagHelper),
                            CreateTagHelperDirectiveDescriptor("Mic*, " + assemblyA, TagHelperDirectiveType.RemoveTagHelper),
                        }
                    },
                    {
                        new Dictionary<string, IEnumerable<Type>>
                        {
                            { assemblyA, new [] { Valid_PlainTagHelperType, Valid_InheritedTagHelperType } },
                        },
                        new []
                        {
                            CreateTagHelperDirectiveDescriptor("Mic*, " + assemblyA, TagHelperDirectiveType.AddTagHelper),
                            CreateTagHelperDirectiveDescriptor(Valid_PlainTagHelperType.FullName + ", " + assemblyA, TagHelperDirectiveType.RemoveTagHelper),
                            CreateTagHelperDirectiveDescriptor(Valid_InheritedTagHelperType.FullName + ", " + assemblyA, TagHelperDirectiveType.RemoveTagHelper)
                        }
                    },
                    {
                        new Dictionary<string, IEnumerable<Type>>
                        {
                            { assemblyA, new [] { Valid_PlainTagHelperType, Valid_InheritedTagHelperType } },
                            { assemblyB, new [] { stringType } },
                        },
                        new []
                        {
                            CreateTagHelperDirectiveDescriptor("Microsoft.*, " + assemblyA, TagHelperDirectiveType.AddTagHelper),
                            CreateTagHelperDirectiveDescriptor("System.*, " + assemblyB, TagHelperDirectiveType.AddTagHelper),
                            CreateTagHelperDirectiveDescriptor(Valid_PlainTagHelperType.Namespace + "*, " + assemblyA, TagHelperDirectiveType.RemoveTagHelper),
                            CreateTagHelperDirectiveDescriptor("System.*, " + assemblyB, TagHelperDirectiveType.RemoveTagHelper)
                        }
                    },
                    {
                        new Dictionary<string, IEnumerable<Type>>
                        {
                            { assemblyA, new [] { Valid_PlainTagHelperType, Valid_InheritedTagHelperType } },
                            { assemblyB, new [] { stringType } },
                        },
                        new []
                        {
                            CreateTagHelperDirectiveDescriptor("?icrosoft.*, " + assemblyA, TagHelperDirectiveType.AddTagHelper),
                            CreateTagHelperDirectiveDescriptor("?ystem.*, " + assemblyB, TagHelperDirectiveType.AddTagHelper),
                            CreateTagHelperDirectiveDescriptor("*?????r, " + assemblyA, TagHelperDirectiveType.RemoveTagHelper),
                            CreateTagHelperDirectiveDescriptor("Sy??em.*, " + assemblyB, TagHelperDirectiveType.RemoveTagHelper)
                        }
                    },
                    {
                        new Dictionary<string, IEnumerable<Type>>
                        {
                            { assemblyA, new [] { Valid_PlainTagHelperType, Valid_InheritedTagHelperType } },
                            { assemblyB, new [] { stringType } },
                        },
                        new []
                        {
                            CreateTagHelperDirectiveDescriptor("?i?crosoft.*, " + assemblyA, TagHelperDirectiveType.AddTagHelper),
                            CreateTagHelperDirectiveDescriptor("??ystem.*, " + assemblyB, TagHelperDirectiveType.AddTagHelper),
                        }
                    },
                };
            }
        }

        [Theory]
        [MemberData(nameof(ResolveDirectiveDescriptorsData_EmptyResult))]
        public void Resolve_CanReturnEmptyDescriptorsBasedOnDirectiveDescriptors(
            Dictionary<string, IEnumerable<Type>> descriptorAssemblyLookups,
            IEnumerable<TagHelperDirectiveDescriptor> directiveDescriptors)
        {
            // Arrange
            var tagHelperDescriptorResolver =
                new TestTagHelperDescriptorResolver(
                    new LookupBasedTagHelperTypeResolver(descriptorAssemblyLookups));
            var resolutionContext = new TagHelperDescriptorResolutionContext(
                directiveDescriptors,
                new ErrorSink());

            // Act
            var descriptors = tagHelperDescriptorResolver.Resolve(resolutionContext);

            // Assert
            Assert.Empty(descriptors);
        }

        [Fact]
        public void DescriptorResolver_DoesNotReturnInvalidTagHelpersWhenSpecified()
        {
            // Arrange
            var tagHelperDescriptorResolver =
                new TestTagHelperDescriptorResolver(
                    new TestTagHelperTypeResolver(TestableTagHelpers));

            // Act
            var descriptors = tagHelperDescriptorResolver.Resolve(
                "Microsoft.AspNetCore.Razor.Runtime.Test.TagHelpers.Invalid_AbstractTagHelper, " + AssemblyName,
                "Microsoft.AspNetCore.Razor.Runtime.Test.TagHelpers.Invalid_GenericTagHelper`, " + AssemblyName,
                "Microsoft.AspNetCore.Razor.Runtime.Test.TagHelpers.Invalid_NestedPublicTagHelper, " + AssemblyName,
                "Microsoft.AspNetCore.Razor.Runtime.Test.TagHelpers.Invalid_NestedInternalTagHelper, " + AssemblyName,
                "Microsoft.AspNetCore.Razor.Runtime.Test.TagHelpers.Invalid_PrivateTagHelper, " + AssemblyName,
                "Microsoft.AspNetCore.Razor.Runtime.Test.TagHelpers.Invalid_ProtectedTagHelper, " + AssemblyName,
                "Microsoft.AspNetCore.Razor.Runtime.Test.TagHelpers.Invalid_InternalTagHelper, " + AssemblyName);

            // Assert
            Assert.Empty(descriptors);
        }

        public static TheoryData<string> DescriptorResolver_IgnoresSpacesData
        {
            get
            {
                var typeName = typeof(Valid_PlainTagHelper).FullName;
                return new TheoryData<string>
                {
                    $"{typeName},{AssemblyName}",
                    $"    {typeName},{AssemblyName}",
                    $"{typeName}    ,{AssemblyName}",
                    $"    {typeName}    ,{AssemblyName}",
                    $"{typeName},    {AssemblyName}",
                    $"{typeName},{AssemblyName}    ",
                    $"{typeName},    {AssemblyName}    ",
                    $"    {typeName},    {AssemblyName}    ",
                    $"    {typeName}    ,    {AssemblyName}    "
                };
            }
        }

        [Theory]
        [MemberData(nameof(DescriptorResolver_IgnoresSpacesData))]
        public void DescriptorResolver_IgnoresSpaces(string lookupText)
        {
            // Arrange
            var invoked = false;
            var tagHelperTypeResolver = new TestTagHelperTypeResolver(TestableTagHelpers)
            {
                OnGetExportedTypes = (assemblyName) =>
                {
                    Assert.Equal(AssemblyName, assemblyName.Name);
                    invoked = true;
                }
            };
            var tagHelperDescriptorResolver = new TestTagHelperDescriptorResolver(tagHelperTypeResolver);

            // Act
            var descriptors = tagHelperDescriptorResolver.Resolve(lookupText);

            // Assert
            Assert.True(invoked);
            var descriptor = Assert.Single(descriptors);
            Assert.Equal(AssemblyName, descriptor.AssemblyName);
            Assert.Equal(typeof(Valid_PlainTagHelper).FullName, descriptor.TypeName);
        }

        [Fact]
        public void DescriptorResolver_ResolvesOnlyTypeResolverProvidedTypes()
        {
            // Arrange
            var resolver = new TestTagHelperDescriptorResolver(
                new LookupBasedTagHelperTypeResolver(
                    new Dictionary<string, IEnumerable<Type>>(StringComparer.OrdinalIgnoreCase)
                    {
                        { AssemblyName, ValidTestableTagHelpers },
                        {
                            Valid_PlainTagHelperType.FullName + ", " + AssemblyName,
                            new Type[] { Valid_PlainTagHelperType }
                        }
                    }));

            // Act
            var descriptors = resolver.Resolve(Valid_PlainTagHelperType + ", " + AssemblyName);

            // Assert
            var descriptor = Assert.Single(descriptors);
            Assert.Equal(Valid_PlainTagHelperDescriptor, descriptor, CaseSensitiveTagHelperDescriptorComparer.Default);
        }

        [Fact]
        public void DescriptorResolver_ResolvesMultipleTypes()
        {
            // Arrange
            var resolver = new TestTagHelperDescriptorResolver(
                new LookupBasedTagHelperTypeResolver(
                    new Dictionary<string, IEnumerable<Type>>(StringComparer.OrdinalIgnoreCase)
                    {
                        { AssemblyName, new Type[]{ Valid_PlainTagHelperType, Valid_InheritedTagHelperType } },
                    }));
            var expectedDescriptors = new TagHelperDescriptor[]
            {
                Valid_PlainTagHelperDescriptor,
                Valid_InheritedTagHelperDescriptor
            };

            // Act
            var descriptors = resolver.Resolve("*, " + AssemblyName).ToArray();

            // Assert
            Assert.Equal(descriptors.Length, 2);
            Assert.Equal(expectedDescriptors, descriptors, CaseSensitiveTagHelperDescriptorComparer.Default);
        }

        [Fact]
        public void DescriptorResolver_DoesNotResolveTypesForNoTypeResolvingLookupText()
        {
            // Arrange
            var resolver = new TestTagHelperDescriptorResolver(
                new LookupBasedTagHelperTypeResolver(
                    new Dictionary<string, IEnumerable<Type>>(StringComparer.OrdinalIgnoreCase)
                    {
                        { AssemblyName, ValidTestableTagHelpers },
                        {
                            Valid_PlainTagHelperType.FullName + ", " + AssemblyName,
                            new Type[]{ Valid_PlainTagHelperType }
                        }
                    }));

            // Act
            var descriptors = resolver.Resolve("*, lookupText").ToArray();

            // Assert
            Assert.Empty(descriptors);
        }

        [Theory]
        [InlineData("", 1)]
        [InlineData("*,", 2)]
        [InlineData("?,", 2)]
        [InlineData(",", 1)]
        [InlineData(",,,", 3)]
        [InlineData("First, ", 7)]
        [InlineData("First , ", 8)]
        [InlineData(" ,Second", 8)]
        [InlineData(" , Second", 9)]
        [InlineData("SomeType,", 9)]
        [InlineData("SomeAssembly", 12)]
        [InlineData("First,Second,Third", 18)]
        public void DescriptorResolver_CreatesErrorIfInvalidLookupText_DoesNotThrow(string lookupText, int errorLength)
        {
            // Arrange
            var errorSink = new ErrorSink();
            var tagHelperDescriptorResolver =
                new TestTagHelperDescriptorResolver(
                    new TestTagHelperTypeResolver(InvalidTestableTagHelpers));
            var documentLocation = new SourceLocation(1, 2, 3);
            var directiveType = TagHelperDirectiveType.AddTagHelper;
            var expectedErrorMessage = string.Format(
                "Invalid tag helper directive look up text '{0}'. The correct look up text " +
                "format is: \"typeName, assemblyName\".",
                lookupText);
            var resolutionContext = new TagHelperDescriptorResolutionContext(
                new[]
                {
                    new TagHelperDirectiveDescriptor
                    {
                        DirectiveText = lookupText,
                        Location = documentLocation,
                        DirectiveType = directiveType
                    }
                },
                errorSink);

            // Act
            tagHelperDescriptorResolver.Resolve(resolutionContext);

            // Assert
            var error = Assert.Single(errorSink.Errors);
            Assert.Equal(errorLength, error.Length);
            Assert.Equal(documentLocation, error.Location);
            Assert.Equal(expectedErrorMessage, error.Message);
        }

        [Fact]
        public void DescriptorResolver_UnderstandsUnexpectedExceptions_DoesNotThrow()
        {
            // Arrange
            var expectedErrorMessage = "Encountered an unexpected error when attempting to resolve tag helper " +
                                       "directive '@addTagHelper' with value 'A custom, lookup text'. Error: A " +
                                       "custom exception";
            var documentLocation = new SourceLocation(1, 2, 3);
            var directiveType = TagHelperDirectiveType.AddTagHelper;
            var errorSink = new ErrorSink();
            var expectedError = new Exception("A custom exception");
            var tagHelperDescriptorResolver = new ThrowingTagHelperDescriptorResolver(expectedError);
            var resolutionContext = new TagHelperDescriptorResolutionContext(
                new[]
                {
                    new TagHelperDirectiveDescriptor
                    {
                        DirectiveText = "A custom, lookup text",
                        Location = documentLocation,
                        DirectiveType = directiveType
                    }
                },
                errorSink);


            // Act
            tagHelperDescriptorResolver.Resolve(resolutionContext);

            // Assert
            var error = Assert.Single(errorSink.Errors);
            Assert.Equal(21, error.Length);
            Assert.Equal(documentLocation, error.Location);
            Assert.Equal(expectedErrorMessage, error.Message);
        }

        private static TagHelperDescriptor CreateDescriptor(
            string prefix,
            string tagName,
            string typeName,
            string assemblyName)
        {
            return new TagHelperDescriptor
            {
                Prefix = prefix,
                TagName = tagName,
                TypeName = typeName,
                AssemblyName = assemblyName
            };
        }

        private static TagHelperDescriptor CreatePrefixedValidPlainDescriptor(string prefix)
        {
            return CreateDescriptor(
                prefix,
                tagName: "valid_plain",
                typeName: Valid_PlainTagHelperType.FullName,
                assemblyName: AssemblyName);
        }

        private static TagHelperDescriptor CreatePrefixedValidInheritedDescriptor(string prefix)
        {
            return CreateDescriptor(
                prefix,
                tagName: "valid_inherited",
                typeName: Valid_InheritedTagHelperType.FullName,
                assemblyName: AssemblyName);
        }

        private static TagHelperDescriptor CreatePrefixedStringDescriptor(string prefix)
        {
            var stringType = typeof(string);

            return CreateDescriptor(
                prefix,
                tagName: "string",
                typeName: stringType.FullName,
                assemblyName: stringType.GetTypeInfo().Assembly.GetName().Name);
        }

        private static TagHelperDirectiveDescriptor CreateTagHelperDirectiveDescriptor(
            string directiveText,
            TagHelperDirectiveType directiveType)
        {
            return new TagHelperDirectiveDescriptor
            {
                DirectiveText = directiveText,
                Location = SourceLocation.Zero,
                DirectiveType = directiveType
            };
        }

        public class InspectableTagHelperDescriptorResolver : TagHelperDescriptorResolver
        {
            public InspectableTagHelperDescriptorResolver()
                : base(designTime: false)
            {
            }

            public SourceLocation DocumentLocation { get; private set; }

            protected override IEnumerable<TagHelperDescriptor> ResolveDescriptorsInAssembly(
                string assemblyName,
                SourceLocation documentLocation,
                ErrorSink errorSink)
            {
                DocumentLocation = documentLocation;

                return Enumerable.Empty<TagHelperDescriptor>();
            }
        }

        private class TestTagHelperDescriptorResolver : TagHelperDescriptorResolver
        {
            public TestTagHelperDescriptorResolver(TagHelperTypeResolver typeResolver)
                : base(typeResolver, new TagHelperDescriptorFactory(designTime: false))
            {
            }

            public IEnumerable<TagHelperDescriptor> Resolve(params string[] lookupTexts)
            {
                return Resolve(
                    new TagHelperDescriptorResolutionContext(
                        lookupTexts.Select(
                            lookupText =>
                                CreateTagHelperDirectiveDescriptor(lookupText, TagHelperDirectiveType.AddTagHelper)),
                    new ErrorSink()));
            }
        }

        private class LookupBasedTagHelperTypeResolver : TagHelperTypeResolver
        {
            private Dictionary<string, IEnumerable<Type>> _lookupValues;

            public LookupBasedTagHelperTypeResolver(Dictionary<string, IEnumerable<Type>> lookupValues)
            {
                _lookupValues = lookupValues;
            }

            protected override IEnumerable<TypeInfo> GetExportedTypes(AssemblyName assemblyName)
            {
                IEnumerable<Type> types;

                _lookupValues.TryGetValue(assemblyName.Name, out types);

                return types?.Select(type => type.GetTypeInfo()) ?? Enumerable.Empty<TypeInfo>();
            }

            protected override bool IsTagHelper(TypeInfo typeInfo)
            {
                return true;
            }
        }

        private class AssemblyCheckingTagHelperDescriptorResolver : TagHelperDescriptorResolver
        {
            public AssemblyCheckingTagHelperDescriptorResolver()
                : base(designTime: false)
            {
            }

            public string CalledWithAssemblyName { get; set; }

            protected override IEnumerable<TagHelperDescriptor> ResolveDescriptorsInAssembly(
                string assemblyName,
                SourceLocation documentLocation,
                ErrorSink errorSink)
            {
                CalledWithAssemblyName = assemblyName;

                return Enumerable.Empty<TagHelperDescriptor>();
            }
        }

        private class ThrowingTagHelperDescriptorResolver : TagHelperDescriptorResolver
        {
            private readonly Exception _error;

            public ThrowingTagHelperDescriptorResolver(Exception error)
                : base(designTime: false)
            {
                _error = error;
            }

            protected override IEnumerable<TagHelperDescriptor> ResolveDescriptorsInAssembly(
                string assemblyName,
                SourceLocation documentLocation,
                ErrorSink errorSink)
            {
                throw _error;
            }
        }
    }
}