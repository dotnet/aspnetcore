// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.SignalR;

namespace Microsoft.AspNetCore.Builder
{
    /// <summary>
    /// Extensions for <see cref="IEndpointRouteBuilder"/>.
    /// </summary>
    public static class ComponentEndpointRouteBuilderExtensions
    {
        /// <summary>
        /// Maps the Blazor <see cref="Hub" /> to the default path.
        /// </summary>
        /// <param name="endpoints">The <see cref="IEndpointRouteBuilder"/>.</param>
        /// <returns>The <see cref="ComponentEndpointConventionBuilder"/>.</returns>
        public static ComponentEndpointConventionBuilder MapBlazorHub(this IEndpointRouteBuilder endpoints)
        {
            if (endpoints == null)
            {
                throw new ArgumentNullException(nameof(endpoints));
            }

            return endpoints.MapBlazorHub(ComponentHub.DefaultPath);
        }

        /// <summary>
        /// Maps the Blazor <see cref="Hub" /> to the path <paramref name="path"/>.
        /// </summary>
        /// <param name="endpoints">The <see cref="IEndpointRouteBuilder"/>.</param>
        /// <param name="path">The path to map the Blazor <see cref="Hub" />.</param>
        /// <returns>The <see cref="ComponentEndpointConventionBuilder"/>.</returns>
        public static ComponentEndpointConventionBuilder MapBlazorHub(
            this IEndpointRouteBuilder endpoints,
            string path)
        {
            if (endpoints == null)
            {
                throw new ArgumentNullException(nameof(endpoints));
            }

            if (path == null)
            {
                throw new ArgumentNullException(nameof(path));
            }

            return endpoints.MapBlazorHub(path, configureOptions: _ => { });
        }

        /// <summary>
        ///Maps the Blazor <see cref="Hub" /> to the default path.
        /// </summary>
        /// <param name="endpoints">The <see cref="IEndpointRouteBuilder"/>.</param>
        /// <param name="configureOptions">A callback to configure dispatcher options.</param>
        /// <returns>The <see cref="ComponentEndpointConventionBuilder"/>.</returns>
        public static ComponentEndpointConventionBuilder MapBlazorHub(
            this IEndpointRouteBuilder endpoints,
            Action<HttpConnectionDispatcherOptions> configureOptions)
        {
            if (endpoints == null)
            {
                throw new ArgumentNullException(nameof(endpoints));
            }

            if (configureOptions == null)
            {
                throw new ArgumentNullException(nameof(configureOptions));
            }

            return endpoints.MapBlazorHub(ComponentHub.DefaultPath, configureOptions);
        }

        /// <summary>
        /// Maps the Blazor <see cref="Hub" /> to the path <paramref name="path"/>.
        /// </summary>
        /// <param name="endpoints">The <see cref="IEndpointRouteBuilder"/>.</param>
        /// <param name="path">The path to map the Blazor <see cref="Hub" />.</param>
        /// <param name="configureOptions">A callback to configure dispatcher options.</param>
        /// <returns>The <see cref="ComponentEndpointConventionBuilder"/>.</returns>
        public static ComponentEndpointConventionBuilder MapBlazorHub(
            this IEndpointRouteBuilder endpoints,
            string path,
            Action<HttpConnectionDispatcherOptions> configureOptions)
        {
            if (endpoints == null)
            {
                throw new ArgumentNullException(nameof(endpoints));
            }

            if (path == null)
            {
                throw new ArgumentNullException(nameof(path));
            }

            if (configureOptions == null)
            {
                throw new ArgumentNullException(nameof(configureOptions));
            }

            var hubEndpoint = endpoints.MapHub<ComponentHub>(path, configureOptions);

            var disconnectEndpoint = endpoints.Map(
                (path.EndsWith("/") ? path : path + "/") + "disconnect/",
                endpoints.CreateApplicationBuilder().UseMiddleware<CircuitDisconnectMiddleware>().Build())
                .WithDisplayName("Blazor disconnect");

            return new ComponentEndpointConventionBuilder(hubEndpoint, disconnectEndpoint);
        }
    }
}
