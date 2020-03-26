// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Runtime.CompilerServices;

namespace WebAssembly.JSInterop
{
    /// <summary>
    /// Methods that map to the functions compiled into the Mono WebAssembly runtime,
    /// as defined by 'mono_add_internal_call' calls in driver.c
    /// </summary>
    internal class InternalCalls
    {
        // The exact namespace, type, and method names must match the corresponding entries
        // in driver.c in the Mono distribution

        // We're passing asyncHandle by ref not because we want it to be writable, but so it gets
        // passed as a pointer (4 bytes). We can pass 4-byte values, but not 8-byte ones.
        [MethodImpl(MethodImplOptions.InternalCall)]
        public static extern string InvokeJSMarshalled(out string exception, ref long asyncHandle, string functionIdentifier, string argsJson);

        [MethodImpl(MethodImplOptions.InternalCall)]
        public static extern TRes InvokeJSUnmarshalled<T0, T1, T2, TRes>(out string exception, string functionIdentifier, T0 arg0, T1 arg1, T2 arg2);
    }
}
