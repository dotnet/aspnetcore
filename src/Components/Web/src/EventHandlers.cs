// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Components
{
    /// <summary>
    /// Holds <see cref="EventHandler"/> attributes to configure the mappings between event names and
    /// event argument types.
    /// </summary>

    // Focus events
    [EventHandler("onfocus", typeof(UIFocusEventArgs))]
    [EventHandler("onblur", typeof(UIFocusEventArgs))]
    [EventHandler("onfocusin", typeof(UIFocusEventArgs))]
    [EventHandler("onfocusout", typeof(UIFocusEventArgs))]

    // Mouse events
    [EventHandler("onmouseover", typeof(UIMouseEventArgs))]
    [EventHandler("onmouseout", typeof(UIMouseEventArgs))]
    [EventHandler("onmousemove", typeof(UIMouseEventArgs))]
    [EventHandler("onmousedown", typeof(UIMouseEventArgs))]
    [EventHandler("onmouseup", typeof(UIMouseEventArgs))]
    [EventHandler("onclick", typeof(UIMouseEventArgs))]
    [EventHandler("ondblclick", typeof(UIMouseEventArgs))]
    [EventHandler("onwheel", typeof(UIWheelEventArgs))]
    [EventHandler("onmousewheel", typeof(UIWheelEventArgs))]
    [EventHandler("oncontextmenu", typeof(UIMouseEventArgs))]

    // Drag events
    [EventHandler("ondrag", typeof(UIDragEventArgs))]
    [EventHandler("ondragend", typeof(UIDragEventArgs))]
    [EventHandler("ondragenter", typeof(UIDragEventArgs))]
    [EventHandler("ondragleave", typeof(UIDragEventArgs))]
    [EventHandler("ondragover", typeof(UIDragEventArgs))]
    [EventHandler("ondragstart", typeof(UIDragEventArgs))]
    [EventHandler("ondrop", typeof(UIDragEventArgs))]

    // Keyboard events
    [EventHandler("onkeydown", typeof(UIKeyboardEventArgs))]
    [EventHandler("onkeyup", typeof(UIKeyboardEventArgs))]
    [EventHandler("onkeypress", typeof(UIKeyboardEventArgs))]

    // Input events
    [EventHandler("onchange", typeof(ChangeEventArgs))]
    [EventHandler("oninput", typeof(ChangeEventArgs))]
    [EventHandler("oninvalid", typeof(EventArgs))]
    [EventHandler("onreset", typeof(EventArgs))]
    [EventHandler("onselect", typeof(EventArgs))]
    [EventHandler("onselectstart", typeof(EventArgs))]
    [EventHandler("onselectionchange", typeof(EventArgs))]
    [EventHandler("onsubmit", typeof(EventArgs))]

    // Clipboard events
    [EventHandler("onbeforecopy", typeof(EventArgs))]
    [EventHandler("onbeforecut", typeof(EventArgs))]
    [EventHandler("onbeforepaste", typeof(EventArgs))]
    [EventHandler("oncopy", typeof(UIClipboardEventArgs))]
    [EventHandler("oncut", typeof(UIClipboardEventArgs))]
    [EventHandler("onpaste", typeof(UIClipboardEventArgs))]

    // Touch events
    [EventHandler("ontouchcancel", typeof(UITouchEventArgs))]
    [EventHandler("ontouchend", typeof(UITouchEventArgs))]
    [EventHandler("ontouchmove", typeof(UITouchEventArgs))]
    [EventHandler("ontouchstart", typeof(UITouchEventArgs))]
    [EventHandler("ontouchenter", typeof(UITouchEventArgs))]
    [EventHandler("ontouchleave", typeof(UITouchEventArgs))]

    // Pointer events
    [EventHandler("ongotpointercapture", typeof(UIPointerEventArgs))]
    [EventHandler("onlostpointercapture", typeof(UIPointerEventArgs))]
    [EventHandler("onpointercancel", typeof(UIPointerEventArgs))]
    [EventHandler("onpointerdown", typeof(UIPointerEventArgs))]
    [EventHandler("onpointerenter", typeof(UIPointerEventArgs))]
    [EventHandler("onpointerleave", typeof(UIPointerEventArgs))]
    [EventHandler("onpointermove", typeof(UIPointerEventArgs))]
    [EventHandler("onpointerout", typeof(UIPointerEventArgs))]
    [EventHandler("onpointerover", typeof(UIPointerEventArgs))]
    [EventHandler("onpointerup", typeof(UIPointerEventArgs))]

    // Media events
    [EventHandler("oncanplay", typeof(EventArgs))]
    [EventHandler("oncanplaythrough", typeof(EventArgs))]
    [EventHandler("oncuechange", typeof(EventArgs))]
    [EventHandler("ondurationchange", typeof(EventArgs))]
    [EventHandler("onemptied", typeof(EventArgs))]
    [EventHandler("onpause", typeof(EventArgs))]
    [EventHandler("onplay", typeof(EventArgs))]
    [EventHandler("onplaying", typeof(EventArgs))]
    [EventHandler("onratechange", typeof(EventArgs))]
    [EventHandler("onseeked", typeof(EventArgs))]
    [EventHandler("onseeking", typeof(EventArgs))]
    [EventHandler("onstalled", typeof(EventArgs))]
    [EventHandler("onstop", typeof(EventArgs))]
    [EventHandler("onsuspend", typeof(EventArgs))]
    [EventHandler("ontimeupdate", typeof(EventArgs))]
    [EventHandler("onvolumechange", typeof(EventArgs))]
    [EventHandler("onwaiting", typeof(EventArgs))]

    // Progress events
    [EventHandler("onloadstart", typeof(UIProgressEventArgs))]
    [EventHandler("ontimeout", typeof(UIProgressEventArgs))]
    [EventHandler("onabort", typeof(UIProgressEventArgs))]
    [EventHandler("onload", typeof(UIProgressEventArgs))]
    [EventHandler("onloadend", typeof(UIProgressEventArgs))]
    [EventHandler("onprogress", typeof(UIProgressEventArgs))]
    [EventHandler("onerror", typeof(UIErrorEventArgs))]

    // General events
    [EventHandler("onactivate", typeof(EventArgs))]
    [EventHandler("onbeforeactivate", typeof(EventArgs))]
    [EventHandler("onbeforedeactivate", typeof(EventArgs))]
    [EventHandler("ondeactivate", typeof(EventArgs))]
    [EventHandler("onended", typeof(EventArgs))]
    [EventHandler("onfullscreenchange", typeof(EventArgs))]
    [EventHandler("onfullscreenerror", typeof(EventArgs))]
    [EventHandler("onloadeddata", typeof(EventArgs))]
    [EventHandler("onloadedmetadata", typeof(EventArgs))]
    [EventHandler("onpointerlockchange", typeof(EventArgs))]
    [EventHandler("onpointerlockerror", typeof(EventArgs))]
    [EventHandler("onreadystatechange", typeof(EventArgs))]
    [EventHandler("onscroll", typeof(EventArgs))]
    public static class EventHandlers
    {
    }
}
