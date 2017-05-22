// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.AspNetCore.Razor.Language.Extensions;
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

            var sectionVerifier = new NestedSectionVerifier();
            sectionVerifier.Verify(syntaxTree);

            return syntaxTree;
        }

        private class NestedSectionVerifier : ParserVisitor
        {
            private int _nestedLevel;

            public void Verify(RazorSyntaxTree tree)
            {
                tree.Root.Accept(this);
            }

            public override void VisitDirectiveBlock(DirectiveChunkGenerator chunkGenerator, Block block)
            {
                if (_nestedLevel > 0)
                {
                    var directiveStart = block.Children.First(child => !child.IsBlock && ((Span)child).Kind == SpanKindInternal.Transition).Start;
                    var errorLength = /* @ */ 1 + SectionDirective.Directive.Directive.Length;
                    var error = RazorDiagnostic.Create(
                        new RazorError(
                            LegacyResources.FormatParseError_Sections_Cannot_Be_Nested(LegacyResources.SectionExample_CS),
                            directiveStart,
                            errorLength));
                    chunkGenerator.Diagnostics.Add(error);
                }

                _nestedLevel++;

                VisitDefault(block);

                _nestedLevel--;
            }
        }
    }
}
