// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Razor.Chunks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.FileProviders;

namespace Microsoft.AspNetCore.Mvc.Razor.Directives
{
    /// <summary>
    /// Default implementation of <see cref="IChunkTreeCache"/>.
    /// </summary>
    public class DefaultChunkTreeCache : IChunkTreeCache
    {
        private static readonly MemoryCacheOptions MemoryCacheOptions = new MemoryCacheOptions
        {
            CompactOnMemoryPressure = false
        };
        private static readonly TimeSpan SlidingExpirationDuration = TimeSpan.FromMinutes(1);
        private readonly IFileProvider _fileProvider;
        private readonly IMemoryCache _chunkTreeCache;

        /// <summary>
        /// Initializes a new instance of <see cref="DefaultChunkTreeCache"/>.
        /// </summary>
        /// <param name="fileProvider">The application's <see cref="IFileProvider"/>.</param>
        public DefaultChunkTreeCache(IFileProvider fileProvider)
            : this(fileProvider, MemoryCacheOptions)
        {
        }

        // Internal for unit testing
        internal DefaultChunkTreeCache(
            IFileProvider fileProvider,
            MemoryCacheOptions options)
        {
            _fileProvider = fileProvider;
            _chunkTreeCache = new MemoryCache(options);
        }

        /// <inheritdoc />
        public ChunkTree GetOrAdd(
            string pagePath,
            Func<IFileInfo, ChunkTree> getChunkTree)
        {
            if (pagePath == null)
            {
                throw new ArgumentNullException(nameof(pagePath));
            }

            if (getChunkTree == null)
            {
                throw new ArgumentNullException(nameof(getChunkTree));
            }

            ChunkTree chunkTree;
            if (!_chunkTreeCache.TryGetValue(pagePath, out chunkTree))
            {
                // GetOrAdd is invoked for each _ViewImport that might potentially exist in the path.
                // We can avoid performing file system lookups for files that do not exist by caching
                // negative results and adding a Watch for that file.

                var options = new MemoryCacheEntryOptions()
                    .AddExpirationToken(_fileProvider.Watch(pagePath))
                    .SetSlidingExpiration(SlidingExpirationDuration);

                var file = _fileProvider.GetFileInfo(pagePath);
                chunkTree = file.Exists ? getChunkTree(file) : null;

                _chunkTreeCache.Set(pagePath, chunkTree, options);
            }

            return chunkTree;
        }

        public void Dispose()
        {
            _chunkTreeCache.Dispose();
        }
    }
}