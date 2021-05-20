// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Razor.Language.CodeGeneration;
using Microsoft.AspNetCore.Razor.Language.Intermediate;
using Microsoft.AspNetCore.Testing;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language
{
    public class DefaultRazorCSharpLoweringPhaseTest
    {
        [Fact]
        public void Execute_ThrowsForMissingDependency_IRDocument()
        {
            // Arrange
            var phase = new DefaultRazorCSharpLoweringPhase();

            var engine = RazorProjectEngine.CreateEmpty(b => b.Phases.Add(phase));

            var codeDocument = TestRazorCodeDocument.CreateEmpty();

            codeDocument.SetSyntaxTree(RazorSyntaxTree.Parse(codeDocument.Source));

            // Act & Assert
            ExceptionAssert.Throws<InvalidOperationException>(
                () => phase.Execute(codeDocument),
                $"The '{nameof(DefaultRazorCSharpLoweringPhase)}' phase requires a '{nameof(DocumentIntermediateNode)}' " +
                $"provided by the '{nameof(RazorCodeDocument)}'.");
        }

        [Fact]
        public void Execute_ThrowsForMissingDependency_CodeTarget()
        {
            // Arrange
            var phase = new DefaultRazorCSharpLoweringPhase();

            var engine = RazorProjectEngine.CreateEmpty(b => b.Phases.Add(phase));

            var codeDocument = TestRazorCodeDocument.CreateEmpty();

            codeDocument.SetSyntaxTree(RazorSyntaxTree.Parse(codeDocument.Source));

            var irDocument = new DocumentIntermediateNode()
            {
                DocumentKind = "test",
            };
            codeDocument.SetDocumentIntermediateNode(irDocument);

            // Act & Assert
            ExceptionAssert.Throws<InvalidOperationException>(
                () => phase.Execute(codeDocument),
                $"The document of kind 'test' does not have a '{nameof(CodeTarget)}'. " +
                $"The document classifier must set a value for '{nameof(DocumentIntermediateNode.Target)}'.");
        }

        [Fact]
        public void Execute_CollatesIRDocumentDiagnosticsFromSourceDocument()
        {
            // Arrange
            var phase = new DefaultRazorCSharpLoweringPhase();
            var engine = RazorProjectEngine.CreateEmpty(b => b.Phases.Add(phase));
            var codeDocument = TestRazorCodeDocument.Create("<p class=@(");
            var options = RazorCodeGenerationOptions.CreateDefault();
            var irDocument = new DocumentIntermediateNode()
            {
                DocumentKind = "test",
                Target = CodeTarget.CreateDefault(codeDocument, options),
                Options = options,
            };
            var expectedDiagnostic = RazorDiagnostic.Create(
                    new RazorDiagnosticDescriptor("1234", () => "I am an error.", RazorDiagnosticSeverity.Error),
                    new SourceSpan("SomeFile.cshtml", 11, 0, 11, 1));
            irDocument.Diagnostics.Add(expectedDiagnostic);
            codeDocument.SetDocumentIntermediateNode(irDocument);

            // Act
            phase.Execute(codeDocument);

            // Assert
            var csharpDocument = codeDocument.GetCSharpDocument();
            var diagnostic = Assert.Single(csharpDocument.Diagnostics);
            Assert.Same(expectedDiagnostic, diagnostic);
        }
    }
}
