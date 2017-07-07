// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Linq;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Razor.Internal;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Mvc.RazorPages.Internal
{
    public class PageActionDescriptorChangeProvider : IActionDescriptorChangeProvider
    {
        private readonly IFileProvider _fileProvider;
        private readonly string _searchPattern;
        private readonly string[] _additionalFilesToTrack;

        public PageActionDescriptorChangeProvider(
            RazorTemplateEngine templateEngine,
            IRazorViewEngineFileProviderAccessor fileProviderAccessor,
            IOptions<RazorPagesOptions> razorPagesOptions)
        {
            if (templateEngine == null)
            {
                throw new ArgumentNullException(nameof(templateEngine));
            }

            if (fileProviderAccessor == null)
            {
                throw new ArgumentNullException(nameof(fileProviderAccessor));
            }

            if (razorPagesOptions == null)
            {
                throw new ArgumentNullException(nameof(razorPagesOptions));
            }

            _fileProvider = fileProviderAccessor.FileProvider;

            var rootDirectory = razorPagesOptions.Value.RootDirectory;
            Debug.Assert(!string.IsNullOrEmpty(rootDirectory));
            rootDirectory = rootDirectory.TrimEnd('/');

            var importFileAtPagesRoot = rootDirectory + "/" + templateEngine.Options.ImportsFileName;
            _additionalFilesToTrack = templateEngine.GetImportItems(importFileAtPagesRoot)
                .Select(item => item.FilePath)
                .ToArray();

            _searchPattern = rootDirectory + "/**/*.cshtml";
        }

        public IChangeToken GetChangeToken()
        {
            var wildcardChangeToken = _fileProvider.Watch(_searchPattern);
            if (_additionalFilesToTrack.Length == 0)
            {
                return wildcardChangeToken;
            }

            var changeTokens = new IChangeToken[_additionalFilesToTrack.Length + 1];
            for (var i = 0; i < _additionalFilesToTrack.Length; i++)
            {
                changeTokens[i] = _fileProvider.Watch(_additionalFilesToTrack[i]);
            }

            changeTokens[changeTokens.Length - 1] = wildcardChangeToken;
            return new CompositeChangeToken(changeTokens);
        }
    }
}
