// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Reflection.Metadata;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Internal;
using static Microsoft.AspNetCore.Internal.LinkerFlags;

[assembly: MetadataUpdateHandler(typeof(Microsoft.JSInterop.Infrastructure.DotNetDispatcher.MetadataUpdateHandler))]

namespace Microsoft.JSInterop.Infrastructure;

/// <summary>
/// Provides methods that receive incoming calls from JS to .NET.
/// </summary>
public static class DotNetDispatcher
{
    private const string DisposeDotNetObjectReferenceMethodName = "__Dispose";

    internal static readonly JsonEncodedText DotNetObjectRefKey = JsonEncodedText.Encode("__dotNetObject");

    private static readonly ConcurrentDictionary<AssemblyKey, IReadOnlyDictionary<string, (MethodInfo, Type[])>> _cachedMethodsByAssembly = new();

    private static readonly ConcurrentDictionary<Type, IReadOnlyDictionary<string, (MethodInfo, Type[])>> _cachedMethodsByType = new();

    private static readonly ConcurrentDictionary<Type, Func<object, Task>> _cachedConvertToTaskByType = new();

    private static readonly MethodInfo _taskConverterMethodInfo = typeof(DotNetDispatcher).GetMethod(nameof(CreateValueTaskConverter), BindingFlags.NonPublic | BindingFlags.Static)!;

    /// <summary>
    /// Receives a call from JS to .NET, locating and invoking the specified method.
    /// </summary>
    /// <param name="jsRuntime">The <see cref="JSRuntime"/>.</param>
    /// <param name="invocationInfo">The <see cref="DotNetInvocationInfo"/>.</param>
    /// <param name="argsJson">A JSON representation of the parameters.</param>
    /// <returns>A JSON representation of the return value, or null.</returns>
    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "We expect application code is configured to ensure return types of JSInvokable methods are retained.")]
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

        var syncResult = InvokeSynchronously(jsRuntime, invocationInfo, targetInstance, argsJson);
        if (syncResult == null)
        {
            return null;
        }

        return JsonSerializer.Serialize(syncResult, jsRuntime.JsonSerializerOptions);
    }

    /// <summary>
    /// Receives a call from JS to .NET, locating and invoking the specified method asynchronously.
    /// </summary>
    /// <param name="jsRuntime">The <see cref="JSRuntime"/>.</param>
    /// <param name="invocationInfo">The <see cref="DotNetInvocationInfo"/>.</param>
    /// <param name="argsJson">A JSON representation of the parameters.</param>
    /// <returns>A JSON representation of the return value, or null.</returns>
    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "We expect application code is configured to ensure return types of JSInvokable methods are retained.")]
    public static void BeginInvokeDotNet(JSRuntime jsRuntime, DotNetInvocationInfo invocationInfo, [StringSyntax(StringSyntaxAttribute.Json)] string argsJson)
    {
        // This method doesn't need [JSInvokable] because the platform is responsible for having
        // some way to dispatch calls here. The logic inside here is the thing that checks whether
        // the targeted method has [JSInvokable]. It is not itself subject to that restriction,
        // because there would be nobody to police that. This method *is* the police.

        // Using ExceptionDispatchInfo here throughout because we want to always preserve
        // original stack traces.

        var callId = invocationInfo.CallId;

        object? syncResult = null;
        ExceptionDispatchInfo? syncException = null;
        IDotNetObjectReference? targetInstance = null;
        try
        {
            if (invocationInfo.DotNetObjectId != default)
            {
                targetInstance = jsRuntime.GetObjectReference(invocationInfo.DotNetObjectId);
            }

            syncResult = InvokeSynchronously(jsRuntime, invocationInfo, targetInstance, argsJson);
        }
        catch (Exception ex)
        {
            syncException = ExceptionDispatchInfo.Capture(ex);
        }

        // If there was no callId, the caller does not want to be notified about the result
        if (callId == null)
        {
            return;
        }
        else if (syncException != null)
        {
            // Threw synchronously, let's respond.
            jsRuntime.EndInvokeDotNet(invocationInfo, new DotNetInvocationResult(syncException.SourceException, "InvocationFailure"));
        }
        else if (syncResult is Task task)
        {
            // Returned a task - we need to continue that task and then report an exception
            // or return the value.
            task.ContinueWith(t => EndInvokeDotNetAfterTask(t, jsRuntime, invocationInfo), TaskScheduler.Current);

        }
        else if (syncResult is ValueTask valueTaskResult)
        {
            valueTaskResult.AsTask().ContinueWith(t => EndInvokeDotNetAfterTask(t, jsRuntime, invocationInfo), TaskScheduler.Current);
        }
        else if (syncResult?.GetType() is { IsGenericType: true } syncResultType
            && syncResultType.GetGenericTypeDefinition() == typeof(ValueTask<>))
        {
            // It's a ValueTask<T>. We'll coerce it to a Task so that we can attach a continuation.
            var innerTask = GetTaskByType(syncResultType.GenericTypeArguments[0], syncResult);

            innerTask!.ContinueWith(t => EndInvokeDotNetAfterTask(t, jsRuntime, invocationInfo), TaskScheduler.Current);
        }
        else
        {
            var syncResultJson = JsonSerializer.Serialize(syncResult, jsRuntime.JsonSerializerOptions);
            var dispatchResult = new DotNetInvocationResult(syncResultJson);
            jsRuntime.EndInvokeDotNet(invocationInfo, dispatchResult);
        }
    }

    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "We expect application code is configured to ensure return types of JSInvokable methods are retained.")]
    private static void EndInvokeDotNetAfterTask(Task task, JSRuntime jsRuntime, in DotNetInvocationInfo invocationInfo)
    {
        if (task.Exception != null)
        {
            var exceptionDispatchInfo = ExceptionDispatchInfo.Capture(task.Exception.GetBaseException());
            var dispatchResult = new DotNetInvocationResult(exceptionDispatchInfo.SourceException, "InvocationFailure");
            jsRuntime.EndInvokeDotNet(invocationInfo, dispatchResult);
        }

        var result = TaskGenericsUtil.GetTaskResult(task);
        var resultJson = JsonSerializer.Serialize(result, jsRuntime.JsonSerializerOptions);
        jsRuntime.EndInvokeDotNet(invocationInfo, new DotNetInvocationResult(resultJson));
    }

    private static object? InvokeSynchronously(JSRuntime jsRuntime, in DotNetInvocationInfo callInfo, IDotNetObjectReference? objectReference, string argsJson)
    {
        var assemblyName = callInfo.AssemblyName;
        var methodIdentifier = callInfo.MethodIdentifier;

        AssemblyKey assemblyKey;
        MethodInfo methodInfo;
        Type[] parameterTypes;
        if (objectReference is null)
        {
            assemblyKey = new AssemblyKey(assemblyName!);
            (methodInfo, parameterTypes) = GetCachedMethodInfo(assemblyKey, methodIdentifier);
        }
        else
        {
            if (assemblyName != null)
            {
                throw new ArgumentException($"For instance method calls, '{nameof(assemblyName)}' should be null. Value received: '{assemblyName}'.");
            }

            if (string.Equals(DisposeDotNetObjectReferenceMethodName, methodIdentifier, StringComparison.Ordinal))
            {
                // The client executed dotNetObjectReference.dispose(). Dispose the reference and exit.
                objectReference.Dispose();
                return default;
            }

            (methodInfo, parameterTypes) = GetCachedMethodInfo(objectReference, methodIdentifier);
        }

        var suppliedArgs = ParseArguments(jsRuntime, methodIdentifier, argsJson, parameterTypes);

        try
        {
            // objectReference will be null if this call invokes a static JSInvokable method.
            return methodInfo.Invoke(objectReference?.Value, suppliedArgs);
        }
        catch (TargetInvocationException tie) // Avoid using exception filters for AOT runtime support
        {
            if (tie.InnerException != null)
            {
                ExceptionDispatchInfo.Capture(tie.InnerException).Throw();
                throw tie.InnerException; // Unreachable
            }

            throw;
        }
        finally
        {
            // We require the invoked method to retrieve any pending byte arrays synchronously. If we didn't,
            // we wouldn't be able to have overlapping async calls. As a way to enforce this, we clear the
            // pending byte arrays synchronously after the call. This also helps because the recipient isn't
            // required to consume all the pending byte arrays, since it's legal for the JS data model to contain
            // more data than the .NET data model (like overposting)
            jsRuntime.ByteArraysToBeRevived.Clear();
        }
    }

    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "We expect application code is configured to ensure return types of JSInvokable methods are retained.")]
    internal static object?[] ParseArguments(JSRuntime jsRuntime, string methodIdentifier, string arguments, Type[] parameterTypes)
    {
        if (parameterTypes.Length == 0)
        {
            return Array.Empty<object>();
        }

        var count = Encoding.UTF8.GetByteCount(arguments);
        var buffer = ArrayPool<byte>.Shared.Rent(count);
        try
        {
            var receivedBytes = Encoding.UTF8.GetBytes(arguments, buffer);
            Debug.Assert(count == receivedBytes);

            var reader = new Utf8JsonReader(buffer.AsSpan(0, count));
            if (!reader.Read() || reader.TokenType != JsonTokenType.StartArray)
            {
                throw new JsonException("Invalid JSON");
            }

            var suppliedArgs = new object?[parameterTypes.Length];

            var index = 0;
            while (index < parameterTypes.Length && reader.Read() && reader.TokenType != JsonTokenType.EndArray)
            {
                var parameterType = parameterTypes[index];
                if (reader.TokenType == JsonTokenType.StartObject && IsIncorrectDotNetObjectRefUse(parameterType, reader))
                {
                    throw new InvalidOperationException($"In call to '{methodIdentifier}', parameter of type '{parameterType.Name}' at index {(index + 1)} must be declared as type 'DotNetObjectRef<{parameterType.Name}>' to receive the incoming value.");
                }

                suppliedArgs[index] = JsonSerializer.Deserialize(ref reader, parameterType, jsRuntime.JsonSerializerOptions);
                index++;
            }

            if (index < parameterTypes.Length)
            {
                // If we parsed fewer parameters, we can always make a definitive claim about how many parameters were received.
                throw new ArgumentException($"The call to '{methodIdentifier}' expects '{parameterTypes.Length}' parameters, but received '{index}'.");
            }

            if (!reader.Read() || reader.TokenType != JsonTokenType.EndArray)
            {
                // Either we received more parameters than we expected or the JSON is malformed.
                throw new JsonException($"Unexpected JSON token {reader.TokenType}. Ensure that the call to `{methodIdentifier}' is supplied with exactly '{parameterTypes.Length}' parameters.");
            }

            return suppliedArgs;

            // Note that the JsonReader instance is intentionally not passed by ref (or an in parameter) since we want a copy of the original reader.
            static bool IsIncorrectDotNetObjectRefUse(Type parameterType, Utf8JsonReader jsonReader)
            {
                // Check for incorrect use of DotNetObjectRef<T> at the top level. We know it's
                // an incorrect use if there's a object that looks like { '__dotNetObject': <some number> },
                // but we aren't assigning to DotNetObjectRef{T}.
                if (jsonReader.Read() &&
                    jsonReader.TokenType == JsonTokenType.PropertyName &&
                    jsonReader.ValueTextEquals(DotNetObjectRefKey.EncodedUtf8Bytes))
                {
                    // The JSON payload has the shape we expect from a DotNetObjectRef instance.
                    return !parameterType.IsGenericType || parameterType.GetGenericTypeDefinition() != typeof(DotNetObjectReference<>);
                }

                return false;
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
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
    public static void EndInvokeJS(JSRuntime jsRuntime, string arguments)
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
    /// <param name="id">Identifier for the byte array being transfered.</param>
    /// <param name="data">Byte array to be transfered from JS.</param>
    public static void ReceiveByteArray(JSRuntime jsRuntime, int id, byte[] data)
    {
        jsRuntime.ReceiveByteArray(id, data);
    }

    private static (MethodInfo, Type[]) GetCachedMethodInfo(AssemblyKey assemblyKey, string methodIdentifier)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(assemblyKey.AssemblyName);
        ArgumentException.ThrowIfNullOrWhiteSpace(methodIdentifier);

        var assemblyMethods = _cachedMethodsByAssembly.GetOrAdd(assemblyKey, ScanAssemblyForCallableMethods);
        if (assemblyMethods.TryGetValue(methodIdentifier, out var result))
        {
            return result;
        }
        else
        {
            throw new ArgumentException($"The assembly '{assemblyKey.AssemblyName}' does not contain a public invokable method with [{nameof(JSInvokableAttribute)}(\"{methodIdentifier}\")].");
        }
    }

    [UnconditionalSuppressMessage(
        "ReflectionAnalysis",
        "IL2060:MakeGenericMethod",
        Justification = "https://github.com/mono/linker/issues/1727")]
    private static Task GetTaskByType(Type type, object obj)
    {
        var converterDelegate = _cachedConvertToTaskByType.GetOrAdd(type, (Type t, MethodInfo taskConverterMethodInfo) =>
            taskConverterMethodInfo.MakeGenericMethod(t).CreateDelegate<Func<object, Task>>(), _taskConverterMethodInfo);

        return converterDelegate.Invoke(obj);
    }

    private static Task CreateValueTaskConverter<[DynamicallyAccessedMembers(LinkerFlags.JsonSerialized)] T>(object result) => ((ValueTask<T>)result).AsTask();

    private static (MethodInfo methodInfo, Type[] parameterTypes) GetCachedMethodInfo(IDotNetObjectReference objectReference, string methodIdentifier)
    {
        var type = objectReference.Value.GetType();

        // Suppressed with "pragma warning disable" in addition to WarningSuppressions.xml so ILLink Roslyn Anayzer doesn't report the warning.
#pragma warning disable IL2111 // Method with parameters or return value with `DynamicallyAccessedMembersAttribute` is accessed via reflection. Trimmer can't guarantee availability of the requirements of the method.
        var assemblyMethods = _cachedMethodsByType.GetOrAdd(type, ScanTypeForCallableMethods);
#pragma warning restore IL2111 // Method with parameters or return value with `DynamicallyAccessedMembersAttribute` is accessed via reflection. Trimmer can't guarantee availability of the requirements of the method.

        if (assemblyMethods.TryGetValue(methodIdentifier, out var result))
        {
            return result;
        }
        else
        {
            throw new ArgumentException($"The type '{type.Name}' does not contain a public invokable method with [{nameof(JSInvokableAttribute)}(\"{methodIdentifier}\")].");
        }

        static Dictionary<string, (MethodInfo, Type[])> ScanTypeForCallableMethods([DynamicallyAccessedMembers(JSInvokable)] Type type)
        {
            var result = new Dictionary<string, (MethodInfo, Type[])>(StringComparer.Ordinal);

            foreach (var method in type.GetMethods(BindingFlags.Instance | BindingFlags.Public))
            {
                if (method.ContainsGenericParameters || !method.IsDefined(typeof(JSInvokableAttribute), inherit: false))
                {
                    continue;
                }

                var identifier = method.GetCustomAttribute<JSInvokableAttribute>(false)!.Identifier ?? method.Name!;
                var parameterTypes = GetParameterTypes(method);

                if (result.ContainsKey(identifier))
                {
                    throw new InvalidOperationException($"The type {type.Name} contains more than one " +
                        $"[JSInvokable] method with identifier '{identifier}'. All [JSInvokable] methods within the same " +
                        "type must have different identifiers. You can pass a custom identifier as a parameter to " +
                        $"the [JSInvokable] attribute.");
                }

                result.Add(identifier, (method, parameterTypes));
            }

            return result;
        }
    }

    [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026", Justification = "We expect application code is configured to ensure JSInvokable methods are retained. https://github.com/dotnet/aspnetcore/issues/29946")]
    [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2072", Justification = "We expect application code is configured to ensure JSInvokable methods are retained. https://github.com/dotnet/aspnetcore/issues/29946")]
    [UnconditionalSuppressMessage("Trimming", "IL2075", Justification = "We expect application code is configured to ensure JSInvokable methods are retained. https://github.com/dotnet/aspnetcore/issues/29946")]
    private static Dictionary<string, (MethodInfo, Type[])> ScanAssemblyForCallableMethods(AssemblyKey assemblyKey)
    {
        // TODO: Consider looking first for assembly-level attributes (i.e., if there are any,
        // only use those) to avoid scanning, especially for framework assemblies.
        var result = new Dictionary<string, (MethodInfo, Type[])>(StringComparer.Ordinal);
        var exportedTypes = GetRequiredLoadedAssembly(assemblyKey).GetExportedTypes();
        foreach (var type in exportedTypes)
        {
            foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.Static))
            {
                if (method.ContainsGenericParameters || !method.IsDefined(typeof(JSInvokableAttribute), inherit: false))
                {
                    continue;
                }

                var identifier = method.GetCustomAttribute<JSInvokableAttribute>(false)!.Identifier ?? method.Name;
                var parameterTypes = GetParameterTypes(method);

                if (result.ContainsKey(identifier))
                {
                    throw new InvalidOperationException($"The assembly '{assemblyKey.AssemblyName}' contains more than one " +
                        $"[JSInvokable] method with identifier '{identifier}'. All [JSInvokable] methods within the same " +
                        $"assembly must have different identifiers. You can pass a custom identifier as a parameter to " +
                        $"the [JSInvokable] attribute.");
                }

                result.Add(identifier, (method, parameterTypes));
            }
        }

        return result;
    }

    private static Type[] GetParameterTypes(MethodInfo method)
    {
        var parameters = method.GetParameters();
        if (parameters.Length == 0)
        {
            return Type.EmptyTypes;
        }

        var parameterTypes = new Type[parameters.Length];
        for (var i = 0; i < parameters.Length; i++)
        {
            parameterTypes[i] = parameters[i].ParameterType;
        }

        return parameterTypes;
    }

    private static Assembly GetRequiredLoadedAssembly(AssemblyKey assemblyKey)
    {
        // We don't want to load assemblies on demand here, because we don't necessarily trust
        // "assemblyName" to be something the developer intended to load. So only pick from the
        // set of already-loaded assemblies.
        // In some edge cases this might force developers to explicitly call something on the
        // target assembly (from .NET) before they can invoke its allowed methods from JS.

        // Using the last to workaround https://github.com/dotnet/arcade/issues/2816.
        // In most ordinary scenarios, we wouldn't have two instances of the same Assembly in the AppDomain
        // so this doesn't change the outcome.
        Assembly? assembly = null;
        foreach (Assembly a in AppDomain.CurrentDomain.GetAssemblies())
        {
            if (new AssemblyKey(a).Equals(assemblyKey))
            {
                assembly = a;
            }
        }

        return assembly
            ?? throw new ArgumentException($"There is no loaded assembly with the name '{assemblyKey.AssemblyName}'.");
    }

    // don't point the MetadataUpdateHandlerAttribute at the DotNetDispatcher class, since the attribute has
    // DynamicallyAccessedMemberTypes.All. This causes unnecessary trim warnings on the non-MetadataUpdateHandler methods.
    internal static class MetadataUpdateHandler
    {
        public static void ClearCache(Type[]? _)
        {
            _cachedMethodsByAssembly.Clear();
            _cachedMethodsByType.Clear();
            _cachedConvertToTaskByType.Clear();
        }
    }

    private readonly struct AssemblyKey : IEquatable<AssemblyKey>
    {
        public AssemblyKey(Assembly assembly)
        {
            Assembly = assembly;
            AssemblyName = assembly.GetName().Name!;
        }

        public AssemblyKey(string assemblyName)
        {
            Assembly = null;
            AssemblyName = assemblyName;
        }

        public Assembly? Assembly { get; }

        public string AssemblyName { get; }

        public bool Equals(AssemblyKey other)
        {
            if (Assembly != null && other.Assembly != null)
            {
                return Assembly == other.Assembly;
            }

            return AssemblyName.Equals(other.AssemblyName, StringComparison.Ordinal);
        }

        public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(AssemblyName);
    }
}
