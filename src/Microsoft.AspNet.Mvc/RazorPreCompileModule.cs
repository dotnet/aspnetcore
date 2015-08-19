// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.FileProviders;
using Microsoft.AspNet.Mvc.Razor.Precompilation;
using Microsoft.Dnx.Compilation.CSharp;
using Microsoft.Dnx.Runtime;
using Microsoft.Framework.Caching.Memory;
using Microsoft.Framework.DependencyInjection;

namespace Microsoft.AspNet.Mvc
{
    /// <summary>
    /// An <see cref="ICompileModule"/> implementation that pre-compiles Razor views in the application.
    /// </summary>
    public abstract class RazorPreCompileModule : ICompileModule
    {
        private readonly IAssemblyLoadContext _loadContext;
        private readonly IMemoryCache _memoryCache;

        /// <summary>
        /// Instantiates a new <see cref="RazorPreCompileModule"/> instance.
        /// </summary>
        /// <param name="services">The <see cref="IServiceProvider"/> for the application.</param>
        public RazorPreCompileModule(IServiceProvider services)
        {
            _loadContext = services.GetRequiredService<IAssemblyLoadContext>();

            // When CompactOnMemoryPressure is true, the MemoryCache evicts items at every gen2 collection.
            // In DTH, gen2 happens frequently enough to make it undesirable for caching precompilation results. We'll
            // disable listening for memory pressure for the MemoryCache instance used by precompilation.
            _memoryCache = new MemoryCache(new MemoryCacheOptions { CompactOnMemoryPressure = false });
        }

        /// <summary>
        /// Gets or sets a value that determines if symbols (.pdb) file for the precompiled views is generated.
        /// </summary>
        public bool GenerateSymbols { get; protected set; }

        /// <inheritdoc />
        /// <remarks>Pre-compiles all Razor views in the application.</remarks>
        public virtual void BeforeCompile(BeforeCompileContext context)
        {
            var fileProvider = new PhysicalFileProvider(context.ProjectContext.ProjectDirectory);

            var viewCompiler = new RazorPreCompiler(
                context,
                _loadContext,
                fileProvider,
                _memoryCache)
            {
                GenerateSymbols = GenerateSymbols
            };

            viewCompiler.CompileViews();
        }

        /// <inheritdoc />
        public void AfterCompile(AfterCompileContext context)
        {
        }
    }
}
