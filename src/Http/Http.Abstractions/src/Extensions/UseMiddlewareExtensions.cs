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

            return UseMiddlewareInterface(app, middleware);
        }

        var applicationServices = app.ApplicationServices;
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

        var state = new InvokeMiddlewareState(middleware);

        return app.Use(next =>
        {
            var middleware = state.Middleware;

            var ctorArgs = new object[args.Length + 1];
            ctorArgs[0] = next;
            Array.Copy(args, 0, ctorArgs, 1, args.Length);
            var instance = ActivatorUtilities.CreateInstance(app.ApplicationServices, middleware, ctorArgs);
            if (parameters.Length == 1)
            {
                return (RequestDelegate)invokeMethod.CreateDelegate(typeof(RequestDelegate), instance);
            }

            // Performance optimization: Use compiled expressions to invoke middleware with services injected in Invoke.
            // If IsDynamicCodeCompiled is false then use standard reflection to avoid overhead of interpreting expressions.
            var factory = RuntimeFeature.IsDynamicCodeCompiled
                ? CompileExpression<object>(invokeMethod, parameters)
                : ReflectionFallback<object>(invokeMethod, parameters);

            return context =>
            {
                var serviceProvider = context.RequestServices ?? applicationServices;
                if (serviceProvider == null)
                {
                    throw new InvalidOperationException(Resources.FormatException_UseMiddlewareIServiceProviderNotAvailable(nameof(IServiceProvider)));
                }

                return factory(instance, context, serviceProvider);
            };
        });
    }

    private static IApplicationBuilder UseMiddlewareInterface(
        IApplicationBuilder app,
        Type middlewareType)
    {
        return app.Use(next =>
        {
            return async context =>
            {
                var middlewareFactory = (IMiddlewareFactory?)context.RequestServices.GetService(typeof(IMiddlewareFactory));
                if (middlewareFactory == null)
                {
                    // No middleware factory
                    throw new InvalidOperationException(Resources.FormatException_UseMiddlewareNoMiddlewareFactory(typeof(IMiddlewareFactory)));
                }

                var middleware = middlewareFactory.Create(middlewareType);
                if (middleware == null)
                {
                    // The factory returned null, it's a broken implementation
                    throw new InvalidOperationException(Resources.FormatException_UseMiddlewareUnableToCreateMiddleware(middlewareFactory.GetType(), middlewareType));
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
        });
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

    // Workaround for linker bug: https://github.com/dotnet/linker/issues/1981
    private readonly struct InvokeMiddlewareState
    {
        public InvokeMiddlewareState([DynamicallyAccessedMembers(MiddlewareAccessibility)] Type middleware)
        {
            Middleware = middleware;
        }

        [DynamicallyAccessedMembers(MiddlewareAccessibility)]
        public Type Middleware { get; }
    }
}
