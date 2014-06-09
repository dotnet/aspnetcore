// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.FileSystems;
using Microsoft.AspNet.Mvc.Rendering;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.Runtime;

namespace Microsoft.AspNet.Mvc.Razor
{
    public class VirtualPathViewFactory : IVirtualPathViewFactory
    {
        private readonly PhysicalFileSystem _fileSystem;
        private readonly IRazorCompilationService _compilationService;
        private readonly ITypeActivator _activator;
        private readonly IServiceProvider _serviceProvider;

        public VirtualPathViewFactory(IApplicationEnvironment env,
                                      IRazorCompilationService compilationService,
                                      ITypeActivator typeActivator,
                                      IServiceProvider serviceProvider)
        {
            // TODO: Continue to inject the IFileSystem but only when we get it from the host
            _fileSystem = new PhysicalFileSystem(env.ApplicationBasePath);
            _compilationService = compilationService;
            _activator = typeActivator;
            _serviceProvider = serviceProvider;
        }

        public IView CreateInstance([NotNull] string virtualPath)
        {
            IFileInfo fileInfo;
            if (_fileSystem.TryGetFileInfo(virtualPath, out fileInfo))
            {
                var result = _compilationService.Compile(fileInfo);
                return (IView)_activator.CreateInstance(_serviceProvider, result.CompiledType);
            }

            return null;
        }
    }
}
