// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNet.Http;

namespace Microsoft.AspNet.Builder
{
    public static class UseExtensions
    {
        /// <summary>
        /// Use middleware defined in-line.
        /// </summary>
        /// <param name="app"></param>
        /// <param name="middleware">A function that handles the request or calls the given next function.</param>
        /// <returns></returns>
        public static IApplicationBuilder Use(this IApplicationBuilder app, Func<HttpContext, Func<Task>, Task> middleware)
        {
            return app.Use(next =>
            {
                return context =>
                {
                    Func<Task> simpleNext = () => next(context);
                    return middleware(context, simpleNext);
                };
            });
        }

        /// <summary>
        /// Use middleware defined in-line
        /// </summary>
        /// <typeparam name="TService1">Per-request service required by middleware</typeparam>
        /// <param name="app"></param>
        /// <param name="middleware">A function that handles the request or calls the given next function.</param>
        /// <returns></returns>
        public static IApplicationBuilder Use<TService1>(this IApplicationBuilder app, Func<HttpContext, Func<Task>, TService1, Task> middleware)
        {
            var applicationServices = app.ApplicationServices;
            return app.Use(next => context =>
            {
                var serviceProvider = context.RequestServices ?? context.ApplicationServices ?? applicationServices;
                if (serviceProvider == null)
                {
                    throw new Exception("TODO: IServiceProvider is not available");
                }
                return middleware(
                    context,
                    () => next(context),
                    GetRequiredService<TService1>(serviceProvider));
            });
        }

        /// <summary>
        /// Use middleware defined in-line
        /// </summary>
        /// <typeparam name="TService1">Per-request service required by middleware</typeparam>
        /// <typeparam name="TService2">Per-request service required by middleware</typeparam>
        /// <param name="app"></param>
        /// <param name="middleware">A function that handles the request or calls the given next function.</param>
        /// <returns></returns>
        public static IApplicationBuilder Use<TService1, TService2>(this IApplicationBuilder app, Func<HttpContext, Func<Task>, TService1, TService2, Task> middleware)
        {
            var applicationServices = app.ApplicationServices;
            return app.Use(next => context =>
            {
                var serviceProvider = context.RequestServices ?? context.ApplicationServices ?? applicationServices;
                if (serviceProvider == null)
                {
                    throw new Exception("TODO: IServiceProvider is not available");
                }
                return middleware(
                    context,
                    () => next(context),
                    GetRequiredService<TService1>(serviceProvider),
                    GetRequiredService<TService2>(serviceProvider));
            });
        }

        /// <summary>
        /// Use middleware defined in-line
        /// </summary>
        /// <typeparam name="TService1">Per-request service required by middleware</typeparam>
        /// <typeparam name="TService2">Per-request service required by middleware</typeparam>
        /// <typeparam name="TService3">Per-request service required by middleware</typeparam>
        /// <param name="app"></param>
        /// <param name="middleware">A function that handles the request or calls the given next function.</param>
        /// <returns></returns>
        public static IApplicationBuilder Use<TService1, TService2, TService3>(this IApplicationBuilder app, Func<HttpContext, Func<Task>, TService1, TService2, TService3, Task> middleware)
        {
            var applicationServices = app.ApplicationServices;
            return app.Use(next => context =>
            {
                var serviceProvider = context.RequestServices ?? context.ApplicationServices ?? applicationServices;
                if (serviceProvider == null)
                {
                    throw new Exception("TODO: IServiceProvider is not available");
                }
                return middleware(
                    context,
                    () => next(context),
                    GetRequiredService<TService1>(serviceProvider),
                    GetRequiredService<TService2>(serviceProvider),
                    GetRequiredService<TService3>(serviceProvider));
            });
        }

        /// <summary>
        /// Use middleware defined in-line
        /// </summary>
        /// <typeparam name="TService1">Per-request service required by middleware</typeparam>
        /// <typeparam name="TService2">Per-request service required by middleware</typeparam>
        /// <typeparam name="TService3">Per-request service required by middleware</typeparam>
        /// <typeparam name="TService4">Per-request service required by middleware</typeparam>
        /// <param name="app"></param>
        /// <param name="middleware">A function that handles the request or calls the given next function.</param>
        /// <returns></returns>
        public static IApplicationBuilder Use<TService1, TService2, TService3, TService4>(this IApplicationBuilder app, Func<HttpContext, Func<Task>, TService1, TService2, TService3, TService4, Task> middleware)
        {
            var applicationServices = app.ApplicationServices;
            return app.Use(next => context =>
            {
                var serviceProvider = context.RequestServices ?? context.ApplicationServices ?? applicationServices;
                if (serviceProvider == null)
                {
                    throw new Exception("TODO: IServiceProvider is not available");
                }
                return middleware(
                    context,
                    () => next(context),
                    GetRequiredService<TService1>(serviceProvider),
                    GetRequiredService<TService2>(serviceProvider),
                    GetRequiredService<TService3>(serviceProvider),
                    GetRequiredService<TService4>(serviceProvider));
            });
        }

        private static TService GetRequiredService<TService>(IServiceProvider serviceProvider)
        {
            var service = (TService)serviceProvider.GetService(typeof(TService));

            if (service == null)
            {
                throw new Exception(string.Format("TODO: No service for type '{0}' has been registered.", typeof(TService)));
            }

            return service;
        }
    }
}