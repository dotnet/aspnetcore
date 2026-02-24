// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using Microsoft.Extensions.Internal;
#nullable enable

namespace Microsoft.Extensions.StackTrace.Sources;

internal sealed class StackTraceHelper
{
    [UnconditionalSuppressMessage("Trimmer", "IL2026", Justification = "MethodInfo for a stack frame might be incomplete or removed. GetFrames does the best it can to provide frame details.")]
    public static IList<StackFrameInfo> GetFrames(Exception exception, out AggregateException? error)
    {
        if (exception == null)
        {
            error = default;
            return Array.Empty<StackFrameInfo>();
        }

        var needFileInfo = true;
        var stackTrace = new System.Diagnostics.StackTrace(exception, needFileInfo);
        var stackFrames = stackTrace.GetFrames();

        if (stackFrames == null)
        {
            error = default;
            return Array.Empty<StackFrameInfo>();
        }

        var frames = new List<StackFrameInfo>(stackFrames.Length);

        List<Exception>? exceptions = null;

        for (var i = 0; i < stackFrames.Length; i++)
        {
            var frame = stackFrames[i];
            var method = frame.GetMethod();

            // MethodInfo should always be available for methods in the stack, but double check for null here.
            // Apps with trimming enabled may remove some metadata. Better to be safe than sorry.
            if (method == null)
            {
                continue;
            }

            // Always show last stackFrame
            if (!ShowInStackTrace(method) && i < stackFrames.Length - 1)
            {
                continue;
            }

            var stackFrame = new StackFrameInfo(frame.GetFileLineNumber(), frame.GetFileName(), frame, GetMethodDisplayString(method));
            frames.Add(stackFrame);
        }

        if (exceptions != null)
        {
            error = new AggregateException(exceptions);
            return frames;
        }

        error = default;
        return frames;
    }

    internal static MethodDisplayInfo? GetMethodDisplayString(MethodBase? method)
    {
        // Special case: no method available
        if (method == null)
        {
            return null;
        }

        // Type name
        var type = method.DeclaringType;

        var methodName = method.Name;

        string? subMethod = null;
        if (type != null && type.IsDefined(typeof(CompilerGeneratedAttribute)) &&
            (typeof(IAsyncStateMachine).IsAssignableFrom(type) || typeof(IEnumerator).IsAssignableFrom(type)))
        {
            // Convert StateMachine methods to correct overload +MoveNext()
            if (TryResolveStateMachineMethod(ref method, out type))
            {
                subMethod = methodName;
            }
        }

        string? declaringTypeName = null;
        // ResolveStateMachineMethod may have set declaringType to null
        if (type != null)
        {
            // Use generic type definition for consistent display on Mono where types are inflated
            var typeForDisplay = type.IsGenericType && !type.IsGenericTypeDefinition ?
                type.GetGenericTypeDefinition() : type;
            declaringTypeName = TypeNameHelper.GetTypeDisplayName(typeForDisplay, includeGenericParameterNames: true);
        }

        string? genericArguments = null;
        if (method.IsGenericMethod)
        {
            // Use generic method definition to get parameter names (T, U) instead of concrete types (string, int)
            // This is especially important on Mono where stack frame methods are inflated with concrete types
            Type[] genericArgs;
            if (method is MethodInfo methodInfo && !method.IsGenericMethodDefinition)
            {
                genericArgs = methodInfo.GetGenericMethodDefinition().GetGenericArguments();
            }
            else
            {
                genericArgs = method.GetGenericArguments();
            }
            genericArguments = "<" + string.Join(", ", genericArgs
                .Select(arg => TypeNameHelper.GetTypeDisplayName(arg, fullName: false, includeGenericParameterNames: true))) + ">";
        }

        // Method parameters
        // Use method from generic type definition for consistent parameter type display
        MethodBase methodForParams = method;
        if (method.IsGenericMethod && !method.IsGenericMethodDefinition && method is MethodInfo methodInfoForParams)
        {
            // Handle generic methods: use the generic method definition
            methodForParams = methodInfoForParams.GetGenericMethodDefinition();
        }
        else if (method.DeclaringType != null && method.DeclaringType.IsGenericType && !method.DeclaringType.IsGenericTypeDefinition)
        {
            // Handle methods on generic types: get the method from the generic type definition
            // This is especially important on Mono where methods on generic types show concrete type parameters
            methodForParams = TryGetMethodFromGenericTypeDefinition(method) ?? method;
        }
        var parameters = methodForParams.GetParameters().Select(parameter =>
        {
            var parameterType = parameter.ParameterType;

            var prefix = string.Empty;
            if (parameter.IsOut)
            {
                prefix = "out";
            }
            else if (parameterType != null && parameterType.IsByRef)
            {
                prefix = "ref";
            }

            var parameterTypeString = "?";
            if (parameterType != null)
            {
                if (parameterType.IsByRef)
                {
                    parameterType = parameterType.GetElementType();
                }

                parameterTypeString = TypeNameHelper.GetTypeDisplayName(parameterType!, fullName: false, includeGenericParameterNames: true);
            }

            return new ParameterDisplayInfo
            {
                Prefix = prefix,
                Name = parameter.Name,
                Type = parameterTypeString,
            };
        });

        var methodDisplayInfo = new MethodDisplayInfo(declaringTypeName, method.Name, genericArguments, subMethod, parameters);

        return methodDisplayInfo;
    }

    private static bool ShowInStackTrace(MethodBase method)
    {
        // Don't show any methods marked with the StackTraceHiddenAttribute
        // https://github.com/dotnet/coreclr/pull/14652
        if (HasStackTraceHiddenAttribute(method))
        {
            return false;
        }

        var type = method.DeclaringType;
        if (type == null)
        {
            return true;
        }

        if (HasStackTraceHiddenAttribute(type))
        {
            return false;
        }

        // Fallbacks for runtime pre-StackTraceHiddenAttribute
        if (type == typeof(ExceptionDispatchInfo) && method.Name == "Throw")
        {
            return false;
        }
        else if (type == typeof(TaskAwaiter) ||
            type == typeof(TaskAwaiter<>) ||
            type == typeof(ConfiguredTaskAwaitable.ConfiguredTaskAwaiter) ||
            type == typeof(ConfiguredTaskAwaitable<>.ConfiguredTaskAwaiter))
        {
            switch (method.Name)
            {
                case "HandleNonSuccessAndDebuggerNotification":
                case "ThrowForNonSuccess":
                case "ValidateEnd":
                case "GetResult":
                    return false;
            }
        }

        return true;
    }

    [UnconditionalSuppressMessage("Trimmer", "IL2075", Justification = "Unable to require a method has all information on it to resolve state machine.")]
    private static bool TryResolveStateMachineMethod(ref MethodBase method, out Type? declaringType)
    {
        Debug.Assert(method != null);
        Debug.Assert(method.DeclaringType != null);

        declaringType = method.DeclaringType;

        var parentType = declaringType.DeclaringType;
        if (parentType == null)
        {
            return false;
        }

        var methods = parentType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly);
        if (methods == null)
        {
            return false;
        }

        foreach (var candidateMethod in methods)
        {
            var attributes = candidateMethod.GetCustomAttributes<StateMachineAttribute>();
            if (attributes == null)
            {
                continue;
            }

            foreach (var asma in attributes)
            {
                if (asma.StateMachineType == declaringType)
                {
                    method = candidateMethod;
                    declaringType = candidateMethod.DeclaringType;
                    // Mark the iterator as changed; so it gets the + annotation of the original method
                    // async statemachines resolve directly to their builder methods so aren't marked as changed
                    return asma is IteratorStateMachineAttribute;
                }
            }
        }

        return false;
    }

    [UnconditionalSuppressMessage("Trimmer", "IL2075", Justification = "Method lookup on generic type definition for stack trace display. Failure is acceptable and handled gracefully.")]
    private static MethodBase? TryGetMethodFromGenericTypeDefinition(MethodBase method)
    {
        try
        {
            if (method.DeclaringType?.IsGenericType != true || method.DeclaringType.IsGenericTypeDefinition)
            {
                return null;
            }

            var genericTypeDefinition = method.DeclaringType.GetGenericTypeDefinition();
            var methodsOnGenericType = genericTypeDefinition.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);

            // Find the matching method on the generic type definition
            foreach (var candidateMethod in methodsOnGenericType)
            {
                if (candidateMethod.Name == method.Name &&
                    candidateMethod.MetadataToken == method.MetadataToken)
                {
                    return candidateMethod;
                }
            }
        }
        catch
        {
            // If we can't get the generic type definition method, return null
            // This maintains compatibility with trimming scenarios
        }

        return null;
    }

    private static bool HasStackTraceHiddenAttribute(MemberInfo memberInfo)
    {
        IList<CustomAttributeData> attributes;
        try
        {
            // Accessing MemberInfo.GetCustomAttributesData throws for some types (such as types in dynamically generated assemblies).
            // We'll skip looking up StackTraceHiddenAttributes on such types.
            attributes = memberInfo.GetCustomAttributesData();
        }
        catch
        {
            return false;
        }

        for (var i = 0; i < attributes.Count; i++)
        {
            if (attributes[i].AttributeType.Name == "StackTraceHiddenAttribute")
            {
                return true;
            }
        }

        return false;
    }
}
