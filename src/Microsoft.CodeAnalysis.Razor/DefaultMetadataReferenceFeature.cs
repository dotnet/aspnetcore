// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.AspNetCore.Razor.Language;

namespace Microsoft.CodeAnalysis.Razor
{
    public class DefaultMetadataReferenceFeature : IMetadataReferenceFeature
    {
        public RazorEngine Engine { get; set; }

        public IReadOnlyList<MetadataReference> References { get; set; }
    }
}
