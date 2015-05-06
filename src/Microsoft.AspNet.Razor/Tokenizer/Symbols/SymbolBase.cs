// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Microsoft.Framework.Internal;
using Microsoft.Internal.Web.Utils;

namespace Microsoft.AspNet.Razor.Tokenizer.Symbols
{
    public abstract class SymbolBase<TType> : ISymbol
        where TType: struct
    {
        protected SymbolBase(
            SourceLocation start,
            [NotNull] string content,
            TType type,
            IEnumerable<RazorError> errors)
        {
            Start = start;
            Content = content;
            Type = type;
            Errors = errors;
        }

        public SourceLocation Start { get; private set; }

        public string Content { get; }

        public IEnumerable<RazorError> Errors { get; }

        [SuppressMessage("Microsoft.Naming", "CA1721:PropertyNamesShouldNotMatchGetMethods", Justification = "This is the most appropriate name for this property and conflicts are unlikely")]
        public TType Type { get; }

        public override bool Equals(object obj)
        {
            SymbolBase<TType> other = obj as SymbolBase<TType>;
            return other != null &&
                Start.Equals(other.Start) &&
                string.Equals(Content, other.Content, StringComparison.Ordinal) &&
                Type.Equals(other.Type);
        }

        public override int GetHashCode()
        {
            // Hash code should include only immutable properties.
            return HashCodeCombiner.Start()
                .Add(Content, StringComparer.Ordinal)
                .Add(Type)
                .CombinedHash;
        }

        public override string ToString()
        {
            return string.Format(CultureInfo.InvariantCulture, "{0} {1} - [{2}]", Start, Type, Content);
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
