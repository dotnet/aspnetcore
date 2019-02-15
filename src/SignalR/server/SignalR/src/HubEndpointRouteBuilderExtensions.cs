using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Builder;

namespace Microsoft.AspNetCore.Routing
{
    public static class HubEndpointRouteBuilderExtensions
    {
        /// <summary>
        /// Maps incoming requests with the specified path to the specified <see cref="Hub"/> type.
        /// </summary>
        /// <typeparam name="THub">The <see cref="Hub"/> type to map requests to.</typeparam>
        /// <param name="builder">The <see cref="IEndpointRouteBuilder"/></param>
        /// <param name="pattern">The request path.</param>
        public static IEndpointConventionBuilder MapHub<THub>(this IEndpointRouteBuilder builder, string pattern) where THub : Hub
        {
            return builder.MapHub<THub>(pattern, configureOptions: null);
        }

        /// <summary>
        /// Maps incoming requests with the specified path to the specified <see cref="Hub"/> type.
        /// </summary>
        /// <typeparam name="THub">The <see cref="Hub"/> type to map requests to.</typeparam>
        /// <param name="builder">The <see cref="IEndpointRouteBuilder"/></param>
        /// <param name="pattern">The request path.</param>
        /// <param name="configureOptions">A callback to configure dispatcher options.</param>
        public static IEndpointConventionBuilder MapHub<THub>(this IEndpointRouteBuilder builder, string pattern, Action<HttpConnectionDispatcherOptions> configureOptions) where THub : Hub
        {
            var marker = builder.ServiceProvider.GetService<SignalRMarkerService>();

            if (marker == null)
            {
                throw new InvalidOperationException("Unable to find the required services. Please add all the required services by calling " +
                                                    "'IServiceCollection.AddSignalR' inside the call to 'ConfigureServices(...)' in the application startup code.");
            }

            // find auth attributes
            var authorizeAttributes = typeof(THub).GetCustomAttributes<AuthorizeAttribute>(inherit: true);
            var options = new HttpConnectionDispatcherOptions();
            foreach (var attribute in authorizeAttributes)
            {
                options.AuthorizationData.Add(attribute);
            }
            configureOptions?.Invoke(options);

            return builder.MapConnections(pattern, options, b =>
            {
                b.UseHub<THub>();
            });
        }
    }
}
