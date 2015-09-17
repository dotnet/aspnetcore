// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
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

            var injectChunk = ChunkHelper.EnsureChunk<InjectChunk>(chunk);
            injectChunk.TypeName = ChunkHelper.ReplaceTModel(injectChunk.TypeName, _modelType);
            _addedMemberNames.Add(injectChunk.MemberName);
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

            var injectChunk = ChunkHelper.EnsureChunk<InjectChunk>(chunk);
            if (!_addedMemberNames.Contains(injectChunk.MemberName))
            {
                _addedMemberNames.Add(injectChunk.MemberName);
                chunkTree.Chunks.Add(TransformChunk(injectChunk));
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