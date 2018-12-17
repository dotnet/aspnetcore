// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.AspNetCore.Razor.Language;

namespace Microsoft.CodeAnalysis.Razor
{
    public sealed class DefaultMetadataReferenceFeature : RazorEngineFeatureBase, IMetadataReferenceFeature
    {
        public IReadOnlyList<MetadataReference> References { get; set; }
    }
}
