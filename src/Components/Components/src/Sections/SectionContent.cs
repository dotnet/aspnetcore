// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Components.Sections
{
    public sealed class SectionContent : IComponent, IDisposable
    {
        private SectionRegistry _registry = default!;

        [Parameter] public string Name { get; set; } = default!;
        [Parameter] public RenderFragment? ChildContent { get; set; } = default;

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

            _registry.SetContent(Name, ChildContent);

            return Task.CompletedTask;
        }

        public void Dispose()
        {
            // TODO: This relies on the assumption that the old SectionContent gets disposed before the
            // new one is added to the output. This won't be the case in all possible scenarios.
            // We should have the registry keep track of which SectionContent is the most recent
            // one to supply new content, and disregard updates from ones that were superseded.
            _registry.SetContent(Name, null);
        }
    }
}
