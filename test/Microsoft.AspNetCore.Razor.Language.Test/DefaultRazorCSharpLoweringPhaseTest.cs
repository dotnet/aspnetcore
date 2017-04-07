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

            var engine = RazorEngine.CreateEmpty(b => b.Phases.Add(phase));

            var codeDocument = TestRazorCodeDocument.CreateEmpty();

            codeDocument.SetSyntaxTree(RazorSyntaxTree.Parse(codeDocument.Source));

            // Act & Assert
            ExceptionAssert.Throws<InvalidOperationException>(
                () => phase.Execute(codeDocument),
                $"The '{nameof(DefaultRazorCSharpLoweringPhase)}' phase requires a '{nameof(DocumentIRNode)}' " +
                $"provided by the '{nameof(RazorCodeDocument)}'.");
        }

        [Fact]
        public void Execute_ThrowsForMissingDependency_SyntaxTree()
        {
            // Arrange
            var phase = new DefaultRazorCSharpLoweringPhase();

            var engine = RazorEngine.CreateEmpty(b => b.Phases.Add(phase));

            var codeDocument = TestRazorCodeDocument.CreateEmpty();

            var irDocument = new DocumentIRNode();
            codeDocument.SetIRDocument(irDocument);

            // Act & Assert
            ExceptionAssert.Throws<InvalidOperationException>(
                () => phase.Execute(codeDocument),
                $"The '{nameof(DefaultRazorCSharpLoweringPhase)}' phase requires a '{nameof(RazorSyntaxTree)}' " +
                $"provided by the '{nameof(RazorCodeDocument)}'.");
        }

        [Fact]
        public void Execute_ThrowsForMissingDependency_RuntimeTarget()
        {
            // Arrange
            var phase = new DefaultRazorCSharpLoweringPhase();

            var engine = RazorEngine.CreateEmpty(b => b.Phases.Add(phase));

            var codeDocument = TestRazorCodeDocument.CreateEmpty();

            codeDocument.SetSyntaxTree(RazorSyntaxTree.Parse(codeDocument.Source));

            var irDocument = new DocumentIRNode()
            {
                DocumentKind = "test",
            };
            codeDocument.SetIRDocument(irDocument);

            // Act & Assert
            ExceptionAssert.Throws<InvalidOperationException>(
                () => phase.Execute(codeDocument),
                $"The document of kind 'test' does not have a '{nameof(RuntimeTarget)}'. " +
                $"The document classifier must set a value for '{nameof(DocumentIRNode.Target)}'.");
        }

        [Fact]
        public void Execute_CollatesSyntaxDiagnosticsFromSourceDocument()
        {
            // Arrange
            var phase = new DefaultRazorCSharpLoweringPhase();
            var engine = RazorEngine.CreateEmpty(b => b.Phases.Add(phase));
            var codeDocument = TestRazorCodeDocument.Create("<p class=@(");
            codeDocument.SetSyntaxTree(RazorSyntaxTree.Parse(codeDocument.Source));
            var options = RazorParserOptions.CreateDefaultOptions();
            var irDocument = new DocumentIRNode()
            {
                DocumentKind = "test",
                Target = RuntimeTarget.CreateDefault(codeDocument, options),
                Options = options,
            };
            codeDocument.SetIRDocument(irDocument);

            // Act
            phase.Execute(codeDocument);

            // Assert
            var csharpDocument = codeDocument.GetCSharpDocument();
            var diagnostic = Assert.Single(csharpDocument.Diagnostics);
            Assert.Equal(@"The explicit expression block is missing a closing "")"" character.  Make sure you have a matching "")"" character for all the ""("" characters within this block, and that none of the "")"" characters are being interpreted as markup.",
                diagnostic.GetMessage());
        }

        [Fact]
        public void Execute_CollatesSyntaxDiagnosticsFromImportDocuments()
        {
            // Arrange
            var phase = new DefaultRazorCSharpLoweringPhase();
            var engine = RazorEngine.CreateEmpty(b => b.Phases.Add(phase));

            var codeDocument = TestRazorCodeDocument.CreateEmpty();
            codeDocument.SetSyntaxTree(RazorSyntaxTree.Parse(codeDocument.Source));
            codeDocument.SetImportSyntaxTrees(new[]
            {
                RazorSyntaxTree.Parse(TestRazorSourceDocument.Create("@ ")),
                RazorSyntaxTree.Parse(TestRazorSourceDocument.Create("<p @(")),
            });
            var options = RazorParserOptions.CreateDefaultOptions();
            var irDocument = new DocumentIRNode()
            {
                DocumentKind = "test",
                Target = RuntimeTarget.CreateDefault(codeDocument, options),
                Options = options,
            };
            codeDocument.SetIRDocument(irDocument);

            // Act
            phase.Execute(codeDocument);

            // Assert
            var csharpDocument = codeDocument.GetCSharpDocument();
            Assert.Collection(csharpDocument.Diagnostics,
                diagnostic =>
                {
                    Assert.Equal(@"A space or line break was encountered after the ""@"" character.  Only valid identifiers, keywords, comments, ""("" and ""{"" are valid at the start of a code block and they must occur immediately following ""@"" with no space in between.",
                        diagnostic.GetMessage());
                },
                diagnostic =>
                {
                    Assert.Equal(@"The explicit expression block is missing a closing "")"" character.  Make sure you have a matching "")"" character for all the ""("" characters within this block, and that none of the "")"" characters are being interpreted as markup.",
                        diagnostic.GetMessage());
                });
        }
    }
}
