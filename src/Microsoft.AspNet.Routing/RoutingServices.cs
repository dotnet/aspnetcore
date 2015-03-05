// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Framework.DependencyInjection;

namespace Microsoft.AspNet.Routing
{
    public static class RoutingServices
    {
        public static IServiceCollection AddRouting(this IServiceCollection services)
        {
            services.TryAdd(ServiceDescriptor.Transient<IInlineConstraintResolver, DefaultInlineConstraintResolver>());
            return services;
        }
    }
}
