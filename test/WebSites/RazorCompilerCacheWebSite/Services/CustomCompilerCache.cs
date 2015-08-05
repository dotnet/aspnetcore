// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.AspNet.Mvc.Razor;
using Microsoft.AspNet.Mvc.Razor.Compilation;
using Microsoft.AspNet.Mvc.Razor.Precompilation;
using Microsoft.Dnx.Runtime;
using Microsoft.Framework.OptionsModel;

namespace RazorCompilerCacheWebSite
{
    public class CustomCompilerCache : CompilerCache
    {
        public CustomCompilerCache(IEnumerable<RazorFileInfoCollection> fileInfoCollection,
                                   IAssemblyLoadContextAccessor loadContextAccessor,
                                   IOptions<RazorViewEngineOptions> optionsAccessor,
                                   CompilerCacheInitialiedService cacheInitializedService)
            : base(fileInfoCollection, loadContextAccessor, optionsAccessor)
        {
            cacheInitializedService.Initialized = true;
        }
    }
}