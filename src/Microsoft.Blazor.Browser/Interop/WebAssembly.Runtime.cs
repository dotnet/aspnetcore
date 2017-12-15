// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Blazor.Browser.Interop;
using System.Runtime.CompilerServices;

namespace WebAssembly
{
    internal static class Runtime
    {
        public static string EvaluateJavaScript(string expression)
        {
            var result = InvokeJS(expression, out var resultIsException);

            if (resultIsException != 0)
            {
                throw new JavaScriptException(result);
            }

            return result;
        }

        // The exact namespace, type, and method name must match the corresponding entry in
        // driver.c in the Mono distribution
        [MethodImpl(MethodImplOptions.InternalCall)]
        static extern string InvokeJS(string str, out int resultIsException);
    }
}
