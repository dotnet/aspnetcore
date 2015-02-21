// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNet.Razor.Generator.Compiler;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Mvc.Razor.Directives
{
    /// <summary>
    /// A <see cref="IChunkMerger"/> that merges <see cref="UsingChunk"/> instances.
    /// </summary>
    public class UsingChunkMerger : IChunkMerger
    {
        private readonly HashSet<string> _currentUsings = new HashSet<string>(StringComparer.Ordinal);

        /// <inheritdoc />
        public void VisitChunk([NotNull] Chunk chunk)
        {
            var namespaceChunk = ChunkHelper.EnsureChunk<UsingChunk>(chunk);
            _currentUsings.Add(namespaceChunk.Namespace);
        }

        /// <inheritdoc />
        public void Merge([NotNull] CodeTree codeTree, [NotNull] Chunk chunk)
        {
            var namespaceChunk = ChunkHelper.EnsureChunk<UsingChunk>(chunk);

            if (!_currentUsings.Contains(namespaceChunk.Namespace))
            {
                _currentUsings.Add(namespaceChunk.Namespace);
                codeTree.Chunks.Add(namespaceChunk);
            }
        }
    }
}