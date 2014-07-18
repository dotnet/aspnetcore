// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Mvc.Core;
using Microsoft.Framework.DependencyInjection;

namespace Microsoft.AspNet.Mvc.Razor
{
    /// <summary>
    /// Represents a <see cref="IRazorPageFactory"/> that creates <see cref="RazorPage"/> instances
    /// from razor files in the file system.
    /// </summary>
    public class FileBasedRazorPageFactory : IRazorPageFactory
    {
        private readonly IRazorCompilationService _compilationService;
        private readonly ITypeActivator _activator;
        private readonly IServiceProvider _serviceProvider;
        private readonly IFileInfoCache _fileInfoCache;

        public FileBasedRazorPageFactory(IRazorCompilationService compilationService,
                                    ITypeActivator typeActivator,
                                    IServiceProvider serviceProvider,
                                    IFileInfoCache fileInfoCache)
        {
            _compilationService = compilationService;
            _activator = typeActivator;
            _serviceProvider = serviceProvider;
            _fileInfoCache = fileInfoCache;
        }

        /// <inheritdoc />
        public RazorPage CreateInstance([NotNull] string viewPath)
        {
            var fileInfo = _fileInfoCache.GetFileInfo(viewPath.TrimStart('~'));

            if (fileInfo != null)
            {
                var result = _compilationService.Compile(fileInfo);
                var page = (RazorPage)_activator.CreateInstance(_serviceProvider, result.CompiledType);
                return page;
            }

            return null;
        }
    }
}
