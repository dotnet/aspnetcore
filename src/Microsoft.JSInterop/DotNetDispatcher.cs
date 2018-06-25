// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Microsoft.JSInterop
{
    /// <summary>
    /// Provides methods that receive incoming calls from JS to .NET.
    /// </summary>
    public static class DotNetDispatcher
    {
        private static ConcurrentDictionary<string, IReadOnlyDictionary<string, (MethodInfo, Type[])>> _cachedMethodsByAssembly
            = new ConcurrentDictionary<string, IReadOnlyDictionary<string, (MethodInfo, Type[])>>();

        /// <summary>
        /// Receives a call from JS to .NET, locating and invoking the specified method.
        /// </summary>
        /// <param name="assemblyName">The assembly containing the method to be invoked.</param>
        /// <param name="methodIdentifier">The identifier of the method to be invoked. The method must be annotated with a <see cref="JSInvokableAttribute"/> matching this identifier string.</param>
        /// <param name="argsJson">A JSON representation of the parameters.</param>
        /// <returns>A JSON representation of the return value, or null.</returns>
        public static string Invoke(string assemblyName, string methodIdentifier, string argsJson)
        {
            // This method doesn't need [JSInvokable] because the platform is responsible for having
            // some way to dispatch calls here. The logic inside here is the thing that checks whether
            // the targeted method has [JSInvokable]. It is not itself subject to that restriction,
            // because there would be nobody to police that. This method *is* the police.

            var syncResult = InvokeSynchronously(assemblyName, methodIdentifier, argsJson);
            return syncResult == null ? null : Json.Serialize(syncResult);
        }

        /// <summary>
        /// Receives a call from JS to .NET, locating and invoking the specified method asynchronously.
        /// </summary>
        /// <param name="callId">A value identifying the asynchronous call that should be passed back with the result, or null if no result notification is required.</param>
        /// <param name="assemblyName">The assembly containing the method to be invoked.</param>
        /// <param name="methodIdentifier">The identifier of the method to be invoked. The method must be annotated with a <see cref="JSInvokableAttribute"/> matching this identifier string.</param>
        /// <param name="argsJson">A JSON representation of the parameters.</param>
        /// <returns>A JSON representation of the return value, or null.</returns>
        public static void BeginInvoke(string callId, string assemblyName, string methodIdentifier, string argsJson)
        {
            // This method doesn't need [JSInvokable] because the platform is responsible for having
            // some way to dispatch calls here. The logic inside here is the thing that checks whether
            // the targeted method has [JSInvokable]. It is not itself subject to that restriction,
            // because there would be nobody to police that. This method *is* the police.

            var syncResult = InvokeSynchronously(assemblyName, methodIdentifier, argsJson);

            // If there was no callId, the caller does not want to be notified about the result
            if (callId != null)
            {
                // Invoke and coerce the result to a Task so the caller can use the same async API
                // for both synchronous and asynchronous methods
                var task = syncResult is Task syncResultTask ? syncResultTask : Task.FromResult(syncResult);
                task.ContinueWith(completedTask =>
                {
                    // DotNetDispatcher only works with JSRuntimeBase instances.
                    // If the developer wants to use a totally custom IJSRuntime, then their JS-side
                    // code has to implement its own way of returning async results.
                    var jsRuntimeBaseInstance = (JSRuntimeBase)JSRuntime.Current;

                    try
                    {
                        var result = TaskGenericsUtil.GetTaskResult(completedTask);
                        jsRuntimeBaseInstance.EndInvokeDotNet(callId, true, result);
                    }
                    catch (Exception ex)
                    {
                        ex = UnwrapException(ex);
                        jsRuntimeBaseInstance.EndInvokeDotNet(callId, false, ex);
                    }
                });
            }
        }

        private static object InvokeSynchronously(string assemblyName, string methodIdentifier, string argsJson)
        {
            var (methodInfo, parameterTypes) = GetCachedMethodInfo(assemblyName, methodIdentifier);

            // There's no direct way to say we want to deserialize as an array with heterogenous
            // entry types (e.g., [string, int, bool]), so we need to deserialize in two phases.
            // First we deserialize as object[], for which SimpleJson will supply JsonObject
            // instances for nonprimitive values.
            var suppliedArgs = (object[])null;
            var suppliedArgsLength = 0;
            if (argsJson != null)
            {
                suppliedArgs = Json.Deserialize<SimpleJson.JsonArray>(argsJson).ToArray<object>();
                suppliedArgsLength = suppliedArgs.Length;
            }
            if (suppliedArgsLength != parameterTypes.Length)
            {
                throw new ArgumentException($"In call to '{methodIdentifier}', expected {parameterTypes.Length} parameters but received {suppliedArgsLength}.");
            }

            // Second, convert each supplied value to the type expected by the method
            var serializerStrategy = SimpleJson.SimpleJson.CurrentJsonSerializerStrategy;
            for (var i = 0; i < suppliedArgsLength; i++)
            {
                suppliedArgs[i] = serializerStrategy.DeserializeObject(
                    suppliedArgs[i], parameterTypes[i]);
            }

            try
            {
                return methodInfo.Invoke(null, suppliedArgs);
            }
            catch (Exception ex)
            {
                throw UnwrapException(ex);
            }
        }

        /// <summary>
        /// Receives notification that a call from .NET to JS has finished, marking the
        /// associated <see cref="Task"/> as completed.
        /// </summary>
        /// <param name="asyncHandle">The identifier for the function invocation.</param>
        /// <param name="succeeded">A flag to indicate whether the invocation succeeded.</param>
        /// <param name="resultOrException">If <paramref name="succeeded"/> is <c>true</c>, specifies the invocation result. If <paramref name="succeeded"/> is <c>false</c>, gives the <see cref="Exception"/> corresponding to the invocation failure.</param>
        [JSInvokable(nameof(DotNetDispatcher) + "." + nameof(EndInvoke))]
        public static void EndInvoke(long asyncHandle, bool succeeded, object resultOrException)
            => ((JSRuntimeBase)JSRuntime.Current).EndInvokeJS(asyncHandle, succeeded, resultOrException);

        private static (MethodInfo, Type[]) GetCachedMethodInfo(string assemblyName, string methodIdentifier)
        {
            if (string.IsNullOrWhiteSpace(assemblyName))
            {
                throw new ArgumentException("Cannot be null, empty, or whitespace.", nameof(assemblyName));
            }

            if (string.IsNullOrWhiteSpace(methodIdentifier))
            {
                throw new ArgumentException("Cannot be null, empty, or whitespace.", nameof(methodIdentifier));
            }

            var assemblyMethods = _cachedMethodsByAssembly.GetOrAdd(assemblyName, ScanAssemblyForCallableMethods);
            if (assemblyMethods.TryGetValue(methodIdentifier, out var result))
            {
                return result;
            }
            else
            {
                throw new ArgumentException($"The assembly '{assemblyName}' does not contain a public method with [{nameof(JSInvokableAttribute)}(\"{methodIdentifier}\")].");
            }
        }

        private static IReadOnlyDictionary<string, (MethodInfo, Type[])> ScanAssemblyForCallableMethods(string assemblyName)
        {
            // TODO: Consider looking first for assembly-level attributes (i.e., if there are any,
            // only use those) to avoid scanning, especially for framework assemblies.
            return GetRequiredLoadedAssembly(assemblyName)
                .GetExportedTypes()
                .SelectMany(type => type.GetMethods())
                .Where(method => method.IsDefined(typeof(JSInvokableAttribute), inherit: false))
                .ToDictionary(
                    method => method.GetCustomAttribute<JSInvokableAttribute>(false).Identifier,
                    method => (method, method.GetParameters().Select(p => p.ParameterType).ToArray())
                );
        }

        private static Assembly GetRequiredLoadedAssembly(string assemblyName)
        {
            // We don't want to load assemblies on demand here, because we don't necessarily trust
            // "assemblyName" to be something the developer intended to load. So only pick from the
            // set of already-loaded assemblies.
            // In some edge cases this might force developers to explicitly call something on the
            // target assembly (from .NET) before they can invoke its allowed methods from JS.
            var loadedAssemblies = AppDomain.CurrentDomain.GetAssemblies();
            return loadedAssemblies.FirstOrDefault(a => a.GetName().Name.Equals(assemblyName, StringComparison.Ordinal))
                ?? throw new ArgumentException($"There is no loaded assembly with the name '{assemblyName}'.");
        }

        private static Exception UnwrapException(Exception ex)
        {
            while ((ex is AggregateException || ex is TargetInvocationException) && ex.InnerException != null)
            {
                ex = ex.InnerException;
            }

            return ex;
        }
    }
}
