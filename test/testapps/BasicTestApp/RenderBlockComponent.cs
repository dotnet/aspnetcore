// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Blazor.Components;
using Microsoft.AspNetCore.Blazor.RenderTree;

namespace BasicTestApp
{
    public class RenderBlockComponent : IComponent, IHandleEvent
    {
        private RenderHandle _renderHandle;
        private bool _showRegion;

        // Important: Notice that the sequence numbers inside the fragment are higher
        // that the sequence numbers outside it. Without the region delimiter, the
        // differencer would think the following nodes had been removed, then the
        // region was inserted, followed by a new copy of the following nodes. That's
        // not as efficient and wouldn't preserve focus etc.
        private RenderFragment _exampleContent = builder =>
        {
            builder.OpenElement(100, "p");
            builder.AddAttribute(101, "name", "region-element");
            builder.AddAttribute(102, "style", "color: red");
            builder.AddContent(103, "This is from the region");
            builder.CloseElement();
        };

        public void Init(RenderHandle renderHandle)
            => _renderHandle = renderHandle;

        public void SetParameters(ParameterCollection parameters)
            => Render();

        public void HandleEvent(UIEventHandler handler, UIEventArgs args)
        {
            // TODO: Remove the necessity to implement IHandleEvent if you just want
            // the event handler to be called. Then call Render from inside the handler.
            handler(args);
            Render();
        }

        private void Render() => _renderHandle.Render(builder =>
        {
            builder.OpenElement(0, "div"); // Container so we can see that passing through regions is OK
            builder.OpenRegion(1);
            builder.AddContent(2, "Region will be toggled below ");

            if (_showRegion)
            {
                builder.OpenRegion(3);
                _exampleContent(builder);
                builder.CloseRegion();
            }

            builder.OpenElement(4, "button");
            builder.AddAttribute(5, "onclick", ToggleRegion);
            builder.AddContent(6, "Toggle");
            builder.CloseElement();

            builder.CloseRegion();
            builder.OpenElement(7, "p");
            builder.AddContent(8, "The end");
            builder.CloseElement();
            builder.CloseElement();
        });

        private void ToggleRegion(UIEventArgs eventArgs)
            => _showRegion = !_showRegion;
    }
}
