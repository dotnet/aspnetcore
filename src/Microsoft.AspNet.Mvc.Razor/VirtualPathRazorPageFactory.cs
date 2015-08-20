// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Mvc.Razor.Compilation;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Mvc.Razor
{
    /// <summary>
    /// Represents a <see cref="IRazorPageFactory"/> that creates <see cref="RazorPage"/> instances
    /// from razor files in the file system.
    /// </summary>
    public class VirtualPathRazorPageFactory : IRazorPageFactory
    {
        /// <remarks>
        /// This delegate holds on to an instance of <see cref="IRazorCompilationService"/>.
        /// </remarks>
        private readonly Func<RelativeFileInfo, CompilationResult> _compileDelegate;
        private readonly ICompilerCacheProvider _compilerCacheProvider;
        private ICompilerCache _compilerCache;

        /// <summary>
        /// Initializes a new instance of <see cref="VirtualPathRazorPageFactory"/>.
        /// </summary>
        /// <param name="razorCompilationService">The <see cref="IRazorCompilationService"/>.</param>
        /// <param name="compilerCacheProvider">The <see cref="ICompilerCacheProvider"/>.</param>
        public VirtualPathRazorPageFactory(
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
        public IRazorPage CreateInstance([NotNull] string relativePath)
        {
            if (relativePath.StartsWith("~/", StringComparison.Ordinal))
            {
                // For tilde slash paths, drop the leading ~ to make it work with the underlying IFileProvider.
                relativePath = relativePath.Substring(1);
            }

            var result = CompilerCache.GetOrAdd(relativePath, _compileDelegate);

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