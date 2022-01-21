// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace WebAssembly.JSInterop;

/// <summary>
/// Methods that map to the functions compiled into the Mono WebAssembly runtime,
/// as defined by 'mono_add_internal_call' calls in driver.c.
/// </summary>
internal static class InternalCalls
{
    // The exact namespace, type, and method names must match the corresponding entries
    // in driver.c in the Mono distribution
    /// See: https://github.com/mono/mono/blob/90574987940959fe386008a850982ea18236a533/sdks/wasm/src/driver.c#L318-L319

    [MethodImpl(MethodImplOptions.InternalCall)]
    public static extern TRes InvokeJS<T0, T1, T2, TRes>(out string exception, ref JSCallInfo callInfo, [AllowNull] T0 arg0, [AllowNull] T1 arg1, [AllowNull] T2 arg2);
}
