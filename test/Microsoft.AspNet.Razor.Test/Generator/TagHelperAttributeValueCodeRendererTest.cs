// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Reflection;
using Microsoft.AspNet.Razor.Generator;
using Microsoft.AspNet.Razor.Generator.Compiler;
using Microsoft.AspNet.Razor.Generator.Compiler.CSharp;
using Microsoft.AspNet.Razor.TagHelpers;
using Moq;
using Xunit;

namespace Microsoft.AspNet.Razor.Test.Generator
{
    public class TagHelperAttributeValueCodeRendererTest : TagHelperTestBase
    {
        [Fact]
        public void TagHelpers_CanReplaceAttributeCodeGeneratorLogic()
        {
            // Arrange
            var inputTypePropertyInfo = new Mock<PropertyInfo>();
            inputTypePropertyInfo.Setup(propertyInfo => propertyInfo.PropertyType).Returns(typeof(string));
            inputTypePropertyInfo.Setup(propertyInfo => propertyInfo.Name).Returns("Type");
            var checkedPropertyInfo = new Mock<PropertyInfo>();
            checkedPropertyInfo.Setup(propertyInfo => propertyInfo.PropertyType).Returns(typeof(bool));
            checkedPropertyInfo.Setup(propertyInfo => propertyInfo.Name).Returns("Checked");
            var tagHelperDescriptors = new TagHelperDescriptor[]
            {
                new TagHelperDescriptor("p", "PTagHelper", ContentBehavior.None),
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
            RunTagHelperTest(testName: "BasicTagHelpers",
                             baseLineName: "BasicTagHelpers.CustomAttributeCodeGenerator",
                             tagHelperDescriptors: tagHelperDescriptors,
                             hostConfig: (host) =>
                             {
                                 return new CodeBuilderReplacingHost(host);
                             });
        }

        private class CodeBuilderReplacingHost : RazorEngineHost
        {
            public CodeBuilderReplacingHost(RazorEngineHost originalHost)
                : base(new CSharpRazorCodeLanguage())
            {
                GeneratedClassContext = originalHost.GeneratedClassContext;
            }

            public override CodeBuilder DecorateCodeBuilder(CodeBuilder incomingBuilder, CodeBuilderContext context)
            {
                return new AttributeCodeGeneratorReplacingCodeBuilder(context);
            }
        }

        private class AttributeCodeGeneratorReplacingCodeBuilder : CSharpCodeBuilder
        {
            public AttributeCodeGeneratorReplacingCodeBuilder(CodeBuilderContext context)
                : base(context)
            {
            }

            protected override CSharpCodeVisitor CreateCSharpCodeVisitor([NotNull] CSharpCodeWriter writer,
                                                                         [NotNull] CodeBuilderContext context)
            {
                var bodyVisitor = base.CreateCSharpCodeVisitor(writer, context);

                bodyVisitor.TagHelperRenderer.AttributeValueCodeRenderer = new CustomTagHelperAttributeCodeRenderer();

                return bodyVisitor;
            }
        }

        private class CustomTagHelperAttributeCodeRenderer : TagHelperAttributeValueCodeRenderer
        {
            public override void RenderAttributeValue([NotNull] TagHelperAttributeDescriptor attributeInfo,
                                                      [NotNull] CSharpCodeWriter writer,
                                                      [NotNull] CodeBuilderContext context,
                                                      [NotNull] Action<CSharpCodeWriter> renderAttributeValue)
            {
                writer.Write("**From custom attribute code renderer**: ");

                base.RenderAttributeValue(attributeInfo, writer, context, renderAttributeValue);
            }
        }
    }
}