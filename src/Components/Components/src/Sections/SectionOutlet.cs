// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Components.Sections
{
    public sealed class SectionOutlet : ISectionContentSubscriber, IComponent, IDisposable
    {
        private static readonly RenderFragment _emptyRenderFragment = _ => { };

        private string? _subscribedName = default;
        private RenderHandle _renderHandle = default;
        private SectionRegistry _registry = default!;

        [Parameter] public string Name { get; set; } = default!;
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
