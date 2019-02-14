// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Extensions.Caching.Memory;

namespace Microsoft.AspNetCore.Mvc.Razor.Compilation
{
    /// <summary>
    /// Provides an instance of <see cref="IMemoryCache"/> that is used to store compiled Razor views. 
    /// </summary>
    public interface IViewCompilationMemoryCacheProvider
    {
        IMemoryCache CompilationMemoryCache { get; }
    }
}
