// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;
using Microsoft.AspNetCore.Razor.Language.Intermediate;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language.CodeGeneration
{
    public class TagHelperHtmlAttributeRuntimeBasicWriterTest
    {
        [Fact]
        public void WriteHtmlAttributeValue_RendersCorrectly()
        {
            var writer = new TagHelperHtmlAttributeRuntimeBasicWriter();
            var context = GetCSharpRenderingContext(writer);

            var content = "<input checked=\"hello-world @false\" />";
            var sourceDocument = TestRazorSourceDocument.Create(content);
            var codeDocument = RazorCodeDocument.Create(sourceDocument);
            var irDocument = Lower(codeDocument);
            var node = irDocument.Children.OfType<HtmlAttributeIRNode>().Single().Children[0] as HtmlAttributeValueIRNode;

            // Act
            writer.WriteHtmlAttributeValue(context, node);

            // Assert
            var csharp = context.Writer.Builder.ToString();
            Assert.Equal(
@"AddHtmlAttributeValue("""", 16, ""hello-world"", 16, 11, true);
",
                csharp,
                ignoreLineEndingDifferences: true);
        }

        [Fact]
        public void WriteCSharpAttributeValue_RendersCorrectly()
        {
            var writer = new TagHelperHtmlAttributeRuntimeBasicWriter();
            var context = GetCSharpRenderingContext(writer);

            var content = "<input checked=\"hello-world @false\" />";
            var sourceDocument = TestRazorSourceDocument.Create(content);
            var codeDocument = RazorCodeDocument.Create(sourceDocument);
            var irDocument = Lower(codeDocument);
            var node = irDocument.Children.OfType<HtmlAttributeIRNode>().Single().Children[1] as CSharpAttributeValueIRNode;

            // Act
            writer.WriteCSharpAttributeValue(context, node);

            // Assert
            var csharp = context.Writer.Builder.ToString();
            Assert.Equal(
@"#line 1 ""test.cshtml""
AddHtmlAttributeValue("" "", 27, false, 28, 6, false);

#line default
#line hidden
",
                csharp,
                ignoreLineEndingDifferences: true);
        }

        [Fact]
        public void WriteCSharpAttributeValue_NonExpression_BuffersResult()
        {
            var writer = new TagHelperHtmlAttributeRuntimeBasicWriter();
            var context = GetCSharpRenderingContext(writer);

            var content = "<input checked=\"hello-world @if(@true){ }\" />";
            var sourceDocument = TestRazorSourceDocument.Create(content);
            var codeDocument = RazorCodeDocument.Create(sourceDocument);
            var irDocument = Lower(codeDocument);
            var node = irDocument.Children.OfType<HtmlAttributeIRNode>().Single().Children[1] as CSharpAttributeValueIRNode;

            // Act
            writer.WriteCSharpAttributeValue(context, node);

            // Assert
            var csharp = context.Writer.Builder.ToString();
            Assert.Equal(
@"AddHtmlAttributeValue("" "", 27, new Microsoft.AspNetCore.Mvc.Razor.HelperResult(async(__razor_attribute_value_writer) => {
    Render Children
}
), 28, 13, false);
",
                csharp,
                ignoreLineEndingDifferences: true);
        }

        private static CSharpRenderingContext GetCSharpRenderingContext(BasicWriter writer)
        {
            var options = RazorParserOptions.CreateDefaultOptions();
            var codeWriter = new Legacy.CSharpCodeWriter();
            var context = new CSharpRenderingContext()
            {
                Writer = codeWriter,
                Options = options,
                BasicWriter = writer,
                RenderChildren = n =>
                {
                    codeWriter.WriteLine("Render Children");
                }
            };

            return context;
        }

        private static DocumentIRNode Lower(RazorCodeDocument codeDocument)
        {
            var engine = RazorEngine.Create();

            return Lower(codeDocument, engine);
        }

        private static DocumentIRNode Lower(RazorCodeDocument codeDocument, RazorEngine engine)
        {
            for (var i = 0; i < engine.Phases.Count; i++)
            {
                var phase = engine.Phases[i];
                phase.Execute(codeDocument);

                if (phase is IRazorIRLoweringPhase)
                {
                    break;
                }
            }

            var irDocument = codeDocument.GetIRDocument();
            Assert.NotNull(irDocument);

            return irDocument;
        }
    }
}
