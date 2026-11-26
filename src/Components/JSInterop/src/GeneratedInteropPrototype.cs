// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Microsoft.JSInterop.Infrastructure;

namespace Microsoft.JSInterop;

/// <summary>
/// Experimental registrations standing in for generated JS-invokable dispatch code.
/// This prototype is not a complete Native AOT implementation.
/// </summary>
public sealed class GeneratedInteropPrototype
{
    private readonly Dictionary<(string Assembly, string Method), (int Arity, bool Async, Invoker Invoke)> _statics = new();
    private readonly Dictionary<(Type Type, string Method), (int Arity, bool Async, Invoker Invoke)> _instances = new();

    /// <summary>
    /// Invokes a registered callback and returns its serialized result. All arguments must be
    /// deserialized using the runtime's metadata before the first asynchronous suspension.
    /// </summary>
    /// <param name="runtime">The runtime owning converters and references.</param>
    /// <param name="target">The instance, or <see langword="null"/> for a static callback.</param>
    /// <param name="args">The arguments to deserialize before awaiting.</param>
    /// <returns>The serialized result.</returns>
    public delegate ValueTask<string?> Invoker(JSRuntime runtime, object? target, JsonElement[] args);

    /// <summary>
    /// Gets or sets the experimental startup hook for configuring runtimes before JSON options
    /// become read-only. Configure this hook before creating runtimes, not during dispatch.
    /// </summary>
    public static Action<JSRuntime, JsonSerializerOptions>? ConfigureRuntime { get; set; }

    [FeatureSwitchDefinition("Microsoft.JSInterop.GeneratedOnly")]
    internal static bool GeneratedOnly => AppContext.TryGetSwitch("Microsoft.JSInterop.GeneratedOnly", out var enabled) && enabled;

    /// <summary>
    /// Registers a static callback before dispatch begins.
    /// </summary>
    /// <param name="assembly">The assembly name supplied by JavaScript.</param>
    /// <param name="method">The JavaScript invocation identifier.</param>
    /// <param name="arity">The expected argument count.</param>
    /// <param name="invoke">The generated or handwritten strongly typed invocation delegate.</param>
    /// <param name="isAsync">Whether the delegate requires asynchronous invocation.</param>
    public void RegisterStatic(string assembly, string method, int arity, Invoker invoke, bool isAsync = false)
    {
        ArgumentException.ThrowIfNullOrEmpty(assembly);
        ArgumentException.ThrowIfNullOrEmpty(method);
        ArgumentOutOfRangeException.ThrowIfNegative(arity);
        ArgumentNullException.ThrowIfNull(invoke);
        _statics.Add((assembly, method), (arity, isAsync, invoke));
    }

    /// <summary>
    /// Registers an instance callback for an exact runtime type before dispatch begins.
    /// </summary>
    /// <typeparam name="T">The target type.</typeparam>
    /// <param name="method">The JavaScript invocation identifier.</param>
    /// <param name="arity">The expected argument count.</param>
    /// <param name="invoke">The generated or handwritten strongly typed invocation delegate.</param>
    /// <param name="isAsync">Whether the delegate requires asynchronous invocation.</param>
    public void RegisterInstance<T>(string method, int arity, Invoker invoke, bool isAsync = false) where T : class
    {
        ArgumentException.ThrowIfNullOrEmpty(method);
        ArgumentOutOfRangeException.ThrowIfNegative(arity);
        ArgumentNullException.ThrowIfNull(invoke);
        _instances.Add((typeof(T), method), (arity, isAsync, invoke));
    }

    /// <summary>
    /// Registers a closed reference converter without constructing a generic converter at runtime.
    /// </summary>
    /// <typeparam name="T">The referenced type.</typeparam>
    /// <param name="runtime">The runtime owning the references.</param>
    /// <param name="options">The runtime's mutable serialization options.</param>
    public static void RegisterReferenceConverter<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] T>(
        JSRuntime runtime, JsonSerializerOptions options) where T : class
    {
        ArgumentNullException.ThrowIfNull(runtime);
        ArgumentNullException.ThrowIfNull(options);
        options.Converters.Insert(0, new DotNetObjectReferenceJsonConverter<T>(runtime));
    }

    /// <summary>
    /// Gets metadata bound to the runtime's converters. Missing application metadata fails explicitly.
    /// </summary>
    /// <typeparam name="T">The serialized type.</typeparam>
    /// <param name="runtime">The runtime owning the serialization options.</param>
    /// <returns>The runtime-bound metadata.</returns>
    public static JsonTypeInfo<T> Metadata<T>(JSRuntime runtime)
    {
        ArgumentNullException.ThrowIfNull(runtime);

        return (JsonTypeInfo<T>)runtime.JsonSerializerOptions.GetTypeInfo(typeof(T));
    }

    internal ValueTask<string?> Invoke(JSRuntime runtime, DotNetInvocationInfo info, string argsJson, bool synchronous)
    {
        try
        {
            object? target = null;
            (int Arity, bool Async, Invoker Invoke) entry;
            if (info.DotNetObjectId != 0)
            {
                var reference = runtime.GetObjectReference(info.DotNetObjectId);
                if (info.AssemblyName is not null)
                {
                    throw new ArgumentException("Instance calls must not specify an assembly.");
                }

                if (info.MethodIdentifier == "__Dispose")
                {
                    reference.Dispose();
                    return ValueTask.FromResult<string?>("null");
                }

                target = reference.Value;
                if (!_instances.TryGetValue((target.GetType(), info.MethodIdentifier), out entry))
                {
                    throw new ArgumentException($"No generated instance registration for '{info.MethodIdentifier}'.");
                }
            }
            else if (!_statics.TryGetValue((info.AssemblyName!, info.MethodIdentifier), out entry))
            {
                throw new ArgumentException($"No generated static registration for '{info.AssemblyName}:{info.MethodIdentifier}'.");
            }

            if (synchronous && entry.Async)
            {
                throw new InvalidOperationException("An asynchronous callback requires invokeMethodAsync.");
            }

            var args = JsonSerializer.Deserialize(argsJson, GeneratedInteropJsonContext.Default.JsonElementArray)
                ?? throw new JsonException("Arguments must be an array, not null.");
            if (args.Length != entry.Arity)
            {
                throw new ArgumentException($"'{info.MethodIdentifier}' expects {entry.Arity} arguments, received {args.Length}.");
            }

            return entry.Invoke(runtime, target, args);
        }
        finally
        {
            // Registrations must consume byte arrays before awaiting, so overlapping calls cannot
            // revive arrays belonging to an earlier invocation.
            runtime.ByteArraysToBeRevived.Clear();
        }
    }

    internal async Task BeginInvoke(JSRuntime runtime, DotNetInvocationInfo info, string argsJson)
    {
        DotNetInvocationResult result;
        try
        {
            result = new DotNetInvocationResult(await Invoke(runtime, info, argsJson, synchronous: false));
        }
        catch (Exception ex)
        {
            result = new DotNetInvocationResult(ex, "InvocationFailure");
        }

        if (info.CallId is not null)
        {
            runtime.EndInvokeDotNet(info, in result);
        }
        else if (!result.Success)
        {
            Trace.TraceError("Generated JSInterop invocation failed: {0}", result.Exception);
        }
    }
}

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase, PropertyNameCaseInsensitive = true)]
[JsonSerializable(typeof(object[]))]
[JsonSerializable(typeof(JsonElement[]))]
[JsonSerializable(typeof(string))]
[JsonSerializable(typeof(int))]
[JsonSerializable(typeof(long))]
[JsonSerializable(typeof(bool))]
[JsonSerializable(typeof(double))]
internal partial class GeneratedInteropJsonContext : JsonSerializerContext;
