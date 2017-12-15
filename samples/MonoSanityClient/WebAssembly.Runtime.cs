// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Runtime.CompilerServices;

namespace WebAssembly
{
    internal static class Runtime
    {
        // The exact namespace, type, and method name must match the corresponding entry in
        // driver.c in the Mono distribution
        [MethodImpl(MethodImplOptions.InternalCall)]
        public static extern string InvokeJS(string str, out int resultIsException);
    }
}
