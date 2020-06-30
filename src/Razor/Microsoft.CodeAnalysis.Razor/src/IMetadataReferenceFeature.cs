// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Razor.Language;
using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.Razor
{
    public interface IMetadataReferenceFeature : IRazorEngineFeature
    {
        IReadOnlyList<MetadataReference> References { get; }
    }
}
