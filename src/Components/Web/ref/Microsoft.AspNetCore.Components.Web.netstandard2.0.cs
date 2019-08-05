// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Components
{
    [Microsoft.AspNetCore.Components.BindElementAttribute("select", null, "value", "onchange")]
    [Microsoft.AspNetCore.Components.BindElementAttribute("textarea", null, "value", "onchange")]
    [Microsoft.AspNetCore.Components.BindInputElementAttribute("checkbox", null, "checked", "onchange", false, null)]
    [Microsoft.AspNetCore.Components.BindInputElementAttribute("date", "value", "value", "onchange", true, "yyyy-MM-dd")]
    [Microsoft.AspNetCore.Components.BindInputElementAttribute("date", null, "value", "onchange", true, "yyyy-MM-dd")]
    [Microsoft.AspNetCore.Components.BindInputElementAttribute("number", "value", "value", "onchange", true, null)]
    [Microsoft.AspNetCore.Components.BindInputElementAttribute("number", null, "value", "onchange", true, null)]
    [Microsoft.AspNetCore.Components.BindInputElementAttribute("text", null, "value", "onchange", false, null)]
    [Microsoft.AspNetCore.Components.BindInputElementAttribute(null, "value", "value", "onchange", false, null)]
    [Microsoft.AspNetCore.Components.BindInputElementAttribute(null, null, "value", "onchange", false, null)]
    public static partial class BindAttributes
    {
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Class, AllowMultiple=true, Inherited=true)]
    public sealed partial class BindElementAttribute : System.Attribute
    {
        public BindElementAttribute(string element, string suffix, string valueAttribute, string changeAttribute) { }
        public string ChangeAttribute { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public string Element { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public string Suffix { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public string ValueAttribute { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Class, AllowMultiple=true, Inherited=true)]
    public sealed partial class BindInputElementAttribute : System.Attribute
    {
        public BindInputElementAttribute(string type, string suffix, string valueAttribute, string changeAttribute, bool isInvariantCulture, string format) { }
        public string ChangeAttribute { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public string Format { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public bool IsInvariantCulture { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public string Suffix { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public string Type { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public string ValueAttribute { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
    }
    public partial class DataTransfer
    {
        public DataTransfer() { }
        public string DropEffect { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public string EffectAllowed { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public string[] Files { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public Microsoft.AspNetCore.Components.UIDataTransferItem[] Items { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public string[] Types { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Class, AllowMultiple=true, Inherited=true)]
    public sealed partial class EventHandlerAttribute : System.Attribute
    {
        public EventHandlerAttribute(string attributeName, System.Type eventArgsType) { }
        public string AttributeName { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public System.Type EventArgsType { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
    }
    [Microsoft.AspNetCore.Components.EventHandlerAttribute("onabort", typeof(Microsoft.AspNetCore.Components.UIProgressEventArgs))]
    [Microsoft.AspNetCore.Components.EventHandlerAttribute("onactivate", typeof(System.EventArgs))]
    [Microsoft.AspNetCore.Components.EventHandlerAttribute("onbeforeactivate", typeof(System.EventArgs))]
    [Microsoft.AspNetCore.Components.EventHandlerAttribute("onbeforecopy", typeof(System.EventArgs))]
    [Microsoft.AspNetCore.Components.EventHandlerAttribute("onbeforecut", typeof(System.EventArgs))]
    [Microsoft.AspNetCore.Components.EventHandlerAttribute("onbeforedeactivate", typeof(System.EventArgs))]
    [Microsoft.AspNetCore.Components.EventHandlerAttribute("onbeforepaste", typeof(System.EventArgs))]
    [Microsoft.AspNetCore.Components.EventHandlerAttribute("onblur", typeof(Microsoft.AspNetCore.Components.UIFocusEventArgs))]
    [Microsoft.AspNetCore.Components.EventHandlerAttribute("oncanplay", typeof(System.EventArgs))]
    [Microsoft.AspNetCore.Components.EventHandlerAttribute("oncanplaythrough", typeof(System.EventArgs))]
    [Microsoft.AspNetCore.Components.EventHandlerAttribute("onchange", typeof(Microsoft.AspNetCore.Components.ChangeEventArgs))]
    [Microsoft.AspNetCore.Components.EventHandlerAttribute("onclick", typeof(Microsoft.AspNetCore.Components.UIMouseEventArgs))]
    [Microsoft.AspNetCore.Components.EventHandlerAttribute("oncontextmenu", typeof(Microsoft.AspNetCore.Components.UIMouseEventArgs))]
    [Microsoft.AspNetCore.Components.EventHandlerAttribute("oncopy", typeof(Microsoft.AspNetCore.Components.UIClipboardEventArgs))]
    [Microsoft.AspNetCore.Components.EventHandlerAttribute("oncuechange", typeof(System.EventArgs))]
    [Microsoft.AspNetCore.Components.EventHandlerAttribute("oncut", typeof(Microsoft.AspNetCore.Components.UIClipboardEventArgs))]
    [Microsoft.AspNetCore.Components.EventHandlerAttribute("ondblclick", typeof(Microsoft.AspNetCore.Components.UIMouseEventArgs))]
    [Microsoft.AspNetCore.Components.EventHandlerAttribute("ondeactivate", typeof(System.EventArgs))]
    [Microsoft.AspNetCore.Components.EventHandlerAttribute("ondrag", typeof(Microsoft.AspNetCore.Components.UIDragEventArgs))]
    [Microsoft.AspNetCore.Components.EventHandlerAttribute("ondragend", typeof(Microsoft.AspNetCore.Components.UIDragEventArgs))]
    [Microsoft.AspNetCore.Components.EventHandlerAttribute("ondragenter", typeof(Microsoft.AspNetCore.Components.UIDragEventArgs))]
    [Microsoft.AspNetCore.Components.EventHandlerAttribute("ondragleave", typeof(Microsoft.AspNetCore.Components.UIDragEventArgs))]
    [Microsoft.AspNetCore.Components.EventHandlerAttribute("ondragover", typeof(Microsoft.AspNetCore.Components.UIDragEventArgs))]
    [Microsoft.AspNetCore.Components.EventHandlerAttribute("ondragstart", typeof(Microsoft.AspNetCore.Components.UIDragEventArgs))]
    [Microsoft.AspNetCore.Components.EventHandlerAttribute("ondrop", typeof(Microsoft.AspNetCore.Components.UIDragEventArgs))]
    [Microsoft.AspNetCore.Components.EventHandlerAttribute("ondurationchange", typeof(System.EventArgs))]
    [Microsoft.AspNetCore.Components.EventHandlerAttribute("onemptied", typeof(System.EventArgs))]
    [Microsoft.AspNetCore.Components.EventHandlerAttribute("onended", typeof(System.EventArgs))]
    [Microsoft.AspNetCore.Components.EventHandlerAttribute("onerror", typeof(Microsoft.AspNetCore.Components.UIErrorEventArgs))]
    [Microsoft.AspNetCore.Components.EventHandlerAttribute("onfocus", typeof(Microsoft.AspNetCore.Components.UIFocusEventArgs))]
    [Microsoft.AspNetCore.Components.EventHandlerAttribute("onfocusin", typeof(Microsoft.AspNetCore.Components.UIFocusEventArgs))]
    [Microsoft.AspNetCore.Components.EventHandlerAttribute("onfocusout", typeof(Microsoft.AspNetCore.Components.UIFocusEventArgs))]
    [Microsoft.AspNetCore.Components.EventHandlerAttribute("onfullscreenchange", typeof(System.EventArgs))]
    [Microsoft.AspNetCore.Components.EventHandlerAttribute("onfullscreenerror", typeof(System.EventArgs))]
    [Microsoft.AspNetCore.Components.EventHandlerAttribute("ongotpointercapture", typeof(Microsoft.AspNetCore.Components.UIPointerEventArgs))]
    [Microsoft.AspNetCore.Components.EventHandlerAttribute("oninput", typeof(Microsoft.AspNetCore.Components.ChangeEventArgs))]
    [Microsoft.AspNetCore.Components.EventHandlerAttribute("oninvalid", typeof(System.EventArgs))]
    [Microsoft.AspNetCore.Components.EventHandlerAttribute("onkeydown", typeof(Microsoft.AspNetCore.Components.UIKeyboardEventArgs))]
    [Microsoft.AspNetCore.Components.EventHandlerAttribute("onkeypress", typeof(Microsoft.AspNetCore.Components.UIKeyboardEventArgs))]
    [Microsoft.AspNetCore.Components.EventHandlerAttribute("onkeyup", typeof(Microsoft.AspNetCore.Components.UIKeyboardEventArgs))]
    [Microsoft.AspNetCore.Components.EventHandlerAttribute("onload", typeof(Microsoft.AspNetCore.Components.UIProgressEventArgs))]
    [Microsoft.AspNetCore.Components.EventHandlerAttribute("onloadeddata", typeof(System.EventArgs))]
    [Microsoft.AspNetCore.Components.EventHandlerAttribute("onloadedmetadata", typeof(System.EventArgs))]
    [Microsoft.AspNetCore.Components.EventHandlerAttribute("onloadend", typeof(Microsoft.AspNetCore.Components.UIProgressEventArgs))]
    [Microsoft.AspNetCore.Components.EventHandlerAttribute("onloadstart", typeof(Microsoft.AspNetCore.Components.UIProgressEventArgs))]
    [Microsoft.AspNetCore.Components.EventHandlerAttribute("onlostpointercapture", typeof(Microsoft.AspNetCore.Components.UIPointerEventArgs))]
    [Microsoft.AspNetCore.Components.EventHandlerAttribute("onmousedown", typeof(Microsoft.AspNetCore.Components.UIMouseEventArgs))]
    [Microsoft.AspNetCore.Components.EventHandlerAttribute("onmousemove", typeof(Microsoft.AspNetCore.Components.UIMouseEventArgs))]
    [Microsoft.AspNetCore.Components.EventHandlerAttribute("onmouseout", typeof(Microsoft.AspNetCore.Components.UIMouseEventArgs))]
    [Microsoft.AspNetCore.Components.EventHandlerAttribute("onmouseover", typeof(Microsoft.AspNetCore.Components.UIMouseEventArgs))]
    [Microsoft.AspNetCore.Components.EventHandlerAttribute("onmouseup", typeof(Microsoft.AspNetCore.Components.UIMouseEventArgs))]
    [Microsoft.AspNetCore.Components.EventHandlerAttribute("onmousewheel", typeof(Microsoft.AspNetCore.Components.UIWheelEventArgs))]
    [Microsoft.AspNetCore.Components.EventHandlerAttribute("onpaste", typeof(Microsoft.AspNetCore.Components.UIClipboardEventArgs))]
    [Microsoft.AspNetCore.Components.EventHandlerAttribute("onpause", typeof(System.EventArgs))]
    [Microsoft.AspNetCore.Components.EventHandlerAttribute("onplay", typeof(System.EventArgs))]
    [Microsoft.AspNetCore.Components.EventHandlerAttribute("onplaying", typeof(System.EventArgs))]
    [Microsoft.AspNetCore.Components.EventHandlerAttribute("onpointercancel", typeof(Microsoft.AspNetCore.Components.UIPointerEventArgs))]
    [Microsoft.AspNetCore.Components.EventHandlerAttribute("onpointerdown", typeof(Microsoft.AspNetCore.Components.UIPointerEventArgs))]
    [Microsoft.AspNetCore.Components.EventHandlerAttribute("onpointerenter", typeof(Microsoft.AspNetCore.Components.UIPointerEventArgs))]
    [Microsoft.AspNetCore.Components.EventHandlerAttribute("onpointerleave", typeof(Microsoft.AspNetCore.Components.UIPointerEventArgs))]
    [Microsoft.AspNetCore.Components.EventHandlerAttribute("onpointerlockchange", typeof(System.EventArgs))]
    [Microsoft.AspNetCore.Components.EventHandlerAttribute("onpointerlockerror", typeof(System.EventArgs))]
    [Microsoft.AspNetCore.Components.EventHandlerAttribute("onpointermove", typeof(Microsoft.AspNetCore.Components.UIPointerEventArgs))]
    [Microsoft.AspNetCore.Components.EventHandlerAttribute("onpointerout", typeof(Microsoft.AspNetCore.Components.UIPointerEventArgs))]
    [Microsoft.AspNetCore.Components.EventHandlerAttribute("onpointerover", typeof(Microsoft.AspNetCore.Components.UIPointerEventArgs))]
    [Microsoft.AspNetCore.Components.EventHandlerAttribute("onpointerup", typeof(Microsoft.AspNetCore.Components.UIPointerEventArgs))]
    [Microsoft.AspNetCore.Components.EventHandlerAttribute("onprogress", typeof(Microsoft.AspNetCore.Components.UIProgressEventArgs))]
    [Microsoft.AspNetCore.Components.EventHandlerAttribute("onratechange", typeof(System.EventArgs))]
    [Microsoft.AspNetCore.Components.EventHandlerAttribute("onreadystatechange", typeof(System.EventArgs))]
    [Microsoft.AspNetCore.Components.EventHandlerAttribute("onreset", typeof(System.EventArgs))]
    [Microsoft.AspNetCore.Components.EventHandlerAttribute("onscroll", typeof(System.EventArgs))]
    [Microsoft.AspNetCore.Components.EventHandlerAttribute("onseeked", typeof(System.EventArgs))]
    [Microsoft.AspNetCore.Components.EventHandlerAttribute("onseeking", typeof(System.EventArgs))]
    [Microsoft.AspNetCore.Components.EventHandlerAttribute("onselect", typeof(System.EventArgs))]
    [Microsoft.AspNetCore.Components.EventHandlerAttribute("onselectionchange", typeof(System.EventArgs))]
    [Microsoft.AspNetCore.Components.EventHandlerAttribute("onselectstart", typeof(System.EventArgs))]
    [Microsoft.AspNetCore.Components.EventHandlerAttribute("onstalled", typeof(System.EventArgs))]
    [Microsoft.AspNetCore.Components.EventHandlerAttribute("onstop", typeof(System.EventArgs))]
    [Microsoft.AspNetCore.Components.EventHandlerAttribute("onsubmit", typeof(System.EventArgs))]
    [Microsoft.AspNetCore.Components.EventHandlerAttribute("onsuspend", typeof(System.EventArgs))]
    [Microsoft.AspNetCore.Components.EventHandlerAttribute("ontimeout", typeof(Microsoft.AspNetCore.Components.UIProgressEventArgs))]
    [Microsoft.AspNetCore.Components.EventHandlerAttribute("ontimeupdate", typeof(System.EventArgs))]
    [Microsoft.AspNetCore.Components.EventHandlerAttribute("ontouchcancel", typeof(Microsoft.AspNetCore.Components.UITouchEventArgs))]
    [Microsoft.AspNetCore.Components.EventHandlerAttribute("ontouchend", typeof(Microsoft.AspNetCore.Components.UITouchEventArgs))]
    [Microsoft.AspNetCore.Components.EventHandlerAttribute("ontouchenter", typeof(Microsoft.AspNetCore.Components.UITouchEventArgs))]
    [Microsoft.AspNetCore.Components.EventHandlerAttribute("ontouchleave", typeof(Microsoft.AspNetCore.Components.UITouchEventArgs))]
    [Microsoft.AspNetCore.Components.EventHandlerAttribute("ontouchmove", typeof(Microsoft.AspNetCore.Components.UITouchEventArgs))]
    [Microsoft.AspNetCore.Components.EventHandlerAttribute("ontouchstart", typeof(Microsoft.AspNetCore.Components.UITouchEventArgs))]
    [Microsoft.AspNetCore.Components.EventHandlerAttribute("onvolumechange", typeof(System.EventArgs))]
    [Microsoft.AspNetCore.Components.EventHandlerAttribute("onwaiting", typeof(System.EventArgs))]
    [Microsoft.AspNetCore.Components.EventHandlerAttribute("onwheel", typeof(Microsoft.AspNetCore.Components.UIWheelEventArgs))]
    public static partial class EventHandlers
    {
    }
    public partial class UIClipboardEventArgs : System.EventArgs
    {
        public UIClipboardEventArgs() { }
        public string Type { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
    }
    public partial class UIDataTransferItem
    {
        public UIDataTransferItem() { }
        public string Kind { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public string Type { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
    }
    public partial class UIDragEventArgs : Microsoft.AspNetCore.Components.UIMouseEventArgs
    {
        public UIDragEventArgs() { }
        public Microsoft.AspNetCore.Components.DataTransfer DataTransfer { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
    }
    public partial class UIErrorEventArgs : System.EventArgs
    {
        public UIErrorEventArgs() { }
        public int Colno { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public string Filename { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public int Lineno { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public string Message { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public string Type { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
    }
    public partial class UIFocusEventArgs : System.EventArgs
    {
        public UIFocusEventArgs() { }
        public string Type { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
    }
    public partial class UIKeyboardEventArgs : System.EventArgs
    {
        public UIKeyboardEventArgs() { }
        public bool AltKey { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public string Code { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public bool CtrlKey { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public string Key { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public float Location { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public bool MetaKey { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public bool Repeat { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public bool ShiftKey { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public string Type { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
    }
    public partial class UIMouseEventArgs : System.EventArgs
    {
        public UIMouseEventArgs() { }
        public bool AltKey { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public long Button { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public long Buttons { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public double ClientX { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public double ClientY { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public bool CtrlKey { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public long Detail { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public bool MetaKey { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public double ScreenX { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public double ScreenY { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public bool ShiftKey { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public string Type { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
    }
    public partial class UIPointerEventArgs : Microsoft.AspNetCore.Components.UIMouseEventArgs
    {
        public UIPointerEventArgs() { }
        public float Height { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public bool IsPrimary { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public long PointerId { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public string PointerType { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public float Pressure { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public float TiltX { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public float TiltY { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public float Width { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
    }
    public partial class UIProgressEventArgs : System.EventArgs
    {
        public UIProgressEventArgs() { }
        public bool LengthComputable { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public long Loaded { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public long Total { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public string Type { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
    }
    public partial class UITouchEventArgs : System.EventArgs
    {
        public UITouchEventArgs() { }
        public bool AltKey { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public Microsoft.AspNetCore.Components.UITouchPoint[] ChangedTouches { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public bool CtrlKey { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public long Detail { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public bool MetaKey { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public bool ShiftKey { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public Microsoft.AspNetCore.Components.UITouchPoint[] TargetTouches { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public Microsoft.AspNetCore.Components.UITouchPoint[] Touches { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public string Type { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
    }
    public partial class UITouchPoint
    {
        public UITouchPoint() { }
        public double ClientX { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public double ClientY { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public long Identifier { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public double PageX { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public double PageY { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public double ScreenX { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public double ScreenY { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
    }
    public partial class UIWheelEventArgs : Microsoft.AspNetCore.Components.UIMouseEventArgs
    {
        public UIWheelEventArgs() { }
        public long DeltaMode { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public double DeltaX { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public double DeltaY { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public double DeltaZ { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
    }
    public static partial class WebEventCallbackFactoryUIEventArgsExtensions
    {
        public static Microsoft.AspNetCore.Components.EventCallback<Microsoft.AspNetCore.Components.UIClipboardEventArgs> Create(this Microsoft.AspNetCore.Components.EventCallbackFactory factory, object receiver, System.Action<Microsoft.AspNetCore.Components.UIClipboardEventArgs> callback) { throw null; }
        public static Microsoft.AspNetCore.Components.EventCallback<Microsoft.AspNetCore.Components.UIDragEventArgs> Create(this Microsoft.AspNetCore.Components.EventCallbackFactory factory, object receiver, System.Action<Microsoft.AspNetCore.Components.UIDragEventArgs> callback) { throw null; }
        public static Microsoft.AspNetCore.Components.EventCallback<Microsoft.AspNetCore.Components.UIErrorEventArgs> Create(this Microsoft.AspNetCore.Components.EventCallbackFactory factory, object receiver, System.Action<Microsoft.AspNetCore.Components.UIErrorEventArgs> callback) { throw null; }
        public static Microsoft.AspNetCore.Components.EventCallback<Microsoft.AspNetCore.Components.UIFocusEventArgs> Create(this Microsoft.AspNetCore.Components.EventCallbackFactory factory, object receiver, System.Action<Microsoft.AspNetCore.Components.UIFocusEventArgs> callback) { throw null; }
        public static Microsoft.AspNetCore.Components.EventCallback<Microsoft.AspNetCore.Components.UIKeyboardEventArgs> Create(this Microsoft.AspNetCore.Components.EventCallbackFactory factory, object receiver, System.Action<Microsoft.AspNetCore.Components.UIKeyboardEventArgs> callback) { throw null; }
        public static Microsoft.AspNetCore.Components.EventCallback<Microsoft.AspNetCore.Components.UIMouseEventArgs> Create(this Microsoft.AspNetCore.Components.EventCallbackFactory factory, object receiver, System.Action<Microsoft.AspNetCore.Components.UIMouseEventArgs> callback) { throw null; }
        public static Microsoft.AspNetCore.Components.EventCallback<Microsoft.AspNetCore.Components.UIPointerEventArgs> Create(this Microsoft.AspNetCore.Components.EventCallbackFactory factory, object receiver, System.Action<Microsoft.AspNetCore.Components.UIPointerEventArgs> callback) { throw null; }
        public static Microsoft.AspNetCore.Components.EventCallback<Microsoft.AspNetCore.Components.UIProgressEventArgs> Create(this Microsoft.AspNetCore.Components.EventCallbackFactory factory, object receiver, System.Action<Microsoft.AspNetCore.Components.UIProgressEventArgs> callback) { throw null; }
        public static Microsoft.AspNetCore.Components.EventCallback<Microsoft.AspNetCore.Components.UITouchEventArgs> Create(this Microsoft.AspNetCore.Components.EventCallbackFactory factory, object receiver, System.Action<Microsoft.AspNetCore.Components.UITouchEventArgs> callback) { throw null; }
        public static Microsoft.AspNetCore.Components.EventCallback<Microsoft.AspNetCore.Components.UIWheelEventArgs> Create(this Microsoft.AspNetCore.Components.EventCallbackFactory factory, object receiver, System.Action<Microsoft.AspNetCore.Components.UIWheelEventArgs> callback) { throw null; }
        public static Microsoft.AspNetCore.Components.EventCallback<Microsoft.AspNetCore.Components.UIClipboardEventArgs> Create(this Microsoft.AspNetCore.Components.EventCallbackFactory factory, object receiver, System.Func<Microsoft.AspNetCore.Components.UIClipboardEventArgs, System.Threading.Tasks.Task> callback) { throw null; }
        public static Microsoft.AspNetCore.Components.EventCallback<Microsoft.AspNetCore.Components.UIDragEventArgs> Create(this Microsoft.AspNetCore.Components.EventCallbackFactory factory, object receiver, System.Func<Microsoft.AspNetCore.Components.UIDragEventArgs, System.Threading.Tasks.Task> callback) { throw null; }
        public static Microsoft.AspNetCore.Components.EventCallback<Microsoft.AspNetCore.Components.UIErrorEventArgs> Create(this Microsoft.AspNetCore.Components.EventCallbackFactory factory, object receiver, System.Func<Microsoft.AspNetCore.Components.UIErrorEventArgs, System.Threading.Tasks.Task> callback) { throw null; }
        public static Microsoft.AspNetCore.Components.EventCallback<Microsoft.AspNetCore.Components.UIFocusEventArgs> Create(this Microsoft.AspNetCore.Components.EventCallbackFactory factory, object receiver, System.Func<Microsoft.AspNetCore.Components.UIFocusEventArgs, System.Threading.Tasks.Task> callback) { throw null; }
        public static Microsoft.AspNetCore.Components.EventCallback<Microsoft.AspNetCore.Components.UIKeyboardEventArgs> Create(this Microsoft.AspNetCore.Components.EventCallbackFactory factory, object receiver, System.Func<Microsoft.AspNetCore.Components.UIKeyboardEventArgs, System.Threading.Tasks.Task> callback) { throw null; }
        public static Microsoft.AspNetCore.Components.EventCallback<Microsoft.AspNetCore.Components.UIMouseEventArgs> Create(this Microsoft.AspNetCore.Components.EventCallbackFactory factory, object receiver, System.Func<Microsoft.AspNetCore.Components.UIMouseEventArgs, System.Threading.Tasks.Task> callback) { throw null; }
        public static Microsoft.AspNetCore.Components.EventCallback<Microsoft.AspNetCore.Components.UIPointerEventArgs> Create(this Microsoft.AspNetCore.Components.EventCallbackFactory factory, object receiver, System.Func<Microsoft.AspNetCore.Components.UIPointerEventArgs, System.Threading.Tasks.Task> callback) { throw null; }
        public static Microsoft.AspNetCore.Components.EventCallback<Microsoft.AspNetCore.Components.UIProgressEventArgs> Create(this Microsoft.AspNetCore.Components.EventCallbackFactory factory, object receiver, System.Func<Microsoft.AspNetCore.Components.UIProgressEventArgs, System.Threading.Tasks.Task> callback) { throw null; }
        public static Microsoft.AspNetCore.Components.EventCallback<Microsoft.AspNetCore.Components.UITouchEventArgs> Create(this Microsoft.AspNetCore.Components.EventCallbackFactory factory, object receiver, System.Func<Microsoft.AspNetCore.Components.UITouchEventArgs, System.Threading.Tasks.Task> callback) { throw null; }
        public static Microsoft.AspNetCore.Components.EventCallback<Microsoft.AspNetCore.Components.UIWheelEventArgs> Create(this Microsoft.AspNetCore.Components.EventCallbackFactory factory, object receiver, System.Func<Microsoft.AspNetCore.Components.UIWheelEventArgs, System.Threading.Tasks.Task> callback) { throw null; }
    }
}
namespace Microsoft.AspNetCore.Components.Forms
{
    public static partial class EditContextFieldClassExtensions
    {
        public static string FieldCssClass(this Microsoft.AspNetCore.Components.Forms.EditContext editContext, in Microsoft.AspNetCore.Components.Forms.FieldIdentifier fieldIdentifier) { throw null; }
        public static string FieldCssClass<TField>(this Microsoft.AspNetCore.Components.Forms.EditContext editContext, System.Linq.Expressions.Expression<System.Func<TField>> accessor) { throw null; }
    }
    public partial class EditForm : Microsoft.AspNetCore.Components.ComponentBase
    {
        public EditForm() { }
        [Microsoft.AspNetCore.Components.ParameterAttribute(CaptureUnmatchedValues=true)]
        public System.Collections.Generic.IReadOnlyDictionary<string, object> AdditionalAttributes { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        [Microsoft.AspNetCore.Components.ParameterAttribute]
        public Microsoft.AspNetCore.Components.RenderFragment<Microsoft.AspNetCore.Components.Forms.EditContext> ChildContent { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        [Microsoft.AspNetCore.Components.ParameterAttribute]
        public Microsoft.AspNetCore.Components.Forms.EditContext EditContext { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        [Microsoft.AspNetCore.Components.ParameterAttribute]
        public object Model { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        [Microsoft.AspNetCore.Components.ParameterAttribute]
        public Microsoft.AspNetCore.Components.EventCallback<Microsoft.AspNetCore.Components.Forms.EditContext> OnInvalidSubmit { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        [Microsoft.AspNetCore.Components.ParameterAttribute]
        public Microsoft.AspNetCore.Components.EventCallback<Microsoft.AspNetCore.Components.Forms.EditContext> OnSubmit { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        [Microsoft.AspNetCore.Components.ParameterAttribute]
        public Microsoft.AspNetCore.Components.EventCallback<Microsoft.AspNetCore.Components.Forms.EditContext> OnValidSubmit { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        protected override void BuildRenderTree(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder builder) { }
        protected override void OnParametersSet() { }
    }
    public abstract partial class InputBase<T> : Microsoft.AspNetCore.Components.ComponentBase
    {
        protected InputBase() { }
        [Microsoft.AspNetCore.Components.ParameterAttribute(CaptureUnmatchedValues=true)]
        public System.Collections.Generic.IReadOnlyDictionary<string, object> AdditionalAttributes { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        protected string CssClass { get { throw null; } }
        protected T CurrentValue { get { throw null; } set { } }
        protected string CurrentValueAsString { get { throw null; } set { } }
        protected Microsoft.AspNetCore.Components.Forms.EditContext EditContext { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        protected Microsoft.AspNetCore.Components.Forms.FieldIdentifier FieldIdentifier { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        [Microsoft.AspNetCore.Components.ParameterAttribute]
        public T Value { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        [Microsoft.AspNetCore.Components.ParameterAttribute]
        public Microsoft.AspNetCore.Components.EventCallback<T> ValueChanged { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        [Microsoft.AspNetCore.Components.ParameterAttribute]
        public System.Linq.Expressions.Expression<System.Func<T>> ValueExpression { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        protected virtual string FormatValueAsString(T value) { throw null; }
        public override System.Threading.Tasks.Task SetParametersAsync(Microsoft.AspNetCore.Components.ParameterView parameters) { throw null; }
        protected abstract bool TryParseValueFromString(string value, out T result, out string validationErrorMessage);
    }
    public partial class InputCheckbox : Microsoft.AspNetCore.Components.Forms.InputBase<bool>
    {
        public InputCheckbox() { }
        protected override void BuildRenderTree(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder builder) { }
        protected override bool TryParseValueFromString(string value, out bool result, out string validationErrorMessage) { throw null; }
    }
    public partial class InputDate<T> : Microsoft.AspNetCore.Components.Forms.InputBase<T>
    {
        public InputDate() { }
        [Microsoft.AspNetCore.Components.ParameterAttribute]
        public string ParsingErrorMessage { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        protected override void BuildRenderTree(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder builder) { }
        protected override string FormatValueAsString(T value) { throw null; }
        protected override bool TryParseValueFromString(string value, out T result, out string validationErrorMessage) { throw null; }
    }
    public partial class InputNumber<T> : Microsoft.AspNetCore.Components.Forms.InputBase<T>
    {
        public InputNumber() { }
        [Microsoft.AspNetCore.Components.ParameterAttribute]
        public string ParsingErrorMessage { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        protected override void BuildRenderTree(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder builder) { }
        protected override string FormatValueAsString(T value) { throw null; }
        protected override bool TryParseValueFromString(string value, out T result, out string validationErrorMessage) { throw null; }
    }
    public partial class InputSelect<T> : Microsoft.AspNetCore.Components.Forms.InputBase<T>
    {
        public InputSelect() { }
        [Microsoft.AspNetCore.Components.ParameterAttribute]
        public Microsoft.AspNetCore.Components.RenderFragment ChildContent { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        protected override void BuildRenderTree(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder builder) { }
        protected override bool TryParseValueFromString(string value, out T result, out string validationErrorMessage) { throw null; }
    }
    public partial class InputText : Microsoft.AspNetCore.Components.Forms.InputBase<string>
    {
        public InputText() { }
        protected override void BuildRenderTree(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder builder) { }
        protected override bool TryParseValueFromString(string value, out string result, out string validationErrorMessage) { throw null; }
    }
    public partial class InputTextArea : Microsoft.AspNetCore.Components.Forms.InputBase<string>
    {
        public InputTextArea() { }
        protected override void BuildRenderTree(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder builder) { }
        protected override bool TryParseValueFromString(string value, out string result, out string validationErrorMessage) { throw null; }
    }
    public partial class ValidationMessage<T> : Microsoft.AspNetCore.Components.ComponentBase, System.IDisposable
    {
        public ValidationMessage() { }
        [Microsoft.AspNetCore.Components.ParameterAttribute(CaptureUnmatchedValues=true)]
        public System.Collections.Generic.IReadOnlyDictionary<string, object> AdditionalAttributes { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        [Microsoft.AspNetCore.Components.ParameterAttribute]
        public System.Linq.Expressions.Expression<System.Func<T>> For { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        protected override void BuildRenderTree(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder builder) { }
        protected virtual void Dispose(bool disposing) { }
        protected override void OnParametersSet() { }
        void System.IDisposable.Dispose() { }
    }
    public partial class ValidationSummary : Microsoft.AspNetCore.Components.ComponentBase, System.IDisposable
    {
        public ValidationSummary() { }
        [Microsoft.AspNetCore.Components.ParameterAttribute(CaptureUnmatchedValues=true)]
        public System.Collections.Generic.IReadOnlyDictionary<string, object> AdditionalAttributes { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        protected override void BuildRenderTree(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder builder) { }
        protected virtual void Dispose(bool disposing) { }
        protected override void OnParametersSet() { }
        void System.IDisposable.Dispose() { }
    }
}
namespace Microsoft.AspNetCore.Components.Routing
{
    public partial class NavLink : Microsoft.AspNetCore.Components.ComponentBase, System.IDisposable
    {
        public NavLink() { }
        [Microsoft.AspNetCore.Components.ParameterAttribute]
        public string ActiveClass { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        [Microsoft.AspNetCore.Components.ParameterAttribute(CaptureUnmatchedValues=true)]
        public System.Collections.Generic.IReadOnlyDictionary<string, object> AdditionalAttributes { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        [Microsoft.AspNetCore.Components.ParameterAttribute]
        public Microsoft.AspNetCore.Components.RenderFragment ChildContent { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        protected string CssClass { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        [Microsoft.AspNetCore.Components.ParameterAttribute]
        public Microsoft.AspNetCore.Components.Routing.NavLinkMatch Match { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        protected override void BuildRenderTree(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder builder) { }
        public void Dispose() { }
        protected override void OnInitialized() { }
        protected override void OnParametersSet() { }
    }
    public enum NavLinkMatch
    {
        Prefix = 0,
        All = 1,
    }
}
namespace Microsoft.AspNetCore.Components.Web
{
    public static partial class RendererRegistryEventDispatcher
    {
        [Microsoft.JSInterop.JSInvokableAttribute("DispatchEvent")]
        public static System.Threading.Tasks.Task DispatchEvent(Microsoft.AspNetCore.Components.Web.RendererRegistryEventDispatcher.BrowserEventDescriptor eventDescriptor, string eventArgsJson) { throw null; }
        public partial class BrowserEventDescriptor
        {
            public BrowserEventDescriptor() { }
            public int BrowserRendererId { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
            public string EventArgsType { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
            public Microsoft.AspNetCore.Components.Rendering.EventFieldInfo EventFieldInfo { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
            public ulong EventHandlerId { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        }
    }
}
