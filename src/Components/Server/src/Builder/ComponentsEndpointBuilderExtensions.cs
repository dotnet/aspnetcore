// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Components.Server;

namespace Microsoft.AspNetCore.Builder
{
    /// <summary>
    /// Extensions for <see cref="ComponentEndpointBuilder"/>.
    /// </summary>
    public static class ComponentEndpointbuilderExtensions
    {
        /// <summary>
        /// Adds <typeparamref name="TComponent"/> to the list of components registered with this <see cref="ComponentHub"/> instance.
        /// The DOM selector associated with the <typeparamref name="TComponent"/> will default to the component type name in lowercase.
        /// </summary>
        /// <typeparam name="TComponent">The component type.</typeparam>
        /// <param name="builder">The <see cref="ComponentEndpointBuilder"/>.</param>
        /// <returns>The <paramref name="builder"/>.</returns>
        public static ComponentEndpointBuilder AddComponent<TComponent>(this ComponentEndpointBuilder builder)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            return AddComponent(builder, typeof(TComponent), typeof(TComponent).Name.ToLowerInvariant());
        }

        /// <summary>
        /// Adds <typeparamref name="TComponent"/> to the list of components registered with this <see cref="ComponentHub"/> instance.
        /// </summary>
        /// <typeparam name="TComponent">The component type.</typeparam>
        /// <param name="builder">The <see cref="ComponentEndpointBuilder"/>.</param>
        /// <param name="selector">The component selector in the DOM for the <typeparamref name="TComponent"/>.</param>
        /// <returns>The <paramref name="builder"/>.</returns>
        public static ComponentEndpointBuilder AddComponent<TComponent>(this ComponentEndpointBuilder builder, string selector)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (selector == null)
            {
                throw new ArgumentNullException(nameof(selector));
            }

            return AddComponent(builder, typeof(TComponent), selector);
        }

        /// <summary>
        /// Adds <paramref name="componentType"/> to the list of components registered with this <see cref="ComponentHub"/> instance.
        /// The DOM selector associated with the <paramref name="componentType"/> will default to the component type name in lowercase.
        /// </summary>
        /// <param name="builder">The <see cref="ComponentEndpointBuilder"/>.</param>
        /// <param name="componentType">The component type.</param>
        /// <returns>The <paramref name="builder"/>.</returns>
        public static ComponentEndpointBuilder AddComponent(this ComponentEndpointBuilder builder, Type componentType)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (componentType == null)
            {
                throw new ArgumentNullException(nameof(componentType));
            }

            return builder.AddComponent(componentType, componentType.Name.ToLowerInvariant());
        }

        /// <summary>
        /// Adds <paramref name="componentType"/> to the list of components registered with this <see cref="ComponentHub"/> instance.
        /// The selector will default to the component name in lowercase.
        /// </summary>
        /// <param name="builder">The <see cref="ComponentEndpointBuilder"/>.</param>
        /// <param name="componentType">The component type.</param>
        /// <param name="selector">The component selector in the DOM for the <paramref name="componentType"/>.</param>
        /// <returns>The <paramref name="builder"/>.</returns>
        public static ComponentEndpointBuilder AddComponent(this ComponentEndpointBuilder builder, Type componentType, string selector)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (componentType == null)
            {
                throw new ArgumentNullException(nameof(componentType));
            }

            if (selector == null)
            {
                throw new ArgumentNullException(nameof(selector));
            }

            builder.Add(endpointBuilder => AddComponent(endpointBuilder.Metadata, componentType, selector));
            return builder;
        }

        private static void AddComponent(IList<object> metadata, Type type, string selector)
        {
            metadata.Add(new ComponentDescriptor
            {
                ComponentType = type,
                Selector = selector
            });
        }
    }
}
