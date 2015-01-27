// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Mvc;
using Microsoft.AspNet.Mvc.Razor;

namespace RazorCompilerCacheWebSite
{
    public class CustomCompilerCache : CompilerCache
    {
        public CustomCompilerCache(IAssemblyProvider assemblyProvider,
                                   IRazorFileProviderCache fileProvider,
                                   CompilerCacheInitialiedService cacheInitializedService)
            : base(assemblyProvider, fileProvider)
        {
            cacheInitializedService.Initialized = true;
        }
    }
}