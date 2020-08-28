// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections;
using Microsoft.AspNetCore.Components;

namespace Microsoft.AspNetCore.Components.WebAssembly.Hosting
{
    /// <summary>
    /// Defines a mapping between a root <see cref="IComponent"/> and a DOM element selector.
    /// </summary>
    public readonly struct RootComponentMapping
    {
        /// <summary>
        /// Creates a new instance of <see cref="RootComponentMapping"/> with the provided <paramref name="componentType"/>
        /// and <paramref name="selector"/>.
        /// </summary>
        /// <param name="componentType">The component type. Must implement <see cref="IComponent"/>.</param>
        /// <param name="selector">The DOM element selector or component registration id for the component.</param>
        public RootComponentMapping(Type componentType, string selector)
        {
            if (componentType is null)
            {
                throw new ArgumentNullException(nameof(componentType));
            }

            if (!typeof(IComponent).IsAssignableFrom(componentType))
            {
                throw new ArgumentException(
                    $"The type '{componentType.Name}' must implement {nameof(IComponent)} to be used as a root component.",
                    nameof(componentType));
            }

            if (selector is null)
            {
                throw new ArgumentNullException(nameof(selector));
            }

            ComponentType = componentType;
            Selector = selector;
            Parameters = ParameterView.Empty;
        }

        /// <summary>
        /// Creates a new instance of <see cref="RootComponentMapping"/> with the provided <paramref name="componentType"/>
        /// and <paramref name="selector"/>.
        /// </summary>
        /// <param name="componentType">The component type. Must implement <see cref="IComponent"/>.</param>
        /// <param name="selector">The DOM element selector or registration id for the component.</param>
        /// <param name="parameters">The parameters to pass to the component.</param>
        public RootComponentMapping(Type componentType, string selector, ParameterView parameters) : this(componentType, selector)
        {
            Parameters = parameters;
        }

        /// <summary>
        /// Gets the component type.
        /// </summary>
        public Type ComponentType { get; }

        /// <summary>
        /// Gets the DOM element selector.
        /// </summary>
        public string Selector { get; }

        /// <summary>
        /// Gets the parameters to pass to the root component.
        /// </summary>
        public ParameterView Parameters { get; }
    }
}
