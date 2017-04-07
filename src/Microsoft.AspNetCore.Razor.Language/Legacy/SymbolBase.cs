// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.Extensions.Internal;

namespace Microsoft.AspNetCore.Razor.Language.Legacy
{
    internal abstract class SymbolBase<TType> : ISymbol where TType : struct
    {
        protected SymbolBase(
            string content,
            TType type,
            IReadOnlyList<RazorError> errors)
        {
            if (content == null)
            {
                throw new ArgumentNullException(nameof(content));
            }

            Content = content;
            Type = type;
            Errors = errors;
        }

        public Span Parent { get; set; }

        public IReadOnlyList<RazorError> Errors { get; }

        public string Content { get; }

        public TType Type { get; }

        public SourceLocation Start
        {
            get
            {
                if (Parent == null)
                {
                    return SourceLocation.Undefined;
                }

                var tracker = new SourceLocationTracker(Parent.Start);
                for (var i = 0; i < Parent.Symbols.Count; i++)
                {
                    var symbol = Parent.Symbols[i];
                    if (object.ReferenceEquals(this, symbol))
                    {
                        break;
                    }

                    tracker.UpdateLocation(symbol.Content);
                }

                return tracker.CurrentLocation;
            }
        }

        public override bool Equals(object obj)
        {
            var other = obj as SymbolBase<TType>;
            return other != null &&
                string.Equals(Content, other.Content, StringComparison.Ordinal) &&
                Type.Equals(other.Type);
        }

        public override int GetHashCode()
        {
            // Hash code should include only immutable properties.
            var hash = HashCodeCombiner.Start();
            hash.Add(Content, StringComparer.Ordinal);
            hash.Add(Type);

            return hash;
        }

        public override string ToString()
        {
            return string.Format(CultureInfo.InvariantCulture, "{0} [{1}]", Type, Content);
        }
    }
}
