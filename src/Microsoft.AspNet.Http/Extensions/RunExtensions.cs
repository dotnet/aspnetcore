// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Http;
using System.Threading.Tasks;

namespace Microsoft.AspNet.Builder
{
    public static class RunExtensions
    {
        public static void Run([NotNull] this IApplicationBuilder app, [NotNull] RequestDelegate handler)
        {
            app.Use(_ => handler);
        }

        public static void Run<TService1>(this IApplicationBuilder app, Func<HttpContext, TService1, Task> handler)
        {
            app.Use<TService1>((ctx, _, s1) => handler(ctx, s1));
        }

        public static void Run<TService1, TService2>(this IApplicationBuilder app, Func<HttpContext, TService1, TService2, Task> handler)
        {
            app.Use<TService1, TService2>((ctx, _, s1, s2) => handler(ctx, s1, s2));
        }

        public static void Run<TService1, TService2, TService3>(this IApplicationBuilder app, Func<HttpContext, TService1, TService2, TService3, Task> handler)
        {
            app.Use<TService1, TService2, TService3>((ctx, _, s1, s2, s3) => handler(ctx, s1, s2, s3));
        }

        public static void Run<TService1, TService2, TService3, TService4>(this IApplicationBuilder app, Func<HttpContext, TService1, TService2, TService3, TService4, Task> handler)
        {
            app.Use<TService1, TService2, TService3, TService4>((ctx, _, s1, s2, s3, s4) => handler(ctx, s1, s2, s3, s4));
        }
    }
}