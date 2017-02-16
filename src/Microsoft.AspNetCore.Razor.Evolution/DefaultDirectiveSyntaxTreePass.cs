// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;
using Microsoft.AspNetCore.Razor.Evolution.Legacy;

namespace Microsoft.AspNetCore.Razor.Evolution
{
    internal class DefaultDirectiveSyntaxTreePass : IRazorSyntaxTreePass
    {
        public RazorEngine Engine { get; set; }

        public int Order => 75;

        public RazorSyntaxTree Execute(RazorCodeDocument codeDocument, RazorSyntaxTree syntaxTree)
        {
            var errorSink = new ErrorSink();
            var sectionVerifier = new NestedSectionVerifier();
            sectionVerifier.Verify(syntaxTree, errorSink);

            if (errorSink.Errors.Count > 0)
            {
                var combinedErrors = syntaxTree.Diagnostics.Concat(errorSink.Errors).ToList();
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
                    var directiveStart = block.Children.First(child => !child.IsBlock && ((Span)child).Kind == SpanKind.Transition).Start;
                    var errorLength = /* @ */ 1 + CSharpCodeParser.SectionDirectiveDescriptor.Name.Length;
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
