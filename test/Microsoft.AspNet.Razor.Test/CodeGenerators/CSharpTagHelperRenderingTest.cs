// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
#if DNXCORE50
using System.Reflection;
#endif
using Microsoft.AspNet.Razor.CodeGenerators;
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
                                isIndexer: false,
                                designTimeDescriptor: null),
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
                                isIndexer: false,
                                designTimeDescriptor: null),
                            new TagHelperAttributeDescriptor(
                                "input-bound-string",
                                "BoundString",
                                typeof(string).FullName,
                                isIndexer: false,
                                designTimeDescriptor: null)
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
                                isIndexer: false,
                                designTimeDescriptor: null),
                            new TagHelperAttributeDescriptor(
                                name: "int-dictionary",
                                propertyName: "IntDictionaryProperty",
                                typeName: typeof(IDictionary<string, int>).FullName,
                                isIndexer: false,
                                designTimeDescriptor: null),
                            new TagHelperAttributeDescriptor(
                                name: "string-dictionary",
                                propertyName: "StringDictionaryProperty",
                                typeName: "Namespace.DictionaryWithoutParameterlessConstructor<string, string>",
                                isIndexer: false,
                                designTimeDescriptor: null),
                            new TagHelperAttributeDescriptor(
                                name: "string-prefix-grabber",
                                propertyName: "StringProperty",
                                typeName: typeof(string).FullName,
                                isIndexer: false,
                                designTimeDescriptor: null),
                            new TagHelperAttributeDescriptor(
                                name: "int-prefix-",
                                propertyName: "IntDictionaryProperty",
                                typeName: typeof(int).FullName,
                                isIndexer: true,
                                designTimeDescriptor: null),
                            new TagHelperAttributeDescriptor(
                                name: "string-prefix-",
                                propertyName: "StringDictionaryProperty",
                                typeName: typeof(string).FullName,
                                isIndexer: true,
                                designTimeDescriptor: null),
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
                                isIndexer: false,
                                designTimeDescriptor: null),
                            new TagHelperAttributeDescriptor(
                                name: "string-dictionary",
                                propertyName: "StringDictionaryProperty",
                                typeName: "Namespace.DictionaryWithoutParameterlessConstructor<string, string>",
                                isIndexer: false,
                                designTimeDescriptor: null),
                            new TagHelperAttributeDescriptor(
                                name: "int-prefix-",
                                propertyName: "IntDictionaryProperty",
                                typeName: typeof(int).FullName,
                                isIndexer: true,
                                designTimeDescriptor: null),
                            new TagHelperAttributeDescriptor(
                                name: "string-prefix-",
                                propertyName: "StringDictionaryProperty",
                                typeName: typeof(string).FullName,
                                isIndexer: true,
                                designTimeDescriptor: null),
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
                                documentCharacterOffsetIndex: 38,
                                generatedAbsoluteIndex: 1194,
                                generatedLineIndex: 38,
                                generatedCharacterOffsetIndex: 6,
                                contentLength: 23),
                            BuildLineMapping(
                                documentAbsoluteIndex: 287,
                                documentLineIndex: 6,
                                generatedAbsoluteIndex: 1677,
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
                                documentCharacterOffsetIndex: 46,
                                generatedAbsoluteIndex: 1873,
                                generatedLineIndex: 72,
                                generatedCharacterOffsetIndex: 6,
                                contentLength: 8),
                            BuildLineMapping(
                                documentAbsoluteIndex: 463,
                                documentLineIndex: 15,
                                generatedAbsoluteIndex: 2127,
                                generatedLineIndex: 79,
                                characterOffsetIndex: 63,
                                contentLength: 4),
                            BuildLineMapping(
                                documentAbsoluteIndex: 507,
                                documentLineIndex: 16,
                                documentCharacterOffsetIndex: 31,
                                generatedAbsoluteIndex: 2403,
                                generatedLineIndex: 87,
                                generatedCharacterOffsetIndex: 6,
                                contentLength: 30),
                            BuildLineMapping(
                                documentAbsoluteIndex: 574,
                                documentLineIndex: 17,
                                documentCharacterOffsetIndex: 30,
                                generatedAbsoluteIndex: 2752,
                                generatedLineIndex: 96,
                                generatedCharacterOffsetIndex: 0,
                                contentLength: 10),
                            BuildLineMapping(
                                documentAbsoluteIndex: 606,
                                documentLineIndex: 17,
                                documentCharacterOffsetIndex: 62,
                                generatedAbsoluteIndex: 2834,
                                generatedLineIndex: 102,
                                generatedCharacterOffsetIndex: 0,
                                contentLength: 1),
                            BuildLineMapping(
                                documentAbsoluteIndex: 607,
                                documentLineIndex: 17,
                                documentCharacterOffsetIndex: 63,
                                generatedAbsoluteIndex: 2907,
                                generatedLineIndex: 108,
                                generatedCharacterOffsetIndex: 0,
                                contentLength: 8),
                            BuildLineMapping(
                                documentAbsoluteIndex: 637,
                                documentLineIndex: 17,
                                documentCharacterOffsetIndex: 93,
                                generatedAbsoluteIndex: 2987,
                                generatedLineIndex: 114,
                                generatedCharacterOffsetIndex: 0,
                                contentLength: 1),
                            BuildLineMapping(
                                documentAbsoluteIndex: 638,
                                documentLineIndex: 17,
                                documentCharacterOffsetIndex: 94,
                                generatedAbsoluteIndex: 3060,
                                generatedLineIndex: 120,
                                generatedCharacterOffsetIndex: 0,
                                contentLength: 1),
                            BuildLineMapping(
                                documentAbsoluteIndex: 645,
                                documentLineIndex: 18,
                                generatedAbsoluteIndex: 3245,
                                generatedLineIndex: 128,
                                characterOffsetIndex: 0,
                                contentLength: 15),
                            BuildLineMapping(
                                documentAbsoluteIndex: 163,
                                documentLineIndex: 7,
                                documentCharacterOffsetIndex: 32,
                                generatedAbsoluteIndex: 3394,
                                generatedLineIndex: 135,
                                generatedCharacterOffsetIndex: 6,
                                contentLength: 12),
                            BuildLineMapping(
                                documentAbsoluteIndex: 771,
                                documentLineIndex: 21,
                                generatedAbsoluteIndex: 3477,
                                generatedLineIndex: 140,
                                characterOffsetIndex: 0,
                                contentLength: 12),
                            BuildLineMapping(
                                documentAbsoluteIndex: 785,
                                documentLineIndex: 21,
                                generatedAbsoluteIndex: 3575,
                                generatedLineIndex: 146,
                                characterOffsetIndex: 14,
                                contentLength: 21),
                            BuildLineMapping(
                                documentAbsoluteIndex: 839,
                                documentLineIndex: 22,
                                documentCharacterOffsetIndex: 30,
                                generatedAbsoluteIndex: 3832,
                                generatedLineIndex: 154,
                                generatedCharacterOffsetIndex: 28,
                                contentLength: 7),
                            BuildLineMapping(
                                documentAbsoluteIndex: 713,
                                documentLineIndex: 20,
                                documentCharacterOffsetIndex: 39,
                                generatedAbsoluteIndex: 4007,
                                generatedLineIndex: 160,
                                generatedCharacterOffsetIndex: 38,
                                contentLength: 23),
                            BuildLineMapping(
                                documentAbsoluteIndex: 736,
                                documentLineIndex: 20,
                                documentCharacterOffsetIndex: 62,
                                generatedAbsoluteIndex: 4030,
                                generatedLineIndex: 160,
                                generatedCharacterOffsetIndex: 61,
                                contentLength: 7),
                            BuildLineMapping(
                                documentAbsoluteIndex: 981,
                                documentLineIndex: 25,
                                documentCharacterOffsetIndex: 62,
                                generatedAbsoluteIndex: 4304,
                                generatedLineIndex: 167,
                                generatedCharacterOffsetIndex: 60,
                                contentLength: 30),
                            BuildLineMapping(
                                documentAbsoluteIndex: 883,
                                documentLineIndex: 24,
                                documentCharacterOffsetIndex: 16,
                                generatedAbsoluteIndex: 4483,
                                generatedLineIndex: 173,
                                generatedCharacterOffsetIndex: 19,
                                contentLength: 8),
                            BuildLineMapping(
                                documentAbsoluteIndex: 892,
                                documentLineIndex: 24,
                                documentCharacterOffsetIndex: 25,
                                generatedAbsoluteIndex: 4491,
                                generatedLineIndex: 173,
                                generatedCharacterOffsetIndex: 27,
                                contentLength: 23),
                            BuildLineMapping(
                                documentAbsoluteIndex: 1110,
                                documentLineIndex: 28,
                                generatedAbsoluteIndex: 4749,
                                generatedLineIndex: 180,
                                characterOffsetIndex: 28,
                                contentLength: 30),
                            BuildLineMapping(
                                documentAbsoluteIndex: 1048,
                                documentLineIndex: 27,
                                documentCharacterOffsetIndex: 16,
                                generatedAbsoluteIndex: 4928,
                                generatedLineIndex: 186,
                                generatedCharacterOffsetIndex: 19,
                                contentLength: 30),
                            BuildLineMapping(
                                documentAbsoluteIndex: 1240,
                                documentLineIndex: 31,
                                generatedAbsoluteIndex: 5193,
                                generatedLineIndex: 193,
                                characterOffsetIndex: 28,
                                contentLength: 3),
                            BuildLineMapping(
                                documentAbsoluteIndex: 1245,
                                documentLineIndex: 31,
                                documentCharacterOffsetIndex: 33,
                                generatedAbsoluteIndex: 5196,
                                generatedLineIndex: 193,
                                generatedCharacterOffsetIndex: 31,
                                contentLength: 27),
                            BuildLineMapping(
                                documentAbsoluteIndex: 1273,
                                documentLineIndex: 31,
                                documentCharacterOffsetIndex: 61,
                                generatedAbsoluteIndex: 5223,
                                generatedLineIndex: 193,
                                generatedCharacterOffsetIndex: 58,
                                contentLength: 10),
                            BuildLineMapping(
                                documentAbsoluteIndex: 1178,
                                documentLineIndex: 30,
                                documentCharacterOffsetIndex: 18,
                                generatedAbsoluteIndex: 5382,
                                generatedLineIndex: 199,
                                generatedCharacterOffsetIndex: 19,
                                contentLength: 29),
                            BuildLineMapping(
                                documentAbsoluteIndex: 1315,
                                documentLineIndex: 34,
                                generatedAbsoluteIndex: 5482,
                                generatedLineIndex: 204,
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
                                documentCharacterOffsetIndex: 51,
                                generatedAbsoluteIndex: 1199,
                                generatedLineIndex: 41,
                                generatedCharacterOffsetIndex: 6,
                                contentLength: 12),
                            BuildLineMapping(
                                documentAbsoluteIndex: 223,
                                documentLineIndex: 5,
                                generatedAbsoluteIndex: 1467,
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
                                documentCharacterOffsetIndex: 42,
                                generatedAbsoluteIndex: 4142,
                                generatedLineIndex: 107,
                                generatedCharacterOffsetIndex: 6,
                                contentLength: 8),
                            BuildLineMapping(
                                documentAbsoluteIndex: 826,
                                documentLineIndex: 21,
                                documentCharacterOffsetIndex: 29,
                                generatedAbsoluteIndex: 4621,
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
                    { "DuplicateAttributeTagHelpers", null, DefaultPAndInputTagHelperDescriptors },
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
                    requiredAttributes: Enumerable.Empty<string>(),
                    designTimeDescriptor: null),
                new TagHelperDescriptor(
                    prefix,
                    tagName: "input",
                    typeName: "InputTagHelper",
                    assemblyName: "SomeAssembly",
                    attributes: new TagHelperAttributeDescriptor[]
                    {
                        new TagHelperAttributeDescriptor("type", inputTypePropertyInfo)
                    },
                    requiredAttributes: Enumerable.Empty<string>(),
                    designTimeDescriptor: null),
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
                    requiredAttributes: Enumerable.Empty<string>(),
                    designTimeDescriptor: null)
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
