// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Microsoft.JSInterop.Infrastructure
{
    /// <summary>
    /// Provides methods that receive incoming calls from JS to .NET.
    /// </summary>
    public static class DotNetDispatcher
    {
        private const string DisposeDotNetObjectReferenceMethodName = "__Dispose";
        internal static readonly JsonEncodedText DotNetObjectRefKey = JsonEncodedText.Encode("__dotNetObject");

        private static readonly ConcurrentDictionary<AssemblyKey, IReadOnlyDictionary<string, (MethodInfo, Type[])>> _cachedMethodsByAssembly
            = new ConcurrentDictionary<AssemblyKey, IReadOnlyDictionary<string, (MethodInfo, Type[])>>();

        private static readonly ConcurrentDictionary<Type, IReadOnlyDictionary<string, (MethodInfo, Type[])>> _cachedMethodsByType
            = new ConcurrentDictionary<Type, IReadOnlyDictionary<string, (MethodInfo, Type[])>>();

        /// <summary>
        /// Receives a call from JS to .NET, locating and invoking the specified method.
        /// </summary>
        /// <param name="jsRuntime">The <see cref="JSRuntime"/>.</param>
        /// <param name="invocationInfo">The <see cref="DotNetInvocationInfo"/>.</param>
        /// <param name="argsJson">A JSON representation of the parameters.</param>
        /// <returns>A JSON representation of the return value, or null.</returns>
        public static string Invoke(JSRuntime jsRuntime, in DotNetInvocationInfo invocationInfo, string argsJson)
        {
            // This method doesn't need [JSInvokable] because the platform is responsible for having
            // some way to dispatch calls here. The logic inside here is the thing that checks whether
            // the targeted method has [JSInvokable]. It is not itself subject to that restriction,
            // because there would be nobody to police that. This method *is* the police.

            IDotNetObjectReference targetInstance = default;
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
        public static void BeginInvokeDotNet(JSRuntime jsRuntime, DotNetInvocationInfo invocationInfo, string argsJson)
        {
            // This method doesn't need [JSInvokable] because the platform is responsible for having
            // some way to dispatch calls here. The logic inside here is the thing that checks whether
            // the targeted method has [JSInvokable]. It is not itself subject to that restriction,
            // because there would be nobody to police that. This method *is* the police.

            // Using ExceptionDispatchInfo here throughout because we want to always preserve
            // original stack traces.

            var callId = invocationInfo.CallId;

            object syncResult = null;
            ExceptionDispatchInfo syncException = null;
            IDotNetObjectReference targetInstance = null;
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
                task.ContinueWith(t =>
                {
                    if (t.Exception != null)
                    {
                        var exceptionDispatchInfo = ExceptionDispatchInfo.Capture(t.Exception.GetBaseException());
                        var dispatchResult = new DotNetInvocationResult(exceptionDispatchInfo.SourceException, "InvocationFailure");
                        jsRuntime.EndInvokeDotNet(invocationInfo, dispatchResult);
                    }

                    var result = TaskGenericsUtil.GetTaskResult(task);
                    jsRuntime.EndInvokeDotNet(invocationInfo, new DotNetInvocationResult(result));
                }, TaskScheduler.Current);
            }
            else
            {
                var dispatchResult = new DotNetInvocationResult(syncResult);
                jsRuntime.EndInvokeDotNet(invocationInfo, dispatchResult);
            }
        }

        private static object InvokeSynchronously(JSRuntime jsRuntime, in DotNetInvocationInfo callInfo, IDotNetObjectReference objectReference, string argsJson)
        {
            var assemblyName = callInfo.AssemblyName;
            var methodIdentifier = callInfo.MethodIdentifier;

            AssemblyKey assemblyKey;
            MethodInfo methodInfo;
            Type[] parameterTypes;
            if (objectReference is null)
            {
                assemblyKey = new AssemblyKey(assemblyName);
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
                    throw null; // unreached
                }

                throw;
            }
        }

        internal static object[] ParseArguments(JSRuntime jsRuntime, string methodIdentifier, string arguments, Type[] parameterTypes)
        {
            if (parameterTypes.Length == 0)
            {
                return Array.Empty<object>();
            }

            var utf8JsonBytes = Encoding.UTF8.GetBytes(arguments);
            var reader = new Utf8JsonReader(utf8JsonBytes);
            if (!reader.Read() || reader.TokenType != JsonTokenType.StartArray)
            {
                throw new JsonException("Invalid JSON");
            }

            var suppliedArgs = new object[parameterTypes.Length];

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
            jsRuntime.EndInvokeJS(taskId, success, ref reader);

            if (!reader.Read() || reader.TokenType != JsonTokenType.EndArray)
            {
                throw new JsonException("Invalid JSON");
            }
        }

        private static (MethodInfo, Type[]) GetCachedMethodInfo(AssemblyKey assemblyKey, string methodIdentifier)
        {
            if (string.IsNullOrWhiteSpace(assemblyKey.AssemblyName))
            {
                throw new ArgumentException("Cannot be null, empty, or whitespace.", nameof(assemblyKey.AssemblyName));
            }

            if (string.IsNullOrWhiteSpace(methodIdentifier))
            {
                throw new ArgumentException("Cannot be null, empty, or whitespace.", nameof(methodIdentifier));
            }

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

        private static (MethodInfo methodInfo, Type[] parameterTypes) GetCachedMethodInfo(IDotNetObjectReference objectReference, string methodIdentifier)
        {
            var type = objectReference.Value.GetType();
            var assemblyMethods = _cachedMethodsByType.GetOrAdd(type, ScanTypeForCallableMethods);
            if (assemblyMethods.TryGetValue(methodIdentifier, out var result))
            {
                return result;
            }
            else
            {
                throw new ArgumentException($"The type '{type.Name}' does not contain a public invokable method with [{nameof(JSInvokableAttribute)}(\"{methodIdentifier}\")].");
            }

            static Dictionary<string, (MethodInfo, Type[])> ScanTypeForCallableMethods(Type type)
            {
                var result = new Dictionary<string, (MethodInfo, Type[])>(StringComparer.Ordinal);
                var invokableMethods = type
                    .GetMethods(BindingFlags.Public | BindingFlags.Instance)
                    .Where(method => !method.ContainsGenericParameters && method.IsDefined(typeof(JSInvokableAttribute), inherit: false));

                foreach (var method in invokableMethods)
                {
                    var identifier = method.GetCustomAttribute<JSInvokableAttribute>(false).Identifier ?? method.Name;
                    var parameterTypes = method.GetParameters().Select(p => p.ParameterType).ToArray();

                    if (result.ContainsKey(identifier))
                    {
                        throw new InvalidOperationException($"The type {type.Name} contains more than one " +
                            $"[JSInvokable] method with identifier '{identifier}'. All [JSInvokable] methods within the same " +
                            $"type must have different identifiers. You can pass a custom identifier as a parameter to " +
                            $"the [JSInvokable] attribute.");
                    }

                    result.Add(identifier, (method, parameterTypes));
                }

                return result;
            }
        }

        private static Dictionary<string, (MethodInfo, Type[])> ScanAssemblyForCallableMethods(AssemblyKey assemblyKey)
        {
            // TODO: Consider looking first for assembly-level attributes (i.e., if there are any,
            // only use those) to avoid scanning, especially for framework assemblies.
            var result = new Dictionary<string, (MethodInfo, Type[])>(StringComparer.Ordinal);
            var invokableMethods = GetRequiredLoadedAssembly(assemblyKey)
                .GetExportedTypes()
                .SelectMany(type => type.GetMethods(BindingFlags.Public | BindingFlags.Static))
                .Where(method => !method.ContainsGenericParameters && method.IsDefined(typeof(JSInvokableAttribute), inherit: false));
            foreach (var method in invokableMethods)
            {
                var identifier = method.GetCustomAttribute<JSInvokableAttribute>(false).Identifier ?? method.Name;
                var parameterTypes = method.GetParameters().Select(p => p.ParameterType).ToArray();

                if (result.ContainsKey(identifier))
                {
                    throw new InvalidOperationException($"The assembly '{assemblyKey.AssemblyName}' contains more than one " +
                        $"[JSInvokable] method with identifier '{identifier}'. All [JSInvokable] methods within the same " +
                        $"assembly must have different identifiers. You can pass a custom identifier as a parameter to " +
                        $"the [JSInvokable] attribute.");
                }

                result.Add(identifier, (method, parameterTypes));
            }

            return result;
        }

        private static Assembly GetRequiredLoadedAssembly(AssemblyKey assemblyKey)
        {
            // We don't want to load assemblies on demand here, because we don't necessarily trust
            // "assemblyName" to be something the developer intended to load. So only pick from the
            // set of already-loaded assemblies.
            // In some edge cases this might force developers to explicitly call something on the
            // target assembly (from .NET) before they can invoke its allowed methods from JS.
            var loadedAssemblies = AppDomain.CurrentDomain.GetAssemblies();

            // Using LastOrDefault to workaround for https://github.com/dotnet/arcade/issues/2816.
            // In most ordinary scenarios, we wouldn't have two instances of the same Assembly in the AppDomain
            // so this doesn't change the outcome.
            var assembly = loadedAssemblies.LastOrDefault(a => new AssemblyKey(a).Equals(assemblyKey));

            return assembly
                ?? throw new ArgumentException($"There is no loaded assembly with the name '{assemblyKey.AssemblyName}'.");
        }

        private readonly struct AssemblyKey : IEquatable<AssemblyKey>
        {
            public AssemblyKey(Assembly assembly)
            {
                Assembly = assembly;
                AssemblyName = assembly.GetName().Name;
            }

            public AssemblyKey(string assemblyName)
            {
                Assembly = null;
                AssemblyName = assemblyName;
            }

            public Assembly Assembly { get; }

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
}
