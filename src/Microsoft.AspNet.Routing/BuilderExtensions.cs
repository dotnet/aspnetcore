// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Routing;

namespace Microsoft.AspNet.Builder
{
    public static class BuilderExtensions
    {
        public static IBuilder UseRouter([NotNull] this IBuilder builder, [NotNull] IRouter router)
        {
            builder.Use((next) => new RouterMiddleware(next, router).Invoke);
            return builder;
        }
    }
}