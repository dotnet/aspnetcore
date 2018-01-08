// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Blazor.Components;
using Microsoft.Blazor.UITree;

namespace BasicTestApp
{
    public class ParentChildComponent : IComponent
    {
        public void BuildUITree(UITreeBuilder builder)
        {
            builder.OpenElement("fieldset");
            builder.OpenElement("legend");
            builder.AddText("Parent component");
            builder.CloseElement();
            builder.AddComponent<ChildComponent>();
            builder.CloseElement();
        }

        private class ChildComponent : IComponent
        {
            public void BuildUITree(UITreeBuilder builder)
            {
                builder.AddText("Child component");
            }
        }
    }
}
