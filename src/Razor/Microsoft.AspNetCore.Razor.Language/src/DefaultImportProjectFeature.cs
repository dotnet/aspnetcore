// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

namespace Microsoft.AspNetCore.Razor.Language;

internal class DefaultImportProjectFeature : RazorProjectEngineFeatureBase, IImportProjectFeature
{
    public IReadOnlyList<RazorProjectItem> GetImports(RazorProjectItem projectItem) => Array.Empty<RazorProjectItem>();
}
