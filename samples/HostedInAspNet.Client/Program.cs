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
            new BrowserRenderer().AddComponent<MyComponent>("app");
        }
    }

    internal class MyComponent : IComponent
    {
        public void Init(RenderHandle renderHandle)
        {
        }

        public void SetParameters(ParameterCollection parameters)
        {
        }
    }
}
