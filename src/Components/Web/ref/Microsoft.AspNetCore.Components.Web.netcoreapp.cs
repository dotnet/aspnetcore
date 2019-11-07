// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Components
{
    [System.AttributeUsageAttribute(System.AttributeTargets.Class, AllowMultiple=true, Inherited=true)]
    public sealed partial class BindInputElementAttribute : System.Attribute
    {
        public BindInputElementAttribute(string type, string suffix, string valueAttribute, string changeAttribute, bool isInvariantCulture, string format) { }
        public string ChangeAttribute { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public string Format { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public bool IsInvariantCulture { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public string Suffix { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public string Type { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public string ValueAttribute { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
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
        public System.Collections.Generic.IReadOnlyDictionary<string, object> AdditionalAttributes { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        [Microsoft.AspNetCore.Components.ParameterAttribute]
        public Microsoft.AspNetCore.Components.RenderFragment<Microsoft.AspNetCore.Components.Forms.EditContext> ChildContent { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        [Microsoft.AspNetCore.Components.ParameterAttribute]
        public Microsoft.AspNetCore.Components.Forms.EditContext EditContext { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        [Microsoft.AspNetCore.Components.ParameterAttribute]
        public object Model { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        [Microsoft.AspNetCore.Components.ParameterAttribute]
        public Microsoft.AspNetCore.Components.EventCallback<Microsoft.AspNetCore.Components.Forms.EditContext> OnInvalidSubmit { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        [Microsoft.AspNetCore.Components.ParameterAttribute]
        public Microsoft.AspNetCore.Components.EventCallback<Microsoft.AspNetCore.Components.Forms.EditContext> OnSubmit { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        [Microsoft.AspNetCore.Components.ParameterAttribute]
        public Microsoft.AspNetCore.Components.EventCallback<Microsoft.AspNetCore.Components.Forms.EditContext> OnValidSubmit { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        protected override void BuildRenderTree(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder builder) { }
        protected override void OnParametersSet() { }
    }
    public abstract partial class InputBase<TValue> : Microsoft.AspNetCore.Components.ComponentBase, System.IDisposable
    {
        protected InputBase() { }
        [Microsoft.AspNetCore.Components.ParameterAttribute(CaptureUnmatchedValues=true)]
        public System.Collections.Generic.IReadOnlyDictionary<string, object> AdditionalAttributes { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        protected string CssClass { get { throw null; } }
        protected TValue CurrentValue { get { throw null; } set { } }
        protected string CurrentValueAsString { get { throw null; } set { } }
        protected Microsoft.AspNetCore.Components.Forms.EditContext EditContext { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        protected Microsoft.AspNetCore.Components.Forms.FieldIdentifier FieldIdentifier { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        [Microsoft.AspNetCore.Components.ParameterAttribute]
        public TValue Value { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        [Microsoft.AspNetCore.Components.ParameterAttribute]
        public Microsoft.AspNetCore.Components.EventCallback<TValue> ValueChanged { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        [Microsoft.AspNetCore.Components.ParameterAttribute]
        public System.Linq.Expressions.Expression<System.Func<TValue>> ValueExpression { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        protected virtual void Dispose(bool disposing) { }
        protected virtual string FormatValueAsString(TValue value) { throw null; }
        public override System.Threading.Tasks.Task SetParametersAsync(Microsoft.AspNetCore.Components.ParameterView parameters) { throw null; }
        void System.IDisposable.Dispose() { }
        protected abstract bool TryParseValueFromString(string value, out TValue result, out string validationErrorMessage);
    }
    public partial class InputCheckbox : Microsoft.AspNetCore.Components.Forms.InputBase<bool>
    {
        public InputCheckbox() { }
        protected override void BuildRenderTree(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder builder) { }
        protected override bool TryParseValueFromString(string value, out bool result, out string validationErrorMessage) { throw null; }
    }
    public partial class InputDate<TValue> : Microsoft.AspNetCore.Components.Forms.InputBase<TValue>
    {
        public InputDate() { }
        [Microsoft.AspNetCore.Components.ParameterAttribute]
        public string ParsingErrorMessage { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        protected override void BuildRenderTree(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder builder) { }
        protected override string FormatValueAsString(TValue value) { throw null; }
        protected override bool TryParseValueFromString(string value, out TValue result, out string validationErrorMessage) { throw null; }
    }
    public partial class InputNumber<TValue> : Microsoft.AspNetCore.Components.Forms.InputBase<TValue>
    {
        public InputNumber() { }
        [Microsoft.AspNetCore.Components.ParameterAttribute]
        public string ParsingErrorMessage { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        protected override void BuildRenderTree(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder builder) { }
        protected override string FormatValueAsString(TValue value) { throw null; }
        protected override bool TryParseValueFromString(string value, out TValue result, out string validationErrorMessage) { throw null; }
    }
    public partial class InputSelect<TValue> : Microsoft.AspNetCore.Components.Forms.InputBase<TValue>
    {
        public InputSelect() { }
        [Microsoft.AspNetCore.Components.ParameterAttribute]
        public Microsoft.AspNetCore.Components.RenderFragment ChildContent { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        protected override void BuildRenderTree(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder builder) { }
        protected override bool TryParseValueFromString(string value, out TValue result, out string validationErrorMessage) { throw null; }
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
    public partial class ValidationMessage<TValue> : Microsoft.AspNetCore.Components.ComponentBase, System.IDisposable
    {
        public ValidationMessage() { }
        [Microsoft.AspNetCore.Components.ParameterAttribute(CaptureUnmatchedValues=true)]
        public System.Collections.Generic.IReadOnlyDictionary<string, object> AdditionalAttributes { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        [Microsoft.AspNetCore.Components.ParameterAttribute]
        public System.Linq.Expressions.Expression<System.Func<TValue>> For { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        protected override void BuildRenderTree(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder builder) { }
        protected virtual void Dispose(bool disposing) { }
        protected override void OnParametersSet() { }
        void System.IDisposable.Dispose() { }
    }
    public partial class ValidationSummary : Microsoft.AspNetCore.Components.ComponentBase, System.IDisposable
    {
        public ValidationSummary() { }
        [Microsoft.AspNetCore.Components.ParameterAttribute(CaptureUnmatchedValues=true)]
        public System.Collections.Generic.IReadOnlyDictionary<string, object> AdditionalAttributes { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        [Microsoft.AspNetCore.Components.ParameterAttribute]
        public object Model { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        protected override void BuildRenderTree(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder builder) { }
        protected virtual void Dispose(bool disposing) { }
        protected override void OnParametersSet() { }
        void System.IDisposable.Dispose() { }
    }
}
namespace Microsoft.AspNetCore.Components.RenderTree
{
    public sealed partial class WebEventDescriptor
    {
        public WebEventDescriptor() { }
        public int BrowserRendererId { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public string EventArgsType { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public Microsoft.AspNetCore.Components.RenderTree.EventFieldInfo EventFieldInfo { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public ulong EventHandlerId { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
    }
}
namespace Microsoft.AspNetCore.Components.Routing
{
    public partial class NavLink : Microsoft.AspNetCore.Components.ComponentBase, System.IDisposable
    {
        public NavLink() { }
        [Microsoft.AspNetCore.Components.ParameterAttribute]
        public string ActiveClass { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        [Microsoft.AspNetCore.Components.ParameterAttribute(CaptureUnmatchedValues=true)]
        public System.Collections.Generic.IReadOnlyDictionary<string, object> AdditionalAttributes { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        [Microsoft.AspNetCore.Components.ParameterAttribute]
        public Microsoft.AspNetCore.Components.RenderFragment ChildContent { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        protected string CssClass { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        [Microsoft.AspNetCore.Components.ParameterAttribute]
        public Microsoft.AspNetCore.Components.Routing.NavLinkMatch Match { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
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
    [Microsoft.AspNetCore.Components.BindElementAttribute("select", null, "value", "onchange")]
    [Microsoft.AspNetCore.Components.BindElementAttribute("textarea", null, "value", "onchange")]
    [Microsoft.AspNetCore.Components.BindInputElementAttribute("checkbox", null, "checked", "onchange", false, null)]
    [Microsoft.AspNetCore.Components.BindInputElementAttribute("date", "value", "value", "onchange", true, "yyyy-MM-dd")]
    [Microsoft.AspNetCore.Components.BindInputElementAttribute("date", null, "value", "onchange", true, "yyyy-MM-dd")]
    [Microsoft.AspNetCore.Components.BindInputElementAttribute("datetime-local", "value", "value", "onchange", true, "yyyy-MM-ddTHH:mm:ss")]
    [Microsoft.AspNetCore.Components.BindInputElementAttribute("datetime-local", null, "value", "onchange", true, "yyyy-MM-ddTHH:mm:ss")]
    [Microsoft.AspNetCore.Components.BindInputElementAttribute("month", "value", "value", "onchange", true, "yyyy-MM")]
    [Microsoft.AspNetCore.Components.BindInputElementAttribute("month", null, "value", "onchange", true, "yyyy-MM")]
    [Microsoft.AspNetCore.Components.BindInputElementAttribute("number", "value", "value", "onchange", true, null)]
    [Microsoft.AspNetCore.Components.BindInputElementAttribute("number", null, "value", "onchange", true, null)]
    [Microsoft.AspNetCore.Components.BindInputElementAttribute("text", null, "value", "onchange", false, null)]
    [Microsoft.AspNetCore.Components.BindInputElementAttribute("time", "value", "value", "onchange", true, "HH:mm:ss")]
    [Microsoft.AspNetCore.Components.BindInputElementAttribute("time", null, "value", "onchange", true, "HH:mm:ss")]
    [Microsoft.AspNetCore.Components.BindInputElementAttribute(null, "value", "value", "onchange", false, null)]
    [Microsoft.AspNetCore.Components.BindInputElementAttribute(null, null, "value", "onchange", false, null)]
    public static partial class BindAttributes
    {
    }
    public partial class ClipboardEventArgs : System.EventArgs
    {
        public ClipboardEventArgs() { }
        public string Type { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
    }
    public partial class DataTransfer
    {
        public DataTransfer() { }
        public string DropEffect { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public string EffectAllowed { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public string[] Files { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public Microsoft.AspNetCore.Components.Web.DataTransferItem[] Items { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public string[] Types { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
    }
    public partial class DataTransferItem
    {
        public DataTransferItem() { }
        public string Kind { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public string Type { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
    }
    public partial class DragEventArgs : Microsoft.AspNetCore.Components.Web.MouseEventArgs
    {
        public DragEventArgs() { }
        public Microsoft.AspNetCore.Components.Web.DataTransfer DataTransfer { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
    }
    public partial class ErrorEventArgs : System.EventArgs
    {
        public ErrorEventArgs() { }
        public int Colno { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public string Filename { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public int Lineno { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public string Message { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public string Type { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
    }
    [Microsoft.AspNetCore.Components.EventHandlerAttribute("onabort", typeof(Microsoft.AspNetCore.Components.Web.ProgressEventArgs), true, true)]
    [Microsoft.AspNetCore.Components.EventHandlerAttribute("onactivate", typeof(System.EventArgs), true, true)]
    [Microsoft.AspNetCore.Components.EventHandlerAttribute("onbeforeactivate", typeof(System.EventArgs), true, true)]
    [Microsoft.AspNetCore.Components.EventHandlerAttribute("onbeforecopy", typeof(System.EventArgs), true, true)]
    [Microsoft.AspNetCore.Components.EventHandlerAttribute("onbeforecut", typeof(System.EventArgs), true, true)]
    [Microsoft.AspNetCore.Components.EventHandlerAttribute("onbeforedeactivate", typeof(System.EventArgs), true, true)]
    [Microsoft.AspNetCore.Components.EventHandlerAttribute("onbeforepaste", typeof(System.EventArgs), true, true)]
    [Microsoft.AspNetCore.Components.EventHandlerAttribute("onblur", typeof(Microsoft.AspNetCore.Components.Web.FocusEventArgs), true, true)]
    [Microsoft.AspNetCore.Components.EventHandlerAttribute("oncanplay", typeof(System.EventArgs), true, true)]
    [Microsoft.AspNetCore.Components.EventHandlerAttribute("oncanplaythrough", typeof(System.EventArgs), true, true)]
    [Microsoft.AspNetCore.Components.EventHandlerAttribute("onchange", typeof(Microsoft.AspNetCore.Components.ChangeEventArgs), true, true)]
    [Microsoft.AspNetCore.Components.EventHandlerAttribute("onclick", typeof(Microsoft.AspNetCore.Components.Web.MouseEventArgs), true, true)]
    [Microsoft.AspNetCore.Components.EventHandlerAttribute("oncontextmenu", typeof(Microsoft.AspNetCore.Components.Web.MouseEventArgs), true, true)]
    [Microsoft.AspNetCore.Components.EventHandlerAttribute("oncopy", typeof(Microsoft.AspNetCore.Components.Web.ClipboardEventArgs), true, true)]
    [Microsoft.AspNetCore.Components.EventHandlerAttribute("oncuechange", typeof(System.EventArgs), true, true)]
    [Microsoft.AspNetCore.Components.EventHandlerAttribute("oncut", typeof(Microsoft.AspNetCore.Components.Web.ClipboardEventArgs), true, true)]
    [Microsoft.AspNetCore.Components.EventHandlerAttribute("ondblclick", typeof(Microsoft.AspNetCore.Components.Web.MouseEventArgs), true, true)]
    [Microsoft.AspNetCore.Components.EventHandlerAttribute("ondeactivate", typeof(System.EventArgs), true, true)]
    [Microsoft.AspNetCore.Components.EventHandlerAttribute("ondrag", typeof(Microsoft.AspNetCore.Components.Web.DragEventArgs), true, true)]
    [Microsoft.AspNetCore.Components.EventHandlerAttribute("ondragend", typeof(Microsoft.AspNetCore.Components.Web.DragEventArgs), true, true)]
    [Microsoft.AspNetCore.Components.EventHandlerAttribute("ondragenter", typeof(Microsoft.AspNetCore.Components.Web.DragEventArgs), true, true)]
    [Microsoft.AspNetCore.Components.EventHandlerAttribute("ondragleave", typeof(Microsoft.AspNetCore.Components.Web.DragEventArgs), true, true)]
    [Microsoft.AspNetCore.Components.EventHandlerAttribute("ondragover", typeof(Microsoft.AspNetCore.Components.Web.DragEventArgs), true, true)]
    [Microsoft.AspNetCore.Components.EventHandlerAttribute("ondragstart", typeof(Microsoft.AspNetCore.Components.Web.DragEventArgs), true, true)]
    [Microsoft.AspNetCore.Components.EventHandlerAttribute("ondrop", typeof(Microsoft.AspNetCore.Components.Web.DragEventArgs), true, true)]
    [Microsoft.AspNetCore.Components.EventHandlerAttribute("ondurationchange", typeof(System.EventArgs), true, true)]
    [Microsoft.AspNetCore.Components.EventHandlerAttribute("onemptied", typeof(System.EventArgs), true, true)]
    [Microsoft.AspNetCore.Components.EventHandlerAttribute("onended", typeof(System.EventArgs), true, true)]
    [Microsoft.AspNetCore.Components.EventHandlerAttribute("onerror", typeof(Microsoft.AspNetCore.Components.Web.ErrorEventArgs), true, true)]
    [Microsoft.AspNetCore.Components.EventHandlerAttribute("onfocus", typeof(Microsoft.AspNetCore.Components.Web.FocusEventArgs), true, true)]
    [Microsoft.AspNetCore.Components.EventHandlerAttribute("onfocusin", typeof(Microsoft.AspNetCore.Components.Web.FocusEventArgs), true, true)]
    [Microsoft.AspNetCore.Components.EventHandlerAttribute("onfocusout", typeof(Microsoft.AspNetCore.Components.Web.FocusEventArgs), true, true)]
    [Microsoft.AspNetCore.Components.EventHandlerAttribute("onfullscreenchange", typeof(System.EventArgs), true, true)]
    [Microsoft.AspNetCore.Components.EventHandlerAttribute("onfullscreenerror", typeof(System.EventArgs), true, true)]
    [Microsoft.AspNetCore.Components.EventHandlerAttribute("ongotpointercapture", typeof(Microsoft.AspNetCore.Components.Web.PointerEventArgs), true, true)]
    [Microsoft.AspNetCore.Components.EventHandlerAttribute("oninput", typeof(Microsoft.AspNetCore.Components.ChangeEventArgs), true, true)]
    [Microsoft.AspNetCore.Components.EventHandlerAttribute("oninvalid", typeof(System.EventArgs), true, true)]
    [Microsoft.AspNetCore.Components.EventHandlerAttribute("onkeydown", typeof(Microsoft.AspNetCore.Components.Web.KeyboardEventArgs), true, true)]
    [Microsoft.AspNetCore.Components.EventHandlerAttribute("onkeypress", typeof(Microsoft.AspNetCore.Components.Web.KeyboardEventArgs), true, true)]
    [Microsoft.AspNetCore.Components.EventHandlerAttribute("onkeyup", typeof(Microsoft.AspNetCore.Components.Web.KeyboardEventArgs), true, true)]
    [Microsoft.AspNetCore.Components.EventHandlerAttribute("onload", typeof(Microsoft.AspNetCore.Components.Web.ProgressEventArgs), true, true)]
    [Microsoft.AspNetCore.Components.EventHandlerAttribute("onloadeddata", typeof(System.EventArgs), true, true)]
    [Microsoft.AspNetCore.Components.EventHandlerAttribute("onloadedmetadata", typeof(System.EventArgs), true, true)]
    [Microsoft.AspNetCore.Components.EventHandlerAttribute("onloadend", typeof(Microsoft.AspNetCore.Components.Web.ProgressEventArgs), true, true)]
    [Microsoft.AspNetCore.Components.EventHandlerAttribute("onloadstart", typeof(Microsoft.AspNetCore.Components.Web.ProgressEventArgs), true, true)]
    [Microsoft.AspNetCore.Components.EventHandlerAttribute("onlostpointercapture", typeof(Microsoft.AspNetCore.Components.Web.PointerEventArgs), true, true)]
    [Microsoft.AspNetCore.Components.EventHandlerAttribute("onmousedown", typeof(Microsoft.AspNetCore.Components.Web.MouseEventArgs), true, true)]
    [Microsoft.AspNetCore.Components.EventHandlerAttribute("onmousemove", typeof(Microsoft.AspNetCore.Components.Web.MouseEventArgs), true, true)]
    [Microsoft.AspNetCore.Components.EventHandlerAttribute("onmouseout", typeof(Microsoft.AspNetCore.Components.Web.MouseEventArgs), true, true)]
    [Microsoft.AspNetCore.Components.EventHandlerAttribute("onmouseover", typeof(Microsoft.AspNetCore.Components.Web.MouseEventArgs), true, true)]
    [Microsoft.AspNetCore.Components.EventHandlerAttribute("onmouseup", typeof(Microsoft.AspNetCore.Components.Web.MouseEventArgs), true, true)]
    [Microsoft.AspNetCore.Components.EventHandlerAttribute("onmousewheel", typeof(Microsoft.AspNetCore.Components.Web.WheelEventArgs), true, true)]
    [Microsoft.AspNetCore.Components.EventHandlerAttribute("onpaste", typeof(Microsoft.AspNetCore.Components.Web.ClipboardEventArgs), true, true)]
    [Microsoft.AspNetCore.Components.EventHandlerAttribute("onpause", typeof(System.EventArgs), true, true)]
    [Microsoft.AspNetCore.Components.EventHandlerAttribute("onplay", typeof(System.EventArgs), true, true)]
    [Microsoft.AspNetCore.Components.EventHandlerAttribute("onplaying", typeof(System.EventArgs), true, true)]
    [Microsoft.AspNetCore.Components.EventHandlerAttribute("onpointercancel", typeof(Microsoft.AspNetCore.Components.Web.PointerEventArgs), true, true)]
    [Microsoft.AspNetCore.Components.EventHandlerAttribute("onpointerdown", typeof(Microsoft.AspNetCore.Components.Web.PointerEventArgs), true, true)]
    [Microsoft.AspNetCore.Components.EventHandlerAttribute("onpointerenter", typeof(Microsoft.AspNetCore.Components.Web.PointerEventArgs), true, true)]
    [Microsoft.AspNetCore.Components.EventHandlerAttribute("onpointerleave", typeof(Microsoft.AspNetCore.Components.Web.PointerEventArgs), true, true)]
    [Microsoft.AspNetCore.Components.EventHandlerAttribute("onpointerlockchange", typeof(System.EventArgs), true, true)]
    [Microsoft.AspNetCore.Components.EventHandlerAttribute("onpointerlockerror", typeof(System.EventArgs), true, true)]
    [Microsoft.AspNetCore.Components.EventHandlerAttribute("onpointermove", typeof(Microsoft.AspNetCore.Components.Web.PointerEventArgs), true, true)]
    [Microsoft.AspNetCore.Components.EventHandlerAttribute("onpointerout", typeof(Microsoft.AspNetCore.Components.Web.PointerEventArgs), true, true)]
    [Microsoft.AspNetCore.Components.EventHandlerAttribute("onpointerover", typeof(Microsoft.AspNetCore.Components.Web.PointerEventArgs), true, true)]
    [Microsoft.AspNetCore.Components.EventHandlerAttribute("onpointerup", typeof(Microsoft.AspNetCore.Components.Web.PointerEventArgs), true, true)]
    [Microsoft.AspNetCore.Components.EventHandlerAttribute("onprogress", typeof(Microsoft.AspNetCore.Components.Web.ProgressEventArgs), true, true)]
    [Microsoft.AspNetCore.Components.EventHandlerAttribute("onratechange", typeof(System.EventArgs), true, true)]
    [Microsoft.AspNetCore.Components.EventHandlerAttribute("onreadystatechange", typeof(System.EventArgs), true, true)]
    [Microsoft.AspNetCore.Components.EventHandlerAttribute("onreset", typeof(System.EventArgs), true, true)]
    [Microsoft.AspNetCore.Components.EventHandlerAttribute("onscroll", typeof(System.EventArgs), true, true)]
    [Microsoft.AspNetCore.Components.EventHandlerAttribute("onseeked", typeof(System.EventArgs), true, true)]
    [Microsoft.AspNetCore.Components.EventHandlerAttribute("onseeking", typeof(System.EventArgs), true, true)]
    [Microsoft.AspNetCore.Components.EventHandlerAttribute("onselect", typeof(System.EventArgs), true, true)]
    [Microsoft.AspNetCore.Components.EventHandlerAttribute("onselectionchange", typeof(System.EventArgs), true, true)]
    [Microsoft.AspNetCore.Components.EventHandlerAttribute("onselectstart", typeof(System.EventArgs), true, true)]
    [Microsoft.AspNetCore.Components.EventHandlerAttribute("onstalled", typeof(System.EventArgs), true, true)]
    [Microsoft.AspNetCore.Components.EventHandlerAttribute("onstop", typeof(System.EventArgs), true, true)]
    [Microsoft.AspNetCore.Components.EventHandlerAttribute("onsubmit", typeof(System.EventArgs), true, true)]
    [Microsoft.AspNetCore.Components.EventHandlerAttribute("onsuspend", typeof(System.EventArgs), true, true)]
    [Microsoft.AspNetCore.Components.EventHandlerAttribute("ontimeout", typeof(Microsoft.AspNetCore.Components.Web.ProgressEventArgs), true, true)]
    [Microsoft.AspNetCore.Components.EventHandlerAttribute("ontimeupdate", typeof(System.EventArgs), true, true)]
    [Microsoft.AspNetCore.Components.EventHandlerAttribute("ontouchcancel", typeof(Microsoft.AspNetCore.Components.Web.TouchEventArgs), true, true)]
    [Microsoft.AspNetCore.Components.EventHandlerAttribute("ontouchend", typeof(Microsoft.AspNetCore.Components.Web.TouchEventArgs), true, true)]
    [Microsoft.AspNetCore.Components.EventHandlerAttribute("ontouchenter", typeof(Microsoft.AspNetCore.Components.Web.TouchEventArgs), true, true)]
    [Microsoft.AspNetCore.Components.EventHandlerAttribute("ontouchleave", typeof(Microsoft.AspNetCore.Components.Web.TouchEventArgs), true, true)]
    [Microsoft.AspNetCore.Components.EventHandlerAttribute("ontouchmove", typeof(Microsoft.AspNetCore.Components.Web.TouchEventArgs), true, true)]
    [Microsoft.AspNetCore.Components.EventHandlerAttribute("ontouchstart", typeof(Microsoft.AspNetCore.Components.Web.TouchEventArgs), true, true)]
    [Microsoft.AspNetCore.Components.EventHandlerAttribute("onvolumechange", typeof(System.EventArgs), true, true)]
    [Microsoft.AspNetCore.Components.EventHandlerAttribute("onwaiting", typeof(System.EventArgs), true, true)]
    [Microsoft.AspNetCore.Components.EventHandlerAttribute("onwheel", typeof(Microsoft.AspNetCore.Components.Web.WheelEventArgs), true, true)]
    public static partial class EventHandlers
    {
    }
    public partial class FocusEventArgs : System.EventArgs
    {
        public FocusEventArgs() { }
        public string Type { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
    }
    public partial class KeyboardEventArgs : System.EventArgs
    {
        public KeyboardEventArgs() { }
        public bool AltKey { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public string Code { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public bool CtrlKey { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public string Key { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public float Location { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public bool MetaKey { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public bool Repeat { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public bool ShiftKey { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public string Type { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
    }
    public partial class MouseEventArgs : System.EventArgs
    {
        public MouseEventArgs() { }
        public bool AltKey { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public long Button { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public long Buttons { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public double ClientX { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public double ClientY { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public bool CtrlKey { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public long Detail { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public bool MetaKey { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public double ScreenX { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public double ScreenY { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public bool ShiftKey { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public string Type { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
    }
    public partial class PointerEventArgs : Microsoft.AspNetCore.Components.Web.MouseEventArgs
    {
        public PointerEventArgs() { }
        public float Height { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public bool IsPrimary { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public long PointerId { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public string PointerType { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public float Pressure { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public float TiltX { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public float TiltY { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public float Width { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
    }
    public partial class ProgressEventArgs : System.EventArgs
    {
        public ProgressEventArgs() { }
        public bool LengthComputable { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public long Loaded { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public long Total { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public string Type { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
    }
    public partial class TouchEventArgs : System.EventArgs
    {
        public TouchEventArgs() { }
        public bool AltKey { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public Microsoft.AspNetCore.Components.Web.TouchPoint[] ChangedTouches { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public bool CtrlKey { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public long Detail { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public bool MetaKey { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public bool ShiftKey { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public Microsoft.AspNetCore.Components.Web.TouchPoint[] TargetTouches { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public Microsoft.AspNetCore.Components.Web.TouchPoint[] Touches { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public string Type { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
    }
    public partial class TouchPoint
    {
        public TouchPoint() { }
        public double ClientX { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public double ClientY { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public long Identifier { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public double PageX { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public double PageY { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public double ScreenX { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public double ScreenY { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
    }
    public static partial class WebEventCallbackFactoryEventArgsExtensions
    {
        public static Microsoft.AspNetCore.Components.EventCallback<Microsoft.AspNetCore.Components.Web.ClipboardEventArgs> Create(this Microsoft.AspNetCore.Components.EventCallbackFactory factory, object receiver, System.Action<Microsoft.AspNetCore.Components.Web.ClipboardEventArgs> callback) { throw null; }
        public static Microsoft.AspNetCore.Components.EventCallback<Microsoft.AspNetCore.Components.Web.DragEventArgs> Create(this Microsoft.AspNetCore.Components.EventCallbackFactory factory, object receiver, System.Action<Microsoft.AspNetCore.Components.Web.DragEventArgs> callback) { throw null; }
        public static Microsoft.AspNetCore.Components.EventCallback<Microsoft.AspNetCore.Components.Web.ErrorEventArgs> Create(this Microsoft.AspNetCore.Components.EventCallbackFactory factory, object receiver, System.Action<Microsoft.AspNetCore.Components.Web.ErrorEventArgs> callback) { throw null; }
        public static Microsoft.AspNetCore.Components.EventCallback<Microsoft.AspNetCore.Components.Web.FocusEventArgs> Create(this Microsoft.AspNetCore.Components.EventCallbackFactory factory, object receiver, System.Action<Microsoft.AspNetCore.Components.Web.FocusEventArgs> callback) { throw null; }
        public static Microsoft.AspNetCore.Components.EventCallback<Microsoft.AspNetCore.Components.Web.KeyboardEventArgs> Create(this Microsoft.AspNetCore.Components.EventCallbackFactory factory, object receiver, System.Action<Microsoft.AspNetCore.Components.Web.KeyboardEventArgs> callback) { throw null; }
        public static Microsoft.AspNetCore.Components.EventCallback<Microsoft.AspNetCore.Components.Web.MouseEventArgs> Create(this Microsoft.AspNetCore.Components.EventCallbackFactory factory, object receiver, System.Action<Microsoft.AspNetCore.Components.Web.MouseEventArgs> callback) { throw null; }
        public static Microsoft.AspNetCore.Components.EventCallback<Microsoft.AspNetCore.Components.Web.PointerEventArgs> Create(this Microsoft.AspNetCore.Components.EventCallbackFactory factory, object receiver, System.Action<Microsoft.AspNetCore.Components.Web.PointerEventArgs> callback) { throw null; }
        public static Microsoft.AspNetCore.Components.EventCallback<Microsoft.AspNetCore.Components.Web.ProgressEventArgs> Create(this Microsoft.AspNetCore.Components.EventCallbackFactory factory, object receiver, System.Action<Microsoft.AspNetCore.Components.Web.ProgressEventArgs> callback) { throw null; }
        public static Microsoft.AspNetCore.Components.EventCallback<Microsoft.AspNetCore.Components.Web.TouchEventArgs> Create(this Microsoft.AspNetCore.Components.EventCallbackFactory factory, object receiver, System.Action<Microsoft.AspNetCore.Components.Web.TouchEventArgs> callback) { throw null; }
        public static Microsoft.AspNetCore.Components.EventCallback<Microsoft.AspNetCore.Components.Web.WheelEventArgs> Create(this Microsoft.AspNetCore.Components.EventCallbackFactory factory, object receiver, System.Action<Microsoft.AspNetCore.Components.Web.WheelEventArgs> callback) { throw null; }
        public static Microsoft.AspNetCore.Components.EventCallback<Microsoft.AspNetCore.Components.Web.ClipboardEventArgs> Create(this Microsoft.AspNetCore.Components.EventCallbackFactory factory, object receiver, System.Func<Microsoft.AspNetCore.Components.Web.ClipboardEventArgs, System.Threading.Tasks.Task> callback) { throw null; }
        public static Microsoft.AspNetCore.Components.EventCallback<Microsoft.AspNetCore.Components.Web.DragEventArgs> Create(this Microsoft.AspNetCore.Components.EventCallbackFactory factory, object receiver, System.Func<Microsoft.AspNetCore.Components.Web.DragEventArgs, System.Threading.Tasks.Task> callback) { throw null; }
        public static Microsoft.AspNetCore.Components.EventCallback<Microsoft.AspNetCore.Components.Web.ErrorEventArgs> Create(this Microsoft.AspNetCore.Components.EventCallbackFactory factory, object receiver, System.Func<Microsoft.AspNetCore.Components.Web.ErrorEventArgs, System.Threading.Tasks.Task> callback) { throw null; }
        public static Microsoft.AspNetCore.Components.EventCallback<Microsoft.AspNetCore.Components.Web.FocusEventArgs> Create(this Microsoft.AspNetCore.Components.EventCallbackFactory factory, object receiver, System.Func<Microsoft.AspNetCore.Components.Web.FocusEventArgs, System.Threading.Tasks.Task> callback) { throw null; }
        public static Microsoft.AspNetCore.Components.EventCallback<Microsoft.AspNetCore.Components.Web.KeyboardEventArgs> Create(this Microsoft.AspNetCore.Components.EventCallbackFactory factory, object receiver, System.Func<Microsoft.AspNetCore.Components.Web.KeyboardEventArgs, System.Threading.Tasks.Task> callback) { throw null; }
        public static Microsoft.AspNetCore.Components.EventCallback<Microsoft.AspNetCore.Components.Web.MouseEventArgs> Create(this Microsoft.AspNetCore.Components.EventCallbackFactory factory, object receiver, System.Func<Microsoft.AspNetCore.Components.Web.MouseEventArgs, System.Threading.Tasks.Task> callback) { throw null; }
        public static Microsoft.AspNetCore.Components.EventCallback<Microsoft.AspNetCore.Components.Web.PointerEventArgs> Create(this Microsoft.AspNetCore.Components.EventCallbackFactory factory, object receiver, System.Func<Microsoft.AspNetCore.Components.Web.PointerEventArgs, System.Threading.Tasks.Task> callback) { throw null; }
        public static Microsoft.AspNetCore.Components.EventCallback<Microsoft.AspNetCore.Components.Web.ProgressEventArgs> Create(this Microsoft.AspNetCore.Components.EventCallbackFactory factory, object receiver, System.Func<Microsoft.AspNetCore.Components.Web.ProgressEventArgs, System.Threading.Tasks.Task> callback) { throw null; }
        public static Microsoft.AspNetCore.Components.EventCallback<Microsoft.AspNetCore.Components.Web.TouchEventArgs> Create(this Microsoft.AspNetCore.Components.EventCallbackFactory factory, object receiver, System.Func<Microsoft.AspNetCore.Components.Web.TouchEventArgs, System.Threading.Tasks.Task> callback) { throw null; }
        public static Microsoft.AspNetCore.Components.EventCallback<Microsoft.AspNetCore.Components.Web.WheelEventArgs> Create(this Microsoft.AspNetCore.Components.EventCallbackFactory factory, object receiver, System.Func<Microsoft.AspNetCore.Components.Web.WheelEventArgs, System.Threading.Tasks.Task> callback) { throw null; }
    }
    public static partial class WebRenderTreeBuilderExtensions
    {
        public static void AddEventPreventDefaultAttribute(this Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder builder, int sequence, string eventName, bool value) { }
        public static void AddEventStopPropagationAttribute(this Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder builder, int sequence, string eventName, bool value) { }
    }
    public partial class WheelEventArgs : Microsoft.AspNetCore.Components.Web.MouseEventArgs
    {
        public WheelEventArgs() { }
        public long DeltaMode { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public double DeltaX { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public double DeltaY { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public double DeltaZ { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
    }
}
