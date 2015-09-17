// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNet.Razor.Chunks;

namespace Microsoft.AspNet.Mvc.Razor.Directives
{
    /// <summary>
    /// A <see cref="IChunkMerger"/> that merges <see cref="UsingChunk"/> instances.
    /// </summary>
    public class UsingChunkMerger : IChunkMerger
    {
        private readonly HashSet<string> _currentUsings = new HashSet<string>(StringComparer.Ordinal);

        /// <inheritdoc />
        public void VisitChunk(Chunk chunk)
        {
            if (chunk == null)
            {
                throw new ArgumentNullException(nameof(chunk));
            }

            var namespaceChunk = ChunkHelper.EnsureChunk<UsingChunk>(chunk);
            _currentUsings.Add(namespaceChunk.Namespace);
        }

        /// <inheritdoc />
        public void Merge(ChunkTree chunkTree, Chunk chunk)
        {
            if (chunkTree == null)
            {
                throw new ArgumentNullException(nameof(chunkTree));
            }

            if (chunk == null)
            {
                throw new ArgumentNullException(nameof(chunk));
            }

            var namespaceChunk = ChunkHelper.EnsureChunk<UsingChunk>(chunk);

            if (!_currentUsings.Contains(namespaceChunk.Namespace))
            {
                _currentUsings.Add(namespaceChunk.Namespace);
                chunkTree.Chunks.Add(namespaceChunk);
            }
        }
    }
}