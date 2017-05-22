// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Internal;

namespace Microsoft.AspNetCore.Razor.Language.Legacy
{
    internal class AddTagHelperChunkGenerator : SpanChunkGenerator
    {
        public AddTagHelperChunkGenerator(string lookupText, List<RazorDiagnostic> diagnostics)
        {
            LookupText = lookupText;
            Diagnostics = diagnostics;
        }

        public string LookupText { get; }

        public List<RazorDiagnostic> Diagnostics { get; }

        public override void Accept(ParserVisitor visitor, Span span)
        {
            visitor.VisitAddTagHelperSpan(this, span);
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            var other = obj as AddTagHelperChunkGenerator;
            return base.Equals(other) &&
                Enumerable.SequenceEqual(Diagnostics, other.Diagnostics) &&
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
