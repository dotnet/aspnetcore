// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Components.Sections
{
    public sealed class SectionOutlet : IComponent, IDisposable
    {
        private static readonly RenderFragment _emptyRenderFragment = _ => { };

        private string? _subscribedName = default;
        private SectionRegistry _registry = default!;
        private Action<RenderFragment?> _onChangeCallback = default!;

        [Parameter] public string Name { get; set; } = default!;
        [Parameter] public RenderFragment? ChildContent { get; set; } = default;

        void IComponent.Attach(RenderHandle renderHandle)
        {
            _onChangeCallback = content => renderHandle.Render(content ?? ChildContent ?? _emptyRenderFragment);
            _registry = SectionRegistry.GetRegistry(renderHandle);
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
                    _registry.Unsubscribe(_subscribedName, _onChangeCallback);
                }

                _registry.Subscribe(Name, _onChangeCallback);
                _subscribedName = Name;
            }

            _onChangeCallback(null);

            return Task.CompletedTask;
        }

        public void Dispose()
        {
            if (_subscribedName is not null)
            {
                _registry.Unsubscribe(_subscribedName, _onChangeCallback);
            }

            GC.SuppressFinalize(this);
        }
    }
}
