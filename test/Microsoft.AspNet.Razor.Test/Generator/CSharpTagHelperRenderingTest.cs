// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
#if DNXCORE50
using System.Reflection;
#endif
using Microsoft.AspNet.Razor.Generator.Compiler;
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

        private static IEnumerable<TagHelperDescriptor> MinimizedTagHelpers_Descriptors
        {
            get
            {
                return new[]
                {
                    new TagHelperDescriptor(
                        tagName: "*",
                        typeName: "CatchAllTagHelper",
                        assemblyName: "SomeAssembly",
                        attributes: new[]
                        {
                            new TagHelperAttributeDescriptor(
                                "catchall-bound-string",
                                "BoundRequiredString",
                                typeof(string).FullName,
                                isIndexer: false),
                        },
                        requiredAttributes: new[] { "catchall-unbound-required" }),
                    new TagHelperDescriptor(
                        tagName: "input",
                        typeName: "InputTagHelper",
                        assemblyName: "SomeAssembly",
                        attributes: new[]
                        {
                            new TagHelperAttributeDescriptor(
                                "input-bound-required-string",
                                "BoundRequiredString",
                                typeof(string).FullName,
                                isIndexer: false),
                            new TagHelperAttributeDescriptor(
                                "input-bound-string",
                                "BoundString",
                                typeof(string).FullName,
                                isIndexer: false)
                        },
                        requiredAttributes: new[] { "input-bound-required-string", "input-unbound-required" }),
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
                    new TagHelperDescriptor(
                        tagName: "*",
                        typeName: "CatchAllTagHelper",
                        assemblyName: "SomeAssembly",
                        attributes: new TagHelperAttributeDescriptor[]
                        {
                            new TagHelperAttributeDescriptor("type", inputTypePropertyInfo),
                            new TagHelperAttributeDescriptor("checked", inputCheckedPropertyInfo)
                        },
                        requiredAttributes: new[] { "type" }),
                    new TagHelperDescriptor(
                        tagName: "*",
                        typeName: "CatchAllTagHelper",
                        assemblyName: "SomeAssembly",
                        attributes: new TagHelperAttributeDescriptor[]
                        {
                            new TagHelperAttributeDescriptor("type", inputTypePropertyInfo),
                            new TagHelperAttributeDescriptor("checked", inputCheckedPropertyInfo)
                        },
                        requiredAttributes: new[] { "checked" }),
                    new TagHelperDescriptor(
                        tagName: "input",
                        typeName: "InputTagHelper",
                        assemblyName: "SomeAssembly",
                        attributes: new TagHelperAttributeDescriptor[]
                        {
                            new TagHelperAttributeDescriptor("type", inputTypePropertyInfo),
                            new TagHelperAttributeDescriptor("checked", inputCheckedPropertyInfo)
                        },
                        requiredAttributes: new[] { "type" }),
                    new TagHelperDescriptor(
                        tagName: "input",
                        typeName: "InputTagHelper",
                        assemblyName: "SomeAssembly",
                        attributes: new TagHelperAttributeDescriptor[]
                        {
                            new TagHelperAttributeDescriptor("type", inputTypePropertyInfo),
                            new TagHelperAttributeDescriptor("checked", inputCheckedPropertyInfo)
                        },
                        requiredAttributes: new[] { "checked" })
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
                    new TagHelperDescriptor(
                        tagName: "p",
                        typeName: "PTagHelper",
                        assemblyName: "SomeAssembly",
                        attributes: new TagHelperAttributeDescriptor[0],
                        requiredAttributes: new[] { "class" }),
                    new TagHelperDescriptor(
                        tagName: "input",
                        typeName: "InputTagHelper",
                        assemblyName: "SomeAssembly",
                        attributes: new TagHelperAttributeDescriptor[]
                        {
                            new TagHelperAttributeDescriptor("type", inputTypePropertyInfo)
                        },
                        requiredAttributes: new[] { "type" }),
                    new TagHelperDescriptor(
                        tagName: "input",
                        typeName: "InputTagHelper2",
                        assemblyName: "SomeAssembly",
                        attributes: new TagHelperAttributeDescriptor[]
                        {
                            new TagHelperAttributeDescriptor("type", inputTypePropertyInfo),
                            new TagHelperAttributeDescriptor("checked", inputCheckedPropertyInfo)
                        },
                        requiredAttributes: new[] { "type", "checked" }),
                    new TagHelperDescriptor(
                        tagName: "*",
                        typeName: "CatchAllTagHelper",
                        assemblyName: "SomeAssembly",
                        attributes: new TagHelperAttributeDescriptor[0],
                        requiredAttributes: new[] { "catchAll" })
                };
            }
        }

        private static IEnumerable<TagHelperDescriptor> PrefixedAttributeTagHelperDescriptors
        {
            get
            {
                return new[]
                {
                    new TagHelperDescriptor(
                        tagName: "input",
                        typeName: "InputTagHelper1",
                        assemblyName: "SomeAssembly",
                        attributes: new[]
                        {
                            new TagHelperAttributeDescriptor(
                                name: "int-prefix-grabber",
                                propertyName: "IntProperty",
                                typeName: typeof(int).FullName,
                                isIndexer: false),
                            new TagHelperAttributeDescriptor(
                                name: "int-dictionary",
                                propertyName: "IntDictionaryProperty",
                                typeName: typeof(IDictionary<string, int>).FullName,
                                isIndexer: false),
                            new TagHelperAttributeDescriptor(
                                name: "string-dictionary",
                                propertyName: "StringDictionaryProperty",
                                typeName: "Namespace.DictionaryWithoutParameterlessConstructor<string, string>",
                                isIndexer: false),
                            new TagHelperAttributeDescriptor(
                                name: "string-prefix-grabber",
                                propertyName: "StringProperty",
                                typeName: typeof(string).FullName,
                                isIndexer: false),
                            new TagHelperAttributeDescriptor(
                                name: "int-prefix-",
                                propertyName: "IntDictionaryProperty",
                                typeName: typeof(int).FullName,
                                isIndexer: true),
                            new TagHelperAttributeDescriptor(
                                name: "string-prefix-",
                                propertyName: "StringDictionaryProperty",
                                typeName: typeof(string).FullName,
                                isIndexer: true),
                        }),
                    new TagHelperDescriptor(
                        tagName: "input",
                        typeName: "InputTagHelper2",
                        assemblyName: "SomeAssembly",
                        attributes: new[]
                        {
                            new TagHelperAttributeDescriptor(
                                name: "int-dictionary",
                                propertyName: "IntDictionaryProperty",
                                typeName: typeof(IDictionary<string, int>).FullName,
                                isIndexer: false),
                            new TagHelperAttributeDescriptor(
                                name: "string-dictionary",
                                propertyName: "StringDictionaryProperty",
                                typeName: "Namespace.DictionaryWithoutParameterlessConstructor<string, string>",
                                isIndexer: false),
                            new TagHelperAttributeDescriptor(
                                name: "int-prefix-",
                                propertyName: "IntDictionaryProperty",
                                typeName: typeof(int).FullName,
                                isIndexer: true),
                            new TagHelperAttributeDescriptor(
                                name: "string-prefix-",
                                propertyName: "StringDictionaryProperty",
                                typeName: typeof(string).FullName,
                                isIndexer: true),
                        }),
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
                return new TheoryData<string, string, IEnumerable<TagHelperDescriptor>, List<LineMapping>>
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
                            BuildLineMapping(documentAbsoluteIndex: 14,
                                             documentLineIndex: 0,
                                             generatedAbsoluteIndex: 475,
                                             generatedLineIndex: 15,
                                             characterOffsetIndex: 14,
                                             contentLength: 17),
                            BuildLineMapping(documentAbsoluteIndex: 202,
                                             documentLineIndex: 5,
                                             documentCharacterOffsetIndex: 38,
                                             generatedAbsoluteIndex: 1300,
                                             generatedLineIndex: 40,
                                             generatedCharacterOffsetIndex: 6,
                                             contentLength: 23),
                            BuildLineMapping(documentAbsoluteIndex: 287,
                                             documentLineIndex: 6,
                                             generatedAbsoluteIndex: 1677,
                                             generatedLineIndex: 49,
                                             characterOffsetIndex: 40,
                                             contentLength: 4)
                        }
                    },
                    {
                        "BasicTagHelpers.Prefixed",
                        "BasicTagHelpers.Prefixed.DesignTime",
                        PrefixedPAndInputTagHelperDescriptors,
                        new List<LineMapping>
                        {
                            BuildLineMapping(documentAbsoluteIndex: 17,
                                             documentLineIndex: 0,
                                             generatedAbsoluteIndex: 496,
                                             generatedLineIndex: 15,
                                             characterOffsetIndex: 17,
                                             contentLength: 5),
                            BuildLineMapping(documentAbsoluteIndex: 38,
                                             documentLineIndex: 1,
                                             generatedAbsoluteIndex: 655,
                                             generatedLineIndex: 22,
                                             characterOffsetIndex: 14,
                                             contentLength: 17),
                            BuildLineMapping(documentAbsoluteIndex: 228,
                                             documentLineIndex: 7,
                                             generatedAbsoluteIndex: 1480,
                                             generatedLineIndex: 46,
                                             characterOffsetIndex: 43,
                                             contentLength: 4)
                        }
                    },
                    {
                        "ComplexTagHelpers",
                        "ComplexTagHelpers.DesignTime",
                        DefaultPAndInputTagHelperDescriptors,
                        new List<LineMapping>
                        {
                            BuildLineMapping(14, 0, 479, 15, 14, 17),
                            BuildLineMapping(36, 2, 1, 1001, 35, 0, 48),
                            BuildLineMapping(211, 9, 1119, 44, 0, 12),
                            BuildLineMapping(224, 9, 13, 1215, 50, 12, 27),
                            BuildLineMapping(352, 12, 1613, 62, 0, 48),
                            BuildLineMapping(446, 15, 46, 1804, 71, 6, 8),
                            BuildLineMapping(463, 15, 2127, 79, 63, 4),
                            BuildLineMapping(507, 16, 31, 2334, 86, 6, 30),
                            BuildLineMapping(574, 17, 30, 2683, 95, 0, 10),
                            BuildLineMapping(606, 17, 62, 2765, 101, 0, 1),
                            BuildLineMapping(607, 17, 63, 2838, 107, 0, 8),
                            BuildLineMapping(637, 17, 93, 2918, 113, 0, 1),
                            BuildLineMapping(638, 17, 94, 2991, 119, 0, 1),
                            BuildLineMapping(645, 18, 0, 3245, 128, 0, 15),
                            BuildLineMapping(163, 7, 32, 3394, 135, 6, 12),
                            BuildLineMapping(725, 21, 0, 3477, 140, 0, 12),
                            BuildLineMapping(739, 21, 14, 3575, 146, 14, 21),
                            BuildLineMapping(793, 22, 30, 3832, 154, 28, 7),
                            BuildLineMapping(691, 20, 17, 3988, 160, 19, 23),
                            BuildLineMapping(714, 20, 40, 4011, 160, 42, 7),
                            BuildLineMapping(903, 25, 30, 4253, 167, 28, 30),
                            BuildLineMapping(837, 24, 16, 4432, 173, 19, 8),
                            BuildLineMapping(846, 24, 25, 4440, 173, 27, 23),
                            BuildLineMapping(1032, 28, 28, 4698, 180, 28, 30),
                            BuildLineMapping(970, 27, 16, 4877, 186, 19, 30),
                            BuildLineMapping(1162, 31, 28, 5142, 193, 28, 3),
                            BuildLineMapping(1167, 31, 33, 5145, 193, 31, 27),
                            BuildLineMapping(1195, 31, 61, 5172, 193, 58, 10),
                            BuildLineMapping(1100, 30, 18, 5331, 199, 19, 29),
                            BuildLineMapping(1237, 34, 0, 5431, 204, 0, 1),
                        }
                    },
                    {
                        "EmptyAttributeTagHelpers",
                        "EmptyAttributeTagHelpers.DesignTime",
                        DefaultPAndInputTagHelperDescriptors,
                        new List<LineMapping>
                        {
                            BuildLineMapping(documentAbsoluteIndex: 14,
                                             documentLineIndex: 0,
                                             generatedAbsoluteIndex: 493,
                                             generatedLineIndex: 15,
                                             characterOffsetIndex: 14,
                                             contentLength: 11),
                            BuildLineMapping(documentAbsoluteIndex: 62,
                                             documentLineIndex: 3,
                                             documentCharacterOffsetIndex: 26,
                                             generatedAbsoluteIndex: 1289,
                                             generatedLineIndex: 39,
                                             generatedCharacterOffsetIndex: 28,
                                             contentLength: 0),
                            BuildLineMapping(documentAbsoluteIndex: 122,
                                             documentLineIndex: 5,
                                             generatedAbsoluteIndex: 1634,
                                             generatedLineIndex: 48,
                                             characterOffsetIndex: 30,
                                             contentLength: 0),
                            BuildLineMapping(documentAbsoluteIndex: 88,
                                             documentLineIndex: 4,
                                             documentCharacterOffsetIndex: 12,
                                             generatedAbsoluteIndex: 1789,
                                             generatedLineIndex: 54,
                                             generatedCharacterOffsetIndex: 19,
                                             contentLength: 0)
                        }
                    },
                    {
                        "EscapedTagHelpers",
                        "EscapedTagHelpers.DesignTime",
                        DefaultPAndInputTagHelperDescriptors,
                        new List<LineMapping>
                        {
                            BuildLineMapping(documentAbsoluteIndex: 14,
                                             documentLineIndex: 0,
                                             generatedAbsoluteIndex: 479,
                                             generatedLineIndex: 15,
                                             characterOffsetIndex: 14,
                                             contentLength: 11),
                            BuildLineMapping(documentAbsoluteIndex: 102,
                                             documentLineIndex: 3,
                                             generatedAbsoluteIndex: 975,
                                             generatedLineIndex: 34,
                                             characterOffsetIndex: 29,
                                             contentLength: 12),
                            BuildLineMapping(documentAbsoluteIndex: 200,
                                             documentLineIndex: 5,
                                             documentCharacterOffsetIndex: 51,
                                             generatedAbsoluteIndex: 1130,
                                             generatedLineIndex: 40,
                                             generatedCharacterOffsetIndex: 6,
                                             contentLength: 12),
                            BuildLineMapping(documentAbsoluteIndex: 223,
                                             documentLineIndex: 5,
                                             generatedAbsoluteIndex: 1467,
                                             generatedLineIndex: 48,
                                             characterOffsetIndex: 74,
                                             contentLength: 4)
                        }
                    },
                    {
                        "AttributeTargetingTagHelpers",
                        "AttributeTargetingTagHelpers.DesignTime",
                        AttributeTargetingTagHelperDescriptors,
                        new List<LineMapping>
                        {
                            BuildLineMapping(documentAbsoluteIndex: 14,
                                             documentLineIndex: 0,
                                             generatedAbsoluteIndex: 501,
                                             generatedLineIndex: 15,
                                             characterOffsetIndex: 14,
                                             contentLength: 14),
                            BuildLineMapping(documentAbsoluteIndex: 186,
                                             documentLineIndex: 5,
                                             generatedAbsoluteIndex: 1460,
                                             generatedLineIndex: 41,
                                             characterOffsetIndex: 36,
                                             contentLength: 4),
                            BuildLineMapping(documentAbsoluteIndex: 232,
                                             documentLineIndex: 6,
                                             documentCharacterOffsetIndex: 36,
                                             generatedAbsoluteIndex: 1827,
                                             generatedLineIndex: 50,
                                             generatedCharacterOffsetIndex: 36,
                                             contentLength: 4)
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
                                generatedAbsoluteIndex: 1430,
                                generatedLineIndex: 50,
                                characterOffsetIndex: 43,
                                contentLength: 13),
                            BuildLineMapping(
                                documentAbsoluteIndex: 404,
                                documentLineIndex: 15,
                                generatedAbsoluteIndex: 1601,
                                generatedLineIndex: 55,
                                characterOffsetIndex: 77,
                                contentLength: 16),
                            BuildLineMapping(
                                documentAbsoluteIndex: 468,
                                documentLineIndex: 16,
                                generatedAbsoluteIndex: 2077,
                                generatedLineIndex: 64,
                                characterOffsetIndex: 43,
                                contentLength: 13),
                            BuildLineMapping(
                                documentAbsoluteIndex: 502,
                                documentLineIndex: 16,
                                generatedAbsoluteIndex: 2248,
                                generatedLineIndex: 69,
                                characterOffsetIndex: 77,
                                contentLength: 2),
                            BuildLineMapping(
                                documentAbsoluteIndex: 526,
                                documentLineIndex: 16,
                                generatedAbsoluteIndex: 2432,
                                generatedLineIndex: 74,
                                characterOffsetIndex: 101,
                                contentLength: 2),
                            BuildLineMapping(
                                documentAbsoluteIndex: 590,
                                documentLineIndex: 18,
                                documentCharacterOffsetIndex: 31,
                                generatedAbsoluteIndex: 2994,
                                generatedLineIndex: 84,
                                generatedCharacterOffsetIndex: 32,
                                contentLength: 2),
                            BuildLineMapping(
                                documentAbsoluteIndex: 611,
                                documentLineIndex: 18,
                                generatedAbsoluteIndex: 3129,
                                generatedLineIndex: 89,
                                characterOffsetIndex: 52,
                                contentLength: 2),
                            BuildLineMapping(
                                documentAbsoluteIndex: 634,
                                documentLineIndex: 18,
                                generatedAbsoluteIndex: 3287,
                                generatedLineIndex: 94,
                                characterOffsetIndex: 75,
                                contentLength: 2),
                            BuildLineMapping(
                                documentAbsoluteIndex: 783,
                                documentLineIndex: 20,
                                documentCharacterOffsetIndex: 42,
                                generatedAbsoluteIndex: 3521,
                                generatedLineIndex: 101,
                                generatedCharacterOffsetIndex: 6,
                                contentLength: 8),
                            BuildLineMapping(
                                documentAbsoluteIndex: 826,
                                documentLineIndex: 21,
                                documentCharacterOffsetIndex: 29,
                                generatedAbsoluteIndex: 4552,
                                generatedLineIndex: 115,
                                generatedCharacterOffsetIndex: 51,
                                contentLength: 2),
                        }
                    },
                };
            }
        }

        [Theory]
        [MemberData(nameof(DesignTimeTagHelperTestData))]
        public void TagHelpers_GenerateExpectedDesignTimeOutput(string testName,
                                                                string baseLineName,
                                                                IEnumerable<TagHelperDescriptor> tagHelperDescriptors,
                                                                List<LineMapping> expectedDesignTimePragmas)
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
                    { "SingleTagHelper", null, DefaultPAndInputTagHelperDescriptors },
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
        public void CSharpCodeGenerator_CorrectlyGeneratesMappings_ForRemoveTagHelperDirective()
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
        public void CSharpCodeGenerator_CorrectlyGeneratesMappings_ForAddTagHelperDirective()
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
                                new TagHelperDescriptor("p", "pTagHelper", "SomeAssembly")
                             });
        }

        [Fact]
        public void TagHelpers_WithinHelpersAndSections_GeneratesExpectedOutput()
        {
            // Arrange
            var propertyInfo = typeof(TestType).GetProperty("BoundProperty");
            var tagHelperDescriptors = new TagHelperDescriptor[]
            {
                new TagHelperDescriptor("MyTagHelper",
                                        "MyTagHelper",
                                        "SomeAssembly",
                                        new []
                                        {
                                            new TagHelperAttributeDescriptor("BoundProperty", propertyInfo)
                                        }),
                new TagHelperDescriptor("NestedTagHelper", "NestedTagHelper", "SomeAssembly")
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
                new TagHelperDescriptor(
                    prefix,
                    tagName: "p",
                    typeName: "PTagHelper",
                    assemblyName: "SomeAssembly",
                    attributes: new []
                    {
                        new TagHelperAttributeDescriptor("age", pAgePropertyInfo)
                    },
                    requiredAttributes: Enumerable.Empty<string>()),
                new TagHelperDescriptor(
                    prefix,
                    tagName: "input",
                    typeName: "InputTagHelper",
                    assemblyName: "SomeAssembly",
                    attributes: new TagHelperAttributeDescriptor[]
                    {
                        new TagHelperAttributeDescriptor("type", inputTypePropertyInfo)
                    },
                    requiredAttributes: Enumerable.Empty<string>()),
                new TagHelperDescriptor(
                    prefix,
                    tagName: "input",
                    typeName: "InputTagHelper2",
                    assemblyName: "SomeAssembly",
                    attributes: new TagHelperAttributeDescriptor[]
                    {
                        new TagHelperAttributeDescriptor("type", inputTypePropertyInfo),
                        new TagHelperAttributeDescriptor("checked", checkedPropertyInfo)
                    },
                    requiredAttributes: Enumerable.Empty<string>())
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
