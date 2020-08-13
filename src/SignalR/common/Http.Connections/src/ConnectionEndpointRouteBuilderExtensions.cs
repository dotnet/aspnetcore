// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.Http.Connections.Internal;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Builder
{
    public static class ConnectionEndpointRouteBuilderExtensions
    {
        /// <summary>
        /// Maps incoming requests with the specified path to the provided connection pipeline.
        /// </summary>
        /// <param name="endpoints">The <see cref="IEndpointRouteBuilder"/> to add the route to.</param>
        /// <param name="pattern">The route pattern.</param>
        /// <param name="configure">A callback to configure the connection.</param>
        /// <returns>An <see cref="ConnectionEndpointRouteBuilder"/> for endpoints associated with the connections.</returns>
        public static ConnectionEndpointRouteBuilder MapConnections(this IEndpointRouteBuilder endpoints, string pattern, Action<IConnectionBuilder> configure) =>
            endpoints.MapConnections(pattern, new HttpConnectionDispatcherOptions(), configure);

        /// <summary>
        /// Maps incoming requests with the specified path to the provided connection pipeline.
        /// </summary>
        /// <typeparam name="TConnectionHandler">The <see cref="ConnectionHandler"/> type.</typeparam>
        /// <param name="endpoints">The <see cref="IEndpointRouteBuilder"/> to add the route to.</param>
        /// <param name="pattern">The route pattern.</param>
        /// <returns>An <see cref="ConnectionEndpointRouteBuilder"/> for endpoints associated with the connections.</returns>
        public static ConnectionEndpointRouteBuilder MapConnectionHandler<TConnectionHandler>(this IEndpointRouteBuilder endpoints, string pattern) where TConnectionHandler : ConnectionHandler
        {
            return endpoints.MapConnectionHandler<TConnectionHandler>(pattern, configureOptions: null);
        }

        /// <summary>
        /// Maps incoming requests with the specified path to the provided connection pipeline.
        /// </summary>
        /// <typeparam name="TConnectionHandler">The <see cref="ConnectionHandler"/> type.</typeparam>
        /// <param name="endpoints">The <see cref="IEndpointRouteBuilder"/> to add the route to.</param>
        /// <param name="pattern">The route pattern.</param>
        /// <param name="configureOptions">A callback to configure dispatcher options.</param>
        /// <returns>An <see cref="ConnectionEndpointRouteBuilder"/> for endpoints associated with the connections.</returns>
        public static ConnectionEndpointRouteBuilder MapConnectionHandler<TConnectionHandler>(this IEndpointRouteBuilder endpoints, string pattern, Action<HttpConnectionDispatcherOptions> configureOptions) where TConnectionHandler : ConnectionHandler
        {
            var options = new HttpConnectionDispatcherOptions();
            configureOptions?.Invoke(options);

            var conventionBuilder = endpoints.MapConnections(pattern, options, b =>
            {
                b.UseConnectionHandler<TConnectionHandler>();
            });

            var attributes = typeof(TConnectionHandler).GetCustomAttributes(inherit: true);
            conventionBuilder.Add(e =>
            {
                // Add all attributes on the ConnectionHandler has metadata (this will allow for things like)
                // auth attributes and cors attributes to work seamlessly
                foreach (var item in attributes)
                {
                    e.Metadata.Add(item);
                }
            });

            return conventionBuilder;
        }


        /// <summary>
        /// Maps incoming requests with the specified path to the provided connection pipeline.
        /// </summary>
        /// <param name="endpoints">The <see cref="IEndpointRouteBuilder"/> to add the route to.</param>
        /// <param name="pattern">The route pattern.</param>
        /// <param name="options">Options used to configure the connection.</param>
        /// <param name="configure">A callback to configure the connection.</param>
        /// <returns>An <see cref="ConnectionEndpointRouteBuilder"/> for endpoints associated with the connections.</returns>
        public static ConnectionEndpointRouteBuilder MapConnections(this IEndpointRouteBuilder endpoints, string pattern, HttpConnectionDispatcherOptions options, Action<IConnectionBuilder> configure)
        {
            var dispatcher = endpoints.ServiceProvider.GetRequiredService<HttpConnectionDispatcher>();

            var connectionBuilder = new ConnectionBuilder(endpoints.ServiceProvider);
            configure(connectionBuilder);
            var connectionDelegate = connectionBuilder.Build();

            // REVIEW: Consider expanding the internals of the dispatcher as endpoint routes instead of
            // using if statements we can let the matcher handle

            var conventionBuilders = new List<IEndpointConventionBuilder>();

            // Build the negotiate application
            var app = endpoints.CreateApplicationBuilder();
            app.UseWebSockets();
            app.Run(c => dispatcher.ExecuteNegotiateAsync(c, options));
            var negotiateHandler = app.Build();

            var negotiateBuilder = endpoints.Map(pattern + "/negotiate", negotiateHandler);
            conventionBuilders.Add(negotiateBuilder);
            // Add the negotiate metadata so this endpoint can be identified
            negotiateBuilder.WithMetadata(new NegotiateMetadata());

            // build the execute handler part of the protocol
            app = endpoints.CreateApplicationBuilder();
            app.UseWebSockets();
            app.Run(c => dispatcher.ExecuteAsync(c, options, connectionDelegate));
            var executehandler = app.Build();

            var executeBuilder = endpoints.Map(pattern, executehandler);
            conventionBuilders.Add(executeBuilder);

            var compositeConventionBuilder = new CompositeEndpointConventionBuilder(conventionBuilders);

            // Add metadata to all of Endpoints
            compositeConventionBuilder.Add(e =>
            {
                // Add the authorization data as metadata
                foreach (var data in options.AuthorizationData)
                {
                    e.Metadata.Add(data);
                }
            });

            return new ConnectionEndpointRouteBuilder(compositeConventionBuilder);
        }

        private class CompositeEndpointConventionBuilder : IEndpointConventionBuilder
        {
            private readonly List<IEndpointConventionBuilder> _endpointConventionBuilders;

            public CompositeEndpointConventionBuilder(List<IEndpointConventionBuilder> endpointConventionBuilders)
            {
                _endpointConventionBuilders = endpointConventionBuilders;
            }

            public void Add(Action<EndpointBuilder> convention)
            {
                foreach (var endpointConventionBuilder in _endpointConventionBuilders)
                {
                    endpointConventionBuilder.Add(convention);
                }
            }
        }
    }
}
