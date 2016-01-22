// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Mvc.Razor.Compilation;

namespace Microsoft.AspNet.Mvc.Razor
{
    /// <summary>
    /// Represents a <see cref="IRazorPageFactoryProvider"/> that creates <see cref="RazorPage"/> instances
    /// from razor files in the file system.
    /// </summary>
    public class DefaultRazorPageFactoryProvider : IRazorPageFactoryProvider
    {
        /// <remarks>
        /// This delegate holds on to an instance of <see cref="IRazorCompilationService"/>.
        /// </remarks>
        private readonly Func<RelativeFileInfo, CompilationResult> _compileDelegate;
        private readonly ICompilerCacheProvider _compilerCacheProvider;
        private ICompilerCache _compilerCache;

        /// <summary>
        /// Initializes a new instance of <see cref="DefaultRazorPageFactoryProvider"/>.
        /// </summary>
        /// <param name="razorCompilationService">The <see cref="IRazorCompilationService"/>.</param>
        /// <param name="compilerCacheProvider">The <see cref="ICompilerCacheProvider"/>.</param>
        public DefaultRazorPageFactoryProvider(
            IRazorCompilationService razorCompilationService,
            ICompilerCacheProvider compilerCacheProvider)
        {
            _compileDelegate = razorCompilationService.Compile;
            _compilerCacheProvider = compilerCacheProvider;
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
        public RazorPageFactoryResult CreateFactory(string relativePath)
        {
            if (relativePath == null)
            {
                throw new ArgumentNullException(nameof(relativePath));
            }

            if (relativePath.StartsWith("~/", StringComparison.Ordinal))
            {
                // For tilde slash paths, drop the leading ~ to make it work with the underlying IFileProvider.
                relativePath = relativePath.Substring(1);
            }
            var result = CompilerCache.GetOrAdd(relativePath, _compileDelegate);
            if (result.Success)
            {
                var pageFactory = GetPageFactory(result.CompilationResult.CompiledType, relativePath);
                return new RazorPageFactoryResult(pageFactory, result.ExpirationTokens);
            }
            else
            {
                return new RazorPageFactoryResult(result.ExpirationTokens);
            }
        }

        /// <summary>
        /// Creates a factory for <see cref="IRazorPage"/>.
        /// </summary>
        /// <param name="compiledType">The <see cref="Type"/> to produce an instance of <see cref="IRazorPage"/>
        /// from.</param>
        /// <param name="relativePath">The application relative path of the page.</param>
        /// <returns>A factory for <paramref name="compiledType"/>.</returns>
        protected virtual Func<IRazorPage> GetPageFactory(Type compiledType, string relativePath)
        {
            return () =>
            {
                var page = (IRazorPage)Activator.CreateInstance(compiledType);
                page.Path = relativePath;
                return page;
            };
        }
    }
}