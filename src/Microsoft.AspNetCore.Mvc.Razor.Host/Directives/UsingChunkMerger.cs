// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
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

            var namespaceChunk = chunk as UsingChunk;
            if (namespaceChunk != null)
            {
                _currentUsings.Add(namespaceChunk.Namespace);
            }
        }

        /// <inheritdoc />
        public void MergeInheritedChunks(ChunkTree chunkTree, IReadOnlyList<Chunk> inheritedChunks)
        {
            if (chunkTree == null)
            {
                throw new ArgumentNullException(nameof(chunkTree));
            }

            if (inheritedChunks == null)
            {
                throw new ArgumentNullException(nameof(inheritedChunks));
            }

            var namespaceChunks = inheritedChunks.OfType<UsingChunk>();
            foreach (var namespaceChunk in namespaceChunks)
            {
                if (_currentUsings.Add(namespaceChunk.Namespace))
                {
                    chunkTree.Children.Add(namespaceChunk);
                }
            }
        }
    }
}