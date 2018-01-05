// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Blazor.Components;
using Microsoft.Blazor.UITree;

namespace BasicTestApp
{
    public class RedTextComponent : IComponent
    {
        public void BuildUITree(UITreeBuilder builder)
        {
            builder.OpenElement("h1");
            builder.AddAttribute("style", "color: red;");
            builder.AddAttribute("customattribute", "somevalue");
            builder.AddText("Hello, world!");
            builder.CloseElement();
        }
    }
}
