// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Mvc.Core;
using Microsoft.AspNet.Mvc.Rendering;
using Microsoft.Framework.DependencyInjection;

namespace Microsoft.AspNet.Mvc.Razor
{
    public class VirtualPathViewFactory : IVirtualPathViewFactory
    {
        private readonly IRazorCompilationService _compilationService;
        private readonly ITypeActivator _activator;
        private readonly IServiceProvider _serviceProvider;
        private readonly IFileInfoCache _fileInfoCache;

        public VirtualPathViewFactory(IRazorCompilationService compilationService,
                                      ITypeActivator typeActivator,
                                      IServiceProvider serviceProvider,
                                      IFileInfoCache fileInfoCache)
        {
            _compilationService = compilationService;
            _activator = typeActivator;
            _serviceProvider = serviceProvider;
            _fileInfoCache = fileInfoCache;
        }

        public IView CreateInstance([NotNull] string virtualPath)
        {
            var fileInfo = _fileInfoCache.GetFileInfo(virtualPath);

            if (fileInfo != null)
            {
                var result = _compilationService.Compile(fileInfo);
                return (IView)_activator.CreateInstance(_serviceProvider, result.CompiledType);
            }

            return null;
        }
    }
}
