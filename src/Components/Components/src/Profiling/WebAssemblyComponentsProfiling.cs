// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using WebAssembly.JSInterop;

namespace Microsoft.AspNetCore.Components.Profiling
{
    // Later on, we will likely want to move this into the WebAssembly package. However it needs to
    // be inlined into the Components package directly until we're ready to make the underlying
    // ComponentsProfile abstraction into a public API. It's possible that this API will never become
    // public, or that it will be replaced by something more standard for .NET, if it's possible to
    // make that work performantly on WebAssembly.

    internal class WebAssemblyComponentsProfiling : ComponentsProfiling
    {
        static bool IsCapturing = false;

        public static void SetCapturing(bool isCapturing)
        {
            IsCapturing = isCapturing;
        }

        public override void Start(string? name)
        {
            if (IsCapturing)
            {
                InternalCalls.InvokeJSUnmarshalled<string, object, object, object>(
                    out _, "_blazorProfileStart", name, null, null);
            }
        }

        public override void End(string? name)
        {
            if (IsCapturing)
            {
                InternalCalls.InvokeJSUnmarshalled<string, object, object, object>(
                    out _, "_blazorProfileEnd", name, null, null);
            }
        }
    }
}
