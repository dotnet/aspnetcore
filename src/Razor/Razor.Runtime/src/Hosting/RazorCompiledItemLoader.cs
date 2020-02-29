// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Reflection;

namespace Microsoft.AspNetCore.Razor.Hosting
{
    /// <summary>
    /// A loader implementation that can load <see cref="RazorCompiledItem"/> objects from an
    /// <see cref="Assembly"/> using reflection.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Inherit from <see cref="RazorCompiledItemLoader"/> to customize the behavior when loading 
    /// <see cref="RazorCompiledItem"/> objects from an <see cref="Assembly"/>. The default implementations of methods
    /// defined by this class use reflection in a trivial way to load attributes from the assembly.
    /// </para>
    /// <para>
    /// Inheriting from <see cref="RazorCompiledItemLoader"/> is useful when an implementation needs to consider
    /// additional configuration or data outside of the <see cref="Assembly"/> being loaded.
    /// </para>
    /// <para>
    /// Subclasses of <see cref="RazorCompiledItemLoader"/> can return subclasses of <see cref="RazorCompiledItem"/>
    /// with additional data members by overriding <see cref="CreateItem(RazorCompiledItemAttribute)"/>.
    /// </para>
    /// </remarks>
    public class RazorCompiledItemLoader
    {
        /// <summary>
        /// Loads a list of <see cref="RazorCompiledItem"/> objects from the provided <see cref="Assembly"/>.
        /// </summary>
        /// <param name="assembly">The assembly to search.</param>
        /// <returns>A list of <see cref="RazorCompiledItem"/> objects.</returns>
        public virtual IReadOnlyList<RazorCompiledItem> LoadItems(Assembly assembly)
        {
            if (assembly == null)
            {
                throw new ArgumentNullException(nameof(assembly));
            }

            var items = new List<RazorCompiledItem>();
            foreach (var attribute in LoadAttributes(assembly))
            {
                items.Add(CreateItem(attribute));
            }

            return items;
        }

        /// <summary>
        /// Creates a <see cref="RazorCompiledItem"/> from a <see cref="RazorCompiledItemAttribute"/>.
        /// </summary>
        /// <param name="attribute">The <see cref="RazorCompiledItemAttribute"/>.</param>
        /// <returns>A <see cref="RazorCompiledItem"/> created from <paramref name="attribute"/>.</returns>
        protected virtual RazorCompiledItem CreateItem(RazorCompiledItemAttribute attribute)
        {
            if (attribute == null)
            {
                throw new ArgumentNullException(nameof(attribute));
            }

            return new DefaultRazorCompiledItem(attribute.Type, attribute.Kind, attribute.Identifier);
        }

        /// <summary>
        /// Retrieves the list of <see cref="RazorCompiledItemAttribute"/> attributes defined for the provided
        /// <see cref="Assembly"/>.
        /// </summary>
        /// <param name="assembly">The <see cref="Assembly"/> to search.</param>
        /// <returns>A list of <see cref="RazorCompiledItemAttribute"/> attributes.</returns>
        protected IEnumerable<RazorCompiledItemAttribute> LoadAttributes(Assembly assembly)
        {
            if (assembly == null)
            {
                throw new ArgumentNullException(nameof(assembly));
            }

            return assembly.GetCustomAttributes<RazorCompiledItemAttribute>();
        }
    }
}
