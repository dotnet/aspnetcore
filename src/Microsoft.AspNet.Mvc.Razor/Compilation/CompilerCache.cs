// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.AspNet.FileProviders;
using Microsoft.AspNet.Mvc.Razor.Precompilation;
using Microsoft.Dnx.Runtime;
using Microsoft.Framework.Caching.Memory;
using Microsoft.Framework.Internal;
using Microsoft.Framework.OptionsModel;

namespace Microsoft.AspNet.Mvc.Razor.Compilation
{
    /// <summary>
    /// Caches the result of runtime compilation of Razor files for the duration of the application lifetime.
    /// </summary>
    public class CompilerCache : ICompilerCache
    {
        private static readonly Assembly RazorHostAssembly = typeof(CompilerCache).GetTypeInfo().Assembly;
        private readonly IFileProvider _fileProvider;
        private readonly IMemoryCache _cache;

        /// <summary>
        /// Initializes a new instance of <see cref="CompilerCache"/> populated with precompiled views
        /// discovered using <paramref name="provider"/>.
        /// </summary>
        /// <param name="razorFileInfoCollections">The sequence of <see cref="RazorFileInfoCollection"/> that provides
        /// information for precompiled view discovery.</param>
        /// <param name="loaderContextAccessor">The <see cref="IAssemblyLoadContextAccessor"/>.</param>
        /// <param name="optionsAccessor">An accessor to the <see cref="RazorViewEngineOptions"/>.</param>
        public CompilerCache(
            IEnumerable<RazorFileInfoCollection> razorFileInfoCollections,
            IAssemblyLoadContextAccessor loadContextAccessor,
            IOptions<RazorViewEngineOptions> optionsAccessor)
            : this(razorFileInfoCollections,
                  loadContextAccessor.GetLoadContext(RazorHostAssembly),
                  optionsAccessor.Options.FileProvider)
        {
        }

        internal CompilerCache(
            IEnumerable<RazorFileInfoCollection> razorFileInfoCollections,
            IAssemblyLoadContext loadContext,
            IFileProvider fileProvider)
        {
            _fileProvider = fileProvider;
            _cache = new MemoryCache(new MemoryCacheOptions { CompactOnMemoryPressure = false });

            foreach (var viewCollection in razorFileInfoCollections)
            {
                var containingAssembly = viewCollection.LoadAssembly(loadContext);
                foreach (var fileInfo in viewCollection.FileInfos)
                {
                    var viewType = containingAssembly.GetType(fileInfo.FullTypeName);
                    var cacheEntry = new CompilerCacheResult(CompilationResult.Successful(viewType));
                    var normalizedPath = NormalizePath(fileInfo.RelativePath);
                    _cache.Set(normalizedPath, cacheEntry);
                }
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
