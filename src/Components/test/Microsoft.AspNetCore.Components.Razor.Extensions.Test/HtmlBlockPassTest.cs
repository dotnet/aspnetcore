// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Intermediate;
using Xunit;


namespace Microsoft.AspNetCore.Components.Razor
{
    public class HtmlBlockPassTest
    {
        public HtmlBlockPassTest()
        {
            Pass = new HtmlBlockPass();
            Engine = RazorProjectEngine.Create(
                BlazorExtensionInitializer.DefaultConfiguration,
                RazorProjectFileSystem.Create(Environment.CurrentDirectory),
                b =>
                {
                    BlazorExtensionInitializer.Register(b);

                    if (b.Features.OfType<HtmlBlockPass>().Any())
                    {
                        b.Features.Remove(b.Features.OfType<HtmlBlockPass>().Single());
                    }
                }).Engine;

            Pass.Engine = Engine;
        }

        private RazorEngine Engine { get; }

        private HtmlBlockPass Pass { get; }

        [Fact]
        public void Execute_RewritesHtml_Basic()
        {
            // Arrange
            var document = CreateDocument(@"
<html>
  <head cool=""beans"">
    Hello, World!
  </head>
</html>");

            var expected = NormalizeContent(@"
<html>
  <head cool=""beans"">
    Hello, World!
  </head>
</html>");

            var documentNode = Lower(document);

            // Act
            Pass.Execute(document, documentNode);

            // Assert
            var block = documentNode.FindDescendantNodes<HtmlBlockIntermediateNode>().Single();
            Assert.Equal(expected, block.Content, ignoreLineEndingDifferences: true);
        }

        [Fact]
        public void Execute_RewritesHtml_WithComment()
        {
            // Arrange
            var document = CreateDocument(@"Start<!-- -->End");

            var expected = NormalizeContent(@"StartEnd");

            var documentNode = Lower(document);

            // Act
            Pass.Execute(document, documentNode);

            // Assert
            var block = documentNode.FindDescendantNodes<HtmlBlockIntermediateNode>().Single();
            Assert.Equal(expected, block.Content, ignoreLineEndingDifferences: true);
        }

        [Fact]
        public void Execute_RewritesHtml_MergesSiblings()
        {
            // Arrange
            var document = CreateDocument(@"
<html>
  @(""Hi"")<div></div>
  <div></div>
  <div>@(""Hi"")</div>
</html>");

            var expected = NormalizeContent(@"
<div></div>
  <div></div>
  ");

            var documentNode = Lower(document);

            // Act
            Pass.Execute(document, documentNode);

            // Assert
            var block = documentNode.FindDescendantNodes<HtmlBlockIntermediateNode>().Single();
            Assert.Equal(expected, block.Content, ignoreLineEndingDifferences: true);
        }

        [Fact]
        public void Execute_RewritesHtml_MergesSiblings_LeftEdge()
        {
            // Arrange
            var document = CreateDocument(@"
<html><div></div>
  <div></div>
  <div>@(""Hi"")</div>
</html>");

            var expected = NormalizeContent(@"
<div></div>
  <div></div>
  ");

            var documentNode = Lower(document);

            // Act
            Pass.Execute(document, documentNode);

            // Assert
            var block = documentNode.FindDescendantNodes<HtmlBlockIntermediateNode>().Single();
            Assert.Equal(expected, block.Content, ignoreLineEndingDifferences: true);
        }


        [Fact]
        public void Execute_RewritesHtml_CSharpInAttributes()
        {
            // Arrange
            var document = CreateDocument(@"
<html>
  <head cool=""beans"" csharp=""@yes"" mixed=""hi @there"">
    <div>foo</div>
  </head>
</html>");

            var expected = NormalizeContent("<div>foo</div>\n  ");

            var documentNode = Lower(document);

            // Act
            Pass.Execute(document, documentNode);

            // Assert
            var block = documentNode.FindDescendantNodes<HtmlBlockIntermediateNode>().Single();
            Assert.Equal(expected, block.Content, ignoreLineEndingDifferences: true);
        }

        [Fact]
        public void Execute_RewritesHtml_CSharpInBody()
        {
            // Arrange
            var document = CreateDocument(@"
<html>
  <head cool=""beans"">
    <div>@foo</div>
    <div>rewriteme</div>
    <div>@bar</div>
  </head>
</html>");

            var expected = NormalizeContent("<div>rewriteme</div>\n    ");

            var documentNode = Lower(document);

            // Act
            Pass.Execute(document, documentNode);

            // Assert
            var block = documentNode.FindDescendantNodes<HtmlBlockIntermediateNode>().Single();
            Assert.Equal(expected, block.Content, ignoreLineEndingDifferences: true);
        }

        [Fact]
        public void Execute_RewritesHtml_EncodesHtmlEntities()
        {
            // Arrange
            var document = CreateDocument(@"
<div>
    &lt;span&gt;Hi&lt;/span&gt;
</div>");

            var expected = NormalizeContent(@"
<div>
    &lt;span&gt;Hi&lt;/span&gt;
</div>");

            var documentNode = Lower(document);

            // Act
            Pass.Execute(document, documentNode);

            // Assert
            var block = documentNode.FindDescendantNodes<HtmlBlockIntermediateNode>().Single();
            Assert.Equal(expected, block.Content, ignoreLineEndingDifferences: true);
        }

        [Fact]
        public void Execute_RewritesHtml_EmptyNonvoid()
        {
            // Arrange
            var document = CreateDocument(@"<a href=""...""></a>");

            var expected = NormalizeContent(@"<a href=""...""></a>");

            var documentNode = Lower(document);

            // Act
            Pass.Execute(document, documentNode);

            // Assert
            var block = documentNode.FindDescendantNodes<HtmlBlockIntermediateNode>().Single();
            Assert.Equal(expected, block.Content, ignoreLineEndingDifferences: true);
        }

        [Fact]
        public void Execute_RewritesHtml_Void()
        {
            // Arrange
            var document = CreateDocument(@"<link rel=""..."" href=""...""/>");

            var expected = NormalizeContent(@"<link rel=""..."" href=""..."">");

            var documentNode = Lower(document);

            // Act
            Pass.Execute(document, documentNode);

            // Assert
            var block = documentNode.FindDescendantNodes<HtmlBlockIntermediateNode>().Single();
            Assert.Equal(expected, block.Content, ignoreLineEndingDifferences: true);
        }

        [Fact]
        public void Execute_CannotRewriteHtml_CSharpInCode()
        {
            // Arrange
            var document = CreateDocument(@"
<html>
  @if (some_bool)
  {
  <head cool=""beans"">
    @hello
  </head>
  }
</html>");

            var documentNode = Lower(document);

            // Act
            Pass.Execute(document, documentNode);

            // Assert
            Assert.Empty(documentNode.FindDescendantNodes<HtmlBlockIntermediateNode>());
        }

        [Fact]
        public void Execute_CannotRewriteHtml_Script()
        {
            // Arrange
            var document = CreateDocument(@"
<html>
  @if (some_bool)
  {
  <head cool=""beans"">
    <script>...</script>
  </head>
  }
</html>");

            var documentNode = Lower(document);

            // Act
            Pass.Execute(document, documentNode);

            // Assert
            Assert.Empty(documentNode.FindDescendantNodes<HtmlBlockIntermediateNode>());
        }

        // The unclosed tag will have errors, so we won't rewrite it or its parent.
        [Fact]
        public void Execute_CannotRewriteHtml_Errors()
        {
            // Arrange
            var document = CreateDocument(@"
<html>
  <a href=""..."">
</html>");

            var documentNode = Lower(document);

            // Act
            Pass.Execute(document, documentNode);

            // Assert
            Assert.Empty(documentNode.FindDescendantNodes<HtmlBlockIntermediateNode>());
        }

        [Fact]
        public void Execute_RewritesHtml_MismatchedClosingTag()
        {
            // Arrange
            var document = CreateDocument(@"
<html>
  <div>
    <div>rewriteme</div>
  </span>
</html>");

            var expected = NormalizeContent("<div>rewriteme</div>\n  ");

            var documentNode = Lower(document);

            // Act
            Pass.Execute(document, documentNode);

            // Assert
            var block = documentNode.FindDescendantNodes<HtmlBlockIntermediateNode>().Single();
            Assert.Equal(expected, block.Content, ignoreLineEndingDifferences: true);
        }

        private string NormalizeContent(string content)
        {
            // Test inputs frequently have leading space for readability.
            content = content.TrimStart();

            // Normalize newlines since we are testing lengths of things.
            content = content.Replace("\r", "");
            content = content.Replace("\n", "\r\n");

            return content;
        }

        private RazorCodeDocument CreateDocument(string content)
        {
            // Normalize newlines since we are testing lengths of things.
            content = content.Replace("\r", "");
            content = content.Replace("\n", "\r\n");

            var source = RazorSourceDocument.Create(content, "test.cshtml");
            return RazorCodeDocument.Create(source);
        }

        private DocumentIntermediateNode Lower(RazorCodeDocument codeDocument)
        {
            for (var i = 0; i < Engine.Phases.Count; i++)
            {
                var phase = Engine.Phases[i];
                if (phase is IRazorCSharpLoweringPhase)
                {
                    break;
                }

                phase.Execute(codeDocument);
            }

            var document = codeDocument.GetDocumentIntermediateNode();
            Engine.Features.OfType<ComponentDocumentClassifierPass>().Single().Execute(codeDocument, document);
            return document;
        }

        private class StaticTagHelperFeature : ITagHelperFeature
        {
            public RazorEngine Engine { get; set; }

            public List<TagHelperDescriptor> TagHelpers { get; set; }

            public IReadOnlyList<TagHelperDescriptor> GetDescriptors()
            {
                return TagHelpers;
            }
        }
    }
}