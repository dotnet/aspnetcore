// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.AspNetCore.Razor.Language.Syntax;

namespace Microsoft.AspNetCore.Razor.Language.Legacy
{
    internal class CodeBlockEditHandler : SpanEditHandler
    {
        public CodeBlockEditHandler(Func<string, IEnumerable<Syntax.InternalSyntax.SyntaxToken>> tokenizer) : base(tokenizer)
        {
        }

        protected override PartialParseResultInternal CanAcceptChange(SyntaxNode target, SourceChange change)
        {
            if (IsAcceptableDeletion(target, change))
            {
                return PartialParseResultInternal.Accepted;
            }

            if (IsAcceptableReplacement(target, change))
            {
                return PartialParseResultInternal.Accepted;
            }

            if (IsAcceptableInsertion(change))
            {
                return PartialParseResultInternal.Accepted;
            }

            return PartialParseResultInternal.Rejected;
        }

        // Internal for testing
        internal static bool IsAcceptableReplacement(SyntaxNode target, SourceChange change)
        {
            if (!change.IsReplace)
            {
                return false;
            }

            if (ContainsInvalidContent(change))
            {
                return false;
            }

            if (ModifiesInvalidContent(target, change))
            {
                return false;
            }

            return true;
        }

        // Internal for testing
        internal static bool IsAcceptableDeletion(SyntaxNode target, SourceChange change)
        {
            if (!change.IsDelete)
            {
                return false;
            }

            if (ModifiesInvalidContent(target, change))
            {
                return false;
            }

            return true;
        }

        // Internal for testing
        internal static bool ModifiesInvalidContent(SyntaxNode target, SourceChange change)
        {
            var relativePosition = change.Span.AbsoluteIndex - target.Position;

            if (target.GetContent().IndexOfAny(new[] { '{', '}' }, relativePosition, change.Span.Length) >= 0)
            {
                return true;
            }

            return false;
        }

        // Internal for testing
        internal static bool IsAcceptableInsertion(SourceChange change)
        {
            if (!change.IsInsert)
            {
                return false;
            }

            if (ContainsInvalidContent(change))
            {
                return false;
            }

            return true;
        }

        // Internal for testing
        internal static bool ContainsInvalidContent(SourceChange change)
        {
            if (change.NewText.IndexOfAny(new[] { '{', '}' }) >= 0)
            {
                return true;
            }

            return false;
        }

        public override string ToString()
        {
            return string.Format(CultureInfo.InvariantCulture, "{0};CodeBlock", base.ToString());
        }

        public override bool Equals(object obj)
        {
            return obj is CodeBlockEditHandler other &&
                base.Equals(other);
        }

        public override int GetHashCode() => base.GetHashCode();
    }
}
