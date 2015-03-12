// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Versioning;
using Microsoft.AspNet.Hosting;
using Microsoft.AspNet.Mvc.Razor;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
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

            // When ListenForMemoryPressure is true, the MemoryCache evicts items at every gen2 collection.
            // In DTH, gen2 happens frequently enough to make it undesirable for caching precompilation results. We'll
            // disable listening for memory pressure for the MemoryCache instance used by precompilation.
            _memoryCache = new MemoryCache(new MemoryCacheOptions { ListenForMemoryPressure = false });
        }

        /// <summary>
        /// Gets or sets a value that determines if symbols (.pdb) file for the precompiled views is generated.
        /// </summary>
        public bool GenerateSymbols { get; protected set; }

        /// <inheritdoc />
        /// <remarks>Pre-compiles all Razor views in the application.</remarks>
        public virtual void BeforeCompile(IBeforeCompileContext context)
        {
            var applicationEnvironment = _appServices.GetRequiredService<IApplicationEnvironment>();
            var compilerOptionsProvider = _appServices.GetRequiredService<ICompilerOptionsProvider>();
            var compilationSettings = compilerOptionsProvider.GetCompilationSettings(applicationEnvironment);

            // Create something similar to a HttpContext.RequestServices provider. Necessary because this class is
            // instantiated in a lower-level "HttpContext.ApplicationServices" context. One important added service
            // is an IOptions<RazorViewEngineOptions> but use AddMvc() for simplicity and flexibility.
            var serviceCollection = HostingServices.Create(_appServices);
            serviceCollection.AddMvc();

            // We also need an IApplicationEnvironment with a base path that matches the containing web site, to
            // find the razor files. We don't have a guarantee that the base path of the current application is
            // this site. For example similar functional test changes to the IApplicationEnvironment happen later,
            // after everything is compiled. IOptions<RazorViewEngineOptions> setup initializes the
            // RazorViewEngineOptions based on this IApplicationEnvironment implementation.
            var directory = context.ProjectContext.ProjectDirectory;
            var precompilationApplicationEnvironment = new PrecompilationApplicationEnvironment(
                applicationEnvironment,
                context.ProjectContext.ProjectDirectory);
            serviceCollection.AddInstance<IApplicationEnvironment>(precompilationApplicationEnvironment);

            var serviceProvider = serviceCollection.BuildServiceProvider();
            var viewCompiler = new RazorPreCompiler(serviceProvider, context, _memoryCache, compilationSettings)
            {
                GenerateSymbols = GenerateSymbols
            };

            viewCompiler.CompileViews();
        }

        /// <inheritdoc />
        public void AfterCompile(IAfterCompileContext context)
        {
        }

        private class PrecompilationApplicationEnvironment : IApplicationEnvironment
        {
            private readonly IApplicationEnvironment _originalApplicationEnvironment;
            private readonly string _applicationBasePath;

            public PrecompilationApplicationEnvironment(IApplicationEnvironment original, string appBasePath)
            {
                _originalApplicationEnvironment = original;
                _applicationBasePath = appBasePath;
            }

            public string ApplicationName
            {
                get
                {
                    return _originalApplicationEnvironment.ApplicationName;
                }
            }

            public string Version
            {
                get
                {
                    return _originalApplicationEnvironment.Version;
                }
            }

            public string ApplicationBasePath
            {
                get
                {
                    return _applicationBasePath;
                }
            }

            public string Configuration
            {
                get
                {
                    return _originalApplicationEnvironment.Configuration;
                }
            }

            public FrameworkName RuntimeFramework
            {
                get
                {
                    return _originalApplicationEnvironment.RuntimeFramework;
                }
            }
        }
    }
}
