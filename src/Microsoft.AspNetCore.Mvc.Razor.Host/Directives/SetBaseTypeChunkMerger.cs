// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNet.Razor.Chunks;

namespace Microsoft.AspNet.Mvc.Razor.Directives
{
    /// <summary>
    /// A <see cref="IChunkMerger"/> that merges <see cref="SetBaseTypeChunk"/> instances.
    /// </summary>
    public class SetBaseTypeChunkMerger : IChunkMerger
    {
        private readonly string _modelType;
        private bool _isBaseTypeSet;

        /// <summary>
        /// Initializes a new instance of <see cref="SetBaseTypeChunkMerger"/>.
        /// </summary>
        /// <param name="modelType">The type name of the model used by default.</param>
        public SetBaseTypeChunkMerger(string modelType)
        {
            _modelType = "<" + modelType + ">";
        }

        /// <inheritdoc />
        public void VisitChunk(Chunk chunk)
        {
            if (chunk == null)
            {
                throw new ArgumentNullException(nameof(chunk));
            }

            var setBaseTypeChunk = chunk as SetBaseTypeChunk;
            if (setBaseTypeChunk != null)
            {
                setBaseTypeChunk.TypeName = ChunkHelper.ReplaceTModel(setBaseTypeChunk.TypeName, _modelType);
                _isBaseTypeSet = true;
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

            if (!_isBaseTypeSet)
            {
                for (var i = inheritedChunks.Count - 1; i >= 0; i--)
                {
                    var baseTypeChunk = inheritedChunks[i] as SetBaseTypeChunk;
                    if (baseTypeChunk != null)
                    {
                        chunkTree.Children.Add(TransformChunk(baseTypeChunk));
                        break;
                    }
                }
            }
        }

        private SetBaseTypeChunk TransformChunk(SetBaseTypeChunk setBaseTypeChunk)
        {
            var typeName = ChunkHelper.ReplaceTModel(setBaseTypeChunk.TypeName, _modelType);
            if (typeName != setBaseTypeChunk.TypeName)
            {
                return new SetBaseTypeChunk
                {
                    TypeName = typeName,
                    Start = setBaseTypeChunk.Start,
                    Association = setBaseTypeChunk.Association
                };
            }
            return setBaseTypeChunk;
        }
    }
}