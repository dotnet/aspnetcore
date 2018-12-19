// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Blazor.Rendering;
using Microsoft.AspNetCore.Components.Builder;

namespace Microsoft.AspNetCore.Blazor.Hosting
{
    internal class WebAssemblyBlazorApplicationBuilder : IBlazorApplicationBuilder
    {
        public WebAssemblyBlazorApplicationBuilder(IServiceProvider services)
        {
            Entries = new List<(Type componentType, string domElementSelector)>();
            Services = services;
        }

        public List<(Type componentType, string domElementSelector)> Entries { get; }

        public IServiceProvider Services { get; }

        public void AddComponent(Type componentType, string domElementSelector)
        {
            if (componentType == null)
            {
                throw new ArgumentNullException(nameof(componentType));
            }

            if (domElementSelector == null)
            {
                throw new ArgumentNullException(nameof(domElementSelector));
            }

            Entries.Add((componentType, domElementSelector));
        }

        public WebAssemblyRenderer CreateRenderer()
        {
            var renderer = new WebAssemblyRenderer(Services);
            for (var i = 0; i < Entries.Count; i++)
            {
                var entry = Entries[i];
                renderer.AddComponent(entry.componentType, entry.domElementSelector);
            }

            return renderer;
        }
    }
}
