// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.AspNetCore.Razor.Language.Syntax;

namespace Microsoft.AspNetCore.Razor.Language.Legacy
{
    internal class SpanEditHandler
    {
        private static readonly int TypeHashCode = typeof(SpanEditHandler).GetHashCode();

        public SpanEditHandler(Func<string, IEnumerable<Syntax.InternalSyntax.SyntaxToken>> tokenizer)
            : this(tokenizer, AcceptedCharactersInternal.Any)
        {
        }

        public SpanEditHandler(Func<string, IEnumerable<Syntax.InternalSyntax.SyntaxToken>> tokenizer, AcceptedCharactersInternal accepted)
        {
            AcceptedCharacters = accepted;
            Tokenizer = tokenizer;
        }

        public AcceptedCharactersInternal AcceptedCharacters { get; set; }

        public Func<string, IEnumerable<Syntax.InternalSyntax.SyntaxToken>> Tokenizer { get; set; }

        public static SpanEditHandler CreateDefault()
        {
            return CreateDefault(c => Enumerable.Empty<Syntax.InternalSyntax.SyntaxToken>());
        }

        public static SpanEditHandler CreateDefault(Func<string, IEnumerable<Syntax.InternalSyntax.SyntaxToken>> tokenizer)
        {
            return new SpanEditHandler(tokenizer);
        }

        public virtual EditResult ApplyChange(SyntaxNode target, SourceChange change)
        {
            return ApplyChange(target, change, force: false);
        }

        public virtual EditResult ApplyChange(SyntaxNode target, SourceChange change, bool force)
        {
            var result = PartialParseResultInternal.Accepted;
            if (!force)
            {
                result = CanAcceptChange(target, change);
            }

            // If the change is accepted then apply the change
            if ((result & PartialParseResultInternal.Accepted) == PartialParseResultInternal.Accepted)
            {
                return new EditResult(result, UpdateSpan(target, change));
            }
            return new EditResult(result, target);
        }

        public virtual bool OwnsChange(SyntaxNode target, SourceChange change)
        {
            var end = target.EndPosition;
            var changeOldEnd = change.Span.AbsoluteIndex + change.Span.Length;
            return change.Span.AbsoluteIndex >= target.Position &&
                   (changeOldEnd < end || (changeOldEnd == end && AcceptedCharacters != AcceptedCharactersInternal.None));
        }

        protected virtual PartialParseResultInternal CanAcceptChange(SyntaxNode target, SourceChange change)
        {
            return PartialParseResultInternal.Rejected;
        }

        protected virtual SyntaxNode UpdateSpan(SyntaxNode target, SourceChange change)
        {
            var newContent = change.GetEditedContent(target);
            var builder = Syntax.InternalSyntax.SyntaxListBuilder<Syntax.InternalSyntax.SyntaxToken>.Create();
            foreach (var token in Tokenizer(newContent))
            {
                builder.Add(token);
            }

            SyntaxNode newTarget = null;
            if (target is RazorMetaCodeSyntax)
            {
                newTarget = Syntax.InternalSyntax.SyntaxFactory.RazorMetaCode(builder.ToList()).CreateRed(target.Parent, target.Position);
            }
            else if (target is MarkupTextLiteralSyntax)
            {
                newTarget = Syntax.InternalSyntax.SyntaxFactory.MarkupTextLiteral(builder.ToList()).CreateRed(target.Parent, target.Position);
            }
            else if (target is MarkupEphemeralTextLiteralSyntax)
            {
                newTarget = Syntax.InternalSyntax.SyntaxFactory.MarkupEphemeralTextLiteral(builder.ToList()).CreateRed(target.Parent, target.Position);
            }
            else if (target is CSharpStatementLiteralSyntax)
            {
                newTarget = Syntax.InternalSyntax.SyntaxFactory.CSharpStatementLiteral(builder.ToList()).CreateRed(target.Parent, target.Position);
            }
            else if (target is CSharpExpressionLiteralSyntax)
            {
                newTarget = Syntax.InternalSyntax.SyntaxFactory.CSharpExpressionLiteral(builder.ToList()).CreateRed(target.Parent, target.Position);
            }
            else if (target is CSharpEphemeralTextLiteralSyntax)
            {
                newTarget = Syntax.InternalSyntax.SyntaxFactory.CSharpEphemeralTextLiteral(builder.ToList()).CreateRed(target.Parent, target.Position);
            }
            else if (target is UnclassifiedTextLiteralSyntax)
            {
                newTarget = Syntax.InternalSyntax.SyntaxFactory.UnclassifiedTextLiteral(builder.ToList()).CreateRed(target.Parent, target.Position);
            }
            else
            {
                Debug.Fail($"The type {target?.GetType().Name} is not a supported span node.");
            }

            var context = target.GetSpanContext();
            newTarget = context != null ? newTarget?.WithSpanContext(context) : newTarget;

            return newTarget;
        }

        protected internal static bool IsAtEndOfFirstLine(SyntaxNode target, SourceChange change)
        {
            var endOfFirstLine = target.GetContent().IndexOfAny(new char[] { (char)0x000d, (char)0x000a, (char)0x2028, (char)0x2029 });
            return (endOfFirstLine == -1 || (change.Span.AbsoluteIndex - target.Position) <= endOfFirstLine);
        }

        /// <summary>
        /// Returns true if the specified change is an insertion of text at the end of this span.
        /// </summary>
        protected internal static bool IsEndDeletion(SyntaxNode target, SourceChange change)
        {
            return change.IsDelete && IsAtEndOfSpan(target, change);
        }

        /// <summary>
        /// Returns true if the specified change is a replacement of text at the end of this span.
        /// </summary>
        protected internal static bool IsEndReplace(SyntaxNode target, SourceChange change)
        {
            return change.IsReplace && IsAtEndOfSpan(target, change);
        }

        protected internal static bool IsAtEndOfSpan(SyntaxNode target, SourceChange change)
        {
            return (change.Span.AbsoluteIndex + change.Span.Length) == target.EndPosition;
        }

        public override string ToString()
        {
            return GetType().Name + ";Accepts:" + AcceptedCharacters;
        }

        public override bool Equals(object obj)
        {
            return obj is SpanEditHandler other &&
                GetType() == other.GetType() &&
                AcceptedCharacters == other.AcceptedCharacters;
        }

        public override int GetHashCode()
        {
            // Hash code should include only immutable properties but Equals also checks the type.
            return TypeHashCode;
        }
    }
}
