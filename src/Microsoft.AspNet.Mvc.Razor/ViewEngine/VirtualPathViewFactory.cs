// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNet.FileSystems;
using Microsoft.AspNet.Mvc.Rendering;
using Microsoft.Framework.Runtime;

namespace Microsoft.AspNet.Mvc.Razor
{
    public class VirtualPathViewFactory : IVirtualPathViewFactory
    {
        private readonly PhysicalFileSystem _fileSystem;
        private readonly IRazorCompilationService _compilationService;

        public VirtualPathViewFactory(IApplicationEnvironment env,
                                      IRazorCompilationService compilationService)
        {
            // TODO: Continue to inject the IFileSystem but only when we get it from the host
            _fileSystem = new PhysicalFileSystem(env.ApplicationBasePath);
            _compilationService = compilationService;
        }

        public IView CreateInstance([NotNull]string virtualPath)
        {
            IFileInfo fileInfo;
            if (_fileSystem.TryGetFileInfo(virtualPath, out fileInfo))
            {
                CompilationResult result = _compilationService.Compile(fileInfo);
                return (IView)Activator.CreateInstance(result.CompiledType);
            }

            return null;
        }
    }
}
