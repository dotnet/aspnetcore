// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Microsoft.AspNet.Mvc.Razor
{
    /// <inheritdoc />
    public class CompilerCache : ICompilerCache
    {
        private readonly ConcurrentDictionary<string, CompilerCacheEntry> _cache;
        private static readonly Type[] EmptyType = new Type[0];

        /// <summary>
        /// Sets up the runtime compilation cache.
        /// </summary>
        /// <param name="provider">
        /// An <see cref="IAssemblyProvider"/> representing the assemblies
        /// used to search for pre-compiled views.
        /// </param>
        public CompilerCache([NotNull] IAssemblyProvider provider)
            : this(GetFileInfos(provider.CandidateAssemblies))
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
                    _cache.TryAdd(NormalizePath(fileInfo.RelativePath), cacheEntry);
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

        /// <inheritdoc />
        public CompilationResult GetOrAdd([NotNull] RelativeFileInfo fileInfo,
                                          bool enableInstrumentation,
                                          [NotNull] Func<CompilationResult> compile)
        {

            CompilerCacheEntry cacheEntry;
            if (!_cache.TryGetValue(NormalizePath(fileInfo.RelativePath), out cacheEntry))
            {
                return OnCacheMiss(fileInfo, enableInstrumentation, compile);
            }
            else
            {
                if ((cacheEntry.Length != fileInfo.FileInfo.Length) ||
                    (enableInstrumentation && !cacheEntry.IsInstrumented))
                {
                    // Recompile if
                    // (a) If the file lengths differ
                    // (b) If the compiled type is not instrumented but we require it to be instrumented.
                    return OnCacheMiss(fileInfo, enableInstrumentation, compile);
                }

                if (cacheEntry.LastModified == fileInfo.FileInfo.LastModified)
                {
                    // Match, not update needed
                    return CompilationResult.Successful(cacheEntry.CompiledType);
                }

                var hash = RazorFileHash.GetHash(fileInfo.FileInfo);

                // Timestamp doesn't match but it might be because of deployment, compare the hash.
                if (cacheEntry.IsPreCompiled &&
                    string.Equals(cacheEntry.Hash, hash, StringComparison.Ordinal))
                {
                    // Cache hit, but we need to update the entry
                    return OnCacheMiss(fileInfo,
                                       enableInstrumentation,
                                       () => CompilationResult.Successful(cacheEntry.CompiledType));
                }

                // it's not a match, recompile
                return OnCacheMiss(fileInfo, enableInstrumentation, compile);
            }
        }

        private CompilationResult OnCacheMiss(RelativeFileInfo file,
                                              bool isInstrumented,
                                              Func<CompilationResult> compile)
        {
            var result = compile();

            var cacheEntry = new CompilerCacheEntry(file, result.CompiledType, isInstrumented);
            _cache[NormalizePath(file.RelativePath)] = cacheEntry;

            return result;
        }

        private string NormalizePath(string path)
        {
            path = path.Replace('/', '\\');
            path = path.TrimStart('\\');

            return path;
        }
    }
}
