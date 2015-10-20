// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Microsoft.AspNet.FileProviders;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNet.Mvc.Razor.Compilation
{
    /// <summary>
    /// Caches the result of runtime compilation of Razor files for the duration of the application lifetime.
    /// </summary>
    public class CompilerCache : ICompilerCache
    {
        private readonly IFileProvider _fileProvider;
        private readonly IMemoryCache _cache;

        private readonly ConcurrentDictionary<string, string> _normalizedPathLookup =
            new ConcurrentDictionary<string, string>(StringComparer.Ordinal);

        /// <summary>
        /// Initializes a new instance of <see cref="CompilerCache"/>.
        /// </summary>
        /// <param name="fileProvider"><see cref="IFileProvider"/> used to locate Razor views.</param>
        public CompilerCache(IFileProvider fileProvider)
        {
            if (fileProvider == null)
            {
                throw new ArgumentNullException(nameof(fileProvider));
            }

            _fileProvider = fileProvider;
            _cache = new MemoryCache(new MemoryCacheOptions { CompactOnMemoryPressure = false });
        }

        /// <summary>
        /// Initializes a new instance of <see cref="CompilerCache"/> populated with precompiled views
        /// specified by <paramref name="precompiledViews"/>.
        /// </summary>
        /// <param name="fileProvider"><see cref="IFileProvider"/> used to locate Razor views.</param>
        /// <param name="precompiledViews">A mapping of application relative paths of view to the precompiled view
        /// <see cref="Type"/>s.</param>
        public CompilerCache(
            IFileProvider fileProvider,
            IDictionary<string, Type> precompiledViews)
            : this(fileProvider)
        {
            if (precompiledViews == null)
            {
                throw new ArgumentNullException(nameof(precompiledViews));
            }

            foreach (var item in precompiledViews)
            {
                var cacheEntry = new CompilerCacheResult(new CompilationResult(item.Value));
                _cache.Set(GetNormalizedPath(item.Key), cacheEntry);
            }
        }

        /// <inheritdoc />
        public CompilerCacheResult GetOrAdd(
            string relativePath,
            Func<RelativeFileInfo, CompilationResult> compile)
        {
            if (relativePath == null)
            {
                throw new ArgumentNullException(nameof(relativePath));
            }

            if (compile == null)
            {
                throw new ArgumentNullException(nameof(compile));
            }

            CompilerCacheResult cacheResult;
            // Attempt to lookup the cache entry using the passed in path. This will succeed if the path is already
            // normalized and a cache entry exists.
            if (!_cache.TryGetValue(relativePath, out cacheResult))
            {
                var normalizedPath = GetNormalizedPath(relativePath);
                if (!_cache.TryGetValue(normalizedPath, out cacheResult))
                {
                    cacheResult = CreateCacheEntry(normalizedPath, compile);
                }
            }

            return cacheResult;
        }

        private CompilerCacheResult CreateCacheEntry(
            string normalizedPath,
            Func<RelativeFileInfo, CompilationResult> compile)
        {
            var fileInfo = _fileProvider.GetFileInfo(normalizedPath);
            MemoryCacheEntryOptions cacheEntryOptions;
            CompilerCacheResult cacheResult;
            if (!fileInfo.Exists)
            {
                var expirationToken = _fileProvider.Watch(normalizedPath);
                cacheResult = new CompilerCacheResult(new[] { expirationToken });

                cacheEntryOptions = new MemoryCacheEntryOptions();
                cacheEntryOptions.AddExpirationToken(expirationToken);
            }
            else
            {
                var relativeFileInfo = new RelativeFileInfo(fileInfo, normalizedPath);
                var compilationResult = compile(relativeFileInfo);
                compilationResult.EnsureSuccessful();
                cacheEntryOptions = GetMemoryCacheEntryOptions(normalizedPath);
                cacheResult = new CompilerCacheResult(
                    compilationResult,
                    cacheEntryOptions.ExpirationTokens);
            }

            _cache.Set(normalizedPath, cacheResult, cacheEntryOptions);
            return cacheResult;
        }

        private MemoryCacheEntryOptions GetMemoryCacheEntryOptions(string relativePath)
        {
            var options = new MemoryCacheEntryOptions();
            options.AddExpirationToken(_fileProvider.Watch(relativePath));

            var viewImportsPaths = ViewHierarchyUtility.GetViewImportsLocations(relativePath);
            foreach (var location in viewImportsPaths)
            {
                options.AddExpirationToken(_fileProvider.Watch(location));
            }

            return options;
        }

        private string GetNormalizedPath(string relativePath)
        {
            Debug.Assert(relativePath != null);
            if (relativePath.Length == 0)
            {
                return relativePath;
            }

            string normalizedPath;
            if (!_normalizedPathLookup.TryGetValue(relativePath, out normalizedPath))
            {
                var builder = new StringBuilder(relativePath);
                builder.Replace('\\', '/');
                if (builder[0] != '/')
                {
                    builder.Insert(0, '/');
                }
                normalizedPath = builder.ToString();
                _normalizedPathLookup.TryAdd(relativePath, normalizedPath);
            }

            return normalizedPath;
        }
    }
}
