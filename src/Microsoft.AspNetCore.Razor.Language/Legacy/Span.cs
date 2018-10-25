// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.AspNetCore.Razor.Language.Syntax;
using Microsoft.Extensions.Internal;

namespace Microsoft.AspNetCore.Razor.Language.Legacy
{
    internal class Span : SyntaxTreeNode
    {
        private static readonly List<SyntaxToken> EmptyTokenList = new List<SyntaxToken>(0);
        private static readonly int TypeHashCode = typeof(Span).GetHashCode();
        private IReadOnlyList<Syntax.InternalSyntax.SyntaxToken> _greenTokens;
        private string _content;
        private int? _length;
        private SourceLocation _start;

        public Span(SpanBuilder builder)
        {
            ReplaceWith(builder);
        }

        public ISpanChunkGenerator ChunkGenerator { get; private set; }

        public SpanKindInternal Kind { get; private set; }

        public IReadOnlyList<SyntaxToken> Tokens { get; private set; }

        // Allow test code to re-link spans
        public Span Previous { get; internal set; }
        public Span Next { get; internal set; }

        public SpanEditHandler EditHandler { get; private set; }

        public SyntaxNode SyntaxNode { get; private set; }

        public override bool IsBlock => false;

        public override int Length
        {
            get
            {
                if (_length == null)
                {
                    var length = 0;
                    if (_content == null)
                    {
                        for (var i = 0; i < Tokens.Count; i++)
                        {
                            length += Tokens[i].Content.Length;
                        }
                    }
                    else
                    {
                        length = _content.Length;
                    }

                    _length = length;
                }

                return _length.Value;
            }
        }

        public override SourceLocation Start => _start;

        public string Content
        {
            get
            {
                if (_content == null)
                {
                    var tokenCount = Tokens.Count;
                    if (tokenCount == 1)
                    {
                        // Perf: no StringBuilder allocation if not necessary
                        _content = Tokens[0].Content;
                    }
                    else
                    {
                        var builder = new StringBuilder();
                        for (var i = 0; i < tokenCount; i++)
                        {
                            var token = Tokens[i];
                            builder.Append(token.Content);
                        }

                        _content = builder.ToString();
                    }
                }

                return _content;
            }
        }

        public void ReplaceWith(SpanBuilder builder)
        {
            Kind = builder.Kind;
            _greenTokens = builder.Tokens;
            EditHandler = builder.EditHandler;
            ChunkGenerator = builder.ChunkGenerator ?? SpanChunkGenerator.Null;
            _start = builder.Start;
            SyntaxNode = builder.SyntaxNode?.CreateRed(parent: null, position: _start.AbsoluteIndex);
            _content = null;
            _length = null;

            var tokens = EmptyTokenList;
            if (_greenTokens.Count > 0)
            {
                tokens = new List<SyntaxToken>();
                var currentStart = _start.AbsoluteIndex;
                for (var i = 0; i < _greenTokens.Count; i++)
                {
                    var token = new SyntaxToken(_greenTokens[i], parent: SyntaxNode, parentSpan: this, position: currentStart);
                    tokens.Add(token);
                    currentStart += token.FullWidth;
                }
            }

            Tokens = tokens;

            Parent?.ChildChanged();

            // Since we took references to the values in SpanBuilder, clear its references out
            builder.Reset();
        }

        public override string ToString()
        {
            var builder = new StringBuilder();
            builder.Append(Kind);
            builder.AppendFormat(" Span at {0}::{1} - [{2}]", Start, Length, Content);
            builder.Append(" Edit: <");
            builder.Append(EditHandler.ToString());
            builder.Append("> Gen: <");
            builder.Append(ChunkGenerator.ToString());
            builder.Append("> {");
            builder.Append(string.Join(";", Tokens.GroupBy(sym => sym.GetType()).Select(grp => string.Concat(grp.Key.Name, ":", grp.Count()))));
            builder.Append("}");
            return builder.ToString();
        }

        public void ChangeStart(SourceLocation newStart)
        {
            _start = newStart;
            var current = this;
            var tracker = new SourceLocationTracker(newStart);
            tracker.UpdateLocation(Content);
            while ((current = current.Next) != null)
            {
                current._start = tracker.CurrentLocation;
                tracker.UpdateLocation(current.Content);
            }
        }

        /// <summary>
        /// Checks that the specified span is equivalent to the other in that it has the same start point and content.
        /// </summary>
        public override bool EquivalentTo(SyntaxTreeNode node)
        {
            return node is Span other &&
                Kind.Equals(other.Kind) &&
                Start.Equals(other.Start) &&
                EditHandler.Equals(other.EditHandler) &&
                string.Equals(other.Content, Content, StringComparison.Ordinal);
        }

        public override int GetEquivalenceHash()
        {
            // Hash code should include only immutable properties but EquivalentTo also checks the type.
            return TypeHashCode;
        }

        public override bool Equals(object obj)
        {
            return obj is Span other &&
                Kind.Equals(other.Kind) &&
                EditHandler.Equals(other.EditHandler) &&
                ChunkGenerator.Equals(other.ChunkGenerator) &&
                Tokens.SequenceEqual(other.Tokens, SyntaxTokenComparer.Default);
        }

        public override int GetHashCode()
        {
            // Hash code should include only immutable properties but Equals also checks the type.
            return TypeHashCode;
        }

        public override void Accept(ParserVisitor visitor)
        {
            visitor.VisitSpan(this);
        }

        public override SyntaxTreeNode Clone()
        {
            var spanBuilder = new SpanBuilder(this);
            return spanBuilder.Build();
        }

        private class SyntaxTokenComparer : IEqualityComparer<SyntaxToken>
        {
            public static readonly SyntaxTokenComparer Default = new SyntaxTokenComparer();

            private SyntaxTokenComparer()
            {
            }

            public bool Equals(SyntaxToken x, SyntaxToken y)
            {
                return x.IsEquivalentTo(y);
            }

            public int GetHashCode(SyntaxToken obj)
            {
                var hash = HashCodeCombiner.Start();
                hash.Add(obj.Content, StringComparer.Ordinal);
                hash.Add(obj.Kind);

                return hash;
            }
        }
    }
}
