// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Http.Abstractions;
using Microsoft.Extensions.Internal;

namespace Microsoft.AspNet.Builder
{
    /// <summary>
    /// Extension methods for adding typed middlware.
    /// </summary>
    public static class UseMiddlewareExtensions
    {
        private const string InvokeMethodName = "Invoke";

        private static readonly MethodInfo GetServiceInfo = typeof(UseMiddlewareExtensions).GetMethod(nameof(GetService), BindingFlags.NonPublic | BindingFlags.Static);

        /// <summary>
        /// Adds a middleware type to the application's request pipeline.
        /// </summary>
        /// <typeparam name="TMiddleware">The middleware type.</typeparam>
        /// <param name="app">The <see cref="IApplicationBuilder"/> instance.</param>
        /// <param name="args">The arguments to pass to the middleware type instance's constructor.</param>
        /// <returns>The <see cref="IApplicationBuilder"/> instance.</returns>
        public static IApplicationBuilder UseMiddleware<TMiddleware>(this IApplicationBuilder app, params object[] args)
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
        public static IApplicationBuilder UseMiddleware(this IApplicationBuilder app, Type middleware, params object[] args)
        {
            var applicationServices = app.ApplicationServices;
            return app.Use(next =>
            {
                var methods = middleware.GetMethods(BindingFlags.Instance | BindingFlags.Public);
                var invokeMethods = methods.Where(m => string.Equals(m.Name, InvokeMethodName, StringComparison.Ordinal)).ToArray();
                if (invokeMethods.Length > 1)
                {
                    throw new InvalidOperationException(Resources.FormatException_UseMiddleMutlipleInvokes(InvokeMethodName));
                }

                if (invokeMethods.Length == 0)
                {
                    throw new InvalidOperationException(Resources.FormatException_UseMiddlewareNoInvokeMethod(InvokeMethodName));
                }

                var methodinfo = invokeMethods[0];
                if (!typeof(Task).IsAssignableFrom(methodinfo.ReturnType))
                {
                    throw new InvalidOperationException(Resources.FormatException_UseMiddlewareNonTaskReturnType(InvokeMethodName, nameof(Task)));
                }

                var parameters = methodinfo.GetParameters();
                if (parameters.Length == 0 || parameters[0].ParameterType != typeof(HttpContext))
                {
                    throw new InvalidOperationException(Resources.FormatException_UseMiddlewareNoParameters(InvokeMethodName, nameof(HttpContext)));
                }

                var ctorArgs = new object[args.Length + 1];
                ctorArgs[0] = next;
                Array.Copy(args, 0, ctorArgs, 1, args.Length);
                var instance = ActivatorUtilities.CreateInstance(app.ApplicationServices, middleware, ctorArgs);
                if (parameters.Length == 1)
                {
                    return (RequestDelegate)methodinfo.CreateDelegate(typeof(RequestDelegate), instance);
                }

                var factory = Compile<object>(methodinfo, parameters);

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

        private static Func<T, HttpContext, IServiceProvider, Task> Compile<T>(MethodInfo methodinfo, ParameterInfo[] parameters)
        {

            // If we call something like
            // 
            // public class Middleware
            // {
            //    public Task Invoke(HttpContext context, ILoggerFactory loggeryFactory)
            //    {
            //         
            //    }
            // }
            //

            // We'll end up with something like this:
            //   Generic version:
            //
            //   Task Invoke(Middleware instance, HttpContext httpContext, IServiceprovider provider)
            //   {
            //      return instance.Invoke(httpContext, (ILoggerFactory)UseMiddlewareExtensions.GetService(provider, typeof(ILoggerFactory));
            //   }

            //   Non generic version:
            //
            //   Task Invoke(object instance, HttpContext httpContext, IServiceprovider provider)
            //   {
            //      return ((Middleware)instance).Invoke(httpContext, (ILoggerFactory)UseMiddlewareExtensions.GetService(provider, typeof(ILoggerFactory));
            //   }

            //   context =>
            //   {
            //       var serviceProvider = context.RequestServices ?? applicationServices;
            //       if (serviceProvider == null)
            //       {
            //          throw new InvalidOperationException(Resources.FormatException_UseMiddlewareIServiceProviderNotAvailable(nameof(IServiceProvider)));
            //       }
            // 
            //       return Invoke(httpContext, serviceProvider);
            //   }

            var middleware = typeof(T);

            var httpContextArg = Expression.Parameter(typeof(HttpContext), "httpContext");
            var providerArg = Expression.Parameter(typeof(IServiceProvider), "serviceProvider");
            var instanceArg = Expression.Parameter(middleware, "middleware");

            var methodArguments = new Expression[parameters.Length];
            methodArguments[0] = httpContextArg;
            for (int i = 1; i < parameters.Length; i++)
            {
                var parameterType = parameters[i].ParameterType;
                if (parameterType.IsByRef)
                {
                    throw new NotSupportedException(Resources.FormatException_InvokeDoesNotSupportRefOrOutParams(InvokeMethodName));
                }

                var parameterTypeExpression = new Expression[] {
                        providerArg,
                        Expression.Constant(parameterType, typeof(Type)),
                        Expression.Constant(methodinfo.DeclaringType, typeof(Type))
                    };
                methodArguments[i] = Expression.Convert(Expression.Call(GetServiceInfo, parameterTypeExpression), parameterType);
            }

            Expression middlewareInstanceArg = instanceArg;
            if (methodinfo.DeclaringType != typeof(T))
            {
                middlewareInstanceArg = Expression.Convert(middlewareInstanceArg, methodinfo.DeclaringType);
            }

            var body = Expression.Call(middlewareInstanceArg, methodinfo, methodArguments);

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
}