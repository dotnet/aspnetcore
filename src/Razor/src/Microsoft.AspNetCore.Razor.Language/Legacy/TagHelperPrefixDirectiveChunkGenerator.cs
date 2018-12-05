// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Extensions.Internal;

namespace Microsoft.AspNetCore.Razor.Language.Legacy
{
    internal class TagHelperPrefixDirectiveChunkGenerator : SpanChunkGenerator
    {
        public TagHelperPrefixDirectiveChunkGenerator(string prefix, string directiveText, List<RazorDiagnostic> diagnostics)
        {
            Prefix = prefix;
            DirectiveText = directiveText;
            Diagnostics = diagnostics;
        }

        public string Prefix { get; }

        public string DirectiveText { get; }

        public List<RazorDiagnostic> Diagnostics { get; }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            var other = obj as TagHelperPrefixDirectiveChunkGenerator;
            return base.Equals(other) &&
                Enumerable.SequenceEqual(Diagnostics, other.Diagnostics) &&
                string.Equals(Prefix, other.Prefix, StringComparison.Ordinal) &&
                string.Equals(DirectiveText, other.DirectiveText, StringComparison.Ordinal);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            var combiner = HashCodeCombiner.Start();
            combiner.Add(base.GetHashCode());
            combiner.Add(Prefix, StringComparer.Ordinal);
            combiner.Add(DirectiveText, StringComparer.Ordinal);

            return combiner.CombinedHash;
        }

        public override string ToString()
        {
            var builder = new StringBuilder("TagHelperPrefix:{");
            builder.Append(Prefix);
            builder.Append(";");
            builder.Append(DirectiveText);
            builder.Append("}");

            if (Diagnostics.Count > 0)
            {
                builder.Append(" [");
                var ids = string.Join(", ", Diagnostics.Select(diagnostic => $"{diagnostic.Id}{diagnostic.Span}"));
                builder.Append(ids);
                builder.Append("]");
            }

            return builder.ToString();
        }
    }
}