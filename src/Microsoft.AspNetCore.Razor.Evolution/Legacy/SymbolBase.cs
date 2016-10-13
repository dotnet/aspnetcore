// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.Extensions.Internal;

namespace Microsoft.AspNetCore.Razor.Evolution.Legacy
{
    internal abstract class SymbolBase<TType> : ISymbol where TType : struct
    {
        protected SymbolBase(
            SourceLocation start,
            string content,
            TType type,
            IReadOnlyList<RazorError> errors)
        {
            if (content == null)
            {
                throw new ArgumentNullException(nameof(content));
            }

            Start = start;
            Content = content;
            Type = type;
            Errors = errors;
        }

        public SourceLocation Start { get; private set; }

        public IReadOnlyList<RazorError> Errors { get; }

        public string Content { get; }

        public TType Type { get; }

        public override bool Equals(object obj)
        {
            var other = obj as SymbolBase<TType>;
            return other != null &&
                Start.Equals(other.Start) &&
                string.Equals(Content, other.Content, StringComparison.Ordinal) &&
                Type.Equals(other.Type);
        }

        public override int GetHashCode()
        {
            // Hash code should include only immutable properties.
            var hashCodeCombiner = HashCodeCombiner.Start();
            hashCodeCombiner.Add(Content, StringComparer.Ordinal);
            hashCodeCombiner.Add(Type);

            return hashCodeCombiner;
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
