// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.ExceptionServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace Microsoft.JSInterop.Infrastructure;

/// <summary>
/// Experimental registrations and helpers used by generated JS-invokable dispatch code when the
/// <c>Microsoft.JSInterop.GeneratedOnly</c> application switch is enabled.
/// This type is not intended to be used directly by application code, except for registering
/// metadata with <see cref="AddTypeInfoResolver(IJsonTypeInfoResolver)"/> and
/// <see cref="RegisterReferenceConverter{T}"/>.
/// </summary>
[Experimental("ASP0041", UrlFormat = "https://aka.ms/aspnet/analyzer/{0}")]
public sealed class GeneratedJSInterop
{
    private const string DotNetObjectRefKey = "__dotNetObject";

    private readonly ConcurrentDictionary<(string Assembly, string Method), Registration> _statics = new();
    private readonly ConcurrentDictionary<(Type Type, string Method), Registration> _instances = new();
    private static IJsonTypeInfoResolver[] s_typeInfoResolvers = [];
    private static readonly ConcurrentDictionary<Type, Func<JSRuntime, JsonConverter>> s_referenceConverters = new();

    internal GeneratedJSInterop()
    {
    }

    /// <summary>
    /// Invokes a registered callback and returns its serialized result. All arguments must be
    /// deserialized using the runtime's metadata before the first asynchronous suspension.
    /// </summary>
    /// <param name="runtime">The runtime owning converters and references.</param>
    /// <param name="target">The instance, or <see langword="null"/> for a static callback.</param>
    /// <param name="args">The arguments to deserialize before awaiting.</param>
    /// <returns>The serialized result.</returns>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public delegate ValueTask<string?> Invoker(JSRuntime runtime, object? target, JsonElement[] args);

    /// <summary>
    /// Gets the process-wide registrations populated by generated module initializers.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static GeneratedJSInterop Shared { get; } = new();

    /// <summary>
    /// Gets whether the <c>Microsoft.JSInterop.GeneratedOnly</c> application switch selects
    /// generated dispatch. Generated registration code checks this so that trimming can remove
    /// it when the switch is disabled.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    [FeatureSwitchDefinition("Microsoft.JSInterop.GeneratedOnly")]
    public static bool GeneratedOnly => AppContext.TryGetSwitch("Microsoft.JSInterop.GeneratedOnly", out var enabled) && enabled;

    /// <summary>
    /// Registers a static callback before dispatch begins.
    /// </summary>
    /// <param name="assembly">The assembly name supplied by JavaScript.</param>
    /// <param name="method">The JavaScript invocation identifier.</param>
    /// <param name="arity">The expected argument count.</param>
    /// <param name="invoke">The generated or handwritten strongly typed invocation delegate.</param>
    /// <param name="isAsync">Whether the delegate requires asynchronous invocation.</param>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public void RegisterStatic(string assembly, string method, int arity, Invoker invoke, bool isAsync = false)
    {
        ArgumentException.ThrowIfNullOrEmpty(assembly);
        ArgumentException.ThrowIfNullOrEmpty(method);
        ArgumentOutOfRangeException.ThrowIfNegative(arity);
        ArgumentNullException.ThrowIfNull(invoke);
        if (!_statics.TryAdd((assembly, method), new(arity, isAsync, invoke)))
        {
            throw new InvalidOperationException($"The assembly '{assembly}' already has a registration for '{method}'.");
        }
    }

    /// <summary>
    /// Registers an instance callback for a declaring type before dispatch begins. Calls on derived
    /// runtime types resolve through base types.
    /// </summary>
    /// <typeparam name="T">The declaring type.</typeparam>
    /// <param name="method">The JavaScript invocation identifier.</param>
    /// <param name="arity">The expected argument count.</param>
    /// <param name="invoke">The generated or handwritten strongly typed invocation delegate.</param>
    /// <param name="isAsync">Whether the delegate requires asynchronous invocation.</param>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public void RegisterInstance<T>(string method, int arity, Invoker invoke, bool isAsync = false) where T : class
    {
        ArgumentException.ThrowIfNullOrEmpty(method);
        ArgumentOutOfRangeException.ThrowIfNegative(arity);
        ArgumentNullException.ThrowIfNull(invoke);
        if (!_instances.TryAdd((typeof(T), method), new(arity, isAsync, invoke)))
        {
            throw new InvalidOperationException($"The type '{typeof(T).Name}' already has a registration for '{method}'.");
        }
    }

    /// <summary>
    /// Adds JSON metadata, typically for an assembly's internal callback payloads, that every runtime
    /// consults in generated-only mode. Resolvers may be added after a runtime is created, but before
    /// the runtime first resolves the types they provide.
    /// </summary>
    /// <param name="resolver">The source-generated metadata resolver.</param>
    public static void AddTypeInfoResolver(IJsonTypeInfoResolver resolver)
    {
        ArgumentNullException.ThrowIfNull(resolver);
        IJsonTypeInfoResolver[] current, updated;
        do
        {
            current = Volatile.Read(ref s_typeInfoResolvers);
            updated = [.. current, resolver];
        }
        while (Interlocked.CompareExchange(ref s_typeInfoResolvers, updated, current) != current);
    }

    internal static IJsonTypeInfoResolver SharedTypeInfoResolver { get; } = new SharedResolver();

    /// <summary>
    /// Registers a closed <see cref="DotNetObjectReference{TValue}"/> converter for <typeparamref name="T"/>,
    /// so that runtimes don't construct a generic converter type at runtime. Like
    /// <see cref="AddTypeInfoResolver(IJsonTypeInfoResolver)"/>, this may be called after a runtime
    /// is created, but before the runtime first serializes references to <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The referenced type.</typeparam>
    public static void RegisterReferenceConverter<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] T>() where T : class
        => s_referenceConverters.TryAdd(typeof(T), static runtime => new DotNetObjectReferenceJsonConverter<T>(runtime));

    internal static JsonConverter? CreateReferenceConverter(Type referencedType, JSRuntime runtime)
        => s_referenceConverters.TryGetValue(referencedType, out var create) ? create(runtime) : null;

    /// <summary>
    /// Gets metadata bound to the runtime's converters. Missing application metadata fails explicitly.
    /// </summary>
    /// <typeparam name="T">The serialized type.</typeparam>
    /// <param name="runtime">The runtime owning the serialization options.</param>
    /// <returns>The runtime-bound metadata.</returns>
    internal static JsonTypeInfo<T> Metadata<T>(JSRuntime runtime)
    {
        ArgumentNullException.ThrowIfNull(runtime);

        return (JsonTypeInfo<T>)runtime.JsonSerializerOptions.GetTypeInfo(typeof(T));
    }

    /// <summary>
    /// Deserializes one argument using runtime-bound metadata, rejecting a .NET object reference
    /// supplied for a parameter that is not a <see cref="DotNetObjectReference{TValue}"/>.
    /// Generated code calls this synchronously, before any asynchronous suspension.
    /// </summary>
    /// <typeparam name="T">The parameter type.</typeparam>
    /// <param name="runtime">The runtime owning converters and references.</param>
    /// <param name="value">The JSON argument.</param>
    /// <param name="methodIdentifier">The invoked identifier, used in error messages.</param>
    /// <param name="index">The zero-based parameter index.</param>
    /// <returns>The deserialized argument.</returns>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static T? ReadArgument<T>(JSRuntime runtime, JsonElement value, string methodIdentifier, int index)
    {
        if (value.ValueKind == JsonValueKind.Object && !IsDotNetObjectReference(typeof(T)))
        {
            using var properties = value.EnumerateObject();
            if (properties.MoveNext() && properties.Current.NameEquals(DotNetObjectRefKey))
            {
                var name = typeof(T).Name;
                throw new InvalidOperationException($"In call to '{methodIdentifier}', parameter of type '{name}' at index {index + 1} must be declared as type 'DotNetObjectRef<{name}>' to receive the incoming value.");
            }
        }

        return value.Deserialize(Metadata<T>(runtime));

        static bool IsDotNetObjectReference(Type type)
            => type.IsGenericType && type.GetGenericTypeDefinition() == typeof(DotNetObjectReference<>);
    }

    /// <summary>
    /// Serializes a synchronous result. Like reflection dispatch, a value whose runtime type
    /// differs from <typeparamref name="T"/> is serialized using metadata for its runtime type.
    /// </summary>
    /// <typeparam name="T">The declared return type.</typeparam>
    /// <param name="runtime">The runtime owning converters and references.</param>
    /// <param name="value">The result.</param>
    /// <returns>The serialized result, or <see langword="null"/> for a null value.</returns>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static ValueTask<string?> SerializeResult<T>(JSRuntime runtime, T value)
        => ValueTask.FromResult<string?>(Serialize(runtime, value));

    /// <summary>
    /// Awaits a task and serializes its result, reporting faults and cancellation like reflection dispatch.
    /// </summary>
    /// <typeparam name="T">The declared result type.</typeparam>
    /// <param name="runtime">The runtime owning converters and references.</param>
    /// <param name="task">The pending result.</param>
    /// <returns>The serialized result.</returns>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static async ValueTask<string?> SerializeResultAsync<T>(JSRuntime runtime, Task<T> task)
    {
        await ((Task)task).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
        ThrowIfUnsuccessful(task);
        return Serialize(runtime, task.Result);
    }

    /// <summary>
    /// Awaits a value task and serializes its result, reporting faults and cancellation like reflection dispatch.
    /// </summary>
    /// <typeparam name="T">The declared result type.</typeparam>
    /// <param name="runtime">The runtime owning converters and references.</param>
    /// <param name="task">The pending result.</param>
    /// <returns>The serialized result.</returns>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static ValueTask<string?> SerializeResultAsync<T>(JSRuntime runtime, ValueTask<T> task)
        => SerializeResultAsync(runtime, task.AsTask());

    /// <summary>
    /// Awaits a task without a result, reporting faults and cancellation like reflection dispatch.
    /// </summary>
    /// <param name="task">The pending operation.</param>
    /// <returns><see langword="null"/>, which asynchronous dispatch reports as a JSON null.</returns>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static async ValueTask<string?> CompleteAsync(Task task)
    {
        await task.ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
        ThrowIfUnsuccessful(task);
        return null;
    }

    /// <summary>
    /// Awaits a value task without a result, reporting faults and cancellation like reflection dispatch.
    /// </summary>
    /// <param name="task">The pending operation.</param>
    /// <returns><see langword="null"/>, which asynchronous dispatch reports as a JSON null.</returns>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static ValueTask<string?> CompleteAsync(ValueTask task)
        => CompleteAsync(task.AsTask());

    private static string? Serialize<T>(JSRuntime runtime, T value)
    {
        // Like reflection dispatch, synchronous calls return null and asynchronous calls report "null".
        if (value is null)
        {
            return null;
        }

        var type = value.GetType();
        return type == typeof(T)
            ? JsonSerializer.Serialize(value, Metadata<T>(runtime))
            : JsonSerializer.Serialize(value, runtime.JsonSerializerOptions.GetTypeInfo(type));
    }

    private static void ThrowIfUnsuccessful(Task task)
    {
        if (task.IsFaulted)
        {
            ExceptionDispatchInfo.Throw(task.Exception!.GetBaseException());
        }

        if (task.IsCanceled)
        {
            throw new TaskCanceledException(task);
        }
    }

    internal ValueTask<string?> Invoke(JSRuntime runtime, DotNetInvocationInfo info, string argsJson, bool synchronous)
    {
        try
        {
            object? target = null;
            Registration entry;
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
                    return default;
                }

                target = reference.Value;
                if (!TryGetInstance(target.GetType(), info.MethodIdentifier, out entry))
                {
                    throw new ArgumentException($"No generated instance registration for '{info.MethodIdentifier}'.");
                }
            }
            else if (!_statics.TryGetValue((info.AssemblyName!, info.MethodIdentifier), out entry) &&
                !Shared._statics.TryGetValue((info.AssemblyName!, info.MethodIdentifier), out entry))
            {
                throw new ArgumentException($"No generated static registration for '{info.AssemblyName}:{info.MethodIdentifier}'.");
            }

            if (synchronous && entry.Async)
            {
                throw new InvalidOperationException("An asynchronous callback requires invokeMethodAsync.");
            }

            // Like reflection dispatch, a parameterless method ignores the supplied arguments.
            JsonElement[] args = [];
            if (entry.Arity != 0)
            {
                args = JsonSerializer.Deserialize(argsJson, GeneratedInteropJsonContext.Default.JsonElementArray)
                    ?? throw new JsonException("Invalid JSON");
                if (args.Length < entry.Arity)
                {
                    throw new ArgumentException($"The call to '{info.MethodIdentifier}' expects '{entry.Arity}' parameters, but received '{args.Length}'.");
                }

                if (args.Length > entry.Arity)
                {
                    throw new JsonException($"Unexpected JSON token {ToTokenType(args[entry.Arity].ValueKind)}. Ensure that the call to `{info.MethodIdentifier}' is supplied with exactly '{entry.Arity}' parameters.");
                }
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

    // Reflection dispatch reports the extra argument's JsonTokenType.
    private static JsonTokenType ToTokenType(JsonValueKind kind) => kind switch
    {
        JsonValueKind.Object => JsonTokenType.StartObject,
        JsonValueKind.Array => JsonTokenType.StartArray,
        JsonValueKind.String => JsonTokenType.String,
        JsonValueKind.Number => JsonTokenType.Number,
        JsonValueKind.True => JsonTokenType.True,
        JsonValueKind.False => JsonTokenType.False,
        JsonValueKind.Null => JsonTokenType.Null,
        _ => JsonTokenType.None,
    };

    private bool TryGetInstance(Type targetType, string method, out Registration entry)
    {
        for (var type = targetType; type is not null; type = type.BaseType)
        {
            if (_instances.TryGetValue((type, method), out entry) ||
                Shared._instances.TryGetValue((type, method), out entry))
            {
                return true;
            }
        }

        entry = default;
        return false;
    }

    internal async Task BeginInvoke(JSRuntime runtime, DotNetInvocationInfo info, string argsJson)
    {
        DotNetInvocationResult result;
        try
        {
            result = new DotNetInvocationResult(await Invoke(runtime, info, argsJson, synchronous: false) ?? "null");
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

    private readonly record struct Registration(int Arity, bool Async, Invoker Invoke);

    private sealed class SharedResolver : IJsonTypeInfoResolver
    {
        public JsonTypeInfo? GetTypeInfo(Type type, JsonSerializerOptions options)
        {
            foreach (var resolver in Volatile.Read(ref s_typeInfoResolvers))
            {
                if (resolver.GetTypeInfo(type, options) is { } typeInfo)
                {
                    return typeInfo;
                }
            }

            return null;
        }
    }
}

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase, PropertyNameCaseInsensitive = true)]
[JsonSerializable(typeof(object[]))]
[JsonSerializable(typeof(JsonElement))]
[JsonSerializable(typeof(JsonElement[]))]
[JsonSerializable(typeof(string))]
[JsonSerializable(typeof(int))]
[JsonSerializable(typeof(long))]
[JsonSerializable(typeof(bool))]
[JsonSerializable(typeof(float))]
[JsonSerializable(typeof(double))]
internal partial class GeneratedInteropJsonContext : JsonSerializerContext;
