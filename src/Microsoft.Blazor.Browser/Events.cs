// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Blazor.Browser.Interop;
using Microsoft.Blazor.UITree;
using System;

namespace Microsoft.Blazor.Browser
{
    // Invoked by the Microsoft.Blazor.Browser.JS code when a DOM event occurs
    internal static class Events
    {
        public static void RaiseEvent(string domComponentID, string uiTreeNodeIndex, string eventInfoType, string eventInfoJson)
        {
            // We're receiving the uiTreeNodeIndex as a string only because there's not
            // yet a way to pass ints (or construct boxed ones) from JS with the current Mono
            // runtime. When there's a supported way to do that, this can be simplified.
            var renderState = DOMComponentRenderState.FindByDOMComponentID(domComponentID);
            var eventInfo = ParseEventInfo(eventInfoType, eventInfoJson);
            renderState.RaiseEvent(int.Parse(uiTreeNodeIndex), eventInfo);
        }

        private static UIEventInfo ParseEventInfo(string eventInfoType, string eventInfoJson)
        {
            switch (eventInfoType)
            {
                case "mouse":
                    return Json.Deserialize<UIMouseEventInfo>(eventInfoJson);
                case "keyboard":
                    return Json.Deserialize<UIKeyboardEventInfo>(eventInfoJson);
                default:
                    throw new ArgumentException($"Unsupported value '{eventInfoType}'.", nameof(eventInfoType));
            }
        }
    }
}
