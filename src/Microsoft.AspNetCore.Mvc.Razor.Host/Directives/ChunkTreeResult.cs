// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Razor.Chunks;

namespace Microsoft.AspNetCore.Mvc.Razor.Directives
{
    /// <summary>
    /// Contains <see cref="AspNetCore.Razor.Chunks.ChunkTree"/> information.
    /// </summary>
    public class ChunkTreeResult
    {
        /// <summary>
        /// Initializes a new instance of <see cref="ChunkTreeResult"/>.
        /// </summary>
        /// <param name="chunkTree">The <see cref="AspNetCore.Razor.Chunks.ChunkTree"/> generated from the file at the
        /// given <paramref name="filePath"/>.</param>
        /// <param name="filePath">The path to the file that generated the given <paramref name="chunkTree"/>.</param>
        public ChunkTreeResult(ChunkTree chunkTree, string filePath)
        {
            if (chunkTree == null)
            {
                throw new ArgumentNullException(nameof(chunkTree));
            }

            if (filePath == null)
            {
                throw new ArgumentNullException(nameof(filePath));
            }

            ChunkTree = chunkTree;
            FilePath = filePath;
        }

        /// <summary>
        /// The <see cref="AspNetCore.Razor.Chunks.ChunkTree"/> generated from the file at <see cref="FilePath"/>.
        /// </summary>
        public ChunkTree ChunkTree { get; }

        /// <summary>
        /// The path to the file that generated the <see cref="ChunkTree"/>.
        /// </summary>
        public string FilePath { get; }
    }
}
