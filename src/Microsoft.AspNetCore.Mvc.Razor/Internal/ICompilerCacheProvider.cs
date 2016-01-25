// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Mvc.Razor.Internal
{
    /// <summary>
    /// Provides access to a cached <see cref="ICompilerCache"/> instance.
    /// </summary>
    public interface ICompilerCacheProvider
    {
        /// <summary>
        /// The cached <see cref="ICompilerCache"/> instance.
        /// </summary>
        ICompilerCache Cache { get; }
    }
}
