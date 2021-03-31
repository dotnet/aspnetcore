// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Razor.Language.Intermediate;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language.Components
{
    // These tests are really basic and cover a few different structures. The components part of this
    // pass is hard to unit test, so component cases are covered by integration tests.
    public class ComponentDuplicateAttributeDiagnosticPassTest
    {
        public ComponentDuplicateAttributeDiagnosticPassTest()
        {
            Pass = new ComponentMarkupDiagnosticPass();
            ProjectEngine = (DefaultRazorProjectEngine)RazorProjectEngine.Create(
                RazorConfiguration.Default,
                RazorProjectFileSystem.Create(Environment.CurrentDirectory),
                b =>
                {
                    // Don't run the markup mutating passes.
                    b.Features.Remove(b.Features.OfType<ComponentMarkupDiagnosticPass>().Single());
                    b.Features.Remove(b.Features.OfType<ComponentMarkupBlockPass>().Single());
                    b.Features.Remove(b.Features.OfType<ComponentMarkupEncodingPass>().Single());
                });
            Engine = ProjectEngine.Engine;

            Pass.Engine = Engine;
        }
        
        private DefaultRazorProjectEngine ProjectEngine { get; }

        private RazorEngine Engine { get; }

        private ComponentMarkupDiagnosticPass Pass { get; }

        [Fact]
        public void Execute_NoDuplicates()
        {
            // Arrange
            var document = CreateDocument(@"
<html>
  <head cool=""beans"">
    <div></div>
    <ul a=""d"" b="""" c>
      <li id=""15""></li>
    </ul>
  </head>
</html>");

            var documentNode = Lower(document);

            // Act
            Pass.Execute(document, documentNode);

            // Assert
            Assert.Empty(documentNode.GetAllDiagnostics());
        }

        [Fact]
        public void Execute_FindDuplicate()
        {
            // Arrange
            var document = CreateDocument(@"
<html>
  <head cool=""beans"">
    <div></div>
    <ul a=""d"" b="""" c a=""another"">
      <li id=""15""></li>
    </ul>
  </head>
</html>");

            var documentNode = Lower(document);

            // Act
            Pass.Execute(document, documentNode);

            // Assert
            var diagnostic = Assert.Single(documentNode.GetAllDiagnostics());
            Assert.Equal(ComponentDiagnosticFactory.DuplicateMarkupAttribute.Id, diagnostic.Id);

            var node = documentNode.FindDescendantNodes<HtmlAttributeIntermediateNode>().Where(n => n.HasDiagnostics).Single();
            Assert.Equal("a", node.AttributeName);
            Assert.Equal(node.Source, diagnostic.Span);
        }

        [Fact]
        public void Execute_FindDuplicate_CaseInsensitive()
        {
            // Arrange
            var document = CreateDocument(@"
<html>
  <head cool=""beans"">
    <div></div>
    <ul attr=""d"" b="""" c ATTR=""another"">
      <li id=""15""></li>
    </ul>
  </head>
</html>");

            var documentNode = Lower(document);

            // Act
            Pass.Execute(document, documentNode);

            // Assert
            var diagnostic = Assert.Single(documentNode.GetAllDiagnostics());
            Assert.Equal(ComponentDiagnosticFactory.DuplicateMarkupAttribute.Id, diagnostic.Id);

            var node = documentNode.FindDescendantNodes<HtmlAttributeIntermediateNode>().Where(n => n.HasDiagnostics).Single();
            Assert.Equal("attr", node.AttributeName);
            Assert.Equal(node.Source, diagnostic.Span);
        }

        [Fact]
        public void Execute_FindDuplicate_Multiple()
        {
            // Arrange
            var document = CreateDocument(@"
<html>
  <head cool=""beans"">
    <div></div>
    <ul attr=""d"" b="""" c attr=""another"" attr>
      <li id=""15""></li>
    </ul>
  </head>
</html>");

            var documentNode = Lower(document);

            // Act
            Pass.Execute(document, documentNode);

            // Assert
            var diagnostics = documentNode.GetAllDiagnostics();
            var nodes = documentNode.FindDescendantNodes<HtmlAttributeIntermediateNode>().Where(n => n.HasDiagnostics).ToArray();

            Assert.Equal(2, diagnostics.Count);
            Assert.Equal(2, nodes.Length);

            for (var i = 0; i < 2; i++)
            {
                var diagnostic = diagnostics[i];
                var node = nodes[i];

                Assert.Equal(ComponentDiagnosticFactory.DuplicateMarkupAttribute.Id, diagnostic.Id);
                Assert.Equal("attr", node.AttributeName);
                Assert.Equal(node.Source, diagnostic.Span);
            }
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