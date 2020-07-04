// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Razor.Language.Intermediate;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language.Components
{
    public class ComponentMarkupBlockPassTest
    {
        public ComponentMarkupBlockPassTest()
        {
            Pass = new ComponentMarkupBlockPass();
            ProjectEngine = (DefaultRazorProjectEngine)RazorProjectEngine.Create(
                RazorConfiguration.Default,
                RazorProjectFileSystem.Create(Environment.CurrentDirectory),
                b =>
                {
                    if (b.Features.OfType<ComponentMarkupBlockPass>().Any())
                    {
                        b.Features.Remove(b.Features.OfType<ComponentMarkupBlockPass>().Single());
                    }
                });
            Engine = ProjectEngine.Engine;

            Pass.Engine = Engine;
        }

        private DefaultRazorProjectEngine ProjectEngine { get; }

        private RazorEngine Engine { get; }

        private ComponentMarkupBlockPass Pass { get; }

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
<html><head cool=""beans"">
    Hello, World!
  </head></html>");

            var documentNode = Lower(document);

            // Act
            Pass.Execute(document, documentNode);

            // Assert
            var block = documentNode.FindDescendantNodes<MarkupBlockIntermediateNode>().Single();
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
            var block = documentNode.FindDescendantNodes<MarkupBlockIntermediateNode>().Single();
            Assert.Equal(expected, block.Content, ignoreLineEndingDifferences: true);
        }

        // See: https://github.com/dotnet/aspnetcore/issues/6480
        [Fact]
        public void Execute_RewritesHtml_HtmlAttributePrefix()
        {
            // Arrange
            var document = CreateDocument(@"<div class=""one two"">Hi</div>");

            var expected = NormalizeContent(@"<div class=""one two"">Hi</div>");

            var documentNode = Lower(document);

            // Act
            Pass.Execute(document, documentNode);

            // Assert
            var block = documentNode.FindDescendantNodes<MarkupBlockIntermediateNode>().Single();
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
            var block = documentNode.FindDescendantNodes<MarkupBlockIntermediateNode>().Single();
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
            var block = documentNode.FindDescendantNodes<MarkupBlockIntermediateNode>().Single();
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

            var expected = NormalizeContent("<div>foo</div>");

            var documentNode = Lower(document);

            // Act
            Pass.Execute(document, documentNode);

            // Assert
            var block = documentNode.FindDescendantNodes<MarkupBlockIntermediateNode>().Single();
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
            var block = documentNode.FindDescendantNodes<MarkupBlockIntermediateNode>().Single();
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
            var block = documentNode.FindDescendantNodes<MarkupBlockIntermediateNode>().Single();
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
            var block = documentNode.FindDescendantNodes<MarkupBlockIntermediateNode>().Single();
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
            var block = documentNode.FindDescendantNodes<MarkupBlockIntermediateNode>().Single();
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
            Assert.Empty(documentNode.FindDescendantNodes<MarkupBlockIntermediateNode>());
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
            Assert.Empty(documentNode.FindDescendantNodes<MarkupBlockIntermediateNode>());
        }

        [Fact]
        public void Execute_CannotRewriteHtml_SelectOption()
        {
            // Arrange
            var document = CreateDocument(@"
<html>
  @if (some_bool)
  {
  <head cool=""beans"">
    <select>
        <option value='1'>One</option>
        <option selected value='2'>Two</option>
        <option value='3'>Three</option>
    </select>
  </head>
  }
</html>");

            var documentNode = Lower(document);

            // Act
            Pass.Execute(document, documentNode);

            // Assert
            Assert.Empty(documentNode.FindDescendantNodes<MarkupBlockIntermediateNode>());
        }

        [Fact]
        public void Execute_CanRewriteHtml_OptionWithNoSelectAncestor()
        {
            // Arrange
            var document = CreateDocument(@"
<html>
  @if (some_bool)
  {
  <head cool=""beans"">
    <option value='1'>One</option>
    <option selected value='2'>Two</option>
  </head>
  }
</html>");

            var expected = NormalizeContent(@"
<head cool=""beans""><option value=""1"">One</option>
    <option selected value=""2"">Two</option></head>");

            var documentNode = Lower(document);

            // Act
            Pass.Execute(document, documentNode);

            // Assert
            var block = documentNode.FindDescendantNodes<MarkupBlockIntermediateNode>().Single();
            Assert.Equal(expected, block.Content, ignoreLineEndingDifferences: true);
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
            Assert.Empty(documentNode.FindDescendantNodes<MarkupBlockIntermediateNode>());
        }

        // We want duplicate attributes to result in an error and prevent rewriting.
        //
        // This is because Blazor de-duplicates attributes differently from browsers, so we don't
        // want to allow any markup blocks to exist with duplicate attributes or else they will have
        // the browser's behavior.
        [Fact]
        public void Execute_CannotRewriteHtml_DuplicateAttribute()
        {
            // Arrange
            var document = CreateDocument(@"
<html>
  <a href=""test1"" href=""test2""></a>
</html>");

            var documentNode = Lower(document);

            // Act
            Pass.Execute(document, documentNode);

            // Assert
            Assert.Empty(documentNode.FindDescendantNodes<MarkupBlockIntermediateNode>());

            var diagnostic = Assert.Single(documentNode.GetAllDiagnostics());
            Assert.Same(ComponentDiagnosticFactory.DuplicateMarkupAttribute.Id, diagnostic.Id);
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
            var block = documentNode.FindDescendantNodes<MarkupBlockIntermediateNode>().Single();
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
            return ProjectEngine.CreateCodeDocumentCore(source, FileKinds.Component);
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
            Engine.Features.OfType<ComponentMarkupDiagnosticPass>().Single().Execute(codeDocument, document);
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