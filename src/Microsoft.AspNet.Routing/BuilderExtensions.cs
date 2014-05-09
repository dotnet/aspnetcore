// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Routing;

namespace Microsoft.AspNet.Builder
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

