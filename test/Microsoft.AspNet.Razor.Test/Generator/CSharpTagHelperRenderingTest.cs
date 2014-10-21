// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Reflection;
using Microsoft.AspNet.Razor.Generator.Compiler;
using Microsoft.AspNet.Razor.TagHelpers;
using Xunit;

namespace Microsoft.AspNet.Razor.Test.Generator
{
    public class CSharpTagHelperRenderingTest : TagHelperTestBase
    {
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
                                new TagHelperDescriptor("p", "pTagHelper", ContentBehavior.None)
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
                                        ContentBehavior.None,
                                        new [] {
                                            new TagHelperAttributeDescriptor("BoundProperty",
                                                                             propertyInfo)
                                        }),
                new TagHelperDescriptor("NestedTagHelper", "NestedTagHelper", ContentBehavior.Modify)
            };

            // Act & Assert
            RunTagHelperTest(testType, tagHelperDescriptors: tagHelperDescriptors);
        }

        [Theory]
        [InlineData("SingleTagHelper")]
        [InlineData("BasicTagHelpers")]
        [InlineData("BasicTagHelpers.RemoveTagHelper")]
        [InlineData("ComplexTagHelpers")]
        public void TagHelpers_GenerateExpectedOutput(string testType)
        {
            // Arrange
            var pFooPropertyInfo = typeof(TestType).GetProperty("Foo");
            var inputTypePropertyInfo = typeof(TestType).GetProperty("Type");
            var checkedPropertyInfo = typeof(TestType).GetProperty("Checked");
            var tagHelperDescriptors = new TagHelperDescriptor[]
            {
                new TagHelperDescriptor("p",
                                        "PTagHelper",
                                        ContentBehavior.None,
                                        new [] {
                                            new TagHelperAttributeDescriptor("foo", pFooPropertyInfo)
                                        }),
                new TagHelperDescriptor("input",
                                        "InputTagHelper",
                                        ContentBehavior.None,
                                        new TagHelperAttributeDescriptor[] {
                                            new TagHelperAttributeDescriptor("type", inputTypePropertyInfo)
                                        }),
                new TagHelperDescriptor("input",
                                        "InputTagHelper2",
                                        ContentBehavior.None,
                                        new TagHelperAttributeDescriptor[] {
                                            new TagHelperAttributeDescriptor("type", inputTypePropertyInfo),
                                            new TagHelperAttributeDescriptor("checked", checkedPropertyInfo)
                                        })
            };

            // Act & Assert
            RunTagHelperTest(testType, tagHelperDescriptors: tagHelperDescriptors);
        }

        [Fact]
        public void TagHelpers_WithContentBehaviors_GenerateExpectedOutput()
        {
            // Arrange
            var tagHelperDescriptors = new TagHelperDescriptor[]
            {
                    new TagHelperDescriptor("modify", "ModifyTagHelper", ContentBehavior.Modify),
                    new TagHelperDescriptor("none", "NoneTagHelper", ContentBehavior.None),
                    new TagHelperDescriptor("append", "AppendTagHelper", ContentBehavior.Append),
                    new TagHelperDescriptor("prepend", "PrependTagHelper", ContentBehavior.Prepend),
                    new TagHelperDescriptor("replace", "ReplaceTagHelper", ContentBehavior.Replace),
            };

            // Act & Assert
            RunTagHelperTest("ContentBehaviorTagHelpers", tagHelperDescriptors: tagHelperDescriptors);
        }

        private class TestType
        {
            public int Foo { get; set; }

            public string Type { get; set; }

            public bool Checked { get; set; }

            public string BoundProperty { get; set; }
        }
    }
}