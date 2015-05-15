// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.FileProviders;
using Microsoft.AspNet.Mvc.Razor.Precompilation;
using Microsoft.Framework.Caching.Memory;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.Runtime;
using Microsoft.Framework.Runtime.Roslyn;

namespace Microsoft.AspNet.Mvc
{
    /// <summary>
    /// An <see cref="ICompileModule"/> implementation that pre-compiles Razor views in the application.
    /// </summary>
    public abstract class RazorPreCompileModule : ICompileModule
    {
        private readonly IServiceProvider _appServices;
        private readonly IMemoryCache _memoryCache;

        /// <summary>
        /// Instantiates a new <see cref="RazorPreCompileModule"/> instance.
        /// </summary>
        /// <param name="services">The <see cref="IServiceProvider"/> for the application.</param>
        public RazorPreCompileModule(IServiceProvider services)
        {
            _appServices = services;

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
            var compilerOptionsProvider = _appServices.GetRequiredService<ICompilerOptionsProvider>();
            var loadContextAccessor = _appServices.GetRequiredService<IAssemblyLoadContextAccessor>();
            var compilationSettings = GetCompilationSettings(compilerOptionsProvider, context.ProjectContext);
            var fileProvider = new PhysicalFileProvider(context.ProjectContext.ProjectDirectory);

            var viewCompiler = new RazorPreCompiler(
                context,
                loadContextAccessor,
                fileProvider,
                _memoryCache,
                compilationSettings)
            {
                GenerateSymbols = GenerateSymbols
            };

            viewCompiler.CompileViews();
        }

        /// <inheritdoc />
        public void AfterCompile(AfterCompileContext context)
        {
        }

        private static CompilationSettings GetCompilationSettings(
            ICompilerOptionsProvider compilerOptionsProvider,
            ProjectContext projectContext)
        {
            return compilerOptionsProvider.GetCompilerOptions(projectContext.Name,
                                                              projectContext.TargetFramework,
                                                              projectContext.Configuration)
                                          .ToCompilationSettings(projectContext.TargetFramework);
        }
    }
}
