// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;

namespace Microsoft.AspNetCore.Razor.Language.Legacy
{
    internal abstract class LanguageCharacteristics<TTokenizer, TSymbol, TSymbolType>
        where TSymbolType : struct
        where TTokenizer : Tokenizer<TSymbol, TSymbolType>
        where TSymbol : SymbolBase<TSymbolType>
    {
        public abstract string GetSample(TSymbolType type);
        public abstract TTokenizer CreateTokenizer(ITextDocument source);
        public abstract TSymbolType FlipBracket(TSymbolType bracket);
        public abstract TSymbol CreateMarkerSymbol();

        public virtual IEnumerable<TSymbol> TokenizeString(string content)
        {
            return TokenizeString(SourceLocation.Zero, content);
        }

        public virtual IEnumerable<TSymbol> TokenizeString(SourceLocation start, string input)
        {
            using (var reader = new SeekableTextReader(input, start.FilePath))
            {
                var tok = CreateTokenizer(reader);
                TSymbol sym;
                while ((sym = tok.NextSymbol()) != null)
                {
                    yield return sym;
                }
            }
        }

        public virtual bool IsWhiteSpace(TSymbol symbol)
        {
            return IsKnownSymbolType(symbol, KnownSymbolType.WhiteSpace);
        }

        public virtual bool IsNewLine(TSymbol symbol)
        {
            return IsKnownSymbolType(symbol, KnownSymbolType.NewLine);
        }

        public virtual bool IsIdentifier(TSymbol symbol)
        {
            return IsKnownSymbolType(symbol, KnownSymbolType.Identifier);
        }

        public virtual bool IsKeyword(TSymbol symbol)
        {
            return IsKnownSymbolType(symbol, KnownSymbolType.Keyword);
        }

        public virtual bool IsTransition(TSymbol symbol)
        {
            return IsKnownSymbolType(symbol, KnownSymbolType.Transition);
        }

        public virtual bool IsCommentStart(TSymbol symbol)
        {
            return IsKnownSymbolType(symbol, KnownSymbolType.CommentStart);
        }

        public virtual bool IsCommentStar(TSymbol symbol)
        {
            return IsKnownSymbolType(symbol, KnownSymbolType.CommentStar);
        }

        public virtual bool IsCommentBody(TSymbol symbol)
        {
            return IsKnownSymbolType(symbol, KnownSymbolType.CommentBody);
        }

        public virtual bool IsUnknown(TSymbol symbol)
        {
            return IsKnownSymbolType(symbol, KnownSymbolType.Unknown);
        }

        public virtual bool IsKnownSymbolType(TSymbol symbol, KnownSymbolType type)
        {
            return symbol != null && Equals(symbol.Type, GetKnownSymbolType(type));
        }

        public virtual Tuple<TSymbol, TSymbol> SplitSymbol(TSymbol symbol, int splitAt, TSymbolType leftType)
        {
            var left = CreateSymbol(symbol.Content.Substring(0, splitAt), leftType, RazorError.EmptyArray);

            TSymbol right = null;
            if (splitAt < symbol.Content.Length)
            {
                right = CreateSymbol(symbol.Content.Substring(splitAt), symbol.Type, symbol.Errors);
            }

            return Tuple.Create(left, right);
        }

        public abstract TSymbolType GetKnownSymbolType(KnownSymbolType type);

        public virtual bool KnowsSymbolType(KnownSymbolType type)
        {
            return type == KnownSymbolType.Unknown || !Equals(GetKnownSymbolType(type), GetKnownSymbolType(KnownSymbolType.Unknown));
        }

        protected abstract TSymbol CreateSymbol(string content, TSymbolType type, IReadOnlyList<RazorError> errors);
    }
}
