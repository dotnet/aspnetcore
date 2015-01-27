// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
#if ASPNETCORE50
using System.Reflection;
#endif
using Microsoft.AspNet.Razor.Generator.Compiler;
using Microsoft.AspNet.Razor.TagHelpers;
using Xunit;

namespace Microsoft.AspNet.Razor.Test.Generator
{
    public class CSharpTagHelperRenderingTest : TagHelperTestBase
    {
        private static IEnumerable<TagHelperDescriptor> PAndInputTagHelperDescriptors
        {
            get
            {
                var pAgePropertyInfo = typeof(TestType).GetProperty("Age");
                var inputTypePropertyInfo = typeof(TestType).GetProperty("Type");
                var checkedPropertyInfo = typeof(TestType).GetProperty("Checked");
                return new[]
                {
                    new TagHelperDescriptor("p",
                                            "PTagHelper",
                                            "SomeAssembly",
                                            new [] {
                                                new TagHelperAttributeDescriptor("age", pAgePropertyInfo)
                                            }),
                    new TagHelperDescriptor("input",
                                            "InputTagHelper",
                                            "SomeAssembly",
                                            new TagHelperAttributeDescriptor[] {
                                                new TagHelperAttributeDescriptor("type", inputTypePropertyInfo)
                                            }),
                    new TagHelperDescriptor("input",
                                            "InputTagHelper2",
                                            "SomeAssembly",
                                            new TagHelperAttributeDescriptor[] {
                                                new TagHelperAttributeDescriptor("type", inputTypePropertyInfo),
                                                new TagHelperAttributeDescriptor("checked", checkedPropertyInfo)
                                            })
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
                        PAndInputTagHelperDescriptors,
                        PAndInputTagHelperDescriptors,
                        false
                    },
                    {
                        "SingleTagHelper",
                        "SingleTagHelper.DesignTime",
                        PAndInputTagHelperDescriptors,
                        PAndInputTagHelperDescriptors,
                        true
                    },
                    {
                        "BasicTagHelpers",
                        "BasicTagHelpers",
                        PAndInputTagHelperDescriptors,
                        PAndInputTagHelperDescriptors,
                        false
                    },
                    {
                        "BasicTagHelpers",
                        "BasicTagHelpers.DesignTime",
                        PAndInputTagHelperDescriptors,
                        PAndInputTagHelperDescriptors,
                        true
                    },
                    {
                        "BasicTagHelpers.RemoveTagHelper",
                        "BasicTagHelpers.RemoveTagHelper",
                        PAndInputTagHelperDescriptors,
                        Enumerable.Empty<TagHelperDescriptor>(),
                        false
                    },
                    {
                        "ComplexTagHelpers",
                        "ComplexTagHelpers",
                        PAndInputTagHelperDescriptors,
                        PAndInputTagHelperDescriptors,
                        false
                    },
                    {
                        "ComplexTagHelpers",
                        "ComplexTagHelpers.DesignTime",
                        PAndInputTagHelperDescriptors,
                        PAndInputTagHelperDescriptors,
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
                        PAndInputTagHelperDescriptors,
                        new List<LineMapping>
                        {
                            BuildLineMapping(documentAbsoluteIndex: 14,
                                             documentLineIndex: 0,
                                             generatedAbsoluteIndex: 475,
                                             generatedLineIndex: 15,
                                             characterOffsetIndex: 14,
                                             contentLength: 11),
                            BuildLineMapping(documentAbsoluteIndex: 57,
                                             documentLineIndex: 2,
                                             generatedAbsoluteIndex: 958,
                                             generatedLineIndex: 34,
                                             characterOffsetIndex: 28,
                                             contentLength: 4)
                        }
                    },
                    {
                        "BasicTagHelpers",
                        "BasicTagHelpers.DesignTime",
                        PAndInputTagHelperDescriptors,
                        new List<LineMapping>
                        {
                            BuildLineMapping(documentAbsoluteIndex: 14,
                                             documentLineIndex: 0,
                                             generatedAbsoluteIndex: 475,
                                             generatedLineIndex: 15,
                                             characterOffsetIndex: 14,
                                             contentLength: 11),
                            BuildLineMapping(documentAbsoluteIndex: 189,
                                             documentLineIndex: 6,
                                             generatedAbsoluteIndex: 1574,
                                             generatedLineIndex: 44,
                                             characterOffsetIndex: 40,
                                             contentLength: 4)
                        }
                    },
                    {
                        "ComplexTagHelpers",
                        "ComplexTagHelpers.DesignTime",
                        PAndInputTagHelperDescriptors,
                        new List<LineMapping>
                        {
                            BuildLineMapping(14, 0, 479, 15, 14, 11),
                            BuildLineMapping(30, 2, 1, 995, 35, 0, 48),
                            BuildLineMapping(205, 9, 1113, 44, 0, 12),
                            BuildLineMapping(218, 9, 13, 1209, 50, 12, 27),
                            BuildLineMapping(346, 12, 1607, 62, 0, 48),
                            BuildLineMapping(440, 15, 46, 1798, 71, 6, 8),
                            BuildLineMapping(457, 15, 2121, 79, 63, 4),
                            BuildLineMapping(501, 16, 31, 2328, 86, 6, 30),
                            BuildLineMapping(568, 17, 30, 2677, 95, 0, 10),
                            BuildLineMapping(601, 17, 63, 2759, 101, 0, 8),
                            BuildLineMapping(632, 17, 94, 2839, 107, 0, 1),
                            BuildLineMapping(639, 18, 3093, 116, 0, 15),
                            BuildLineMapping(157, 7, 32, 3242, 123, 6, 12),
                            BuildLineMapping(719, 21, 3325, 128, 0, 12),
                            BuildLineMapping(733, 21, 3423, 134, 14, 21),
                            BuildLineMapping(787, 22, 30, 3680, 142, 28, 7),
                            BuildLineMapping(685, 20, 17, 3836, 148, 19, 23),
                            BuildLineMapping(708, 20, 40, 3859, 148, 42, 7),
                            BuildLineMapping(897, 25, 30, 4101, 155, 28, 30),
                            BuildLineMapping(831, 24, 16, 4280, 161, 19, 8),
                            BuildLineMapping(840, 24, 25, 4288, 161, 27, 23),
                            BuildLineMapping(1026, 28, 4546, 168, 28, 30),
                            BuildLineMapping(964, 27, 16, 4725, 174, 19, 30),
                            BuildLineMapping(1156, 31, 4990, 181, 28, 3),
                            BuildLineMapping(1161, 31, 33, 4993, 181, 31, 27),
                            BuildLineMapping(1189, 31, 61, 5020, 181, 58, 10),
                            BuildLineMapping(1094, 30, 18, 5179, 187, 19, 29),
                            BuildLineMapping(1231, 34, 5279, 192, 0, 1),
                        }
                    },
                    {
                        "EmptyAttributeTagHelpers",
                        "EmptyAttributeTagHelpers.DesignTime",
                        PAndInputTagHelperDescriptors,
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
                return new TheoryData<string, IEnumerable<TagHelperDescriptor>>
                {
                    { "SingleTagHelper", PAndInputTagHelperDescriptors },
                    { "BasicTagHelpers", PAndInputTagHelperDescriptors },
                    { "BasicTagHelpers.RemoveTagHelper", PAndInputTagHelperDescriptors },
                    { "ComplexTagHelpers", PAndInputTagHelperDescriptors },
                    { "EmptyAttributeTagHelpers", PAndInputTagHelperDescriptors },
                };
            }
        }

        [Theory]
        [MemberData(nameof(RuntimeTimeTagHelperTestData))]
        public void TagHelpers_GenerateExpectedRuntimeOutput(string testName,
                                                             IEnumerable<TagHelperDescriptor> tagHelperDescriptors)
        {
            // Act & Assert
            RunTagHelperTest(testName, tagHelperDescriptors: tagHelperDescriptors);
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
                                                     contentLength: 11)
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
                                                     contentLength: 11)
                             });
        }

        [Fact]
        public void TagHelpers_Directive_GenerateDesignTimeMappings()
        {
            // Act & Assert
            RunTagHelperTest("AddTagHelperDirective",
                             designTimeMode: true,
                             tagHelperDescriptors: new[] {
                                new TagHelperDescriptor("p", "pTagHelper", "SomeAssembly")
                             });
        }

        [Theory]
        [InlineData("TagHelpersInSection")]
        [InlineData("TagHelpersInHelper")]
        public void TagHelpers_WithinHelpersAndSections_GeneratesExpectedOutput(string testType)
        {
            // Arrange
            var propertyInfo = typeof(TestType).GetProperty("BoundProperty");
            var tagHelperDescriptors = new TagHelperDescriptor[]
            {
                new TagHelperDescriptor("MyTagHelper",
                                        "MyTagHelper",
                                        "SomeAssembly",
                                        new [] {
                                            new TagHelperAttributeDescriptor("BoundProperty",
                                                                             propertyInfo)
                                        }),
                new TagHelperDescriptor("NestedTagHelper", "NestedTagHelper", "SomeAssembly")
            };

            // Act & Assert
            RunTagHelperTest(testType, tagHelperDescriptors: tagHelperDescriptors);
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