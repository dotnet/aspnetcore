// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Abstractions;
using Microsoft.Extensions.Internal;

namespace Microsoft.AspNetCore.Builder;

/// <summary>
/// Extension methods for adding typed middleware.
/// </summary>
public static class UseMiddlewareExtensions
{
    internal const string InvokeMethodName = "Invoke";
    internal const string InvokeAsyncMethodName = "InvokeAsync";

    private static readonly MethodInfo GetServiceInfo = typeof(UseMiddlewareExtensions).GetMethod(nameof(GetService), BindingFlags.NonPublic | BindingFlags.Static)!;

    // We're going to keep all public constructors and public methods on middleware
    private const DynamicallyAccessedMemberTypes MiddlewareAccessibility =
        DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicMethods;

    /// <summary>
    /// Adds a middleware type to the application's request pipeline.
    /// </summary>
    /// <typeparam name="TMiddleware">The middleware type.</typeparam>
    /// <param name="app">The <see cref="IApplicationBuilder"/> instance.</param>
    /// <param name="args">The arguments to pass to the middleware type instance's constructor.</param>
    /// <returns>The <see cref="IApplicationBuilder"/> instance.</returns>
    public static IApplicationBuilder UseMiddleware<[DynamicallyAccessedMembers(MiddlewareAccessibility)] TMiddleware>(this IApplicationBuilder app, params object?[] args)
    {
        return app.UseMiddleware(typeof(TMiddleware), args);
    }

    /// <summary>
    /// Adds a middleware type to the application's request pipeline.
    /// </summary>
    /// <param name="app">The <see cref="IApplicationBuilder"/> instance.</param>
    /// <param name="middleware">The middleware type.</param>
    /// <param name="args">The arguments to pass to the middleware type instance's constructor.</param>
    /// <returns>The <see cref="IApplicationBuilder"/> instance.</returns>
    public static IApplicationBuilder UseMiddleware(
        this IApplicationBuilder app,
        [DynamicallyAccessedMembers(MiddlewareAccessibility)] Type middleware,
        params object?[] args)
    {
        if (typeof(IMiddleware).IsAssignableFrom(middleware))
        {
            // IMiddleware doesn't support passing args directly since it's
            // activated from the container
            if (args.Length > 0)
            {
                throw new NotSupportedException(Resources.FormatException_UseMiddlewareExplicitArgumentsNotSupported(typeof(IMiddleware)));
            }

            var interfaceBinder = new InterfaceMiddlewareBinder(middleware);
            return app.Use(interfaceBinder.CreateMiddleware);
        }

        var methods = middleware.GetMethods(BindingFlags.Instance | BindingFlags.Public);
        MethodInfo? invokeMethod = null;
        foreach (var method in methods)
        {
            if (string.Equals(method.Name, InvokeMethodName, StringComparison.Ordinal) || string.Equals(method.Name, InvokeAsyncMethodName, StringComparison.Ordinal))
            {
                if (invokeMethod is not null)
                {
                    throw new InvalidOperationException(Resources.FormatException_UseMiddleMutlipleInvokes(InvokeMethodName, InvokeAsyncMethodName));
                }

                invokeMethod = method;
            }
        }

        if (invokeMethod is null)
        {
            throw new InvalidOperationException(Resources.FormatException_UseMiddlewareNoInvokeMethod(InvokeMethodName, InvokeAsyncMethodName, middleware));
        }

        if (!typeof(Task).IsAssignableFrom(invokeMethod.ReturnType))
        {
            throw new InvalidOperationException(Resources.FormatException_UseMiddlewareNonTaskReturnType(InvokeMethodName, InvokeAsyncMethodName, nameof(Task)));
        }

        var parameters = invokeMethod.GetParameters();
        if (parameters.Length == 0 || parameters[0].ParameterType != typeof(HttpContext))
        {
            throw new InvalidOperationException(Resources.FormatException_UseMiddlewareNoParameters(InvokeMethodName, InvokeAsyncMethodName, nameof(HttpContext)));
        }

        var reflectionBinder = new ReflectionMiddlewareBinder(app, middleware, args, invokeMethod, parameters);
        return app.Use(reflectionBinder.CreateMiddleware);
    }

    private sealed class ReflectionMiddlewareBinder
    {
        private readonly IApplicationBuilder _app;
        [DynamicallyAccessedMembers(MiddlewareAccessibility)]
        private readonly Type _middleware;
        private readonly object?[] _args;
        private readonly MethodInfo _invokeMethod;
        private readonly ParameterInfo[] _parameters;

        public ReflectionMiddlewareBinder(
            IApplicationBuilder app,
            [DynamicallyAccessedMembers(MiddlewareAccessibility)] Type middleware,
            object?[] args,
            MethodInfo invokeMethod,
            ParameterInfo[] parameters)
        {
            _app = app;
            _middleware = middleware;
            _args = args;
            _invokeMethod = invokeMethod;
            _parameters = parameters;
        }

        // The CreateMiddleware method name is used by ApplicationBuilder to resolve the middleware type.
        public RequestDelegate CreateMiddleware(RequestDelegate next)
        {
            var ctorArgs = new object[_args.Length + 1];
            ctorArgs[0] = next;
            Array.Copy(_args, 0, ctorArgs, 1, _args.Length);
            var instance = ActivatorUtilities.CreateInstance(_app.ApplicationServices, _middleware, ctorArgs);
            if (_parameters.Length == 1)
            {
                return (RequestDelegate)_invokeMethod.CreateDelegate(typeof(RequestDelegate), instance);
            }

            // Performance optimization: Use compiled expressions to invoke middleware with services injected in Invoke.
            // If IsDynamicCodeCompiled is false then use standard reflection to avoid overhead of interpreting expressions.
            var factory = RuntimeFeature.IsDynamicCodeCompiled
                ? CompileExpression<object>(_invokeMethod, _parameters)
                : ReflectionFallback<object>(_invokeMethod, _parameters);

            return context =>
            {
                var serviceProvider = context.RequestServices ?? _app.ApplicationServices;
                if (serviceProvider == null)
                {
                    throw new InvalidOperationException(Resources.FormatException_UseMiddlewareIServiceProviderNotAvailable(nameof(IServiceProvider)));
                }

                return factory(instance, context, serviceProvider);
            };
        }

        public override string ToString() => _middleware.ToString();
    }

    private sealed class InterfaceMiddlewareBinder
    {
        private readonly Type _middlewareType;

        public InterfaceMiddlewareBinder(Type middlewareType)
        {
            _middlewareType = middlewareType;
        }

        // The CreateMiddleware method name is used by ApplicationBuilder to resolve the middleware type.
        public RequestDelegate CreateMiddleware(RequestDelegate next)
        {
            return async context =>
            {
                var middlewareFactory = (IMiddlewareFactory?)context.RequestServices.GetService(typeof(IMiddlewareFactory));
                if (middlewareFactory == null)
                {
                    // No middleware factory
                    throw new InvalidOperationException(Resources.FormatException_UseMiddlewareNoMiddlewareFactory(typeof(IMiddlewareFactory)));
                }

                var middleware = middlewareFactory.Create(_middlewareType);
                if (middleware == null)
                {
                    // The factory returned null, it's a broken implementation
                    throw new InvalidOperationException(Resources.FormatException_UseMiddlewareUnableToCreateMiddleware(middlewareFactory.GetType(), _middlewareType));
                }

                try
                {
                    await middleware.InvokeAsync(context, next);
                }
                finally
                {
                    middlewareFactory.Release(middleware);
                }
            };
        }

        public override string ToString() => _middlewareType.ToString();
    }

    private static Func<T, HttpContext, IServiceProvider, Task> ReflectionFallback<T>(MethodInfo methodInfo, ParameterInfo[] parameters)
    {
        Debug.Assert(!RuntimeFeature.IsDynamicCodeSupported, "Use reflection fallback when dynamic code is not supported.");

        for (var i = 1; i < parameters.Length; i++)
        {
            var parameterType = parameters[i].ParameterType;
            if (parameterType.IsByRef)
            {
                throw new NotSupportedException(Resources.FormatException_InvokeDoesNotSupportRefOrOutParams(InvokeMethodName));
            }
        }

        return (middleware, context, serviceProvider) =>
        {
            var methodArguments = new object[parameters.Length];
            methodArguments[0] = context;
            for (var i = 1; i < parameters.Length; i++)
            {
                methodArguments[i] = GetService(serviceProvider, parameters[i].ParameterType, methodInfo.DeclaringType!);
            }

            return (Task)methodInfo.Invoke(middleware, BindingFlags.DoNotWrapExceptions, binder: null, methodArguments, culture: null)!;
        };
    }

    private static Func<T, HttpContext, IServiceProvider, Task> CompileExpression<T>(MethodInfo methodInfo, ParameterInfo[] parameters)
    {
        Debug.Assert(RuntimeFeature.IsDynamicCodeSupported, "Use compiled expression when dynamic code is supported.");

        // If we call something like
        //
        // public class Middleware
        // {
        //    public Task Invoke(HttpContext context, ILoggerFactory loggerFactory)
        //    {
        //
        //    }
        // }
        //

        // We'll end up with something like this:
        //   Generic version:
        //
        //   Task Invoke(Middleware instance, HttpContext httpContext, IServiceProvider provider)
        //   {
        //      return instance.Invoke(httpContext, (ILoggerFactory)UseMiddlewareExtensions.GetService(provider, typeof(ILoggerFactory));
        //   }

        //   Non generic version:
        //
        //   Task Invoke(object instance, HttpContext httpContext, IServiceProvider provider)
        //   {
        //      return ((Middleware)instance).Invoke(httpContext, (ILoggerFactory)UseMiddlewareExtensions.GetService(provider, typeof(ILoggerFactory));
        //   }

        var middleware = typeof(T);

        var httpContextArg = Expression.Parameter(typeof(HttpContext), "httpContext");
        var providerArg = Expression.Parameter(typeof(IServiceProvider), "serviceProvider");
        var instanceArg = Expression.Parameter(middleware, "middleware");

        var methodArguments = new Expression[parameters.Length];
        methodArguments[0] = httpContextArg;
        for (var i = 1; i < parameters.Length; i++)
        {
            var parameterType = parameters[i].ParameterType;
            if (parameterType.IsByRef)
            {
                throw new NotSupportedException(Resources.FormatException_InvokeDoesNotSupportRefOrOutParams(InvokeMethodName));
            }

            var parameterTypeExpression = new Expression[]
            {
                providerArg,
                Expression.Constant(parameterType, typeof(Type)),
                Expression.Constant(methodInfo.DeclaringType, typeof(Type))
            };

            var getServiceCall = Expression.Call(GetServiceInfo, parameterTypeExpression);
            methodArguments[i] = Expression.Convert(getServiceCall, parameterType);
        }

        Expression middlewareInstanceArg = instanceArg;
        if (methodInfo.DeclaringType != null && methodInfo.DeclaringType != typeof(T))
        {
            middlewareInstanceArg = Expression.Convert(middlewareInstanceArg, methodInfo.DeclaringType);
        }

        var body = Expression.Call(middlewareInstanceArg, methodInfo, methodArguments);

        var lambda = Expression.Lambda<Func<T, HttpContext, IServiceProvider, Task>>(body, instanceArg, httpContextArg, providerArg);

        return lambda.Compile();
    }

    private static object GetService(IServiceProvider sp, Type type, Type middleware)
    {
        var service = sp.GetService(type);
        if (service == null)
        {
            throw new InvalidOperationException(Resources.FormatException_InvokeMiddlewareNoService(type, middleware));
        }

        return service;
    }
}
