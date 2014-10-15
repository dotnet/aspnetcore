// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNet.FileSystems;
using Microsoft.Framework.OptionsModel;

namespace Microsoft.AspNet.Mvc.Razor
{
    /// <inheritdoc />
    public class ViewStartProvider : IViewStartProvider
    {
        private readonly IFileSystem _fileSystem;
        private readonly IRazorPageFactory _pageFactory;

        public ViewStartProvider(IRazorPageFactory pageFactory,
                                 IOptions<RazorViewEngineOptions> optionsAccessor)
        {
            _fileSystem = optionsAccessor.Options.FileSystem;
            _pageFactory = pageFactory;
        }

        /// <inheritdoc />
        public IEnumerable<IRazorPage> GetViewStartPages([NotNull] string path)
        {
            var viewStartLocations = ViewStartUtility.GetViewStartLocations(_fileSystem, path);
            var viewStarts = viewStartLocations.Select(_pageFactory.CreateInstance)
                                               .Where(p => p != null)
                                               .ToArray();

            // GetViewStartLocations return ViewStarts inside-out that is the _ViewStart closest to the page 
            // is the first: e.g. [ /Views/Home/_ViewStart, /Views/_ViewStart, /_ViewStart ]
            // However they need to be executed outside in, so we'll reverse the sequence.
            Array.Reverse(viewStarts);

            return viewStarts;
        }
    }
}