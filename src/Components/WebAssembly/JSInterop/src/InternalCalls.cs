// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.JavaScript;
using Microsoft.JSInterop.Implementation;

namespace WebAssembly.JSInterop;

internal interface IWebAssemblyInternalCalls : IJSInternalCalls
{
    [Obsolete]
    TRes InvokeJS<T0, T1, T2, TRes>(out string exception, ref JSCallInfo callInfo, [AllowNull] T0 arg0, [AllowNull] T1 arg1, [AllowNull] T2 arg2);
    string InvokeJSJson(string identifier, long targetInstanceId, int resultType, string marshalledCallArgsJson, long marshalledCallAsyncHandle);
    void EndInvokeDotNetFromJS(string? id, bool success, string jsonOrError);
    void ReceiveByteArray(int id, byte[] data);
}

/// <summary>
/// Methods that map to the functions compiled into the Mono WebAssembly runtime,
/// as defined by 'mono_add_internal_call' calls in driver.c.
/// Or inside Blazor's Boot.WebAssembly.ts
/// </summary>
internal partial class DefaultWebAssemblyInternalCalls : DefaultInternalCalls, IWebAssemblyInternalCalls
{
    internal static new readonly IWebAssemblyInternalCalls Instance = new DefaultWebAssemblyInternalCalls();

    [Obsolete]
    public TRes InvokeJS<T0, T1, T2, TRes>(out string exception, ref JSCallInfo callInfo, [AllowNull] T0 arg0, [AllowNull] T1 arg1, [AllowNull] T2 arg2) => _InvokeJS<T0, T1, T2, TRes>(out exception, ref callInfo, arg0, arg1, arg2);
    public string InvokeJSJson(string identifier, long targetInstanceId, int resultType, string marshalledCallArgsJson, long marshalledCallAsyncHandle) => _InvokeJSJson(identifier, targetInstanceId, resultType, marshalledCallArgsJson, marshalledCallAsyncHandle);
    public void EndInvokeDotNetFromJS(string? id, bool success, string jsonOrError) => EndInvokeDotNetFromJS(id, success, jsonOrError);
    public void ReceiveByteArray(int id, byte[] data) => ReceiveByteArray(id, data);

    // The exact namespace, type, and method names must match the corresponding entries
    // in driver.c in the Mono distribution
    /// See: https://github.com/mono/mono/blob/90574987940959fe386008a850982ea18236a533/sdks/wasm/src/driver.c#L318-L319
    [MethodImpl(MethodImplOptions.InternalCall)]
    [Obsolete]
    private static extern TRes _InvokeJS<T0, T1, T2, TRes>(out string exception, ref JSCallInfo callInfo, [AllowNull] T0 arg0, [AllowNull] T1 arg1, [AllowNull] T2 arg2);

    [JSImport("Blazor._internal.invokeJSJson")]
    private static partial string _InvokeJSJson(
        string identifier,
        [JSMarshalAs<JSType.Number>] long targetInstanceId,
        int resultType,
        string marshalledCallArgsJson,
        [JSMarshalAs<JSType.Number>] long marshalledCallAsyncHandle);

    [JSImport("Blazor._internal.endInvokeDotNetFromJS")]
    private static partial void _EndInvokeDotNetFromJS(
        string? id,
        bool success,
        string jsonOrError);

    [JSImport("Blazor._internal.receiveByteArray")]
    private static partial void _ReceiveByteArray(int id, byte[] data);
}
