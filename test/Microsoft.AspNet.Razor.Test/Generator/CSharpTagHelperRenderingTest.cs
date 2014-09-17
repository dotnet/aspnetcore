// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Reflection;
using Microsoft.AspNet.Razor.Generator.Compiler;
using Microsoft.AspNet.Razor.TagHelpers;
using Moq;
using Xunit;

namespace Microsoft.AspNet.Razor.Test.Generator
{
    public class CSharpTagHelperRenderingTest : TagHelperTestBase
    {
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
            var propertyInfoMock = new Mock<PropertyInfo>();
            propertyInfoMock.Setup(propertyInfo => propertyInfo.PropertyType).Returns(typeof(string));
            propertyInfoMock.Setup(propertyInfo => propertyInfo.Name).Returns("BoundProperty");
            var tagHelperDescriptors = new TagHelperDescriptor[]
            {
                new TagHelperDescriptor("MyTagHelper",
                                        "MyTagHelper",
                                        ContentBehavior.None,
                                        new [] {
                                            new TagHelperAttributeDescriptor("BoundProperty",
                                                                             propertyInfoMock.Object)
                                        }),
                new TagHelperDescriptor("NestedTagHelper", "NestedTagHelper", ContentBehavior.Modify)
            };

            // Act & Assert
            RunTagHelperTest(testType, tagHelperDescriptors: tagHelperDescriptors);
        }

        [Theory]
        [InlineData("SingleTagHelper")]
        [InlineData("BasicTagHelpers")]
        [InlineData("ComplexTagHelpers")]
        public void TagHelpers_GenerateExpectedOutput(string testType)
        {
            // Arrange
            var pFooPropertyInfo = new Mock<PropertyInfo>();
            pFooPropertyInfo.Setup(propertyInfo => propertyInfo.PropertyType).Returns(typeof(int));
            pFooPropertyInfo.Setup(propertyInfo => propertyInfo.Name).Returns("Foo");
            var inputTypePropertyInfo = new Mock<PropertyInfo>();
            inputTypePropertyInfo.Setup(propertyInfo => propertyInfo.PropertyType).Returns(typeof(string));
            inputTypePropertyInfo.Setup(propertyInfo => propertyInfo.Name).Returns("Type");
            var checkedPropertyInfo = new Mock<PropertyInfo>();
            checkedPropertyInfo.Setup(propertyInfo => propertyInfo.PropertyType).Returns(typeof(bool));
            checkedPropertyInfo.Setup(propertyInfo => propertyInfo.Name).Returns("Checked");
            var tagHelperDescriptors = new TagHelperDescriptor[]
            {
                new TagHelperDescriptor("p",
                                        "PTagHelper",
                                        ContentBehavior.None,
                                        new [] {
                                            new TagHelperAttributeDescriptor("foo", pFooPropertyInfo.Object)
                                        }),
                new TagHelperDescriptor("input",
                                        "InputTagHelper",
                                        ContentBehavior.None,
                                        new TagHelperAttributeDescriptor[] {
                                            new TagHelperAttributeDescriptor("type", inputTypePropertyInfo.Object)
                                        }),
                new TagHelperDescriptor("input",
                                        "InputTagHelper2",
                                        ContentBehavior.None,
                                        new TagHelperAttributeDescriptor[] {
                                            new TagHelperAttributeDescriptor("type", inputTypePropertyInfo.Object),
                                            new TagHelperAttributeDescriptor("checked", checkedPropertyInfo.Object)
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
    }
}