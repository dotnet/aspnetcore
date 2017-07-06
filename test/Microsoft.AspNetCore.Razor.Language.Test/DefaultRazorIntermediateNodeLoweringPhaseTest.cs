// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.AspNetCore.Razor.Language.Intermediate;
using Microsoft.AspNetCore.Testing;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language
{
    public class DefaultRazorIntermediateNodeLoweringPhaseTest
    {
        [Fact]
        public void Execute_AutomaticallyImportsSingleLineSinglyOccurringDirective()
        {
            // Arrange
            var directive = DirectiveDescriptor.CreateSingleLineDirective(
                "custom",
                builder =>
                {
                    builder.AddStringToken();
                    builder.Usage = DirectiveUsage.FileScopedSinglyOccurring;
                });
            var phase = new DefaultRazorIntermediateNodeLoweringPhase();
            var engine = RazorEngine.CreateEmpty(b =>
            {
                b.Phases.Add(phase);
                b.AddDirective(directive);
            });
            var options = RazorParserOptions.Create(builder => builder.Directives.Add(directive));
            var importSource = TestRazorSourceDocument.Create("@custom \"hello\"", fileName: "import.cshtml");
            var codeDocument = TestRazorCodeDocument.Create("<p>NonDirective</p>");
            codeDocument.SetSyntaxTree(RazorSyntaxTree.Parse(codeDocument.Source, options));
            codeDocument.SetImportSyntaxTrees(new[] { RazorSyntaxTree.Parse(importSource, options) });

            // Act
            phase.Execute(codeDocument);

            // Assert
            var documentNode = codeDocument.GetDocumentIntermediateNode();
            var customDirectives = documentNode.FindDirectiveReferences(directive);
            var customDirective = (DirectiveIntermediateNode)Assert.Single(customDirectives).Node;
            var stringToken = Assert.Single(customDirective.Tokens);
            Assert.Equal("\"hello\"", stringToken.Content);
        }

        [Fact]
        public void Execute_AutomaticallyOverridesImportedSingleLineSinglyOccurringDirective_MainDocument()
        {
            // Arrange
            var directive = DirectiveDescriptor.CreateSingleLineDirective(
                "custom",
                builder =>
                {
                    builder.AddStringToken();
                    builder.Usage = DirectiveUsage.FileScopedSinglyOccurring;
                });
            var phase = new DefaultRazorIntermediateNodeLoweringPhase();
            var engine = RazorEngine.CreateEmpty(b =>
            {
                b.Phases.Add(phase);
                b.AddDirective(directive);
            });
            var options = RazorParserOptions.Create(builder => builder.Directives.Add(directive));
            var importSource = TestRazorSourceDocument.Create("@custom \"hello\"", fileName: "import.cshtml");
            var codeDocument = TestRazorCodeDocument.Create("@custom \"world\"");
            codeDocument.SetSyntaxTree(RazorSyntaxTree.Parse(codeDocument.Source, options));
            codeDocument.SetImportSyntaxTrees(new[] { RazorSyntaxTree.Parse(importSource, options) });

            // Act
            phase.Execute(codeDocument);

            // Assert
            var documentNode = codeDocument.GetDocumentIntermediateNode();
            var customDirectives = documentNode.FindDirectiveReferences(directive);
            var customDirective = (DirectiveIntermediateNode)Assert.Single(customDirectives).Node;
            var stringToken = Assert.Single(customDirective.Tokens);
            Assert.Equal("\"world\"", stringToken.Content);
        }

        [Fact]
        public void Execute_AutomaticallyOverridesImportedSingleLineSinglyOccurringDirective_MultipleImports()
        {
            // Arrange
            var directive = DirectiveDescriptor.CreateSingleLineDirective(
                "custom",
                builder =>
                {
                    builder.AddStringToken();
                    builder.Usage = DirectiveUsage.FileScopedSinglyOccurring;
                });
            var phase = new DefaultRazorIntermediateNodeLoweringPhase();
            var engine = RazorEngine.CreateEmpty(b =>
            {
                b.Phases.Add(phase);
                b.AddDirective(directive);
            });
            var options = RazorParserOptions.Create(builder => builder.Directives.Add(directive));
            var importSource1 = TestRazorSourceDocument.Create("@custom \"hello\"", fileName: "import1.cshtml");
            var importSource2 = TestRazorSourceDocument.Create("@custom \"world\"", fileName: "import2.cshtml");
            var codeDocument = TestRazorCodeDocument.Create("<p>NonDirective</p>");
            codeDocument.SetSyntaxTree(RazorSyntaxTree.Parse(codeDocument.Source, options));
            codeDocument.SetImportSyntaxTrees(new[] { RazorSyntaxTree.Parse(importSource1, options), RazorSyntaxTree.Parse(importSource2, options) });

            // Act
            phase.Execute(codeDocument);

            // Assert
            var documentNode = codeDocument.GetDocumentIntermediateNode();
            var customDirectives = documentNode.FindDirectiveReferences(directive);
            var customDirective = (DirectiveIntermediateNode)Assert.Single(customDirectives).Node;
            var stringToken = Assert.Single(customDirective.Tokens);
            Assert.Equal("\"world\"", stringToken.Content);
        }

        [Fact]
        public void Execute_DoesNotImportNonFileScopedSinglyOccurringDirectives_Block()
        {
            // Arrange
            var codeBlockDirective = DirectiveDescriptor.CreateCodeBlockDirective("code", b => b.AddStringToken());
            var razorBlockDirective = DirectiveDescriptor.CreateRazorBlockDirective("razor", b => b.AddStringToken());
            var phase = new DefaultRazorIntermediateNodeLoweringPhase();
            var engine = RazorEngine.CreateEmpty(b =>
            {
                b.Phases.Add(phase);
                b.AddDirective(codeBlockDirective);
                b.AddDirective(razorBlockDirective);
            });
            var options = RazorParserOptions.Create(builder =>
            {
                builder.Directives.Add(codeBlockDirective);
                builder.Directives.Add(razorBlockDirective); 
            });
            var importSource = TestRazorSourceDocument.Create(
@"@code ""code block"" { }
@razor ""razor block"" { }",
                fileName: "testImports.cshtml");
            var codeDocument = TestRazorCodeDocument.Create("<p>NonDirective</p>");
            codeDocument.SetSyntaxTree(RazorSyntaxTree.Parse(codeDocument.Source, options));
            codeDocument.SetImportSyntaxTrees(new[] { RazorSyntaxTree.Parse(importSource, options) });

            // Act
            phase.Execute(codeDocument);

            // Assert
            var documentNode = codeDocument.GetDocumentIntermediateNode();
            var directives = documentNode.Children.OfType<DirectiveIntermediateNode>();
            Assert.Empty(directives);
        }

        [Fact]
        public void Execute_ErrorsForCodeBlockFileScopedSinglyOccurringDirectives()
        {
            // Arrange
            var directive = DirectiveDescriptor.CreateCodeBlockDirective("custom", b => b.Usage = DirectiveUsage.FileScopedSinglyOccurring);
            var phase = new DefaultRazorIntermediateNodeLoweringPhase();
            var engine = RazorEngine.CreateEmpty(b =>
            {
                b.Phases.Add(phase);
                b.AddDirective(directive);
            });
            var options = RazorParserOptions.Create(builder => builder.Directives.Add(directive));
            var importSource = TestRazorSourceDocument.Create("@custom { }", fileName: "import.cshtml");
            var codeDocument = TestRazorCodeDocument.Create("<p>NonDirective</p>");
            codeDocument.SetSyntaxTree(RazorSyntaxTree.Parse(codeDocument.Source, options));
            codeDocument.SetImportSyntaxTrees(new[] { RazorSyntaxTree.Parse(importSource, options) });
            var expectedDiagnostic = RazorDiagnosticFactory.CreateDirective_BlockDirectiveCannotBeImported("custom");

            // Act
            phase.Execute(codeDocument);

            // Assert
            var documentNode = codeDocument.GetDocumentIntermediateNode();
            var directives = documentNode.Children.OfType<DirectiveIntermediateNode>();
            Assert.Empty(directives);
            var diagnostic = Assert.Single(documentNode.GetAllDiagnostics());
            Assert.Equal(expectedDiagnostic, diagnostic);
        }

        [Fact]
        public void Execute_ErrorsForRazorBlockFileScopedSinglyOccurringDirectives()
        {
            // Arrange
            var directive = DirectiveDescriptor.CreateRazorBlockDirective("custom", b => b.Usage = DirectiveUsage.FileScopedSinglyOccurring);
            var phase = new DefaultRazorIntermediateNodeLoweringPhase();
            var engine = RazorEngine.CreateEmpty(b =>
            {
                b.Phases.Add(phase);
                b.AddDirective(directive);
            });
            var options = RazorParserOptions.Create(builder => builder.Directives.Add(directive));
            var importSource = TestRazorSourceDocument.Create("@custom { }", fileName: "import.cshtml");
            var codeDocument = TestRazorCodeDocument.Create("<p>NonDirective</p>");
            codeDocument.SetSyntaxTree(RazorSyntaxTree.Parse(codeDocument.Source, options));
            codeDocument.SetImportSyntaxTrees(new[] { RazorSyntaxTree.Parse(importSource, options) });
            var expectedDiagnostic = RazorDiagnosticFactory.CreateDirective_BlockDirectiveCannotBeImported("custom");

            // Act
            phase.Execute(codeDocument);

            // Assert
            var documentNode = codeDocument.GetDocumentIntermediateNode();
            var directives = documentNode.Children.OfType<DirectiveIntermediateNode>();
            Assert.Empty(directives);
            var diagnostic = Assert.Single(documentNode.GetAllDiagnostics());
            Assert.Equal(expectedDiagnostic, diagnostic);
        }

        [Fact]
        public void Execute_ThrowsForMissingDependency_SyntaxTree()
        {
            // Arrange
            var phase = new DefaultRazorIntermediateNodeLoweringPhase();

            var engine = RazorEngine.CreateEmpty(b => b.Phases.Add(phase));

            var codeDocument = TestRazorCodeDocument.CreateEmpty();

            // Act & Assert
            ExceptionAssert.Throws<InvalidOperationException>(
                () => phase.Execute(codeDocument),
                $"The '{nameof(DefaultRazorIntermediateNodeLoweringPhase)}' phase requires a '{nameof(RazorSyntaxTree)}' " +
                $"provided by the '{nameof(RazorCodeDocument)}'.");
        }

        [Fact]
        public void Execute_CollatesSyntaxDiagnosticsFromSourceDocument()
        {
            // Arrange
            var phase = new DefaultRazorIntermediateNodeLoweringPhase();
            var engine = RazorEngine.CreateEmpty(b => b.Phases.Add(phase));
            var codeDocument = TestRazorCodeDocument.Create("<p class=@(");
            codeDocument.SetSyntaxTree(RazorSyntaxTree.Parse(codeDocument.Source));

            // Act
            phase.Execute(codeDocument);

            // Assert
            var documentNode = codeDocument.GetDocumentIntermediateNode();
            var diagnostic = Assert.Single(documentNode.Diagnostics);
            Assert.Equal(@"The explicit expression block is missing a closing "")"" character.  Make sure you have a matching "")"" character for all the ""("" characters within this block, and that none of the "")"" characters are being interpreted as markup.",
                diagnostic.GetMessage());
        }

        [Fact]
        public void Execute_CollatesSyntaxDiagnosticsFromImportDocuments()
        {
            // Arrange
            var phase = new DefaultRazorIntermediateNodeLoweringPhase();
            var engine = RazorEngine.CreateEmpty(b => b.Phases.Add(phase));

            var codeDocument = TestRazorCodeDocument.CreateEmpty();
            codeDocument.SetSyntaxTree(RazorSyntaxTree.Parse(codeDocument.Source));
            codeDocument.SetImportSyntaxTrees(new[]
            {
                RazorSyntaxTree.Parse(TestRazorSourceDocument.Create("@ ")),
                RazorSyntaxTree.Parse(TestRazorSourceDocument.Create("<p @(")),
            });
            var options = RazorCodeGenerationOptions.CreateDefault();

            // Act
            phase.Execute(codeDocument);

            // Assert
            var documentNode = codeDocument.GetDocumentIntermediateNode();
            Assert.Collection(documentNode.Diagnostics,
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
