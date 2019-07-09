// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Components
{
    public partial class AuthenticationState
    {
        public AuthenticationState(System.Security.Claims.ClaimsPrincipal user) { }
        public System.Security.Claims.ClaimsPrincipal User { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
    }
    public delegate void AuthenticationStateChangedHandler(System.Threading.Tasks.Task<Microsoft.AspNetCore.Components.AuthenticationState> task);
    public abstract partial class AuthenticationStateProvider
    {
        protected AuthenticationStateProvider() { }
        public event Microsoft.AspNetCore.Components.AuthenticationStateChangedHandler AuthenticationStateChanged { add { } remove { } }
        public abstract System.Threading.Tasks.Task<Microsoft.AspNetCore.Components.AuthenticationState> GetAuthenticationStateAsync();
        protected void NotifyAuthenticationStateChanged(System.Threading.Tasks.Task<Microsoft.AspNetCore.Components.AuthenticationState> task) { }
    }
    [Microsoft.AspNetCore.Components.BindElementAttribute("select", null, "value", "onchange")]
    [Microsoft.AspNetCore.Components.BindElementAttribute("textarea", null, "value", "onchange")]
    [Microsoft.AspNetCore.Components.BindInputElementAttribute("checkbox", null, "checked", "onchange")]
    [Microsoft.AspNetCore.Components.BindInputElementAttribute("text", null, "value", "onchange")]
    [Microsoft.AspNetCore.Components.BindInputElementAttribute(null, "value", "value", "onchange")]
    [Microsoft.AspNetCore.Components.BindInputElementAttribute(null, null, "value", "onchange")]
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
        public BindInputElementAttribute(string type, string suffix, string valueAttribute, string changeAttribute) { }
        public string ChangeAttribute { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public string Suffix { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public string Type { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public string ValueAttribute { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
    }
    public static partial class BindMethods
    {
        public static string GetValue(System.DateTime value, string format) { throw null; }
        public static string GetValue(System.DateTimeOffset value, string format) { throw null; }
        public static string GetValue(System.DateTimeOffset? value, string format) { throw null; }
        public static string GetValue(System.DateTime? value, string format) { throw null; }
        public static T GetValue<T>(T value) { throw null; }
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Property, AllowMultiple=false, Inherited=true)]
    public sealed partial class CascadingParameterAttribute : System.Attribute
    {
        public CascadingParameterAttribute() { }
        public string Name { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
    }
    public abstract partial class ComponentBase : Microsoft.AspNetCore.Components.IComponent, Microsoft.AspNetCore.Components.IHandleAfterRender, Microsoft.AspNetCore.Components.IHandleEvent
    {
        public ComponentBase() { }
        protected virtual void BuildRenderTree(Microsoft.AspNetCore.Components.RenderTree.RenderTreeBuilder builder) { }
        protected System.Threading.Tasks.Task InvokeAsync(System.Action workItem) { throw null; }
        protected System.Threading.Tasks.Task InvokeAsync(System.Func<System.Threading.Tasks.Task> workItem) { throw null; }
        void Microsoft.AspNetCore.Components.IComponent.Configure(Microsoft.AspNetCore.Components.RenderHandle renderHandle) { }
        System.Threading.Tasks.Task Microsoft.AspNetCore.Components.IHandleAfterRender.OnAfterRenderAsync() { throw null; }
        System.Threading.Tasks.Task Microsoft.AspNetCore.Components.IHandleEvent.HandleEventAsync(Microsoft.AspNetCore.Components.EventCallbackWorkItem callback, object arg) { throw null; }
        protected virtual void OnAfterRender() { }
        protected virtual System.Threading.Tasks.Task OnAfterRenderAsync() { throw null; }
        protected virtual void OnInitialized() { }
        protected virtual System.Threading.Tasks.Task OnInitializedAsync() { throw null; }
        protected virtual void OnParametersSet() { }
        protected virtual System.Threading.Tasks.Task OnParametersSetAsync() { throw null; }
        public virtual System.Threading.Tasks.Task SetParametersAsync(Microsoft.AspNetCore.Components.ParameterCollection parameters) { throw null; }
        protected virtual bool ShouldRender() { throw null; }
        protected void StateHasChanged() { }
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
    public abstract partial class Dispatcher
    {
        protected Dispatcher() { }
        public abstract bool CheckAccess();
        public static Microsoft.AspNetCore.Components.Dispatcher CreateDefault() { throw null; }
        public abstract System.Threading.Tasks.Task InvokeAsync(System.Action workItem);
        public abstract System.Threading.Tasks.Task InvokeAsync(System.Func<System.Threading.Tasks.Task> workItem);
        public abstract System.Threading.Tasks.Task<TResult> InvokeAsync<TResult>(System.Func<System.Threading.Tasks.Task<TResult>> workItem);
        public abstract System.Threading.Tasks.Task<TResult> InvokeAsync<TResult>(System.Func<TResult> workItem);
        protected void OnUnhandledException(System.UnhandledExceptionEventArgs e) { }
    }
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public readonly partial struct ElementRef
    {
        private readonly object _dummy;
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        public string __internalId { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
    }
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public readonly partial struct EventCallback
    {
        private readonly object _dummy;
        public static readonly Microsoft.AspNetCore.Components.EventCallback Empty;
        public static readonly Microsoft.AspNetCore.Components.EventCallbackFactory Factory;
        public EventCallback(Microsoft.AspNetCore.Components.IHandleEvent receiver, System.MulticastDelegate @delegate) { throw null; }
        public bool HasDelegate { get { throw null; } }
        public System.Threading.Tasks.Task InvokeAsync(object arg) { throw null; }
    }
    public sealed partial class EventCallbackFactory
    {
        public EventCallbackFactory() { }
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        public Microsoft.AspNetCore.Components.EventCallback Create(object receiver, Microsoft.AspNetCore.Components.EventCallback callback) { throw null; }
        public Microsoft.AspNetCore.Components.EventCallback Create(object receiver, System.Action callback) { throw null; }
        public Microsoft.AspNetCore.Components.EventCallback Create(object receiver, System.Action<object> callback) { throw null; }
        public Microsoft.AspNetCore.Components.EventCallback Create(object receiver, System.Func<object, System.Threading.Tasks.Task> callback) { throw null; }
        public Microsoft.AspNetCore.Components.EventCallback Create(object receiver, System.Func<System.Threading.Tasks.Task> callback) { throw null; }
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        public Microsoft.AspNetCore.Components.EventCallback<T> CreateInferred<T>(object receiver, System.Action<T> callback, T value) { throw null; }
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        public Microsoft.AspNetCore.Components.EventCallback<T> CreateInferred<T>(object receiver, System.Func<T, System.Threading.Tasks.Task> callback, T value) { throw null; }
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        public Microsoft.AspNetCore.Components.EventCallback<T> Create<T>(object receiver, Microsoft.AspNetCore.Components.EventCallback callback) { throw null; }
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        public Microsoft.AspNetCore.Components.EventCallback<T> Create<T>(object receiver, Microsoft.AspNetCore.Components.EventCallback<T> callback) { throw null; }
        public Microsoft.AspNetCore.Components.EventCallback<T> Create<T>(object receiver, System.Action callback) { throw null; }
        public Microsoft.AspNetCore.Components.EventCallback<T> Create<T>(object receiver, System.Action<T> callback) { throw null; }
        public Microsoft.AspNetCore.Components.EventCallback<T> Create<T>(object receiver, System.Func<System.Threading.Tasks.Task> callback) { throw null; }
        public Microsoft.AspNetCore.Components.EventCallback<T> Create<T>(object receiver, System.Func<T, System.Threading.Tasks.Task> callback) { throw null; }
    }
    public static partial class EventCallbackFactoryBinderExtensions
    {
        public static Microsoft.AspNetCore.Components.EventCallback<Microsoft.AspNetCore.Components.UIChangeEventArgs> CreateBinder(this Microsoft.AspNetCore.Components.EventCallbackFactory factory, object receiver, System.Action<bool> setter, bool existingValue, System.Globalization.CultureInfo culture = null) { throw null; }
        public static Microsoft.AspNetCore.Components.EventCallback<Microsoft.AspNetCore.Components.UIChangeEventArgs> CreateBinder(this Microsoft.AspNetCore.Components.EventCallbackFactory factory, object receiver, System.Action<System.DateTimeOffset> setter, System.DateTimeOffset existingValue, System.Globalization.CultureInfo culture = null) { throw null; }
        public static Microsoft.AspNetCore.Components.EventCallback<Microsoft.AspNetCore.Components.UIChangeEventArgs> CreateBinder(this Microsoft.AspNetCore.Components.EventCallbackFactory factory, object receiver, System.Action<System.DateTimeOffset> setter, System.DateTimeOffset existingValue, string format, System.Globalization.CultureInfo culture = null) { throw null; }
        public static Microsoft.AspNetCore.Components.EventCallback<Microsoft.AspNetCore.Components.UIChangeEventArgs> CreateBinder(this Microsoft.AspNetCore.Components.EventCallbackFactory factory, object receiver, System.Action<System.DateTime> setter, System.DateTime existingValue, System.Globalization.CultureInfo culture = null) { throw null; }
        public static Microsoft.AspNetCore.Components.EventCallback<Microsoft.AspNetCore.Components.UIChangeEventArgs> CreateBinder(this Microsoft.AspNetCore.Components.EventCallbackFactory factory, object receiver, System.Action<System.DateTime> setter, System.DateTime existingValue, string format, System.Globalization.CultureInfo culture = null) { throw null; }
        public static Microsoft.AspNetCore.Components.EventCallback<Microsoft.AspNetCore.Components.UIChangeEventArgs> CreateBinder(this Microsoft.AspNetCore.Components.EventCallbackFactory factory, object receiver, System.Action<decimal> setter, decimal existingValue, System.Globalization.CultureInfo culture = null) { throw null; }
        public static Microsoft.AspNetCore.Components.EventCallback<Microsoft.AspNetCore.Components.UIChangeEventArgs> CreateBinder(this Microsoft.AspNetCore.Components.EventCallbackFactory factory, object receiver, System.Action<double> setter, double existingValue, System.Globalization.CultureInfo culture = null) { throw null; }
        public static Microsoft.AspNetCore.Components.EventCallback<Microsoft.AspNetCore.Components.UIChangeEventArgs> CreateBinder(this Microsoft.AspNetCore.Components.EventCallbackFactory factory, object receiver, System.Action<int> setter, int existingValue, System.Globalization.CultureInfo culture = null) { throw null; }
        public static Microsoft.AspNetCore.Components.EventCallback<Microsoft.AspNetCore.Components.UIChangeEventArgs> CreateBinder(this Microsoft.AspNetCore.Components.EventCallbackFactory factory, object receiver, System.Action<long> setter, long existingValue, System.Globalization.CultureInfo culture = null) { throw null; }
        public static Microsoft.AspNetCore.Components.EventCallback<Microsoft.AspNetCore.Components.UIChangeEventArgs> CreateBinder(this Microsoft.AspNetCore.Components.EventCallbackFactory factory, object receiver, System.Action<bool?> setter, bool? existingValue, System.Globalization.CultureInfo culture = null) { throw null; }
        public static Microsoft.AspNetCore.Components.EventCallback<Microsoft.AspNetCore.Components.UIChangeEventArgs> CreateBinder(this Microsoft.AspNetCore.Components.EventCallbackFactory factory, object receiver, System.Action<System.DateTimeOffset?> setter, System.DateTimeOffset? existingValue, System.Globalization.CultureInfo culture = null) { throw null; }
        public static Microsoft.AspNetCore.Components.EventCallback<Microsoft.AspNetCore.Components.UIChangeEventArgs> CreateBinder(this Microsoft.AspNetCore.Components.EventCallbackFactory factory, object receiver, System.Action<System.DateTimeOffset?> setter, System.DateTimeOffset? existingValue, string format, System.Globalization.CultureInfo culture = null) { throw null; }
        public static Microsoft.AspNetCore.Components.EventCallback<Microsoft.AspNetCore.Components.UIChangeEventArgs> CreateBinder(this Microsoft.AspNetCore.Components.EventCallbackFactory factory, object receiver, System.Action<System.DateTime?> setter, System.DateTime? existingValue, System.Globalization.CultureInfo culture = null) { throw null; }
        public static Microsoft.AspNetCore.Components.EventCallback<Microsoft.AspNetCore.Components.UIChangeEventArgs> CreateBinder(this Microsoft.AspNetCore.Components.EventCallbackFactory factory, object receiver, System.Action<System.DateTime?> setter, System.DateTime? existingValue, string format, System.Globalization.CultureInfo culture = null) { throw null; }
        public static Microsoft.AspNetCore.Components.EventCallback<Microsoft.AspNetCore.Components.UIChangeEventArgs> CreateBinder(this Microsoft.AspNetCore.Components.EventCallbackFactory factory, object receiver, System.Action<decimal?> setter, decimal? existingValue, System.Globalization.CultureInfo culture = null) { throw null; }
        public static Microsoft.AspNetCore.Components.EventCallback<Microsoft.AspNetCore.Components.UIChangeEventArgs> CreateBinder(this Microsoft.AspNetCore.Components.EventCallbackFactory factory, object receiver, System.Action<double?> setter, double? existingValue, System.Globalization.CultureInfo culture = null) { throw null; }
        public static Microsoft.AspNetCore.Components.EventCallback<Microsoft.AspNetCore.Components.UIChangeEventArgs> CreateBinder(this Microsoft.AspNetCore.Components.EventCallbackFactory factory, object receiver, System.Action<int?> setter, int? existingValue, System.Globalization.CultureInfo culture = null) { throw null; }
        public static Microsoft.AspNetCore.Components.EventCallback<Microsoft.AspNetCore.Components.UIChangeEventArgs> CreateBinder(this Microsoft.AspNetCore.Components.EventCallbackFactory factory, object receiver, System.Action<long?> setter, long? existingValue, System.Globalization.CultureInfo culture = null) { throw null; }
        public static Microsoft.AspNetCore.Components.EventCallback<Microsoft.AspNetCore.Components.UIChangeEventArgs> CreateBinder(this Microsoft.AspNetCore.Components.EventCallbackFactory factory, object receiver, System.Action<float?> setter, float? existingValue, System.Globalization.CultureInfo culture = null) { throw null; }
        public static Microsoft.AspNetCore.Components.EventCallback<Microsoft.AspNetCore.Components.UIChangeEventArgs> CreateBinder(this Microsoft.AspNetCore.Components.EventCallbackFactory factory, object receiver, System.Action<float> setter, float existingValue, System.Globalization.CultureInfo culture = null) { throw null; }
        public static Microsoft.AspNetCore.Components.EventCallback<Microsoft.AspNetCore.Components.UIChangeEventArgs> CreateBinder(this Microsoft.AspNetCore.Components.EventCallbackFactory factory, object receiver, System.Action<string> setter, string existingValue, System.Globalization.CultureInfo culture = null) { throw null; }
        public static Microsoft.AspNetCore.Components.EventCallback<Microsoft.AspNetCore.Components.UIChangeEventArgs> CreateBinder<T>(this Microsoft.AspNetCore.Components.EventCallbackFactory factory, object receiver, System.Action<T> setter, T existingValue, System.Globalization.CultureInfo culture = null) { throw null; }
    }
    public static partial class EventCallbackFactoryUIEventArgsExtensions
    {
        public static Microsoft.AspNetCore.Components.EventCallback<Microsoft.AspNetCore.Components.UIChangeEventArgs> Create(this Microsoft.AspNetCore.Components.EventCallbackFactory factory, object receiver, System.Action<Microsoft.AspNetCore.Components.UIChangeEventArgs> callback) { throw null; }
        public static Microsoft.AspNetCore.Components.EventCallback<Microsoft.AspNetCore.Components.UIClipboardEventArgs> Create(this Microsoft.AspNetCore.Components.EventCallbackFactory factory, object receiver, System.Action<Microsoft.AspNetCore.Components.UIClipboardEventArgs> callback) { throw null; }
        public static Microsoft.AspNetCore.Components.EventCallback<Microsoft.AspNetCore.Components.UIDragEventArgs> Create(this Microsoft.AspNetCore.Components.EventCallbackFactory factory, object receiver, System.Action<Microsoft.AspNetCore.Components.UIDragEventArgs> callback) { throw null; }
        public static Microsoft.AspNetCore.Components.EventCallback<Microsoft.AspNetCore.Components.UIErrorEventArgs> Create(this Microsoft.AspNetCore.Components.EventCallbackFactory factory, object receiver, System.Action<Microsoft.AspNetCore.Components.UIErrorEventArgs> callback) { throw null; }
        public static Microsoft.AspNetCore.Components.EventCallback<Microsoft.AspNetCore.Components.UIEventArgs> Create(this Microsoft.AspNetCore.Components.EventCallbackFactory factory, object receiver, System.Action<Microsoft.AspNetCore.Components.UIEventArgs> callback) { throw null; }
        public static Microsoft.AspNetCore.Components.EventCallback<Microsoft.AspNetCore.Components.UIFocusEventArgs> Create(this Microsoft.AspNetCore.Components.EventCallbackFactory factory, object receiver, System.Action<Microsoft.AspNetCore.Components.UIFocusEventArgs> callback) { throw null; }
        public static Microsoft.AspNetCore.Components.EventCallback<Microsoft.AspNetCore.Components.UIKeyboardEventArgs> Create(this Microsoft.AspNetCore.Components.EventCallbackFactory factory, object receiver, System.Action<Microsoft.AspNetCore.Components.UIKeyboardEventArgs> callback) { throw null; }
        public static Microsoft.AspNetCore.Components.EventCallback<Microsoft.AspNetCore.Components.UIMouseEventArgs> Create(this Microsoft.AspNetCore.Components.EventCallbackFactory factory, object receiver, System.Action<Microsoft.AspNetCore.Components.UIMouseEventArgs> callback) { throw null; }
        public static Microsoft.AspNetCore.Components.EventCallback<Microsoft.AspNetCore.Components.UIPointerEventArgs> Create(this Microsoft.AspNetCore.Components.EventCallbackFactory factory, object receiver, System.Action<Microsoft.AspNetCore.Components.UIPointerEventArgs> callback) { throw null; }
        public static Microsoft.AspNetCore.Components.EventCallback<Microsoft.AspNetCore.Components.UIProgressEventArgs> Create(this Microsoft.AspNetCore.Components.EventCallbackFactory factory, object receiver, System.Action<Microsoft.AspNetCore.Components.UIProgressEventArgs> callback) { throw null; }
        public static Microsoft.AspNetCore.Components.EventCallback<Microsoft.AspNetCore.Components.UITouchEventArgs> Create(this Microsoft.AspNetCore.Components.EventCallbackFactory factory, object receiver, System.Action<Microsoft.AspNetCore.Components.UITouchEventArgs> callback) { throw null; }
        public static Microsoft.AspNetCore.Components.EventCallback<Microsoft.AspNetCore.Components.UIWheelEventArgs> Create(this Microsoft.AspNetCore.Components.EventCallbackFactory factory, object receiver, System.Action<Microsoft.AspNetCore.Components.UIWheelEventArgs> callback) { throw null; }
        public static Microsoft.AspNetCore.Components.EventCallback<Microsoft.AspNetCore.Components.UIChangeEventArgs> Create(this Microsoft.AspNetCore.Components.EventCallbackFactory factory, object receiver, System.Func<Microsoft.AspNetCore.Components.UIChangeEventArgs, System.Threading.Tasks.Task> callback) { throw null; }
        public static Microsoft.AspNetCore.Components.EventCallback<Microsoft.AspNetCore.Components.UIClipboardEventArgs> Create(this Microsoft.AspNetCore.Components.EventCallbackFactory factory, object receiver, System.Func<Microsoft.AspNetCore.Components.UIClipboardEventArgs, System.Threading.Tasks.Task> callback) { throw null; }
        public static Microsoft.AspNetCore.Components.EventCallback<Microsoft.AspNetCore.Components.UIDragEventArgs> Create(this Microsoft.AspNetCore.Components.EventCallbackFactory factory, object receiver, System.Func<Microsoft.AspNetCore.Components.UIDragEventArgs, System.Threading.Tasks.Task> callback) { throw null; }
        public static Microsoft.AspNetCore.Components.EventCallback<Microsoft.AspNetCore.Components.UIErrorEventArgs> Create(this Microsoft.AspNetCore.Components.EventCallbackFactory factory, object receiver, System.Func<Microsoft.AspNetCore.Components.UIErrorEventArgs, System.Threading.Tasks.Task> callback) { throw null; }
        public static Microsoft.AspNetCore.Components.EventCallback<Microsoft.AspNetCore.Components.UIEventArgs> Create(this Microsoft.AspNetCore.Components.EventCallbackFactory factory, object receiver, System.Func<Microsoft.AspNetCore.Components.UIEventArgs, System.Threading.Tasks.Task> callback) { throw null; }
        public static Microsoft.AspNetCore.Components.EventCallback<Microsoft.AspNetCore.Components.UIFocusEventArgs> Create(this Microsoft.AspNetCore.Components.EventCallbackFactory factory, object receiver, System.Func<Microsoft.AspNetCore.Components.UIFocusEventArgs, System.Threading.Tasks.Task> callback) { throw null; }
        public static Microsoft.AspNetCore.Components.EventCallback<Microsoft.AspNetCore.Components.UIKeyboardEventArgs> Create(this Microsoft.AspNetCore.Components.EventCallbackFactory factory, object receiver, System.Func<Microsoft.AspNetCore.Components.UIKeyboardEventArgs, System.Threading.Tasks.Task> callback) { throw null; }
        public static Microsoft.AspNetCore.Components.EventCallback<Microsoft.AspNetCore.Components.UIMouseEventArgs> Create(this Microsoft.AspNetCore.Components.EventCallbackFactory factory, object receiver, System.Func<Microsoft.AspNetCore.Components.UIMouseEventArgs, System.Threading.Tasks.Task> callback) { throw null; }
        public static Microsoft.AspNetCore.Components.EventCallback<Microsoft.AspNetCore.Components.UIPointerEventArgs> Create(this Microsoft.AspNetCore.Components.EventCallbackFactory factory, object receiver, System.Func<Microsoft.AspNetCore.Components.UIPointerEventArgs, System.Threading.Tasks.Task> callback) { throw null; }
        public static Microsoft.AspNetCore.Components.EventCallback<Microsoft.AspNetCore.Components.UIProgressEventArgs> Create(this Microsoft.AspNetCore.Components.EventCallbackFactory factory, object receiver, System.Func<Microsoft.AspNetCore.Components.UIProgressEventArgs, System.Threading.Tasks.Task> callback) { throw null; }
        public static Microsoft.AspNetCore.Components.EventCallback<Microsoft.AspNetCore.Components.UITouchEventArgs> Create(this Microsoft.AspNetCore.Components.EventCallbackFactory factory, object receiver, System.Func<Microsoft.AspNetCore.Components.UITouchEventArgs, System.Threading.Tasks.Task> callback) { throw null; }
        public static Microsoft.AspNetCore.Components.EventCallback<Microsoft.AspNetCore.Components.UIWheelEventArgs> Create(this Microsoft.AspNetCore.Components.EventCallbackFactory factory, object receiver, System.Func<Microsoft.AspNetCore.Components.UIWheelEventArgs, System.Threading.Tasks.Task> callback) { throw null; }
    }
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public readonly partial struct EventCallbackWorkItem
    {
        private readonly object _dummy;
        public static readonly Microsoft.AspNetCore.Components.EventCallbackWorkItem Empty;
        public EventCallbackWorkItem(System.MulticastDelegate @delegate) { throw null; }
        public System.Threading.Tasks.Task InvokeAsync(object arg) { throw null; }
    }
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public readonly partial struct EventCallback<T>
    {
        private readonly object _dummy;
        public static readonly Microsoft.AspNetCore.Components.EventCallback<T> Empty;
        public EventCallback(Microsoft.AspNetCore.Components.IHandleEvent receiver, System.MulticastDelegate @delegate) { throw null; }
        public bool HasDelegate { get { throw null; } }
        public System.Threading.Tasks.Task InvokeAsync(T arg) { throw null; }
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
    public partial interface IComponent
    {
        void Configure(Microsoft.AspNetCore.Components.RenderHandle renderHandle);
        System.Threading.Tasks.Task SetParametersAsync(Microsoft.AspNetCore.Components.ParameterCollection parameters);
    }
    public partial interface IComponentContext
    {
        bool IsConnected { get; }
    }
    public partial interface IHandleAfterRender
    {
        System.Threading.Tasks.Task OnAfterRenderAsync();
    }
    public partial interface IHandleEvent
    {
        System.Threading.Tasks.Task HandleEventAsync(Microsoft.AspNetCore.Components.EventCallbackWorkItem item, object arg);
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Property, AllowMultiple=false, Inherited=true)]
    public sealed partial class InjectAttribute : System.Attribute
    {
        public InjectAttribute() { }
    }
    public partial interface IUriHelper
    {
        event System.EventHandler<Microsoft.AspNetCore.Components.Routing.LocationChangedEventArgs> OnLocationChanged;
        string GetAbsoluteUri();
        string GetBaseUri();
        void NavigateTo(string uri);
        void NavigateTo(string uri, bool forceLoad);
        System.Uri ToAbsoluteUri(string href);
        string ToBaseRelativePath(string baseUri, string locationAbsolute);
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Class, AllowMultiple=false, Inherited=true)]
    public sealed partial class LayoutAttribute : System.Attribute
    {
        public LayoutAttribute(System.Type layoutType) { }
        public System.Type LayoutType { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
    }
    public abstract partial class LayoutComponentBase : Microsoft.AspNetCore.Components.ComponentBase
    {
        protected LayoutComponentBase() { }
        [Microsoft.AspNetCore.Components.ParameterAttribute]
        protected Microsoft.AspNetCore.Components.RenderFragment Body { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
    }
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public readonly partial struct MarkupString
    {
        private readonly object _dummy;
        public MarkupString(string value) { throw null; }
        public string Value { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public static explicit operator Microsoft.AspNetCore.Components.MarkupString (string value) { throw null; }
        public override string ToString() { throw null; }
    }
    public partial class NavigationException : System.Exception
    {
        public NavigationException(string uri) { }
        public string Location { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
    }
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public readonly partial struct Parameter
    {
        private readonly object _dummy;
        private readonly int _dummyPrimitive;
        public bool Cascading { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public string Name { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public object Value { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Property, AllowMultiple=false, Inherited=true)]
    public sealed partial class ParameterAttribute : System.Attribute
    {
        public ParameterAttribute() { }
        public bool CaptureUnmatchedValues { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
    }
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public readonly partial struct ParameterCollection
    {
        private readonly object _dummy;
        private readonly int _dummyPrimitive;
        public static Microsoft.AspNetCore.Components.ParameterCollection Empty { get { throw null; } }
        public static Microsoft.AspNetCore.Components.ParameterCollection FromDictionary(System.Collections.Generic.IDictionary<string, object> parameters) { throw null; }
        public Microsoft.AspNetCore.Components.ParameterEnumerator GetEnumerator() { throw null; }
        public T GetValueOrDefault<T>(string parameterName) { throw null; }
        public T GetValueOrDefault<T>(string parameterName, T defaultValue) { throw null; }
        public System.Collections.Generic.IReadOnlyDictionary<string, object> ToDictionary() { throw null; }
        public bool TryGetValue<T>(string parameterName, out T result) { throw null; }
    }
    public static partial class ParameterCollectionExtensions
    {
        public static void SetParameterProperties(this in Microsoft.AspNetCore.Components.ParameterCollection parameterCollection, object target) { }
    }
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public partial struct ParameterEnumerator
    {
        private object _dummy;
        private int _dummyPrimitive;
        public Microsoft.AspNetCore.Components.Parameter Current { get { throw null; } }
        public bool MoveNext() { throw null; }
    }
    public delegate void RenderFragment(Microsoft.AspNetCore.Components.RenderTree.RenderTreeBuilder builder);
    public delegate Microsoft.AspNetCore.Components.RenderFragment RenderFragment<T>(T value);
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public readonly partial struct RenderHandle
    {
        private readonly object _dummy;
        private readonly int _dummyPrimitive;
        public Microsoft.AspNetCore.Components.Dispatcher Dispatcher { get { throw null; } }
        public bool IsInitialized { get { throw null; } }
        public void Render(Microsoft.AspNetCore.Components.RenderFragment renderFragment) { }
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Class, AllowMultiple=true, Inherited=false)]
    public sealed partial class RouteAttribute : System.Attribute
    {
        public RouteAttribute(string template) { }
        public string Template { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
    }
    public static partial class RuntimeHelpers
    {
        public static T TypeCheck<T>(T value) { throw null; }
    }
    public partial class UIChangeEventArgs : Microsoft.AspNetCore.Components.UIEventArgs
    {
        public UIChangeEventArgs() { }
        public object Value { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
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
    public partial class UIEventArgs
    {
        public static readonly Microsoft.AspNetCore.Components.UIEventArgs Empty;
        public UIEventArgs() { }
        public string Type { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
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
    public abstract partial class UriHelperBase : Microsoft.AspNetCore.Components.IUriHelper
    {
        protected UriHelperBase() { }
        public event System.EventHandler<Microsoft.AspNetCore.Components.Routing.LocationChangedEventArgs> OnLocationChanged { add { } remove { } }
        protected virtual void EnsureInitialized() { }
        public string GetAbsoluteUri() { throw null; }
        public virtual string GetBaseUri() { throw null; }
        public virtual void InitializeState(string uriAbsolute, string baseUriAbsolute) { }
        public void NavigateTo(string uri) { }
        public void NavigateTo(string uri, bool forceLoad) { }
        protected abstract void NavigateToCore(string uri, bool forceLoad);
        protected void SetAbsoluteBaseUri(string baseUri) { }
        protected void SetAbsoluteUri(string uri) { }
        public System.Uri ToAbsoluteUri(string href) { throw null; }
        public string ToBaseRelativePath(string baseUri, string locationAbsolute) { throw null; }
        protected void TriggerOnLocationChanged(bool isinterceptedLink) { }
    }
}
namespace Microsoft.AspNetCore.Components.CompilerServices
{
    public static partial class RuntimeHelpers
    {
        public static T TypeCheck<T>(T value) { throw null; }
    }
}
namespace Microsoft.AspNetCore.Components.Forms
{
    public sealed partial class EditContext
    {
        public EditContext(object model) { }
        public object Model { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public event System.EventHandler<Microsoft.AspNetCore.Components.Forms.FieldChangedEventArgs> OnFieldChanged { add { } remove { } }
        public event System.EventHandler<Microsoft.AspNetCore.Components.Forms.ValidationRequestedEventArgs> OnValidationRequested { add { } remove { } }
        public event System.EventHandler<Microsoft.AspNetCore.Components.Forms.ValidationStateChangedEventArgs> OnValidationStateChanged { add { } remove { } }
        public Microsoft.AspNetCore.Components.Forms.FieldIdentifier Field(string fieldName) { throw null; }
        public System.Collections.Generic.IEnumerable<string> GetValidationMessages() { throw null; }
        public System.Collections.Generic.IEnumerable<string> GetValidationMessages(Microsoft.AspNetCore.Components.Forms.FieldIdentifier fieldIdentifier) { throw null; }
        public bool IsModified() { throw null; }
        public bool IsModified(in Microsoft.AspNetCore.Components.Forms.FieldIdentifier fieldIdentifier) { throw null; }
        public void MarkAsUnmodified() { }
        public void MarkAsUnmodified(in Microsoft.AspNetCore.Components.Forms.FieldIdentifier fieldIdentifier) { }
        public void NotifyFieldChanged(in Microsoft.AspNetCore.Components.Forms.FieldIdentifier fieldIdentifier) { }
        public void NotifyValidationStateChanged() { }
        public bool Validate() { throw null; }
    }
    public static partial class EditContextDataAnnotationsExtensions
    {
        public static Microsoft.AspNetCore.Components.Forms.EditContext AddDataAnnotationsValidation(this Microsoft.AspNetCore.Components.Forms.EditContext editContext) { throw null; }
    }
    public static partial class EditContextExpressionExtensions
    {
        public static System.Collections.Generic.IEnumerable<string> GetValidationMessages(this Microsoft.AspNetCore.Components.Forms.EditContext editContext, System.Linq.Expressions.Expression<System.Func<object>> accessor) { throw null; }
        public static bool IsModified(this Microsoft.AspNetCore.Components.Forms.EditContext editContext, System.Linq.Expressions.Expression<System.Func<object>> accessor) { throw null; }
    }
    public static partial class EditContextFieldClassExtensions
    {
        public static string FieldClass(this Microsoft.AspNetCore.Components.Forms.EditContext editContext, in Microsoft.AspNetCore.Components.Forms.FieldIdentifier fieldIdentifier) { throw null; }
        public static string FieldClass<TField>(this Microsoft.AspNetCore.Components.Forms.EditContext editContext, System.Linq.Expressions.Expression<System.Func<TField>> accessor) { throw null; }
    }
    public sealed partial class FieldChangedEventArgs
    {
        internal FieldChangedEventArgs() { }
        public Microsoft.AspNetCore.Components.Forms.FieldIdentifier FieldIdentifier { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
    }
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public readonly partial struct FieldIdentifier
    {
        private readonly object _dummy;
        public FieldIdentifier(object model, string fieldName) { throw null; }
        public string FieldName { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public object Model { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public static Microsoft.AspNetCore.Components.Forms.FieldIdentifier Create<T>(System.Linq.Expressions.Expression<System.Func<T>> accessor) { throw null; }
        public override bool Equals(object obj) { throw null; }
        public override int GetHashCode() { throw null; }
    }
    public sealed partial class ValidationMessageStore
    {
        public ValidationMessageStore(Microsoft.AspNetCore.Components.Forms.EditContext editContext) { }
        public System.Collections.Generic.IEnumerable<string> this[Microsoft.AspNetCore.Components.Forms.FieldIdentifier fieldIdentifier] { get { throw null; } }
        public System.Collections.Generic.IEnumerable<string> this[System.Linq.Expressions.Expression<System.Func<object>> accessor] { get { throw null; } }
        public void Add(in Microsoft.AspNetCore.Components.Forms.FieldIdentifier fieldIdentifier, string message) { }
        public void AddRange(in Microsoft.AspNetCore.Components.Forms.FieldIdentifier fieldIdentifier, System.Collections.Generic.IEnumerable<string> messages) { }
        public void Clear() { }
        public void Clear(in Microsoft.AspNetCore.Components.Forms.FieldIdentifier fieldIdentifier) { }
    }
    public static partial class ValidationMessageStoreExpressionExtensions
    {
        public static void Add(this Microsoft.AspNetCore.Components.Forms.ValidationMessageStore store, System.Linq.Expressions.Expression<System.Func<object>> accessor, string message) { }
        public static void AddRange(this Microsoft.AspNetCore.Components.Forms.ValidationMessageStore store, System.Linq.Expressions.Expression<System.Func<object>> accessor, System.Collections.Generic.IEnumerable<string> messages) { }
        public static void Clear(this Microsoft.AspNetCore.Components.Forms.ValidationMessageStore store, System.Linq.Expressions.Expression<System.Func<object>> accessor) { }
    }
    public sealed partial class ValidationRequestedEventArgs
    {
        internal ValidationRequestedEventArgs() { }
    }
    public sealed partial class ValidationStateChangedEventArgs
    {
        internal ValidationStateChangedEventArgs() { }
    }
}
namespace Microsoft.AspNetCore.Components.Rendering
{
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public readonly partial struct ComponentRenderedText
    {
        private readonly object _dummy;
        private readonly int _dummyPrimitive;
        public int ComponentId { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public System.Collections.Generic.IEnumerable<string> Tokens { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
    }
    public partial class EventFieldInfo
    {
        public EventFieldInfo() { }
        public int ComponentId { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public object FieldValue { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
    }
    public partial class HtmlRenderer : Microsoft.AspNetCore.Components.Rendering.Renderer
    {
        public HtmlRenderer(System.IServiceProvider serviceProvider, Microsoft.Extensions.Logging.ILoggerFactory loggerFactory, System.Func<string, string> htmlEncoder) : base (default(System.IServiceProvider), default(Microsoft.Extensions.Logging.ILoggerFactory)) { }
        public override Microsoft.AspNetCore.Components.Dispatcher Dispatcher { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        protected override void HandleException(System.Exception exception) { }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public System.Threading.Tasks.Task<Microsoft.AspNetCore.Components.Rendering.ComponentRenderedText> RenderComponentAsync(System.Type componentType, Microsoft.AspNetCore.Components.ParameterCollection initialParameters) { throw null; }
        public System.Threading.Tasks.Task<Microsoft.AspNetCore.Components.Rendering.ComponentRenderedText> RenderComponentAsync<TComponent>(Microsoft.AspNetCore.Components.ParameterCollection initialParameters) where TComponent : Microsoft.AspNetCore.Components.IComponent { throw null; }
        protected override System.Threading.Tasks.Task UpdateDisplayAsync(in Microsoft.AspNetCore.Components.Rendering.RenderBatch renderBatch) { throw null; }
    }
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public readonly partial struct RenderBatch
    {
        private readonly object _dummy;
        public Microsoft.AspNetCore.Components.RenderTree.ArrayRange<int> DisposedComponentIDs { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public Microsoft.AspNetCore.Components.RenderTree.ArrayRange<int> DisposedEventHandlerIDs { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public Microsoft.AspNetCore.Components.RenderTree.ArrayRange<Microsoft.AspNetCore.Components.RenderTree.RenderTreeFrame> ReferenceFrames { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public Microsoft.AspNetCore.Components.RenderTree.ArrayRange<Microsoft.AspNetCore.Components.RenderTree.RenderTreeDiff> UpdatedComponents { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
    }
    public abstract partial class Renderer : System.IDisposable
    {
        public Renderer(System.IServiceProvider serviceProvider, Microsoft.Extensions.Logging.ILoggerFactory loggerFactory) { }
        public abstract Microsoft.AspNetCore.Components.Dispatcher Dispatcher { get; }
        public event System.UnhandledExceptionEventHandler UnhandledSynchronizationException { add { } remove { } }
        protected internal virtual void AddToRenderQueue(int componentId, Microsoft.AspNetCore.Components.RenderFragment renderFragment) { }
        protected internal int AssignRootComponentId(Microsoft.AspNetCore.Components.IComponent component) { throw null; }
        public virtual System.Threading.Tasks.Task DispatchEventAsync(int eventHandlerId, Microsoft.AspNetCore.Components.Rendering.EventFieldInfo fieldInfo, Microsoft.AspNetCore.Components.UIEventArgs eventArgs) { throw null; }
        public void Dispose() { }
        protected virtual void Dispose(bool disposing) { }
        protected abstract void HandleException(System.Exception exception);
        protected Microsoft.AspNetCore.Components.IComponent InstantiateComponent(System.Type componentType) { throw null; }
        protected System.Threading.Tasks.Task RenderRootComponentAsync(int componentId) { throw null; }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        protected System.Threading.Tasks.Task RenderRootComponentAsync(int componentId, Microsoft.AspNetCore.Components.ParameterCollection initialParameters) { throw null; }
        protected abstract System.Threading.Tasks.Task UpdateDisplayAsync(in Microsoft.AspNetCore.Components.Rendering.RenderBatch renderBatch);
    }
}
namespace Microsoft.AspNetCore.Components.RenderTree
{
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public readonly partial struct ArrayBuilderSegment<T> : System.Collections.Generic.IEnumerable<T>, System.Collections.IEnumerable
    {
        private readonly object _dummy;
        private readonly int _dummyPrimitive;
        public T[] Array { get { throw null; } }
        public int Count { get { throw null; } }
        public T this[int index] { get { throw null; } }
        public int Offset { get { throw null; } }
        System.Collections.Generic.IEnumerator<T> System.Collections.Generic.IEnumerable<T>.GetEnumerator() { throw null; }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw null; }
    }
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public readonly partial struct ArrayRange<T>
    {
        public readonly T[] Array;
        public readonly int Count;
        public ArrayRange(T[] array, int count) { throw null; }
        public Microsoft.AspNetCore.Components.RenderTree.ArrayRange<T> Clone() { throw null; }
    }
    public partial class RenderTreeBuilder
    {
        public const string ChildContent = "ChildContent";
        public RenderTreeBuilder(Microsoft.AspNetCore.Components.Rendering.Renderer renderer) { }
        public void AddAttribute(int sequence, in Microsoft.AspNetCore.Components.RenderTree.RenderTreeFrame frame) { }
        public void AddAttribute(int sequence, string name, Microsoft.AspNetCore.Components.EventCallback value) { }
        public void AddAttribute(int sequence, string name, System.Action value) { }
        public void AddAttribute(int sequence, string name, bool value) { }
        public void AddAttribute(int sequence, string name, System.Func<System.Threading.Tasks.Task> value) { }
        public void AddAttribute(int sequence, string name, System.MulticastDelegate value) { }
        public void AddAttribute(int sequence, string name, object value) { }
        public void AddAttribute(int sequence, string name, string value) { }
        public void AddAttribute<T>(int sequence, string name, Microsoft.AspNetCore.Components.EventCallback<T> value) { }
        public void AddComponentReferenceCapture(int sequence, System.Action<object> componentReferenceCaptureAction) { }
        public void AddContent(int sequence, Microsoft.AspNetCore.Components.MarkupString markupContent) { }
        public void AddContent(int sequence, Microsoft.AspNetCore.Components.RenderFragment fragment) { }
        public void AddContent(int sequence, object textContent) { }
        public void AddContent(int sequence, string textContent) { }
        public void AddContent<T>(int sequence, Microsoft.AspNetCore.Components.RenderFragment<T> fragment, T value) { }
        public void AddElementReferenceCapture(int sequence, System.Action<Microsoft.AspNetCore.Components.ElementRef> elementReferenceCaptureAction) { }
        public void AddMarkupContent(int sequence, string markupContent) { }
        public void AddMultipleAttributes(int sequence, System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<string, object>> attributes) { }
        public void Clear() { }
        public void CloseComponent() { }
        public void CloseElement() { }
        public Microsoft.AspNetCore.Components.RenderTree.ArrayRange<Microsoft.AspNetCore.Components.RenderTree.RenderTreeFrame> GetFrames() { throw null; }
        public void OpenComponent(int sequence, System.Type componentType) { }
        public void OpenComponent<TComponent>(int sequence) where TComponent : Microsoft.AspNetCore.Components.IComponent { }
        public void OpenElement(int sequence, string elementName) { }
        public void SetKey(object value) { }
        public void SetUpdatesAttributeName(string updatesAttributeName) { }
    }
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public readonly partial struct RenderTreeDiff
    {
        public readonly int ComponentId;
        public readonly Microsoft.AspNetCore.Components.RenderTree.ArrayBuilderSegment<Microsoft.AspNetCore.Components.RenderTree.RenderTreeEdit> Edits;
    }
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Explicit)]
    public readonly partial struct RenderTreeEdit
    {
        [System.Runtime.InteropServices.FieldOffsetAttribute(8)]
        public readonly int MoveToSiblingIndex;
        [System.Runtime.InteropServices.FieldOffsetAttribute(8)]
        public readonly int ReferenceFrameIndex;
        [System.Runtime.InteropServices.FieldOffsetAttribute(16)]
        public readonly string RemovedAttributeName;
        [System.Runtime.InteropServices.FieldOffsetAttribute(4)]
        public readonly int SiblingIndex;
        [System.Runtime.InteropServices.FieldOffsetAttribute(0)]
        public readonly Microsoft.AspNetCore.Components.RenderTree.RenderTreeEditType Type;
    }
    public enum RenderTreeEditType
    {
        PrependFrame = 1,
        RemoveFrame = 2,
        SetAttribute = 3,
        RemoveAttribute = 4,
        UpdateText = 5,
        StepIn = 6,
        StepOut = 7,
        UpdateMarkup = 8,
        PermutationListEntry = 9,
        PermutationListEnd = 10,
    }
    public enum RenderTreeFrameType
    {
        None = 0,
        Element = 1,
        Text = 2,
        Attribute = 3,
        Component = 4,
        Region = 5,
        ElementReferenceCapture = 6,
        ComponentReferenceCapture = 7,
        Markup = 8,
    }
}
namespace Microsoft.AspNetCore.Components.Routing
{
    public partial interface INavigationInterception
    {
        System.Threading.Tasks.Task EnableNavigationInterceptionAsync();
    }
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public readonly partial struct LocationChangedEventArgs
    {
        private readonly object _dummy;
        private readonly int _dummyPrimitive;
        public LocationChangedEventArgs(string location, bool isNavigationIntercepted) { throw null; }
        public bool IsNavigationIntercepted { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public string Location { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
    }
    public enum NavLinkMatch
    {
        Prefix = 0,
        All = 1,
    }
}
