// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Components.Sections
{
    /// <summary>
    /// Provides content to <see cref="SectionOutlet"/> components with matching <c>Name</c>s.
    /// </summary>
    internal class SectionContent : ISectionContentProvider, IComponent, IDisposable
    {
        private string? _registeredName;
        private SectionRegistry _registry = default!;

        /// <summary>
        /// Gets or sets the name that determines which <see cref="SectionOutlet"/> instances will render
        /// the content of this instance.
        /// </summary>
        [Parameter] public string Name { get; set; } = default!;

        /// <summary>
        /// Gets or sets the content to be rendered in corresponding <see cref="SectionOutlet"/> instances.
        /// </summary>
        [Parameter] public RenderFragment? ChildContent { get; set; }

        RenderFragment? ISectionContentProvider.Content => ChildContent;

        void IComponent.Attach(RenderHandle renderHandle)
        {
            _registry = renderHandle.Dispatcher.SectionRegistry;
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

        /// <inheritdoc/>
        public void Dispose()
        {
            if (_registeredName is not null)
            {
                _registry.RemoveProvider(_registeredName, this);
            }
        }
    }
}
