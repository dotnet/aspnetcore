// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Components;
using Mono.WebAssembly.Interop;

namespace Microsoft.AspNetCore.Blazor.Services
{
    internal sealed class WebAssemblyJSRuntime : MonoWebAssemblyJSRuntime
    {
        private static readonly WebAssemblyJSRuntime _instance = new WebAssemblyJSRuntime();
        private static bool _initialized;

        public WebAssemblyJSRuntime()
        {
            JsonSerializerOptions.Converters.Add(new ElementReferenceJsonConverter());
        }

        public static WebAssemblyJSRuntime Instance
        {
            get
            {
                if (!_initialized)
                {
                    // This is executing in MonoWASM. Consequently we do not to have concern ourselves with thread safety.
                    _initialized = true;
                    Initialize(_instance);
                }

                return _instance;
            }
        }
    }
}
