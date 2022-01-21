// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Web;

/// <summary>
/// Holds <see cref="EventHandler"/> attributes to configure the mappings between event names and
/// event argument types.
/// </summary>
// Focus events
[EventHandler("onfocus", typeof(FocusEventArgs), true, true)]
[EventHandler("onblur", typeof(FocusEventArgs), true, true)]
[EventHandler("onfocusin", typeof(FocusEventArgs), true, true)]
[EventHandler("onfocusout", typeof(FocusEventArgs), true, true)]

// Mouse events
[EventHandler("onmouseover", typeof(MouseEventArgs), true, true)]
[EventHandler("onmouseout", typeof(MouseEventArgs), true, true)]
[EventHandler("onmousemove", typeof(MouseEventArgs), true, true)]
[EventHandler("onmousedown", typeof(MouseEventArgs), true, true)]
[EventHandler("onmouseup", typeof(MouseEventArgs), true, true)]
[EventHandler("onclick", typeof(MouseEventArgs), true, true)]
[EventHandler("ondblclick", typeof(MouseEventArgs), true, true)]
[EventHandler("onwheel", typeof(WheelEventArgs), true, true)]
[EventHandler("onmousewheel", typeof(WheelEventArgs), true, true)]
[EventHandler("oncontextmenu", typeof(MouseEventArgs), true, true)]

// Drag events
[EventHandler("ondrag", typeof(DragEventArgs), true, true)]
[EventHandler("ondragend", typeof(DragEventArgs), true, true)]
[EventHandler("ondragenter", typeof(DragEventArgs), true, true)]
[EventHandler("ondragleave", typeof(DragEventArgs), true, true)]
[EventHandler("ondragover", typeof(DragEventArgs), true, true)]
[EventHandler("ondragstart", typeof(DragEventArgs), true, true)]
[EventHandler("ondrop", typeof(DragEventArgs), true, true)]

// Keyboard events
[EventHandler("onkeydown", typeof(KeyboardEventArgs), true, true)]
[EventHandler("onkeyup", typeof(KeyboardEventArgs), true, true)]
[EventHandler("onkeypress", typeof(KeyboardEventArgs), true, true)]

// Input events
[EventHandler("onchange", typeof(ChangeEventArgs), true, true)]
[EventHandler("oninput", typeof(ChangeEventArgs), true, true)]
[EventHandler("oninvalid", typeof(EventArgs), true, true)]
[EventHandler("onreset", typeof(EventArgs), true, true)]
[EventHandler("onselect", typeof(EventArgs), true, true)]
[EventHandler("onselectstart", typeof(EventArgs), true, true)]
[EventHandler("onselectionchange", typeof(EventArgs), true, true)]
[EventHandler("onsubmit", typeof(EventArgs), true, true)]

// Clipboard events
[EventHandler("onbeforecopy", typeof(EventArgs), true, true)]
[EventHandler("onbeforecut", typeof(EventArgs), true, true)]
[EventHandler("onbeforepaste", typeof(EventArgs), true, true)]
[EventHandler("oncopy", typeof(ClipboardEventArgs), true, true)]
[EventHandler("oncut", typeof(ClipboardEventArgs), true, true)]
[EventHandler("onpaste", typeof(ClipboardEventArgs), true, true)]

// Touch events
[EventHandler("ontouchcancel", typeof(TouchEventArgs), true, true)]
[EventHandler("ontouchend", typeof(TouchEventArgs), true, true)]
[EventHandler("ontouchmove", typeof(TouchEventArgs), true, true)]
[EventHandler("ontouchstart", typeof(TouchEventArgs), true, true)]
[EventHandler("ontouchenter", typeof(TouchEventArgs), true, true)]
[EventHandler("ontouchleave", typeof(TouchEventArgs), true, true)]

// Pointer events
[EventHandler("ongotpointercapture", typeof(PointerEventArgs), true, true)]
[EventHandler("onlostpointercapture", typeof(PointerEventArgs), true, true)]
[EventHandler("onpointercancel", typeof(PointerEventArgs), true, true)]
[EventHandler("onpointerdown", typeof(PointerEventArgs), true, true)]
[EventHandler("onpointerenter", typeof(PointerEventArgs), true, true)]
[EventHandler("onpointerleave", typeof(PointerEventArgs), true, true)]
[EventHandler("onpointermove", typeof(PointerEventArgs), true, true)]
[EventHandler("onpointerout", typeof(PointerEventArgs), true, true)]
[EventHandler("onpointerover", typeof(PointerEventArgs), true, true)]
[EventHandler("onpointerup", typeof(PointerEventArgs), true, true)]

// Media events
[EventHandler("oncanplay", typeof(EventArgs), true, true)]
[EventHandler("oncanplaythrough", typeof(EventArgs), true, true)]
[EventHandler("oncuechange", typeof(EventArgs), true, true)]
[EventHandler("ondurationchange", typeof(EventArgs), true, true)]
[EventHandler("onemptied", typeof(EventArgs), true, true)]
[EventHandler("onpause", typeof(EventArgs), true, true)]
[EventHandler("onplay", typeof(EventArgs), true, true)]
[EventHandler("onplaying", typeof(EventArgs), true, true)]
[EventHandler("onratechange", typeof(EventArgs), true, true)]
[EventHandler("onseeked", typeof(EventArgs), true, true)]
[EventHandler("onseeking", typeof(EventArgs), true, true)]
[EventHandler("onstalled", typeof(EventArgs), true, true)]
[EventHandler("onstop", typeof(EventArgs), true, true)]
[EventHandler("onsuspend", typeof(EventArgs), true, true)]
[EventHandler("ontimeupdate", typeof(EventArgs), true, true)]
[EventHandler("onvolumechange", typeof(EventArgs), true, true)]
[EventHandler("onwaiting", typeof(EventArgs), true, true)]

// Progress events
[EventHandler("onloadstart", typeof(ProgressEventArgs), true, true)]
[EventHandler("ontimeout", typeof(ProgressEventArgs), true, true)]
[EventHandler("onabort", typeof(ProgressEventArgs), true, true)]
[EventHandler("onload", typeof(ProgressEventArgs), true, true)]
[EventHandler("onloadend", typeof(ProgressEventArgs), true, true)]
[EventHandler("onprogress", typeof(ProgressEventArgs), true, true)]
[EventHandler("onerror", typeof(ErrorEventArgs), true, true)]

// General events
[EventHandler("onactivate", typeof(EventArgs), true, true)]
[EventHandler("onbeforeactivate", typeof(EventArgs), true, true)]
[EventHandler("onbeforedeactivate", typeof(EventArgs), true, true)]
[EventHandler("ondeactivate", typeof(EventArgs), true, true)]
[EventHandler("onended", typeof(EventArgs), true, true)]
[EventHandler("onfullscreenchange", typeof(EventArgs), true, true)]
[EventHandler("onfullscreenerror", typeof(EventArgs), true, true)]
[EventHandler("onloadeddata", typeof(EventArgs), true, true)]
[EventHandler("onloadedmetadata", typeof(EventArgs), true, true)]
[EventHandler("onpointerlockchange", typeof(EventArgs), true, true)]
[EventHandler("onpointerlockerror", typeof(EventArgs), true, true)]
[EventHandler("onreadystatechange", typeof(EventArgs), true, true)]
[EventHandler("onscroll", typeof(EventArgs), true, true)]

[EventHandler("ontoggle", typeof(EventArgs), true, true)]
public static class EventHandlers
{
}
