// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Razor.Generator.Compiler;
using Microsoft.Framework.Internal;

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
        /// <param name="defaultModelType">The type name of the model used by default.</param>
        public SetBaseTypeChunkMerger(string modelType)
        {
            _modelType = "<" + modelType + ">";
        }

        /// <inheritdoc />
        public void VisitChunk([NotNull] Chunk chunk)
        {
            var setBaseTypeChunk = ChunkHelper.EnsureChunk<SetBaseTypeChunk>(chunk);
            setBaseTypeChunk.TypeName = ChunkHelper.ReplaceTModel(setBaseTypeChunk.TypeName, _modelType);
            _isBaseTypeSet = true;
        }

        /// <inheritdoc />
        public void Merge([NotNull] CodeTree codeTree, [NotNull] Chunk chunk)
        {
            if (!_isBaseTypeSet)
            {
                var baseTypeChunk = ChunkHelper.EnsureChunk<SetBaseTypeChunk>(chunk);

                // The base type can set exactly once and the first one we encounter wins.
                _isBaseTypeSet = true;

                codeTree.Chunks.Add(TransformChunk(baseTypeChunk));
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