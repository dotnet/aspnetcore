// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Microsoft.JSInterop.Infrastructure;
using WebAssembly.JSInterop;

namespace Microsoft.JSInterop.WebAssembly;

/// <summary>
/// Provides methods for invoking JavaScript functions for applications running
/// on the Mono WebAssembly runtime.
/// </summary>
public abstract class WebAssemblyJSRuntime : JSInProcessRuntime
{
    /// <summary>
    /// Initializes a new instance of <see cref="WebAssemblyJSRuntime"/>.
    /// </summary>
    protected WebAssemblyJSRuntime()
    {
        JsonSerializerOptions.Converters.Insert(0, new WebAssemblyJSObjectReferenceJsonConverter(this));
    }

    /// <inheritdoc />
    protected override string InvokeJS(string identifier, [StringSyntax(StringSyntaxAttribute.Json)] string? argsJson, JSCallResultType resultType, long targetInstanceId)
    {
        return InternalCalls.InvokeJSJson(
            identifier,
            targetInstanceId,
            (int)resultType,
            argsJson ?? "[]",
            (int)JSCallType.FunctionCall
        );
    }

    /// <inheritdoc />
    protected override string InvokeJS(in JSInvocationInfo invocationInfo)
    {
        try
        {
            return InternalCalls.InvokeJSJson(
                invocationInfo.Identifier,
                invocationInfo.TargetInstanceId,
                (int)invocationInfo.ResultType,
                invocationInfo.ArgsJson,
                (int)invocationInfo.CallType
            );
        }
        catch (Exception ex)
        {
            throw new JSException(ex.Message, ex);
        }
    }

    /// <inheritdoc />
    protected override void BeginInvokeJS(long asyncHandle, string identifier, [StringSyntax(StringSyntaxAttribute.Json)] string? argsJson, JSCallResultType resultType, long targetInstanceId)
    {
        // Dead code: async JS calls now use InvokeJSAsync (direct Promise→Task path).
        // This override is only retained because the base class declares it as abstract.
        throw new NotSupportedException();
    }

    /// <inheritdoc />
    protected override Task<string?>? InvokeJSAsync(in JSInvocationInfo invocationInfo)
    {
        return InternalCalls.InvokeJSJsonAsync(
            invocationInfo.Identifier,
            invocationInfo.TargetInstanceId,
            (int)invocationInfo.ResultType,
            invocationInfo.ArgsJson,
            (int)invocationInfo.CallType);
    }

    /// <inheritdoc />
    protected override void EndInvokeDotNet(DotNetInvocationInfo callInfo, in DotNetInvocationResult dispatchResult)
    {
        // This is intentionally a no-op for WebAssembly. The JS→.NET async invocation path uses
        // InvokeDotNetAsync (JSExport returning Task<string?>) which completes via Promise resolution,
        // bypassing the begin/end callback pattern.
    }

    /// <inheritdoc />
    protected override void SendByteArray(int id, byte[] data)
    {
        InternalCalls.ReceiveByteArray(id, data);
    }
}
