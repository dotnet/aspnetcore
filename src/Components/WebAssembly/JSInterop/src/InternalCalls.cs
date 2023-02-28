// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.JavaScript;

namespace WebAssembly.JSInterop;

internal static partial class InternalCalls
{
    // This method only exists for backwards compatibility and will be removed in the future.
    // The exact namespace, type, and method name must match the corresponding entries
    // in driver.c in the Mono distribution.
    // See: https://github.com/mono/mono/blob/90574987940959fe386008a850982ea18236a533/sdks/wasm/src/driver.c#L318-L319
    [MethodImpl(MethodImplOptions.InternalCall)]
    [Obsolete]
    public static extern TRes InvokeJS<T0, T1, T2, TRes>(out string exception, ref JSCallInfo callInfo, [AllowNull] T0 arg0, [AllowNull] T1 arg1, [AllowNull] T2 arg2);

    [JSImport("Blazor._internal.invokeJSJson", "blazor-internal")]
    public static partial string InvokeJSJson(
        string identifier,
        [JSMarshalAs<JSType.Number>] long targetInstanceId,
        int resultType,
        string argsJson,
        [JSMarshalAs<JSType.Number>] long asyncHandle);

    [JSImport("Blazor._internal.endInvokeDotNetFromJS", "blazor-internal")]
    public static partial void EndInvokeDotNetFromJS(
        string? id,
        bool success,
        string jsonOrError);

    [JSImport("Blazor._internal.receiveByteArray", "blazor-internal")]
    public static partial void ReceiveByteArray(
        int id,
        byte[] data);
}
