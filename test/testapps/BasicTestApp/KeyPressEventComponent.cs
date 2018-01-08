// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Blazor.Components;
using Microsoft.Blazor.UITree;
using System.Collections.Generic;

namespace BasicTestApp
{
    public class KeyPressEventComponent : IComponent
    {
        private List<string> keysPressed = new List<string>();

        public void BuildUITree(UITreeBuilder builder)
        {
            builder.AddText("Type here:");
            builder.OpenElement("input");
            builder.AddAttribute("onkeypress", OnKeyPressed);
            builder.CloseElement();

            builder.OpenElement("ul");
            foreach (var key in keysPressed)
            {
                builder.OpenElement("li");
                builder.AddText(key);
                builder.CloseElement();
            }
            builder.CloseElement();
        }

        private void OnKeyPressed(UIEventInfo eventInfo)
        {
            keysPressed.Add(((UIKeyboardEventInfo)eventInfo).Key);
        }
    }
}
