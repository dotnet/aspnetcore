// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Microsoft.AspNetCore.Components.WebView.WindowsForms
{
    /// <summary>
    /// Provides a set of extension methods for modifying collections of <see cref="IComponent"/> objects.
    /// </summary>
    public static class RootComponentCollectionExtensions
    {
        /// <summary>
        /// Adds the component specified by <typeparamref name="TComponent"/> to the collection specified by
        /// <paramref name="components" /> to be associated with the selector specified by <paramref name="selector"/>
        /// and to be instantiated with the parameters specified by <paramref name="parameters"/>.
        /// </summary>
        /// <typeparam name="TComponent">The <see cref="IComponent"/> to add to the collection.</typeparam>
        /// <param name="components">The collection to which the component should be added.</param>
        /// <param name="selector">The selector to which the component will be associated.</param>
        /// <param name="parameters">The optional creation parameters for the component.</param>
        public static void Add<TComponent>(this ObservableCollection<RootComponent> components, string selector, IDictionary<string, object> parameters = null)
            where TComponent : IComponent
        {
            components.Add(new RootComponent(selector, typeof(TComponent), parameters));
        }

        /// <summary>
        /// Removes the component associated with the specified <paramref name="selector"/> from the collection
        /// specified by <paramref name="components" /> .
        /// </summary>
        /// <param name="components">The collection from which the component associated with the selector should be removed.</param>
        /// <param name="selector">The selector associated with the component to be removed.</param>
        public static void Remove(this ObservableCollection<RootComponent> components, string selector)
        {
            for (var i = 0; i < components.Count; i++)
            {
                if (components[i].Selector.Equals(selector, StringComparison.Ordinal))
                {
                    components.RemoveAt(i);
                    return;
                }
            }

            throw new ArgumentException($"There is no root component with selector '{selector}'.", nameof(selector));
        }
    }
}
