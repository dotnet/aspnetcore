// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Components.WebView.WebView2
{
    /// <summary>
    /// Types in the Microsoft.AspNetCore.Components.WebView.WebView2 are not recommended for use outside
    /// of the Blazor framework. These types will change in a future release.
    ///
    /// Event args for the AcceleratorKeyPressed event.
    /// </summary>
    public interface ICoreWebView2AcceleratorKeyPressedEventArgsWrapper
    {
        /// <summary>
        /// Gets the Win32 virtual key code of the key that was pressed or released.
        /// </summary>
        uint VirtualKey { get; }

        /// <summary>
        /// Gets the LPARAM value that accompanied the window message.
        /// </summary>
        int KeyEventLParam { get; }

        /// <summary>
        /// Indicates whether the AcceleratorKeyPressed event is handled by host.
        /// </summary>
        /// <remarks>
        /// If set to <c>true</c> then this prevents the WebView from performing the default action for this accelerator key. Otherwise the WebView will perform the default action for the accelerator key.
        /// </remarks>
        bool Handled { get; set; }
    }
}
