// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
#if DNXCORE50
using System.Reflection;
#endif
using Microsoft.AspNet.Razor.Generator;
using Microsoft.AspNet.Razor.Generator.Compiler;
using Microsoft.AspNet.Razor.Generator.Compiler.CSharp;
using Microsoft.AspNet.Razor.TagHelpers;
using Xunit;

namespace Microsoft.AspNet.Razor.Test.Generator
{
    public class TagHelperAttributeValueCodeRendererTest : TagHelperTestBase
    {
        [Fact]
        public void TagHelpers_CanReplaceAttributeCodeGeneratorLogic()
        {
            // Arrange
            var inputTypePropertyInfo = typeof(TestType).GetProperty("Type");
            var checkedPropertyInfo = typeof(TestType).GetProperty("Checked");
            var tagHelperDescriptors = new TagHelperDescriptor[]
            {
                new TagHelperDescriptor("p", "PTagHelper", "SomeAssembly"),
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

            // Act & Assert
            RunTagHelperTest(testName: "BasicTagHelpers",
                             baseLineName: "BasicTagHelpers.CustomAttributeCodeGenerator",
                             tagHelperDescriptors: tagHelperDescriptors,
                             hostConfig: (host) =>
                             {
                                 return new CodeBuilderReplacingHost(host);
                             });
        }

        private class CodeBuilderReplacingHost : CodeGenTestHost
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

        private class AttributeCodeGeneratorReplacingCodeBuilder : TestCSharpCodeBuilder
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
                                                      [NotNull] Action<CSharpCodeWriter> renderAttributeValue,
                                                      bool complexValue)
            {
                writer.Write("**From custom attribute code renderer**: ");

                base.RenderAttributeValue(attributeInfo, writer, context, renderAttributeValue, complexValue);
            }
        }

        private class TestType
        {
            public string Type { get; set; }

            public bool Checked { get; set; }
        }
    }
}
