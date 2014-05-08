// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR
// CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING
// WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF
// TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR
// NON-INFRINGEMENT.
// See the Apache 2 License for the specific language governing
// permissions and limitations under the License.

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
