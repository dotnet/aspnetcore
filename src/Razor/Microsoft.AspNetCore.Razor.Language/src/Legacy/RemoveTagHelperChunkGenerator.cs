// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microsoft.AspNetCore.Razor.Language.Legacy
{
    internal class RemoveTagHelperChunkGenerator : SpanChunkGenerator
    {
        public RemoveTagHelperChunkGenerator(
            string lookupText,
            string directiveText,
            string typePattern,
            string assemblyName,
            List<RazorDiagnostic> diagnostics)
        {
            LookupText = lookupText;
            DirectiveText = directiveText;
            TypePattern = typePattern;
            AssemblyName = assemblyName;
            Diagnostics = diagnostics;
        }

        public string LookupText { get; }

        public string DirectiveText { get; set; }

        public string TypePattern { get; set; }

        public string AssemblyName { get; set; }

        public List<RazorDiagnostic> Diagnostics { get; }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            var other = obj as RemoveTagHelperChunkGenerator;
            return base.Equals(other) &&
                Enumerable.SequenceEqual(Diagnostics, other.Diagnostics) &&
                string.Equals(LookupText, other.LookupText, StringComparison.Ordinal) &&
                string.Equals(DirectiveText, other.DirectiveText, StringComparison.Ordinal) &&
                string.Equals(TypePattern, other.TypePattern, StringComparison.Ordinal) &&
                string.Equals(AssemblyName, other.AssemblyName, StringComparison.Ordinal);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            var combiner = new HashCode();
            combiner.Add(base.GetHashCode());
            combiner.Add(LookupText ?? string.Empty, StringComparer.Ordinal);
            combiner.Add(DirectiveText ?? string.Empty, StringComparer.Ordinal);
            combiner.Add(TypePattern ?? string.Empty, StringComparer.Ordinal);
            combiner.Add(AssemblyName ?? string.Empty, StringComparer.Ordinal);

            return combiner.ToHashCode();
        }

        public override string ToString()
        {
            var builder = new StringBuilder("RemoveTagHelper:{");
            builder.Append(LookupText);
            builder.Append(';');
            builder.Append(DirectiveText);
            builder.Append(';');
            builder.Append(TypePattern);
            builder.Append(';');
            builder.Append(AssemblyName);
            builder.Append('}');

            if (Diagnostics.Count > 0)
            {
                builder.Append(" [");
                var ids = string.Join(", ", Diagnostics.Select(diagnostic => $"{diagnostic.Id}{diagnostic.Span}"));
                builder.Append(ids);
                builder.Append(']');
            }

            return builder.ToString();
        }
    }
}