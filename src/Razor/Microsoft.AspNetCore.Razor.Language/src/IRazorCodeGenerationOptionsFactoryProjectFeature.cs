// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.AspNetCore.Razor.Language;

internal interface IRazorCodeGenerationOptionsFactoryProjectFeature : IRazorProjectEngineFeature
{
    RazorCodeGenerationOptions Create(string fileKind, Action<RazorCodeGenerationOptionsBuilder> configure);
}
