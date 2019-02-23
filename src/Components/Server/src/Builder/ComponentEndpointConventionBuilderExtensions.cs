// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Components.Server;

namespace Microsoft.AspNetCore.Builder
{
    /// <summary>
    /// Extensions for <see cref="IEndpointConventionBuilder"/>.
    /// </summary>
    public static class ComponentEndpointConventionBuilderExtensions
    {
        /// <summary>
        /// Adds <typeparamref name="TComponent"/> to the list of components registered with this <see cref="ComponentHub"/> instance.
        /// </summary>
        /// <typeparam name="TComponent">The component type.</typeparam>
        /// <param name="builder">The <see cref="IEndpointConventionBuilder"/>.</param>
        /// <param name="selector">A CSS selector that identifies the DOM element into which the <typeparamref name="TComponent"/> will be placed.</param>
        /// <returns>The <paramref name="builder"/>.</returns>
        public static IEndpointConventionBuilder AddComponent<TComponent>(this IEndpointConventionBuilder builder, string selector)
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
        /// The selector will default to the component name in lowercase.
        /// </summary>
        /// <param name="builder">The <see cref="IEndpointConventionBuilder"/>.</param>
        /// <param name="componentType">The component type.</param>
        /// <param name="selector">The component selector in the DOM for the <paramref name="componentType"/>.</param>
        /// <returns>The <paramref name="builder"/>.</returns>
        public static IEndpointConventionBuilder AddComponent(this IEndpointConventionBuilder builder, Type componentType, string selector)
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
