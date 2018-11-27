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
    public class ComponentDocumentRewritePassTest
    {
        public ComponentDocumentRewritePassTest()
        {
            var test = TagHelperDescriptorBuilder.Create("test", "test");
            test.TagMatchingRule(b => b.TagName = "test");

            TagHelpers = new List<TagHelperDescriptor>()
            {
                test.Build(),
            };

            Pass = new ComponentDocumentRewritePass();
            Engine = RazorProjectEngine.Create(
                BlazorExtensionInitializer.DefaultConfiguration,
                RazorProjectFileSystem.Create(Environment.CurrentDirectory), 
                b =>
                {
                    b.Features.Add(new ComponentDocumentClassifierPass());
                    b.Features.Add(Pass);
                    b.Features.Add(new StaticTagHelperFeature() { TagHelpers = TagHelpers, });
                }).Engine;
        }

        private RazorEngine Engine { get; }

        private ComponentDocumentRewritePass Pass { get; }

        private List<TagHelperDescriptor> TagHelpers { get; }

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

            var documentNode = Lower(document);

            // Act
            Pass.Execute(document, documentNode);

            // Assert
            var method = documentNode.FindPrimaryMethod();
            Assert.Collection(
                method.Children,
                c => Assert.IsType<CSharpCodeIntermediateNode>(c),
                c => NodeAssert.Whitespace(c),
                c => NodeAssert.Element(c, "html"));

            var html = NodeAssert.Element(method.Children[2], "html");
            Assert.Equal(2, html.Source.Value.AbsoluteIndex);
            Assert.Equal(1, html.Source.Value.LineIndex);
            Assert.Equal(0, html.Source.Value.CharacterIndex);
            Assert.Equal(68, html.Source.Value.Length);
            Assert.Collection(
                html.Children,
                c => NodeAssert.Whitespace(c),
                c => NodeAssert.Element(c, "head"),
                c => NodeAssert.Whitespace(c));

            var head = NodeAssert.Element(html.Children[1], "head");
            Assert.Equal(12, head.Source.Value.AbsoluteIndex);
            Assert.Equal(2, head.Source.Value.LineIndex);
            Assert.Equal(2, head.Source.Value.CharacterIndex);
            Assert.Equal(49, head.Source.Value.Length);
            Assert.Collection(
                head.Children,
                c => NodeAssert.Attribute(c, "cool", "beans"),
                c => NodeAssert.Content(c, "Hello, World!"));
        }

        [Fact]
        public void Execute_RewritesHtml_Mixed()
        {
            // Arrange
            var document = CreateDocument(@"
<html>
  <head cool=""beans"" csharp=""@yes"" mixed=""hi @there"">
  </head>
</html>");

            var documentNode = Lower(document);

            // Act
            Pass.Execute(document, documentNode);

            // Assert
            var method = documentNode.FindPrimaryMethod();
            Assert.Collection(
                method.Children,
                c => Assert.IsType<CSharpCodeIntermediateNode>(c),
                c => NodeAssert.Whitespace(c),
                c => NodeAssert.Element(c, "html"));

            var html = NodeAssert.Element(method.Children[2], "html");
            Assert.Equal(2, html.Source.Value.AbsoluteIndex);
            Assert.Equal(1, html.Source.Value.LineIndex);
            Assert.Equal(0, html.Source.Value.CharacterIndex);
            Assert.Equal(81, html.Source.Value.Length);
            Assert.Collection(
                html.Children,
                c => NodeAssert.Whitespace(c),
                c => NodeAssert.Element(c, "head"),
                c => NodeAssert.Whitespace(c));

            var head = NodeAssert.Element(html.Children[1], "head");
            Assert.Equal(12, head.Source.Value.AbsoluteIndex);
            Assert.Equal(2, head.Source.Value.LineIndex);
            Assert.Equal(2, head.Source.Value.CharacterIndex);
            Assert.Equal(62, head.Source.Value.Length);
            Assert.Collection(
                head.Children,
                c => NodeAssert.Attribute(c, "cool", "beans"),
                c => NodeAssert.CSharpAttribute(c, "csharp", "yes"),
                c => Assert.IsType<HtmlAttributeIntermediateNode>(c),
                c => NodeAssert.Whitespace(c));

            var mixed = Assert.IsType<HtmlAttributeIntermediateNode>(head.Children[2]);
            Assert.Collection(
                mixed.Children,
                c => Assert.IsType<HtmlAttributeValueIntermediateNode>(c),
                c => Assert.IsType<CSharpExpressionAttributeValueIntermediateNode>(c));
        }

        [Fact]
        public void Execute_RewritesHtml_WithCode()
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
            var method = documentNode.FindPrimaryMethod();
            Assert.Collection(
                method.Children,
                c => Assert.IsType<CSharpCodeIntermediateNode>(c),
                c => NodeAssert.Whitespace(c),
                c => NodeAssert.Element(c, "html"));

            var html = NodeAssert.Element(method.Children[2], "html");
            Assert.Equal(2, html.Source.Value.AbsoluteIndex);
            Assert.Equal(1, html.Source.Value.LineIndex);
            Assert.Equal(0, html.Source.Value.CharacterIndex);
            Assert.Equal(90, html.Source.Value.Length);
            Assert.Collection(
                html.Children,
                c => NodeAssert.Whitespace(c),
                c => Assert.IsType<CSharpCodeIntermediateNode>(c),
                c => Assert.IsType<CSharpCodeIntermediateNode>(c),
                c => NodeAssert.Whitespace(c),
                c => NodeAssert.Element(c, "head"),
                c => NodeAssert.Whitespace(c),
                c => Assert.IsType<CSharpCodeIntermediateNode>(c));

            var head = NodeAssert.Element(html.Children[4], "head");
            Assert.Equal(36, head.Source.Value.AbsoluteIndex);
            Assert.Equal(4, head.Source.Value.LineIndex);
            Assert.Equal(2, head.Source.Value.CharacterIndex);
            Assert.Equal(42, head.Source.Value.Length);
            Assert.Collection(
                head.Children,
                c => NodeAssert.Attribute(c, "cool", "beans"),
                c => NodeAssert.Whitespace(c),
                c => Assert.IsType<CSharpExpressionIntermediateNode>(c),
                c => NodeAssert.Whitespace(c));
        }

        [Fact]
        public void Execute_RewritesHtml_TagHelper()
        {
            // Arrange
            var document = CreateDocument(@"
@addTagHelper ""*, test""
<html>
  <test>
    <head cool=""beans"">
      Hello, World!
    </head>
  </test>
</html>");

            var documentNode = Lower(document);

            // Act
            Pass.Execute(document, documentNode);

            // Assert
            var method = documentNode.FindPrimaryMethod();
            Assert.Collection(
                method.Children,
                c => Assert.IsType<CSharpCodeIntermediateNode>(c),
                c => NodeAssert.Whitespace(c),
                c => Assert.IsType<DirectiveIntermediateNode>(c),
                c => NodeAssert.Element(c, "html"));

            var html = NodeAssert.Element(method.Children[3], "html");
            Assert.Equal(27, html.Source.Value.AbsoluteIndex);
            Assert.Equal(2, html.Source.Value.LineIndex);
            Assert.Equal(0, html.Source.Value.CharacterIndex);
            Assert.Equal(95, html.Source.Value.Length);
            Assert.Collection(
                html.Children,
                c => NodeAssert.Whitespace(c),
                c => Assert.IsType<TagHelperIntermediateNode>(c),
                c => NodeAssert.Whitespace(c));

            var body = html.Children
                .OfType<TagHelperIntermediateNode>().Single().Children
                .OfType<TagHelperBodyIntermediateNode>().Single();

            Assert.Collection(
                body.Children,
                c => NodeAssert.Whitespace(c),
                c => NodeAssert.Element(c, "head"),
                c => NodeAssert.Whitespace(c));

            var head = body.Children[1];
            Assert.Equal(49, head.Source.Value.AbsoluteIndex);
            Assert.Equal(4, head.Source.Value.LineIndex);
            Assert.Equal(4, head.Source.Value.CharacterIndex);
            Assert.Equal(53, head.Source.Value.Length);
            Assert.Collection(
                head.Children,
                c => NodeAssert.Attribute(c, "cool", "beans"),
                c => NodeAssert.Content(c, "Hello, World!"));
        }

        [Fact]
        public void Execute_RewritesHtml_UnbalancedClosing_MisuseOfVoidElement()
        {
            // Arrange
            var document = CreateDocument(@"<input></input>");

            var documentNode = Lower(document);

            // Act
            Pass.Execute(document, documentNode);

            // Assert
            var method = documentNode.FindPrimaryMethod();
            Assert.Collection(
                method.Children,
                c => Assert.IsType<CSharpCodeIntermediateNode>(c),
                c => NodeAssert.Element(c, "input"),
                c => NodeAssert.Element(c, "input"));

            var input2 = NodeAssert.Element(method.Children[2], "input");
            Assert.Equal(7, input2.Source.Value.AbsoluteIndex);
            Assert.Equal(0, input2.Source.Value.LineIndex);
            Assert.Equal(7, input2.Source.Value.CharacterIndex);
            Assert.Equal(8, input2.Source.Value.Length);

            var diagnostic = Assert.Single(input2.Diagnostics);
            Assert.Same(BlazorDiagnosticFactory.UnexpectedClosingTagForVoidElement.Id, diagnostic.Id);
            Assert.Equal(input2.Source, diagnostic.Span);
        }

        [Fact]
        public void Execute_RewritesHtml_UnbalancedClosingTagAtTopLevel()
        {
            // Arrange
            var document = CreateDocument(@"
</html>");

            var documentNode = Lower(document);

            // Act
            Pass.Execute(document, documentNode);

            // Assert
            var method = documentNode.FindPrimaryMethod();
            Assert.Collection(
                method.Children,
                c => Assert.IsType<CSharpCodeIntermediateNode>(c),
                c => NodeAssert.Whitespace(c),
                c => NodeAssert.Element(c, "html"));

            var html = NodeAssert.Element(method.Children[2], "html");
            Assert.Equal(2, html.Source.Value.AbsoluteIndex);
            Assert.Equal(1, html.Source.Value.LineIndex);
            Assert.Equal(0, html.Source.Value.CharacterIndex);
            Assert.Equal(7, html.Source.Value.Length);

            var diagnostic = Assert.Single(html.Diagnostics);
            Assert.Same(BlazorDiagnosticFactory.UnexpectedClosingTag.Id, diagnostic.Id);
            Assert.Equal(html.Source, diagnostic.Span);
        }

        [Fact]
        public void Execute_RewritesHtml_MismatchedClosingTag()
        {
            // Arrange
            var document = CreateDocument(@"
<html>
  <div>
  </span>
</html>");

            var documentNode = Lower(document);

            // Act
            Pass.Execute(document, documentNode);

            // Assert
            var method = documentNode.FindPrimaryMethod();
            Assert.Collection(
                method.Children,
                c => Assert.IsType<CSharpCodeIntermediateNode>(c),
                c => NodeAssert.Whitespace(c),
                c => NodeAssert.Element(c, "html"));

            var html = NodeAssert.Element(method.Children[2], "html");
            Assert.Collection(
                html.Children,
                c => NodeAssert.Whitespace(c),
                c => NodeAssert.Element(c, "div"),
                c => NodeAssert.Whitespace(c));

            var div = NodeAssert.Element(html.Children[1], "div");
            Assert.Equal(12, div.Source.Value.AbsoluteIndex);
            Assert.Equal(2, div.Source.Value.LineIndex);
            Assert.Equal(2, div.Source.Value.CharacterIndex);
            Assert.Equal(5, div.Source.Value.Length);
            
            var diagnostic = Assert.Single(div.Diagnostics);
            Assert.Same(BlazorDiagnosticFactory.MismatchedClosingTag.Id, diagnostic.Id);
            Assert.Equal(21,diagnostic.Span.AbsoluteIndex);
            Assert.Equal(3, diagnostic.Span.LineIndex);
            Assert.Equal(2, diagnostic.Span.CharacterIndex);
            Assert.Equal(7, diagnostic.Span.Length);
        }

        [Fact]
        public void Execute_RewritesHtml_MalformedHtmlAtEnd()
        {
            // Arrange
            var document = CreateDocument(@"
<ht");

            var documentNode = Lower(document);

            // Act
            Pass.Execute(document, documentNode);

            // Assert
            var method = documentNode.FindPrimaryMethod();
            Assert.Collection(
                method.Children,
                c => Assert.IsType<CSharpCodeIntermediateNode>(c),
                c => NodeAssert.Whitespace(c),
                c => NodeAssert.Content(c, "<ht"));

            var content = NodeAssert.Content(method.Children[2], "<ht");
            var diagnostic = Assert.Single(content.Diagnostics);
            Assert.Same(BlazorDiagnosticFactory.InvalidHtmlContent.Id, diagnostic.Id);
            Assert.Equal(2, diagnostic.Span.AbsoluteIndex);
            Assert.Equal(1, diagnostic.Span.LineIndex);
            Assert.Equal(0, diagnostic.Span.CharacterIndex);
            Assert.Equal(3, diagnostic.Span.Length);
        }

        [Fact]
        public void Execute_RewritesHtml_UnclosedTags()
        {
            // Arrange
            var document = CreateDocument(@"
<html>
  <div>");

            var documentNode = Lower(document);

            // Act
            Pass.Execute(document, documentNode);

            // Assert
            var method = documentNode.FindPrimaryMethod();
            Assert.Collection(
                method.Children,
                c => Assert.IsType<CSharpCodeIntermediateNode>(c),
                c => NodeAssert.Whitespace(c),
                c => NodeAssert.Element(c, "html"));

            var html = NodeAssert.Element(method.Children[2], "html");
            Assert.Collection(
                html.Children,
                c => NodeAssert.Whitespace(c),
                c => NodeAssert.Element(c, "div"));

            var diagnostic = Assert.Single(html.Diagnostics);
            Assert.Same(BlazorDiagnosticFactory.UnclosedTag.Id, diagnostic.Id);
            Assert.Equal(2, diagnostic.Span.AbsoluteIndex);
            Assert.Equal(1, diagnostic.Span.LineIndex);
            Assert.Equal(0, diagnostic.Span.CharacterIndex);
            Assert.Equal(6, diagnostic.Span.Length);

            var div = NodeAssert.Element(html.Children[1], "div");

            diagnostic = Assert.Single(div.Diagnostics);
            Assert.Same(BlazorDiagnosticFactory.UnclosedTag.Id, diagnostic.Id);
            Assert.Equal(12, diagnostic.Span.AbsoluteIndex);
            Assert.Equal(2, diagnostic.Span.LineIndex);
            Assert.Equal(2, diagnostic.Span.CharacterIndex);
            Assert.Equal(5, diagnostic.Span.Length);
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
                if (phase is IRazorDocumentClassifierPhase)
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