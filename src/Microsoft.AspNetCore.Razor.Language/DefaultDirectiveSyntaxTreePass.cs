// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.AspNetCore.Razor.Language.Legacy;

namespace Microsoft.AspNetCore.Razor.Language
{
    internal class DefaultDirectiveSyntaxTreePass : RazorEngineFeatureBase, IRazorSyntaxTreePass
    {
        public int Order => 75;

        public RazorSyntaxTree Execute(RazorCodeDocument codeDocument, RazorSyntaxTree syntaxTree)
        {
            if (codeDocument == null)
            {
                throw new ArgumentNullException(nameof(codeDocument));
            }

            if (syntaxTree == null)
            {
                throw new ArgumentNullException(nameof(syntaxTree));
            }

            var errorSink = new ErrorSink();
            var sectionVerifier = new NestedSectionVerifier();
            sectionVerifier.Verify(syntaxTree, errorSink);

            if (errorSink.Errors.Count > 0)
            {
                // Temporary code while we're still using legacy diagnostics in the SyntaxTree.
                var errors = errorSink.Errors.Select(error => RazorDiagnostic.Create(error));

                var combinedErrors = syntaxTree.Diagnostics.Concat(errors);
                syntaxTree = RazorSyntaxTree.Create(syntaxTree.Root, syntaxTree.Source, combinedErrors, syntaxTree.Options);
            }

            return syntaxTree;
        }

        private class NestedSectionVerifier : ParserVisitor
        {
            private int _nestedLevel;
            private ErrorSink _errorSink;

            public void Verify(RazorSyntaxTree tree, ErrorSink errorSink)
            {
                _errorSink = errorSink;
                tree.Root.Accept(this);
            }

            public override void VisitDirectiveBlock(DirectiveChunkGenerator chunkGenerator, Block block)
            {
                if (_nestedLevel > 0)
                {
                    var directiveStart = block.Children.First(child => !child.IsBlock && ((Span)child).Kind == SpanKindInternal.Transition).Start;
                    var errorLength = /* @ */ 1 + CSharpCodeParser.SectionDirectiveDescriptor.Directive.Length;
                    _errorSink.OnError(
                        directiveStart,
                        LegacyResources.FormatParseError_Sections_Cannot_Be_Nested(LegacyResources.SectionExample_CS),
                        errorLength);
                }

                _nestedLevel++;

                VisitDefault(block);

                _nestedLevel--;
            }
        }
    }
}
