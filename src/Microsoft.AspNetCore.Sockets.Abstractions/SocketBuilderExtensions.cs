// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Sockets
{
    public static class SocketBuilderExtensions
    {
        public static ISocketBuilder Use(this ISocketBuilder socketBuilder, Func<ConnectionContext, Func<Task>, Task> middleware)
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

        public static ISocketBuilder Run(this ISocketBuilder socketBuilder, Func<ConnectionContext, Task> middleware)
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
