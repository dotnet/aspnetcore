// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Components.Builder
{
    /// <summary>
    /// Provides extension methods for <see cref="IComponentsApplicationBuilder"/>.
    /// </summary>
    public static class ComponentsApplicationBuilderExtensions
    {
        /// <summary>
        /// Associates the component type with the application,
        /// causing it to be displayed in the specified DOM element.
        /// </summary>
        /// <param name="app">The <see cref="IComponentsApplicationBuilder"/>.</param>
        /// <typeparam name="TComponent">The type of the component.</typeparam>
        /// <param name="domElementSelector">A CSS selector that uniquely identifies a DOM element.</param>
        public static void AddComponent<TComponent>(this IComponentsApplicationBuilder app, string domElementSelector)
            where TComponent : IComponent
        {
            app.AddComponent(typeof(TComponent), domElementSelector);
        }
    }
}
