// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Globalization;
using Microsoft.AspNetCore.Razor.Language.Syntax.InternalSyntax;

namespace Microsoft.AspNetCore.Razor.Language.Legacy
{
    internal class RawTextToken : IToken
    {
        public RawTextToken(SourceLocation start, string content)
        {
            Start = start;
            Content = content;
        }

        public SourceLocation Start { get; private set; }
        public string Content { get; }
        public Span Parent { get; set; }

        public SyntaxKind SyntaxKind => SyntaxToken.Kind;

        public SyntaxToken SyntaxToken => SyntaxFactory.UnknownToken(Content);

        public override bool Equals(object obj)
        {
            var other = obj as RawTextToken;
            return other != null && Equals(Start, other.Start) && Equals(Content, other.Content);
        }

        internal bool EquivalentTo(IToken token)
        {
            return Equals(Start, token.Start) && Equals(Content, token.Content);
        }

        public override int GetHashCode()
        {
            // Hash code should include only immutable properties.
            return Content == null ? 0 : Content.GetHashCode();
        }

        public override string ToString()
        {
            return string.Format(CultureInfo.InvariantCulture, "{0} RAW - [{1}]", Start, Content);
        }

        internal void CalculateStart(Span prev)
        {
            if (prev == null)
            {
                Start = SourceLocation.Zero;
            }
            else
            {
                Start = new SourceLocationTracker(prev.Start).UpdateLocation(prev.Content).CurrentLocation;
            }
        }
    }
}
