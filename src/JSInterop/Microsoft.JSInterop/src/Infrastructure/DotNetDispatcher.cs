// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Reflection.Metadata;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Text.Json;

[assembly: MetadataUpdateHandler(typeof(Microsoft.JSInterop.Infrastructure.ReflectionJSInvokableMethodResolver.MetadataUpdateHandler))]

namespace Microsoft.JSInterop.Infrastructure;

/// <summary>
/// Provides methods that receive incoming calls from JS to .NET.
/// </summary>
public static class DotNetDispatcher
{
    private const string DisposeDotNetObjectReferenceMethodName = "__Dispose";

    // What JsonSerializer.Serialize produces for a null result, which is what a descriptor returning no
    // value has to be reported as so the two dispatch paths look identical on the wire.
    private const string JsonNullLiteral = "null";

    internal static readonly JsonEncodedText DotNetObjectRefKey = JsonEncodedText.Encode("__dotNetObject");

    private static readonly IJSInvokableMethodResolver _reflectionResolver = CreateReflectionResolver();

    /// <summary>
    /// Receives a call from JS to .NET, locating and invoking the specified method.
    /// </summary>
    /// <param name="jsRuntime">The <see cref="JSRuntime"/>.</param>
    /// <param name="invocationInfo">The <see cref="DotNetInvocationInfo"/>.</param>
    /// <param name="argsJson">A JSON representation of the parameters.</param>
    /// <returns>A JSON representation of the return value, or null.</returns>
    public static string? Invoke(JSRuntime jsRuntime, in DotNetInvocationInfo invocationInfo, [StringSyntax(StringSyntaxAttribute.Json)] string argsJson)
    {
        // This method doesn't need [JSInvokable] because the platform is responsible for having
        // some way to dispatch calls here. The logic inside here is the thing that checks whether
        // the targeted method has [JSInvokable]. It is not itself subject to that restriction,
        // because there would be nobody to police that. This method *is* the police.

        IDotNetObjectReference? targetInstance = default;
        if (invocationInfo.DotNetObjectId != default)
        {
            targetInstance = jsRuntime.GetObjectReference(invocationInfo.DotNetObjectId);
        }

        var result = ResolveAndInvoke(jsRuntime, invocationInfo, targetInstance, argsJson);
        if (!result.IsCompleted)
        {
            throw new InvalidOperationException(
                $"The call to '{invocationInfo.MethodIdentifier}' returned a result that had not completed. " +
                $"Only methods that complete synchronously can be invoked through '{nameof(Invoke)}'.");
        }

        return result.GetAwaiter().GetResult();
    }

    /// <summary>
    /// Receives a call from JS to .NET, locating and invoking the specified method asynchronously.
    /// </summary>
    /// <param name="jsRuntime">The <see cref="JSRuntime"/>.</param>
    /// <param name="invocationInfo">The <see cref="DotNetInvocationInfo"/>.</param>
    /// <param name="argsJson">A JSON representation of the parameters.</param>
    /// <returns>A JSON representation of the return value, or null.</returns>
    public static void BeginInvokeDotNet(JSRuntime jsRuntime, DotNetInvocationInfo invocationInfo, [StringSyntax(StringSyntaxAttribute.Json)] string argsJson)
    {
        // This method doesn't need [JSInvokable] because the platform is responsible for having
        // some way to dispatch calls here. The logic inside here is the thing that checks whether
        // the targeted method has [JSInvokable]. It is not itself subject to that restriction,
        // because there would be nobody to police that. This method *is* the police.

        // Using ExceptionDispatchInfo here throughout because we want to always preserve
        // original stack traces.

        var callId = invocationInfo.CallId;

        ValueTask<string?> invocationResult = default;
        var invoked = false;
        ExceptionDispatchInfo? syncException = null;
        IDotNetObjectReference? targetInstance = null;
        try
        {
            if (invocationInfo.DotNetObjectId != default)
            {
                targetInstance = jsRuntime.GetObjectReference(invocationInfo.DotNetObjectId);
            }

            invocationResult = ResolveAndInvoke(jsRuntime, invocationInfo, targetInstance, argsJson);
            invoked = true;
        }
        catch (Exception ex)
        {
            syncException = ExceptionDispatchInfo.Capture(ex);
        }

        // If there was no callId, the caller does not want to be notified about the result
        if (callId == null)
        {
            if (invoked && !invocationResult.IsCompletedSuccessfully)
            {
                _ = invocationResult.AsTask();
            }

            return;
        }
        else if (syncException != null)
        {
            // Threw synchronously, let's respond.
            jsRuntime.EndInvokeDotNet(invocationInfo, new DotNetInvocationResult(syncException.SourceException, "InvocationFailure"));
        }
        else
        {
            EndInvokeDotNetAfterInvocation(invocationResult, jsRuntime, invocationInfo);
        }
    }

    private static ValueTask<string?> ResolveAndInvoke(
        JSRuntime jsRuntime,
        in DotNetInvocationInfo callInfo,
        IDotNetObjectReference? objectReference,
        string argsJson)
    {
        var methodIdentifier = callInfo.MethodIdentifier;
        JSInvokableMethodInfo methodInfo;
        if (objectReference is null)
        {
            methodInfo = new JSInvokableMethodInfo(callInfo.AssemblyName, null, methodIdentifier);
        }
        else
        {
            if (callInfo.AssemblyName is not null)
            {
                throw new ArgumentException($"For instance method calls, '{nameof(callInfo.AssemblyName)}' should be null. Value received: '{callInfo.AssemblyName}'.");
            }

            if (string.Equals(DisposeDotNetObjectReferenceMethodName, methodIdentifier, StringComparison.Ordinal))
            {
                objectReference.Dispose();
                return default;
            }

            methodInfo = new JSInvokableMethodInfo(null, objectReference.Value.GetType(), methodIdentifier);
        }

        var descriptor = ResolveMethod(methodInfo);
        try
        {
            return descriptor.Invoke(objectReference?.Value, argsJson ?? "[]", jsRuntime.JsonSerializerOptions);
        }
        finally
        {
            jsRuntime.ByteArraysToBeRevived.Clear();
        }
    }

    private static JSInvokableMethodDescriptor ResolveMethod(in JSInvokableMethodInfo methodInfo)
    {
        if (_reflectionResolver.TryResolve(methodInfo, out var descriptor))
        {
            return descriptor;
        }

        if (methodInfo.IsStatic)
        {
            throw new ArgumentException($"The assembly '{methodInfo.AssemblyName}' does not contain a public invokable method with [{nameof(JSInvokableAttribute)}(\"{methodInfo.Identifier}\")].");
        }

        throw new ArgumentException($"The type '{methodInfo.TargetType!.Name}' does not contain a public invokable method with [{nameof(JSInvokableAttribute)}(\"{methodInfo.Identifier}\")].");
    }

    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "This factory creates the reflection-only compatibility resolver.")]
    [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "This factory creates the reflection-only compatibility resolver.")]
    private static ReflectionJSInvokableMethodResolver CreateReflectionResolver()
        => new();

    private static void EndInvokeDotNetAfterInvocation(ValueTask<string?> result, JSRuntime jsRuntime, in DotNetInvocationInfo invocationInfo)
    {
        if (result.IsCompletedSuccessfully)
        {
            jsRuntime.EndInvokeDotNet(invocationInfo, new DotNetInvocationResult(result.Result ?? JsonNullLiteral));
            return;
        }

        var capturedInvocationInfo = invocationInfo;
        result.AsTask().ContinueWith(
            task => EndInvokeDotNetAfterInvocationTask(task, jsRuntime, capturedInvocationInfo),
            TaskScheduler.Current);
    }

    private static void EndInvokeDotNetAfterInvocationTask(Task<string?> task, JSRuntime jsRuntime, in DotNetInvocationInfo invocationInfo)
    {
        string? result;
        try
        {
            result = task.GetAwaiter().GetResult();
        }
        catch (Exception exception)
        {
            jsRuntime.EndInvokeDotNet(invocationInfo, new DotNetInvocationResult(exception, "InvocationFailure"));
            return;
        }

        jsRuntime.EndInvokeDotNet(invocationInfo, new DotNetInvocationResult(result ?? JsonNullLiteral));
    }

    /// <summary>
    /// Receives notification that a call from .NET to JS has finished, marking the
    /// associated <see cref="Task"/> as completed.
    /// </summary>
    /// <remarks>
    /// All exceptions from <see cref="EndInvokeJS"/> are caught
    /// are delivered via JS interop to the JavaScript side when it requests confirmation, as
    /// the mechanism to call <see cref="EndInvokeJS"/> relies on
    /// using JS->.NET interop. This overload is meant for directly triggering completion callbacks
    /// for .NET -> JS operations without going through JS interop, so the callsite for this
    /// method is responsible for handling any possible exception generated from the arguments
    /// passed in as parameters.
    /// </remarks>
    /// <param name="jsRuntime">The <see cref="JSRuntime"/>.</param>
    /// <param name="arguments">The serialized arguments for the callback completion.</param>
    /// <exception cref="Exception">
    /// This method can throw any exception either from the argument received or as a result
    /// of executing any callback synchronously upon completion.
    /// </exception>
    public static void EndInvokeJS(JSRuntime jsRuntime, [StringSyntax(StringSyntaxAttribute.Json)] string arguments)
    {
        var utf8JsonBytes = Encoding.UTF8.GetBytes(arguments);

        // The payload that we're trying to parse is of the format
        // [ taskId: long, success: boolean, value: string? | object ]
        // where value is the .NET type T originally specified on InvokeAsync<T> or the error string if success is false.
        // We parse the first two arguments and call in to JSRuntimeBase to deserialize the actual value.

        var reader = new Utf8JsonReader(utf8JsonBytes);

        if (!reader.Read() || reader.TokenType != JsonTokenType.StartArray)
        {
            throw new JsonException("Invalid JSON");
        }

        reader.Read();
        var taskId = reader.GetInt64();

        reader.Read();
        var success = reader.GetBoolean();

        reader.Read();
        if (!jsRuntime.EndInvokeJS(taskId, success, ref reader))
        {
            return;
        }

        if (!reader.Read() || reader.TokenType != JsonTokenType.EndArray)
        {
            throw new JsonException("Invalid JSON");
        }
    }

    /// <summary>
    /// Accepts the byte array data being transferred from JS to DotNet.
    /// </summary>
    /// <param name="jsRuntime">The <see cref="JSRuntime"/>.</param>
    /// <param name="id">Identifier for the byte array being transferred.</param>
    /// <param name="data">Byte array to be transferred from JS.</param>
    public static void ReceiveByteArray(JSRuntime jsRuntime, int id, byte[] data)
    {
        jsRuntime.ReceiveByteArray(id, data);
    }

}
