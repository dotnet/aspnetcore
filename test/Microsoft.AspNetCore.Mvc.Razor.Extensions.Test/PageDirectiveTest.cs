// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Intermediate;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.Razor.Extensions
{
    public class PageDirectiveTest
    {
        [Fact]
        public void TryGetPageDirective_ReturnsTrue_IfPageIsMalformed()
        {
            // Arrange
            var content = "@page \"some-route-template\" Invalid";
            var sourceDocument = RazorSourceDocument.Create(content, "file");
            var codeDocument = RazorCodeDocument.Create(sourceDocument);
            var engine = CreateEngine();
            var irDocument = CreateIRDocument(engine, codeDocument);

            // Act
            var result = PageDirective.TryGetPageDirective(irDocument, out var pageDirective);

            // Assert
            Assert.True(result);
            Assert.Equal("some-route-template", pageDirective.RouteTemplate);
            Assert.NotNull(pageDirective.DirectiveNode);
        }

        [Fact]
        public void TryGetPageDirective_ReturnsTrue_IfPageIsImported()
        {
            // Arrange
            var content = "Hello world";
            var sourceDocument = RazorSourceDocument.Create(content, "file");
            var importDocument = RazorSourceDocument.Create("@page", "imports.cshtml");
            var codeDocument = RazorCodeDocument.Create(sourceDocument, new[] { importDocument });
            var engine = CreateEngine();
            var irDocument = CreateIRDocument(engine, codeDocument);

            // Act
            var result = PageDirective.TryGetPageDirective(irDocument, out var pageDirective);

            // Assert
            Assert.True(result);
            Assert.Null(pageDirective.RouteTemplate);
        }

        [Fact]
        public void TryGetPageDirective_ReturnsFalse_IfPageDoesNotHaveDirective()
        {
            // Arrange
            var content = "Hello world";
            var sourceDocument = RazorSourceDocument.Create(content, "file");
            var codeDocument = RazorCodeDocument.Create(sourceDocument);
            var engine = CreateEngine();
            var irDocument = CreateIRDocument(engine, codeDocument);

            // Act
            var result = PageDirective.TryGetPageDirective(irDocument, out var pageDirective);

            // Assert
            Assert.False(result);
            Assert.Null(pageDirective);
        }

        [Fact]
        public void TryGetPageDirective_ReturnsTrue_IfPageDoesStartWithDirective()
        {
            // Arrange
            var content = "Hello @page";
            var sourceDocument = RazorSourceDocument.Create(content, "file");
            var codeDocument = RazorCodeDocument.Create(sourceDocument);
            var engine = CreateEngine();
            var irDocument = CreateIRDocument(engine, codeDocument);

            // Act
            var result = PageDirective.TryGetPageDirective(irDocument, out var pageDirective);

            // Assert
            Assert.True(result);
            Assert.Null(pageDirective.RouteTemplate);
            Assert.NotNull(pageDirective.DirectiveNode);
        }

        [Fact]
        public void TryGetPageDirective_ReturnsTrue_IfContentHasDirective()
        {
            // Arrange
            var content = "@page";
            var sourceDocument = RazorSourceDocument.Create(content, "file");
            var codeDocument = RazorCodeDocument.Create(sourceDocument);
            var engine = CreateEngine();
            var irDocument = CreateIRDocument(engine, codeDocument);

            // Act
            var result = PageDirective.TryGetPageDirective(irDocument, out var pageDirective);

            // Assert
            Assert.True(result);
            Assert.Null(pageDirective.RouteTemplate);
        }

        [Fact]
        public void TryGetPageDirective_ParsesRouteTemplate()
        {
            // Arrange
            var content = "@page \"some-route-template\"";
            var sourceDocument = RazorSourceDocument.Create(content, "file");
            var codeDocument = RazorCodeDocument.Create(sourceDocument);
            var engine = CreateEngine();
            var irDocument = CreateIRDocument(engine, codeDocument);

            // Act
            var result = PageDirective.TryGetPageDirective(irDocument, out var pageDirective);

            // Assert
            Assert.True(result);
            Assert.Equal("some-route-template", pageDirective.RouteTemplate);
        }

        private RazorEngine CreateEngine()
        {
            return RazorEngine.Create(b =>
            {
                PageDirective.Register(b);
            });
        }

        private DocumentIntermediateNode CreateIRDocument(RazorEngine engine, RazorCodeDocument codeDocument)
        {
            for (var i = 0; i < engine.Phases.Count; i++)
            {
                var phase = engine.Phases[i];
                phase.Execute(codeDocument);

                if (phase is IRazorDocumentClassifierPhase)
                {
                    break;
                }
            }

            return codeDocument.GetDocumentIntermediateNode();
        }
    }
}
