// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Mvc.Razor.Internal
{
    /// <summary>
    /// Default implementation for <see cref="ICompilerCacheProvider"/>.
    /// </summary>
    public class DefaultCompilerCacheProvider : ICompilerCacheProvider
    {
        /// <summary>
        /// Initializes a new instance of <see cref="DefaultCompilerCacheProvider"/>.
        /// </summary>
        /// <param name="fileProviderAccessor">The <see cref="IRazorViewEngineFileProviderAccessor"/>.</param>
        public DefaultCompilerCacheProvider(IRazorViewEngineFileProviderAccessor fileProviderAccessor)
        {
            Cache = new CompilerCache(fileProviderAccessor.FileProvider);
        }

        /// <inheritdoc />
        public ICompilerCache Cache { get; }
    }
}