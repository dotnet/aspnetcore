// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Razor.Language;

namespace Microsoft.AspNetCore.Blazor.Razor
{
    public static class BlazorConfiguration 
    {
        public static readonly RazorConfiguration Default = new RazorConfiguration(
            RazorLanguageVersion.Version_2_1,
            "Blazor-0.1",
            Array.Empty<RazorExtension>());
    }
}
