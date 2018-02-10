// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.AspNetCore.Razor.Language
{
    internal class DefaultRazorImportFeature : RazorProjectEngineFeatureBase, IRazorImportFeature
    {
        public IReadOnlyList<RazorSourceDocument> GetImports(RazorProjectItem projectItem) => Array.Empty<RazorSourceDocument>();
    }
}
