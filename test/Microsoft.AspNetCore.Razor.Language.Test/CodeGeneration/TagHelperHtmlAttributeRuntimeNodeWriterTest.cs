// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;
using Microsoft.AspNetCore.Razor.Language.Intermediate;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language.CodeGeneration
{
    public class TagHelperHtmlAttributeRuntimeNodeWriterTest
    {
        [Fact]
        public void WriteHtmlAttributeValue_RendersCorrectly()
        {
            var writer = new TagHelperHtmlAttributeRuntimeNodeWriter();

            var content = "<input checked=\"hello-world @false\" />";
            var sourceDocument = TestRazorSourceDocument.Create(content);
            var codeDocument = RazorCodeDocument.Create(sourceDocument);
            var documentNode = Lower(codeDocument);
            var node = documentNode.Children.OfType<HtmlAttributeIntermediateNode>().Single().Children[0] as HtmlAttributeValueIntermediateNode;

            var context = TestCodeRenderingContext.CreateRuntime();

            // Act
            writer.WriteHtmlAttributeValue(context, node);

            // Assert
            var csharp = context.CodeWriter.GenerateCode();
            Assert.Equal(
@"AddHtmlAttributeValue("""", 16, ""hello-world"", 16, 11, true);
",
                csharp,
                ignoreLineEndingDifferences: true);
        }

        [Fact]
        public void WriteCSharpExpressionAttributeValue_RendersCorrectly()
        {
            var writer = new TagHelperHtmlAttributeRuntimeNodeWriter();
            var content = "<input checked=\"hello-world @false\" />";
            var sourceDocument = TestRazorSourceDocument.Create(content);
            var codeDocument = RazorCodeDocument.Create(sourceDocument);
            var documentNode = Lower(codeDocument);
            var node = documentNode.Children.OfType<HtmlAttributeIntermediateNode>().Single().Children[1] as CSharpExpressionAttributeValueIntermediateNode;

            var context = TestCodeRenderingContext.CreateRuntime();

            // Act
            writer.WriteCSharpExpressionAttributeValue(context, node);

            // Assert
            var csharp = context.CodeWriter.GenerateCode();
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
        public void WriteCSharpCodeAttributeValue_BuffersResult()
        {
            var writer = new TagHelperHtmlAttributeRuntimeNodeWriter();

            var content = "<input checked=\"hello-world @if(@true){ }\" />";
            var sourceDocument = TestRazorSourceDocument.Create(content);
            var codeDocument = RazorCodeDocument.Create(sourceDocument);
            var documentNode = Lower(codeDocument);
            var node = documentNode.Children.OfType<HtmlAttributeIntermediateNode>().Single().Children[1] as CSharpCodeAttributeValueIntermediateNode;

            var context = TestCodeRenderingContext.CreateRuntime(source: sourceDocument);

            // Act
            writer.WriteCSharpCodeAttributeValue(context, node);

            // Assert
            var csharp = context.CodeWriter.GenerateCode();
            Assert.Equal(
@"AddHtmlAttributeValue("" "", 27, new Microsoft.AspNetCore.Mvc.Razor.HelperResult(async(__razor_attribute_value_writer) => {
    PushWriter(__razor_attribute_value_writer);
#line 1 ""test.cshtml""
                             if(@true){ }

#line default
#line hidden
    PopWriter();
}
), 28, 13, false);
",
                csharp,
                ignoreLineEndingDifferences: true);
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

            var documentNode = codeDocument.GetDocumentIntermediateNode();
            Assert.NotNull(documentNode);

            return documentNode;
        }
    }
}
