﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using Microsoft.AspNet.Routing;

namespace Microsoft.AspNet.Abstractions
{
    public static class BuilderExtensions
    {
        public static IBuilder UseRouter(this IBuilder builder, IRouteCollection routes)
        {
            builder.Use((next) => new RouterMiddleware(next, routes).Invoke);
            return builder;
        }
    }
}

