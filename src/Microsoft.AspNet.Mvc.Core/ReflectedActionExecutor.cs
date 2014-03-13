// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using Microsoft.AspNet.Mvc.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;

namespace Microsoft.AspNet.Mvc
{
    public static class ReflectedActionExecutor
    {
        private static readonly MethodInfo _convertOfTMethod = typeof(ReflectedActionExecutor).GetRuntimeMethods().Single(methodInfo => methodInfo.Name == "Convert");

        // Method called via reflection.
        private static Task<object> Convert<T>(object taskAsObject)
        {
            var task = (Task<T>)taskAsObject;
            return CastToObject<T>(task);
        }

        public static async Task<object> ExecuteAsync(MethodInfo actionMethodInfo, object instance, IDictionary<string, object> actionArguments)
        {
            var orderedArguments = PrepareArguments(actionArguments, actionMethodInfo.GetParameters());
            return await ExecuteAsync(actionMethodInfo, instance, orderedArguments);
        }

        public static async Task<object> ExecuteAsync(MethodInfo actionMethodInfo, object instance, object[] orderedActionArguments)
        {
            object invocationResult = null;
            try
            {
                invocationResult = actionMethodInfo.Invoke(instance, orderedActionArguments);
            }
            catch (TargetInvocationException targetInvocationException)
            {
                // Capturing the actual exception and the original callstack and rethrow for external exception handlers to observe.
                ExceptionDispatchInfo exceptionDispatchInfo = ExceptionDispatchInfo.Capture(targetInvocationException.InnerException);
                exceptionDispatchInfo.Throw();
            }

            return await CoerceResultToTaskAsync(invocationResult, actionMethodInfo.ReturnType, actionMethodInfo.Name, actionMethodInfo.DeclaringType);
        }

        // We need to CoerceResult as the object value returned from methodInfo.Invoke has to be cast to a Task<T>.
        // This is necessary to enable calling await on the returned task.
        // i.e we need to write the following var result = await (Task<ActualType>)mInfo.Invoke.
        // Returning Task<object> enables us to await on the result.
        private static async Task<object> CoerceResultToTaskAsync(object result, Type returnType, string methodName, Type declaringType)
        {
            // If it is either a Task or Task<T>
            // must coerce the return value to Task<object>
            var resultAsTask = result as Task;
            if (resultAsTask != null)
            {
                if (returnType == typeof(Task))
                {
                    ThrowIfWrappedTaskInstance(resultAsTask.GetType(), methodName, declaringType);
                    return await CastToObject(resultAsTask);
                }

                Type taskValueType = TypeHelper.GetTaskInnerTypeOrNull(returnType);
                if (taskValueType != null)
                {
                    // for: public Task<T> Action()
                    // constructs: return (Task<object>)Convert<T>((Task<T>)result)
                    var genericMethodInfo = _convertOfTMethod.MakeGenericMethod(taskValueType);
                    var convertedResult = (Task<object>)genericMethodInfo.Invoke(null, new object[] { result });
                    return await convertedResult;
                }

                // This will be the case for:
                // 1. Types which have derived from Task and Task<T>,
                // 2. Action methods which use dynamic keyword but return a Task or Task<T>.
                throw new InvalidOperationException(Resources.FormatActionExecutor_UnexpectedTaskInstance(methodName, declaringType));
            }
            else
            {
                return result;
            }
        }

        private static object[] PrepareArguments(IDictionary<string, object> actionParameters, ParameterInfo[] declaredParameterInfos)
        {
            int count = declaredParameterInfos.Length;
            if (count == 0)
            {
                return null;
            }

            var arguments = new object[count];
            for (int index = 0; index < count; index++)
            {
                var parameterInfo = declaredParameterInfos[index];
                object value;

                if (!actionParameters.TryGetValue(parameterInfo.Name, out value))
                {
                    if (parameterInfo.HasDefaultValue)
                    {
                        value = parameterInfo.DefaultValue;
                    }
                    else
                    {
                        value = parameterInfo.ParameterType.IsValueType()
                            ? Activator.CreateInstance(parameterInfo.ParameterType)
                            : null;
                    }
                }

                arguments[index] = value;
            }

            return arguments;
        }

        private static void ThrowIfWrappedTaskInstance(Type actualTypeReturned, string methodName, Type declaringType)
        {
            // Throw if a method declares a return type of Task and returns an instance of Task<Task> or Task<Task<T>>
            // This most likely indicates that the developer forgot to call Unwrap() somewhere.
            if (actualTypeReturned != typeof(Task))
            {
                Type innerTaskType = TypeHelper.GetTaskInnerTypeOrNull(actualTypeReturned);
                if (innerTaskType != null && typeof(Task).IsAssignableFrom(innerTaskType))
                {
                    throw new InvalidOperationException(
                                    Resources.FormatActionExecutor_WrappedTaskInstance(
                                                                        methodName,
                                                                        declaringType,
                                                                        actualTypeReturned.FullName
                                                                        ));
                }
            }
        }

        /// <summary>
        /// Cast Task to Task of object
        /// </summary>
        private static async Task<object> CastToObject(Task task)
        {
            await task;
            return null;
        }

        /// <summary>
        /// Cast Task of T to Task of object
        /// </summary>
        private static async Task<object> CastToObject<T>(Task<T> task)
        {
            return (object)await task;
        }
    }
}