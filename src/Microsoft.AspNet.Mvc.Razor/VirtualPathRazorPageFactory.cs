// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Framework.DependencyInjection;

namespace Microsoft.AspNet.Mvc.Razor
{
    /// <summary>
    /// Represents a <see cref="IRazorPageFactory"/> that creates <see cref="RazorPage"/> instances
    /// from razor files in the file system.
    /// </summary>
    public class VirtualPathRazorPageFactory : IRazorPageFactory
    {
        private IRazorCompilationService _compilationService;
        private IRazorCompilationService CompilationService
        {
            get
            {
                if (_compilationService == null)
                {
                    _compilationService = _serviceProvider.GetService<IRazorCompilationService>();
                }

                return _compilationService;
            }
        }

        private readonly ITypeActivator _activator;
        private readonly IServiceProvider _serviceProvider;
        private readonly IFileInfoCache _fileInfoCache;

        public VirtualPathRazorPageFactory(ITypeActivator typeActivator,
                                           IServiceProvider serviceProvider,
                                           IFileInfoCache fileInfoCache)
        {
            _activator = typeActivator;
            _serviceProvider = serviceProvider;
            _fileInfoCache = fileInfoCache;
        }

        /// <inheritdoc />
        public IRazorPage CreateInstance([NotNull] string relativePath, bool enableInstrumentation)
        {
            if (relativePath.StartsWith("~/", StringComparison.Ordinal))
            {
                // For tilde slash paths, drop the leading ~ to make it work with the underlying IFileSystem.
                relativePath = relativePath.Substring(1);
            }

            var fileInfo = _fileInfoCache.GetFileInfo(relativePath);

            if (fileInfo != null)
            {
                var relativeFileInfo = new RelativeFileInfo()
                {
                    FileInfo = fileInfo,
                    RelativePath = relativePath,
                };

                var result = CompilationService.Compile(relativeFileInfo, enableInstrumentation);
                var page = (IRazorPage)_activator.CreateInstance(_serviceProvider, result.CompiledType);
                page.Path = relativePath;

                return page;
            }

            return null;
        }
    }
}
