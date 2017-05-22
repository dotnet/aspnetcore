// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Internal;

namespace Microsoft.AspNetCore.Razor.Language.Legacy
{
    internal class TagHelperPrefixDirectiveChunkGenerator : SpanChunkGenerator
    {
        public TagHelperPrefixDirectiveChunkGenerator(string prefix, List<RazorDiagnostic> diagnostics)
        {
            Prefix = prefix;
            Diagnostics = diagnostics;
        }

        public string Prefix { get; }

        public List<RazorDiagnostic> Diagnostics { get; }

        public override void Accept(ParserVisitor visitor, Span span)
        {
            visitor.VisitTagHelperPrefixDirectiveSpan(this, span);
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            var other = obj as TagHelperPrefixDirectiveChunkGenerator;
            return base.Equals(other) &&
                Enumerable.SequenceEqual(Diagnostics, other.Diagnostics) &&
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