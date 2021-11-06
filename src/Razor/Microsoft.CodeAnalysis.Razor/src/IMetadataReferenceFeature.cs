// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Razor.Language;
using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.Razor;

public interface IMetadataReferenceFeature : IRazorEngineFeature
{
    IReadOnlyList<MetadataReference> References { get; }
}
