// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Microsoft.AspNetCore.Razor.Language;

namespace Microsoft.CodeAnalysis.Razor;

public sealed class DefaultMetadataReferenceFeature : RazorEngineFeatureBase, IMetadataReferenceFeature
{
    public IReadOnlyList<MetadataReference> References { get; set; }
}
