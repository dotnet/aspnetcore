// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Razor.Language.Intermediate;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language.CodeGeneration
{
    public class RuntimeTagHelperWriterTest
    {
        [Fact]
        public void WriteDeclareTagHelperFields_DeclaresRequiredFields()
        {
            // Arrange
            var node = new DeclareTagHelperFieldsIntermediateNode();
            var writer = new RuntimeTagHelperWriter();
            var context = GetCodeRenderingContext(writer);

            // Act
            writer.WriteDeclareTagHelperFields(context, node);

            // Assert
            var csharp = context.CodeWriter.Builder.ToString();
            Assert.Equal(
@"#line hidden
#pragma warning disable 0414
private string __tagHelperStringValueBuffer = null;
#pragma warning restore 0414
private global::Microsoft.AspNetCore.Razor.Runtime.TagHelpers.TagHelperExecutionContext __tagHelperExecutionContext = null;
private global::Microsoft.AspNetCore.Razor.Runtime.TagHelpers.TagHelperRunner __tagHelperRunner = new global::Microsoft.AspNetCore.Razor.Runtime.TagHelpers.TagHelperRunner();
private global::Microsoft.AspNetCore.Razor.Runtime.TagHelpers.TagHelperScopeManager __backed__tagHelperScopeManager = null;
private global::Microsoft.AspNetCore.Razor.Runtime.TagHelpers.TagHelperScopeManager __tagHelperScopeManager
{
    get
    {
        if (__backed__tagHelperScopeManager == null)
        {
            __backed__tagHelperScopeManager = new global::Microsoft.AspNetCore.Razor.Runtime.TagHelpers.TagHelperScopeManager(StartTagHelperWritingScope, EndTagHelperWritingScope);
        }
        return __backed__tagHelperScopeManager;
    }
}
",
                csharp,
                ignoreLineEndingDifferences: true);
        }

        [Fact]
        public void WriteDeclareTagHelperFields_DeclaresUsedTagHelperTypes()
        {
            // Arrange
            var node = new DeclareTagHelperFieldsIntermediateNode();
            node.UsedTagHelperTypeNames.Add("PTagHelper");
            node.UsedTagHelperTypeNames.Add("MyTagHelper");

            var writer = new RuntimeTagHelperWriter();
            var context = GetCodeRenderingContext(writer);

            // Act
            writer.WriteDeclareTagHelperFields(context, node);

            // Assert
            var csharp = context.CodeWriter.Builder.ToString();
            Assert.Equal(
@"#line hidden
#pragma warning disable 0414
private string __tagHelperStringValueBuffer = null;
#pragma warning restore 0414
private global::Microsoft.AspNetCore.Razor.Runtime.TagHelpers.TagHelperExecutionContext __tagHelperExecutionContext = null;
private global::Microsoft.AspNetCore.Razor.Runtime.TagHelpers.TagHelperRunner __tagHelperRunner = new global::Microsoft.AspNetCore.Razor.Runtime.TagHelpers.TagHelperRunner();
private global::Microsoft.AspNetCore.Razor.Runtime.TagHelpers.TagHelperScopeManager __backed__tagHelperScopeManager = null;
private global::Microsoft.AspNetCore.Razor.Runtime.TagHelpers.TagHelperScopeManager __tagHelperScopeManager
{
    get
    {
        if (__backed__tagHelperScopeManager == null)
        {
            __backed__tagHelperScopeManager = new global::Microsoft.AspNetCore.Razor.Runtime.TagHelpers.TagHelperScopeManager(StartTagHelperWritingScope, EndTagHelperWritingScope);
        }
        return __backed__tagHelperScopeManager;
    }
}
private global::PTagHelper __PTagHelper = null;
private global::MyTagHelper __MyTagHelper = null;
",
                csharp,
                ignoreLineEndingDifferences: true);
        }

        [Fact]
        public void WriteTagHelperBody_RendersCorrectly_UsesTagNameAndModeFromContext()
        {
            // Arrange
            var node = new TagHelperBodyIntermediateNode();
            var writer = new RuntimeTagHelperWriter();
            var context = GetCodeRenderingContext(writer);
            context.Items[CodeRenderingContext.SuppressUniqueIds] = "test";
            context.TagHelperRenderingContext = new TagHelperRenderingContext()
            {
                TagName = "p",
                TagMode = TagMode.SelfClosing
            };

            // Act
            writer.WriteTagHelperBody(context, node);

            // Assert
            var csharp = context.CodeWriter.Builder.ToString();
            Assert.Equal(
@"__tagHelperExecutionContext = __tagHelperScopeManager.Begin(""p"", global::Microsoft.AspNetCore.Razor.TagHelpers.TagMode.SelfClosing, ""test"", async() => {
    Render Children
}
);
",
                csharp,
                ignoreLineEndingDifferences: true);
        }

        [Fact]
        public void WriteCreateTagHelper_RendersCorrectly_UsesSpecifiedTagHelperType()
        {
            // Arrange
            var node = new CreateTagHelperIntermediateNode()
            {
                TagHelperTypeName = "TestNamespace.MyTagHelper"
            };
            var writer = new RuntimeTagHelperWriter();
            var context = GetCodeRenderingContext(writer);

            // Act
            writer.WriteCreateTagHelper(context, node);

            // Assert
            var csharp = context.CodeWriter.Builder.ToString();
            Assert.Equal(
@"__TestNamespace_MyTagHelper = CreateTagHelper<global::TestNamespace.MyTagHelper>();
__tagHelperExecutionContext.Add(__TestNamespace_MyTagHelper);
",
                csharp,
                ignoreLineEndingDifferences: true);
        }

        [Fact]
        public void WriteTagHelper_RendersCorrectly_SetsTagNameAndModeInContext()
        {
            // Arrange
            var node = new TagHelperIntermediateNode();
            var writer = new RuntimeTagHelperWriter();
            var context = GetCodeRenderingContext(writer);

            // Act
            writer.WriteTagHelper(context, node);

            // Assert
            var csharp = context.CodeWriter.Builder.ToString();
            Assert.Equal(
@"Render Children
await __tagHelperRunner.RunAsync(__tagHelperExecutionContext);
if (!__tagHelperExecutionContext.Output.IsContentModified)
{
    await __tagHelperExecutionContext.SetOutputContentAsync();
}
Write(__tagHelperExecutionContext.Output);
__tagHelperExecutionContext = __tagHelperScopeManager.End();
",
                csharp,
                ignoreLineEndingDifferences: true);
        }

        [Fact]
        public void WriteAddTagHelperHtmlAttribute_RendersCorrectly()
        {
            // Arrange
            var descriptors = new[]
            {
                CreateTagHelperDescriptor(
                    tagName: "input",
                    typeName: "InputTagHelper",
                    assemblyName: "TestAssembly")
            };
            var engine = RazorEngine.Create(builder => builder.AddTagHelpers(descriptors));
            var content = @"
@addTagHelper *, TestAssembly
<input name=""value"" />";
            var sourceDocument = TestRazorSourceDocument.Create(content);
            var codeDocument = RazorCodeDocument.Create(sourceDocument);
            var irDocument = Lower(codeDocument, engine);
            var node = irDocument.Children.Last().Children[2] as AddTagHelperHtmlAttributeIntermediateNode;

            var writer = new RuntimeTagHelperWriter();
            var context = GetCodeRenderingContext(writer, codeDocument);

            // Act
            writer.WriteAddTagHelperHtmlAttribute(context, node);

            // Assert
            var csharp = context.CodeWriter.Builder.ToString();
            Assert.Equal(
@"BeginWriteTagHelperAttribute();
Render Children
__tagHelperStringValueBuffer = EndWriteTagHelperAttribute();
__tagHelperExecutionContext.AddHtmlAttribute(""name"", Html.Raw(__tagHelperStringValueBuffer), global::Microsoft.AspNetCore.Razor.TagHelpers.HtmlAttributeValueStyle.DoubleQuotes);
",
                csharp,
                ignoreLineEndingDifferences: true);
        }

        [Fact]
        public void WriteAddTagHelperHtmlAttribute_DataAttribute_RendersCorrectly()
        {
            // Arrange
            var descriptors = new[]
            {
                CreateTagHelperDescriptor(
                    tagName: "input",
                    typeName: "InputTagHelper",
                    assemblyName: "TestAssembly")
            };
            var engine = RazorEngine.Create(builder => builder.AddTagHelpers(descriptors));
            var content = @"
@addTagHelper *, TestAssembly
<input data-test=""Blah-@Foo"" />";
            var sourceDocument = TestRazorSourceDocument.Create(content);
            var codeDocument = RazorCodeDocument.Create(sourceDocument);
            var irDocument = Lower(codeDocument, engine);
            var node = irDocument.Children.Last().Children[2] as AddTagHelperHtmlAttributeIntermediateNode;

            var writer = new RuntimeTagHelperWriter();
            var context = GetCodeRenderingContext(writer, codeDocument);

            // Act
            writer.WriteAddTagHelperHtmlAttribute(context, node);

            // Assert
            var csharp = context.CodeWriter.Builder.ToString();
            Assert.Equal(
@"BeginWriteTagHelperAttribute();
Render Children
__tagHelperStringValueBuffer = EndWriteTagHelperAttribute();
__tagHelperExecutionContext.AddHtmlAttribute(""data-test"", Html.Raw(__tagHelperStringValueBuffer), global::Microsoft.AspNetCore.Razor.TagHelpers.HtmlAttributeValueStyle.DoubleQuotes);
",
                csharp,
                ignoreLineEndingDifferences: true);
        }

        [Fact]
        public void WriteAddTagHelperHtmlAttribute_DynamicAttribute_RendersCorrectly()
        {
            // Arrange
            var descriptors = new[]
            {
                CreateTagHelperDescriptor(
                    tagName: "input",
                    typeName: "InputTagHelper",
                    assemblyName: "TestAssembly")
            };
            var engine = RazorEngine.Create(builder => builder.AddTagHelpers(descriptors));
            var content = @"
@addTagHelper *, TestAssembly
<input test=""Blah-@Foo"" />";
            var sourceDocument = TestRazorSourceDocument.Create(content);
            var codeDocument = RazorCodeDocument.Create(sourceDocument);
            var irDocument = Lower(codeDocument, engine);
            var node = irDocument.Children.Last().Children[2] as AddTagHelperHtmlAttributeIntermediateNode;

            var writer = new RuntimeTagHelperWriter();
            var context = GetCodeRenderingContext(writer, codeDocument);

            // Act
            writer.WriteAddTagHelperHtmlAttribute(context, node);

            // Assert
            var csharp = context.CodeWriter.Builder.ToString();
            Assert.Equal(
@"BeginAddHtmlAttributeValues(__tagHelperExecutionContext, ""test"", 2, global::Microsoft.AspNetCore.Razor.TagHelpers.HtmlAttributeValueStyle.DoubleQuotes);
Render Children
EndAddHtmlAttributeValues(__tagHelperExecutionContext);
",
                csharp,
                ignoreLineEndingDifferences: true);
        }

        [Fact]
        public void WriteSetTagHelperProperty_RendersCorrectly()
        {
            // Arrange
            var descriptors = new[]
            {
                CreateTagHelperDescriptor(
                    tagName: "input",
                    typeName: "InputTagHelper",
                    assemblyName: "TestAssembly",
                    attributes: new Action<BoundAttributeDescriptorBuilder>[]
                    {
                        builder => builder
                            .Name("bound")
                            .PropertyName("FooProp")
                            .TypeName("System.String"),
                    })
            };
            var engine = RazorEngine.Create(builder => builder.AddTagHelpers(descriptors));
            var content = @"
@addTagHelper *, TestAssembly
<input bound=""value"" />";
            var sourceDocument = TestRazorSourceDocument.Create(content);
            var codeDocument = RazorCodeDocument.Create(sourceDocument);
            var irDocument = Lower(codeDocument, engine);
            var node = irDocument.Children.Last().Children[2] as SetTagHelperPropertyIntermediateNode;

            var writer = new RuntimeTagHelperWriter();
            var context = GetCodeRenderingContext(writer, codeDocument);

            // Act
            writer.WriteSetTagHelperProperty(context, node);

            // Assert
            var csharp = context.CodeWriter.Builder.ToString();

            // The attribute value is not rendered inline because we are not using the preallocated writer.
            Assert.Equal(
@"BeginWriteTagHelperAttribute();
Render Children
__tagHelperStringValueBuffer = EndWriteTagHelperAttribute();
__InputTagHelper.FooProp = __tagHelperStringValueBuffer;
__tagHelperExecutionContext.AddTagHelperAttribute(""bound"", __InputTagHelper.FooProp, global::Microsoft.AspNetCore.Razor.TagHelpers.HtmlAttributeValueStyle.DoubleQuotes);
",
                csharp,
                ignoreLineEndingDifferences: true);
        }

        [Fact]
        public void WriteSetTagHelperProperty_NonStringAttribute_RendersCorrectly()
        {
            // Arrange
            var descriptors = new[]
            {
                CreateTagHelperDescriptor(
                    tagName: "input",
                    typeName: "InputTagHelper",
                    assemblyName: "TestAssembly",
                    attributes: new Action<BoundAttributeDescriptorBuilder>[]
                    {
                        builder => builder
                            .Name("bound")
                            .PropertyName("FooProp")
                            .TypeName("System.Int32"),
                    })
            };
            var engine = RazorEngine.Create(builder => builder.AddTagHelpers(descriptors));
            var content = @"
@addTagHelper *, TestAssembly
<input bound=""42"" />";
            var sourceDocument = TestRazorSourceDocument.Create(content);
            var codeDocument = RazorCodeDocument.Create(sourceDocument);
            var irDocument = Lower(codeDocument, engine);
            var node = irDocument.Children.Last().Children[2] as SetTagHelperPropertyIntermediateNode;

            var writer = new RuntimeTagHelperWriter();
            var context = GetCodeRenderingContext(writer, codeDocument);

            // Act
            writer.WriteSetTagHelperProperty(context, node);

            // Assert
            var csharp = context.CodeWriter.Builder.ToString();
            Assert.Equal(
@"#line 3 ""test.cshtml""
__InputTagHelper.FooProp = 42;

#line default
#line hidden
__tagHelperExecutionContext.AddTagHelperAttribute(""bound"", __InputTagHelper.FooProp, global::Microsoft.AspNetCore.Razor.TagHelpers.HtmlAttributeValueStyle.DoubleQuotes);
",
                csharp,
                ignoreLineEndingDifferences: true);
        }

        [Fact]
        public void WriteSetTagHelperProperty_IndexerAttribute_RendersCorrectly()
        {
            // Arrange
            var descriptors = new[]
            {
                CreateTagHelperDescriptor(
                    tagName: "input",
                    typeName: "InputTagHelper",
                    assemblyName: "TestAssembly",
                    attributes: new Action<BoundAttributeDescriptorBuilder>[]
                    {
                        builder => builder
                            .Name("bound")
                            .PropertyName("FooProp")
                            .TypeName("System.Collections.Generic.Dictionary<System.String, System.Int32>")
                            .AsDictionaryAttribute("foo-", "System.Int32"),
                    })
            };
            var engine = RazorEngine.Create(builder => builder.AddTagHelpers(descriptors));
            var content = @"
@addTagHelper *, TestAssembly
<input foo-bound=""42"" />";
            var sourceDocument = TestRazorSourceDocument.Create(content);
            var codeDocument = RazorCodeDocument.Create(sourceDocument);
            var irDocument = Lower(codeDocument, engine);
            var node = irDocument.Children.Last().Children[2] as SetTagHelperPropertyIntermediateNode;

            var writer = new RuntimeTagHelperWriter();
            var context = GetCodeRenderingContext(writer, codeDocument);

            // Act
            writer.WriteSetTagHelperProperty(context, node);

            // Assert
            var csharp = context.CodeWriter.Builder.ToString();
            Assert.Equal(
@"if (__InputTagHelper.FooProp == null)
{
    throw new InvalidOperationException(InvalidTagHelperIndexerAssignment(""foo-bound"", ""InputTagHelper"", ""FooProp""));
}
#line 3 ""test.cshtml""
__InputTagHelper.FooProp[""bound""] = 42;

#line default
#line hidden
__tagHelperExecutionContext.AddTagHelperAttribute(""foo-bound"", __InputTagHelper.FooProp[""bound""], global::Microsoft.AspNetCore.Razor.TagHelpers.HtmlAttributeValueStyle.DoubleQuotes);
",
                csharp,
                ignoreLineEndingDifferences: true);
        }

        private static CodeRenderingContext GetCodeRenderingContext(TagHelperWriter writer, RazorCodeDocument codeDocument = null)
        {
            var codeWriter = new CodeWriter();
            var nodeWriter = new RuntimeNodeWriter();
            var options = RazorCodeGenerationOptions.CreateDefault();
            var context = new DefaultCodeRenderingContext(codeWriter, nodeWriter, codeDocument?.Source, options)
            {
                TagHelperWriter = writer,
                TagHelperRenderingContext = new TagHelperRenderingContext()
            };
            context.SetRenderChildren(n =>
            {
                codeWriter.WriteLine("Render Children");
            });

            return context;
        }

        private static DocumentIntermediateNode Lower(RazorCodeDocument codeDocument)
        {
            var engine = RazorEngine.Create();

            return Lower(codeDocument, engine);
        }

        private static DocumentIntermediateNode Lower(RazorCodeDocument codeDocument, RazorEngine engine)
        {
            for (var i = 0; i < engine.Phases.Count; i++)
            {
                var phase = engine.Phases[i];
                phase.Execute(codeDocument);

                if (phase is IRazorIntermediateNodeLoweringPhase)
                {
                    break;
                }
            }

            var irDocument = codeDocument.GetDocumentIntermediateNode();
            Assert.NotNull(irDocument);

            return irDocument;
        }

        private static TagHelperDescriptor CreateTagHelperDescriptor(
            string tagName,
            string typeName,
            string assemblyName,
            IEnumerable<Action<BoundAttributeDescriptorBuilder>> attributes = null)
        {
            var builder = TagHelperDescriptorBuilder.Create(typeName, assemblyName);
            builder.TypeName(typeName);

            if (attributes != null)
            {
                foreach (var attributeBuilder in attributes)
                {
                    builder.BoundAttributeDescriptor(attributeBuilder);
                }
            }

            builder.TagMatchingRuleDescriptor(ruleBuilder => ruleBuilder.RequireTagName(tagName));

            var descriptor = builder.Build();

            return descriptor;
        }
    }
}
