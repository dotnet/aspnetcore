// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.Internal;

namespace Microsoft.AspNetCore.Razor.Evolution.Legacy
{
    internal class AddTagHelperChunkGenerator : SpanChunkGenerator
    {
        public AddTagHelperChunkGenerator(string lookupText)
        {
            LookupText = lookupText;
        }

        public string LookupText { get; }

        public override void GenerateChunk(Span target, ChunkGeneratorContext context)
        {
            //context.ChunkTreeBuilder.AddAddTagHelperChunk(LookupText, target);
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            var other = obj as AddTagHelperChunkGenerator;
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
