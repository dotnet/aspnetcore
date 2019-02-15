using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.Http.Connections.Internal;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Routing
{
    public static class ConnectionEndpointRouteBuilderExtensions
    {
        /// <summary>
        /// Maps incoming requests with the specified path to the provided connection pipeline.
        /// </summary>
        /// <param name="builder">The <see cref="IEndpointRouteBuilder"/></param>
        /// <param name="pattern">The request path.</param>
        /// <param name="configure">A callback to configure the connection.</param>
        public static IEndpointConventionBuilder MapConnections(this IEndpointRouteBuilder builder, string pattern, Action<IConnectionBuilder> configure) =>
            builder.MapConnections(pattern, new HttpConnectionDispatcherOptions(), configure);

        /// <summary>
        /// Maps incoming requests with the specified path to the provided connection pipeline.
        /// </summary>
        /// <typeparam name="TConnectionHandler">The <see cref="ConnectionHandler"/> type.</typeparam>
        /// <param name="builder">The <see cref="IEndpointRouteBuilder"/></param>
        /// <param name="pattern">The request path.</param>
        public static IEndpointConventionBuilder MapConnectionHandler<TConnectionHandler>(this IEndpointRouteBuilder builder, string pattern) where TConnectionHandler : ConnectionHandler
        {
            return builder.MapConnectionHandler<TConnectionHandler>(pattern, configureOptions: null);
        }

        /// <summary>
        /// Maps incoming requests with the specified path to the provided connection pipeline.
        /// </summary>
        /// <typeparam name="TConnectionHandler">The <see cref="ConnectionHandler"/> type.</typeparam>
        /// <param name="builder">The <see cref="IEndpointRouteBuilder"/></param>
        /// <param name="pattern">The request path.</param>
        /// <param name="configureOptions">A callback to configure dispatcher options.</param>
        public static IEndpointConventionBuilder MapConnectionHandler<TConnectionHandler>(this IEndpointRouteBuilder builder, string pattern, Action<HttpConnectionDispatcherOptions> configureOptions) where TConnectionHandler : ConnectionHandler
        {
            var authorizeAttributes = typeof(TConnectionHandler).GetCustomAttributes<AuthorizeAttribute>(inherit: true);
            var options = new HttpConnectionDispatcherOptions();
            foreach (var attribute in authorizeAttributes)
            {
                options.AuthorizationData.Add(attribute);
            }
            configureOptions?.Invoke(options);

            return builder.MapConnections(pattern, options, b =>
            {
                b.UseConnectionHandler<TConnectionHandler>();
            });
        }


        /// <summary>
        /// Maps incoming requests with the specified path to the provided connection pipeline.
        /// </summary>
        /// <param name="builder">The <see cref="IEndpointRouteBuilder"/></param>
        /// <param name="pattern">The request path.</param>
        /// <param name="options">Options used to configure the connection.</param>
        /// <param name="configure">A callback to configure the connection.</param>
        public static IEndpointConventionBuilder MapConnections(this IEndpointRouteBuilder builder, string pattern, HttpConnectionDispatcherOptions options, Action<IConnectionBuilder> configure)
        {
            var dispatcher = builder.ServiceProvider.GetRequiredService<HttpConnectionDispatcher>();

            var connectionBuilder = new ConnectionBuilder(builder.ServiceProvider);
            configure(connectionBuilder);
            var connectionDelegate = connectionBuilder.Build();

            // REVIEW: Consider expanding the internals of the dispatcher as endpoint routes instead of
            // using if statemants we can let the matcher handle

            var conventionBuilders = new List<IEndpointConventionBuilder>();

            // Build the negotiate application
            var app = builder.CreateApplicationBuilder();
            app.UseWebSockets();
            app.Run(c => dispatcher.ExecuteNegotiateAsync(c, options));
            var negotiateHandler = app.Build();

            var negotiateBuilder = builder.Map(pattern + "/negotiate", negotiateHandler);
            conventionBuilders.Add(negotiateBuilder);

            // build the execute handler part of the protocol
            app = builder.CreateApplicationBuilder();
            app.UseWebSockets();
            app.Run(c => dispatcher.ExecuteAsync(c, options, connectionDelegate));
            var executehandler = app.Build();

            var executeBuilder = builder.Map(pattern, executehandler);
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

            return compositeConventionBuilder;
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
