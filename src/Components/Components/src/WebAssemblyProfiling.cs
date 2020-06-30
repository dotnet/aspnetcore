// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Runtime.CompilerServices;
using WebAssembly.JSInterop;

namespace Microsoft.AspNetCore.Components
{
    internal static class WebAssemblyProfiling
    {
        // TODO: Only actually call InvokeJSUnmarshalled if we're currently capturing
        // Need to maintain a static field set by JS to determine this
        // TODO: Can this be moved into the M.A.C.WebAssembly project via some abstraction?
        //       Or does that add too much overhead? Ideally we would have some abstraction
        //       so that Blazor Server can also use this and passes the calls through to
        //       .NET's EventSource or similar.

        public static void Start([CallerMemberName] string? name = null)
        {
            InternalCalls.InvokeJSUnmarshalled<string, object, object, object>(
                out _, "_blazorProfileStart", name!, null!, null!);
        }

        public static void End([CallerMemberName] string? name = null)
        {
            InternalCalls.InvokeJSUnmarshalled<string, object, object, object>(
                out _, "_blazorProfileEnd", name!, null!, null!);
        }
    }
}
