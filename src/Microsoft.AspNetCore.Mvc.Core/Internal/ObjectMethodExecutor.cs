// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.Internal;

namespace Microsoft.AspNetCore.Mvc.Internal
{
    public class ObjectMethodExecutor
    {
        private readonly object[] _parameterDefaultValues;
        private readonly ActionExecutorAsync _executorAsync;
        private readonly ActionExecutor _executor;

        private static readonly MethodInfo _convertOfTMethod =
            typeof(ObjectMethodExecutor).GetRuntimeMethods().Single(methodInfo => methodInfo.Name == nameof(ObjectMethodExecutor.Convert));

        private ObjectMethodExecutor(MethodInfo methodInfo, TypeInfo targetTypeInfo)
        {
            if (methodInfo == null)
            {
                throw new ArgumentNullException(nameof(methodInfo));
            }

            MethodInfo = methodInfo;
            TargetTypeInfo = targetTypeInfo;
            ActionParameters = methodInfo.GetParameters();
            MethodReturnType = methodInfo.ReturnType;
            IsMethodAsync = typeof(Task).IsAssignableFrom(MethodReturnType);
            TaskGenericType = IsMethodAsync ? GetTaskInnerTypeOrNull(MethodReturnType) : null;
            IsTypeAssignableFromIActionResult = typeof(IActionResult).IsAssignableFrom(TaskGenericType ?? MethodReturnType);

            if (IsMethodAsync && TaskGenericType != null)
            {
                // For backwards compatibility we're creating a sync-executor for an async method. This was
                // supported in the past even though MVC wouldn't have called it.
                _executor = GetExecutor(methodInfo, targetTypeInfo);
                _executorAsync = GetExecutorAsync(TaskGenericType, methodInfo, targetTypeInfo);
            }
            else
            {
                _executor = GetExecutor(methodInfo, targetTypeInfo);
            }

            _parameterDefaultValues = GetParameterDefaultValues(ActionParameters);
        }

        private delegate Task<object> ActionExecutorAsync(object target, object[] parameters);

        private delegate object ActionExecutor(object target, object[] parameters);

        private delegate void VoidActionExecutor(object target, object[] parameters);

        public MethodInfo MethodInfo { get; }

        public ParameterInfo[] ActionParameters { get; }

        public TypeInfo TargetTypeInfo { get; }

        public Type TaskGenericType { get; }

        // This field is made internal set because it is set in unit tests.
        public Type MethodReturnType { get; internal set; }

        public bool IsMethodAsync { get; }

        public bool IsTypeAssignableFromIActionResult { get; }

        public static ObjectMethodExecutor Create(MethodInfo methodInfo, TypeInfo targetTypeInfo)
        {
            var executor = new ObjectMethodExecutor(methodInfo, targetTypeInfo);
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

        private static ActionExecutorAsync GetExecutorAsync(Type taskInnerType, MethodInfo methodInfo, TypeInfo targetTypeInfo)
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

            var coerceMethodCall = GetCoerceMethodCallExpression(taskInnerType, methodCall, methodInfo);
            var lambda = Expression.Lambda<ActionExecutorAsync>(coerceMethodCall, targetParameter, parametersParameter);
            return lambda.Compile();
        }

        // We need to CoerceResult as the object value returned from methodInfo.Invoke has to be cast to a Task<T>.
        // This is necessary to enable calling await on the returned task.
        // i.e we need to write the following var result = await (Task<ActualType>)mInfo.Invoke.
        // Returning Task<object> enables us to await on the result.
        private static Expression GetCoerceMethodCallExpression(
            Type taskValueType,
            MethodCallExpression methodCall,
            MethodInfo methodInfo)
        {
            var castMethodCall = Expression.Convert(methodCall, typeof(object));
            // for: public Task<T> Action()
            // constructs: return (Task<object>)Convert<T>((Task<T>)result)
            var genericMethodInfo = _convertOfTMethod.MakeGenericMethod(taskValueType);
            var genericMethodCall = Expression.Call(null, genericMethodInfo, castMethodCall);
            var convertedResult = Expression.Convert(genericMethodCall, typeof(Task<object>));
            return convertedResult;
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

        private static object[] GetParameterDefaultValues(ParameterInfo[] parameters)
        {
            var values = new object[parameters.Length];

            for (var i = 0; i < parameters.Length; i++)
            {
                var parameterInfo = parameters[i];
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

                values[i] = defaultValue;
            }

            return values;
        }
    }
}
