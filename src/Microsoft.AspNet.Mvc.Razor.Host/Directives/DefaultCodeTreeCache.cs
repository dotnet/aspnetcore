// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.FileProviders;
using Microsoft.AspNet.Razor.Generator.Compiler;
using Microsoft.Framework.Cache.Memory;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Mvc.Razor.Directives
{
    /// <summary>
    /// Default implementation of <see cref="ICodeTreeCache"/>.
    /// </summary>
    public class DefaultCodeTreeCache : ICodeTreeCache
    {
        private static readonly MemoryCacheOptions MemoryCacheOptions = new MemoryCacheOptions
        {
            ListenForMemoryPressure = false
        };
        private static readonly TimeSpan SlidingExpirationDuration = TimeSpan.FromMinutes(1);
        private readonly IFileProvider _fileProvider;
        private readonly IMemoryCache _codeTreeCache;

        /// <summary>
        /// Initializes a new instance of <see cref="DefaultCodeTreeCache"/>.
        /// </summary>
        /// <param name="fileProvider">The application's <see cref="IFileProvider"/>.</param>
        public DefaultCodeTreeCache(IFileProvider fileProvider)
            : this(fileProvider, MemoryCacheOptions)
        {
        }

        // Internal for unit testing
        internal DefaultCodeTreeCache(IFileProvider fileProvider,
                                      MemoryCacheOptions options)
        {
            _fileProvider = fileProvider;
            _codeTreeCache = new MemoryCache(options);
        }

        /// <inheritdoc />
        public CodeTree GetOrAdd([NotNull] string pagePath,
                                 [NotNull] Func<IFileInfo, CodeTree> getCodeTree)
        {
            return _codeTreeCache.GetOrSet(pagePath, getCodeTree, OnCacheMiss);
        }

        private CodeTree OnCacheMiss(ICacheSetContext cacheSetContext)
        {
            var pagePath = cacheSetContext.Key;
            var getCodeTree = (Func<IFileInfo, CodeTree>)cacheSetContext.State;

            // GetOrAdd is invoked for each _ViewStart that might potentially exist in the path.
            // We can avoid performing file system lookups for files that do not exist by caching
            // negative results and adding a Watch for that file.
            cacheSetContext.AddExpirationTrigger(_fileProvider.Watch(pagePath));
            cacheSetContext.SetSlidingExpiration(SlidingExpirationDuration);

            var file = _fileProvider.GetFileInfo(pagePath);
            return file.Exists ? getCodeTree(file) : null;
        }
    }
}