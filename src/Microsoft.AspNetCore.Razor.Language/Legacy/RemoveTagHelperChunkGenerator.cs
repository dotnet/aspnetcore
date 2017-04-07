// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.Internal;

namespace Microsoft.AspNetCore.Razor.Language.Legacy
{
    internal class RemoveTagHelperChunkGenerator : SpanChunkGenerator
    {
        public RemoveTagHelperChunkGenerator(string lookupText)
        {
            LookupText = lookupText;
        }

        public string LookupText { get; }

        public override void Accept(ParserVisitor visitor, Span span)
        {
            visitor.VisitRemoveTagHelperSpan(this, span);
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            var other = obj as RemoveTagHelperChunkGenerator;
            return base.Equals(other) &&
                string.Equals(LookupText, other.LookupText, StringComparison.Ordinal);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            var combiner = HashCodeCombiner.Start();
            combiner.Add(base.GetHashCode());
            combiner.Add(LookupText, StringComparer.Ordinal);

            return combiner.CombinedHash;
        }
    }
}