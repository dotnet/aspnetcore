// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.JSInterop;
using Mono.WebAssembly.Interop;

namespace Microsoft.AspNetCore.Components.Browser.Services
{
    /// <summary>
    /// Temporary mechanism for registering the Mono JS runtime. Developers do not need to
    /// use this directly, and it will be removed shortly.
    /// </summary>
    public class ActivateMonoJSRuntime
    {
        static object ensureActivatedReturnValue = new object();

        static ActivateMonoJSRuntime()
        {
            // Temporarily enable MonoWebAssemblyJSRuntime on this class constructor
            // Later it will become part of the app startup and config mechanism
            JSRuntime.SetCurrentJSRuntime(new MonoWebAssemblyJSRuntime());
        }

        /// <summary>
        /// Temporary mechanism for registering the Mono JS runtime. Developers do not need to
        /// use this directly, and it will be removed shortly.
        /// The return value is intended to be used with GC.KeepAlive purely as a way of ensuring
        /// the invocation doesn't get stripped out by the linker (if it's smart enough to do so).
        /// </summary>
        public static object EnsureActivated()
            => ensureActivatedReturnValue;
    }
}
