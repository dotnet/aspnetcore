// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
#if NETCOREAPP1_0
using System.Reflection;
#endif
using Microsoft.AspNetCore.Razor.CodeGenerators.Visitors;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.AspNetCore.Razor.Compilation.TagHelpers;
using Xunit;
using Microsoft.AspNetCore.Razor.Test.CodeGenerators;

namespace Microsoft.AspNetCore.Razor.CodeGenerators
{
    public class TagHelperAttributeValueCodeRendererTest : TagHelperTestBase
    {
        [Fact]
        public void TagHelpers_CanReplaceAttributeChunkGeneratorLogic()
        {
            // Arrange
            var inputTypePropertyInfo = typeof(TestType).GetProperty("Type");
            var checkedPropertyInfo = typeof(TestType).GetProperty("Checked");
            var tagHelperDescriptors = new TagHelperDescriptor[]
            {
                new TagHelperDescriptor
                {
                    TagName = "p",
                    TypeName = "TestNamespace.PTagHelper",
                    AssemblyName = "SomeAssembly"
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
                    TagStructure = TagStructure.WithoutEndTag
                },
                new TagHelperDescriptor
                {
                    TagName = "input",
                    TypeName = "TestNamespace.InputTagHelper2",
                    AssemblyName = "SomeAssembly",
                    Attributes = new TagHelperAttributeDescriptor[]
                    {
                        new TagHelperAttributeDescriptor("type", inputTypePropertyInfo),
                        new TagHelperAttributeDescriptor("checked", checkedPropertyInfo)
                    }
                }
            };

            // Act & Assert
            RunTagHelperTest(testName: "BasicTagHelpers",
                             baseLineName: "BasicTagHelpers.CustomAttributeCodeGenerator",
                             tagHelperDescriptors: tagHelperDescriptors,
                             hostConfig: (host) =>
                             {
                                 return new CodeGeneratorReplacingHost(host);
                             });
        }

        private class CodeGeneratorReplacingHost : CodeGenTestHost
        {
            public CodeGeneratorReplacingHost(RazorEngineHost originalHost)
                : base(new CSharpRazorCodeLanguage())
            {
                GeneratedClassContext = originalHost.GeneratedClassContext;
            }

            public override CodeGenerator DecorateCodeGenerator(
                CodeGenerator incomingBuilder,
                CodeGeneratorContext context)
            {
                return new AttributeChunkGeneratorReplacingCodeGenerator(context);
            }
        }

        private class AttributeChunkGeneratorReplacingCodeGenerator : TestCSharpCodeGenerator
        {
            public AttributeChunkGeneratorReplacingCodeGenerator(CodeGeneratorContext context)
                : base(context)
            {
            }

            protected override CSharpCodeVisitor CreateCSharpCodeVisitor(
                CSharpCodeWriter writer,
                CodeGeneratorContext context)
            {
                var bodyVisitor = base.CreateCSharpCodeVisitor(writer, context);

                bodyVisitor.TagHelperRenderer.AttributeValueCodeRenderer = new CustomTagHelperAttributeCodeRenderer();

                return bodyVisitor;
            }
        }

        private class CustomTagHelperAttributeCodeRenderer : TagHelperAttributeValueCodeRenderer
        {
            public override void RenderAttributeValue(
                TagHelperAttributeDescriptor attributeDescriptor,
                CSharpCodeWriter writer,
                CodeGeneratorContext context,
                Action<CSharpCodeWriter> renderAttributeValue,
                bool complexValue)
            {
                writer.Write("**From custom attribute code renderer**: ");

                base.RenderAttributeValue(attributeDescriptor, writer, context, renderAttributeValue, complexValue);
            }
        }

        private class TestType
        {
            public string Type { get; set; }

            public bool Checked { get; set; }
        }
    }
}
