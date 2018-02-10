// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Protocols;

namespace Microsoft.AspNetCore.Sockets
{
    public static class SocketBuilderExtensions
    {
        public static IConnectionBuilder Use(this IConnectionBuilder socketBuilder, Func<ConnectionContext, Func<Task>, Task> middleware)
        {
            return socketBuilder.Use(next =>
            {
                return context =>
                {
                    Func<Task> simpleNext = () => next(context);
                    return middleware(context, simpleNext);
                };
            });
        }

        public static IConnectionBuilder Run(this IConnectionBuilder socketBuilder, Func<ConnectionContext, Task> middleware)
        {
            return socketBuilder.Use(next =>
            {
                return context =>
                {
                    return middleware(context);
                };
            });
        }
    }
}
