// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.Internal;

namespace Microsoft.AspNetCore.Razor.Language.Legacy
{
    internal class TagHelperPrefixDirectiveChunkGenerator : SpanChunkGenerator
    {
        public TagHelperPrefixDirectiveChunkGenerator(string prefix)
        {
            Prefix = prefix;
        }

        public string Prefix { get; }

        public override void Accept(ParserVisitor visitor, Span span)
        {
            visitor.VisitTagHelperPrefixDirectiveSpan(this, span);
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            var other = obj as TagHelperPrefixDirectiveChunkGenerator;
            return base.Equals(other) &&
                string.Equals(Prefix, other.Prefix, StringComparison.Ordinal);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            var combiner = HashCodeCombiner.Start();
            combiner.Add(base.GetHashCode());
            combiner.Add(Prefix, StringComparer.Ordinal);

            return combiner.CombinedHash;
        }
    }
}