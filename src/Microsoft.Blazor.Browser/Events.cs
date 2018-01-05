// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.Blazor.Browser
{
    // Invoked by the Microsoft.Blazor.Browser.JS code when a DOM event occurs
    internal static class Events
    {
        public static void RaiseEvent(string domComponentID, string uiTreeNodeIndex)
        {
            // We're receiving the uiTreeNodeIndex as a string only because there's not
            // yet a way to pass ints (or construct boxed ones) from JS with the current Mono
            // runtime. When there's a supported way to do that, this can be simplified.
            var renderState = DOMComponentRenderState.FindByDOMComponentID(domComponentID);
            renderState.RaiseEvent(int.Parse(uiTreeNodeIndex));
        }
    }
}
