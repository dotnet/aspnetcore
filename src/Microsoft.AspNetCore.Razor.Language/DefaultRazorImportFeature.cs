// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.AspNetCore.Razor.Language
{
    internal class DefaultRazorImportFeature : IRazorImportFeature
    {
        public RazorProjectEngine ProjectEngine { get; set; }

        public IReadOnlyList<RazorSourceDocument> GetImports(string sourceFilePath) => Array.Empty<RazorSourceDocument>();
    }
}
