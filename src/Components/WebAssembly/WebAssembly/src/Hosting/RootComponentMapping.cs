// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics.CodeAnalysis;
using static Microsoft.AspNetCore.Internal.LinkerFlags;

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
        public RootComponentMapping([DynamicallyAccessedMembers(Component)] Type componentType, string selector)
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
            AppendContent = false;
        }

        /// <summary>
        /// Creates a new instance of <see cref="RootComponentMapping"/> with the provided <paramref name="componentType"/>
        /// and <paramref name="selector"/>.
        /// </summary>
        /// <param name="componentType">The component type. Must implement <see cref="IComponent"/>.</param>
        /// <param name="selector">The DOM element selector or registration id for the component.</param>
        /// <param name="parameters">The parameters to pass to the component.</param>
        public RootComponentMapping([DynamicallyAccessedMembers(Component)] Type componentType, string selector, ParameterView parameters)
            : this(componentType, selector)
        {
            Parameters = parameters;
        }

        /// <summary>
        /// Creates a new instance of <see cref="RootComponentMapping"/> with the provided <paramref name="componentType"/>
        /// and <paramref name="selector"/>.
        /// </summary>
        /// <param name="componentType">The component type. Must implement <see cref="IComponent"/>.</param>
        /// <param name="selector">The DOM element selector or registration id for the component.</param>
        /// <param name="parameters">The parameters to pass to the component.</param>
        /// <param name="appendContent">
        /// If <c>true</c>, the child content of the root component will be appended to existing HTML content.
        /// This is useful when treating the HTML <c>&lt;head&gt;</c> as a root component.
        /// </param>
        public RootComponentMapping([DynamicallyAccessedMembers(Component)] Type componentType, string selector, ParameterView parameters, bool appendContent)
            : this(componentType, selector, parameters)
        {
            AppendContent = appendContent;
        }

        /// <summary>
        /// Gets the component type.
        /// </summary>
        [DynamicallyAccessedMembers(Component)]
        public Type ComponentType { get; }

        /// <summary>
        /// Gets the DOM element selector.
        /// </summary>
        public string Selector { get; }

        /// <summary>
        /// Gets the parameters to pass to the root component.
        /// </summary>
        public ParameterView Parameters { get; }

        /// <summary>
        /// Gets whether the child content of the root component will be appended to existing HTML content.
        /// This is useful when treating the HTML <c>&lt;head&gt;</c> as a root component.
        /// </summary>
        public bool AppendContent { get; }
    }
}
