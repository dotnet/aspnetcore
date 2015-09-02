// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Framework.OptionsModel;

namespace Microsoft.AspNet.Mvc.Razor.Compilation
{
    /// <summary>
    /// Default implementation for <see cref="ICompilerCacheProvider"/>.
    /// </summary>
    public class DefaultCompilerCacheProvider : ICompilerCacheProvider
    {
        /// <summary>
        /// Initializes a new instance of <see cref="DefaultCompilerCacheProvider"/>.
        /// </summary>
        /// <param name="optionsAccessor">An accessor to the <see cref="RazorViewEngineOptions"/>.</param>
        public DefaultCompilerCacheProvider(IOptions<RazorViewEngineOptions> mvcViewOptions)
        {
            var fileProvider = mvcViewOptions.Value.FileProvider;
            Cache = new CompilerCache(fileProvider);
        }

        /// <inheritdoc />
        public ICompilerCache Cache { get; }
    }
}