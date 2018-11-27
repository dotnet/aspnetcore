// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Razor.Language.Legacy;
using Microsoft.AspNetCore.Razor.Language.Syntax;

namespace Microsoft.AspNetCore.Razor.Language
{
    // Verifies recursively that a syntax tree has no gaps in terms of position/location.
    internal class SyntaxTreeVerifier
    {
        public static void Verify(RazorSyntaxTree syntaxTree)
        {
            new Verifier(syntaxTree.Source).Visit(syntaxTree.Root);
        }

        private class Verifier : SyntaxRewriter
        {
            private readonly SourceLocationTracker _tracker;
            private readonly RazorSourceDocument _source;

            public Verifier(RazorSourceDocument source)
            {
                _tracker = new SourceLocationTracker(new SourceLocation(source.FilePath, 0, 0, 0));
                _source = source;
            }

            public override SyntaxNode VisitToken(SyntaxToken token)
            {
                if (token != null && !token.IsMissing && token.Kind != SyntaxKind.Marker)
                {
                    var start = token.GetSourceLocation(_source);
                    if (!start.Equals(_tracker.CurrentLocation))
                    {
                        throw new InvalidOperationException($"Token starting at {start} should start at {_tracker.CurrentLocation} - {token} ");
                    }

                    _tracker.UpdateLocation(token.Content);
                }

                return base.VisitToken(token);
            }
        }
    }
}
