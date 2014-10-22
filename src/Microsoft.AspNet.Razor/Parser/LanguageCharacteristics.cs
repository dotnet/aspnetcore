// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.AspNet.Razor.Parser.SyntaxTree;
using Microsoft.AspNet.Razor.Text;
using Microsoft.AspNet.Razor.Tokenizer;
using Microsoft.AspNet.Razor.Tokenizer.Symbols;

namespace Microsoft.AspNet.Razor.Parser
{
    [SuppressMessage("Microsoft.Design", "CA1005:AvoidExcessiveParametersOnGenericTypes", Justification = "All generic type parameters are required")]
    public abstract class LanguageCharacteristics<TTokenizer, TSymbol, TSymbolType>
        where TTokenizer : Tokenizer<TSymbol, TSymbolType>
        where TSymbol : SymbolBase<TSymbolType>
    {
        public abstract string GetSample(TSymbolType type);
        public abstract TTokenizer CreateTokenizer(ITextDocument source);
        public abstract TSymbolType FlipBracket(TSymbolType bracket);
        public abstract TSymbol CreateMarkerSymbol(SourceLocation location);

        public virtual IEnumerable<TSymbol> TokenizeString(string content)
        {
            return TokenizeString(SourceLocation.Zero, content);
        }

        public virtual IEnumerable<TSymbol> TokenizeString(SourceLocation start, string input)
        {
            using (SeekableTextReader reader = new SeekableTextReader(input))
            {
                var tok = CreateTokenizer(reader);
                TSymbol sym;
                while ((sym = tok.NextSymbol()) != null)
                {
                    sym.OffsetStart(start);
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
            var left = CreateSymbol(symbol.Start, symbol.Content.Substring(0, splitAt), leftType, Enumerable.Empty<RazorError>());
            TSymbol right = null;
            if (splitAt < symbol.Content.Length)
            {
                right = CreateSymbol(SourceLocationTracker.CalculateNewLocation(symbol.Start, left.Content), symbol.Content.Substring(splitAt), symbol.Type, symbol.Errors);
            }
            return Tuple.Create(left, right);
        }

        public abstract TSymbolType GetKnownSymbolType(KnownSymbolType type);

        public virtual bool KnowsSymbolType(KnownSymbolType type)
        {
            return type == KnownSymbolType.Unknown || !Equals(GetKnownSymbolType(type), GetKnownSymbolType(KnownSymbolType.Unknown));
        }

        protected abstract TSymbol CreateSymbol(SourceLocation location, string content, TSymbolType type, IEnumerable<RazorError> errors);
    }
}
