// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Components
{
    [Microsoft.AspNetCore.Components.BindElementAttribute("select", null, "value", "onchange")]
    [Microsoft.AspNetCore.Components.BindElementAttribute("textarea", null, "value", "onchange")]
    [Microsoft.AspNetCore.Components.BindInputElementAttribute("checkbox", null, "checked", "onchange", false, null)]
    [Microsoft.AspNetCore.Components.BindInputElementAttribute("date", null, "value", "onchange", true, "yyyy-MM-dd")]
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
    [Microsoft.AspNetCore.Components.EventHandlerAttribute("onactivate", typeof(Microsoft.AspNetCore.Components.UIEventArgs))]
    [Microsoft.AspNetCore.Components.EventHandlerAttribute("onbeforeactivate", typeof(Microsoft.AspNetCore.Components.UIEventArgs))]
    [Microsoft.AspNetCore.Components.EventHandlerAttribute("onbeforecopy", typeof(Microsoft.AspNetCore.Components.UIEventArgs))]
    [Microsoft.AspNetCore.Components.EventHandlerAttribute("onbeforecut", typeof(Microsoft.AspNetCore.Components.UIEventArgs))]
    [Microsoft.AspNetCore.Components.EventHandlerAttribute("onbeforedeactivate", typeof(Microsoft.AspNetCore.Components.UIEventArgs))]
    [Microsoft.AspNetCore.Components.EventHandlerAttribute("onbeforepaste", typeof(Microsoft.AspNetCore.Components.UIEventArgs))]
    [Microsoft.AspNetCore.Components.EventHandlerAttribute("onblur", typeof(Microsoft.AspNetCore.Components.UIFocusEventArgs))]
    [Microsoft.AspNetCore.Components.EventHandlerAttribute("oncanplay", typeof(Microsoft.AspNetCore.Components.UIEventArgs))]
    [Microsoft.AspNetCore.Components.EventHandlerAttribute("oncanplaythrough", typeof(Microsoft.AspNetCore.Components.UIEventArgs))]
    [Microsoft.AspNetCore.Components.EventHandlerAttribute("onchange", typeof(Microsoft.AspNetCore.Components.UIChangeEventArgs))]
    [Microsoft.AspNetCore.Components.EventHandlerAttribute("onclick", typeof(Microsoft.AspNetCore.Components.UIMouseEventArgs))]
    [Microsoft.AspNetCore.Components.EventHandlerAttribute("oncontextmenu", typeof(Microsoft.AspNetCore.Components.UIMouseEventArgs))]
    [Microsoft.AspNetCore.Components.EventHandlerAttribute("oncopy", typeof(Microsoft.AspNetCore.Components.UIClipboardEventArgs))]
    [Microsoft.AspNetCore.Components.EventHandlerAttribute("oncuechange", typeof(Microsoft.AspNetCore.Components.UIEventArgs))]
    [Microsoft.AspNetCore.Components.EventHandlerAttribute("oncut", typeof(Microsoft.AspNetCore.Components.UIClipboardEventArgs))]
    [Microsoft.AspNetCore.Components.EventHandlerAttribute("ondblclick", typeof(Microsoft.AspNetCore.Components.UIMouseEventArgs))]
    [Microsoft.AspNetCore.Components.EventHandlerAttribute("ondeactivate", typeof(Microsoft.AspNetCore.Components.UIEventArgs))]
    [Microsoft.AspNetCore.Components.EventHandlerAttribute("ondrag", typeof(Microsoft.AspNetCore.Components.UIDragEventArgs))]
    [Microsoft.AspNetCore.Components.EventHandlerAttribute("ondragend", typeof(Microsoft.AspNetCore.Components.UIDragEventArgs))]
    [Microsoft.AspNetCore.Components.EventHandlerAttribute("ondragenter", typeof(Microsoft.AspNetCore.Components.UIDragEventArgs))]
    [Microsoft.AspNetCore.Components.EventHandlerAttribute("ondragleave", typeof(Microsoft.AspNetCore.Components.UIDragEventArgs))]
    [Microsoft.AspNetCore.Components.EventHandlerAttribute("ondragover", typeof(Microsoft.AspNetCore.Components.UIDragEventArgs))]
    [Microsoft.AspNetCore.Components.EventHandlerAttribute("ondragstart", typeof(Microsoft.AspNetCore.Components.UIDragEventArgs))]
    [Microsoft.AspNetCore.Components.EventHandlerAttribute("ondrop", typeof(Microsoft.AspNetCore.Components.UIDragEventArgs))]
    [Microsoft.AspNetCore.Components.EventHandlerAttribute("ondurationchange", typeof(Microsoft.AspNetCore.Components.UIEventArgs))]
    [Microsoft.AspNetCore.Components.EventHandlerAttribute("onemptied", typeof(Microsoft.AspNetCore.Components.UIEventArgs))]
    [Microsoft.AspNetCore.Components.EventHandlerAttribute("onended", typeof(Microsoft.AspNetCore.Components.UIEventArgs))]
    [Microsoft.AspNetCore.Components.EventHandlerAttribute("onerror", typeof(Microsoft.AspNetCore.Components.UIErrorEventArgs))]
    [Microsoft.AspNetCore.Components.EventHandlerAttribute("onfocus", typeof(Microsoft.AspNetCore.Components.UIFocusEventArgs))]
    [Microsoft.AspNetCore.Components.EventHandlerAttribute("onfocusin", typeof(Microsoft.AspNetCore.Components.UIFocusEventArgs))]
    [Microsoft.AspNetCore.Components.EventHandlerAttribute("onfocusout", typeof(Microsoft.AspNetCore.Components.UIFocusEventArgs))]
    [Microsoft.AspNetCore.Components.EventHandlerAttribute("onfullscreenchange", typeof(Microsoft.AspNetCore.Components.UIEventArgs))]
    [Microsoft.AspNetCore.Components.EventHandlerAttribute("onfullscreenerror", typeof(Microsoft.AspNetCore.Components.UIEventArgs))]
    [Microsoft.AspNetCore.Components.EventHandlerAttribute("ongotpointercapture", typeof(Microsoft.AspNetCore.Components.UIPointerEventArgs))]
    [Microsoft.AspNetCore.Components.EventHandlerAttribute("oninput", typeof(Microsoft.AspNetCore.Components.UIChangeEventArgs))]
    [Microsoft.AspNetCore.Components.EventHandlerAttribute("oninvalid", typeof(Microsoft.AspNetCore.Components.UIEventArgs))]
    [Microsoft.AspNetCore.Components.EventHandlerAttribute("onkeydown", typeof(Microsoft.AspNetCore.Components.UIKeyboardEventArgs))]
    [Microsoft.AspNetCore.Components.EventHandlerAttribute("onkeypress", typeof(Microsoft.AspNetCore.Components.UIKeyboardEventArgs))]
    [Microsoft.AspNetCore.Components.EventHandlerAttribute("onkeyup", typeof(Microsoft.AspNetCore.Components.UIKeyboardEventArgs))]
    [Microsoft.AspNetCore.Components.EventHandlerAttribute("onload", typeof(Microsoft.AspNetCore.Components.UIProgressEventArgs))]
    [Microsoft.AspNetCore.Components.EventHandlerAttribute("onloadeddata", typeof(Microsoft.AspNetCore.Components.UIEventArgs))]
    [Microsoft.AspNetCore.Components.EventHandlerAttribute("onloadedmetadata", typeof(Microsoft.AspNetCore.Components.UIEventArgs))]
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
    [Microsoft.AspNetCore.Components.EventHandlerAttribute("onpause", typeof(Microsoft.AspNetCore.Components.UIEventArgs))]
    [Microsoft.AspNetCore.Components.EventHandlerAttribute("onplay", typeof(Microsoft.AspNetCore.Components.UIEventArgs))]
    [Microsoft.AspNetCore.Components.EventHandlerAttribute("onplaying", typeof(Microsoft.AspNetCore.Components.UIEventArgs))]
    [Microsoft.AspNetCore.Components.EventHandlerAttribute("onpointercancel", typeof(Microsoft.AspNetCore.Components.UIPointerEventArgs))]
    [Microsoft.AspNetCore.Components.EventHandlerAttribute("onpointerdown", typeof(Microsoft.AspNetCore.Components.UIPointerEventArgs))]
    [Microsoft.AspNetCore.Components.EventHandlerAttribute("onpointerenter", typeof(Microsoft.AspNetCore.Components.UIPointerEventArgs))]
    [Microsoft.AspNetCore.Components.EventHandlerAttribute("onpointerleave", typeof(Microsoft.AspNetCore.Components.UIPointerEventArgs))]
    [Microsoft.AspNetCore.Components.EventHandlerAttribute("onpointerlockchange", typeof(Microsoft.AspNetCore.Components.UIEventArgs))]
    [Microsoft.AspNetCore.Components.EventHandlerAttribute("onpointerlockerror", typeof(Microsoft.AspNetCore.Components.UIEventArgs))]
    [Microsoft.AspNetCore.Components.EventHandlerAttribute("onpointermove", typeof(Microsoft.AspNetCore.Components.UIPointerEventArgs))]
    [Microsoft.AspNetCore.Components.EventHandlerAttribute("onpointerout", typeof(Microsoft.AspNetCore.Components.UIPointerEventArgs))]
    [Microsoft.AspNetCore.Components.EventHandlerAttribute("onpointerover", typeof(Microsoft.AspNetCore.Components.UIPointerEventArgs))]
    [Microsoft.AspNetCore.Components.EventHandlerAttribute("onpointerup", typeof(Microsoft.AspNetCore.Components.UIPointerEventArgs))]
    [Microsoft.AspNetCore.Components.EventHandlerAttribute("onprogress", typeof(Microsoft.AspNetCore.Components.UIProgressEventArgs))]
    [Microsoft.AspNetCore.Components.EventHandlerAttribute("onratechange", typeof(Microsoft.AspNetCore.Components.UIEventArgs))]
    [Microsoft.AspNetCore.Components.EventHandlerAttribute("onreadystatechange", typeof(Microsoft.AspNetCore.Components.UIEventArgs))]
    [Microsoft.AspNetCore.Components.EventHandlerAttribute("onreset", typeof(Microsoft.AspNetCore.Components.UIEventArgs))]
    [Microsoft.AspNetCore.Components.EventHandlerAttribute("onscroll", typeof(Microsoft.AspNetCore.Components.UIEventArgs))]
    [Microsoft.AspNetCore.Components.EventHandlerAttribute("onseeked", typeof(Microsoft.AspNetCore.Components.UIEventArgs))]
    [Microsoft.AspNetCore.Components.EventHandlerAttribute("onseeking", typeof(Microsoft.AspNetCore.Components.UIEventArgs))]
    [Microsoft.AspNetCore.Components.EventHandlerAttribute("onselect", typeof(Microsoft.AspNetCore.Components.UIEventArgs))]
    [Microsoft.AspNetCore.Components.EventHandlerAttribute("onselectionchange", typeof(Microsoft.AspNetCore.Components.UIEventArgs))]
    [Microsoft.AspNetCore.Components.EventHandlerAttribute("onselectstart", typeof(Microsoft.AspNetCore.Components.UIEventArgs))]
    [Microsoft.AspNetCore.Components.EventHandlerAttribute("onstalled", typeof(Microsoft.AspNetCore.Components.UIEventArgs))]
    [Microsoft.AspNetCore.Components.EventHandlerAttribute("onstop", typeof(Microsoft.AspNetCore.Components.UIEventArgs))]
    [Microsoft.AspNetCore.Components.EventHandlerAttribute("onsubmit", typeof(Microsoft.AspNetCore.Components.UIEventArgs))]
    [Microsoft.AspNetCore.Components.EventHandlerAttribute("onsuspend", typeof(Microsoft.AspNetCore.Components.UIEventArgs))]
    [Microsoft.AspNetCore.Components.EventHandlerAttribute("ontimeout", typeof(Microsoft.AspNetCore.Components.UIProgressEventArgs))]
    [Microsoft.AspNetCore.Components.EventHandlerAttribute("ontimeupdate", typeof(Microsoft.AspNetCore.Components.UIEventArgs))]
    [Microsoft.AspNetCore.Components.EventHandlerAttribute("ontouchcancel", typeof(Microsoft.AspNetCore.Components.UITouchEventArgs))]
    [Microsoft.AspNetCore.Components.EventHandlerAttribute("ontouchend", typeof(Microsoft.AspNetCore.Components.UITouchEventArgs))]
    [Microsoft.AspNetCore.Components.EventHandlerAttribute("ontouchenter", typeof(Microsoft.AspNetCore.Components.UITouchEventArgs))]
    [Microsoft.AspNetCore.Components.EventHandlerAttribute("ontouchleave", typeof(Microsoft.AspNetCore.Components.UITouchEventArgs))]
    [Microsoft.AspNetCore.Components.EventHandlerAttribute("ontouchmove", typeof(Microsoft.AspNetCore.Components.UITouchEventArgs))]
    [Microsoft.AspNetCore.Components.EventHandlerAttribute("ontouchstart", typeof(Microsoft.AspNetCore.Components.UITouchEventArgs))]
    [Microsoft.AspNetCore.Components.EventHandlerAttribute("onvolumechange", typeof(Microsoft.AspNetCore.Components.UIEventArgs))]
    [Microsoft.AspNetCore.Components.EventHandlerAttribute("onwaiting", typeof(Microsoft.AspNetCore.Components.UIEventArgs))]
    [Microsoft.AspNetCore.Components.EventHandlerAttribute("onwheel", typeof(Microsoft.AspNetCore.Components.UIWheelEventArgs))]
    public static partial class EventHandlers
    {
    }
    public partial class UIClipboardEventArgs : Microsoft.AspNetCore.Components.UIEventArgs
    {
        public UIClipboardEventArgs() { }
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
    public partial class UIErrorEventArgs : Microsoft.AspNetCore.Components.UIEventArgs
    {
        public UIErrorEventArgs() { }
        public int Colno { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public string Filename { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public int Lineno { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public string Message { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
    }
    public partial class UIFocusEventArgs : Microsoft.AspNetCore.Components.UIEventArgs
    {
        public UIFocusEventArgs() { }
    }
    public partial class UIKeyboardEventArgs : Microsoft.AspNetCore.Components.UIEventArgs
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
    }
    public partial class UIMouseEventArgs : Microsoft.AspNetCore.Components.UIEventArgs
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
    public partial class UIProgressEventArgs : Microsoft.AspNetCore.Components.UIEventArgs
    {
        public UIProgressEventArgs() { }
        public bool LengthComputable { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public long Loaded { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public long Total { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
    }
    public partial class UITouchEventArgs : Microsoft.AspNetCore.Components.UIEventArgs
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
        public static string FieldClass(this Microsoft.AspNetCore.Components.Forms.EditContext editContext, in Microsoft.AspNetCore.Components.Forms.FieldIdentifier fieldIdentifier) { throw null; }
        public static string FieldClass<TField>(this Microsoft.AspNetCore.Components.Forms.EditContext editContext, System.Linq.Expressions.Expression<System.Func<TField>> accessor) { throw null; }
    }
}
namespace Microsoft.AspNetCore.Components.Routing
{
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
