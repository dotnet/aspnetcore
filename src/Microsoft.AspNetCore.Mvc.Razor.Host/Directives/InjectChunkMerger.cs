// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNet.Razor.Chunks;

namespace Microsoft.AspNet.Mvc.Razor.Directives
{
    /// <summary>
    /// A <see cref="IChunkMerger"/> that merges <see cref="InjectChunk"/> instances.
    /// </summary>
    public class InjectChunkMerger : IChunkMerger
    {
        private readonly HashSet<string> _addedMemberNames = new HashSet<string>(StringComparer.Ordinal);
        private string _modelType;

        /// <summary>
        /// Initializes a new instance of <see cref="InjectChunkMerger"/>.
        /// </summary>
        /// <param name="modelType">The model type to be used to replace &lt;TModel&gt; tokens.</param>
        public InjectChunkMerger(string modelType)
        {
            if (modelType == null)
            {
                throw new ArgumentNullException(nameof(modelType));
            }

            _modelType = "<" + modelType + ">";
        }

        /// <inheritdoc />
        public void VisitChunk(Chunk chunk)
        {
            if (chunk == null)
            {
                throw new ArgumentNullException(nameof(chunk));
            }

            var injectChunk = chunk as InjectChunk;
            if (injectChunk != null)
            {
                injectChunk.TypeName = ChunkHelper.ReplaceTModel(injectChunk.TypeName, _modelType);
                _addedMemberNames.Add(injectChunk.MemberName);
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

            for (var i = inheritedChunks.Count - 1; i >= 0; i--)
            {
                var injectChunk = inheritedChunks[i] as InjectChunk;
                if (injectChunk != null &&
                    _addedMemberNames.Add(injectChunk.MemberName))
                {
                    chunkTree.Children.Add(TransformChunk(injectChunk));
                }
            }
        }

        private InjectChunk TransformChunk(InjectChunk injectChunk)
        {
            var typeName = ChunkHelper.ReplaceTModel(injectChunk.TypeName, _modelType);
            if (typeName != injectChunk.TypeName)
            {
                return new InjectChunk(typeName, injectChunk.MemberName)
                {
                    Start = injectChunk.Start,
                    Association = injectChunk.Association
                };
            }
            return injectChunk;
        }
    }
}