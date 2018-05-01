// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Blazor
{
    /// <summary>
    /// Supplies information about an event that is being raised.
    /// </summary>
    public class UIEventArgs
    {
        /// <summary>
        /// Gets or sets the type of the event.
        /// </summary>
        public string Type { get; set; }
    }

    /// <summary>
    /// Supplies information about an input change event that is being raised.
    /// </summary>
    public class UIChangeEventArgs : UIEventArgs
    {
        /// <summary>
        /// Gets or sets the new value of the input. This may be a <see cref="string"/>
        /// or a <see cref="bool"/>.
        /// </summary>
        public object Value { get; set; }
    }

    /// <summary>
    /// Supplies information about an clipboard event that is being raised.
    /// </summary>
    public class UIClipboardEventArgs : UIEventArgs
    {
    }

    /// <summary>
    /// Supplies information about an drag event that is being raised.
    /// </summary>
    public class UIDragEventArgs : UIEventArgs
    {
    }

    /// <summary>
    /// Supplies information about an error event that is being raised.
    /// </summary>
    public class UIErrorEventArgs : UIEventArgs
    {
    }

    /// <summary>
    /// Supplies information about a focus event that is being raised.
    /// </summary>
    public class UIFocusEventArgs : UIEventArgs
    {
        // Not including support for 'relatedTarget' since we don't have a good way to represent it.
        // see: https://developer.mozilla.org/en-US/docs/Web/API/FocusEvent
    }

    /// <summary>
    /// Supplies information about a keyboard event that is being raised.
    /// </summary>
    public class UIKeyboardEventArgs : UIEventArgs
    {
        /// <summary>
        /// If applicable, gets or sets the key that produced the event.
        /// </summary>
        public string Key { get; set; }
    }

    /// <summary>
    /// Supplies information about a mouse event that is being raised.
    /// </summary>
    public class UIMouseEventArgs : UIEventArgs
    {
    }

    /// <summary>
    /// Supplies information about a mouse event that is being raised.
    /// </summary>
    public class UIPointerEventArgs : UIMouseEventArgs
    {
    }

    /// <summary>
    /// Supplies information about a progress event that is being raised.
    /// </summary>
    public class UIProgressEventArgs : UIMouseEventArgs
    {
    }

    /// <summary>
    /// Supplies information about a touch event that is being raised.
    /// </summary>
    public class UITouchEventArgs : UIEventArgs
    {
    }

    /// <summary>
    /// Supplies information about a mouse wheel event that is being raised.
    /// </summary>
    public class UIWheelEventArgs : UIEventArgs
    {
    }
}
