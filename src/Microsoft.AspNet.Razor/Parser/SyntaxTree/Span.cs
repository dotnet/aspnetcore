// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Microsoft.AspNet.Razor.Editor;
using Microsoft.AspNet.Razor.Generator;
using Microsoft.AspNet.Razor.Text;
using Microsoft.AspNet.Razor.Tokenizer.Symbols;

namespace Microsoft.AspNet.Razor.Parser.SyntaxTree
{
    public class Span : SyntaxTreeNode
    {
        private static readonly int TypeHashCode = typeof(Span).GetHashCode();
        private SourceLocation _start;

        public Span(SpanBuilder builder)
        {
            ReplaceWith(builder);
        }

        public SpanKind Kind { get; protected set; }
        public IEnumerable<ISymbol> Symbols { get; protected set; }

        // Allow test code to re-link spans
        public Span Previous { get; protected internal set; }
        public Span Next { get; protected internal set; }

        public SpanEditHandler EditHandler { get; protected set; }
        public ISpanCodeGenerator CodeGenerator { get; protected set; }

        public override bool IsBlock
        {
            get { return false; }
        }

        public override int Length
        {
            get { return Content.Length; }
        }

        public override SourceLocation Start
        {
            get { return _start; }
        }

        public string Content { get; private set; }

        public void Change(Action<SpanBuilder> changes)
        {
            var builder = new SpanBuilder(this);
            changes(builder);
            ReplaceWith(builder);
        }

        public void ReplaceWith(SpanBuilder builder)
        {
            Debug.Assert(!builder.Symbols.Any() || builder.Symbols.All(s => s != null));

            Kind = builder.Kind;
            Symbols = builder.Symbols;
            EditHandler = builder.EditHandler;
            CodeGenerator = builder.CodeGenerator ?? SpanCodeGenerator.Null;
            _start = builder.Start;

            // Since we took references to the values in SpanBuilder, clear its references out
            builder.Reset();

            // Calculate other properties
            Content = Symbols.Aggregate(new StringBuilder(), (sb, sym) => sb.Append(sym.Content), sb => sb.ToString());
        }

        /// <summary>
        /// Accepts the specified visitor
        /// </summary>
        /// <remarks>
        /// Calls the VisitSpan method on the specified visitor, passing in this
        /// </remarks>
        public override void Accept(ParserVisitor visitor)
        {
            visitor.VisitSpan(this);
        }

        public override string ToString()
        {
            var builder = new StringBuilder();
            builder.Append(Kind);
            builder.AppendFormat(" Span at {0}::{1} - [{2}]", Start, Length, Content);
            builder.Append(" Edit: <");
            builder.Append(EditHandler.ToString());
            builder.Append(">");
            builder.Append(" Gen: <");
            builder.Append(CodeGenerator.ToString());
            builder.Append("> {");
            builder.Append(string.Join(";", Symbols.GroupBy(sym => sym.GetType()).Select(grp => string.Concat(grp.Key.Name, ":", grp.Count()))));
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

        internal void SetStart(SourceLocation newStart)
        {
            _start = newStart;
        }

        /// <summary>
        /// Checks that the specified span is equivalent to the other in that it has the same start point and content.
        /// </summary>
        public override bool EquivalentTo(SyntaxTreeNode node)
        {
            var other = node as Span;
            return other != null &&
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
            var other = obj as Span;
            return other != null &&
                Kind.Equals(other.Kind) &&
                EditHandler.Equals(other.EditHandler) &&
                CodeGenerator.Equals(other.CodeGenerator) &&
                Symbols.SequenceEqual(other.Symbols);
        }

        public override int GetHashCode()
        {
            // Hash code should include only immutable properties but Equals also checks the type.
            return TypeHashCode;
        }
    }
}
