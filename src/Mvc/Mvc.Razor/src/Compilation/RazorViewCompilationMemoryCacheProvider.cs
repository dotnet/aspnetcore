// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Extensions.Caching.Memory;

namespace Microsoft.AspNetCore.Mvc.Razor.Compilation
{
    internal class RazorViewCompilationMemoryCacheProvider : IViewCompilationMemoryCacheProvider
    {
        IMemoryCache IViewCompilationMemoryCacheProvider.CompilationMemoryCache { get; } = new MemoryCache(new MemoryCacheOptions());
    }
}
