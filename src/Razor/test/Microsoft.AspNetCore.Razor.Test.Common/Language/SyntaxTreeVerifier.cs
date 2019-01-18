// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Text;
using Microsoft.AspNetCore.Razor.Language.Legacy;
using Microsoft.AspNetCore.Razor.Language.Syntax;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language
{
    // Verifies recursively that a syntax tree has no gaps in terms of position/location.
    internal class SyntaxTreeVerifier
    {
        public static void Verify(RazorSyntaxTree syntaxTree, bool ensureFullFidelity = true, int? expectedParseLength = null)
        {
            var verifier = new Verifier(syntaxTree.Source);
            verifier.Visit(syntaxTree.Root);

            if (ensureFullFidelity)
            {
                var syntaxTreeString = syntaxTree.Root.ToFullString();
                var builder = new StringBuilder(syntaxTree.Source.Length);
                var sourceLength = expectedParseLength ?? syntaxTree.Source.Length;
                for (var i = 0; i < sourceLength; i++)
                {
                    builder.Append(syntaxTree.Source[i]);
                }
                var sourceString = builder.ToString();

                // Make sure the syntax tree contains all of the text in the document.
                Assert.Equal(sourceString, syntaxTreeString);
            }
        }

        private class Verifier : SyntaxWalker
        {
            private readonly SourceLocationTracker _tracker;
            private readonly RazorSourceDocument _source;

            public Verifier(RazorSourceDocument source)
            {
                _tracker = new SourceLocationTracker(new SourceLocation(source.FilePath, 0, 0, 0));
                _source = source;
            }

            public SourceLocationTracker SourceLocationTracker => _tracker;

            public override void VisitToken(SyntaxToken token)
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

                base.VisitToken(token);
            }
        }
    }
}
