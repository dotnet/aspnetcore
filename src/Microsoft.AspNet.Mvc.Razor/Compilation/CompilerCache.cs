// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
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
                var normalizedPath = NormalizePath(item.Key);
                _cache.Set(normalizedPath, cacheEntry);
            }
        }

        /// <inheritdoc />
        public CompilerCacheResult GetOrAdd(
            [NotNull] string relativePath,
            [NotNull] Func<RelativeFileInfo, CompilationResult> compile)
        {
            var normalizedPath = NormalizePath(relativePath);
            CompilerCacheResult cacheResult;
            if (!_cache.TryGetValue(normalizedPath, out cacheResult))
            {
                var fileInfo = _fileProvider.GetFileInfo(relativePath);
                MemoryCacheEntryOptions cacheEntryOptions;
                CompilerCacheResult cacheResultToCache;
                if (!fileInfo.Exists)
                {
                    cacheResultToCache = CompilerCacheResult.FileNotFound;
                    cacheResult = CompilerCacheResult.FileNotFound;

                    cacheEntryOptions = new MemoryCacheEntryOptions();
                    cacheEntryOptions.AddExpirationTrigger(_fileProvider.Watch(relativePath));
                }
                else
                {
                    var relativeFileInfo = new RelativeFileInfo(fileInfo, relativePath);
                    var compilationResult = compile(relativeFileInfo).EnsureSuccessful();
                    cacheEntryOptions = GetMemoryCacheEntryOptions(relativePath);

                    // By default the CompilationResult returned by IRoslynCompiler is an instance of
                    // UncachedCompilationResult. This type has the generated code as a string property and do not want
                    // to cache it. We'll instead cache the unwrapped result.
                    cacheResultToCache = new CompilerCacheResult(
                        CompilationResult.Successful(compilationResult.CompiledType));
                    cacheResult = new CompilerCacheResult(compilationResult);
                }

                _cache.Set(normalizedPath, cacheResultToCache, cacheEntryOptions);
            }

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

        private static string NormalizePath(string path)
        {
            // We need to allow for scenarios where the application was precompiled on a machine with forward slashes
            // but is being run in one with backslashes (or vice versa). To this effect, we'll normalize paths to
            // use backslashes for lookups and storage in the dictionary.
            path = path.Replace('/', '\\');
            path = path.TrimStart('\\');

            return path;
        }
    }
}
