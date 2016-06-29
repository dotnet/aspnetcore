// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Mvc.Razor.Internal
{
    /// <summary>
    /// Default implementation of <see cref="IRazorViewEngineFileProviderAccessor"/>.
    /// </summary>
    public class DefaultRazorViewEngineFileProviderAccessor : IRazorViewEngineFileProviderAccessor
    {
        /// <summary>
        /// Initializes a new instance of <see cref="DefaultRazorViewEngineFileProviderAccessor"/>.
        /// </summary>
        /// <param name="optionsAccessor">Accessor to <see cref="RazorViewEngineOptions"/>.</param>
        public DefaultRazorViewEngineFileProviderAccessor(IOptions<RazorViewEngineOptions> optionsAccessor)
        {
            var fileProviders = optionsAccessor.Value.FileProviders;
            if (fileProviders.Count == 0)
            {
                FileProvider = new NullFileProvider();
            }
            else if (fileProviders.Count == 1)
            {
                FileProvider = fileProviders[0];
            }
            else
            {
                FileProvider = new CompositeFileProvider(fileProviders);
            }
        }

        /// <summary>
        /// Gets the <see cref="IFileProvider"/> used to look up Razor files.
        /// </summary>
        public IFileProvider FileProvider { get; }
    }
}
