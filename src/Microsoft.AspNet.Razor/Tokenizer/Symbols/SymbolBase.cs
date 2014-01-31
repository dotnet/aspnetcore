// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Microsoft.AspNet.Razor.Parser.SyntaxTree;
using Microsoft.AspNet.Razor.Text;
using Microsoft.Internal.Web.Utils;

namespace Microsoft.AspNet.Razor.Tokenizer.Symbols
{
    public abstract class SymbolBase<TType> : ISymbol
    {
        protected SymbolBase(SourceLocation start, string content, TType type, IEnumerable<RazorError> errors)
        {
            if (content == null)
            {
                throw new ArgumentNullException("content");
            }
            if (type == null)
            {
                throw new ArgumentNullException("type");
            }

            Start = start;
            Content = content;
            Type = type;
            Errors = errors;
        }

        public SourceLocation Start { get; private set; }
        public string Content { get; private set; }
        public IEnumerable<RazorError> Errors { get; private set; }

        [SuppressMessage("Microsoft.Naming", "CA1721:PropertyNamesShouldNotMatchGetMethods", Justification = "This is the most appropriate name for this property and conflicts are unlikely")]
        public TType Type { get; private set; }

        public override bool Equals(object obj)
        {
            SymbolBase<TType> other = obj as SymbolBase<TType>;
            return other != null &&
                   Start.Equals(other.Start) &&
                   String.Equals(Content, other.Content, StringComparison.Ordinal) &&
                   Type.Equals(other.Type);
        }

        public override int GetHashCode()
        {
            return HashCodeCombiner.Start()
                .Add(Start)
                .Add(Content)
                .Add(Type)
                .CombinedHash;
        }

        public override string ToString()
        {
            return String.Format(CultureInfo.InvariantCulture, "{0} {1} - [{2}]", Start, Type, Content);
        }

        public void OffsetStart(SourceLocation documentStart)
        {
            Start = documentStart + Start;
        }

        public void ChangeStart(SourceLocation newStart)
        {
            Start = newStart;
        }
    }
}
