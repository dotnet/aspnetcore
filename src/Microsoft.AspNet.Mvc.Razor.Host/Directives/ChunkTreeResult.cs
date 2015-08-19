// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Razor.Chunks;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Mvc.Razor.Directives
{
    /// <summary>
    /// Contains <see cref="AspNet.Razor.Chunks.ChunkTree"/> information.
    /// </summary>
    public class ChunkTreeResult
    {
        /// <summary>
        /// Initializes a new instance of <see cref="ChunkTreeResult"/>.
        /// </summary>
        /// <param name="chunkTree">The <see cref="AspNet.Razor.Chunks.ChunkTree"/> generated from the file at the
        /// given <paramref name="filePath"/>.</param>
        /// <param name="filePath">The path to the file that generated the given <paramref name="chunkTree"/>.</param>
        public ChunkTreeResult([NotNull] ChunkTree chunkTree, [NotNull] string filePath)
        {
            ChunkTree = chunkTree;
            FilePath = filePath;
        }

        /// <summary>
        /// The <see cref="AspNet.Razor.Chunks.ChunkTree"/> generated from the file at <see cref="FilePath"/>.
        /// </summary>
        public ChunkTree ChunkTree { get; }

        /// <summary>
        /// The path to the file that generated the <see cref="ChunkTree"/>.
        /// </summary>
        public string FilePath { get; }
    }
}
