// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Razor.Language;

namespace Microsoft.VisualStudio.Editor.Razor
{
    internal class DelegatingTemplateEngine : RazorTemplateEngine
    {
        private readonly RazorProjectEngine _inner;

        public DelegatingTemplateEngine(RazorProjectEngine inner) 
            : base(inner.Engine, inner.FileSystem)
        {
            if (inner == null)
            {
                throw new ArgumentNullException(nameof(inner));
            }

            _inner = inner;
        }
        
        public override IEnumerable<RazorProjectItem> GetImportItems(RazorProjectItem projectItem)
        {
            var feature = _inner.ProjectFeatures.OfType<IImportProjectFeature>().FirstOrDefault();
            if (feature == null)
            {
                return Array.Empty<RazorProjectItem>();
            }

            var imports = feature.GetImports(projectItem);
            var physicalImports = imports.Where(import => import.FilePath != null);

            return physicalImports;
        }
    }
}