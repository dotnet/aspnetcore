// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Components.Builder
{
    /// <summary>
    /// A builder for adding components to an application.
    /// </summary>
    public interface IComponentsApplicationBuilder
    {
        /// <summary>
        /// Gets the application services.
        /// </summary>
        IServiceProvider Services { get; }

        /// <summary>
        /// Associates the <see cref="IComponent"/> with the application,
        /// causing it to be displayed in the specified DOM element.
        /// </summary>
        /// <param name="componentType">The type of the component.</param>
        /// <param name="domElementSelector">A CSS selector that uniquely identifies a DOM element.</param>
        void AddComponent(Type componentType, string domElementSelector);
    }
}
