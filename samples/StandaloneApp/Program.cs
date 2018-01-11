// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Blazor.Browser.Rendering;
using Microsoft.Blazor.Components;
using Microsoft.Blazor.RenderTree;

namespace StandaloneApp
{
    public class Program
    {
        public static void Main(string[] args)
        {
            new BrowserRenderer()
                .AddComponent("app", new PlaceholderComponent());
        }

        private class PlaceholderComponent : IComponent
        {
            public void BuildRenderTree(RenderTreeBuilder builder)
            {
                builder.AddText("Hello from the placeholder component.");
            }
        }
    }
}
