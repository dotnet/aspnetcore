// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Razor.Language.Syntax.InternalSyntax;

namespace Microsoft.AspNetCore.Razor.Language.Legacy
{
    internal abstract class LanguageCharacteristics<TTokenizer>
        where TTokenizer : Tokenizer
    {
        public abstract string GetSample(SyntaxKind type);
        public abstract TTokenizer CreateTokenizer(ITextDocument source);
        public abstract SyntaxKind FlipBracket(SyntaxKind bracket);
        public abstract SyntaxToken CreateMarkerToken();

        public virtual IEnumerable<SyntaxToken> TokenizeString(string content)
        {
            return TokenizeString(SourceLocation.Zero, content);
        }

        public virtual IEnumerable<SyntaxToken> TokenizeString(SourceLocation start, string input)
        {
            using (var reader = new SeekableTextReader(input, start.FilePath))
            {
                var tok = CreateTokenizer(reader);
                SyntaxToken token;
                while ((token = tok.NextToken()) != null)
                {
                    yield return token;
                }
            }
        }

        public virtual bool IsWhiteSpace(SyntaxToken token)
        {
            return IsKnownTokenType(token, KnownTokenType.WhiteSpace);
        }

        public virtual bool IsNewLine(SyntaxToken token)
        {
            return IsKnownTokenType(token, KnownTokenType.NewLine);
        }

        public virtual bool IsIdentifier(SyntaxToken token)
        {
            return IsKnownTokenType(token, KnownTokenType.Identifier);
        }

        public virtual bool IsKeyword(SyntaxToken token)
        {
            return IsKnownTokenType(token, KnownTokenType.Keyword);
        }

        public virtual bool IsTransition(SyntaxToken token)
        {
            return IsKnownTokenType(token, KnownTokenType.Transition);
        }

        public virtual bool IsCommentStart(SyntaxToken token)
        {
            return IsKnownTokenType(token, KnownTokenType.CommentStart);
        }

        public virtual bool IsCommentStar(SyntaxToken token)
        {
            return IsKnownTokenType(token, KnownTokenType.CommentStar);
        }

        public virtual bool IsCommentBody(SyntaxToken token)
        {
            return IsKnownTokenType(token, KnownTokenType.CommentBody);
        }

        public virtual bool IsUnknown(SyntaxToken token)
        {
            return IsKnownTokenType(token, KnownTokenType.Unknown);
        }

        public virtual bool IsKnownTokenType(SyntaxToken token, KnownTokenType type)
        {
            return token != null && Equals(token.Kind, GetKnownTokenType(type));
        }

        public virtual Tuple<SyntaxToken, SyntaxToken> SplitToken(SyntaxToken token, int splitAt, SyntaxKind leftType)
        {
            var left = CreateToken(token.Content.Substring(0, splitAt), leftType, RazorDiagnostic.EmptyArray);

            SyntaxToken right = null;
            if (splitAt < token.Content.Length)
            {
                right = CreateToken(token.Content.Substring(splitAt), token.Kind, token.GetDiagnostics());
            }

            return Tuple.Create(left, right);
        }

        public abstract SyntaxKind GetKnownTokenType(KnownTokenType type);

        public virtual bool KnowsTokenType(KnownTokenType type)
        {
            return type == KnownTokenType.Unknown || !Equals(GetKnownTokenType(type), GetKnownTokenType(KnownTokenType.Unknown));
        }

        protected abstract SyntaxToken CreateToken(string content, SyntaxKind type, IReadOnlyList<RazorDiagnostic> errors);
    }
}
