// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Mvc;
using Microsoft.AspNet.Mvc.Razor;
using Microsoft.AspNet.Mvc.Razor.Compilation;
using Microsoft.Framework.OptionsModel;
using Microsoft.Framework.Runtime;

namespace RazorCompilerCacheWebSite
{
    public class CustomCompilerCache : CompilerCache
    {
        public CustomCompilerCache(IAssemblyProvider assemblyProvider,
                                   IAssemblyLoadContextAccessor loadContextAccessor,
                                   IOptions<RazorViewEngineOptions> optionsAccessor,
                                   CompilerCacheInitialiedService cacheInitializedService)
            : base(assemblyProvider, loadContextAccessor, optionsAccessor)
        {
            cacheInitializedService.Initialized = true;
        }
    }
}