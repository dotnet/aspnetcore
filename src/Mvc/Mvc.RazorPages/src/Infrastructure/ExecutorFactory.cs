// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.Extensions.Internal;

namespace Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure;

internal static class ExecutorFactory
{
    public static PageHandlerExecutorDelegate CreateExecutor(HandlerMethodDescriptor handlerDescriptor)
    {
        ArgumentNullException.ThrowIfNull(handlerDescriptor);

        var handler = CreateHandlerMethod(handlerDescriptor);

        return handler.Execute;
    }

    private static HandlerMethod CreateHandlerMethod(HandlerMethodDescriptor handlerDescriptor)
    {
        var method = handlerDescriptor.MethodInfo;
        var parameters = handlerDescriptor.Parameters.ToArray();

        var returnType = method.ReturnType;
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
        protected static Expression[] Unpack(Expression arguments, HandlerParameterDescriptor[] parameters)
        {
            var unpackExpressions = new Expression[parameters.Length];
            for (var i = 0; i < parameters.Length; i++)
            {
                unpackExpressions[i] = Expression.Convert(
                    Expression.ArrayIndex(arguments, Expression.Constant(i)),
                    parameters[i].ParameterType);
            }

            return unpackExpressions;
        }

        protected HandlerMethod(HandlerParameterDescriptor[] parameters)
        {
            Parameters = parameters;
        }

        public HandlerParameterDescriptor[] Parameters { get; }

        public abstract Task<IActionResult?> Execute(object receiver, object?[]? arguments);
    }

    private sealed class NonGenericTaskHandlerMethod : HandlerMethod
    {
        private readonly Func<object, object?[]?, Task> _thunk;

        public NonGenericTaskHandlerMethod(HandlerParameterDescriptor[] parameters, MethodInfo method)
            : base(parameters)
        {
            var receiver = Expression.Parameter(typeof(object), "receiver");
            var arguments = Expression.Parameter(typeof(object[]), "arguments");

            _thunk = Expression.Lambda<Func<object, object?[]?, Task>>(
                Expression.Call(
                    Expression.Convert(receiver, method.DeclaringType!),
                    method,
                    Unpack(arguments, parameters)),
                receiver,
                arguments).Compile();
        }

        public override async Task<IActionResult?> Execute(object receiver, object?[]? arguments)
        {
            await _thunk(receiver, arguments);
            return null;
        }
    }

    private sealed class GenericTaskHandlerMethod : HandlerMethod
    {
        private static readonly MethodInfo ConvertMethod = typeof(GenericTaskHandlerMethod).GetMethod(
            nameof(Convert),
            BindingFlags.NonPublic | BindingFlags.Static)!;

        private readonly Func<object, object?[]?, Task<object>> _thunk;

        public GenericTaskHandlerMethod(HandlerParameterDescriptor[] parameters, MethodInfo method)
            : base(parameters)
        {
            var receiver = Expression.Parameter(typeof(object), "receiver");
            var arguments = Expression.Parameter(typeof(object[]), "arguments");

            _thunk = Expression.Lambda<Func<object, object?[]?, Task<object>>>(
                Expression.Call(
                    ConvertMethod.MakeGenericMethod(method.ReturnType.GenericTypeArguments),
                    Expression.Convert(
                        Expression.Call(
                            Expression.Convert(receiver, method.DeclaringType!),
                            method,
                            Unpack(arguments, parameters)),
                        typeof(object))),
                receiver,
                arguments).Compile();
        }

        public override async Task<IActionResult?> Execute(object receiver, object?[]? arguments)
        {
            var result = await _thunk(receiver, arguments);
            return (IActionResult)result;
        }

        private static async Task<object?> Convert<T>(object taskAsObject)
        {
            var task = (Task<T>)taskAsObject;
            return await task;
        }
    }

    private sealed class VoidHandlerMethod : HandlerMethod
    {
        private readonly Action<object, object?[]?> _thunk;

        public VoidHandlerMethod(HandlerParameterDescriptor[] parameters, MethodInfo method)
            : base(parameters)
        {
            var receiver = Expression.Parameter(typeof(object), "receiver");
            var arguments = Expression.Parameter(typeof(object[]), "arguments");

            _thunk = Expression.Lambda<Action<object, object?[]?>>(
                Expression.Call(
                    Expression.Convert(receiver, method.DeclaringType!),
                    method,
                    Unpack(arguments, parameters)),
                receiver,
                arguments).Compile();
        }

        public override Task<IActionResult?> Execute(object receiver, object?[]? arguments)
        {
            _thunk(receiver, arguments);
            return Task.FromResult<IActionResult?>(null);
        }
    }

    private sealed class ActionResultHandlerMethod : HandlerMethod
    {
        private readonly Func<object, object?[]?, IActionResult?> _thunk;

        public ActionResultHandlerMethod(HandlerParameterDescriptor[] parameters, MethodInfo method)
            : base(parameters)
        {
            var receiver = Expression.Parameter(typeof(object), "receiver");
            var arguments = Expression.Parameter(typeof(object[]), "arguments");

            _thunk = Expression.Lambda<Func<object, object?[]?, IActionResult?>>(
                Expression.Convert(
                    Expression.Call(
                        Expression.Convert(receiver, method.DeclaringType!),
                        method,
                        Unpack(arguments, parameters)),
                    typeof(IActionResult)),
                receiver,
                arguments).Compile();
        }

        public override Task<IActionResult?> Execute(object receiver, object?[]? arguments)
        {
            return Task.FromResult(_thunk(receiver, arguments));
        }
    }
}
