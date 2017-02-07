// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Mvc.RazorPages.Internal
{
    public static class ExecutorFactory
    {
        public static Func<Page, object, Task<IActionResult>> Create(MethodInfo method)
        {
            return new Executor()
            {
                Method = method,
            }.Execute;
        }

        private class Executor
        {
            public MethodInfo Method { get; set; }

            public async Task<IActionResult> Execute(Page page, object model)
            {
                var handler = HandlerMethod.Create(Method);

                var receiver = Method.DeclaringType.IsAssignableFrom(page.GetType()) ? page : model;

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

                var result = await handler.Execute(receiver, arguments);
                return result;
            }
        }

        private class HandlerParameter
        {
            public string Name { get; set; }

            public Type Type { get; set; }

            public object DefaultValue { get; set; }
        }

        private abstract class HandlerMethod
        {
            public static HandlerMethod Create(MethodInfo method)
            {
                var methodParameters = method.GetParameters();
                var parameters = new HandlerParameter[methodParameters.Length];

                for (var i = 0; i < methodParameters.Length; i++)
                {
                    parameters[i] = new HandlerParameter()
                    {
                        DefaultValue = methodParameters[i].HasDefaultValue ? methodParameters[i].DefaultValue : null,
                        Name = methodParameters[i].Name,
                        Type = methodParameters[i].ParameterType,
                    };
                }

                if (method.ReturnType == typeof(Task))
                {
                    return new NonGenericTaskHandlerMethod(parameters, method);
                }
                else if (method.ReturnType == typeof(void))
                {
                    return new VoidHandlerMethod(parameters, method);
                }
                else if (
                    method.ReturnType.IsConstructedGenericType &&
                    method.ReturnType.GetTypeInfo().GetGenericTypeDefinition() == typeof(Task<>) &&
                    typeof(IActionResult).IsAssignableFrom(method.ReturnType.GetTypeInfo().GetGenericArguments()[0]))
                {
                    return new GenericTaskHandlerMethod(parameters, method);
                }
                else if (typeof(IActionResult).IsAssignableFrom(method.ReturnType))
                {
                    return new ActionResultHandlerMethod(parameters, method);
                }
                else
                {
                    throw new InvalidOperationException("unsupported handler method return type");
                }
            }

            protected static Expression[] Unpack(Expression arguments, HandlerParameter[] parameters)
            {
                var unpackExpressions = new Expression[parameters.Length];
                for (var i = 0; i < parameters.Length; i++)
                {
                    unpackExpressions[i] = Expression.Convert(Expression.ArrayIndex(arguments, Expression.Constant(i)), parameters[i].Type);
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
                return (object)await task;
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
    }
}