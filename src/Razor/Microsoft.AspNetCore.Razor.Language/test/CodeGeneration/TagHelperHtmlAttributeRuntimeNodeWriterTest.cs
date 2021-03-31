// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using Microsoft.AspNetCore.Razor.Language.Intermediate;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language.CodeGeneration
{
    public class TagHelperHtmlAttributeRuntimeNodeWriterTest : RazorProjectEngineTestBase
    {
        protected override RazorLanguageVersion Version => RazorLanguageVersion.Latest;

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
@"
#nullable restore
#line 1 ""test.cshtml""
AddHtmlAttributeValue("" "", 27, false, 28, 6, false);

#line default
#line hidden
#nullable disable
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
#nullable restore
#line 1 ""test.cshtml""
                             if(@true){ }

#line default
#line hidden
#nullable disable
    PopWriter();
}
), 28, 13, false);
",
                csharp,
                ignoreLineEndingDifferences: true);
        }

        private DocumentIntermediateNode Lower(RazorCodeDocument codeDocument)
        {
            var projectEngine = CreateProjectEngine();
            return Lower(codeDocument, projectEngine);
        }

        private DocumentIntermediateNode Lower(RazorCodeDocument codeDocument, RazorProjectEngine projectEngine)
        {
            for (var i = 0; i < projectEngine.Phases.Count; i++)
            {
                var phase = projectEngine.Phases[i];
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
