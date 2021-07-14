// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using static Microsoft.AspNetCore.Internal.LinkerFlags;

namespace Microsoft.AspNetCore.Components.WebAssembly.Hosting
{
    /// <summary>
    /// Defines a collection of <see cref="RootComponentMapping"/> items.
    /// </summary>
    public class RootComponentMappingCollection : Collection<RootComponentMapping>
    {
        /// <summary>
        /// Adds a component mapping to the collection.
        /// </summary>
        /// <typeparam name="TComponent">The component type.</typeparam>
        /// <param name="selector">The DOM element selector.</param>
        public void Add<[DynamicallyAccessedMembers(Component)] TComponent>(string selector) where TComponent : IComponent
        {
            Add<TComponent>(selector, false);
        }

        /// <summary>
        /// Adds a component mapping to the collection.
        /// </summary>
        /// <typeparam name="TComponent">The component type.</typeparam>
        /// <param name="selector">The DOM element selector.</param>
        /// <param name="appendContent">
        /// If <c>true</c>, the child content of the root component will be appended to existing HTML content.
        /// This is useful when treating the HTML <c>&lt;head&gt;</c> as a root component.
        /// </param>
        public void Add<[DynamicallyAccessedMembers(Component)] TComponent>(string selector, bool appendContent) where TComponent : IComponent
        {
            if (selector is null)
            {
                throw new ArgumentNullException(nameof(selector));
            }

            Add(new RootComponentMapping(typeof(TComponent), selector, ParameterView.Empty, appendContent));
        }

        /// <summary>
        /// Adds a component mapping to the collection.
        /// </summary>
        /// <param name="componentType">The component type. Must implement <see cref="IComponent"/>.</param>
        /// <param name="selector">The DOM element selector.</param>
        public void Add([DynamicallyAccessedMembers(Component)] Type componentType, string selector)
        {
            Add(componentType, selector, ParameterView.Empty);
        }

        /// <summary>
        /// Adds a component mapping to the collection.
        /// </summary>
        /// <param name="componentType">The component type. Must implement <see cref="IComponent"/>.</param>
        /// <param name="selector">The DOM element selector.</param>
        /// <param name="parameters">The parameters to the root component.</param>
        public void Add([DynamicallyAccessedMembers(Component)] Type componentType, string selector, ParameterView parameters)
        {
            Add(componentType, selector, parameters, false);
        }

        /// <summary>
        /// Adds a component mapping to the collection.
        /// </summary>
        /// <param name="componentType">The component type. Must implement <see cref="IComponent"/>.</param>
        /// <param name="selector">The DOM element selector.</param>
        /// <param name="parameters">The parameters to the root component.</param>
        /// <param name="appendContent">
        /// If <c>true</c>, the child content of the root component will be appended to existing HTML content.
        /// This is useful when treating the HTML <c>&lt;head&gt;</c> as a root component.
        /// </param>
        public void Add([DynamicallyAccessedMembers(Component)] Type componentType, string selector, ParameterView parameters, bool appendContent)
        {
            if (componentType is null)
            {
                throw new ArgumentNullException(nameof(componentType));
            }

            if (selector is null)
            {
                throw new ArgumentNullException(nameof(selector));
            }

            Add(new RootComponentMapping(componentType, selector, parameters, appendContent));
        }

        /// <summary>
        /// Adds a collection of items to this collection.
        /// </summary>
        /// <param name="items">The items to add.</param>
        public void AddRange(IEnumerable<RootComponentMapping> items)
        {
            if (items is null)
            {
                throw new ArgumentNullException(nameof(items));
            }

            foreach (var item in items)
            {
                Add(item);
            }
        }
    }
}
