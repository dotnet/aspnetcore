// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Blazor.Browser;
using Microsoft.AspNetCore.Blazor.Browser.Rendering;
using Microsoft.AspNetCore.Blazor.Components;
using Microsoft.AspNetCore.Blazor.RenderTree;

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
            builder.OpenElement(0, "h1");
            builder.AddText(1, "Hello from RenderTree");
            builder.CloseElement();

            builder.OpenElement(2, "ul");

            builder.OpenElement(3, "li");
            builder.AddText(4, "First item");
            builder.CloseElement();

            builder.OpenElement(5, "li");
            builder.AddText(6, "Second item");
            builder.CloseElement();

            builder.CloseElement();
        }
    }
}
