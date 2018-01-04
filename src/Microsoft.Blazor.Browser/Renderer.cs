// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Blazor.Browser.Interop;
using Microsoft.Blazor.Components;
using Microsoft.Blazor.UITree;

namespace Microsoft.Blazor.Browser
{
    public static class Renderer
    {
        public static void Render(IComponent component, string elementSelector)
        {
            var builder = new UITreeBuilder();
            component.Render(builder);

            var tree = builder.GetNodes();
            RegisteredFunction.InvokeUnmarshalled<string, UITreeNode[], int, object>(
                "_blazorRender", elementSelector, tree.Array, tree.Count);
        }
    }
}
