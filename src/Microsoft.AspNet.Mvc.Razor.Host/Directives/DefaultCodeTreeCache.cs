// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.FileProviders;
using Microsoft.AspNet.Razor.Generator.Compiler;
using Microsoft.Framework.Caching.Memory;
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
            CompactOnMemoryPressure = false
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
            CodeTree codeTree;
            if (!_codeTreeCache.TryGetValue(pagePath, out codeTree))
            {
                // GetOrAdd is invoked for each _GlobalImport that might potentially exist in the path.
                // We can avoid performing file system lookups for files that do not exist by caching
                // negative results and adding a Watch for that file.

                var options = new MemoryCacheEntryOptions()
                    .AddExpirationTrigger(_fileProvider.Watch(pagePath))
                    .SetSlidingExpiration(SlidingExpirationDuration);

                var file = _fileProvider.GetFileInfo(pagePath);
                codeTree = file.Exists ? getCodeTree(file) : null;


                _codeTreeCache.Set(pagePath, codeTree, options);
            }

            return codeTree;
        }
    }
}