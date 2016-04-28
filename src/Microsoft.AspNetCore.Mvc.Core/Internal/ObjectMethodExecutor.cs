// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Core;
using Microsoft.Extensions.Internal;

namespace Microsoft.AspNetCore.Mvc.Internal
{
    public class ObjectMethodExecutor
    {
        private object[] _parameterDefaultValues;
        private ActionExecutorAsync _executorAsync;
        private ActionExecutor _executor;

        private static readonly MethodInfo _convertOfTMethod =
            typeof(ObjectMethodExecutor).GetRuntimeMethods().Single(methodInfo => methodInfo.Name == nameof(ObjectMethodExecutor.Convert));

        private static readonly Expression<Func<object, Task<object>>> _createTaskFromResultExpression =
            ((result) => Task.FromResult(result));

        private static readonly MethodInfo _createTaskFromResultMethod = 
            ((MethodCallExpression)_createTaskFromResultExpression.Body).Method;

        private static readonly Expression<Func<object, string, Type, Task<object>>> _coerceTaskExpression =
            ((result, methodName, declaringType) => ObjectMethodExecutor.CoerceTaskType(result, methodName, declaringType));

        private static readonly MethodInfo _coerceMethod = ((MethodCallExpression)_coerceTaskExpression.Body).Method;

        private ObjectMethodExecutor(MethodInfo methodInfo)
        {            
            if (methodInfo == null)
            {
                throw new ArgumentNullException(nameof(methodInfo));
            }
            MethodInfo = methodInfo;
            ActionParameters = methodInfo.GetParameters();
        }

        private delegate Task<object> ActionExecutorAsync(object target, object[] parameters);

        private delegate object ActionExecutor(object target, object[] parameters);

        private delegate void VoidActionExecutor(object target, object[] parameters);

        public MethodInfo MethodInfo { get; }

        public ParameterInfo[] ActionParameters { get; }

        public static ObjectMethodExecutor Create(MethodInfo methodInfo, TypeInfo targetTypeInfo)
        {
            var executor = new ObjectMethodExecutor(methodInfo);
            executor._executor = GetExecutor(methodInfo, targetTypeInfo);
            executor._executorAsync = GetExecutorAsync(methodInfo, targetTypeInfo);
            return executor;
        }

        public Task<object> ExecuteAsync(object target, object[] parameters)
        {
            return _executorAsync(target, parameters);
        }

        public object Execute(object target, object[] parameters)
        {
            return _executor(target, parameters);
        }

        public object GetDefaultValueForParameter(int index)
        {
            if (index < 0 || index > ActionParameters.Length - 1)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            EnsureParameterDefaultValues();

            return _parameterDefaultValues[index];
        }

        private static ActionExecutor GetExecutor(MethodInfo methodInfo, TypeInfo targetTypeInfo)
        {
            // Parameters to executor
            var targetParameter = Expression.Parameter(typeof(object), "target");
            var parametersParameter = Expression.Parameter(typeof(object[]), "parameters");

            // Build parameter list
            var parameters = new List<Expression>();
            var paramInfos = methodInfo.GetParameters();
            for (int i = 0; i < paramInfos.Length; i++)
            {
                var paramInfo = paramInfos[i];
                var valueObj = Expression.ArrayIndex(parametersParameter, Expression.Constant(i));
                var valueCast = Expression.Convert(valueObj, paramInfo.ParameterType);

                // valueCast is "(Ti) parameters[i]"
                parameters.Add(valueCast);
            }

            // Call method
            var instanceCast = Expression.Convert(targetParameter, targetTypeInfo.AsType());
            var methodCall = Expression.Call(instanceCast, methodInfo, parameters);

            // methodCall is "((Ttarget) target) method((T0) parameters[0], (T1) parameters[1], ...)"
            // Create function
            if (methodCall.Type == typeof(void))
            {
                var lambda = Expression.Lambda<VoidActionExecutor>(methodCall, targetParameter, parametersParameter);
                var voidExecutor = lambda.Compile();
                return WrapVoidAction(voidExecutor);
            }
            else
            {
                // must coerce methodCall to match ActionExecutor signature
                var castMethodCall = Expression.Convert(methodCall, typeof(object));
                var lambda = Expression.Lambda<ActionExecutor>(castMethodCall, targetParameter, parametersParameter);
                return lambda.Compile();
            }
        }

        private static ActionExecutor WrapVoidAction(VoidActionExecutor executor)
        {
            return delegate (object target, object[] parameters)
            {
                executor(target, parameters);
                return null;
            };
        }

        private static ActionExecutorAsync GetExecutorAsync(MethodInfo methodInfo, TypeInfo targetTypeInfo)
        {
            // Parameters to executor
            var targetParameter = Expression.Parameter(typeof(object), "target");
            var parametersParameter = Expression.Parameter(typeof(object[]), "parameters");

            // Build parameter list
            var parameters = new List<Expression>();
            var paramInfos = methodInfo.GetParameters();
            for (int i = 0; i < paramInfos.Length; i++)
            {
                var paramInfo = paramInfos[i];
                var valueObj = Expression.ArrayIndex(parametersParameter, Expression.Constant(i));
                var valueCast = Expression.Convert(valueObj, paramInfo.ParameterType);

                // valueCast is "(Ti) parameters[i]"
                parameters.Add(valueCast);
            }

            // Call method
            var instanceCast = Expression.Convert(targetParameter, targetTypeInfo.AsType());
            var methodCall = Expression.Call(instanceCast, methodInfo, parameters);

            // methodCall is "((Ttarget) target) method((T0) parameters[0], (T1) parameters[1], ...)"
            // Create function
            if (methodCall.Type == typeof(void))
            {
                var lambda = Expression.Lambda<VoidActionExecutor>(methodCall, targetParameter, parametersParameter);
                var voidExecutor = lambda.Compile();
                return WrapVoidActionAsync(voidExecutor);
            }
            else
            {
                // must coerce methodCall to match ActionExecutorAsync signature
                var coerceMethodCall = GetCoerceMethodCallExpression(methodCall, methodInfo);
                var lambda = Expression.Lambda<ActionExecutorAsync>(coerceMethodCall, targetParameter, parametersParameter);
                return lambda.Compile();
            }
        }

        // We need to CoerceResult as the object value returned from methodInfo.Invoke has to be cast to a Task<T>.
        // This is necessary to enable calling await on the returned task.
        // i.e we need to write the following var result = await (Task<ActualType>)mInfo.Invoke.
        // Returning Task<object> enables us to await on the result.
        private static Expression GetCoerceMethodCallExpression(MethodCallExpression methodCall, MethodInfo methodInfo)
        {
            var castMethodCall = Expression.Convert(methodCall, typeof(object));
            var returnType = methodCall.Type;

            if (typeof(Task).IsAssignableFrom(returnType))
            {
                if (returnType == typeof(Task))
                {
                    var stringExpression = Expression.Constant(methodInfo.Name);
                    var typeExpression = Expression.Constant(methodInfo.DeclaringType);
                    return Expression.Call(null, _coerceMethod, castMethodCall, stringExpression, typeExpression);
                }

                var taskValueType = GetTaskInnerTypeOrNull(returnType);
                if (taskValueType != null)
                {
                    // for: public Task<T> Action()
                    // constructs: return (Task<object>)Convert<T>((Task<T>)result)
                    var genericMethodInfo = _convertOfTMethod.MakeGenericMethod(taskValueType);
                    var genericMethodCall = Expression.Call(null, genericMethodInfo, castMethodCall);
                    var convertedResult = Expression.Convert(genericMethodCall, typeof(Task<object>));
                    return convertedResult;
                }

                // This will be the case for types which have derived from Task and Task<T>
                throw new InvalidOperationException(Resources.FormatActionExecutor_UnexpectedTaskInstance(
                    methodInfo.Name,
                    methodInfo.DeclaringType));
            }

            return Expression.Call(null, _createTaskFromResultMethod, castMethodCall);
        }

        private static ActionExecutorAsync WrapVoidActionAsync(VoidActionExecutor executor)
        {
            return delegate (object target, object[] parameters)
            {
                executor(target, parameters);
                return Task.FromResult<object>(null);
            };
        }

        private static Task<object> CoerceTaskType(object result, string methodName, Type declaringType)
        {
            var resultAsTask = (Task)result;
            return CastToObject(resultAsTask);
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

        private static Type GetTaskInnerTypeOrNull(Type type)
        {
            var genericType = ClosedGenericMatcher.ExtractGenericInterface(type, typeof(Task<>));

            return genericType?.GenericTypeArguments[0];
        }

        private static Task<object> Convert<T>(object taskAsObject)
        {
            var task = (Task<T>)taskAsObject;
            return CastToObject<T>(task);
        }

        private void EnsureParameterDefaultValues()
        {
            if (_parameterDefaultValues == null)
            {
                var count = ActionParameters.Length;
                _parameterDefaultValues = new object[count];

                for (var i = 0; i < count; i++)
                {
                    var parameterInfo = ActionParameters[i];
                    object defaultValue;

                    if (parameterInfo.HasDefaultValue)
                    {
                        defaultValue = parameterInfo.DefaultValue;
                    }
                    else
                    {
                        var defaultValueAttribute = parameterInfo
                            .GetCustomAttribute<DefaultValueAttribute>(inherit: false);

                        if (defaultValueAttribute?.Value == null)
                        {
                            defaultValue = parameterInfo.ParameterType.GetTypeInfo().IsValueType
                                ? Activator.CreateInstance(parameterInfo.ParameterType)
                                : null;
                        }
                        else
                        {
                            defaultValue = defaultValueAttribute.Value;
                        }
                    }

                    _parameterDefaultValues[i] = defaultValue;
                }
            }
        }
    }
}
