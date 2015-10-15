// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
#if DNXCORE50
using System.Reflection;
#endif
using Microsoft.AspNet.Razor.CodeGenerators;
using Microsoft.AspNet.Razor.Runtime.TagHelpers;
using Microsoft.AspNet.Razor.TagHelpers;
using Xunit;

namespace Microsoft.AspNet.Razor.Test.Generator
{
    public class CSharpTagHelperRenderingTest : TagHelperTestBase
    {
        private static IEnumerable<TagHelperDescriptor> DefaultPAndInputTagHelperDescriptors { get; }
            = BuildPAndInputTagHelperDescriptors(prefix: string.Empty);
        private static IEnumerable<TagHelperDescriptor> PrefixedPAndInputTagHelperDescriptors { get; }
            = BuildPAndInputTagHelperDescriptors(prefix: "THS");

        private static IEnumerable<TagHelperDescriptor> SymbolBoundTagHelperDescriptors
        {
            get
            {
                return new[]
                {
                    new TagHelperDescriptor
                    {
                        TagName = "*",
                        TypeName = "CatchAllTagHelper",
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
                        RequiredAttributes = new[] { "bound" },
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
                        TypeName = "CatchAllTagHelper",
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
                        RequiredAttributes = new[] { "catchall-unbound-required" },
                    },
                    new TagHelperDescriptor
                    {
                        TagName = "input",
                        TypeName = "InputTagHelper",
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
                        RequiredAttributes = new[] { "input-bound-required-string", "input-unbound-required" },
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
                        TypeName = "InputTagHelper",
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
                        TypeName = "CatchAllTagHelper",
                        AssemblyName = "SomeAssembly",
                        Attributes = new TagHelperAttributeDescriptor[]
                        {
                            new TagHelperAttributeDescriptor("type", inputTypePropertyInfo),
                            new TagHelperAttributeDescriptor("checked", inputCheckedPropertyInfo)
                        },
                        RequiredAttributes = new[] { "type" },
                    },
                    new TagHelperDescriptor
                    {
                        TagName = "*",
                        TypeName = "CatchAllTagHelper",
                        AssemblyName = "SomeAssembly",
                        Attributes = new TagHelperAttributeDescriptor[]
                        {
                            new TagHelperAttributeDescriptor("type", inputTypePropertyInfo),
                            new TagHelperAttributeDescriptor("checked", inputCheckedPropertyInfo)
                        },
                        RequiredAttributes = new[] { "checked" },
                    },
                    new TagHelperDescriptor
                    {
                        TagName = "input",
                        TypeName = "InputTagHelper",
                        AssemblyName = "SomeAssembly",
                        Attributes = new TagHelperAttributeDescriptor[]
                        {
                            new TagHelperAttributeDescriptor("type", inputTypePropertyInfo),
                            new TagHelperAttributeDescriptor("checked", inputCheckedPropertyInfo)
                        },
                        RequiredAttributes = new[] { "type" },
                    },
                    new TagHelperDescriptor
                    {
                        TagName = "input",
                        TypeName = "InputTagHelper",
                        AssemblyName = "SomeAssembly",
                        Attributes = new TagHelperAttributeDescriptor[]
                        {
                            new TagHelperAttributeDescriptor("type", inputTypePropertyInfo),
                            new TagHelperAttributeDescriptor("checked", inputCheckedPropertyInfo)
                        },
                        RequiredAttributes = new[] { "checked" },
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
                        TypeName = "PTagHelper",
                        AssemblyName = "SomeAssembly",
                        RequiredAttributes = new[] { "class" },
                    },
                    new TagHelperDescriptor
                    {
                        TagName = "input",
                        TypeName = "InputTagHelper",
                        AssemblyName = "SomeAssembly",
                        Attributes = new TagHelperAttributeDescriptor[]
                        {
                            new TagHelperAttributeDescriptor("type", inputTypePropertyInfo)
                        },
                        RequiredAttributes = new[] { "type" },
                    },
                    new TagHelperDescriptor
                    {
                        TagName = "input",
                        TypeName = "InputTagHelper2",
                        AssemblyName = "SomeAssembly",
                        Attributes = new TagHelperAttributeDescriptor[]
                        {
                            new TagHelperAttributeDescriptor("type", inputTypePropertyInfo),
                            new TagHelperAttributeDescriptor("checked", inputCheckedPropertyInfo)
                        },
                        RequiredAttributes = new[] { "type", "checked" },
                    },
                    new TagHelperDescriptor
                    {
                        TagName = "*",
                        TypeName = "CatchAllTagHelper",
                        AssemblyName = "SomeAssembly",
                        RequiredAttributes = new[] { "catchAll" },
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
                        TypeName = "InputTagHelper1",
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
                        TypeName = "InputTagHelper2",
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
                return new TheoryData<string, // Test name
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
                    Assert.Equal(expectedTagHelperDescriptors,
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
                            BuildLineMapping(documentAbsoluteIndex: 14,
                                             documentLineIndex: 0,
                                             generatedAbsoluteIndex: 475,
                                             generatedLineIndex: 15,
                                             characterOffsetIndex: 14,
                                             contentLength: 17),
                            BuildLineMapping(documentAbsoluteIndex: 63,
                                             documentLineIndex: 2,
                                             generatedAbsoluteIndex: 964,
                                             generatedLineIndex: 34,
                                             characterOffsetIndex: 28,
                                             contentLength: 4)
                        }
                    },
                    {
                        "BasicTagHelpers",
                        "BasicTagHelpers.DesignTime",
                        DefaultPAndInputTagHelperDescriptors,
                        new List<LineMapping>
                        {
                            BuildLineMapping(
                                documentAbsoluteIndex: 14,
                                documentLineIndex: 0,
                                generatedAbsoluteIndex: 475,
                                generatedLineIndex: 15,
                                characterOffsetIndex: 14,
                                contentLength: 17),
                            BuildLineMapping(
                                documentAbsoluteIndex: 202,
                                documentLineIndex: 5,
                                generatedAbsoluteIndex: 1226,
                                generatedLineIndex: 38,
                                characterOffsetIndex: 38,
                                contentLength: 23),
                            BuildLineMapping(
                                documentAbsoluteIndex: 285,
                                documentLineIndex: 6,
                                generatedAbsoluteIndex: 1709,
                                generatedLineIndex: 49,
                                characterOffsetIndex: 40,
                                contentLength: 4),
                        }
                    },
                    {
                        "BasicTagHelpers.Prefixed",
                        "BasicTagHelpers.Prefixed.DesignTime",
                        PrefixedPAndInputTagHelperDescriptors,
                        new List<LineMapping>
                        {
                            BuildLineMapping(
                                documentAbsoluteIndex: 17,
                                documentLineIndex: 0,
                                generatedAbsoluteIndex: 496,
                                generatedLineIndex: 15,
                                characterOffsetIndex: 17,
                                contentLength: 5),
                            BuildLineMapping(
                                documentAbsoluteIndex: 38,
                                documentLineIndex: 1,
                                generatedAbsoluteIndex: 655,
                                generatedLineIndex: 22,
                                characterOffsetIndex: 14,
                                contentLength: 17),
                            BuildLineMapping(
                                documentAbsoluteIndex: 226,
                                documentLineIndex: 7,
                                generatedAbsoluteIndex: 1480,
                                generatedLineIndex: 46,
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
                                generatedAbsoluteIndex: 479,
                                generatedLineIndex: 15,
                                characterOffsetIndex: 14,
                                contentLength: 17),
                            BuildLineMapping(
                                documentAbsoluteIndex: 36,
                                documentLineIndex: 2,
                                documentCharacterOffsetIndex: 1,
                                generatedAbsoluteIndex: 1001,
                                generatedLineIndex: 35,
                                generatedCharacterOffsetIndex: 0,
                                contentLength: 48),
                            BuildLineMapping(
                                documentAbsoluteIndex: 211,
                                documentLineIndex: 9,
                                generatedAbsoluteIndex: 1119,
                                generatedLineIndex: 44,
                                characterOffsetIndex: 0,
                                contentLength: 12),
                            BuildLineMapping(
                                documentAbsoluteIndex: 224,
                                documentLineIndex: 9,
                                documentCharacterOffsetIndex: 13,
                                generatedAbsoluteIndex: 1215,
                                generatedLineIndex: 50,
                                generatedCharacterOffsetIndex: 12,
                                contentLength: 27),
                            BuildLineMapping(
                                documentAbsoluteIndex: 352,
                                documentLineIndex: 12,
                                generatedAbsoluteIndex: 1613,
                                generatedLineIndex: 62,
                                characterOffsetIndex: 0,
                                contentLength: 48),
                            BuildLineMapping(
                                documentAbsoluteIndex: 446,
                                documentLineIndex: 15,
                                generatedAbsoluteIndex: 1913,
                                generatedLineIndex: 72,
                                characterOffsetIndex: 46,
                                contentLength: 8),
                            BuildLineMapping(
                                documentAbsoluteIndex: 463,
                                documentLineIndex: 15,
                                generatedAbsoluteIndex: 2167,
                                generatedLineIndex: 79,
                                characterOffsetIndex: 63,
                                contentLength: 4),
                            BuildLineMapping(
                                documentAbsoluteIndex: 507,
                                documentLineIndex: 16,
                                generatedAbsoluteIndex: 2468,
                                generatedLineIndex: 87,
                                characterOffsetIndex: 31,
                                contentLength: 30),
                            BuildLineMapping(
                                documentAbsoluteIndex: 574,
                                documentLineIndex: 17,
                                documentCharacterOffsetIndex: 30,
                                generatedAbsoluteIndex: 2846,
                                generatedLineIndex: 96,
                                generatedCharacterOffsetIndex: 29,
                                contentLength: 10),
                            BuildLineMapping(
                                documentAbsoluteIndex: 606,
                                documentLineIndex: 17,
                                documentCharacterOffsetIndex: 62,
                                generatedAbsoluteIndex: 2989,
                                generatedLineIndex: 102,
                                generatedCharacterOffsetIndex: 61,
                                contentLength: 1),
                            BuildLineMapping(
                                documentAbsoluteIndex: 607,
                                documentLineIndex: 17,
                                documentCharacterOffsetIndex: 63,
                                generatedAbsoluteIndex: 3124,
                                generatedLineIndex: 108,
                                generatedCharacterOffsetIndex: 62,
                                contentLength: 8),
                            BuildLineMapping(
                                documentAbsoluteIndex: 637,
                                documentLineIndex: 17,
                                documentCharacterOffsetIndex: 93,
                                generatedAbsoluteIndex: 3295,
                                generatedLineIndex: 114,
                                generatedCharacterOffsetIndex: 91,
                                contentLength: 1),
                            BuildLineMapping(
                                documentAbsoluteIndex: 638,
                                documentLineIndex: 17,
                                documentCharacterOffsetIndex: 94,
                                generatedAbsoluteIndex: 3460,
                                generatedLineIndex: 120,
                                generatedCharacterOffsetIndex: 92,
                                contentLength: 1),
                            BuildLineMapping(
                                documentAbsoluteIndex: 643,
                                documentLineIndex: 18,
                                generatedAbsoluteIndex: 3645,
                                generatedLineIndex: 128,
                                characterOffsetIndex: 0,
                                contentLength: 15),
                            BuildLineMapping(
                                documentAbsoluteIndex: 163,
                                documentLineIndex: 7,
                                generatedAbsoluteIndex: 3820,
                                generatedLineIndex: 135,
                                characterOffsetIndex: 32,
                                contentLength: 12),
                            BuildLineMapping(
                                documentAbsoluteIndex: 769,
                                documentLineIndex: 21,
                                generatedAbsoluteIndex: 3903,
                                generatedLineIndex: 140,
                                characterOffsetIndex: 0,
                                contentLength: 12),
                            BuildLineMapping(
                                documentAbsoluteIndex: 783,
                                documentLineIndex: 21,
                                generatedAbsoluteIndex: 4001,
                                generatedLineIndex: 146,
                                characterOffsetIndex: 14,
                                contentLength: 21),
                            BuildLineMapping(
                                documentAbsoluteIndex: 836,
                                documentLineIndex: 22,
                                documentCharacterOffsetIndex: 29,
                                generatedAbsoluteIndex: 4258,
                                generatedLineIndex: 154,
                                generatedCharacterOffsetIndex: 28,
                                contentLength: 1),
                            BuildLineMapping(
                                documentAbsoluteIndex: 837,
                                documentLineIndex: 22,
                                documentCharacterOffsetIndex: 30,
                                generatedAbsoluteIndex: 4259,
                                generatedLineIndex: 154,
                                generatedCharacterOffsetIndex: 29,
                                contentLength: 7),
                            BuildLineMapping(
                                documentAbsoluteIndex: 844,
                                documentLineIndex: 22,
                                documentCharacterOffsetIndex: 37,
                                generatedAbsoluteIndex: 4266,
                                generatedLineIndex: 154,
                                generatedCharacterOffsetIndex: 36,
                                contentLength: 1),
                            BuildLineMapping(
                                documentAbsoluteIndex: 711,
                                documentLineIndex: 20,
                                documentCharacterOffsetIndex: 39,
                                generatedAbsoluteIndex: 4435,
                                generatedLineIndex: 160,
                                generatedCharacterOffsetIndex: 38,
                                contentLength: 23),
                            BuildLineMapping(
                                documentAbsoluteIndex: 734,
                                documentLineIndex: 20,
                                documentCharacterOffsetIndex: 62,
                                generatedAbsoluteIndex: 4458,
                                generatedLineIndex: 160,
                                generatedCharacterOffsetIndex: 61,
                                contentLength: 7),
                            BuildLineMapping(
                                documentAbsoluteIndex: 976,
                                documentLineIndex: 25,
                                documentCharacterOffsetIndex: 61,
                                generatedAbsoluteIndex: 4732,
                                generatedLineIndex: 167,
                                generatedCharacterOffsetIndex: 60,
                                contentLength: 1),
                            BuildLineMapping(
                                documentAbsoluteIndex: 977,
                                documentLineIndex: 25,
                                documentCharacterOffsetIndex: 62,
                                generatedAbsoluteIndex: 4733,
                                generatedLineIndex: 167,
                                generatedCharacterOffsetIndex: 61,
                                contentLength: 30),
                            BuildLineMapping(
                                documentAbsoluteIndex: 1007,
                                documentLineIndex: 25,
                                documentCharacterOffsetIndex: 92,
                                generatedAbsoluteIndex: 4763,
                                generatedLineIndex: 167,
                                generatedCharacterOffsetIndex: 91,
                                contentLength: 1),
                            BuildLineMapping(
                                documentAbsoluteIndex: 879,
                                documentLineIndex: 24,
                                documentCharacterOffsetIndex: 16,
                                generatedAbsoluteIndex: 4913,
                                generatedLineIndex: 173,
                                generatedCharacterOffsetIndex: 19,
                                contentLength: 8),
                            BuildLineMapping(
                                documentAbsoluteIndex: 887,
                                documentLineIndex: 24,
                                documentCharacterOffsetIndex: 24,
                                generatedAbsoluteIndex: 4921,
                                generatedLineIndex: 173,
                                generatedCharacterOffsetIndex: 27,
                                contentLength: 1),
                            BuildLineMapping(
                                documentAbsoluteIndex: 888,
                                documentLineIndex: 24,
                                documentCharacterOffsetIndex: 25,
                                generatedAbsoluteIndex: 4922,
                                generatedLineIndex: 173,
                                generatedCharacterOffsetIndex: 28,
                                contentLength: 23),
                            BuildLineMapping(
                                documentAbsoluteIndex: 1106,
                                documentLineIndex: 28,
                                generatedAbsoluteIndex: 5180,
                                generatedLineIndex: 180,
                                characterOffsetIndex: 28,
                                contentLength: 30),
                            BuildLineMapping(
                                documentAbsoluteIndex: 1044,
                                documentLineIndex: 27,
                                documentCharacterOffsetIndex: 16,
                                generatedAbsoluteIndex: 5359,
                                generatedLineIndex: 186,
                                generatedCharacterOffsetIndex: 19,
                                contentLength: 30),
                            BuildLineMapping(
                                documentAbsoluteIndex: 1234,
                                documentLineIndex: 31,
                                generatedAbsoluteIndex: 5624,
                                generatedLineIndex: 193,
                                characterOffsetIndex: 28,
                                contentLength: 3),
                            BuildLineMapping(
                                documentAbsoluteIndex: 1237,
                                documentLineIndex: 31,
                                generatedAbsoluteIndex: 5627,
                                generatedLineIndex: 193,
                                characterOffsetIndex: 31,
                                contentLength: 2),
                            BuildLineMapping(
                                documentAbsoluteIndex: 1239,
                                documentLineIndex: 31,
                                generatedAbsoluteIndex: 5629,
                                generatedLineIndex: 193,
                                characterOffsetIndex: 33,
                                contentLength: 27),
                            BuildLineMapping(
                                documentAbsoluteIndex: 1266,
                                documentLineIndex: 31,
                                generatedAbsoluteIndex: 5656,
                                generatedLineIndex: 193,
                                characterOffsetIndex: 60,
                                contentLength: 1),
                            BuildLineMapping(
                                documentAbsoluteIndex: 1267,
                                documentLineIndex: 31,
                                generatedAbsoluteIndex: 5657,
                                generatedLineIndex: 193,
                                characterOffsetIndex: 61,
                                contentLength: 10),
                            BuildLineMapping(
                                documentAbsoluteIndex: 1171,
                                documentLineIndex: 30,
                                documentCharacterOffsetIndex: 17,
                                generatedAbsoluteIndex: 5816,
                                generatedLineIndex: 199,
                                generatedCharacterOffsetIndex: 19,
                                contentLength: 1),
                            BuildLineMapping(
                                documentAbsoluteIndex: 1172,
                                documentLineIndex: 30,
                                documentCharacterOffsetIndex: 18,
                                generatedAbsoluteIndex: 5817,
                                generatedLineIndex: 199,
                                generatedCharacterOffsetIndex: 20,
                                contentLength: 29),
                            BuildLineMapping(
                                documentAbsoluteIndex: 1201,
                                documentLineIndex: 30,
                                documentCharacterOffsetIndex: 47,
                                generatedAbsoluteIndex: 5846,
                                generatedLineIndex: 199,
                                generatedCharacterOffsetIndex: 49,
                                contentLength: 1),
                            BuildLineMapping(
                                documentAbsoluteIndex: 1306,
                                documentLineIndex: 33,
                                generatedAbsoluteIndex: 5927,
                                generatedLineIndex: 204,
                                characterOffsetIndex: 9,
                                contentLength: 11),
                            BuildLineMapping(
                                documentAbsoluteIndex: 1361,
                                documentLineIndex: 33,
                                documentCharacterOffsetIndex: 64,
                                generatedAbsoluteIndex: 6216,
                                generatedLineIndex: 208,
                                generatedCharacterOffsetIndex: 63,
                                contentLength: 7),
                            BuildLineMapping(
                                documentAbsoluteIndex: 1326,
                                documentLineIndex: 33,
                                generatedAbsoluteIndex: 6374,
                                generatedLineIndex: 214,
                                characterOffsetIndex: 29,
                                contentLength: 3),
                            BuildLineMapping(
                                documentAbsoluteIndex: 1390,
                                documentLineIndex: 35,
                                generatedAbsoluteIndex: 6489,
                                generatedLineIndex: 225,
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
                                generatedAbsoluteIndex: 493,
                                generatedLineIndex: 15,
                                characterOffsetIndex: 14,
                                contentLength: 11),
                            BuildLineMapping(
                                documentAbsoluteIndex: 62,
                                documentLineIndex: 3,
                                documentCharacterOffsetIndex: 26,
                                generatedAbsoluteIndex: 1289,
                                generatedLineIndex: 39,
                                generatedCharacterOffsetIndex: 28,
                                contentLength: 0),
                            BuildLineMapping(
                                documentAbsoluteIndex: 122,
                                documentLineIndex: 5,
                                generatedAbsoluteIndex: 1634,
                                generatedLineIndex: 48,
                                characterOffsetIndex: 30,
                                contentLength: 0),
                            BuildLineMapping(
                                documentAbsoluteIndex: 88,
                                documentLineIndex: 4,
                                documentCharacterOffsetIndex: 12,
                                generatedAbsoluteIndex: 1789,
                                generatedLineIndex: 54,
                                generatedCharacterOffsetIndex: 19,
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
                                generatedAbsoluteIndex: 479,
                                generatedLineIndex: 15,
                                characterOffsetIndex: 14,
                                contentLength: 11),
                            BuildLineMapping(
                                documentAbsoluteIndex: 102,
                                documentLineIndex: 3,
                                generatedAbsoluteIndex: 975,
                                generatedLineIndex: 34,
                                characterOffsetIndex: 29,
                                contentLength: 12),
                            BuildLineMapping(
                                documentAbsoluteIndex: 200,
                                documentLineIndex: 5,
                                generatedAbsoluteIndex: 1244,
                                generatedLineIndex: 41,
                                characterOffsetIndex: 51,
                                contentLength: 12),
                            BuildLineMapping(
                                documentAbsoluteIndex: 223,
                                documentLineIndex: 5,
                                generatedAbsoluteIndex: 1512,
                                generatedLineIndex: 48,
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
                                generatedAbsoluteIndex: 501,
                                generatedLineIndex: 15,
                                characterOffsetIndex: 14,
                                contentLength: 14),
                            BuildLineMapping(
                                documentAbsoluteIndex: 186,
                                documentLineIndex: 5,
                                generatedAbsoluteIndex: 1460,
                                generatedLineIndex: 41,
                                characterOffsetIndex: 36,
                                contentLength: 4),
                            BuildLineMapping(
                                documentAbsoluteIndex: 232,
                                documentLineIndex: 6,
                                generatedAbsoluteIndex: 1900,
                                generatedLineIndex: 51,
                                characterOffsetIndex: 36,
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
                                generatedAbsoluteIndex: 499,
                                generatedLineIndex: 15,
                                characterOffsetIndex: 14,
                                contentLength: 17),
                            BuildLineMapping(
                                documentAbsoluteIndex: 37,
                                documentLineIndex: 2,
                                generatedAbsoluteIndex: 996,
                                generatedLineIndex: 34,
                                characterOffsetIndex: 2,
                                contentLength: 242),
                            BuildLineMapping(
                                documentAbsoluteIndex: 370,
                                documentLineIndex: 15,
                                generatedAbsoluteIndex: 1499,
                                generatedLineIndex: 51,
                                characterOffsetIndex: 43,
                                contentLength: 13),
                            BuildLineMapping(
                                documentAbsoluteIndex: 404,
                                documentLineIndex: 15,
                                generatedAbsoluteIndex: 1766,
                                generatedLineIndex: 57,
                                characterOffsetIndex: 77,
                                contentLength: 16),
                            BuildLineMapping(
                                documentAbsoluteIndex: 468,
                                documentLineIndex: 16,
                                generatedAbsoluteIndex: 2146,
                                generatedLineIndex: 65,
                                characterOffsetIndex: 43,
                                contentLength: 13),
                            BuildLineMapping(
                                documentAbsoluteIndex: 502,
                                documentLineIndex: 16,
                                generatedAbsoluteIndex: 2413,
                                generatedLineIndex: 71,
                                characterOffsetIndex: 77,
                                contentLength: 2),
                            BuildLineMapping(
                                documentAbsoluteIndex: 526,
                                documentLineIndex: 16,
                                generatedAbsoluteIndex: 2713,
                                generatedLineIndex: 77,
                                characterOffsetIndex: 101,
                                contentLength: 2),
                            BuildLineMapping(
                                documentAbsoluteIndex: 590,
                                documentLineIndex: 18,
                                documentCharacterOffsetIndex: 31,
                                generatedAbsoluteIndex: 3063,
                                generatedLineIndex: 85,
                                generatedCharacterOffsetIndex: 32,
                                contentLength: 2),
                            BuildLineMapping(
                                documentAbsoluteIndex: 611,
                                documentLineIndex: 18,
                                generatedAbsoluteIndex: 3295,
                                generatedLineIndex: 91,
                                characterOffsetIndex: 52,
                                contentLength: 2),
                            BuildLineMapping(
                                documentAbsoluteIndex: 634,
                                documentLineIndex: 18,
                                generatedAbsoluteIndex: 3565,
                                generatedLineIndex: 97,
                                characterOffsetIndex: 75,
                                contentLength: 2),
                            BuildLineMapping(
                                documentAbsoluteIndex: 783,
                                documentLineIndex: 20,
                                generatedAbsoluteIndex: 4178,
                                generatedLineIndex: 107,
                                characterOffsetIndex: 42,
                                contentLength: 8),
                            BuildLineMapping(
                                documentAbsoluteIndex: 826,
                                documentLineIndex: 21,
                                documentCharacterOffsetIndex: 29,
                                generatedAbsoluteIndex: 4657,
                                generatedLineIndex: 116,
                                generatedCharacterOffsetIndex: 51,
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
                                generatedAbsoluteIndex: 501,
                                generatedLineIndex: 15,
                                characterOffsetIndex: 14,
                                contentLength: 17),
                            BuildLineMapping(
                                documentAbsoluteIndex: 146,
                                documentLineIndex: 4,
                                generatedAbsoluteIndex: 1567,
                                generatedLineIndex: 43,
                                characterOffsetIndex: 34,
                                contentLength: 4),
                            BuildLineMapping(
                                documentAbsoluteIndex: 43,
                                documentLineIndex: 2,
                                documentCharacterOffsetIndex: 8,
                                generatedAbsoluteIndex: 1730,
                                generatedLineIndex: 49,
                                generatedCharacterOffsetIndex: 19,
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
                                generatedAbsoluteIndex: 497,
                                generatedLineIndex: 15,
                                characterOffsetIndex: 14,
                                contentLength: 17),
                            BuildLineMapping(
                                documentAbsoluteIndex: 59,
                                documentLineIndex: 2,
                                generatedAbsoluteIndex: 1020,
                                generatedLineIndex: 34,
                                characterOffsetIndex: 24,
                                contentLength: 12),
                            BuildLineMapping(
                                documentAbsoluteIndex: 96,
                                documentLineIndex: 4,
                                documentCharacterOffsetIndex: 17,
                                generatedAbsoluteIndex: 1194,
                                generatedLineIndex: 40,
                                generatedCharacterOffsetIndex: 16,
                                contentLength: 12),
                            BuildLineMapping(
                                documentAbsoluteIndex: 109,
                                documentLineIndex: 4,
                                generatedAbsoluteIndex: 1316,
                                generatedLineIndex: 46,
                                characterOffsetIndex: 30,
                                contentLength: 12),
                            BuildLineMapping(
                                documentAbsoluteIndex: 121,
                                documentLineIndex: 4,
                                generatedAbsoluteIndex: 1449,
                                generatedLineIndex: 51,
                                characterOffsetIndex: 42,
                                contentLength: 10),
                            BuildLineMapping(
                                documentAbsoluteIndex: 132,
                                documentLineIndex: 4,
                                generatedAbsoluteIndex: 1592,
                                generatedLineIndex: 57,
                                characterOffsetIndex: 53,
                                contentLength: 5),
                            BuildLineMapping(
                                documentAbsoluteIndex: 137,
                                documentLineIndex: 4,
                                generatedAbsoluteIndex: 1734,
                                generatedLineIndex: 62,
                                characterOffsetIndex: 58,
                                contentLength: 2),
                            BuildLineMapping(
                                documentAbsoluteIndex: 176,
                                documentLineIndex: 6,
                                generatedAbsoluteIndex: 1905,
                                generatedLineIndex: 69,
                                characterOffsetIndex: 22,
                                contentLength: 12),
                            BuildLineMapping(
                                documentAbsoluteIndex: 214,
                                documentLineIndex: 6,
                                generatedAbsoluteIndex: 2108,
                                generatedLineIndex: 75,
                                characterOffsetIndex: 60,
                                contentLength: 12),
                            BuildLineMapping(
                                documentAbsoluteIndex: 256,
                                documentLineIndex: 8,
                                generatedAbsoluteIndex: 2281,
                                generatedLineIndex: 81,
                                characterOffsetIndex: 15,
                                contentLength: 13),
                            BuildLineMapping(
                                documentAbsoluteIndex: 271,
                                documentLineIndex: 8,
                                documentCharacterOffsetIndex: 30,
                                generatedAbsoluteIndex: 2402,
                                generatedLineIndex: 86,
                                generatedCharacterOffsetIndex: 29,
                                contentLength: 12),
                            BuildLineMapping(
                                documentAbsoluteIndex: 284,
                                documentLineIndex: 8,
                                generatedAbsoluteIndex: 2537,
                                generatedLineIndex: 92,
                                characterOffsetIndex: 43,
                                contentLength: 12),
                            BuildLineMapping(
                                documentAbsoluteIndex: 296,
                                documentLineIndex: 8,
                                generatedAbsoluteIndex: 2683,
                                generatedLineIndex: 97,
                                characterOffsetIndex: 55,
                                contentLength: 10),
                            BuildLineMapping(
                                documentAbsoluteIndex: 307,
                                documentLineIndex: 8,
                                generatedAbsoluteIndex: 2839,
                                generatedLineIndex: 103,
                                characterOffsetIndex: 66,
                                contentLength: 5),
                            BuildLineMapping(
                                documentAbsoluteIndex: 312,
                                documentLineIndex: 8,
                                generatedAbsoluteIndex: 2994,
                                generatedLineIndex: 108,
                                characterOffsetIndex: 71,
                                contentLength: 2),
                            BuildLineMapping(
                                documentAbsoluteIndex: 316,
                                documentLineIndex: 8,
                                generatedAbsoluteIndex: 3151,
                                generatedLineIndex: 114,
                                characterOffsetIndex: 75,
                                contentLength: 12),
                            BuildLineMapping(
                                documentAbsoluteIndex: 348,
                                documentLineIndex: 9,
                                generatedAbsoluteIndex: 3312,
                                generatedLineIndex: 120,
                                characterOffsetIndex: 17,
                                contentLength: 13),
                            BuildLineMapping(
                                documentAbsoluteIndex: 363,
                                documentLineIndex: 9,
                                documentCharacterOffsetIndex: 32,
                                generatedAbsoluteIndex: 3436,
                                generatedLineIndex: 125,
                                generatedCharacterOffsetIndex: 31,
                                contentLength: 12),
                            BuildLineMapping(
                                documentAbsoluteIndex: 376,
                                documentLineIndex: 9,
                                generatedAbsoluteIndex: 3574,
                                generatedLineIndex: 131,
                                characterOffsetIndex: 45,
                                contentLength: 12),
                            BuildLineMapping(
                                documentAbsoluteIndex: 388,
                                documentLineIndex: 9,
                                generatedAbsoluteIndex: 3723,
                                generatedLineIndex: 136,
                                characterOffsetIndex: 57,
                                contentLength: 10),
                            BuildLineMapping(
                                documentAbsoluteIndex: 399,
                                documentLineIndex: 9,
                                generatedAbsoluteIndex: 3882,
                                generatedLineIndex: 142,
                                characterOffsetIndex: 68,
                                contentLength: 5),
                            BuildLineMapping(
                                documentAbsoluteIndex: 404,
                                documentLineIndex: 9,
                                generatedAbsoluteIndex: 4040,
                                generatedLineIndex: 147,
                                characterOffsetIndex: 73,
                                contentLength: 2),
                            BuildLineMapping(
                                documentAbsoluteIndex: 408,
                                documentLineIndex: 9,
                                generatedAbsoluteIndex: 4200,
                                generatedLineIndex: 153,
                                characterOffsetIndex: 77,
                                contentLength: 12),
                            BuildLineMapping(
                                documentAbsoluteIndex: 445,
                                documentLineIndex: 11,
                                generatedAbsoluteIndex: 4376,
                                generatedLineIndex: 159,
                                characterOffsetIndex: 17,
                                contentLength: 13),
                            BuildLineMapping(
                                documentAbsoluteIndex: 460,
                                documentLineIndex: 11,
                                generatedAbsoluteIndex: 4501,
                                generatedLineIndex: 164,
                                characterOffsetIndex: 32,
                                contentLength: 12),
                            BuildLineMapping(
                                documentAbsoluteIndex: 492,
                                documentLineIndex: 11,
                                generatedAbsoluteIndex: 4657,
                                generatedLineIndex: 169,
                                characterOffsetIndex: 64,
                                contentLength: 12),
                            BuildLineMapping(
                                documentAbsoluteIndex: 529,
                                documentLineIndex: 13,
                                documentCharacterOffsetIndex: 17,
                                generatedAbsoluteIndex: 4832,
                                generatedLineIndex: 175,
                                generatedCharacterOffsetIndex: 16,
                                contentLength: 12),
                            BuildLineMapping(
                                documentAbsoluteIndex: 542,
                                documentLineIndex: 13,
                                generatedAbsoluteIndex: 4955,
                                generatedLineIndex: 181,
                                characterOffsetIndex: 30,
                                contentLength: 12),
                            BuildLineMapping(
                                documentAbsoluteIndex: 554,
                                documentLineIndex: 13,
                                generatedAbsoluteIndex: 5089,
                                generatedLineIndex: 186,
                                characterOffsetIndex: 42,
                                contentLength: 10),
                            BuildLineMapping(
                                documentAbsoluteIndex: 565,
                                documentLineIndex: 13,
                                generatedAbsoluteIndex: 5233,
                                generatedLineIndex: 192,
                                characterOffsetIndex: 53,
                                contentLength: 5),
                            BuildLineMapping(
                                documentAbsoluteIndex: 570,
                                documentLineIndex: 13,
                                generatedAbsoluteIndex: 5376,
                                generatedLineIndex: 197,
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
                                generatedAbsoluteIndex: 509,
                                generatedLineIndex: 15,
                                characterOffsetIndex: 14,
                                contentLength: 17),
                            BuildLineMapping(
                                documentAbsoluteIndex: 35,
                                documentLineIndex: 1,
                                generatedAbsoluteIndex: 947,
                                generatedLineIndex: 33,
                                characterOffsetIndex: 2,
                                contentLength: 59),
                            BuildLineMapping(
                                documentAbsoluteIndex: 122,
                                documentLineIndex: 6,
                                generatedAbsoluteIndex: 1172,
                                generatedLineIndex: 42,
                                characterOffsetIndex: 23,
                                contentLength: 4),
                            BuildLineMapping(
                                documentAbsoluteIndex: 157,
                                documentLineIndex: 7,
                                generatedAbsoluteIndex: 1332,
                                generatedLineIndex: 48,
                                characterOffsetIndex: 12,
                                contentLength: 6),
                            BuildLineMapping(
                                documentAbsoluteIndex: 171,
                                documentLineIndex: 7,
                                generatedAbsoluteIndex: 1449,
                                generatedLineIndex: 53,
                                characterOffsetIndex: 26,
                                contentLength: 2),
                            BuildLineMapping(
                                documentAbsoluteIndex: 202,
                                documentLineIndex: 8,
                                generatedAbsoluteIndex: 1616,
                                generatedLineIndex: 59,
                                characterOffsetIndex: 21,
                                contentLength: 5),
                            BuildLineMapping(
                                documentAbsoluteIndex: 207,
                                documentLineIndex: 8,
                                generatedAbsoluteIndex: 1621,
                                generatedLineIndex: 59,
                                characterOffsetIndex: 26,
                                contentLength: 1),
                            BuildLineMapping(
                                documentAbsoluteIndex: 208,
                                documentLineIndex: 8,
                                generatedAbsoluteIndex: 1622,
                                generatedLineIndex: 59,
                                characterOffsetIndex: 27,
                                contentLength: 3),
                            BuildLineMapping(
                                documentAbsoluteIndex: 241,
                                documentLineIndex: 9,
                                documentCharacterOffsetIndex: 22,
                                generatedAbsoluteIndex: 1791,
                                generatedLineIndex: 65,
                                generatedCharacterOffsetIndex: 21,
                                contentLength: 3),
                            BuildLineMapping(
                                documentAbsoluteIndex: 274,
                                documentLineIndex: 10,
                                documentCharacterOffsetIndex: 22,
                                generatedAbsoluteIndex: 1960,
                                generatedLineIndex: 71,
                                generatedCharacterOffsetIndex: 21,
                                contentLength: 1),
                            BuildLineMapping(
                                documentAbsoluteIndex: 275,
                                documentLineIndex: 10,
                                documentCharacterOffsetIndex: 23,
                                generatedAbsoluteIndex: 1961,
                                generatedLineIndex: 71,
                                generatedCharacterOffsetIndex: 22,
                                contentLength: 4),
                            BuildLineMapping(
                                documentAbsoluteIndex: 279,
                                documentLineIndex: 10,
                                documentCharacterOffsetIndex: 27,
                                generatedAbsoluteIndex: 1965,
                                generatedLineIndex: 71,
                                generatedCharacterOffsetIndex: 26,
                                contentLength: 1),
                            BuildLineMapping(
                                documentAbsoluteIndex: 307,
                                documentLineIndex: 11,
                                generatedAbsoluteIndex: 2130,
                                generatedLineIndex: 77,
                                characterOffsetIndex: 19,
                                contentLength: 6),
                            BuildLineMapping(
                                documentAbsoluteIndex: 321,
                                documentLineIndex: 11,
                                generatedAbsoluteIndex: 2255,
                                generatedLineIndex: 82,
                                characterOffsetIndex: 33,
                                contentLength: 4),
                            BuildLineMapping(
                                documentAbsoluteIndex: 325,
                                documentLineIndex: 11,
                                generatedAbsoluteIndex: 2259,
                                generatedLineIndex: 82,
                                characterOffsetIndex: 37,
                                contentLength: 2),
                            BuildLineMapping(
                                documentAbsoluteIndex: 327,
                                documentLineIndex: 11,
                                generatedAbsoluteIndex: 2261,
                                generatedLineIndex: 82,
                                characterOffsetIndex: 39,
                                contentLength: 8),
                            BuildLineMapping(
                                documentAbsoluteIndex: 335,
                                documentLineIndex: 11,
                                generatedAbsoluteIndex: 2269,
                                generatedLineIndex: 82,
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
                                generatedAbsoluteIndex: 495,
                                generatedLineIndex: 15,
                                characterOffsetIndex: 14,
                                contentLength: 17),
                            BuildLineMapping(
                                documentAbsoluteIndex: 182,
                                documentLineIndex: 5,
                                generatedAbsoluteIndex: 1033,
                                generatedLineIndex: 35,
                                characterOffsetIndex: 0,
                                contentLength: 12),
                            BuildLineMapping(
                                documentAbsoluteIndex: 195,
                                documentLineIndex: 5,
                                documentCharacterOffsetIndex: 13,
                                generatedAbsoluteIndex: 1136,
                                generatedLineIndex: 41,
                                generatedCharacterOffsetIndex: 12,
                                contentLength: 30),
                            BuildLineMapping(
                                documentAbsoluteIndex: 339,
                                documentLineIndex: 7,
                                generatedAbsoluteIndex: 1429,
                                generatedLineIndex: 49,
                                characterOffsetIndex: 50,
                                contentLength: 23),
                            BuildLineMapping(
                                documentAbsoluteIndex: 389,
                                documentLineIndex: 7,
                                generatedAbsoluteIndex: 1736,
                                generatedLineIndex: 56,
                                characterOffsetIndex: 100,
                                contentLength: 4),
                            BuildLineMapping(
                                documentAbsoluteIndex: 424,
                                documentLineIndex: 9,
                                generatedAbsoluteIndex: 1819,
                                generatedLineIndex: 61,
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
                                generatedAbsoluteIndex: 487,
                                generatedLineIndex: 15,
                                characterOffsetIndex: 14,
                                contentLength: 9),
                            BuildLineMapping(
                                documentAbsoluteIndex: 296,
                                documentLineIndex: 11,
                                documentCharacterOffsetIndex: 18,
                                generatedAbsoluteIndex: 1013,
                                generatedLineIndex: 34,
                                generatedCharacterOffsetIndex: 32,
                                contentLength: 5),
                            BuildLineMapping(
                                documentAbsoluteIndex: 345,
                                documentLineIndex: 12,
                                documentCharacterOffsetIndex: 20,
                                generatedAbsoluteIndex: 1199,
                                generatedLineIndex: 40,
                                generatedCharacterOffsetIndex: 33,
                                contentLength: 5),
                            BuildLineMapping(
                                documentAbsoluteIndex: 399,
                                documentLineIndex: 13,
                                documentCharacterOffsetIndex: 23,
                                generatedAbsoluteIndex: 1381,
                                generatedLineIndex: 46,
                                generatedCharacterOffsetIndex: 29,
                                contentLength: 13),
                            BuildLineMapping(
                                documentAbsoluteIndex: 481,
                                documentLineIndex: 14,
                                documentCharacterOffsetIndex: 24,
                                generatedAbsoluteIndex: 1571,
                                generatedLineIndex: 52,
                                generatedCharacterOffsetIndex: 29,
                                contentLength: 13),
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

        [Fact]
        public void CSharpChunkGenerator_CorrectlyGeneratesMappings_ForRemoveTagHelperDirective()
        {
            // Act & Assert
            RunTagHelperTest("RemoveTagHelperDirective",
                             designTimeMode: true,
                             expectedDesignTimePragmas: new List<LineMapping>()
                             {
                                    BuildLineMapping(documentAbsoluteIndex: 17,
                                                     documentLineIndex: 0,
                                                     generatedAbsoluteIndex: 442,
                                                     generatedLineIndex: 14,
                                                     characterOffsetIndex: 17,
                                                     contentLength: 17)
                             });
        }

        [Fact]
        public void CSharpChunkGenerator_CorrectlyGeneratesMappings_ForAddTagHelperDirective()
        {
            // Act & Assert
            RunTagHelperTest("AddTagHelperDirective",
                             designTimeMode: true,
                             expectedDesignTimePragmas: new List<LineMapping>()
                             {
                                    BuildLineMapping(documentAbsoluteIndex: 14,
                                                     documentLineIndex: 0,
                                                     generatedAbsoluteIndex: 433,
                                                     generatedLineIndex: 14,
                                                     characterOffsetIndex: 14,
                                                     contentLength: 17)
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
                                     TypeName = "pTagHelper",
                                     AssemblyName = "SomeAssembly"
                                 }
                             });
        }

        [Fact]
        public void TagHelpers_WithinHelpersAndSections_GeneratesExpectedOutput()
        {
            // Arrange
            var propertyInfo = typeof(TestType).GetProperty("BoundProperty");
            var tagHelperDescriptors = new TagHelperDescriptor[]
            {
                new TagHelperDescriptor
                {
                    TagName = "MyTagHelper",
                    TypeName = "MyTagHelper",
                    AssemblyName = "SomeAssembly",
                    Attributes = new []
                    {
                        new TagHelperAttributeDescriptor("BoundProperty", propertyInfo)
                    }
                },
                new TagHelperDescriptor
                {
                    TagName = "NestedTagHelper",
                    TypeName = "NestedTagHelper",
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
                    TypeName = "PTagHelper",
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
                    TypeName = "InputTagHelper",
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
                    TypeName = "InputTagHelper2",
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
}
