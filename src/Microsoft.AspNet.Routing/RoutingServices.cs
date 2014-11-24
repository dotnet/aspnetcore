// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Framework.ConfigurationModel;
using Microsoft.Framework.DependencyInjection;

namespace Microsoft.AspNet.Routing
{
    public static class RoutingServices
    {
        public static IServiceCollection AddRouting(this IServiceCollection services, IConfiguration config = null)
        {
            var describe = new ServiceDescriber(config);
            services.TryAdd(describe.Transient<IInlineConstraintResolver, DefaultInlineConstraintResolver>());
            return services;
        }
    }
}
