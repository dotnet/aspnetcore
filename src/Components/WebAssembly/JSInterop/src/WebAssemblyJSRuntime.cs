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
        try
        {
            return InternalCalls.InvokeJSJson(identifier, targetInstanceId, (int)resultType, argsJson ?? "[]", 0);
        }
        catch (Exception ex)
        {
            throw new JSException(ex.Message, ex);
        }
    }

    /// <inheritdoc />
    protected override void BeginInvokeJS(long asyncHandle, string identifier, [StringSyntax(StringSyntaxAttribute.Json)] string? argsJson, JSCallResultType resultType, long targetInstanceId)
    {
        InternalCalls.InvokeJSJson(identifier, targetInstanceId, (int)resultType, argsJson ?? "[]", asyncHandle);
    }

    /// <inheritdoc />
    [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode", Justification = "TODO: This should be in the xml suppressions file, but can't be because https://github.com/mono/linker/issues/2006")]
    protected override void EndInvokeDotNet(DotNetInvocationInfo callInfo, in DotNetInvocationResult dispatchResult)
    {
        var resultJsonOrErrorMessage = dispatchResult.Success
            ? dispatchResult.ResultJson!
            : dispatchResult.Exception!.ToString();
        InternalCalls.EndInvokeDotNetFromJS(callInfo.CallId, dispatchResult.Success, resultJsonOrErrorMessage);
    }

    /// <inheritdoc />
    protected override void SendByteArray(int id, byte[] data)
    {
        InternalCalls.ReceiveByteArray(id, data);
    }
}
