// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.FileSystems;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.PageExecutionInstrumentation;
using Microsoft.Framework.DependencyInjection;

namespace Microsoft.AspNet.Mvc.Razor
{
    /// <summary>
    /// Represents a <see cref="IRazorPageFactory"/> that creates <see cref="RazorPage"/> instances
    /// from razor files in the file system.
    /// </summary>
    public class VirtualPathRazorPageFactory : IRazorPageFactory
    {
        private readonly ITypeActivator _activator;
        private readonly IServiceProvider _serviceProvider;
        private readonly IRazorFileSystemCache _fileSystemCache;
        private readonly ICompilerCache _compilerCache;
        private IRazorCompilationService _razorcompilationService;

        public VirtualPathRazorPageFactory(ITypeActivator typeActivator,
                                           IServiceProvider serviceProvider,
                                           ICompilerCache compilerCache,
                                           IRazorFileSystemCache fileSystemCache)
        {
            _activator = typeActivator;
            _serviceProvider = serviceProvider;
            _compilerCache = compilerCache;
            _fileSystemCache = fileSystemCache;
        }

        private IRazorCompilationService RazorCompilationService
        {
            get
            {
                if (_razorcompilationService == null)
                {
                    // it is ok to use the cached service provider because both this, and the
                    // resolved service are in a lifetime of Scoped.
                    // We don't want to get it upgront because it will force Roslyn to load.
                    _razorcompilationService = _serviceProvider.GetRequiredService<IRazorCompilationService>();
                }

                return _razorcompilationService;
            }
        }

        /// <inheritdoc />
        public IRazorPage CreateInstance([NotNull] string relativePath)
        {
            if (relativePath.StartsWith("~/", StringComparison.Ordinal))
            {
                // For tilde slash paths, drop the leading ~ to make it work with the underlying IFileSystem.
                relativePath = relativePath.Substring(1);
            }

            var fileInfo = _fileSystemCache.GetFileInfo(relativePath);

            if (fileInfo.Exists)
            {
                var relativeFileInfo = new RelativeFileInfo(fileInfo, relativePath);

                var result = _compilerCache.GetOrAdd(
                    relativeFileInfo,
                    RazorCompilationService.Compile);

                var page = (IRazorPage)_activator.CreateInstance(_serviceProvider, result.CompiledType);
                page.Path = relativePath;

                return page;
            }

            return null;
        }
    }
}
