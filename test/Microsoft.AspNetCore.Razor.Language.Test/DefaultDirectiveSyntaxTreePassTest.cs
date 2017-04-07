// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.AspNetCore.Razor.Language.Legacy;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language
{
    public class DefaultDirectiveSyntaxTreePassTest
    {
        [Fact]
        public void Execute_DoesNotRecreateSyntaxTreeWhenNoErrors()
        {
            // Arrange
            var engine = RazorEngine.Create();
            var pass = new DefaultDirectiveSyntaxTreePass()
            {
                Engine = engine,
            };
            var content =
            @"
@section Foo {
}";
            var sourceDocument = TestRazorSourceDocument.Create(content);
            var codeDocument = RazorCodeDocument.Create(sourceDocument);
            var originalTree = RazorSyntaxTree.Parse(sourceDocument);

            // Act
            var outputTree = pass.Execute(codeDocument, originalTree);

            // Assert
            Assert.Empty(originalTree.Diagnostics);
            Assert.Same(originalTree, outputTree);
            Assert.Empty(outputTree.Diagnostics);
        }

        [Fact]
        public void Execute_LogsErrorsForNestedSections()
        {
            // Arrange
            var expectedErrors = new[] {
                new RazorError(
                    LegacyResources.FormatParseError_Sections_Cannot_Be_Nested(LegacyResources.SectionExample_CS),
                    new SourceLocation(18 + Environment.NewLine.Length * 2, 2, 4),
                    length: 8),
                new RazorError(
                    LegacyResources.FormatParseError_Sections_Cannot_Be_Nested(LegacyResources.SectionExample_CS),
                    new SourceLocation(41 + Environment.NewLine.Length * 4, 4, 4),
                    length: 8),
            };
            var expectedDiagnostics = expectedErrors.Select(error => RazorDiagnostic.Create(error));
            var engine = RazorEngine.Create();
            var pass = new DefaultDirectiveSyntaxTreePass()
            {
                Engine = engine,
            };
            var content =
            @"
@section Foo {
    @section Bar {
    }
    @section Baz {
    }
}";
            var sourceDocument = TestRazorSourceDocument.Create(content, fileName: null);
            var codeDocument = RazorCodeDocument.Create(sourceDocument);
            var originalTree = RazorSyntaxTree.Parse(sourceDocument);

            // Act
            var outputTree = pass.Execute(codeDocument, originalTree);

            // Assert
            Assert.Empty(originalTree.Diagnostics);
            Assert.NotSame(originalTree, outputTree);
            Assert.Equal(expectedDiagnostics, outputTree.Diagnostics);
        }

        [Fact]
        public void Execute_CombinesErrorsWhenNestedSections()
        {
            // Arrange
            var expectedErrors = new[] {
                new RazorError("Test Error", SourceLocation.Zero, 3),
                new RazorError(
                    LegacyResources.FormatParseError_Sections_Cannot_Be_Nested(LegacyResources.SectionExample_CS),
                    new SourceLocation(18 + Environment.NewLine.Length * 2, 2, 4),
                    length: 8),
                new RazorError(
                    LegacyResources.FormatParseError_Sections_Cannot_Be_Nested(LegacyResources.SectionExample_CS),
                    new SourceLocation(41 + Environment.NewLine.Length * 4, 4, 4),
                    length: 8),
            };
            var expectedDiagnostics = expectedErrors.Select(error => RazorDiagnostic.Create(error)).ToList();
            var engine = RazorEngine.Create();
            var pass = new DefaultDirectiveSyntaxTreePass()
            {
                Engine = engine,
            };
            var content =
            @"
@section Foo {
    @section Bar {
    }
    @section Baz {
    }
}";
            var sourceDocument = TestRazorSourceDocument.Create(content, fileName: null);
            var codeDocument = RazorCodeDocument.Create(sourceDocument);
            var originalTree = RazorSyntaxTree.Parse(sourceDocument);
            var erroredOriginalTree = RazorSyntaxTree.Create(
                originalTree.Root, 
                originalTree.Source, 
                new[] { expectedDiagnostics[0] }, 
                originalTree.Options);

            // Act
            var outputTree = pass.Execute(codeDocument, erroredOriginalTree);

            // Assert
            Assert.Empty(originalTree.Diagnostics);
            Assert.NotSame(originalTree, outputTree);
            Assert.Equal(expectedDiagnostics, outputTree.Diagnostics);
        }
    }
}
