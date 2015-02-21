// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNet.Razor.Generator.Compiler;
using Microsoft.Framework.Internal;

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
        public InjectChunkMerger([NotNull] string modelType)
        {
            _modelType = "<" + modelType + ">";
        }

        /// <inheritdoc />
        public void VisitChunk([NotNull] Chunk chunk)
        {
            var injectChunk = ChunkHelper.EnsureChunk<InjectChunk>(chunk);
            injectChunk.TypeName = ChunkHelper.ReplaceTModel(injectChunk.TypeName, _modelType);
            _addedMemberNames.Add(injectChunk.MemberName);
        }

        /// <inheritdoc />
        public void Merge([NotNull] CodeTree codeTree, [NotNull] Chunk chunk)
        {
            var injectChunk = ChunkHelper.EnsureChunk<InjectChunk>(chunk);
            if (!_addedMemberNames.Contains(injectChunk.MemberName))
            {
                _addedMemberNames.Add(injectChunk.MemberName);
                codeTree.Chunks.Add(TransformChunk(injectChunk));
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