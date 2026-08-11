// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Text.Json;
using static Microsoft.AspNetCore.Internal.LinkerFlags;

namespace Microsoft.JSInterop.Infrastructure;

internal sealed class ReflectionJSInvokableMethodResolver : IJSInvokableMethodResolver
{
    private static readonly ConcurrentDictionary<AssemblyKey, IReadOnlyDictionary<string, JSInvokableMethodDescriptor>> _cachedMethodsByAssembly = new();
    private static readonly ConcurrentDictionary<Type, IReadOnlyDictionary<string, JSInvokableMethodDescriptor>> _cachedMethodsByType = new();
    private static readonly ConcurrentDictionary<Type, IReturnValueAdapter> _cachedReturnValueAdapters = new();

    [RequiresUnreferencedCode("JS-invokable methods are discovered through reflection.")]
    [RequiresDynamicCode("JS-invokable return values may require runtime generic adapters.")]
    internal ReflectionJSInvokableMethodResolver()
    {
    }

    [UnconditionalSuppressMessage(
        "Trimming",
        "IL2111",
        Justification = "The receiver type comes from DotNetObjectReference<T>, which preserves its public JS-invokable methods. This compatibility resolver is feature-switch guarded.")]
    public bool TryResolve(
        in JSInvokableMethodInfo methodInfo,
        [NotNullWhen(true)] out JSInvokableMethodDescriptor? descriptor)
    {
        if (methodInfo.IsStatic)
        {
            var assemblyKey = new AssemblyKey(methodInfo.AssemblyName!);
            ArgumentException.ThrowIfNullOrWhiteSpace(assemblyKey.AssemblyName);
            ArgumentException.ThrowIfNullOrWhiteSpace(methodInfo.Identifier, "methodIdentifier");
            return _cachedMethodsByAssembly
                .GetOrAdd(assemblyKey, ScanAssemblyForCallableMethods)
                .TryGetValue(methodInfo.Identifier, out descriptor);
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(methodInfo.Identifier, "methodIdentifier");

        // The type is supplied by an object reference whose generic annotation preserves these methods,
        // but the analyzer cannot connect that annotation to this runtime lookup key.
        return _cachedMethodsByType
            .GetOrAdd(methodInfo.TargetType!, ScanTypeForCallableMethods)
            .TryGetValue(methodInfo.Identifier, out descriptor);
    }

    internal static object?[] ParseArguments(
        JsonSerializerOptions options,
        string methodIdentifier,
        string arguments,
        Type[] parameterTypes)
    {
        using var document = JsonDocument.Parse(arguments);
        var argumentsElement = document.RootElement;
        if (argumentsElement.ValueKind != JsonValueKind.Array)
        {
            throw new JsonException("Invalid JSON");
        }

        var actualCount = argumentsElement.GetArrayLength();
        if (actualCount < parameterTypes.Length)
        {
            throw new ArgumentException($"The call to '{methodIdentifier}' expects '{parameterTypes.Length}' parameters, but received '{actualCount}'.");
        }

        if (actualCount > parameterTypes.Length)
        {
            throw new JsonException($"Unexpected JSON token {GetTokenName(argumentsElement[parameterTypes.Length])}. Ensure that the call to `{methodIdentifier}' is supplied with exactly '{parameterTypes.Length}' parameters.");
        }

        var suppliedArgs = parameterTypes.Length == 0
            ? Array.Empty<object?>()
            : new object?[parameterTypes.Length];
        for (var index = 0; index < parameterTypes.Length; index++)
        {
            var parameterType = parameterTypes[index];
            var argument = argumentsElement[index];
            if (argument.ValueKind == JsonValueKind.Object && IsIncorrectDotNetObjectRefUse(parameterType, argument))
            {
                throw new InvalidOperationException($"In call to '{methodIdentifier}', parameter of type '{parameterType.Name}' at index {(index + 1)} must be declared as type 'DotNetObjectRef<{parameterType.Name}>' to receive the incoming value.");
            }

            suppliedArgs[index] = JsonSerializer.Deserialize(argument, options.GetTypeInfo(parameterType));
        }

        return suppliedArgs;

        static bool IsIncorrectDotNetObjectRefUse(Type parameterType, JsonElement argument)
        {
            var properties = argument.EnumerateObject();
            return properties.MoveNext() &&
                properties.Current.NameEquals(DotNetDispatcher.DotNetObjectRefKey.EncodedUtf8Bytes) &&
                (!parameterType.IsGenericType ||
                    parameterType.GetGenericTypeDefinition() != typeof(DotNetObjectReference<>));
        }

        static JsonTokenType GetTokenName(JsonElement argument)
            => argument.ValueKind switch
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
    }

    private static JSInvokableMethodDescriptor CreateDescriptor(MethodInfo method, string identifier)
    {
        var parameterTypes = GetParameterTypes(method);
        return new JSInvokableMethodDescriptor
        {
            AssemblyName = method.DeclaringType!.Assembly.GetName().Name!,
            TargetType = method.DeclaringType,
            Identifier = identifier,
            IsStatic = method.IsStatic,
            Invoke = (target, argsJson, options) =>
                Invoke(method, identifier, parameterTypes, target, argsJson, options),
        };
    }

    private static ValueTask<string?> Invoke(
        MethodInfo method,
        string identifier,
        Type[] parameterTypes,
        object? target,
        string argsJson,
        JsonSerializerOptions options)
    {
        var suppliedArgs = ParseArguments(options, identifier, argsJson, parameterTypes);
        object? result;
        try
        {
            result = method.Invoke(target, suppliedArgs);
        }
        catch (TargetInvocationException exception)
        {
            if (exception.InnerException is not null)
            {
                ExceptionDispatchInfo.Capture(exception.InnerException).Throw();
            }

            throw;
        }

        if (result is null)
        {
            return default;
        }

        var returnValueAdapter = _cachedReturnValueAdapters.GetOrAdd(result.GetType(), CreateReturnValueAdapter);
        return returnValueAdapter.GetResult(result, options);
    }

    private static IReturnValueAdapter CreateReturnValueAdapter(Type returnType)
    {
        if (typeof(Task).IsAssignableFrom(returnType))
        {
            var resultType = GetTaskResultType(returnType);
            return resultType is null
                ? TaskReturnValueAdapter.Instance
                : CreateGenericAdapter(typeof(TaskReturnValueAdapter<>), resultType);
        }

        if (returnType == typeof(ValueTask))
        {
            return ValueTaskReturnValueAdapter.Instance;
        }

        if (returnType.IsGenericType)
        {
            var genericTypeDefinition = returnType.GetGenericTypeDefinition();
            if (genericTypeDefinition == typeof(ValueTask<>))
            {
                return CreateGenericAdapter(typeof(ValueTaskReturnValueAdapter<>), returnType.GenericTypeArguments[0]);
            }
        }

        return new SerializedReturnValueAdapter(returnType);
    }

    private static Type? GetTaskResultType(Type taskType)
    {
        while (taskType != typeof(Task) &&
            (!taskType.IsGenericType || taskType.GetGenericTypeDefinition() != typeof(Task<>)))
        {
            taskType = taskType.BaseType
                ?? throw new ArgumentException($"The type '{taskType.FullName}' is not inherited from '{typeof(Task).FullName}'.");
        }

        return taskType.IsGenericType ? taskType.GenericTypeArguments[0] : null;
    }

    private static string SerializeRuntimeResult(object? result, JsonSerializerOptions options)
    {
        if (result is null)
        {
            return "null";
        }

        return JsonSerializer.Serialize(result, options.GetTypeInfo(result.GetType()));
    }

    [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "Only used when reflection JS-invokable resolution is enabled.")]
    [UnconditionalSuppressMessage("Trimming", "IL2055", Justification = "Only used when reflection JS-invokable resolution is enabled.")]
    private static IReturnValueAdapter CreateGenericAdapter(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] Type adapterType,
        Type resultType)
        => (IReturnValueAdapter)Activator.CreateInstance(adapterType.MakeGenericType(resultType))!;

    [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026", Justification = "Application code is configured to retain JSInvokable methods.")]
    [UnconditionalSuppressMessage("Trimming", "IL2065", Justification = "Application assemblies are configured to retain public static JS-invokable methods.")]
    [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2072", Justification = "Application code is configured to retain JSInvokable methods.")]
    [UnconditionalSuppressMessage("Trimming", "IL2075", Justification = "Application code is configured to retain JSInvokable methods.")]
    private static Dictionary<string, JSInvokableMethodDescriptor> ScanAssemblyForCallableMethods(AssemblyKey assemblyKey)
    {
        var result = new Dictionary<string, JSInvokableMethodDescriptor>(StringComparer.Ordinal);
        foreach (var type in GetRequiredLoadedAssembly(assemblyKey).GetExportedTypes())
        {
            foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.Static))
            {
                AddInvokableMethods(result, method, $"assembly '{assemblyKey.AssemblyName}'", "assembly");
            }
        }

        return result;
    }

    private static Dictionary<string, JSInvokableMethodDescriptor> ScanTypeForCallableMethods(
        [DynamicallyAccessedMembers(JSInvokable)] Type type)
    {
        var result = new Dictionary<string, JSInvokableMethodDescriptor>(StringComparer.Ordinal);
        foreach (var method in type.GetMethods(BindingFlags.Instance | BindingFlags.Public))
        {
            AddInvokableMethods(result, method, $"type {type.Name}", "type");
        }

        return result;
    }

    private static void AddInvokableMethods(
        Dictionary<string, JSInvokableMethodDescriptor> result,
        MethodInfo method,
        string owner,
        string ownerKind)
    {
        if (method.ContainsGenericParameters || !method.IsDefined(typeof(JSInvokableAttribute), inherit: false))
        {
            return;
        }

        foreach (var attribute in method.GetCustomAttributes<JSInvokableAttribute>(false))
        {
            var identifier = attribute.Identifier ?? method.Name;
            if (!result.TryAdd(identifier, CreateDescriptor(method, identifier)))
            {
                throw new InvalidOperationException($"The {owner} contains more than one " +
                    $"[{nameof(JSInvokableAttribute)}] method with identifier '{identifier}'. All [{nameof(JSInvokableAttribute)}] methods within the same " +
                    $"{ownerKind} must have different identifiers. You can pass a custom identifier as a parameter to " +
                    $"the [{nameof(JSInvokableAttribute)}] attribute.");
            }
        }
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
        Assembly? assembly = null;
        foreach (var candidate in AppDomain.CurrentDomain.GetAssemblies())
        {
            if (new AssemblyKey(candidate).Equals(assemblyKey))
            {
                assembly = candidate;
            }
        }

        return assembly
            ?? throw new ArgumentException($"There is no loaded assembly with the name '{assemblyKey.AssemblyName}'.");
    }

    internal static void ClearCache()
    {
        _cachedMethodsByAssembly.Clear();
        _cachedMethodsByType.Clear();
        _cachedReturnValueAdapters.Clear();
    }

    internal static class MetadataUpdateHandler
    {
        public static void ClearCache(Type[]? _) => ReflectionJSInvokableMethodResolver.ClearCache();
    }

    private interface IReturnValueAdapter
    {
        ValueTask<string?> GetResult(object? result, JsonSerializerOptions options);
    }

    private sealed class SerializedReturnValueAdapter(Type returnType) : IReturnValueAdapter
    {
        public ValueTask<string?> GetResult(object? result, JsonSerializerOptions options)
            => new(JsonSerializer.Serialize(result, options.GetTypeInfo(returnType)));
    }

    private sealed class TaskReturnValueAdapter : IReturnValueAdapter
    {
        public static TaskReturnValueAdapter Instance { get; } = new();

        public async ValueTask<string?> GetResult(object? result, JsonSerializerOptions options)
        {
            await ((Task)result!).ConfigureAwait(false);
            return null;
        }
    }

    private sealed class TaskReturnValueAdapter<T> : IReturnValueAdapter
    {
        public async ValueTask<string?> GetResult(object? result, JsonSerializerOptions options)
        {
            var value = await ((Task<T>)result!).ConfigureAwait(false);
            return SerializeRuntimeResult(value, options);
        }
    }

    private sealed class ValueTaskReturnValueAdapter : IReturnValueAdapter
    {
        public static ValueTaskReturnValueAdapter Instance { get; } = new();

        public async ValueTask<string?> GetResult(object? result, JsonSerializerOptions options)
        {
            await ((ValueTask)result!).ConfigureAwait(false);
            return null;
        }
    }

    private sealed class ValueTaskReturnValueAdapter<T> : IReturnValueAdapter
    {
        public async ValueTask<string?> GetResult(object? result, JsonSerializerOptions options)
        {
            var value = await ((ValueTask<T>)result!).ConfigureAwait(false);
            return SerializeRuntimeResult(value, options);
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
            if (Assembly is not null && other.Assembly is not null)
            {
                return Assembly == other.Assembly;
            }

            return AssemblyName.Equals(other.AssemblyName, StringComparison.Ordinal);
        }

        public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(AssemblyName);
    }
}
