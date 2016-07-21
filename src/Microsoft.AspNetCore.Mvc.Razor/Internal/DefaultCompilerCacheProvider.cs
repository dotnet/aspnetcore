// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Razor.Compilation;

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
        /// <param name="applicationPartManager">The <see cref="ApplicationPartManager" /></param>
        /// <param name="fileProviderAccessor">The <see cref="IRazorViewEngineFileProviderAccessor"/>.</param>
        public DefaultCompilerCacheProvider(
            ApplicationPartManager applicationPartManager,
            IRazorViewEngineFileProviderAccessor fileProviderAccessor)
        {
            var feature = new ViewsFeature();
            applicationPartManager.PopulateFeature(feature);
            Cache = new CompilerCache(fileProviderAccessor.FileProvider, feature.Views);
        }

        /// <inheritdoc />
        public ICompilerCache Cache { get; }
    }
}