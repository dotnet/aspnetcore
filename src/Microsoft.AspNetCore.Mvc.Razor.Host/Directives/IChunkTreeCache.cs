// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Razor.Chunks;
using Microsoft.Extensions.FileProviders;

namespace Microsoft.AspNetCore.Mvc.Razor.Directives
{
    /// <summary>
    /// A cache for parsed <see cref="ChunkTree"/>s.
    /// </summary>
    public interface IChunkTreeCache : IDisposable
    {
        /// <summary>
        /// Get an existing <see cref="ChunkTree"/>, or create and add a new one if it is
        /// not available in the cache or is expired.
        /// </summary>
        /// <param name="pagePath">The application relative path of the Razor page.</param>
        /// <param name="getChunkTree">A delegate that creates a new <see cref="ChunkTree"/>.</param>
        /// <returns>The <see cref="ChunkTree"/> if a file exists at <paramref name="pagePath"/>,
        /// <c>null</c> otherwise.</returns>
        /// <remarks>The resulting <see cref="ChunkTree"/> does not contain inherited chunks from _ViewStart or
        /// default inherited chunks.</remarks>
        ChunkTree GetOrAdd(string pagePath, Func<IFileInfo, ChunkTree> getChunkTree);
    }
}