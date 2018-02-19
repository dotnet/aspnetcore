// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Blazor.RenderTree;
using System;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Blazor.Components
{
    // IMPORTANT
    //
    // Many of these names are used in code generation. Keep these in sync with the code generation code
    // See: src/Microsoft.AspNetCore.Blazor.Razor.Extensions/BlazorComponent.cs

    /// <summary>
    /// Optional base class for Blazor components. Alternatively, Blazor components may
    /// implement <see cref="IComponent"/> directly.
    /// </summary>
    public abstract class BlazorComponent : IComponent, IHandleEvent
    {
        public const string BuildRenderTreeMethodName = nameof(BuildRenderTree);

        private readonly RenderFragment _renderFragment;
        private RenderHandle _renderHandle;
        private bool _hasNeverRendered = true;
        private bool _hasPendingQueuedRender;

        public BlazorComponent()
        {
            _renderFragment = BuildRenderTree;
        }

        /// <summary>
        /// Renders the component to the supplied <see cref="RenderTreeBuilder"/>.
        /// </summary>
        /// <param name="builder">A <see cref="RenderTreeBuilder"/> that will receive the render output.</param>
        protected virtual void BuildRenderTree(RenderTreeBuilder builder)
        {
            // Developers can either override this method in derived classes, or can use Razor
            // syntax to define a derived class and have the compiler generate the method.
            _hasPendingQueuedRender = false;
            _hasNeverRendered = false;
        }

        /// <summary>
        /// Method invoked when the component has received parameters from its parent in
        /// the render tree, and the incoming values have been assigned to properties.
        /// </summary>
        protected virtual void OnParametersSet()
        {
        }

        /// <summary>
        /// Notifies the component that its state has changed. When applicable, this will
        /// cause the component to be re-rendered.
        /// </summary>
        protected void StateHasChanged()
        {
            if (_hasPendingQueuedRender)
            {
                return;
            }

            if (_hasNeverRendered || ShouldRender())
            {
                _hasPendingQueuedRender = true;
                _renderHandle.Render(_renderFragment);
            }
        }

        /// <summary>
        /// Returns a flag to indicate whether the component should render.
        /// </summary>
        /// <returns></returns>
        protected virtual bool ShouldRender()
            => true;

        void IComponent.Init(RenderHandle renderHandle)
        {
            // This implicitly means a BlazorComponent can only be associated with a single
            // renderer. That's the only use case we have right now. If there was ever a need,
            // a component could hold a collection of render handles.
            if (_renderHandle.IsInitalized)
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
            // then skip the following. Can put an "out bool" parameter on AssignToProperties.
            OnParametersSet();
            StateHasChanged();
        }

        void IHandleEvent.HandleEvent(UIEventHandler handler, UIEventArgs args)
        {
            handler(args);

            // After each event, we synchronously re-render (unless !ShouldRender())
            // This just saves the developer the trouble of putting "StateHasChanged();"
            // at the end of every event callback.
            StateHasChanged();
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
