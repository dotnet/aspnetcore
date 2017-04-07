// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Razor.Compilation;
using Microsoft.AspNetCore.Razor.Language;

namespace Microsoft.AspNetCore.Mvc.Razor.Internal
{
    public struct CompilerCacheContext
    {
        public CompilerCacheContext(
            RazorProjectItem projectItem,
            IEnumerable<RazorProjectItem> additionalCompilationItems,
            Func<CompilerCacheContext, CompilationResult> compile)
        {
            ProjectItem = projectItem;
            AdditionalCompilationItems = additionalCompilationItems;
            Compile = compile;
        }

        public RazorProjectItem ProjectItem { get; }

        public IEnumerable<RazorProjectItem> AdditionalCompilationItems { get; }

        public Func<CompilerCacheContext, CompilationResult> Compile { get; }
    }
}
