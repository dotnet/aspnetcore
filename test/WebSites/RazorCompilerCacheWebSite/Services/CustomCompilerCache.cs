// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Mvc;
using Microsoft.AspNet.Mvc.Razor;
using Microsoft.Framework.OptionsModel;

namespace RazorCompilerCacheWebSite
{
    public class CustomCompilerCache : CompilerCache
    {
        public CustomCompilerCache(IAssemblyProvider assemblyProvider,
                                   IOptions<RazorViewEngineOptions> optionsAccessor,
                                   CompilerCacheInitialiedService cacheInitializedService)
            : base(assemblyProvider, optionsAccessor)
        {
            cacheInitializedService.Initialized = true;
        }
    }
}