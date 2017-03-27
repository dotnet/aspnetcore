// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Razor.Compilation;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.FileProviders;

namespace Microsoft.AspNetCore.Mvc.Razor.Internal
{
    /// <summary>
    /// Caches the result of runtime compilation of Razor files for the duration of the application lifetime.
    /// </summary>
    public class CompilerCache : ICompilerCache
    {
        private readonly IFileProvider _fileProvider;
        private readonly IMemoryCache _cache;
        private readonly object _cacheLock = new object();

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
            _cache = new MemoryCache(new MemoryCacheOptions());
        }

        /// <summary>
        /// Initializes a new instance of <see cref="CompilerCache"/> populated with precompiled views
        /// specified by <paramref name="views"/>.
        /// </summary>
        /// <param name="fileProvider"><see cref="IFileProvider"/> used to locate Razor views.</param>
        /// <param name="views">A mapping of application relative paths of view to <see cref="Type"/>s that
        /// have already been compiled.</param>
        public CompilerCache(
            IFileProvider fileProvider,
            IDictionary<string, Type> views)
            : this(fileProvider)
        {
            if (views == null)
            {
                throw new ArgumentNullException(nameof(views));
            }

            foreach (var item in views)
            {
                var cacheEntry = new CompilerCacheResult(item.Key, new CompilationResult(item.Value), isPrecompiled: true);
                _cache.Set(GetNormalizedPath(item.Key), Task.FromResult(cacheEntry));
            }
        }

        /// <inheritdoc />
        public CompilerCacheResult GetOrAdd(
            string relativePath,
            Func<string, CompilerCacheContext> cacheContextFactory)
        {
            if (relativePath == null)
            {
                throw new ArgumentNullException(nameof(relativePath));
            }

            if (cacheContextFactory == null)
            {
                throw new ArgumentNullException(nameof(cacheContextFactory));
            }

            Task<CompilerCacheResult> cacheEntry;
            // Attempt to lookup the cache entry using the passed in path. This will succeed if the path is already
            // normalized and a cache entry exists.
            if (!_cache.TryGetValue(relativePath, out cacheEntry))
            {
                var normalizedPath = GetNormalizedPath(relativePath);
                if (!_cache.TryGetValue(normalizedPath, out cacheEntry))
                {
                    cacheEntry = CreateCacheEntry(normalizedPath, cacheContextFactory);
                }
            }

            // The Task does not represent async work and is meant to provide per-entry locking.
            // Hence it is ok to perform .GetResult() to read the result.
            return cacheEntry.GetAwaiter().GetResult();
        }

        private Task<CompilerCacheResult> CreateCacheEntry(
            string normalizedPath,
            Func<string, CompilerCacheContext> cacheContextFactory)
        {
            TaskCompletionSource<CompilerCacheResult> compilationTaskSource = null;
            MemoryCacheEntryOptions cacheEntryOptions;
            Task<CompilerCacheResult> cacheEntry;
            CompilerCacheContext compilerCacheContext;

            // Safe races cannot be allowed when compiling Razor pages. To ensure only one compilation request succeeds
            // per file, we'll lock the creation of a cache entry. Creating the cache entry should be very quick. The
            // actual work for compiling files happens outside the critical section.
            lock (_cacheLock)
            {
                if (_cache.TryGetValue(normalizedPath, out cacheEntry))
                {
                    return cacheEntry;
                }

                if (_fileProvider is NullFileProvider)
                {
                    var message = Resources.FormatFileProvidersAreRequired(
                        typeof(RazorViewEngineOptions).FullName,
                        nameof(RazorViewEngineOptions.FileProviders),
                        typeof(IFileProvider).FullName);
                    throw new InvalidOperationException(message);
                }

                cacheEntryOptions = new MemoryCacheEntryOptions();

                compilerCacheContext = cacheContextFactory(normalizedPath);
                cacheEntryOptions.ExpirationTokens.Add(_fileProvider.Watch(compilerCacheContext.ProjectItem.Path));
                if (!compilerCacheContext.ProjectItem.Exists)
                {
                    cacheEntry = Task.FromResult(new CompilerCacheResult(normalizedPath, cacheEntryOptions.ExpirationTokens));
                }
                else
                {
                    // A file exists and needs to be compiled.
                    compilationTaskSource = new TaskCompletionSource<CompilerCacheResult>();
                    foreach (var projectItem in compilerCacheContext.AdditionalCompilationItems)
                    {
                        cacheEntryOptions.ExpirationTokens.Add(_fileProvider.Watch(projectItem.Path));
                    }
                    cacheEntry = compilationTaskSource.Task;
                }

                cacheEntry = _cache.Set(normalizedPath, cacheEntry, cacheEntryOptions);
            }

            if (compilationTaskSource != null)
            {
                // Indicates that a file was found and needs to be compiled.
                Debug.Assert(cacheEntryOptions != null);

                try
                {
                    var compilationResult = compilerCacheContext.Compile(compilerCacheContext);
                    compilationResult.EnsureSuccessful();
                    compilationTaskSource.SetResult(
                        new CompilerCacheResult(normalizedPath, compilationResult, cacheEntryOptions.ExpirationTokens));
                }
                catch (Exception ex)
                {
                    compilationTaskSource.SetException(ex);
                }
            }

            return cacheEntry;
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
