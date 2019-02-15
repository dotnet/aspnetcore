// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Mono.WebAssembly.Interop;

namespace Microsoft.AspNetCore.Blazor.Services
{
    internal static class WebAssemblyJSRuntime
    {
        public static readonly MonoWebAssemblyJSRuntime Instance = new MonoWebAssemblyJSRuntime();
    }
}
