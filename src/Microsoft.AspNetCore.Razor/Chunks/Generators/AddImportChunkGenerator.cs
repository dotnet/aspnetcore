// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Razor.Parser.SyntaxTree;

namespace Microsoft.AspNet.Razor.Chunks.Generators
{
    public class AddImportChunkGenerator : SpanChunkGenerator
    {
        public AddImportChunkGenerator(string ns)
        {
            Namespace = ns;
        }

        public string Namespace { get; }

        public override void GenerateChunk(Span target, ChunkGeneratorContext context)
        {
            var ns = Namespace;

            if (!string.IsNullOrEmpty(ns) && char.IsWhiteSpace(ns[0]))
            {
                ns = ns.Substring(1);
            }

            context.ChunkTreeBuilder.AddUsingChunk(ns, target);
        }

        public override string ToString()
        {
            return "Import:" + Namespace + ";";
        }

        public override bool Equals(object obj)
        {
            var other = obj as AddImportChunkGenerator;
            return other != null &&
                string.Equals(Namespace, other.Namespace, StringComparison.Ordinal);
        }

        public override int GetHashCode()
        {
            // Hash code should include only immutable properties.
            return Namespace == null ? 0 : StringComparer.Ordinal.GetHashCode(Namespace);
        }
    }
}
