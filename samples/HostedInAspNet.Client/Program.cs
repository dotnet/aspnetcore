// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Blazor.Browser;
using Microsoft.Blazor.Browser.Rendering;
using Microsoft.Blazor.Components;
using Microsoft.Blazor.RenderTree;

namespace HostedInAspNet.Client
{
    public class Program
    {
        static void Main(string[] args)
        {
            // Temporarily render this test component until there's a proper mechanism
            // for testing this.
            new BrowserRenderer().AddComponent("app", new MyComponent());
        }
    }

    internal class MyComponent : IComponent
    {
        public void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenElement("h1");
            builder.AddText("Hello from RenderTree");
            builder.CloseElement();

            builder.OpenElement("ul");

            builder.OpenElement("li");
            builder.AddText("First item");
            builder.CloseElement();

            builder.OpenElement("li");
            builder.AddText("Second item");
            builder.CloseElement();

            builder.CloseElement();
        }
    }
}
