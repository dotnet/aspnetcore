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
        private readonly string[] _searchPatterns;
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

            // Search pattern that matches all cshtml files under the Pages RootDirectory
            var pagesRootSearchPattern = rootDirectory + "/**/*.cshtml";

            // pagesRootSearchPattern will miss _ViewImports outside the RootDirectory despite these influencing
            // compilation. e.g. when RootDirectory = /Dir1/Dir2, the search pattern will ignore changes to 
            // [/_ViewImports.cshtml, /Dir1/_ViewImports.cshtml]. We need to additionally account for these.
            var importFileAtPagesRoot = rootDirectory + "/" + templateEngine.Options.ImportsFileName;
            var additionalImportFilePaths = templateEngine.GetImportItems(importFileAtPagesRoot)
                .Select(item => item.FilePath);

            if (razorPagesOptions.Value.AllowAreas)
            {
                // Search pattern that matches all cshtml files under the Pages AreaRootDirectory
                var areaRootSearchPattern = "/Areas/**/*.cshtml";

                var importFileAtAreaPagesRoot = $"/Areas/{templateEngine.Options.ImportsFileName}";
                var importPathsOutsideAreaPagesRoot = templateEngine.GetImportItems(importFileAtAreaPagesRoot)
                    .Select(item => item.FilePath);

                additionalImportFilePaths = additionalImportFilePaths
                    .Concat(importPathsOutsideAreaPagesRoot)
                    .Distinct(StringComparer.OrdinalIgnoreCase);

                _searchPatterns = new[]
                {
                    pagesRootSearchPattern,
                    areaRootSearchPattern
                };
            }
            else
            {
                _searchPatterns = new[] { pagesRootSearchPattern, };
            }

            _additionalFilesToTrack = additionalImportFilePaths.ToArray();
        }

        public IChangeToken GetChangeToken()
        {
            var changeTokens = new IChangeToken[_additionalFilesToTrack.Length + _searchPatterns.Length];
            for (var i = 0; i < _additionalFilesToTrack.Length; i++)
            {
                changeTokens[i] = _fileProvider.Watch(_additionalFilesToTrack[i]);
            }

            for (var i = 0; i < _searchPatterns.Length; i++)
            {
                var wildcardChangeToken = _fileProvider.Watch(_searchPatterns[i]);
                changeTokens[_additionalFilesToTrack.Length + i] = wildcardChangeToken;
            }

            return new CompositeChangeToken(changeTokens);
        }
    }
}
