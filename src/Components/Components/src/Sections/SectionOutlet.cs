// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Components.Sections
{
    /// <summary>
    /// Renders content provided by <see cref="SectionContent"/> components with matching <c>Name</c>s.
    /// </summary>
    public sealed class SectionOutlet : ISectionContentSubscriber, IComponent, IDisposable
    {
        private static readonly RenderFragment _emptyRenderFragment = _ => { };

        private string? _subscribedName;
        private RenderHandle _renderHandle;
        private SectionRegistry _registry = default!;

        /// <summary>
        /// Gets or sets the name that determines which <see cref="SectionContent"/> instances will provide
        /// content to this instance.
        /// </summary>
        [Parameter] public string Name { get; set; } = default!;

        /// <summary>
        /// The content to be rendered when no <see cref="SectionContent"/> instances are providing content.
        /// </summary>
        [Parameter] public RenderFragment? ChildContent { get; set; } = default;

        void IComponent.Attach(RenderHandle renderHandle)
        {
            _renderHandle = renderHandle;
            _registry = SectionRegistry.GetRegistry(_renderHandle);
        }

        Task IComponent.SetParametersAsync(ParameterView parameters)
        {
            parameters.SetParameterProperties(this);

            if (string.IsNullOrEmpty(Name))
            {
                throw new InvalidOperationException($"{GetType()} requires a non-empty string parameter '{nameof(Name)}'.");
            }

            if (Name != _subscribedName)
            {
                if (_subscribedName is not null)
                {
                    _registry.Unsubscribe(_subscribedName, this);
                }

                _registry.Subscribe(Name, this);
                _subscribedName = Name;
            }

            return Task.CompletedTask;
        }

        void ISectionContentSubscriber.ContentChanged(RenderFragment? content)
        {
            // Here, we guard against rendering after the renderer has been disposed.
            // This can occur after prerendering or when the page is refreshed.
            // In these cases, a no-op is preferred.
            if (_renderHandle.IsRendererDisposed)
            {
                return;
            }

            _renderHandle.Render(content ?? ChildContent ?? _emptyRenderFragment);
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            if (_subscribedName is not null)
            {
                _registry.Unsubscribe(_subscribedName, this);
            }

            GC.SuppressFinalize(this);
        }
    }
}
