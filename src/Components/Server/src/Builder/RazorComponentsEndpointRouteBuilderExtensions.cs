// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.AspNetCore.Routing;

namespace Microsoft.AspNetCore.Builder
{
    /// <summary>
    /// Extensions for <see cref="RazorComponentsEndpointBuilder"/>
    /// </summary>
    public static class RazorComponentsEndpointRouteBuilderExtensions
    {
        /// <summary>
        /// Maps the SignalR Components Hub to the path <see cref="ComponentsHub.DefaultPath"/> and associates
        /// the component <typeparamref name="TComponent"/> to this hub instance.
        /// The DOM selector for <typeparamref name="TComponent"/> is the type name in lowercase.
        /// </summary>
        /// <typeparam name="TComponent">The first <see cref="IComponent"/> associated with this <see cref="ComponentsHub"/>.</typeparam>
        /// <param name="routes">The <see cref="RouteBuilder"/>.</param>
        /// <returns>The <see cref="RazorComponentsEndpointBuilder"/>.</returns>
        public static RazorComponentsEndpointBuilder MapComponentsHub<TComponent>(
            this IEndpointRouteBuilder routes)
        {
            if (routes == null)
            {
                throw new ArgumentNullException(nameof(routes));
            }

            return routes.MapComponentsHub(typeof(TComponent));
        }


        // We need to choose between exposing an overload to pass in the path where the hub gets mapped or
        // the selector to use as they are both stringly typed.

        /// <summary>
        /// Maps the SignalR Components Hub to the path <paramref name="path"/> and associates
        /// the component <typeparamref name="TComponent"/> to this hub instance.
        /// The DOM selector for <typeparamref name="TComponent"/> is the type name in lowercase.
        /// </summary>
        /// <typeparam name="TComponent">The first <see cref="IComponent"/> associated with this <see cref="ComponentsHub"/>.</typeparam>
        /// <param name="routes">The <see cref="RouteBuilder"/>.</param>
        /// <param name="path">The path to map to which the <see cref="ComponentsHub"/> will be mapped.</param>
        /// <returns>The <see cref="RazorComponentsEndpointBuilder"/>.</returns>
        public static RazorComponentsEndpointBuilder MapComponentsHub<TComponent>(
            this IEndpointRouteBuilder routes,
            string path)
        {
            if (routes == null)
            {
                throw new ArgumentNullException(nameof(routes));
            }

            if (path == null)
            {
                throw new ArgumentNullException(nameof(path));
            }

            return routes.MapComponentsHub(path, typeof(TComponent));
        }

        /// <summary>
        /// Maps the SignalR Components Hub to the path <paramref name="path"/> and associates
        /// the component <typeparamref name="TComponent"/> to this hub instance as the given DOM <paramref name="selector"/>.
        /// </summary>
        /// <typeparam name="TComponent">The first <see cref="IComponent"/> associated with this <see cref="ComponentsHub"/>.</typeparam>
        /// <param name="routes">The <see cref="RouteBuilder"/>.</param>
        /// <param name="path">The path to map to which the <see cref="ComponentsHub"/> will be mapped.</param>
        /// <param name="selector">The selector for the <typeparamref name="TComponent"/>.</param>
        /// <returns>The <see cref="RazorComponentsEndpointBuilder"/>.</returns>
        public static RazorComponentsEndpointBuilder MapComponentsHub<TComponent>(
            this IEndpointRouteBuilder routes,
            string path,
            string selector)
        {
            if (routes == null)
            {
                throw new ArgumentNullException(nameof(routes));
            }

            if (path == null)
            {
                throw new ArgumentNullException(nameof(path));
            }

            if (selector == null)
            {
                throw new ArgumentNullException(nameof(selector));
            }

            return routes.MapComponentsHub(path, typeof(TComponent), selector);
        }

        /// <summary>
        /// Maps the SignalR Components Hub to the path <see cref="ComponentsHub.DefaultPath"/> and associates
        /// the component <paramref name="componentType"/> to this hub instance.
        /// The DOM selector for <paramref name="componentType"/> is the type name in lowercase.
        /// </summary>
        /// <param name="routes">The <see cref="RouteBuilder"/>.</param>
        /// <param name="componentType">The first <see cref="IComponent"/> associated with this <see cref="ComponentsHub"/>.</param>
        /// <returns>The <see cref="RazorComponentsEndpointBuilder"/>.</returns>
        public static RazorComponentsEndpointBuilder MapComponentsHub(
            this IEndpointRouteBuilder routes,
            Type componentType)
        {
            if (routes == null)
            {
                throw new ArgumentNullException(nameof(routes));
            }

            if (componentType == null)
            {
                throw new ArgumentNullException(nameof(componentType));
            }

            return routes.MapComponentsHub(ComponentsHub.DefaultPath, componentType);
        }

        /// <summary>
        /// Maps the SignalR Components Hub to the path <paramref name="path"/> and associates
        /// the component <paramref name="componentType"/> to this hub instance. The DOM selector for <paramref name="componentType"/>
        /// is the type name in lowercase.
        /// The DOM selector for <paramref name="componentType"/> is the type name in lowercase.
        /// </summary>
        /// <param name="routes">The <see cref="RouteBuilder"/>.</param>
        /// <param name="path">The path to map to which the <see cref="ComponentsHub"/> will be mapped.</param>
        /// <param name="componentType">The first <see cref="IComponent"/> associated with this <see cref="ComponentsHub"/>.</param>
        /// <returns>The <see cref="RazorComponentsEndpointBuilder"/>.</returns>
        public static RazorComponentsEndpointBuilder MapComponentsHub(
            this IEndpointRouteBuilder routes,
            string path,
            Type componentType)
        {
            if (routes == null)
            {
                throw new ArgumentNullException(nameof(routes));
            }

            if (path == null)
            {
                throw new ArgumentNullException(nameof(path));
            }

            if (componentType == null)
            {
                throw new ArgumentNullException(nameof(componentType));
            }

            return routes.MapComponentsHub(path, componentType, componentType.Name.ToLowerInvariant());
        }

        /// <summary>
        /// Maps the SignalR Components Hub to the path <paramref name="path"/> and associates
        /// the component <paramref name="componentType"/> to this hub instance as the given DOM <paramref name="selector"/>.
        /// </summary>
        /// <param name="routes">The <see cref="RouteBuilder"/>.</param>
        /// <param name="path">The path to map to which the <see cref="ComponentsHub"/> will be mapped.</param>
        /// <param name="componentType">The first <see cref="IComponent"/> associated with this <see cref="ComponentsHub"/>.</param>
        /// <param name="selector">The selector for the <paramref name="componentType"/>.</param>
        /// <returns>The <see cref="RazorComponentsEndpointBuilder"/>.</returns>
        public static RazorComponentsEndpointBuilder MapComponentsHub(
            this IEndpointRouteBuilder routes,
            string path,
            Type componentType,
            string selector)
        {
            if (routes == null)
            {
                throw new ArgumentNullException(nameof(routes));
            }

            if (path == null)
            {
                throw new ArgumentNullException(nameof(path));
            }

            if (componentType == null)
            {
                throw new ArgumentNullException(nameof(componentType));
            }

            if (selector == null)
            {
                throw new ArgumentNullException(nameof(selector));
            }

            return new RazorComponentsEndpointBuilder(routes.MapHub<ComponentsHub>(path)).AddComponent(componentType, selector);
        }
    }
}
