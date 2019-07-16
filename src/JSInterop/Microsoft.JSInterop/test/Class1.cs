// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;

namespace Microsoft.JSInterop
{
    internal class TestJSRuntime : JSRuntimeBase
    {
        protected override void BeginInvokeJS(long asyncHandle, string identifier, string argsJson)
        {
            throw new NotImplementedException();
        }

        public static async Task WithJSRuntime(Action<JSRuntimeBase> testCode)
        {
            // Since the tests rely on the asynclocal JSRuntime.Current, ensure we
            // are on a distinct async context with a non-null JSRuntime.Current
            await Task.Yield();

            var runtime = new TestJSRuntime();
            JSRuntime.SetCurrentJSRuntime(runtime);
            testCode(runtime);
        }
    }
}
