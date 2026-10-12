// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices.JavaScript;

namespace WebAssembly.JSInterop;

internal static partial class InternalCalls
{
    [JSImport("Blazor._internal.invokeJSJson", "blazor-internal")]
    public static partial string InvokeJSJson(
        string identifier,
        [JSMarshalAs<JSType.Number>] long targetInstanceId,
        int resultType,
        string argsJson,
        [JSMarshalAs<JSType.Number>] long asyncHandle,
        int callType);

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
