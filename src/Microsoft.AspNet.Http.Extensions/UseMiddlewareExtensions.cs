// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Reflection;
using Microsoft.Framework.DependencyInjection;

namespace Microsoft.AspNet.Builder
{
    public static class UseMiddlewareExtensions
    {
        public static IApplicationBuilder UseMiddleware<T>(this IApplicationBuilder builder, params object[] args)
        {
            return builder.UseMiddleware(typeof(T), args);
        }

        public static IApplicationBuilder UseMiddleware(this IApplicationBuilder builder, Type middleware, params object[] args)
        {
            return builder.Use(next =>
            {
                var typeActivator = builder.ApplicationServices.GetService<ITypeActivator>();
                var instance = typeActivator.CreateInstance(builder.ApplicationServices, middleware, new[] { next }.Concat(args).ToArray());
                var methodinfo = middleware.GetMethod("Invoke", BindingFlags.Instance | BindingFlags.Public);
                return (RequestDelegate)methodinfo.CreateDelegate(typeof(RequestDelegate), instance);
            });
        }
    }
}