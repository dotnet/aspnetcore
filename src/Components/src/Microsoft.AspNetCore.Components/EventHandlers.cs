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
    [EventHandler("onchange", typeof(UIChangeEventArgs))]
    [EventHandler("oninput", typeof(UIChangeEventArgs))]
    [EventHandler("oninvalid", typeof(UIEventArgs))]
    [EventHandler("onreset", typeof(UIEventArgs))]
    [EventHandler("onselect", typeof(UIEventArgs))]
    [EventHandler("onselectstart", typeof(UIEventArgs))]
    [EventHandler("onselectionchange", typeof(UIEventArgs))]
    [EventHandler("onsubmit", typeof(UIEventArgs))]

    // Clipboard events
    [EventHandler("onbeforecopy", typeof(UIEventArgs))]
    [EventHandler("onbeforecut", typeof(UIEventArgs))]
    [EventHandler("onbeforepaste", typeof(UIEventArgs))]
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
    [EventHandler("gotpointercapture", typeof(UIPointerEventArgs))]
    [EventHandler("lostpointercapture", typeof(UIPointerEventArgs))]
    [EventHandler("pointercancel", typeof(UIPointerEventArgs))]
    [EventHandler("pointerdown", typeof(UIPointerEventArgs))]
    [EventHandler("pointerenter", typeof(UIPointerEventArgs))]
    [EventHandler("pointerleave", typeof(UIPointerEventArgs))]
    [EventHandler("pointermove", typeof(UIPointerEventArgs))]
    [EventHandler("pointerout", typeof(UIPointerEventArgs))]
    [EventHandler("pointerover", typeof(UIPointerEventArgs))]
    [EventHandler("pointerup", typeof(UIPointerEventArgs))]

    // Media events
    [EventHandler("oncanplay", typeof(UIEventArgs))]
    [EventHandler("oncanplaythrough", typeof(UIEventArgs))]
    [EventHandler("oncuechange", typeof(UIEventArgs))]
    [EventHandler("ondurationchange", typeof(UIEventArgs))]
    [EventHandler("onemptied", typeof(UIEventArgs))]
    [EventHandler("onpause", typeof(UIEventArgs))]
    [EventHandler("onplay", typeof(UIEventArgs))]
    [EventHandler("onplaying", typeof(UIEventArgs))]
    [EventHandler("onratechange", typeof(UIEventArgs))]
    [EventHandler("onseeked", typeof(UIEventArgs))]
    [EventHandler("onseeking", typeof(UIEventArgs))]
    [EventHandler("onstalled", typeof(UIEventArgs))]
    [EventHandler("onstop", typeof(UIEventArgs))]
    [EventHandler("onsuspend", typeof(UIEventArgs))]
    [EventHandler("ontimeupdate", typeof(UIEventArgs))]
    [EventHandler("onvolumechange", typeof(UIEventArgs))]
    [EventHandler("onwaiting", typeof(UIEventArgs))]

    // Progress events
    [EventHandler("onloadstart", typeof(UIProgressEventArgs))]
    [EventHandler("ontimeout", typeof(UIProgressEventArgs))]
    [EventHandler("onabort", typeof(UIProgressEventArgs))]
    [EventHandler("onload", typeof(UIProgressEventArgs))]
    [EventHandler("onloadend", typeof(UIProgressEventArgs))]
    [EventHandler("onprogress", typeof(UIProgressEventArgs))]
    [EventHandler("onerror", typeof(UIErrorEventArgs))]

    // General events
    [EventHandler("onactivate", typeof(UIEventArgs))]
    [EventHandler("onbeforeactivate", typeof(UIEventArgs))]
    [EventHandler("onbeforedeactivate", typeof(UIEventArgs))]
    [EventHandler("ondeactivate", typeof(UIEventArgs))]
    [EventHandler("onended", typeof(UIEventArgs))]
    [EventHandler("onfullscreenchange", typeof(UIEventArgs))]
    [EventHandler("onfullscreenerror", typeof(UIEventArgs))]
    [EventHandler("onloadeddata", typeof(UIEventArgs))]
    [EventHandler("onloadedmetadata", typeof(UIEventArgs))]
    [EventHandler("onpointerlockchange", typeof(UIEventArgs))]
    [EventHandler("onpointerlockerror", typeof(UIEventArgs))]
    [EventHandler("onreadystatechange", typeof(UIEventArgs))]
    [EventHandler("onscroll", typeof(UIEventArgs))]
    public static class EventHandlers
    {
    }
}
