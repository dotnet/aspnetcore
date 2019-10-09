// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Components.Web
{
    /// <summary>
    /// Holds <see cref="EventHandler"/> attributes to configure the mappings between event names and
    /// event argument types.
    /// </summary>

    // Focus events
    [EventHandler("onfocus", typeof(FocusEventArgs))]
    [EventHandler("onblur", typeof(FocusEventArgs))]
    [EventHandler("onfocusin", typeof(FocusEventArgs))]
    [EventHandler("onfocusout", typeof(FocusEventArgs))]

    // Mouse events
    [EventHandler("onmouseover", typeof(MouseEventArgs))]
    [EventHandler("onmouseout", typeof(MouseEventArgs))]
    [EventHandler("onmousemove", typeof(MouseEventArgs))]
    [EventHandler("onmousedown", typeof(MouseEventArgs))]
    [EventHandler("onmouseup", typeof(MouseEventArgs))]
    [EventHandler("onclick", typeof(MouseEventArgs))]
    [EventHandler("ondblclick", typeof(MouseEventArgs))]
    [EventHandler("onwheel", typeof(WheelEventArgs))]
    [EventHandler("onmousewheel", typeof(WheelEventArgs))]
    [EventHandler("oncontextmenu", typeof(MouseEventArgs))]

    // Drag events
    [EventHandler("ondrag", typeof(DragEventArgs))]
    [EventHandler("ondragend", typeof(DragEventArgs))]
    [EventHandler("ondragenter", typeof(DragEventArgs))]
    [EventHandler("ondragleave", typeof(DragEventArgs))]
    [EventHandler("ondragover", typeof(DragEventArgs))]
    [EventHandler("ondragstart", typeof(DragEventArgs))]
    [EventHandler("ondrop", typeof(DragEventArgs))]

    // Keyboard events
    [EventHandler("onkeydown", typeof(KeyboardEventArgs))]
    [EventHandler("onkeyup", typeof(KeyboardEventArgs))]
    [EventHandler("onkeypress", typeof(KeyboardEventArgs))]

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
    [EventHandler("oncopy", typeof(ClipboardEventArgs))]
    [EventHandler("oncut", typeof(ClipboardEventArgs))]
    [EventHandler("onpaste", typeof(ClipboardEventArgs))]

    // Touch events
    [EventHandler("ontouchcancel", typeof(TouchEventArgs))]
    [EventHandler("ontouchend", typeof(TouchEventArgs))]
    [EventHandler("ontouchmove", typeof(TouchEventArgs))]
    [EventHandler("ontouchstart", typeof(TouchEventArgs))]
    [EventHandler("ontouchenter", typeof(TouchEventArgs))]
    [EventHandler("ontouchleave", typeof(TouchEventArgs))]

    // Pointer events
    [EventHandler("ongotpointercapture", typeof(PointerEventArgs))]
    [EventHandler("onlostpointercapture", typeof(PointerEventArgs))]
    [EventHandler("onpointercancel", typeof(PointerEventArgs))]
    [EventHandler("onpointerdown", typeof(PointerEventArgs))]
    [EventHandler("onpointerenter", typeof(PointerEventArgs))]
    [EventHandler("onpointerleave", typeof(PointerEventArgs))]
    [EventHandler("onpointermove", typeof(PointerEventArgs))]
    [EventHandler("onpointerout", typeof(PointerEventArgs))]
    [EventHandler("onpointerover", typeof(PointerEventArgs))]
    [EventHandler("onpointerup", typeof(PointerEventArgs))]

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
    [EventHandler("onloadstart", typeof(ProgressEventArgs))]
    [EventHandler("ontimeout", typeof(ProgressEventArgs))]
    [EventHandler("onabort", typeof(ProgressEventArgs))]
    [EventHandler("onload", typeof(ProgressEventArgs))]
    [EventHandler("onloadend", typeof(ProgressEventArgs))]
    [EventHandler("onprogress", typeof(ProgressEventArgs))]
    [EventHandler("onerror", typeof(ErrorEventArgs))]

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
