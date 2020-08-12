// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Razor.Language
{
    internal interface IRazorCodeGenerationOptionsFactoryProjectFeature : IRazorProjectEngineFeature
    {
        RazorCodeGenerationOptions Create(string fileKind, Action<RazorCodeGenerationOptionsBuilder> configure);
    }
}
