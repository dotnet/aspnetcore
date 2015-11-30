// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Runtime.Versioning;
using Microsoft.AspNet.FileProviders;
using Microsoft.Dnx.Compilation.CSharp;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Internal;

namespace Microsoft.AspNet.Mvc.Razor.Precompilation
{
    /// <summary>
    /// An <see cref="ICompileModule"/> implementation that pre-compiles Razor views in the application.
    /// </summary>
    public abstract class RazorPreCompileModule : ICompileModule
    {
        private const string ReleaseConfiguration = "release";
        private readonly object _memoryCacheLookupLock = new object();
        private readonly Dictionary<PrecompilationCacheKey, MemoryCache> _memoryCacheLookup =
            new Dictionary<PrecompilationCacheKey, MemoryCache>();

        /// <summary>
        /// Gets or sets a value that determines if symbols (.pdb) file for the precompiled views is generated.
        /// </summary>
        public bool GenerateSymbols { get; protected set; }

        /// <inheritdoc />
        /// <remarks>Pre-compiles all Razor views in the application.</remarks>
        public virtual void BeforeCompile(BeforeCompileContext context)
        {
            if (!EnablePreCompilation(context))
            {
                return;
            }

            MemoryCache memoryCache;
            lock (_memoryCacheLookupLock)
            {
                var cacheKey = new PrecompilationCacheKey
                {
                    Configuration = context.ProjectContext.Configuration,
                    TargetFramework = context.ProjectContext.TargetFramework
                };

                if (!_memoryCacheLookup.TryGetValue(cacheKey, out memoryCache))
                {
                    // When CompactOnMemoryPressure is true, the MemoryCache evicts items at every gen2 collection.
                    // In DTH, gen2 happens frequently enough to make it undesirable for caching precompilation results. We'll
                    // disable listening for memory pressure for the MemoryCache instance used by precompilation.
                    memoryCache = new MemoryCache(new MemoryCacheOptions { CompactOnMemoryPressure = false });

                    _memoryCacheLookup[cacheKey] = memoryCache;
                }
            }

            using (var fileProvider = new PhysicalFileProvider(context.ProjectContext.ProjectDirectory))
            {
                var viewCompiler = new RazorPreCompiler(
                    context,
                    fileProvider,
                    memoryCache)
                {
                    GenerateSymbols = GenerateSymbols
                };

                viewCompiler.CompileViews();
            }
        }

        /// <inheritdoc />
        public void AfterCompile(AfterCompileContext context)
        {
        }

        /// <summary>
        /// Determines if this instance of <see cref="RazorPreCompileModule"/> should enable
        /// compilation of views.
        /// </summary>
        /// <param name="context">The <see cref="BeforeCompileContext"/>.</param>
        /// <returns><c>true</c> if views should be precompiled; otherwise <c>false</c>.</returns>
        /// <remarks>Returns <c>true</c> if the current application is being built in <c>release</c>
        /// configuration.</remarks>
        protected virtual bool EnablePreCompilation(BeforeCompileContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            return string.Equals(
                context.ProjectContext.Configuration,
                ReleaseConfiguration,
                StringComparison.OrdinalIgnoreCase);
        }

        private class PrecompilationCacheKey : IEquatable<PrecompilationCacheKey>
        {
            public string Configuration { get; set; }

            public FrameworkName TargetFramework { get; set; }

            public bool Equals(PrecompilationCacheKey other)
            {
                return
                    other.TargetFramework == TargetFramework &&
                    string.Equals(other.Configuration, Configuration, StringComparison.Ordinal);
            }

            public override int GetHashCode()
            {
                var hashCodeCombiner = HashCodeCombiner.Start();
                hashCodeCombiner.Add(Configuration);
                hashCodeCombiner.Add(TargetFramework);

                return hashCodeCombiner;
            }
        }
    }
}
