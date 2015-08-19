// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Mvc.Razor.Compilation;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Mvc.Razor
{
    /// <summary>
    /// Represents a <see cref="IRazorPageFactory"/> that creates <see cref="RazorPage"/> instances
    /// from razor files in the file system.
    /// </summary>
    public class VirtualPathRazorPageFactory : IRazorPageFactory
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ICompilerCacheProvider _compilerCacheProvider;
        private IRazorCompilationService _razorcompilationService;
        private ICompilerCache _compilerCache;

        /// <summary>
        /// Initializes a new instance of <see cref="VirtualPathRazorPageFactory"/>.
        /// </summary>
        /// <param name="serviceProvider">The request specific <see cref="IServiceProvider"/>.</param>
        /// <param name="compilerCacheProvider">The <see cref="ICompilerCacheProvider"/>.</param>
        public VirtualPathRazorPageFactory(
            IServiceProvider serviceProvider,
            ICompilerCacheProvider compilerCacheProvider)
        {
            _serviceProvider = serviceProvider;
            _compilerCacheProvider = compilerCacheProvider;
        }

        private IRazorCompilationService RazorCompilationService
        {
            get
            {
                if (_razorcompilationService == null)
                {
                    // it is ok to use the cached service provider because both this, and the
                    // resolved service are in a lifetime of Scoped.
                    // We don't want to get it upfront because it will force Roslyn to load.
                    _razorcompilationService = _serviceProvider.GetRequiredService<IRazorCompilationService>();
                }

                return _razorcompilationService;
            }
        }

        private ICompilerCache CompilerCache
        {
            get
            {
                if (_compilerCache == null)
                {
                    _compilerCache = _compilerCacheProvider.Cache;
                }

                return _compilerCache;
            }
        }

        /// <inheritdoc />
        public IRazorPage CreateInstance([NotNull] string relativePath)
        {
            if (relativePath.StartsWith("~/", StringComparison.Ordinal))
            {
                // For tilde slash paths, drop the leading ~ to make it work with the underlying IFileProvider.
                relativePath = relativePath.Substring(1);
            }

            var result = CompilerCache.GetOrAdd(
                relativePath,
                RazorCompilationService.Compile);

            if (result == CompilerCacheResult.FileNotFound)
            {
                return null;
            }

            var page = (IRazorPage)Activator.CreateInstance(result.CompilationResult.CompiledType);
            page.Path = relativePath;

            return page;
        }
    }
}