// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.Internal;

namespace Microsoft.AspNetCore.Mvc.RazorPages.Internal
{
    public static class ExecutorFactory
    {
        public static Func<Page, object, Task<IActionResult>> CreateExecutor(
            CompiledPageActionDescriptor actionDescriptor,
            MethodInfo method)
        {
            if (actionDescriptor == null)
            {
                throw new ArgumentNullException(nameof(actionDescriptor));
            }

            if (method == null)
            {
                throw new ArgumentNullException(nameof(method));
            }

            var methodIsDeclaredOnPage = method.DeclaringType.GetTypeInfo().IsAssignableFrom(actionDescriptor.PageTypeInfo);
            var handler = CreateHandlerMethod(method);

            return async (page, model) =>
            {
                var arguments = new object[handler.Parameters.Length];
                for (var i = 0; i < handler.Parameters.Length; i++)
                {
                    var parameter = handler.Parameters[i];
                    arguments[i] = await page.Binder.BindModelAsync(
                        page.PageContext,
                        parameter.Type,
                        parameter.DefaultValue,
                        parameter.Name);
                }

                var receiver = methodIsDeclaredOnPage ? page : model;
                var result = await handler.Execute(receiver, arguments);
                return result;
            };
        }

        private static HandlerMethod CreateHandlerMethod(MethodInfo method)
        {
            var methodParameters = method.GetParameters();
            var parameters = new HandlerParameter[methodParameters.Length];

            for (var i = 0; i < methodParameters.Length; i++)
            {
                var methodParameter = methodParameters[i];
                object defaultValue = null;
                if (methodParameter.HasDefaultValue)
                {
                    defaultValue = methodParameter.DefaultValue;
                }
                else if (methodParameter.ParameterType.GetTypeInfo().IsValueType)
                {
                    defaultValue = Activator.CreateInstance(methodParameter.ParameterType);
                }

                parameters[i] = new HandlerParameter(methodParameter.Name, methodParameter.ParameterType, defaultValue);
            }

            var returnType = method.ReturnType;
            var returnTypeInfo = method.ReturnType.GetTypeInfo();
            if (returnType == typeof(void))
            {
                return new VoidHandlerMethod(parameters, method);
            }
            else if (typeof(IActionResult).IsAssignableFrom(returnType))
            {
                return new ActionResultHandlerMethod(parameters, method);
            }
            else if (returnType == typeof(Task))
            {
                return new NonGenericTaskHandlerMethod(parameters, method);
            }
            else
            {
                var taskType = ClosedGenericMatcher.ExtractGenericInterface(returnType, typeof(Task<>));
                if (taskType != null && typeof(IActionResult).IsAssignableFrom(taskType.GenericTypeArguments[0]))
                {
                    return new GenericTaskHandlerMethod(parameters, method);
                }
            }

            throw new InvalidOperationException(Resources.FormatUnsupportedHandlerMethodType(returnType));
        }

        private abstract class HandlerMethod
        {
            protected static Expression[] Unpack(Expression arguments, HandlerParameter[] parameters)
            {
                var unpackExpressions = new Expression[parameters.Length];
                for (var i = 0; i < parameters.Length; i++)
                {
                    unpackExpressions[i] = Expression.Convert(
                        Expression.ArrayIndex(arguments, Expression.Constant(i)),
                        parameters[i].Type);
                }

                return unpackExpressions;
            }

            protected HandlerMethod(HandlerParameter[] parameters)
            {
                Parameters = parameters;
            }

            public HandlerParameter[] Parameters { get; }

            public abstract Task<IActionResult> Execute(object receiver, object[] arguments);
        }

        private class NonGenericTaskHandlerMethod : HandlerMethod
        {
            private readonly Func<object, object[], Task> _thunk;

            public NonGenericTaskHandlerMethod(HandlerParameter[] parameters, MethodInfo method)
                : base(parameters)
            {
                var receiver = Expression.Parameter(typeof(object), "receiver");
                var arguments = Expression.Parameter(typeof(object[]), "arguments");

                _thunk = Expression.Lambda<Func<object, object[], Task>>(
                    Expression.Call(
                        Expression.Convert(receiver, method.DeclaringType),
                        method,
                        Unpack(arguments, parameters)),
                    receiver,
                    arguments).Compile();
            }

            public override async Task<IActionResult> Execute(object receiver, object[] arguments)
            {
                await _thunk(receiver, arguments);
                return null;
            }
        }

        private class GenericTaskHandlerMethod : HandlerMethod
        {
            private static readonly MethodInfo ConvertMethod = typeof(GenericTaskHandlerMethod).GetMethod(
                nameof(Convert),
                BindingFlags.NonPublic | BindingFlags.Static);

            private readonly Func<object, object[], Task<object>> _thunk;

            public GenericTaskHandlerMethod(HandlerParameter[] parameters, MethodInfo method)
                : base(parameters)
            {
                var receiver = Expression.Parameter(typeof(object), "receiver");
                var arguments = Expression.Parameter(typeof(object[]), "arguments");

                _thunk = Expression.Lambda<Func<object, object[], Task<object>>>(
                    Expression.Call(
                        ConvertMethod.MakeGenericMethod(method.ReturnType.GenericTypeArguments),
                        Expression.Convert(
                            Expression.Call(
                                Expression.Convert(receiver, method.DeclaringType),
                                method,
                                Unpack(arguments, parameters)),
                            typeof(object))),
                    receiver,
                    arguments).Compile();
            }

            public override async Task<IActionResult> Execute(object receiver, object[] arguments)
            {
                var result = await _thunk(receiver, arguments);
                return (IActionResult)result;
            }

            private static async Task<object> Convert<T>(object taskAsObject)
            {
                var task = (Task<T>)taskAsObject;
                return await task;
            }
        }

        private class VoidHandlerMethod : HandlerMethod
        {
            private readonly Action<object, object[]> _thunk;

            public VoidHandlerMethod(HandlerParameter[] parameters, MethodInfo method)
                : base(parameters)
            {
                var receiver = Expression.Parameter(typeof(object), "receiver");
                var arguments = Expression.Parameter(typeof(object[]), "arguments");

                _thunk = Expression.Lambda<Action<object, object[]>>(
                    Expression.Call(
                        Expression.Convert(receiver, method.DeclaringType),
                        method,
                        Unpack(arguments, parameters)),
                    receiver,
                    arguments).Compile();
            }

            public override Task<IActionResult> Execute(object receiver, object[] arguments)
            {
                _thunk(receiver, arguments);
                return Task.FromResult<IActionResult>(null);
            }
        }

        private class ActionResultHandlerMethod : HandlerMethod
        {
            private readonly Func<object, object[], IActionResult> _thunk;

            public ActionResultHandlerMethod(HandlerParameter[] parameters, MethodInfo method)
                : base(parameters)
            {
                var receiver = Expression.Parameter(typeof(object), "receiver");
                var arguments = Expression.Parameter(typeof(object[]), "arguments");

                _thunk = Expression.Lambda<Func<object, object[], IActionResult>>(
                    Expression.Convert(
                        Expression.Call(
                            Expression.Convert(receiver, method.DeclaringType),
                            method,
                            Unpack(arguments, parameters)),
                        typeof(IActionResult)),
                    receiver,
                    arguments).Compile();
            }

            public override Task<IActionResult> Execute(object receiver, object[] arguments)
            {
                return Task.FromResult(_thunk(receiver, arguments));
            }
        }

        private struct HandlerParameter
        {
            public HandlerParameter(string name, Type type, object defaultValue)
            {
                Name = name;
                Type = type;
                DefaultValue = defaultValue;
            }

            public string Name { get; }

            public Type Type { get; }

            public object DefaultValue { get; }
        }
    }
}