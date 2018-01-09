// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Blazor.Components;
using Microsoft.Blazor.RenderTree;

namespace BasicTestApp
{
    public class CounterComponent : IComponent
    {
        private int currentCount = 0;

        public void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenElement("h1");
            builder.AddText("Counter");
            builder.CloseElement();

            builder.OpenElement("p");
            builder.AddText("Current count: ");
            builder.AddText(currentCount.ToString());
            builder.CloseElement();

            builder.OpenElement("button");
            builder.AddAttribute("onclick", OnButtonClicked);
            builder.AddText("Click me");
            builder.CloseElement();
        }

        private void OnButtonClicked(UIEventArgs eventInfo)
        {
            currentCount++;
        }
    }
}
