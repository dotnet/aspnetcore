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
    // See: src/Microsoft.AspNetCore.Blazor.Razor.Extensions/BlazorApi.cs

    // Most of the developer-facing component lifecycle concepts are encapsulated in this
    // base class. The core Blazor rendering system doesn't know about them (it only knows
    // about IComponent). This gives us flexibility to change the lifecycle concepts easily,
    // or for developers to design their own lifecycles as different base classes.

    // TODO: When the component lifecycle design stabilises, add proper unit tests for BlazorComponent.

    /// <summary>
    /// Optional base class for Blazor components. Alternatively, Blazor components may
    /// implement <see cref="IComponent"/> directly.
    /// </summary>
    public abstract class BlazorComponent : IComponent, IHandleEvent
    {
        public const string BuildRenderTreeMethodName = nameof(BuildRenderTree);

        private readonly RenderFragment _renderFragment;
        private RenderHandle _renderHandle;
        private bool _hasCalledInit;
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
        /// Method invoked when the component is ready to start, having received its
        /// initial parameters from its parent in the render tree.
        /// </summary>
        protected virtual void OnInit()
        {
        }

        /// <summary>
        /// Method invoked when the component is ready to start, having received its
        /// initial parameters from its parent in the render tree.
        /// 
        /// Override this method if you will perform an asynchronous operation and
        /// want the component to refresh when that operation is completed.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing any asynchronous operation, or <see langword="null"/>.</returns>
        protected virtual Task OnInitAsync()
            => null;

        /// <summary>
        /// Method invoked when the component has received parameters from its parent in
        /// the render tree, and the incoming values have been assigned to properties.
        /// </summary>
        protected virtual void OnParametersSet()
        {
        }

        /// <summary>
        /// Method invoked when the component has received parameters from its parent in
        /// the render tree, and the incoming values have been assigned to properties.
        /// </summary>
        protected virtual Task OnParametersSetAsync()
            => null;

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
            if (_renderHandle.IsInitialized)
            {
                throw new InvalidOperationException($"The render handle is already set. Cannot initialize a {nameof(BlazorComponent)} more than once.");
            }

            _renderHandle = renderHandle;
        }
        
        /// <summary>
        /// Method invoked to apply initial or updated parameters to the component.
        /// </summary>
        /// <param name="parameters">The parameters to apply.</param>
        public virtual void SetParameters(ParameterCollection parameters)
        {
            parameters.AssignToProperties(this);

            if (!_hasCalledInit)
            {
                _hasCalledInit = true;
                OnInit();

                // If you override OnInitAsync and return a nonnull task, then by default
                // we automatically re-render once that task completes.
                OnInitAsync()?.ContinueWith(ContinueAfterLifecycleTask);
            }

            OnParametersSet();
            OnParametersSetAsync()?.ContinueWith(ContinueAfterLifecycleTask);

            StateHasChanged();
        }

        private void ContinueAfterLifecycleTask(Task task)
        {
            if (task.Exception == null)
            {
                StateHasChanged();
            }
            else
            {
                HandleException(task.Exception);
            }
        }

        private void HandleException(Exception ex)
        {
            if (ex is AggregateException && ex.InnerException != null)
            {
                ex = ex.InnerException; // It's more useful
            }

            // TODO: Need better global exception handling
            Console.Error.WriteLine($"[{ex.GetType().FullName}] {ex.Message}\n{ex.StackTrace}");
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
        /// Applies two-way data binding between the element and the property.
        /// </summary>
        /// <param name="value">The model property to be bound to the element.</param>
        protected RenderTreeFrame bind(object value)
            => throw new NotImplementedException($"{nameof(bind)} is a compile-time symbol only and should not be invoked.");

        /// <summary>
        /// Applies two-way data binding between the element and the property.
        /// </summary>
        /// <param name="value">The model property to be bound to the element.</param>
        protected RenderTreeFrame bind(DateTime value, string format)
            => throw new NotImplementedException($"{nameof(bind)} is a compile-time symbol only and should not be invoked.");

        /// <summary>
        /// Handles click events by invoking <paramref name="handler"/>.
        /// </summary>
        /// <param name="handler">The handler to be invoked when the event occurs.</param>
        /// <returns>A <see cref="RenderTreeFrame"/> that represents the event handler.</returns>
        protected RenderTreeFrame onclick(Action handler)
            // Note that the 'sequence' value is updated later when inserted into the tree
            => RenderTreeFrame.Attribute(0, "onclick", _ => handler());

        /// <summary>
        /// Handles change events by invoking <paramref name="handler"/>.
        /// </summary>
        /// <param name="handler">The handler to be invoked when the event occurs. The handler will receive the new value as a parameter.</param>
        /// <returns>A <see cref="RenderTreeFrame"/> that represents the event handler.</returns>
        protected RenderTreeFrame onchange(Action<object> handler)
            // Note that the 'sequence' value is updated later when inserted into the tree
            => RenderTreeFrame.Attribute(0, "onchange", args => handler(((UIChangeEventArgs)args).Value));
    }
}
