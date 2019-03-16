// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Mono.WebAssembly.Interop
{
    public partial class MonoWebAssemblyJSRuntime : Microsoft.JSInterop.JSInProcessRuntimeBase
    {
        public MonoWebAssemblyJSRuntime() { }
        protected override void BeginInvokeJS(long asyncHandle, string identifier, string argsJson) { }
        protected override string InvokeJS(string identifier, string argsJson) { throw null; }
        public TRes InvokeUnmarshalled<TRes>(string identifier) { throw null; }
        public TRes InvokeUnmarshalled<T0, TRes>(string identifier, T0 arg0) { throw null; }
        public TRes InvokeUnmarshalled<T0, T1, TRes>(string identifier, T0 arg0, T1 arg1) { throw null; }
        public TRes InvokeUnmarshalled<T0, T1, T2, TRes>(string identifier, T0 arg0, T1 arg1, T2 arg2) { throw null; }
    }
}
