// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Connections
{
    public static class MultiplexedConnectionBuilderExtensions
    {
        public static IMultiplexedConnectionBuilder UseMultiplexed(this IMultiplexedConnectionBuilder connectionBuilder, Func<MultiplexedConnectionContext, Func<Task>, Task> middleware)
        {
            return connectionBuilder.Use(next =>
            {
                return context =>
                {
                    Func<Task> simpleNext = () => next(context);
                    return middleware(context, simpleNext);
                };
            });
        }

        public static IMultiplexedConnectionBuilder RunMultiplexed(this IMultiplexedConnectionBuilder connectionBuilder, Func<MultiplexedConnectionContext, Task> middleware)
        {
            return connectionBuilder.Use(next =>
            {
                return context =>
                {
                    return middleware(context);
                };
            });
        }
    }
}
