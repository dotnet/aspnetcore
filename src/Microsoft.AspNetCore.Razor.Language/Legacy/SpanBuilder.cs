// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.AspNetCore.Razor.Language.Syntax.InternalSyntax;

namespace Microsoft.AspNetCore.Razor.Language.Legacy
{
    internal class SpanBuilder
    {
        private SourceLocation _start;
        private List<IToken> _tokens;
        private SourceLocationTracker _tracker;

        public SpanBuilder(Span original)
        {
            Kind = original.Kind;
            EditHandler = original.EditHandler;
            _start = original.Start;
            ChunkGenerator = original.ChunkGenerator;

            _tokens = new List<IToken>(original.Tokens);
            _tracker = new SourceLocationTracker(original.Start);
        }

        public SpanBuilder(SourceLocation location)
        {
            _tracker = new SourceLocationTracker();

            Reset();

            Start = location;
        }

        public HtmlNodeSyntax SyntaxNode { get; private set; }

        public ISpanChunkGenerator ChunkGenerator { get; set; }

        public SourceLocation Start
        {
            get { return _start; }
            set
            {
                _start = value;
                _tracker.CurrentLocation = value;
            }
        }

        public SourceLocation End => _tracker.CurrentLocation;

        public SpanKindInternal Kind { get; set; }

        public IReadOnlyList<IToken> Tokens
        {
            get
            {
                if (_tokens == null)
                {
                    _tokens = new List<IToken>();
                }

                return _tokens;
            }
        }

        public SpanEditHandler EditHandler { get; set; }

        public void Reset()
        {
            // Need to potentially allocate a new list because Span.ReplaceWith takes ownership
            // of the original list.
            _tokens = null;
            _tokens = new List<IToken>();

            EditHandler = SpanEditHandler.CreateDefault((content) => Enumerable.Empty<IToken>());
            ChunkGenerator = SpanChunkGenerator.Null;
            Start = SourceLocation.Undefined;
        }

        public Span Build(SyntaxKind syntaxKind = SyntaxKind.Unknown)
        {
            SyntaxNode = GetSyntaxNode(syntaxKind);

            var span = new Span(this);
            
            for (var i = 0; i < span.Tokens.Count; i++)
            {
                var token = span.Tokens[i];
                token.Parent = span;
            }

            return span;
        }

        public void ClearTokens()
        {
            _tokens?.Clear();
        }

        public void Accept(IToken token)
        {
            if (token == null)
            {
                return;
            }

            if (Start.Equals(SourceLocation.Undefined))
            {
                throw new InvalidOperationException("SpanBuilder must have a valid location");
            }

            _tokens.Add(token);
            _tracker.UpdateLocation(token.Content);
        }

        private HtmlNodeSyntax GetSyntaxNode(SyntaxKind syntaxKind)
        {
            if (syntaxKind == SyntaxKind.HtmlText)
            {
                var textTokens = new SyntaxListBuilder<SyntaxToken>(SyntaxListBuilder.Create());
                foreach (var token in Tokens)
                {
                    if (token.SyntaxKind == SyntaxKind.Unknown)
                    {
                        Debug.Assert(false, $"Unexpected html token {((HtmlToken)token).Type}");
                        continue;
                    }

                    textTokens.Add(token.SyntaxToken);
                }
                var textResult = textTokens.ToList();
                return SyntaxFactory.HtmlText(new SyntaxList<SyntaxToken>(textResult.Node));
            }

            return null;
        }
    }
}
