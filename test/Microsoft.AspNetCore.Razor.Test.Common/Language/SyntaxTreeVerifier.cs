// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Razor.Language.Legacy;

namespace Microsoft.AspNetCore.Razor.Language
{
    // Verifies recursively that a syntax tree has no gaps in terms of position/location.
    internal class SyntaxTreeVerifier : ParserVisitor
    {
        private readonly SourceLocationTracker _tracker = new SourceLocationTracker(SourceLocation.Zero);

        private SyntaxTreeVerifier()
        {
        }

        public static void Verify(RazorSyntaxTree syntaxTree)
        {
            Verify(syntaxTree.Root);
        }

        public static void Verify(Block block)
        {
            new SyntaxTreeVerifier().VisitBlock(block);
        }

        public override void VisitSpan(Span span)
        {
            var start = span.Start;
            if (!start.Equals(_tracker.CurrentLocation))
            {
                throw new InvalidOperationException($"Span starting at {span.Start} should start at {_tracker.CurrentLocation} - {span} ");
            }

            for (var i = 0; i < span.Symbols.Count; i++)
            {
                _tracker.UpdateLocation(span.Symbols[i].Content);
            }
        }
    }
}
