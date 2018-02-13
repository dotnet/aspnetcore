// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Blazor.RenderTree;

namespace Microsoft.AspNetCore.Blazor.Components
{
    /// <summary>
    /// Interface implemented by components that receive notification of their events.
    /// </summary>
    public interface IHandleEvent
    {
        /// <summary>
        /// Notifies the component that one of its event handlers has been triggered.
        /// </summary>
        /// <param name="handler">The event handler.</param>
        /// <param name="args">Arguments for the event handler.</param>
        void HandleEvent(UIEventHandler handler, UIEventArgs args);
    }
}
