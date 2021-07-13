// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using static Microsoft.AspNetCore.Internal.LinkerFlags;

namespace Microsoft.AspNetCore.Components.RenderTree
{
    /// <summary>
    /// A <see cref="Renderer"/> that attaches its components to a browser DOM.
    /// </summary>
    public abstract class WebRenderer : Renderer
    {
        private readonly IServiceProvider _serviceProvider;

        /// <summary>
        /// Constructs an instance of <see cref="WebRenderer"/>.
        /// </summary>
        /// <param name="serviceProvider">The <see cref="IServiceProvider"/> to be used when initializing components.</param>
        /// <param name="loggerFactory">The <see cref="ILoggerFactory"/>.</param>
        public WebRenderer(IServiceProvider serviceProvider, ILoggerFactory loggerFactory)
            : base(serviceProvider, loggerFactory)
        {
            _serviceProvider = serviceProvider;
        }

        /// <summary>
        /// Instantiates a root component and attaches it to the browser within the specified element.
        /// </summary>
        /// <param name="componentType">The type of the component.</param>
        /// <param name="domElementSelector">A CSS selector that uniquely identifies a DOM element.</param>
        /// <returns>The new component ID.</returns>
        public int AddRootComponent([DynamicallyAccessedMembers(Component)] Type componentType, string domElementSelector)
        {
            var component = InstantiateComponent(componentType);
            var componentId = AssignRootComponentId(component);
            AttachRootComponentToBrowser(componentId, domElementSelector);
            return componentId;
        }

        /// <summary>
        /// Called by the framework to give a location for the specified root component in the browser DOM.
        /// </summary>
        /// <param name="componentId">The component ID.</param>
        /// <param name="domElementSelector">A CSS selector that uniquely identifies a DOM element.</param>
        protected abstract void AttachRootComponentToBrowser(int componentId, string domElementSelector);
    }
}
