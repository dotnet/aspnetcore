// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Text.Encodings.Web;
using Microsoft.AspNet.Routing;
using Microsoft.AspNet.Routing.Internal;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.ObjectPool;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Contains extension methods to <see cref="IServiceCollection"/>.
    /// </summary>
    public static class RoutingServiceCollectionExtensions
    {
        public static IServiceCollection AddRouting(this IServiceCollection services)
        {
            return AddRouting(services, configureOptions: null);
        }

        public static IServiceCollection AddRouting(
            this IServiceCollection services,
            Action<RouteOptions> configureOptions)
        {
            services.AddOptions();
            services.TryAddTransient<IInlineConstraintResolver, DefaultInlineConstraintResolver>();
            services.TryAddSingleton(UrlEncoder.Default);
            services.TryAddSingleton<ObjectPoolProvider>(new DefaultObjectPoolProvider());
            services.TryAddSingleton<ObjectPool<UriBuildingContext>>(s =>
            {
                var provider = s.GetRequiredService<ObjectPoolProvider>();
                var encoder = s.GetRequiredService<UrlEncoder>();
                return provider.Create<UriBuildingContext>(new UriBuilderContextPooledObjectPolicy(encoder));
            });

            if (configureOptions != null)
            {
                services.Configure(configureOptions);
            }

            return services;
        }
    }
}