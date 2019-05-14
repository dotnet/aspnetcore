// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.AspNetCore.Razor.Language.Legacy
{
    internal abstract class LanguageCharacteristics<TTokenizer, TToken, TTokenType>
        where TTokenType : struct
        where TTokenizer : Tokenizer<TToken, TTokenType>
        where TToken : TokenBase<TTokenType>
    {
        public abstract string GetSample(TTokenType type);
        public abstract TTokenizer CreateTokenizer(ITextDocument source);
        public abstract TTokenType FlipBracket(TTokenType bracket);
        public abstract TToken CreateMarkerToken();

        public virtual IEnumerable<TToken> TokenizeString(string content)
        {
            return TokenizeString(SourceLocation.Zero, content);
        }

        public virtual IEnumerable<TToken> TokenizeString(SourceLocation start, string input)
        {
            using (var reader = new SeekableTextReader(input, start.FilePath))
            {
                var tok = CreateTokenizer(reader);
                TToken token;
                while ((token = tok.NextToken()) != null)
                {
                    yield return token;
                }
            }
        }

        public virtual bool IsWhiteSpace(TToken token)
        {
            return IsKnownTokenType(token, KnownTokenType.WhiteSpace);
        }

        public virtual bool IsNewLine(TToken token)
        {
            return IsKnownTokenType(token, KnownTokenType.NewLine);
        }

        public virtual bool IsIdentifier(TToken token)
        {
            return IsKnownTokenType(token, KnownTokenType.Identifier);
        }

        public virtual bool IsKeyword(TToken token)
        {
            return IsKnownTokenType(token, KnownTokenType.Keyword);
        }

        public virtual bool IsTransition(TToken token)
        {
            return IsKnownTokenType(token, KnownTokenType.Transition);
        }

        public virtual bool IsCommentStart(TToken token)
        {
            return IsKnownTokenType(token, KnownTokenType.CommentStart);
        }

        public virtual bool IsCommentStar(TToken token)
        {
            return IsKnownTokenType(token, KnownTokenType.CommentStar);
        }

        public virtual bool IsCommentBody(TToken token)
        {
            return IsKnownTokenType(token, KnownTokenType.CommentBody);
        }

        public virtual bool IsUnknown(TToken token)
        {
            return IsKnownTokenType(token, KnownTokenType.Unknown);
        }

        public virtual bool IsKnownTokenType(TToken token, KnownTokenType type)
        {
            return token != null && Equals(token.Type, GetKnownTokenType(type));
        }

        public virtual Tuple<TToken, TToken> SplitToken(TToken token, int splitAt, TTokenType leftType)
        {
            var left = CreateToken(token.Content.Substring(0, splitAt), leftType, RazorDiagnostic.EmptyArray);

            TToken right = null;
            if (splitAt < token.Content.Length)
            {
                right = CreateToken(token.Content.Substring(splitAt), token.Type, token.Errors);
            }

            return Tuple.Create(left, right);
        }

        public abstract TTokenType GetKnownTokenType(KnownTokenType type);

        public virtual bool KnowsTokenType(KnownTokenType type)
        {
            return type == KnownTokenType.Unknown || !Equals(GetKnownTokenType(type), GetKnownTokenType(KnownTokenType.Unknown));
        }

        protected abstract TToken CreateToken(string content, TTokenType type, IReadOnlyList<RazorDiagnostic> errors);
    }
}
