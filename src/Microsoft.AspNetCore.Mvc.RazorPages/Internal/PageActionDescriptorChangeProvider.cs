// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Razor.Internal;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Mvc.RazorPages.Internal
{
    public class PageActionDescriptorChangeProvider : IActionDescriptorChangeProvider
    {
        private readonly IFileProvider _fileProvider;
        private readonly string _searchPattern;

        public PageActionDescriptorChangeProvider(
            IRazorViewEngineFileProviderAccessor fileProviderAccessor,
            IOptions<RazorPagesOptions> razorPagesOptions)
        {
            if (fileProviderAccessor == null)
            {
                throw new ArgumentNullException(nameof(fileProviderAccessor));
            }

            if (razorPagesOptions == null)
            {
                throw new ArgumentNullException(nameof(razorPagesOptions));
            }

            _fileProvider = fileProviderAccessor.FileProvider;
            _searchPattern = razorPagesOptions.Value.RootDirectory.TrimEnd('/') +  "/**/*.cshtml";
        }

        public IChangeToken GetChangeToken() => _fileProvider.Watch(_searchPattern);
    }
}
