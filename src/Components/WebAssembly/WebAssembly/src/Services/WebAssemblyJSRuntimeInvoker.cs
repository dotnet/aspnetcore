// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.JSInterop.WebAssembly;

namespace Microsoft.AspNetCore.Components.WebAssembly.Services
{
    /// <summary>
    /// This class exists to enable unit testing for code that needs to call
    /// <see cref="WebAssemblyJSRuntime.InvokeUnmarshalled{T0, T1, T2, TResult}(string, T0, T1, T2)"/>.
    ///
    /// We should only use this in non-perf-critical code paths (for example, during hosting startup,
    /// where we only call this a fixed number of times, and not during rendering where it might be
    /// called arbitrarily frequently due to application logic). In perf-critical code paths, use
    /// <see cref="DefaultWebAssemblyJSRuntime.Instance"/> and call it directly.
    ///
    /// It might not ultimately make any difference but we won't know until we integrate AoT support.
    /// When AoT is used, it's possible that virtual dispatch will force fallback on the interpreter.
    /// </summary>
    internal class WebAssemblyJSRuntimeInvoker
    {
        public static WebAssemblyJSRuntimeInvoker Instance = new WebAssemblyJSRuntimeInvoker();

        public virtual TResult InvokeUnmarshalled<T0, T1, T2, TResult>(string identifier, T0 arg0, T1 arg1, T2 arg2)
            => DefaultWebAssemblyJSRuntime.Instance.InvokeUnmarshalled<T0, T1, T2, TResult>(identifier, arg0, arg1, arg2);
    }
}
