// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
#if NETCOREAPP1_0
using System.Reflection;
#endif
using Microsoft.AspNetCore.Razor.CodeGenerators;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.AspNetCore.Razor.Compilation.TagHelpers;
using Microsoft.AspNetCore.Testing.xunit;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Test.CodeGenerators
{
    public class CSharpTagHelperRenderingTest : TagHelperTestBase
    {
        private static IEnumerable<TagHelperDescriptor> DefaultPAndInputTagHelperDescriptors { get; }
            = BuildPAndInputTagHelperDescriptors(prefix: string.Empty);
        private static IEnumerable<TagHelperDescriptor> PrefixedPAndInputTagHelperDescriptors { get; }
            = BuildPAndInputTagHelperDescriptors(prefix: "THS");

        private static IEnumerable<TagHelperDescriptor> CssSelectorTagHelperDescriptors
        {
            get
            {
                var inputTypePropertyInfo = typeof(TestType).GetProperty("Type");
                var inputCheckedPropertyInfo = typeof(TestType).GetProperty("Checked");

                return new[]
                {
                    new TagHelperDescriptor
                    {
                        TagName = "a",
                        TypeName = "TestNamespace.ATagHelper",
                        AssemblyName = "SomeAssembly",
                        RequiredAttributes = new[]
                        {
                            new TagHelperRequiredAttributeDescriptor
                            {
                                Name = "href",
                                NameComparison = TagHelperRequiredAttributeNameComparison.FullMatch,
                                Value = "~/",
                                ValueComparison = TagHelperRequiredAttributeValueComparison.FullMatch,
                            }
                        },
                    },
                    new TagHelperDescriptor
                    {
                        TagName = "a",
                        TypeName = "TestNamespace.ATagHelperMultipleSelectors",
                        AssemblyName = "SomeAssembly",
                        RequiredAttributes = new[]
                        {
                            new TagHelperRequiredAttributeDescriptor
                            {
                                Name = "href",
                                NameComparison = TagHelperRequiredAttributeNameComparison.FullMatch,
                                Value = "~/",
                                ValueComparison = TagHelperRequiredAttributeValueComparison.PrefixMatch,
                            },
                            new TagHelperRequiredAttributeDescriptor
                            {
                                Name = "href",
                                NameComparison = TagHelperRequiredAttributeNameComparison.FullMatch,
                                Value = "?hello=world",
                                ValueComparison = TagHelperRequiredAttributeValueComparison.SuffixMatch,
                            }
                        },
                    },
                    new TagHelperDescriptor
                    {
                        TagName = "input",
                        TypeName = "TestNamespace.InputTagHelper",
                        AssemblyName = "SomeAssembly",
                        Attributes = new TagHelperAttributeDescriptor[]
                        {
                            new TagHelperAttributeDescriptor("type", inputTypePropertyInfo),
                        },
                        RequiredAttributes = new[]
                        {
                            new TagHelperRequiredAttributeDescriptor
                            {
                                Name = "type",
                                NameComparison = TagHelperRequiredAttributeNameComparison.FullMatch,
                                Value = "text",
                                ValueComparison = TagHelperRequiredAttributeValueComparison.FullMatch,
                            }
                        },
                    },
                    new TagHelperDescriptor
                    {
                        TagName = "input",
                        TypeName = "TestNamespace.InputTagHelper2",
                        AssemblyName = "SomeAssembly",
                        Attributes = new TagHelperAttributeDescriptor[]
                        {
                            new TagHelperAttributeDescriptor("type", inputTypePropertyInfo),
                        },
                        RequiredAttributes = new[]
                        {
                            new TagHelperRequiredAttributeDescriptor
                            {
                                Name = "ty",
                                NameComparison = TagHelperRequiredAttributeNameComparison.PrefixMatch,
                            }
                        },
                    },
                    new TagHelperDescriptor
                    {
                        TagName = "*",
                        TypeName = "TestNamespace.CatchAllTagHelper",
                        AssemblyName = "SomeAssembly",
                        RequiredAttributes = new[]
                        {
                            new TagHelperRequiredAttributeDescriptor
                            {
                                Name = "href",
                                NameComparison = TagHelperRequiredAttributeNameComparison.FullMatch,
                                Value = "~/",
                                ValueComparison = TagHelperRequiredAttributeValueComparison.PrefixMatch,
                            }
                        },
                    },
                    new TagHelperDescriptor
                    {
                        TagName = "*",
                        TypeName = "TestNamespace.CatchAllTagHelper2",
                        AssemblyName = "SomeAssembly",
                        RequiredAttributes = new[]
                        {
                            new TagHelperRequiredAttributeDescriptor
                            {
                                Name = "type",
                                NameComparison = TagHelperRequiredAttributeNameComparison.FullMatch,
                            }
                        },
                    }
                };
            }
        }

        private static IEnumerable<TagHelperDescriptor> EnumTagHelperDescriptors
        {
            get
            {
                return new[]
                {
                    new TagHelperDescriptor
                    {
                        TagName = "*",
                        TypeName = "TestNamespace.CatchAllTagHelper",
                        AssemblyName = "SomeAssembly",
                        Attributes = new[]
                        {
                            new TagHelperAttributeDescriptor
                            {
                                Name = "catch-all",
                                PropertyName = "CatchAll",
                                IsEnum = true,
                                TypeName = typeof(MyEnum).FullName
                            },
                        }
                    },
                    new TagHelperDescriptor
                    {
                        TagName = "input",
                        TypeName = "TestNamespace.InputTagHelper",
                        AssemblyName = "SomeAssembly",
                        Attributes = new[]
                        {
                            new TagHelperAttributeDescriptor
                            {
                                Name = "value",
                                PropertyName = "Value",
                                IsEnum = true,
                                TypeName = typeof(MyEnum).FullName
                            },
                        }
                    },
                };
            }
        }

        private static IEnumerable<TagHelperDescriptor> SymbolBoundTagHelperDescriptors
        {
            get
            {
                return new[]
                {
                    new TagHelperDescriptor
                    {
                        TagName = "*",
                        TypeName = "TestNamespace.CatchAllTagHelper",
                        AssemblyName = "SomeAssembly",
                        Attributes = new[]
                        {
                            new TagHelperAttributeDescriptor
                            {
                                Name = "[item]",
                                PropertyName = "ListItems",
                                TypeName = typeof(List<string>).FullName
                            },
                            new TagHelperAttributeDescriptor
                            {
                                Name = "[(item)]",
                                PropertyName = "ArrayItems",
                                TypeName = typeof(string[]).FullName
                            },
                            new TagHelperAttributeDescriptor
                            {
                                Name = "(click)",
                                PropertyName = "Event1",
                                TypeName = typeof(Action).FullName
                            },
                            new TagHelperAttributeDescriptor
                            {
                                Name = "(^click)",
                                PropertyName = "Event2",
                                TypeName = typeof(Action).FullName
                            },
                            new TagHelperAttributeDescriptor
                            {
                                Name = "*something",
                                PropertyName = "StringProperty1",
                                TypeName = typeof(string).FullName
                            },
                            new TagHelperAttributeDescriptor
                            {
                                Name = "#local",
                                PropertyName = "StringProperty2",
                                TypeName = typeof(string).FullName
                            },
                        },
                        RequiredAttributes = new[] { new TagHelperRequiredAttributeDescriptor { Name = "bound" } },
                    },
                };
            }
        }

        private static IEnumerable<TagHelperDescriptor> MinimizedTagHelpers_Descriptors
        {
            get
            {
                return new[]
                {
                    new TagHelperDescriptor
                    {
                        TagName = "*",
                        TypeName = "TestNamespace.CatchAllTagHelper",
                        AssemblyName = "SomeAssembly",
                        Attributes = new[]
                        {
                            new TagHelperAttributeDescriptor
                            {
                                Name = "catchall-bound-string",
                                PropertyName = "BoundRequiredString",
                                TypeName = typeof(string).FullName,
                                IsStringProperty = true
                            }
                        },
                        RequiredAttributes = new[]
                        {
                            new TagHelperRequiredAttributeDescriptor { Name = "catchall-unbound-required" }
                        },
                    },
                    new TagHelperDescriptor
                    {
                        TagName = "input",
                        TypeName = "TestNamespace.InputTagHelper",
                        AssemblyName = "SomeAssembly",
                        Attributes = new[]
                        {
                            new TagHelperAttributeDescriptor
                            {
                                Name = "input-bound-required-string",
                                PropertyName = "BoundRequiredString",
                                TypeName = typeof(string).FullName,
                                IsStringProperty = true
                            },
                            new TagHelperAttributeDescriptor
                            {
                                Name = "input-bound-string",
                                PropertyName = "BoundString",
                                TypeName = typeof(string).FullName,
                                IsStringProperty = true
                            }
                        },
                        RequiredAttributes = new[]
                        {
                            new TagHelperRequiredAttributeDescriptor { Name = "input-bound-required-string" },
                            new TagHelperRequiredAttributeDescriptor { Name = "input-unbound-required" }
                        },
                    }
                };
            }
        }

        private static IEnumerable<TagHelperDescriptor> DynamicAttributeTagHelpers_Descriptors
        {
            get
            {
                return new[]
                {
                    new TagHelperDescriptor
                    {
                        TagName = "input",
                        TypeName = "TestNamespace.InputTagHelper",
                        AssemblyName = "SomeAssembly",
                        Attributes = new[]
                        {
                            new TagHelperAttributeDescriptor
                            {
                                Name = "bound",
                                PropertyName = "Bound",
                                TypeName = typeof(string).FullName,
                                IsStringProperty = true
                            }
                        }
                    }
                };
            }
        }

        private static IEnumerable<TagHelperDescriptor> DuplicateTargetTagHelperDescriptors
        {
            get
            {
                var inputTypePropertyInfo = typeof(TestType).GetProperty("Type");
                var inputCheckedPropertyInfo = typeof(TestType).GetProperty("Checked");
                return new[]
                {
                    new TagHelperDescriptor
                    {
                        TagName = "*",
                        TypeName = "TestNamespace.CatchAllTagHelper",
                        AssemblyName = "SomeAssembly",
                        Attributes = new TagHelperAttributeDescriptor[]
                        {
                            new TagHelperAttributeDescriptor("type", inputTypePropertyInfo),
                            new TagHelperAttributeDescriptor("checked", inputCheckedPropertyInfo)
                        },
                        RequiredAttributes = new[] { new TagHelperRequiredAttributeDescriptor { Name = "type" } },
                    },
                    new TagHelperDescriptor
                    {
                        TagName = "*",
                        TypeName = "TestNamespace.CatchAllTagHelper",
                        AssemblyName = "SomeAssembly",
                        Attributes = new TagHelperAttributeDescriptor[]
                        {
                            new TagHelperAttributeDescriptor("type", inputTypePropertyInfo),
                            new TagHelperAttributeDescriptor("checked", inputCheckedPropertyInfo)
                        },
                        RequiredAttributes = new[] { new TagHelperRequiredAttributeDescriptor { Name = "checked" } },
                    },
                    new TagHelperDescriptor
                    {
                        TagName = "input",
                        TypeName = "TestNamespace.InputTagHelper",
                        AssemblyName = "SomeAssembly",
                        Attributes = new TagHelperAttributeDescriptor[]
                        {
                            new TagHelperAttributeDescriptor("type", inputTypePropertyInfo),
                            new TagHelperAttributeDescriptor("checked", inputCheckedPropertyInfo)
                        },
                        RequiredAttributes = new[] { new TagHelperRequiredAttributeDescriptor { Name = "type" } },
                    },
                    new TagHelperDescriptor
                    {
                        TagName = "input",
                        TypeName = "TestNamespace.InputTagHelper",
                        AssemblyName = "SomeAssembly",
                        Attributes = new TagHelperAttributeDescriptor[]
                        {
                            new TagHelperAttributeDescriptor("type", inputTypePropertyInfo),
                            new TagHelperAttributeDescriptor("checked", inputCheckedPropertyInfo)
                        },
                        RequiredAttributes = new[] { new TagHelperRequiredAttributeDescriptor { Name = "checked" } },
                    }
                };
            }
        }

        private static IEnumerable<TagHelperDescriptor> AttributeTargetingTagHelperDescriptors
        {
            get
            {
                var inputTypePropertyInfo = typeof(TestType).GetProperty("Type");
                var inputCheckedPropertyInfo = typeof(TestType).GetProperty("Checked");
                return new[]
                {
                    new TagHelperDescriptor
                    {
                        TagName = "p",
                        TypeName = "TestNamespace.PTagHelper",
                        AssemblyName = "SomeAssembly",
                        RequiredAttributes = new[] { new TagHelperRequiredAttributeDescriptor { Name = "class" } },
                    },
                    new TagHelperDescriptor
                    {
                        TagName = "input",
                        TypeName = "TestNamespace.InputTagHelper",
                        AssemblyName = "SomeAssembly",
                        Attributes = new TagHelperAttributeDescriptor[]
                        {
                            new TagHelperAttributeDescriptor("type", inputTypePropertyInfo)
                        },
                        RequiredAttributes = new[] { new TagHelperRequiredAttributeDescriptor { Name = "type" } },
                    },
                    new TagHelperDescriptor
                    {
                        TagName = "input",
                        TypeName = "TestNamespace.InputTagHelper2",
                        AssemblyName = "SomeAssembly",
                        Attributes = new TagHelperAttributeDescriptor[]
                        {
                            new TagHelperAttributeDescriptor("type", inputTypePropertyInfo),
                            new TagHelperAttributeDescriptor("checked", inputCheckedPropertyInfo)
                        },
                        RequiredAttributes = new[]
                        {
                            new TagHelperRequiredAttributeDescriptor { Name = "type" },
                            new TagHelperRequiredAttributeDescriptor { Name = "checked" }
                        },
                    },
                    new TagHelperDescriptor
                    {
                        TagName = "*",
                        TypeName = "TestNamespace.CatchAllTagHelper",
                        AssemblyName = "SomeAssembly",
                        RequiredAttributes = new[] { new TagHelperRequiredAttributeDescriptor { Name = "catchAll" } },
                    }
                };
            }
        }

        private static IEnumerable<TagHelperDescriptor> PrefixedAttributeTagHelperDescriptors
        {
            get
            {
                return new[]
                {
                    new TagHelperDescriptor
                    {
                        TagName = "input",
                        TypeName = "TestNamespace.InputTagHelper1",
                        AssemblyName = "SomeAssembly",
                        Attributes = new[]
                        {
                            new TagHelperAttributeDescriptor
                            {
                                Name = "int-prefix-grabber",
                                PropertyName = "IntProperty",
                                TypeName = typeof(int).FullName
                            },
                            new TagHelperAttributeDescriptor
                            {
                                Name = "int-dictionary",
                                PropertyName = "IntDictionaryProperty",
                                TypeName = typeof(IDictionary<string, int>).FullName
                            },
                            new TagHelperAttributeDescriptor
                            {
                                Name = "string-dictionary",
                                PropertyName = "StringDictionaryProperty",
                                TypeName = "Namespace.DictionaryWithoutParameterlessConstructor<string, string>"
                            },
                            new TagHelperAttributeDescriptor
                            {
                                Name = "string-prefix-grabber",
                                PropertyName = "StringProperty",
                                TypeName = typeof(string).FullName,
                                IsStringProperty = true
                            },
                            new TagHelperAttributeDescriptor
                            {
                                Name = "int-prefix-",
                                PropertyName = "IntDictionaryProperty",
                                TypeName = typeof(int).FullName,
                                IsIndexer = true
                            },
                            new TagHelperAttributeDescriptor
                            {
                                Name = "string-prefix-",
                                PropertyName = "StringDictionaryProperty",
                                TypeName = typeof(string).FullName,
                                IsIndexer = true,
                                IsStringProperty = true
                            }
                        }
                    },
                    new TagHelperDescriptor
                    {
                        TagName = "input",
                        TypeName = "TestNamespace.InputTagHelper2",
                        AssemblyName = "SomeAssembly",
                        Attributes = new[]
                        {
                            new TagHelperAttributeDescriptor
                            {
                                Name = "int-dictionary",
                                PropertyName = "IntDictionaryProperty",
                                TypeName = typeof(int).FullName
                            },
                            new TagHelperAttributeDescriptor
                            {
                                Name = "string-dictionary",
                                PropertyName = "StringDictionaryProperty",
                                TypeName = "Namespace.DictionaryWithoutParameterlessConstructor<string, string>"
                            },
                            new TagHelperAttributeDescriptor
                            {
                                Name = "int-prefix-",
                                PropertyName = "IntDictionaryProperty",
                                TypeName = typeof(int).FullName,
                                IsIndexer = true
                            },
                            new TagHelperAttributeDescriptor
                            {
                                Name = "string-prefix-",
                                PropertyName = "StringDictionaryProperty",
                                TypeName = typeof(string).FullName,
                                IsIndexer = true,
                                IsStringProperty = true
                            }
                        }
                    }
                };
            }
        }

        public static TheoryData TagHelperDescriptorFlowTestData
        {
            get
            {
                return new TheoryData<
                    string,  // Test name
                    string, // Baseline name
                    IEnumerable<TagHelperDescriptor>, // TagHelperDescriptors provided
                    IEnumerable<TagHelperDescriptor>, // Expected TagHelperDescriptors
                    bool> // Design time mode.
                {
                    {
                        "SingleTagHelper",
                        "SingleTagHelper",
                        DefaultPAndInputTagHelperDescriptors,
                        DefaultPAndInputTagHelperDescriptors,
                        false
                    },
                    {
                        "SingleTagHelper",
                        "SingleTagHelper.DesignTime",
                        DefaultPAndInputTagHelperDescriptors,
                        DefaultPAndInputTagHelperDescriptors,
                        true
                    },
                    {
                        "BasicTagHelpers",
                        "BasicTagHelpers",
                        DefaultPAndInputTagHelperDescriptors,
                        DefaultPAndInputTagHelperDescriptors,
                        false
                    },
                    {
                        "DuplicateTargetTagHelper",
                        "DuplicateTargetTagHelper",
                        DuplicateTargetTagHelperDescriptors,
                        DuplicateTargetTagHelperDescriptors,
                        false
                    },
                    {
                        "BasicTagHelpers",
                        "BasicTagHelpers.DesignTime",
                        DefaultPAndInputTagHelperDescriptors,
                        DefaultPAndInputTagHelperDescriptors,
                        true
                    },
                    {
                        "BasicTagHelpers.RemoveTagHelper",
                        "BasicTagHelpers.RemoveTagHelper",
                        DefaultPAndInputTagHelperDescriptors,
                        Enumerable.Empty<TagHelperDescriptor>(),
                        false
                    },
                    {
                        "BasicTagHelpers.Prefixed",
                        "BasicTagHelpers.Prefixed",
                        PrefixedPAndInputTagHelperDescriptors,
                        PrefixedPAndInputTagHelperDescriptors,
                        false
                    },
                    {
                        "BasicTagHelpers.Prefixed",
                        "BasicTagHelpers.Prefixed.DesignTime",
                        PrefixedPAndInputTagHelperDescriptors,
                        PrefixedPAndInputTagHelperDescriptors,
                        true
                    },
                    {
                        "ComplexTagHelpers",
                        "ComplexTagHelpers",
                        DefaultPAndInputTagHelperDescriptors,
                        DefaultPAndInputTagHelperDescriptors,
                        false
                    },
                    {
                        "ComplexTagHelpers",
                        "ComplexTagHelpers.DesignTime",
                        DefaultPAndInputTagHelperDescriptors,
                        DefaultPAndInputTagHelperDescriptors,
                        true
                    },
                    {
                        "AttributeTargetingTagHelpers",
                        "AttributeTargetingTagHelpers",
                        AttributeTargetingTagHelperDescriptors,
                        AttributeTargetingTagHelperDescriptors,
                        false
                    },
                    {
                        "AttributeTargetingTagHelpers",
                        "AttributeTargetingTagHelpers.DesignTime",
                        AttributeTargetingTagHelperDescriptors,
                        AttributeTargetingTagHelperDescriptors,
                        true
                    },
                    {
                        "MinimizedTagHelpers",
                        "MinimizedTagHelpers",
                        MinimizedTagHelpers_Descriptors,
                        MinimizedTagHelpers_Descriptors,
                        false
                    },
                    {
                        "MinimizedTagHelpers",
                        "MinimizedTagHelpers.DesignTime",
                        MinimizedTagHelpers_Descriptors,
                        MinimizedTagHelpers_Descriptors,
                        true
                    }
                };
            }
        }

        [Theory]
        [MemberData(nameof(TagHelperDescriptorFlowTestData))]
        public void TagHelpers_RenderingOutputFlowsFoundTagHelperDescriptors(
            string testName,
            string baselineName,
            IEnumerable<TagHelperDescriptor> tagHelperDescriptors,
            IEnumerable<TagHelperDescriptor> expectedTagHelperDescriptors,
            bool designTimeMode)
        {
            RunTagHelperTest(
                testName,
                baseLineName: baselineName,
                tagHelperDescriptors: tagHelperDescriptors,
                onResults: (results) =>
                {
                    Assert.Equal(
                        expectedTagHelperDescriptors,
                        results.TagHelperDescriptors,
                        TagHelperDescriptorComparer.Default);
                },
                designTimeMode: designTimeMode);
        }

        public static TheoryData DesignTimeTagHelperTestData
        {
            get
            {
                // Test resource name, baseline resource name, expected TagHelperDescriptors, expected LineMappings
                return new TheoryData<string, string, IEnumerable<TagHelperDescriptor>, IList<LineMapping>>
                {
                    {
                        "SingleTagHelper",
                        "SingleTagHelper.DesignTime",
                        DefaultPAndInputTagHelperDescriptors,
                        new List<LineMapping>
                        {
                            BuildLineMapping(
                                documentAbsoluteIndex: 14,
                                documentLineIndex: 0,
                                documentCharacterOffsetIndex: 14,
                                generatedAbsoluteIndex: 372,
                                generatedLineIndex: 12,
                                generatedCharacterOffsetIndex: 48,
                                contentLength: 15),
                            BuildLineMapping(
                                documentAbsoluteIndex: 61,
                                documentLineIndex: 2,
                                documentCharacterOffsetIndex: 28,
                                generatedAbsoluteIndex: 892,
                                generatedLineIndex: 27,
                                generatedCharacterOffsetIndex: 33,
                                contentLength: 4),
                        }
                    },
                    {
                        "BasicTagHelpers",
                        "BasicTagHelpers.DesignTime",
                        DefaultPAndInputTagHelperDescriptors,
                        new[]
                        {
                            BuildLineMapping(
                                documentAbsoluteIndex: 14,
                                documentLineIndex: 0,
                                documentCharacterOffsetIndex: 14,
                                generatedAbsoluteIndex: 371,
                                generatedLineIndex: 12,
                                generatedCharacterOffsetIndex: 47,
                                contentLength: 17),
                            BuildLineMapping(
                                documentAbsoluteIndex: 220,
                                documentLineIndex: 5,
                                generatedAbsoluteIndex: 1293,
                                generatedLineIndex: 31,
                                characterOffsetIndex: 38,
                                contentLength: 23),
                            BuildLineMapping(
                                documentAbsoluteIndex: 303,
                                documentLineIndex: 6,
                                documentCharacterOffsetIndex: 40,
                                generatedAbsoluteIndex: 1934,
                                generatedLineIndex: 42,
                                generatedCharacterOffsetIndex: 42,
                                contentLength: 4),
                        }
                    },
                    {
                        "BasicTagHelpers.Prefixed",
                        "BasicTagHelpers.Prefixed.DesignTime",
                        PrefixedPAndInputTagHelperDescriptors,
                        new[]
                        {
                            BuildLineMapping(
                                documentAbsoluteIndex: 17,
                                documentLineIndex: 0,
                                documentCharacterOffsetIndex: 17,
                                generatedAbsoluteIndex: 380,
                                generatedLineIndex: 12,
                                generatedCharacterOffsetIndex: 47,
                                contentLength: 5),
                            BuildLineMapping(
                                documentAbsoluteIndex: 38,
                                documentLineIndex: 1,
                                documentCharacterOffsetIndex: 14,
                                generatedAbsoluteIndex: 436,
                                generatedLineIndex: 13,
                                generatedCharacterOffsetIndex: 48,
                                contentLength: 15),
                            BuildLineMapping(
                                documentAbsoluteIndex: 224,
                                documentLineIndex: 7,
                                generatedAbsoluteIndex: 1437,
                                generatedLineIndex: 33,
                                characterOffsetIndex: 43,
                                contentLength: 4),
                        }
                    },
                    {
                        "ComplexTagHelpers",
                        "ComplexTagHelpers.DesignTime",
                        DefaultPAndInputTagHelperDescriptors,
                        new List<LineMapping>
                        {
                            BuildLineMapping(
                                documentAbsoluteIndex: 14,
                                documentLineIndex: 0,
                                documentCharacterOffsetIndex: 14,
                                generatedAbsoluteIndex: 374,
                                generatedLineIndex: 12,
                                generatedCharacterOffsetIndex: 48,
                                contentLength: 15),
                            BuildLineMapping(
                                documentAbsoluteIndex: 34,
                                documentLineIndex: 2,
                                documentCharacterOffsetIndex: 1,
                                generatedAbsoluteIndex: 958,
                                generatedLineIndex: 28,
                                generatedCharacterOffsetIndex: 0,
                                contentLength: 48),
                            BuildLineMapping(
                                documentAbsoluteIndex: 209,
                                documentLineIndex: 9,
                                generatedAbsoluteIndex: 1076,
                                generatedLineIndex: 37,
                                characterOffsetIndex: 0,
                                contentLength: 12),
                            BuildLineMapping(
                                documentAbsoluteIndex: 222,
                                documentLineIndex: 9,
                                documentCharacterOffsetIndex: 13,
                                generatedAbsoluteIndex: 1172,
                                generatedLineIndex: 43,
                                generatedCharacterOffsetIndex: 12,
                                contentLength: 27),
                            BuildLineMapping(
                                documentAbsoluteIndex: 350,
                                documentLineIndex: 12,
                                generatedAbsoluteIndex: 1720,
                                generatedLineIndex: 55,
                                characterOffsetIndex: 0,
                                contentLength: 48),
                            BuildLineMapping(
                                documentAbsoluteIndex: 444,
                                documentLineIndex: 15,
                                generatedAbsoluteIndex: 2092,
                                generatedLineIndex: 65,
                                characterOffsetIndex: 46,
                                contentLength: 8),
                            BuildLineMapping(
                                documentAbsoluteIndex: 461,
                                documentLineIndex: 15,
                                generatedAbsoluteIndex: 2388,
                                generatedLineIndex: 72,
                                characterOffsetIndex: 63,
                                contentLength: 4),
                            BuildLineMapping(
                                documentAbsoluteIndex: 505,
                                documentLineIndex: 16,
                                generatedAbsoluteIndex: 2797,
                                generatedLineIndex: 80,
                                characterOffsetIndex: 31,
                                contentLength: 30),
                            BuildLineMapping(
                                documentAbsoluteIndex: 572,
                                documentLineIndex: 17,
                                documentCharacterOffsetIndex: 30,
                                generatedAbsoluteIndex: 3289,
                                generatedLineIndex: 89,
                                generatedCharacterOffsetIndex: 29,
                                contentLength: 10),
                            BuildLineMapping(
                                documentAbsoluteIndex: 604,
                                documentLineIndex: 17,
                                documentCharacterOffsetIndex: 62,
                                generatedAbsoluteIndex: 3432,
                                generatedLineIndex: 95,
                                generatedCharacterOffsetIndex: 61,
                                contentLength: 9),
                            BuildLineMapping(
                                documentAbsoluteIndex: 635,
                                documentLineIndex: 17,
                                documentCharacterOffsetIndex: 93,
                                generatedAbsoluteIndex: 3604,
                                generatedLineIndex: 101,
                                generatedCharacterOffsetIndex: 91,
                                contentLength: 2),
                            BuildLineMapping(
                                documentAbsoluteIndex: 641,
                                documentLineIndex: 18,
                                generatedAbsoluteIndex: 3832,
                                generatedLineIndex: 109,
                                characterOffsetIndex: 0,
                                contentLength: 15),
                            BuildLineMapping(
                                documentAbsoluteIndex: 161,
                                documentLineIndex: 7,
                                generatedAbsoluteIndex: 4043,
                                generatedLineIndex: 116,
                                characterOffsetIndex: 32,
                                contentLength: 12),
                            BuildLineMapping(
                                documentAbsoluteIndex: 767,
                                documentLineIndex: 21,
                                generatedAbsoluteIndex: 4126,
                                generatedLineIndex: 121,
                                characterOffsetIndex: 0,
                                contentLength: 12),
                            BuildLineMapping(
                                documentAbsoluteIndex: 781,
                                documentLineIndex: 21,
                                generatedAbsoluteIndex: 4224,
                                generatedLineIndex: 127,
                                characterOffsetIndex: 14,
                                contentLength: 21),
                            BuildLineMapping(
                                documentAbsoluteIndex: 834,
                                documentLineIndex: 22,
                                documentCharacterOffsetIndex: 29,
                                generatedAbsoluteIndex: 4567,
                                generatedLineIndex: 135,
                                generatedCharacterOffsetIndex: 42,
                                contentLength: 1),
                            BuildLineMapping(
                                documentAbsoluteIndex: 835,
                                documentLineIndex: 22,
                                documentCharacterOffsetIndex: 30,
                                generatedAbsoluteIndex: 4568,
                                generatedLineIndex: 135,
                                generatedCharacterOffsetIndex: 43,
                                contentLength: 7),
                            BuildLineMapping(
                                documentAbsoluteIndex: 842,
                                documentLineIndex: 22,
                                documentCharacterOffsetIndex: 37,
                                generatedAbsoluteIndex: 4575,
                                generatedLineIndex: 135,
                                generatedCharacterOffsetIndex: 50,
                                contentLength: 1),
                            BuildLineMapping(
                                documentAbsoluteIndex: 709,
                                documentLineIndex: 20,
                                documentCharacterOffsetIndex: 39,
                                generatedAbsoluteIndex: 4780,
                                generatedLineIndex: 141,
                                generatedCharacterOffsetIndex: 38,
                                contentLength: 23),
                            BuildLineMapping(
                                documentAbsoluteIndex: 732,
                                documentLineIndex: 20,
                                documentCharacterOffsetIndex: 62,
                                generatedAbsoluteIndex: 4803,
                                generatedLineIndex: 141,
                                generatedCharacterOffsetIndex: 61,
                                contentLength: 7),
                            BuildLineMapping(
                                documentAbsoluteIndex: 974,
                                documentLineIndex: 25,
                                documentCharacterOffsetIndex: 61,
                                generatedAbsoluteIndex: 5149,
                                generatedLineIndex: 148,
                                generatedCharacterOffsetIndex: 60,
                                contentLength: 1),
                            BuildLineMapping(
                                documentAbsoluteIndex: 975,
                                documentLineIndex: 25,
                                documentCharacterOffsetIndex: 62,
                                generatedAbsoluteIndex: 5150,
                                generatedLineIndex: 148,
                                generatedCharacterOffsetIndex: 61,
                                contentLength: 30),
                            BuildLineMapping(
                                documentAbsoluteIndex: 1005,
                                documentLineIndex: 25,
                                documentCharacterOffsetIndex: 92,
                                generatedAbsoluteIndex: 5180,
                                generatedLineIndex: 148,
                                generatedCharacterOffsetIndex: 91,
                                contentLength: 1),
                            BuildLineMapping(
                                documentAbsoluteIndex: 877,
                                documentLineIndex: 24,
                                documentCharacterOffsetIndex: 16,
                                generatedAbsoluteIndex: 5380,
                                generatedLineIndex: 154,
                                generatedCharacterOffsetIndex: 33,
                                contentLength: 8),
                            BuildLineMapping(
                                documentAbsoluteIndex: 885,
                                documentLineIndex: 24,
                                documentCharacterOffsetIndex: 24,
                                generatedAbsoluteIndex: 5388,
                                generatedLineIndex: 154,
                                generatedCharacterOffsetIndex: 41,
                                contentLength: 1),
                            BuildLineMapping(
                                documentAbsoluteIndex: 886,
                                documentLineIndex: 24,
                                documentCharacterOffsetIndex: 25,
                                generatedAbsoluteIndex: 5389,
                                generatedLineIndex: 154,
                                generatedCharacterOffsetIndex: 42,
                                contentLength: 23),
                            BuildLineMapping(
                                documentAbsoluteIndex: 1104,
                                documentLineIndex: 28,
                                documentCharacterOffsetIndex: 28,
                                generatedAbsoluteIndex: 5733,
                                generatedLineIndex: 161,
                                generatedCharacterOffsetIndex: 42,
                                contentLength: 30),
                            BuildLineMapping(
                                documentAbsoluteIndex: 1042,
                                documentLineIndex: 27,
                                documentCharacterOffsetIndex: 16,
                                generatedAbsoluteIndex: 5962,
                                generatedLineIndex: 167,
                                generatedCharacterOffsetIndex: 33,
                                contentLength: 30),
                            BuildLineMapping(
                                documentAbsoluteIndex: 1232,
                                documentLineIndex: 31,
                                documentCharacterOffsetIndex: 28,
                                generatedAbsoluteIndex: 6313,
                                generatedLineIndex: 174,
                                generatedCharacterOffsetIndex: 42,
                                contentLength: 3),
                            BuildLineMapping(
                                documentAbsoluteIndex: 1235,
                                documentLineIndex: 31,
                                documentCharacterOffsetIndex: 31,
                                generatedAbsoluteIndex: 6316,
                                generatedLineIndex: 174,
                                generatedCharacterOffsetIndex: 45,
                                contentLength: 2),
                            BuildLineMapping(
                                documentAbsoluteIndex: 1237,
                                documentLineIndex: 31,
                                documentCharacterOffsetIndex: 33,
                                generatedAbsoluteIndex: 6318,
                                generatedLineIndex: 174,
                                generatedCharacterOffsetIndex: 47,
                                contentLength: 27),
                            BuildLineMapping(
                                documentAbsoluteIndex: 1264,
                                documentLineIndex: 31,
                                documentCharacterOffsetIndex: 60,
                                generatedAbsoluteIndex: 6345,
                                generatedLineIndex: 174,
                                generatedCharacterOffsetIndex: 74,
                                contentLength: 1),
                            BuildLineMapping(
                                documentAbsoluteIndex: 1265,
                                documentLineIndex: 31,
                                documentCharacterOffsetIndex: 61,
                                generatedAbsoluteIndex: 6346,
                                generatedLineIndex: 174,
                                generatedCharacterOffsetIndex: 75,
                                contentLength: 10),
                            BuildLineMapping(
                                documentAbsoluteIndex: 1169,
                                documentLineIndex: 30,
                                documentCharacterOffsetIndex: 17,
                                generatedAbsoluteIndex: 6555,
                                generatedLineIndex: 180,
                                generatedCharacterOffsetIndex: 33,
                                contentLength: 1),
                            BuildLineMapping(
                                documentAbsoluteIndex: 1170,
                                documentLineIndex: 30,
                                documentCharacterOffsetIndex: 18,
                                generatedAbsoluteIndex: 6556,
                                generatedLineIndex: 180,
                                generatedCharacterOffsetIndex: 34,
                                contentLength: 29),
                            BuildLineMapping(
                                documentAbsoluteIndex: 1199,
                                documentLineIndex: 30,
                                documentCharacterOffsetIndex: 47,
                                generatedAbsoluteIndex: 6585,
                                generatedLineIndex: 180,
                                generatedCharacterOffsetIndex: 63,
                                contentLength: 1),
                            BuildLineMapping(
                                documentAbsoluteIndex: 1304,
                                documentLineIndex: 33,
                                generatedAbsoluteIndex: 6666,
                                generatedLineIndex: 185,
                                characterOffsetIndex: 9,
                                contentLength: 11),
                            BuildLineMapping(
                                documentAbsoluteIndex: 1359,
                                documentLineIndex: 33,
                                documentCharacterOffsetIndex: 64,
                                generatedAbsoluteIndex: 7027,
                                generatedLineIndex: 189,
                                generatedCharacterOffsetIndex: 63,
                                contentLength: 7),
                            BuildLineMapping(
                                documentAbsoluteIndex: 1324,
                                documentLineIndex: 33,
                                documentCharacterOffsetIndex: 29,
                                generatedAbsoluteIndex: 7225,
                                generatedLineIndex: 195,
                                generatedCharacterOffsetIndex: 33,
                                contentLength: 3),
                            BuildLineMapping(
                                documentAbsoluteIndex: 1388,
                                documentLineIndex: 35,
                                generatedAbsoluteIndex: 7340,
                                generatedLineIndex: 206,
                                characterOffsetIndex: 0,
                                contentLength: 1),
                        }
                    },
                    {
                        "EmptyAttributeTagHelpers",
                        "EmptyAttributeTagHelpers.DesignTime",
                        DefaultPAndInputTagHelperDescriptors,
                        new List<LineMapping>
                        {
                            BuildLineMapping(
                                documentAbsoluteIndex: 14,
                                documentLineIndex: 0,
                                documentCharacterOffsetIndex: 14,
                                generatedAbsoluteIndex: 381,
                                generatedLineIndex: 12,
                                generatedCharacterOffsetIndex: 48,
                                contentLength: 9),
                            BuildLineMapping(
                                documentAbsoluteIndex: 60,
                                documentLineIndex: 3,
                                documentCharacterOffsetIndex: 26,
                                generatedAbsoluteIndex: 1367,
                                generatedLineIndex: 32,
                                generatedCharacterOffsetIndex: 42,
                                contentLength: 0),
                            BuildLineMapping(
                                documentAbsoluteIndex: 120,
                                documentLineIndex: 5,
                                documentCharacterOffsetIndex: 30,
                                generatedAbsoluteIndex: 1838,
                                generatedLineIndex: 41,
                                generatedCharacterOffsetIndex: 42,
                                contentLength: 0),
                            BuildLineMapping(
                                documentAbsoluteIndex: 86,
                                documentLineIndex: 4,
                                documentCharacterOffsetIndex: 12,
                                generatedAbsoluteIndex: 2043,
                                generatedLineIndex: 47,
                                generatedCharacterOffsetIndex: 33,
                                contentLength: 0),
                        }
                    },
                    {
                        "EscapedTagHelpers",
                        "EscapedTagHelpers.DesignTime",
                        DefaultPAndInputTagHelperDescriptors,
                        new List<LineMapping>
                        {
                            BuildLineMapping(
                                documentAbsoluteIndex: 14,
                                documentLineIndex: 0,
                                documentCharacterOffsetIndex: 14,
                                generatedAbsoluteIndex: 374,
                                generatedLineIndex: 12,
                                generatedCharacterOffsetIndex: 48,
                                contentLength: 9),
                            BuildLineMapping(
                                documentAbsoluteIndex: 100,
                                documentLineIndex: 3,
                                generatedAbsoluteIndex: 896,
                                generatedLineIndex: 27,
                                characterOffsetIndex: 29,
                                contentLength: 12),
                            BuildLineMapping(
                                documentAbsoluteIndex: 198,
                                documentLineIndex: 5,
                                generatedAbsoluteIndex: 1237,
                                generatedLineIndex: 34,
                                characterOffsetIndex: 51,
                                contentLength: 12),
                            BuildLineMapping(
                                documentAbsoluteIndex: 221,
                                documentLineIndex: 5,
                                generatedAbsoluteIndex: 1547,
                                generatedLineIndex: 41,
                                characterOffsetIndex: 74,
                                contentLength: 4),
                        }
                    },
                    {
                        "AttributeTargetingTagHelpers",
                        "AttributeTargetingTagHelpers.DesignTime",
                        AttributeTargetingTagHelperDescriptors,
                        new List<LineMapping>
                        {
                            BuildLineMapping(
                                documentAbsoluteIndex: 14,
                                documentLineIndex: 0,
                                documentCharacterOffsetIndex: 14,
                                generatedAbsoluteIndex: 385,
                                generatedLineIndex: 12,
                                generatedCharacterOffsetIndex: 48,
                                contentLength: 12),
                            BuildLineMapping(
                                documentAbsoluteIndex: 184,
                                documentLineIndex: 5,
                                documentCharacterOffsetIndex: 36,
                                generatedAbsoluteIndex: 1598,
                                generatedLineIndex: 34,
                                generatedCharacterOffsetIndex: 42,
                                contentLength: 4),
                            BuildLineMapping(
                                documentAbsoluteIndex: 230,
                                documentLineIndex: 6,
                                documentCharacterOffsetIndex: 36,
                                generatedAbsoluteIndex: 2194,
                                generatedLineIndex: 44,
                                generatedCharacterOffsetIndex: 42,
                                contentLength: 4),
                        }
                    },
                    {
                        "PrefixedAttributeTagHelpers",
                        "PrefixedAttributeTagHelpers.DesignTime",
                        PrefixedAttributeTagHelperDescriptors,
                        new List<LineMapping>
                        {
                            BuildLineMapping(
                                documentAbsoluteIndex: 14,
                                documentLineIndex: 0,
                                documentCharacterOffsetIndex: 14,
                                generatedAbsoluteIndex: 384,
                                generatedLineIndex: 12,
                                generatedCharacterOffsetIndex: 48,
                                contentLength: 15),
                            BuildLineMapping(
                                documentAbsoluteIndex: 35,
                                documentLineIndex: 2,
                                generatedAbsoluteIndex: 907,
                                generatedLineIndex: 27,
                                characterOffsetIndex: 2,
                                contentLength: 242),
                            BuildLineMapping(
                                documentAbsoluteIndex: 368,
                                documentLineIndex: 15,
                                documentCharacterOffsetIndex: 43,
                                generatedAbsoluteIndex: 1495,
                                generatedLineIndex: 44,
                                generatedCharacterOffsetIndex: 56,
                                contentLength: 13),
                            BuildLineMapping(
                                documentAbsoluteIndex: 402,
                                documentLineIndex: 15,
                                generatedAbsoluteIndex: 1790,
                                generatedLineIndex: 50,
                                characterOffsetIndex: 77,
                                contentLength: 16),
                            BuildLineMapping(
                                documentAbsoluteIndex: 466,
                                documentLineIndex: 16,
                                documentCharacterOffsetIndex: 43,
                                generatedAbsoluteIndex: 2283,
                                generatedLineIndex: 58,
                                generatedCharacterOffsetIndex: 56,
                                contentLength: 13),
                            BuildLineMapping(
                                documentAbsoluteIndex: 500,
                                documentLineIndex: 16,
                                generatedAbsoluteIndex: 2578,
                                generatedLineIndex: 64,
                                characterOffsetIndex: 77,
                                contentLength: 2),
                            BuildLineMapping(
                                documentAbsoluteIndex: 524,
                                documentLineIndex: 16,
                                generatedAbsoluteIndex: 2906,
                                generatedLineIndex: 70,
                                characterOffsetIndex: 101,
                                contentLength: 2),
                            BuildLineMapping(
                                documentAbsoluteIndex: 588,
                                documentLineIndex: 18,
                                documentCharacterOffsetIndex: 31,
                                generatedAbsoluteIndex: 3370,
                                generatedLineIndex: 78,
                                generatedCharacterOffsetIndex: 46,
                                contentLength: 2),
                            BuildLineMapping(
                                documentAbsoluteIndex: 609,
                                documentLineIndex: 18,
                                documentCharacterOffsetIndex: 52,
                                generatedAbsoluteIndex: 3642,
                                generatedLineIndex: 84,
                                generatedCharacterOffsetIndex: 64,
                                contentLength: 2),
                            BuildLineMapping(
                                documentAbsoluteIndex: 632,
                                documentLineIndex: 18,
                                generatedAbsoluteIndex: 3940,
                                generatedLineIndex: 90,
                                characterOffsetIndex: 75,
                                contentLength: 2),
                            BuildLineMapping(
                                documentAbsoluteIndex: 781,
                                documentLineIndex: 20,
                                generatedAbsoluteIndex: 4665,
                                generatedLineIndex: 100,
                                characterOffsetIndex: 42,
                                contentLength: 8),
                            BuildLineMapping(
                                documentAbsoluteIndex: 824,
                                documentLineIndex: 21,
                                documentCharacterOffsetIndex: 29,
                                generatedAbsoluteIndex: 5272,
                                generatedLineIndex: 109,
                                generatedCharacterOffsetIndex: 65,
                                contentLength: 2),
                        }
                    },
                    {
                        "DuplicateAttributeTagHelpers",
                        "DuplicateAttributeTagHelpers.DesignTime",
                        DefaultPAndInputTagHelperDescriptors,
                        new List<LineMapping>
                        {
                            BuildLineMapping(
                                documentAbsoluteIndex: 14,
                                documentLineIndex: 0,
                                documentCharacterOffsetIndex: 14,
                                generatedAbsoluteIndex: 385,
                                generatedLineIndex: 12,
                                generatedCharacterOffsetIndex: 48,
                                contentLength: 15),
                            BuildLineMapping(
                                documentAbsoluteIndex: 144,
                                documentLineIndex: 4,
                                documentCharacterOffsetIndex: 34,
                                generatedAbsoluteIndex: 1749,
                                generatedLineIndex: 36,
                                generatedCharacterOffsetIndex: 42,
                                contentLength: 4),
                            BuildLineMapping(
                                documentAbsoluteIndex: 220,
                                documentLineIndex: 5,
                                documentCharacterOffsetIndex: 34,
                                generatedAbsoluteIndex: 2234,
                                generatedLineIndex: 45,
                                generatedCharacterOffsetIndex: 42,
                                contentLength: 4),
                            BuildLineMapping(
                                documentAbsoluteIndex: 41,
                                documentLineIndex: 2,
                                documentCharacterOffsetIndex: 8,
                                generatedAbsoluteIndex: 2447,
                                generatedLineIndex: 51,
                                generatedCharacterOffsetIndex: 33,
                                contentLength: 1),
                        }
                    },
                    {
                        "DynamicAttributeTagHelpers",
                        "DynamicAttributeTagHelpers.DesignTime",
                        DynamicAttributeTagHelpers_Descriptors,
                        new List<LineMapping>
                        {
                            BuildLineMapping(
                                documentAbsoluteIndex: 14,
                                documentLineIndex: 0,
                                documentCharacterOffsetIndex: 14,
                                generatedAbsoluteIndex: 383,
                                generatedLineIndex: 12,
                                generatedCharacterOffsetIndex: 48,
                                contentLength: 15),
                            BuildLineMapping(
                                documentAbsoluteIndex: 57,
                                documentLineIndex: 2,
                                generatedAbsoluteIndex: 932,
                                generatedLineIndex: 27,
                                characterOffsetIndex: 24,
                                contentLength: 12),
                            BuildLineMapping(
                                documentAbsoluteIndex: 94,
                                documentLineIndex: 4,
                                documentCharacterOffsetIndex: 17,
                                generatedAbsoluteIndex: 1142,
                                generatedLineIndex: 33,
                                generatedCharacterOffsetIndex: 16,
                                contentLength: 12),
                            BuildLineMapping(
                                documentAbsoluteIndex: 107,
                                documentLineIndex: 4,
                                generatedAbsoluteIndex: 1264,
                                generatedLineIndex: 39,
                                characterOffsetIndex: 30,
                                contentLength: 12),
                            BuildLineMapping(
                                documentAbsoluteIndex: 119,
                                documentLineIndex: 4,
                                generatedAbsoluteIndex: 1397,
                                generatedLineIndex: 44,
                                characterOffsetIndex: 42,
                                contentLength: 10),
                            BuildLineMapping(
                                documentAbsoluteIndex: 130,
                                documentLineIndex: 4,
                                generatedAbsoluteIndex: 1540,
                                generatedLineIndex: 50,
                                characterOffsetIndex: 53,
                                contentLength: 5),
                            BuildLineMapping(
                                documentAbsoluteIndex: 135,
                                documentLineIndex: 4,
                                generatedAbsoluteIndex: 1682,
                                generatedLineIndex: 55,
                                characterOffsetIndex: 58,
                                contentLength: 2),
                            BuildLineMapping(
                                documentAbsoluteIndex: 174,
                                documentLineIndex: 6,
                                generatedAbsoluteIndex: 1889,
                                generatedLineIndex: 62,
                                characterOffsetIndex: 22,
                                contentLength: 12),
                            BuildLineMapping(
                                documentAbsoluteIndex: 212,
                                documentLineIndex: 6,
                                generatedAbsoluteIndex: 2106,
                                generatedLineIndex: 68,
                                characterOffsetIndex: 60,
                                contentLength: 12),
                            BuildLineMapping(
                                documentAbsoluteIndex: 254,
                                documentLineIndex: 8,
                                generatedAbsoluteIndex: 2315,
                                generatedLineIndex: 74,
                                characterOffsetIndex: 15,
                                contentLength: 13),
                            BuildLineMapping(
                                documentAbsoluteIndex: 269,
                                documentLineIndex: 8,
                                documentCharacterOffsetIndex: 30,
                                generatedAbsoluteIndex: 2436,
                                generatedLineIndex: 79,
                                generatedCharacterOffsetIndex: 29,
                                contentLength: 12),
                            BuildLineMapping(
                                documentAbsoluteIndex: 282,
                                documentLineIndex: 8,
                                generatedAbsoluteIndex: 2571,
                                generatedLineIndex: 85,
                                characterOffsetIndex: 43,
                                contentLength: 12),
                            BuildLineMapping(
                                documentAbsoluteIndex: 294,
                                documentLineIndex: 8,
                                generatedAbsoluteIndex: 2717,
                                generatedLineIndex: 90,
                                characterOffsetIndex: 55,
                                contentLength: 10),
                            BuildLineMapping(
                                documentAbsoluteIndex: 305,
                                documentLineIndex: 8,
                                generatedAbsoluteIndex: 2873,
                                generatedLineIndex: 96,
                                characterOffsetIndex: 66,
                                contentLength: 5),
                            BuildLineMapping(
                                documentAbsoluteIndex: 310,
                                documentLineIndex: 8,
                                generatedAbsoluteIndex: 3028,
                                generatedLineIndex: 101,
                                characterOffsetIndex: 71,
                                contentLength: 2),
                            BuildLineMapping(
                                documentAbsoluteIndex: 314,
                                documentLineIndex: 8,
                                generatedAbsoluteIndex: 3185,
                                generatedLineIndex: 107,
                                characterOffsetIndex: 75,
                                contentLength: 12),
                            BuildLineMapping(
                                documentAbsoluteIndex: 346,
                                documentLineIndex: 9,
                                generatedAbsoluteIndex: 3360,
                                generatedLineIndex: 113,
                                characterOffsetIndex: 17,
                                contentLength: 13),
                            BuildLineMapping(
                                documentAbsoluteIndex: 361,
                                documentLineIndex: 9,
                                documentCharacterOffsetIndex: 32,
                                generatedAbsoluteIndex: 3484,
                                generatedLineIndex: 118,
                                generatedCharacterOffsetIndex: 31,
                                contentLength: 12),
                            BuildLineMapping(
                                documentAbsoluteIndex: 374,
                                documentLineIndex: 9,
                                generatedAbsoluteIndex: 3622,
                                generatedLineIndex: 124,
                                characterOffsetIndex: 45,
                                contentLength: 12),
                            BuildLineMapping(
                                documentAbsoluteIndex: 386,
                                documentLineIndex: 9,
                                generatedAbsoluteIndex: 3771,
                                generatedLineIndex: 129,
                                characterOffsetIndex: 57,
                                contentLength: 10),
                            BuildLineMapping(
                                documentAbsoluteIndex: 397,
                                documentLineIndex: 9,
                                generatedAbsoluteIndex: 3930,
                                generatedLineIndex: 135,
                                characterOffsetIndex: 68,
                                contentLength: 5),
                            BuildLineMapping(
                                documentAbsoluteIndex: 402,
                                documentLineIndex: 9,
                                generatedAbsoluteIndex: 4088,
                                generatedLineIndex: 140,
                                characterOffsetIndex: 73,
                                contentLength: 2),
                            BuildLineMapping(
                                documentAbsoluteIndex: 406,
                                documentLineIndex: 9,
                                generatedAbsoluteIndex: 4248,
                                generatedLineIndex: 146,
                                characterOffsetIndex: 77,
                                contentLength: 12),
                            BuildLineMapping(
                                documentAbsoluteIndex: 443,
                                documentLineIndex: 11,
                                generatedAbsoluteIndex: 4460,
                                generatedLineIndex: 152,
                                characterOffsetIndex: 17,
                                contentLength: 13),
                            BuildLineMapping(
                                documentAbsoluteIndex: 458,
                                documentLineIndex: 11,
                                generatedAbsoluteIndex: 4585,
                                generatedLineIndex: 157,
                                characterOffsetIndex: 32,
                                contentLength: 12),
                            BuildLineMapping(
                                documentAbsoluteIndex: 490,
                                documentLineIndex: 11,
                                generatedAbsoluteIndex: 4741,
                                generatedLineIndex: 162,
                                characterOffsetIndex: 64,
                                contentLength: 12),
                            BuildLineMapping(
                                documentAbsoluteIndex: 527,
                                documentLineIndex: 13,
                                documentCharacterOffsetIndex: 17,
                                generatedAbsoluteIndex: 4952,
                                generatedLineIndex: 168,
                                generatedCharacterOffsetIndex: 16,
                                contentLength: 12),
                            BuildLineMapping(
                                documentAbsoluteIndex: 540,
                                documentLineIndex: 13,
                                generatedAbsoluteIndex: 5075,
                                generatedLineIndex: 174,
                                characterOffsetIndex: 30,
                                contentLength: 12),
                            BuildLineMapping(
                                documentAbsoluteIndex: 552,
                                documentLineIndex: 13,
                                generatedAbsoluteIndex: 5209,
                                generatedLineIndex: 179,
                                characterOffsetIndex: 42,
                                contentLength: 10),
                            BuildLineMapping(
                                documentAbsoluteIndex: 563,
                                documentLineIndex: 13,
                                generatedAbsoluteIndex: 5353,
                                generatedLineIndex: 185,
                                characterOffsetIndex: 53,
                                contentLength: 5),
                            BuildLineMapping(
                                documentAbsoluteIndex: 568,
                                documentLineIndex: 13,
                                generatedAbsoluteIndex: 5496,
                                generatedLineIndex: 190,
                                characterOffsetIndex: 58,
                                contentLength: 2),
                        }
                    },
                    {
                        "TransitionsInTagHelperAttributes",
                        "TransitionsInTagHelperAttributes.DesignTime",
                        DefaultPAndInputTagHelperDescriptors,
                        new[]
                        {
                            BuildLineMapping(
                                documentAbsoluteIndex: 14,
                                documentLineIndex: 0,
                                documentCharacterOffsetIndex: 14,
                                generatedAbsoluteIndex: 389,
                                generatedLineIndex: 12,
                                generatedCharacterOffsetIndex: 48,
                                contentLength: 15),
                            BuildLineMapping(
                                documentAbsoluteIndex: 33,
                                documentLineIndex: 1,
                                generatedAbsoluteIndex: 817,
                                generatedLineIndex: 26,
                                characterOffsetIndex: 2,
                                contentLength: 59),
                            BuildLineMapping(
                                documentAbsoluteIndex: 120,
                                documentLineIndex: 6,
                                documentCharacterOffsetIndex: 23,
                                generatedAbsoluteIndex: 1088,
                                generatedLineIndex: 35,
                                generatedCharacterOffsetIndex: 33,
                                contentLength: 4),
                            BuildLineMapping(
                                documentAbsoluteIndex: 155,
                                documentLineIndex: 7,
                                generatedAbsoluteIndex: 1284,
                                generatedLineIndex: 41,
                                characterOffsetIndex: 12,
                                contentLength: 6),
                            BuildLineMapping(
                                documentAbsoluteIndex: 169,
                                documentLineIndex: 7,
                                documentCharacterOffsetIndex: 26,
                                generatedAbsoluteIndex: 1408,
                                generatedLineIndex: 46,
                                generatedCharacterOffsetIndex: 33,
                                contentLength: 2),
                            BuildLineMapping(
                                documentAbsoluteIndex: 200,
                                documentLineIndex: 8,
                                documentCharacterOffsetIndex: 21,
                                generatedAbsoluteIndex: 1623,
                                generatedLineIndex: 52,
                                generatedCharacterOffsetIndex: 33,
                                contentLength: 5),
                            BuildLineMapping(
                                documentAbsoluteIndex: 205,
                                documentLineIndex: 8,
                                documentCharacterOffsetIndex: 26,
                                generatedAbsoluteIndex: 1628,
                                generatedLineIndex: 52,
                                generatedCharacterOffsetIndex: 38,
                                contentLength: 1),
                            BuildLineMapping(
                                documentAbsoluteIndex: 206,
                                documentLineIndex: 8,
                                documentCharacterOffsetIndex: 27,
                                generatedAbsoluteIndex: 1629,
                                generatedLineIndex: 52,
                                generatedCharacterOffsetIndex: 39,
                                contentLength: 3),
                            BuildLineMapping(
                                documentAbsoluteIndex: 239,
                                documentLineIndex: 9,
                                documentCharacterOffsetIndex: 22,
                                generatedAbsoluteIndex: 1846,
                                generatedLineIndex: 58,
                                generatedCharacterOffsetIndex: 33,
                                contentLength: 3),
                            BuildLineMapping(
                                documentAbsoluteIndex: 272,
                                documentLineIndex: 10,
                                documentCharacterOffsetIndex: 22,
                                generatedAbsoluteIndex: 2063,
                                generatedLineIndex: 64,
                                generatedCharacterOffsetIndex: 33,
                                contentLength: 1),
                            BuildLineMapping(
                                documentAbsoluteIndex: 273,
                                documentLineIndex: 10,
                                documentCharacterOffsetIndex: 23,
                                generatedAbsoluteIndex: 2064,
                                generatedLineIndex: 64,
                                generatedCharacterOffsetIndex: 34,
                                contentLength: 4),
                            BuildLineMapping(
                                documentAbsoluteIndex: 277,
                                documentLineIndex: 10,
                                documentCharacterOffsetIndex: 27,
                                generatedAbsoluteIndex: 2068,
                                generatedLineIndex: 64,
                                generatedCharacterOffsetIndex: 38,
                                contentLength: 1),
                            BuildLineMapping(
                                documentAbsoluteIndex: 305,
                                documentLineIndex: 11,
                                generatedAbsoluteIndex: 2269,
                                generatedLineIndex: 70,
                                characterOffsetIndex: 19,
                                contentLength: 6),
                            BuildLineMapping(
                                documentAbsoluteIndex: 319,
                                documentLineIndex: 11,
                                generatedAbsoluteIndex: 2394,
                                generatedLineIndex: 75,
                                characterOffsetIndex: 33,
                                contentLength: 4),
                            BuildLineMapping(
                                documentAbsoluteIndex: 323,
                                documentLineIndex: 11,
                                generatedAbsoluteIndex: 2398,
                                generatedLineIndex: 75,
                                characterOffsetIndex: 37,
                                contentLength: 2),
                            BuildLineMapping(
                                documentAbsoluteIndex: 325,
                                documentLineIndex: 11,
                                generatedAbsoluteIndex: 2400,
                                generatedLineIndex: 75,
                                characterOffsetIndex: 39,
                                contentLength: 8),
                            BuildLineMapping(
                                documentAbsoluteIndex: 333,
                                documentLineIndex: 11,
                                generatedAbsoluteIndex: 2408,
                                generatedLineIndex: 75,
                                characterOffsetIndex: 47,
                                contentLength: 1),
                        }
                    },
                    {
                        "NestedScriptTagTagHelpers",
                        "NestedScriptTagTagHelpers.DesignTime",
                        DefaultPAndInputTagHelperDescriptors,
                        new[]
                        {
                            BuildLineMapping(
                                documentAbsoluteIndex: 14,
                                documentLineIndex: 0,
                                documentCharacterOffsetIndex: 14,
                                generatedAbsoluteIndex: 382,
                                generatedLineIndex: 12,
                                generatedCharacterOffsetIndex: 48,
                                contentLength: 15),
                            BuildLineMapping(
                                documentAbsoluteIndex: 180,
                                documentLineIndex: 5,
                                generatedAbsoluteIndex: 982,
                                generatedLineIndex: 28,
                                characterOffsetIndex: 0,
                                contentLength: 12),
                            BuildLineMapping(
                                documentAbsoluteIndex: 193,
                                documentLineIndex: 5,
                                documentCharacterOffsetIndex: 13,
                                generatedAbsoluteIndex: 1085,
                                generatedLineIndex: 34,
                                generatedCharacterOffsetIndex: 12,
                                contentLength: 30),
                            BuildLineMapping(
                                documentAbsoluteIndex: 337,
                                documentLineIndex: 7,
                                generatedAbsoluteIndex: 1450,
                                generatedLineIndex: 42,
                                characterOffsetIndex: 50,
                                contentLength: 23),
                            BuildLineMapping(
                                documentAbsoluteIndex: 387,
                                documentLineIndex: 7,
                                generatedAbsoluteIndex: 1799,
                                generatedLineIndex: 49,
                                characterOffsetIndex: 100,
                                contentLength: 4),
                            BuildLineMapping(
                                documentAbsoluteIndex: 422,
                                documentLineIndex: 9,
                                generatedAbsoluteIndex: 1882,
                                generatedLineIndex: 54,
                                characterOffsetIndex: 0,
                                contentLength: 15),
                        }
                    },
                    {
                        "SymbolBoundAttributes",
                        "SymbolBoundAttributes.DesignTime",
                        SymbolBoundTagHelperDescriptors,
                        new[]
                        {
                            BuildLineMapping(
                                documentAbsoluteIndex: 14,
                                documentLineIndex: 0,
                                documentCharacterOffsetIndex: 14,
                                generatedAbsoluteIndex: 378,
                                generatedLineIndex: 12,
                                generatedCharacterOffsetIndex: 48,
                                contentLength: 7),
                            BuildLineMapping(
                                documentAbsoluteIndex: 294,
                                documentLineIndex: 11,
                                documentCharacterOffsetIndex: 18,
                                generatedAbsoluteIndex: 944,
                                generatedLineIndex: 27,
                                generatedCharacterOffsetIndex: 46,
                                contentLength: 5),
                            BuildLineMapping(
                                documentAbsoluteIndex: 343,
                                documentLineIndex: 12,
                                documentCharacterOffsetIndex: 20,
                                generatedAbsoluteIndex: 1180,
                                generatedLineIndex: 33,
                                generatedCharacterOffsetIndex: 47,
                                contentLength: 5),
                            BuildLineMapping(
                                documentAbsoluteIndex: 397,
                                documentLineIndex: 13,
                                documentCharacterOffsetIndex: 23,
                                generatedAbsoluteIndex: 1412,
                                generatedLineIndex: 39,
                                generatedCharacterOffsetIndex: 43,
                                contentLength: 13),
                            BuildLineMapping(
                                documentAbsoluteIndex: 479,
                                documentLineIndex: 14,
                                documentCharacterOffsetIndex: 24,
                                generatedAbsoluteIndex: 1652,
                                generatedLineIndex: 45,
                                generatedCharacterOffsetIndex: 43,
                                contentLength: 13),
                        }
                    },
                    {
                        "EnumTagHelpers",
                        "EnumTagHelpers.DesignTime",
                        EnumTagHelperDescriptors,
                        new[]
                        {
                            BuildLineMapping(
                                documentAbsoluteIndex: 14,
                                documentLineIndex: 0,
                                documentCharacterOffsetIndex: 14,
                                generatedAbsoluteIndex: 371,
                                generatedLineIndex: 12,
                                generatedCharacterOffsetIndex: 48,
                                contentLength: 15),
                            BuildLineMapping(
                                documentAbsoluteIndex: 35,
                                documentLineIndex: 2,
                                generatedAbsoluteIndex: 870,
                                generatedLineIndex: 27,
                                characterOffsetIndex: 2,
                                contentLength: 39),
                            BuildLineMapping(
                                documentAbsoluteIndex: 94,
                                documentLineIndex: 6,
                                documentCharacterOffsetIndex: 15,
                                generatedAbsoluteIndex: 1226,
                                generatedLineIndex: 36,
                                generatedCharacterOffsetIndex: 39,
                                contentLength: 14),
                            BuildLineMapping(
                                documentAbsoluteIndex: 129,
                                documentLineIndex: 7,
                                generatedAbsoluteIndex: 1534,
                                generatedLineIndex: 43,
                                characterOffsetIndex: 15,
                                contentLength: 20),
                            BuildLineMapping(
                                documentAbsoluteIndex: 169,
                                documentLineIndex: 8,
                                documentCharacterOffsetIndex: 14,
                                generatedAbsoluteIndex: 1934,
                                generatedLineIndex: 50,
                                generatedCharacterOffsetIndex: 101,
                                contentLength: 7),
                            BuildLineMapping(
                                documentAbsoluteIndex: 196,
                                documentLineIndex: 9,
                                documentCharacterOffsetIndex: 14,
                                generatedAbsoluteIndex: 2322,
                                generatedLineIndex: 57,
                                generatedCharacterOffsetIndex: 101,
                                contentLength: 13),
                            BuildLineMapping(
                                documentAbsoluteIndex: 222,
                                documentLineIndex: 9,
                                documentCharacterOffsetIndex: 40,
                                generatedAbsoluteIndex: 2510,
                                generatedLineIndex: 62,
                                generatedCharacterOffsetIndex: 107,
                                contentLength: 7),
                            BuildLineMapping(
                                documentAbsoluteIndex: 249,
                                documentLineIndex: 10,
                                documentCharacterOffsetIndex: 15,
                                generatedAbsoluteIndex: 2836,
                                generatedLineIndex: 69,
                                generatedCharacterOffsetIndex: 39,
                                contentLength: 9),
                            BuildLineMapping(
                                documentAbsoluteIndex: 272,
                                documentLineIndex: 10,
                                documentCharacterOffsetIndex: 38,
                                generatedAbsoluteIndex: 2958,
                                generatedLineIndex: 74,
                                generatedCharacterOffsetIndex: 45,
                                contentLength: 9),
                        }
                    }
                };
            }
        }

        [Theory]
        [MemberData(nameof(DesignTimeTagHelperTestData))]
        public void TagHelpers_GenerateExpectedDesignTimeOutput(
            string testName,
            string baseLineName,
            IEnumerable<TagHelperDescriptor> tagHelperDescriptors,
            IList<LineMapping> expectedDesignTimePragmas)
        {
            // Act & Assert
            RunTagHelperTest(testName,
                             baseLineName,
                             designTimeMode: true,
                             tagHelperDescriptors: tagHelperDescriptors,
                             expectedDesignTimePragmas: expectedDesignTimePragmas);
        }

        public static TheoryData RuntimeTimeTagHelperTestData
        {
            get
            {
                // Test resource name, expected TagHelperDescriptors
                // Note: The baseline resource name is equivalent to the test resource name.
                return new TheoryData<string, string, IEnumerable<TagHelperDescriptor>>
                {
                    { "IncompleteTagHelper", null, DefaultPAndInputTagHelperDescriptors },
                    { "SingleTagHelper", null, DefaultPAndInputTagHelperDescriptors },
                    { "SingleTagHelperWithNewlineBeforeAttributes", null, DefaultPAndInputTagHelperDescriptors },
                    { "TagHelpersWithWeirdlySpacedAttributes", null, DefaultPAndInputTagHelperDescriptors },
                    { "BasicTagHelpers", null, DefaultPAndInputTagHelperDescriptors },
                    { "BasicTagHelpers.RemoveTagHelper", null, DefaultPAndInputTagHelperDescriptors },
                    { "BasicTagHelpers.Prefixed", null, PrefixedPAndInputTagHelperDescriptors },
                    { "ComplexTagHelpers", null, DefaultPAndInputTagHelperDescriptors },
                    { "DuplicateTargetTagHelper", null, DuplicateTargetTagHelperDescriptors },
                    { "EmptyAttributeTagHelpers", null, DefaultPAndInputTagHelperDescriptors },
                    { "EscapedTagHelpers", null, DefaultPAndInputTagHelperDescriptors },
                    { "AttributeTargetingTagHelpers", null, AttributeTargetingTagHelperDescriptors },
                    { "PrefixedAttributeTagHelpers", null, PrefixedAttributeTagHelperDescriptors },
                    {
                        "PrefixedAttributeTagHelpers",
                        "PrefixedAttributeTagHelpers.Reversed",
                        PrefixedAttributeTagHelperDescriptors.Reverse()
                    },
                    { "DuplicateAttributeTagHelpers", null, DefaultPAndInputTagHelperDescriptors },
                    { "DynamicAttributeTagHelpers", null, DynamicAttributeTagHelpers_Descriptors },
                    { "TransitionsInTagHelperAttributes", null, DefaultPAndInputTagHelperDescriptors },
                    { "NestedScriptTagTagHelpers", null, DefaultPAndInputTagHelperDescriptors },
                    { "SymbolBoundAttributes", null, SymbolBoundTagHelperDescriptors },
                    { "EnumTagHelpers", null, EnumTagHelperDescriptors },
                };
            }
        }

        [Theory]
        [MemberData(nameof(RuntimeTimeTagHelperTestData))]
        public void TagHelpers_GenerateExpectedRuntimeOutput(
            string testName,
            string baseLineName,
            IEnumerable<TagHelperDescriptor> tagHelperDescriptors)
        {
            // Arrange & Act & Assert
            RunTagHelperTest(testName, baseLineName, tagHelperDescriptors: tagHelperDescriptors);
        }

        [ConditionalTheory]
        [FrameworkSkipCondition(RuntimeFrameworks.Mono)]
        public void TagHelpers_CssSelectorTagHelperAttributesGeneratesExpectedOutput()
        {
            // Arrange & Act & Assert
            RunTagHelperTest(
                testName: "CssSelectorTagHelperAttributes",
                tagHelperDescriptors: CssSelectorTagHelperDescriptors);
        }

        [Fact]
        public void CSharpChunkGenerator_CorrectlyGeneratesMappings_ForRemoveTagHelperDirective()
        {
            // Act & Assert
            RunTagHelperTest(
                "RemoveTagHelperDirective",
                designTimeMode: true,
                expectedDesignTimePragmas: new List<LineMapping>()
                {
                    BuildLineMapping(
                        documentAbsoluteIndex: 17,
                        documentLineIndex: 0,
                        documentCharacterOffsetIndex: 17,
                        generatedAbsoluteIndex: 381,
                        generatedLineIndex: 12,
                        generatedCharacterOffsetIndex: 48,
                        contentLength: 15),
                });
        }

        [Fact]
        public void CSharpChunkGenerator_CorrectlyGeneratesMappings_ForAddTagHelperDirective()
        {
            // Act & Assert
            RunTagHelperTest(
                "AddTagHelperDirective",
                designTimeMode: true,
                expectedDesignTimePragmas: new List<LineMapping>()
                {
                    BuildLineMapping(
                        documentAbsoluteIndex: 14,
                        documentLineIndex: 0,
                        documentCharacterOffsetIndex: 14,
                        generatedAbsoluteIndex: 378,
                        generatedLineIndex: 12,
                        generatedCharacterOffsetIndex: 48,
                        contentLength: 15),
                });
        }

        [Fact]
        public void TagHelpers_Directive_GenerateDesignTimeMappings()
        {
            // Act & Assert
            RunTagHelperTest("AddTagHelperDirective",
                             designTimeMode: true,
                             tagHelperDescriptors: new[]
                             {
                                 new TagHelperDescriptor
                                 {
                                     TagName = "p",
                                     TypeName = "TestNamespace.PTagHelper",
                                     AssemblyName = "SomeAssembly"
                                 }
                             });
        }

        [Fact]
        public void TagHelpers_WithinHelpersAndSections_GeneratesExpectedOutput()
        {
            // Arrange
            var propertyInfo = typeof(TestType).GetProperty("BoundProperty");
            var tagHelperDescriptors = new[]
            {
                new TagHelperDescriptor
                {
                    TagName = "MyTagHelper",
                    TypeName = "TestNamespace.MyTagHelper",
                    AssemblyName = "SomeAssembly",
                    Attributes = new []
                    {
                        new TagHelperAttributeDescriptor("BoundProperty", propertyInfo)
                    }
                },
                new TagHelperDescriptor
                {
                    TagName = "NestedTagHelper",
                    TypeName = "TestNamespace.NestedTagHelper",
                    AssemblyName = "SomeAssembly"
                }
            };

            // Act & Assert
            RunTagHelperTest("TagHelpersInSection", tagHelperDescriptors: tagHelperDescriptors);
        }

        private static IEnumerable<TagHelperDescriptor> BuildPAndInputTagHelperDescriptors(string prefix)
        {
            var pAgePropertyInfo = typeof(TestType).GetProperty("Age");
            var inputTypePropertyInfo = typeof(TestType).GetProperty("Type");
            var checkedPropertyInfo = typeof(TestType).GetProperty("Checked");

            return new[]
            {
                new TagHelperDescriptor
                {
                    Prefix = prefix,
                    TagName = "p",
                    TypeName = "TestNamespace.PTagHelper",
                    AssemblyName = "SomeAssembly",
                    Attributes = new TagHelperAttributeDescriptor[]
                    {
                        new TagHelperAttributeDescriptor("age", pAgePropertyInfo)
                    },
                    TagStructure = TagStructure.NormalOrSelfClosing
                },
                new TagHelperDescriptor
                {
                    Prefix = prefix,
                    TagName = "input",
                    TypeName = "TestNamespace.InputTagHelper",
                    AssemblyName = "SomeAssembly",
                    Attributes = new TagHelperAttributeDescriptor[]
                    {
                        new TagHelperAttributeDescriptor("type", inputTypePropertyInfo)
                    },
                    TagStructure = TagStructure.WithoutEndTag
                },
                new TagHelperDescriptor
                {
                    Prefix = prefix,
                    TagName = "input",
                    TypeName = "TestNamespace.InputTagHelper2",
                    AssemblyName = "SomeAssembly",
                    Attributes = new TagHelperAttributeDescriptor[]
                    {
                        new TagHelperAttributeDescriptor("type", inputTypePropertyInfo),
                        new TagHelperAttributeDescriptor("checked", checkedPropertyInfo)
                    },
                }
            };
        }

        private class TestType
        {
            public int Age { get; set; }

            public string Type { get; set; }

            public bool Checked { get; set; }

            public string BoundProperty { get; set; }
        }
    }

    public enum MyEnum
    {
        MyValue,
        MySecondValue
    }
}
