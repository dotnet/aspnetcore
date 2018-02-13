// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Blazor.RenderTree;
using System;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Blazor.Components
{
    /// <summary>
    /// Optional base class for Blazor components. Alternatively, Blazor components may
    /// implement <see cref="IComponent"/> directly.
    /// </summary>
    public abstract class BlazorComponent : IComponent
    {
        private RenderHandle _renderHandle;

        void IComponent.Init(RenderHandle renderHandle)
        {
            // This implicitly means a BlazorComponent can only be associated with a single
            // renderer. That's the only use case we have right now. If there was ever a need,
            // a component could hold a collection of render handles.
            if (_renderHandle.IsInitalised)
            {
                throw new InvalidOperationException($"The render handle is already set. Cannot initialize a {nameof(BlazorComponent)} more than once.");
            }

            _renderHandle = renderHandle;
        }

        void IComponent.SetParameters(ParameterCollection parameters)
        {
            parameters.AssignToProperties(this);

            // TODO: If we know conclusively that the parameters have not changed since last
            // time (because they are all primitives and equal to the existing property values)
            // then don't re-render. Can put an "out bool" parameter on AssignToProperties.
            _renderHandle.Render();
        }

        /// <inheritdoc />
        public virtual void BuildRenderTree(RenderTreeBuilder builder)
        {
            // This is virtual rather than abstract so that 'code behind' classes don't have to
            // be marked abstract.
            // Developers can either override this method in derived classes, or can use Razor
            // syntax to define a derived class and have the compiler generate the method.
        }

        // At present, if you have a .cshtml file in a project with <Project Sdk="Microsoft.NET.Sdk.Web">,
        // Visual Studio will run design-time builds for it, codegenning a class that attempts to override
        // this method. Therefore the virtual method must be defined, even though it won't be used at runtime,
        // because otherwise VS will display a design-time error in its 'Error List' pane.
        // TODO: Track down what triggers the design-time build for .cshtml files and how to stop it, then
        // this method can be removed.
        /// <summary>
        /// Not used. Do not invoke this method.
        /// </summary>
        /// <returns>Always throws an exception.</returns>
        public virtual Task ExecuteAsync()
            => throw new NotImplementedException($"Blazor components do not implement {nameof(ExecuteAsync)}.");

        /// <summary>
        /// Handles click events by invoking <paramref name="handler"/>.
        /// </summary>
        /// <param name="handler">The handler to be invoked when the event occurs.</param>
        /// <returns>A <see cref="RenderTreeFrame"/> that represents the event handler.</returns>
        protected RenderTreeFrame onclick(Action handler)
            // Note that the 'sequence' value is updated later when inserted into the tree
            => RenderTreeFrame.Attribute(0, "onclick", _ => handler());
    }
}
