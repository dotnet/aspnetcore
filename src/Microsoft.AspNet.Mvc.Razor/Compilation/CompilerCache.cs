// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Microsoft.AspNet.Mvc.Razor
{
    public class CompilerCache
    {
        private readonly ConcurrentDictionary<string, CompilerCacheEntry> _cache;
        private static readonly Type[] EmptyType = new Type[0];

        public CompilerCache([NotNull] IEnumerable<Assembly> assemblies)
            : this(GetFileInfos(assemblies))
        {
        }

        internal CompilerCache(IEnumerable<RazorFileInfoCollection> viewCollections) : this()
        {
            foreach (var viewCollection in viewCollections)
            {
                foreach (var fileInfo in viewCollection.FileInfos)
                {
                    var containingAssembly = viewCollection.GetType().GetTypeInfo().Assembly;
                    var viewType = containingAssembly.GetType(fileInfo.FullTypeName);
                    var cacheEntry = new CompilerCacheEntry(fileInfo, viewType);

                    // There shouldn't be any duplicates and if there are any the first will win.
                    // If the result doesn't match the one on disk its going to recompile anyways.
                    _cache.TryAdd(fileInfo.RelativePath, cacheEntry);
                }
            }
        }

        internal CompilerCache()
        {
            _cache = new ConcurrentDictionary<string, CompilerCacheEntry>(StringComparer.OrdinalIgnoreCase);
        }

        internal static IEnumerable<RazorFileInfoCollection>
                            GetFileInfos(IEnumerable<Assembly> assemblies)
        {
            return assemblies.SelectMany(a => a.ExportedTypes)
                    .Where(Match)
                    .Select(c => (RazorFileInfoCollection)Activator.CreateInstance(c));
        }

        private static bool Match(Type t)
        {
            var inAssemblyType = typeof(RazorFileInfoCollection);
            if (inAssemblyType.IsAssignableFrom(t))
            {
                var hasParameterlessConstructor = t.GetConstructor(EmptyType) != null;

                return hasParameterlessConstructor
                    && !t.GetTypeInfo().IsAbstract
                    && !t.GetTypeInfo().ContainsGenericParameters;
            }

            return false;
        }

        public CompilationResult GetOrAdd(RelativeFileInfo fileInfo, Func<CompilationResult> compile)
        {
            CompilerCacheEntry cacheEntry;
            if (!_cache.TryGetValue(fileInfo.RelativePath, out cacheEntry))
            {
                return OnCacheMiss(fileInfo, compile);
            }
            else
            {
                if (cacheEntry.Length != fileInfo.FileInfo.Length)
                {
                    // it's not a match, recompile
                    return OnCacheMiss(fileInfo, compile);
                }

                if (cacheEntry.LastModified == fileInfo.FileInfo.LastModified)
                {
                    // Match, not update needed
                    return CompilationResult.Successful(cacheEntry.ViewType);
                }

                var hash = RazorFileHash.GetHash(fileInfo.FileInfo);

                // Timestamp doesn't match but it might be because of deployment, compare the hash.
                if (cacheEntry.IsPreCompiled &&
                    string.Equals(cacheEntry.Hash, hash, StringComparison.Ordinal))
                {
                    // Cache hit, but we need to update the entry
                    return OnCacheMiss(fileInfo, () => CompilationResult.Successful(cacheEntry.ViewType));
                }

                // it's not a match, recompile
                return OnCacheMiss(fileInfo, compile);
            }
        }

        private CompilationResult OnCacheMiss(RelativeFileInfo file, Func<CompilationResult> compile)
        {
            var result = compile();

            var cacheEntry = new CompilerCacheEntry(file, result.CompiledType);
            _cache.AddOrUpdate(file.RelativePath, cacheEntry, (a, b) => cacheEntry);

            return result;
        }
    }
}
