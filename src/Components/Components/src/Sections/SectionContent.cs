// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Components.Sections
{
    public sealed class SectionContent : ISectionContentProvider, IComponent, IDisposable
    {
        private string? _registeredName = default;
        private SectionRegistry _registry = default!;

        [Parameter] public string Name { get; set; } = default!;
        [Parameter] public RenderFragment? ChildContent { get; set; } = default;

        RenderFragment? ISectionContentProvider.Content => ChildContent;

        void IComponent.Attach(RenderHandle renderHandle)
        {
            _registry = SectionRegistry.GetRegistry(renderHandle);
        }

        Task IComponent.SetParametersAsync(ParameterView parameters)
        {
            parameters.SetParameterProperties(this);

            if (string.IsNullOrEmpty(Name))
            {
                throw new InvalidOperationException($"{GetType()} requires a non-empty string parameter '{nameof(Name)}'.");
            }

            if (Name != _registeredName)
            {
                if (_registeredName is not null)
                {
                    _registry.RemoveProvider(_registeredName, this);
                }

                _registry.AddProvider(Name, this);
                _registeredName = Name;
            }

            _registry.NotifyContentChanged(Name, this);

            return Task.CompletedTask;
        }

        public void Dispose()
        {
            if (_registeredName is not null)
            {
                _registry.RemoveProvider(_registeredName, this);
            }

            GC.SuppressFinalize(this);
        }
    }
}
