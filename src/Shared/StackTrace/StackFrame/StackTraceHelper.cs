// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using Microsoft.Extensions.Internal;

namespace Microsoft.Extensions.StackTrace.Sources
{
    internal class StackTraceHelper
    {
        public static IList<StackFrameInfo> GetFrames(Exception exception, out AggregateException error)
        {
            var frames = new List<StackFrameInfo>();

            if (exception == null)
            {
                error = default;
                return frames;
            }

            using (var portablePdbReader = new PortablePdbReader())
            {
                var needFileInfo = true;
                var stackTrace = new System.Diagnostics.StackTrace(exception, needFileInfo);
                var stackFrames = stackTrace.GetFrames();

                if (stackFrames == null)
                {
                    error = default;
                    return frames;
                }

                List<Exception> exceptions = null;

                for (var i = 0; i < stackFrames.Length; i++)
                {
                    var frame = stackFrames[i];
                    var method = frame.GetMethod();

                    // Always show last stackFrame
                    if (!ShowInStackTrace(method) && i < stackFrames.Length - 1)
                    {
                        continue;
                    }

                    var stackFrame = new StackFrameInfo
                    {
                        StackFrame = frame,
                        FilePath = frame.GetFileName(),
                        LineNumber = frame.GetFileLineNumber(),
                        MethodDisplayInfo = GetMethodDisplayString(frame.GetMethod()),
                    };

                    if (string.IsNullOrEmpty(stackFrame.FilePath))
                    {
                        try
                        {
                            // .NET Framework and older versions of mono don't support portable PDBs
                            // so we read it manually to get file name and line information
                            portablePdbReader.PopulateStackFrame(stackFrame, method, frame.GetILOffset());
                        }
                        catch (Exception ex)
                        {
                            if (exceptions is null)
                            {
                                exceptions = new List<Exception>();
                            }

                            exceptions.Add(ex);
                        }
                    }

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
        }

        internal static MethodDisplayInfo GetMethodDisplayString(MethodBase method)
        {
            // Special case: no method available
            if (method == null)
            {
                return null;
            }

            var methodDisplayInfo = new MethodDisplayInfo();

            // Type name
            var type = method.DeclaringType;

            var methodName = method.Name;

            if (type != null && type.IsDefined(typeof(CompilerGeneratedAttribute)) &&
                (typeof(IAsyncStateMachine).IsAssignableFrom(type) || typeof(IEnumerator).IsAssignableFrom(type)))
            {
                // Convert StateMachine methods to correct overload +MoveNext()
                if (TryResolveStateMachineMethod(ref method, out type))
                {
                    methodDisplayInfo.SubMethod = methodName;
                }
            }
            // ResolveStateMachineMethod may have set declaringType to null
            if (type != null)
            {
                methodDisplayInfo.DeclaringTypeName = TypeNameHelper.GetTypeDisplayName(type, includeGenericParameterNames: true);
            }

            // Method name
            methodDisplayInfo.Name = method.Name;
            if (method.IsGenericMethod)
            {
                var genericArguments = string.Join(", ", method.GetGenericArguments()
                    .Select(arg => TypeNameHelper.GetTypeDisplayName(arg, fullName: false, includeGenericParameterNames: true)));
                methodDisplayInfo.GenericArguments += "<" + genericArguments + ">";
            }

            // Method parameters
            methodDisplayInfo.Parameters = method.GetParameters().Select(parameter =>
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

                    parameterTypeString = TypeNameHelper.GetTypeDisplayName(parameterType, fullName: false, includeGenericParameterNames: true);
                }

                return new ParameterDisplayInfo
                {
                    Prefix = prefix,
                    Name = parameter.Name,
                    Type = parameterTypeString,
                };
            });

            return methodDisplayInfo;
        }

        private static bool ShowInStackTrace(MethodBase method)
        {
            Debug.Assert(method != null);

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

        private static bool TryResolveStateMachineMethod(ref MethodBase method, out Type declaringType)
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

        private static bool HasStackTraceHiddenAttribute(MemberInfo memberInfo)
        {
            IList<CustomAttributeData> attributes;
            try
            {
                // Accessing MembmerInfo.GetCustomAttributesData throws for some types (such as types in dynamically generated assemblies).
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
}
