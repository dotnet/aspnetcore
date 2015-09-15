// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Microsoft.AspNet.FileProviders;
using Microsoft.Framework.Caching.Memory;
using Microsoft.Framework.Internal;

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
        public CompilerCache([NotNull] IFileProvider fileProvider)
        {
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
            [NotNull] IFileProvider fileProvider,
            [NotNull] IDictionary<string, Type> precompiledViews)
            : this(fileProvider)
        {
            foreach (var item in precompiledViews)
            {
                var cacheEntry = new CompilerCacheResult(CompilationResult.Successful(item.Value));
                _cache.Set(GetNormalizedPath(item.Key), cacheEntry);
            }
        }

        /// <inheritdoc />
        public CompilerCacheResult GetOrAdd(
            [NotNull] string relativePath,
            [NotNull] Func<RelativeFileInfo, CompilationResult> compile)
        {
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
            CompilerCacheResult cacheResult;
            var fileInfo = _fileProvider.GetFileInfo(normalizedPath);
            MemoryCacheEntryOptions cacheEntryOptions;
            CompilerCacheResult cacheResultToCache;
            if (!fileInfo.Exists)
            {
                cacheResultToCache = CompilerCacheResult.FileNotFound;
                cacheResult = CompilerCacheResult.FileNotFound;

                cacheEntryOptions = new MemoryCacheEntryOptions();
                cacheEntryOptions.AddExpirationTrigger(_fileProvider.Watch(normalizedPath));
            }
            else
            {
                var relativeFileInfo = new RelativeFileInfo(fileInfo, normalizedPath);
                var compilationResult = compile(relativeFileInfo).EnsureSuccessful();
                cacheEntryOptions = GetMemoryCacheEntryOptions(normalizedPath);

                // By default the CompilationResult returned by IRoslynCompiler is an instance of
                // UncachedCompilationResult. This type has the generated code as a string property and do not want
                // to cache it. We'll instead cache the unwrapped result.
                cacheResultToCache = new CompilerCacheResult(
                    CompilationResult.Successful(compilationResult.CompiledType));
                cacheResult = new CompilerCacheResult(compilationResult);
            }

            _cache.Set(normalizedPath, cacheResultToCache, cacheEntryOptions);
            return cacheResult;
        }

        private MemoryCacheEntryOptions GetMemoryCacheEntryOptions(string relativePath)
        {
            var options = new MemoryCacheEntryOptions();
            options.AddExpirationTrigger(_fileProvider.Watch(relativePath));

            var viewImportsPaths = ViewHierarchyUtility.GetViewImportsLocations(relativePath);
            foreach (var location in viewImportsPaths)
            {
                options.AddExpirationTrigger(_fileProvider.Watch(location));
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
