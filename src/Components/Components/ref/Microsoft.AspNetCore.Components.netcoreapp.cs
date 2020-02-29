// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Components
{
    public static partial class BindConverter
    {
        public static bool FormatValue(bool value, System.Globalization.CultureInfo culture = null) { throw null; }
        public static string FormatValue(System.DateTime value, System.Globalization.CultureInfo culture = null) { throw null; }
        public static string FormatValue(System.DateTime value, string format, System.Globalization.CultureInfo culture = null) { throw null; }
        public static string FormatValue(System.DateTimeOffset value, System.Globalization.CultureInfo culture = null) { throw null; }
        public static string FormatValue(System.DateTimeOffset value, string format, System.Globalization.CultureInfo culture = null) { throw null; }
        public static string FormatValue(decimal value, System.Globalization.CultureInfo culture = null) { throw null; }
        public static string FormatValue(double value, System.Globalization.CultureInfo culture = null) { throw null; }
        public static string FormatValue(short value, System.Globalization.CultureInfo culture = null) { throw null; }
        public static string FormatValue(int value, System.Globalization.CultureInfo culture = null) { throw null; }
        public static string FormatValue(long value, System.Globalization.CultureInfo culture = null) { throw null; }
        public static bool? FormatValue(bool? value, System.Globalization.CultureInfo culture = null) { throw null; }
        public static string FormatValue(System.DateTimeOffset? value, System.Globalization.CultureInfo culture = null) { throw null; }
        public static string FormatValue(System.DateTimeOffset? value, string format, System.Globalization.CultureInfo culture = null) { throw null; }
        public static string FormatValue(System.DateTime? value, System.Globalization.CultureInfo culture = null) { throw null; }
        public static string FormatValue(System.DateTime? value, string format, System.Globalization.CultureInfo culture = null) { throw null; }
        public static string FormatValue(decimal? value, System.Globalization.CultureInfo culture = null) { throw null; }
        public static string FormatValue(double? value, System.Globalization.CultureInfo culture = null) { throw null; }
        public static string FormatValue(short? value, System.Globalization.CultureInfo culture = null) { throw null; }
        public static string FormatValue(int? value, System.Globalization.CultureInfo culture = null) { throw null; }
        public static string FormatValue(long? value, System.Globalization.CultureInfo culture = null) { throw null; }
        public static string FormatValue(float? value, System.Globalization.CultureInfo culture = null) { throw null; }
        public static string FormatValue(float value, System.Globalization.CultureInfo culture = null) { throw null; }
        public static string FormatValue(string value, System.Globalization.CultureInfo culture = null) { throw null; }
        public static object FormatValue<T>(T value, System.Globalization.CultureInfo culture = null) { throw null; }
        public static bool TryConvertToBool(object obj, System.Globalization.CultureInfo culture, out bool value) { throw null; }
        public static bool TryConvertToDateTime(object obj, System.Globalization.CultureInfo culture, out System.DateTime value) { throw null; }
        public static bool TryConvertToDateTime(object obj, System.Globalization.CultureInfo culture, string format, out System.DateTime value) { throw null; }
        public static bool TryConvertToDateTimeOffset(object obj, System.Globalization.CultureInfo culture, out System.DateTimeOffset value) { throw null; }
        public static bool TryConvertToDateTimeOffset(object obj, System.Globalization.CultureInfo culture, string format, out System.DateTimeOffset value) { throw null; }
        public static bool TryConvertToDecimal(object obj, System.Globalization.CultureInfo culture, out decimal value) { throw null; }
        public static bool TryConvertToDouble(object obj, System.Globalization.CultureInfo culture, out double value) { throw null; }
        public static bool TryConvertToFloat(object obj, System.Globalization.CultureInfo culture, out float value) { throw null; }
        public static bool TryConvertToInt(object obj, System.Globalization.CultureInfo culture, out int value) { throw null; }
        public static bool TryConvertToLong(object obj, System.Globalization.CultureInfo culture, out long value) { throw null; }
        public static bool TryConvertToNullableBool(object obj, System.Globalization.CultureInfo culture, out bool? value) { throw null; }
        public static bool TryConvertToNullableDateTime(object obj, System.Globalization.CultureInfo culture, out System.DateTime? value) { throw null; }
        public static bool TryConvertToNullableDateTime(object obj, System.Globalization.CultureInfo culture, string format, out System.DateTime? value) { throw null; }
        public static bool TryConvertToNullableDateTimeOffset(object obj, System.Globalization.CultureInfo culture, out System.DateTimeOffset? value) { throw null; }
        public static bool TryConvertToNullableDateTimeOffset(object obj, System.Globalization.CultureInfo culture, string format, out System.DateTimeOffset? value) { throw null; }
        public static bool TryConvertToNullableDecimal(object obj, System.Globalization.CultureInfo culture, out decimal? value) { throw null; }
        public static bool TryConvertToNullableDouble(object obj, System.Globalization.CultureInfo culture, out double? value) { throw null; }
        public static bool TryConvertToNullableFloat(object obj, System.Globalization.CultureInfo culture, out float? value) { throw null; }
        public static bool TryConvertToNullableInt(object obj, System.Globalization.CultureInfo culture, out int? value) { throw null; }
        public static bool TryConvertToNullableLong(object obj, System.Globalization.CultureInfo culture, out long? value) { throw null; }
        public static bool TryConvertToNullableShort(object obj, System.Globalization.CultureInfo culture, out short? value) { throw null; }
        public static bool TryConvertToShort(object obj, System.Globalization.CultureInfo culture, out short value) { throw null; }
        public static bool TryConvertToString(object obj, System.Globalization.CultureInfo culture, out string value) { throw null; }
        public static bool TryConvertTo<T>(object obj, System.Globalization.CultureInfo culture, out T value) { throw null; }
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Class, AllowMultiple=true, Inherited=true)]
    public sealed partial class BindElementAttribute : System.Attribute
    {
        public BindElementAttribute(string element, string suffix, string valueAttribute, string changeAttribute) { }
        public string ChangeAttribute { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public string Element { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public string Suffix { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public string ValueAttribute { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Property, AllowMultiple=false, Inherited=true)]
    public sealed partial class CascadingParameterAttribute : System.Attribute
    {
        public CascadingParameterAttribute() { }
        public string Name { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
    }
    public partial class CascadingValue<TValue> : Microsoft.AspNetCore.Components.IComponent
    {
        public CascadingValue() { }
        [Microsoft.AspNetCore.Components.ParameterAttribute]
        public Microsoft.AspNetCore.Components.RenderFragment ChildContent { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        [Microsoft.AspNetCore.Components.ParameterAttribute]
        public bool IsFixed { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        [Microsoft.AspNetCore.Components.ParameterAttribute]
        public string Name { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        [Microsoft.AspNetCore.Components.ParameterAttribute]
        public TValue Value { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public void Attach(Microsoft.AspNetCore.Components.RenderHandle renderHandle) { }
        public System.Threading.Tasks.Task SetParametersAsync(Microsoft.AspNetCore.Components.ParameterView parameters) { throw null; }
    }
    public partial class ChangeEventArgs : System.EventArgs
    {
        public ChangeEventArgs() { }
        public object Value { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
    }
    public abstract partial class ComponentBase : Microsoft.AspNetCore.Components.IComponent, Microsoft.AspNetCore.Components.IHandleAfterRender, Microsoft.AspNetCore.Components.IHandleEvent
    {
        public ComponentBase() { }
        protected virtual void BuildRenderTree(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder builder) { }
        protected System.Threading.Tasks.Task InvokeAsync(System.Action workItem) { throw null; }
        protected System.Threading.Tasks.Task InvokeAsync(System.Func<System.Threading.Tasks.Task> workItem) { throw null; }
        void Microsoft.AspNetCore.Components.IComponent.Attach(Microsoft.AspNetCore.Components.RenderHandle renderHandle) { }
        System.Threading.Tasks.Task Microsoft.AspNetCore.Components.IHandleAfterRender.OnAfterRenderAsync() { throw null; }
        System.Threading.Tasks.Task Microsoft.AspNetCore.Components.IHandleEvent.HandleEventAsync(Microsoft.AspNetCore.Components.EventCallbackWorkItem callback, object arg) { throw null; }
        protected virtual void OnAfterRender(bool firstRender) { }
        protected virtual System.Threading.Tasks.Task OnAfterRenderAsync(bool firstRender) { throw null; }
        protected virtual void OnInitialized() { }
        protected virtual System.Threading.Tasks.Task OnInitializedAsync() { throw null; }
        protected virtual void OnParametersSet() { }
        protected virtual System.Threading.Tasks.Task OnParametersSetAsync() { throw null; }
        public virtual System.Threading.Tasks.Task SetParametersAsync(Microsoft.AspNetCore.Components.ParameterView parameters) { throw null; }
        protected virtual bool ShouldRender() { throw null; }
        protected void StateHasChanged() { }
    }
    public abstract partial class Dispatcher
    {
        protected Dispatcher() { }
        public void AssertAccess() { }
        public abstract bool CheckAccess();
        public static Microsoft.AspNetCore.Components.Dispatcher CreateDefault() { throw null; }
        public abstract System.Threading.Tasks.Task InvokeAsync(System.Action workItem);
        public abstract System.Threading.Tasks.Task InvokeAsync(System.Func<System.Threading.Tasks.Task> workItem);
        public abstract System.Threading.Tasks.Task<TResult> InvokeAsync<TResult>(System.Func<System.Threading.Tasks.Task<TResult>> workItem);
        public abstract System.Threading.Tasks.Task<TResult> InvokeAsync<TResult>(System.Func<TResult> workItem);
        protected void OnUnhandledException(System.UnhandledExceptionEventArgs e) { }
    }
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public readonly partial struct ElementReference
    {
        private readonly object _dummy;
        private readonly int _dummyPrimitive;
        public ElementReference(string id) { throw null; }
        public string Id { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
    }
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public readonly partial struct EventCallback
    {
        private readonly object _dummy;
        private readonly int _dummyPrimitive;
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
        public Microsoft.AspNetCore.Components.EventCallback<TValue> CreateInferred<TValue>(object receiver, System.Action<TValue> callback, TValue value) { throw null; }
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        public Microsoft.AspNetCore.Components.EventCallback<TValue> CreateInferred<TValue>(object receiver, System.Func<TValue, System.Threading.Tasks.Task> callback, TValue value) { throw null; }
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        public Microsoft.AspNetCore.Components.EventCallback<TValue> Create<TValue>(object receiver, Microsoft.AspNetCore.Components.EventCallback callback) { throw null; }
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        public Microsoft.AspNetCore.Components.EventCallback<TValue> Create<TValue>(object receiver, Microsoft.AspNetCore.Components.EventCallback<TValue> callback) { throw null; }
        public Microsoft.AspNetCore.Components.EventCallback<TValue> Create<TValue>(object receiver, System.Action callback) { throw null; }
        public Microsoft.AspNetCore.Components.EventCallback<TValue> Create<TValue>(object receiver, System.Action<TValue> callback) { throw null; }
        public Microsoft.AspNetCore.Components.EventCallback<TValue> Create<TValue>(object receiver, System.Func<System.Threading.Tasks.Task> callback) { throw null; }
        public Microsoft.AspNetCore.Components.EventCallback<TValue> Create<TValue>(object receiver, System.Func<TValue, System.Threading.Tasks.Task> callback) { throw null; }
    }
    public static partial class EventCallbackFactoryBinderExtensions
    {
        public static Microsoft.AspNetCore.Components.EventCallback<Microsoft.AspNetCore.Components.ChangeEventArgs> CreateBinder(this Microsoft.AspNetCore.Components.EventCallbackFactory factory, object receiver, System.Action<bool> setter, bool existingValue, System.Globalization.CultureInfo culture = null) { throw null; }
        public static Microsoft.AspNetCore.Components.EventCallback<Microsoft.AspNetCore.Components.ChangeEventArgs> CreateBinder(this Microsoft.AspNetCore.Components.EventCallbackFactory factory, object receiver, System.Action<System.DateTimeOffset> setter, System.DateTimeOffset existingValue, System.Globalization.CultureInfo culture = null) { throw null; }
        public static Microsoft.AspNetCore.Components.EventCallback<Microsoft.AspNetCore.Components.ChangeEventArgs> CreateBinder(this Microsoft.AspNetCore.Components.EventCallbackFactory factory, object receiver, System.Action<System.DateTimeOffset> setter, System.DateTimeOffset existingValue, string format, System.Globalization.CultureInfo culture = null) { throw null; }
        public static Microsoft.AspNetCore.Components.EventCallback<Microsoft.AspNetCore.Components.ChangeEventArgs> CreateBinder(this Microsoft.AspNetCore.Components.EventCallbackFactory factory, object receiver, System.Action<System.DateTime> setter, System.DateTime existingValue, System.Globalization.CultureInfo culture = null) { throw null; }
        public static Microsoft.AspNetCore.Components.EventCallback<Microsoft.AspNetCore.Components.ChangeEventArgs> CreateBinder(this Microsoft.AspNetCore.Components.EventCallbackFactory factory, object receiver, System.Action<System.DateTime> setter, System.DateTime existingValue, string format, System.Globalization.CultureInfo culture = null) { throw null; }
        public static Microsoft.AspNetCore.Components.EventCallback<Microsoft.AspNetCore.Components.ChangeEventArgs> CreateBinder(this Microsoft.AspNetCore.Components.EventCallbackFactory factory, object receiver, System.Action<decimal> setter, decimal existingValue, System.Globalization.CultureInfo culture = null) { throw null; }
        public static Microsoft.AspNetCore.Components.EventCallback<Microsoft.AspNetCore.Components.ChangeEventArgs> CreateBinder(this Microsoft.AspNetCore.Components.EventCallbackFactory factory, object receiver, System.Action<double> setter, double existingValue, System.Globalization.CultureInfo culture = null) { throw null; }
        public static Microsoft.AspNetCore.Components.EventCallback<Microsoft.AspNetCore.Components.ChangeEventArgs> CreateBinder(this Microsoft.AspNetCore.Components.EventCallbackFactory factory, object receiver, System.Action<short> setter, short existingValue, System.Globalization.CultureInfo culture = null) { throw null; }
        public static Microsoft.AspNetCore.Components.EventCallback<Microsoft.AspNetCore.Components.ChangeEventArgs> CreateBinder(this Microsoft.AspNetCore.Components.EventCallbackFactory factory, object receiver, System.Action<int> setter, int existingValue, System.Globalization.CultureInfo culture = null) { throw null; }
        public static Microsoft.AspNetCore.Components.EventCallback<Microsoft.AspNetCore.Components.ChangeEventArgs> CreateBinder(this Microsoft.AspNetCore.Components.EventCallbackFactory factory, object receiver, System.Action<long> setter, long existingValue, System.Globalization.CultureInfo culture = null) { throw null; }
        public static Microsoft.AspNetCore.Components.EventCallback<Microsoft.AspNetCore.Components.ChangeEventArgs> CreateBinder(this Microsoft.AspNetCore.Components.EventCallbackFactory factory, object receiver, System.Action<bool?> setter, bool? existingValue, System.Globalization.CultureInfo culture = null) { throw null; }
        public static Microsoft.AspNetCore.Components.EventCallback<Microsoft.AspNetCore.Components.ChangeEventArgs> CreateBinder(this Microsoft.AspNetCore.Components.EventCallbackFactory factory, object receiver, System.Action<System.DateTimeOffset?> setter, System.DateTimeOffset? existingValue, System.Globalization.CultureInfo culture = null) { throw null; }
        public static Microsoft.AspNetCore.Components.EventCallback<Microsoft.AspNetCore.Components.ChangeEventArgs> CreateBinder(this Microsoft.AspNetCore.Components.EventCallbackFactory factory, object receiver, System.Action<System.DateTimeOffset?> setter, System.DateTimeOffset? existingValue, string format, System.Globalization.CultureInfo culture = null) { throw null; }
        public static Microsoft.AspNetCore.Components.EventCallback<Microsoft.AspNetCore.Components.ChangeEventArgs> CreateBinder(this Microsoft.AspNetCore.Components.EventCallbackFactory factory, object receiver, System.Action<System.DateTime?> setter, System.DateTime? existingValue, System.Globalization.CultureInfo culture = null) { throw null; }
        public static Microsoft.AspNetCore.Components.EventCallback<Microsoft.AspNetCore.Components.ChangeEventArgs> CreateBinder(this Microsoft.AspNetCore.Components.EventCallbackFactory factory, object receiver, System.Action<System.DateTime?> setter, System.DateTime? existingValue, string format, System.Globalization.CultureInfo culture = null) { throw null; }
        public static Microsoft.AspNetCore.Components.EventCallback<Microsoft.AspNetCore.Components.ChangeEventArgs> CreateBinder(this Microsoft.AspNetCore.Components.EventCallbackFactory factory, object receiver, System.Action<decimal?> setter, decimal? existingValue, System.Globalization.CultureInfo culture = null) { throw null; }
        public static Microsoft.AspNetCore.Components.EventCallback<Microsoft.AspNetCore.Components.ChangeEventArgs> CreateBinder(this Microsoft.AspNetCore.Components.EventCallbackFactory factory, object receiver, System.Action<double?> setter, double? existingValue, System.Globalization.CultureInfo culture = null) { throw null; }
        public static Microsoft.AspNetCore.Components.EventCallback<Microsoft.AspNetCore.Components.ChangeEventArgs> CreateBinder(this Microsoft.AspNetCore.Components.EventCallbackFactory factory, object receiver, System.Action<short?> setter, short? existingValue, System.Globalization.CultureInfo culture = null) { throw null; }
        public static Microsoft.AspNetCore.Components.EventCallback<Microsoft.AspNetCore.Components.ChangeEventArgs> CreateBinder(this Microsoft.AspNetCore.Components.EventCallbackFactory factory, object receiver, System.Action<int?> setter, int? existingValue, System.Globalization.CultureInfo culture = null) { throw null; }
        public static Microsoft.AspNetCore.Components.EventCallback<Microsoft.AspNetCore.Components.ChangeEventArgs> CreateBinder(this Microsoft.AspNetCore.Components.EventCallbackFactory factory, object receiver, System.Action<long?> setter, long? existingValue, System.Globalization.CultureInfo culture = null) { throw null; }
        public static Microsoft.AspNetCore.Components.EventCallback<Microsoft.AspNetCore.Components.ChangeEventArgs> CreateBinder(this Microsoft.AspNetCore.Components.EventCallbackFactory factory, object receiver, System.Action<float?> setter, float? existingValue, System.Globalization.CultureInfo culture = null) { throw null; }
        public static Microsoft.AspNetCore.Components.EventCallback<Microsoft.AspNetCore.Components.ChangeEventArgs> CreateBinder(this Microsoft.AspNetCore.Components.EventCallbackFactory factory, object receiver, System.Action<float> setter, float existingValue, System.Globalization.CultureInfo culture = null) { throw null; }
        public static Microsoft.AspNetCore.Components.EventCallback<Microsoft.AspNetCore.Components.ChangeEventArgs> CreateBinder(this Microsoft.AspNetCore.Components.EventCallbackFactory factory, object receiver, System.Action<string> setter, string existingValue, System.Globalization.CultureInfo culture = null) { throw null; }
        public static Microsoft.AspNetCore.Components.EventCallback<Microsoft.AspNetCore.Components.ChangeEventArgs> CreateBinder<T>(this Microsoft.AspNetCore.Components.EventCallbackFactory factory, object receiver, System.Action<T> setter, T existingValue, System.Globalization.CultureInfo culture = null) { throw null; }
    }
    public static partial class EventCallbackFactoryEventArgsExtensions
    {
        public static Microsoft.AspNetCore.Components.EventCallback<Microsoft.AspNetCore.Components.ChangeEventArgs> Create(this Microsoft.AspNetCore.Components.EventCallbackFactory factory, object receiver, System.Action<Microsoft.AspNetCore.Components.ChangeEventArgs> callback) { throw null; }
        public static Microsoft.AspNetCore.Components.EventCallback<System.EventArgs> Create(this Microsoft.AspNetCore.Components.EventCallbackFactory factory, object receiver, System.Action<System.EventArgs> callback) { throw null; }
        public static Microsoft.AspNetCore.Components.EventCallback<Microsoft.AspNetCore.Components.ChangeEventArgs> Create(this Microsoft.AspNetCore.Components.EventCallbackFactory factory, object receiver, System.Func<Microsoft.AspNetCore.Components.ChangeEventArgs, System.Threading.Tasks.Task> callback) { throw null; }
        public static Microsoft.AspNetCore.Components.EventCallback<System.EventArgs> Create(this Microsoft.AspNetCore.Components.EventCallbackFactory factory, object receiver, System.Func<System.EventArgs, System.Threading.Tasks.Task> callback) { throw null; }
    }
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public readonly partial struct EventCallbackWorkItem
    {
        private readonly object _dummy;
        private readonly int _dummyPrimitive;
        public static readonly Microsoft.AspNetCore.Components.EventCallbackWorkItem Empty;
        public EventCallbackWorkItem(System.MulticastDelegate @delegate) { throw null; }
        public System.Threading.Tasks.Task InvokeAsync(object arg) { throw null; }
    }
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public readonly partial struct EventCallback<TValue>
    {
        private readonly object _dummy;
        private readonly int _dummyPrimitive;
        public static readonly Microsoft.AspNetCore.Components.EventCallback<TValue> Empty;
        public EventCallback(Microsoft.AspNetCore.Components.IHandleEvent receiver, System.MulticastDelegate @delegate) { throw null; }
        public bool HasDelegate { get { throw null; } }
        public System.Threading.Tasks.Task InvokeAsync(TValue arg) { throw null; }
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Class, AllowMultiple=true, Inherited=true)]
    public sealed partial class EventHandlerAttribute : System.Attribute
    {
        public EventHandlerAttribute(string attributeName, System.Type eventArgsType) { }
        public EventHandlerAttribute(string attributeName, System.Type eventArgsType, bool enableStopPropagation, bool enablePreventDefault) { }
        public string AttributeName { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public bool EnablePreventDefault { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public bool EnableStopPropagation { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public System.Type EventArgsType { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
    }
    public partial interface IComponent
    {
        void Attach(Microsoft.AspNetCore.Components.RenderHandle renderHandle);
        System.Threading.Tasks.Task SetParametersAsync(Microsoft.AspNetCore.Components.ParameterView parameters);
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
    [System.AttributeUsageAttribute(System.AttributeTargets.Class, AllowMultiple=false, Inherited=true)]
    public sealed partial class LayoutAttribute : System.Attribute
    {
        public LayoutAttribute(System.Type layoutType) { }
        public System.Type LayoutType { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
    }
    public abstract partial class LayoutComponentBase : Microsoft.AspNetCore.Components.ComponentBase
    {
        protected LayoutComponentBase() { }
        [Microsoft.AspNetCore.Components.ParameterAttribute]
        public Microsoft.AspNetCore.Components.RenderFragment Body { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
    }
    public partial class LayoutView : Microsoft.AspNetCore.Components.IComponent
    {
        public LayoutView() { }
        [Microsoft.AspNetCore.Components.ParameterAttribute]
        public Microsoft.AspNetCore.Components.RenderFragment ChildContent { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        [Microsoft.AspNetCore.Components.ParameterAttribute]
        public System.Type Layout { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public void Attach(Microsoft.AspNetCore.Components.RenderHandle renderHandle) { }
        public System.Threading.Tasks.Task SetParametersAsync(Microsoft.AspNetCore.Components.ParameterView parameters) { throw null; }
    }
    public sealed partial class LocationChangeException : System.Exception
    {
        public LocationChangeException(string message, System.Exception innerException) { }
    }
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public readonly partial struct MarkupString
    {
        private readonly object _dummy;
        private readonly int _dummyPrimitive;
        public MarkupString(string value) { throw null; }
        public string Value { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public static explicit operator Microsoft.AspNetCore.Components.MarkupString (string value) { throw null; }
        public override string ToString() { throw null; }
    }
    public partial class NavigationException : System.Exception
    {
        public NavigationException(string uri) { }
        public string Location { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
    }
    public abstract partial class NavigationManager
    {
        protected NavigationManager() { }
        public string BaseUri { get { throw null; } protected set { } }
        public string Uri { get { throw null; } protected set { } }
        public event System.EventHandler<Microsoft.AspNetCore.Components.Routing.LocationChangedEventArgs> LocationChanged { add { } remove { } }
        protected virtual void EnsureInitialized() { }
        protected void Initialize(string baseUri, string uri) { }
        public void NavigateTo(string uri, bool forceLoad = false) { }
        protected abstract void NavigateToCore(string uri, bool forceLoad);
        protected void NotifyLocationChanged(bool isInterceptedLink) { }
        public System.Uri ToAbsoluteUri(string relativeUri) { throw null; }
        public string ToBaseRelativePath(string uri) { throw null; }
    }
    public abstract partial class OwningComponentBase : Microsoft.AspNetCore.Components.ComponentBase, System.IDisposable
    {
        protected OwningComponentBase() { }
        protected bool IsDisposed { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        protected System.IServiceProvider ScopedServices { get { throw null; } }
        protected virtual void Dispose(bool disposing) { }
        void System.IDisposable.Dispose() { }
    }
    public abstract partial class OwningComponentBase<TService> : Microsoft.AspNetCore.Components.OwningComponentBase, System.IDisposable
    {
        protected OwningComponentBase() { }
        protected TService Service { get { throw null; } }
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Property, AllowMultiple=false, Inherited=true)]
    public sealed partial class ParameterAttribute : System.Attribute
    {
        public ParameterAttribute() { }
        public bool CaptureUnmatchedValues { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
    }
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public readonly partial struct ParameterValue
    {
        private readonly object _dummy;
        private readonly int _dummyPrimitive;
        public bool Cascading { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public string Name { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public object Value { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
    }
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public readonly partial struct ParameterView
    {
        private readonly object _dummy;
        private readonly int _dummyPrimitive;
        public static Microsoft.AspNetCore.Components.ParameterView Empty { get { throw null; } }
        public static Microsoft.AspNetCore.Components.ParameterView FromDictionary(System.Collections.Generic.IDictionary<string, object> parameters) { throw null; }
        public Microsoft.AspNetCore.Components.ParameterView.Enumerator GetEnumerator() { throw null; }
        public TValue GetValueOrDefault<TValue>(string parameterName) { throw null; }
        public TValue GetValueOrDefault<TValue>(string parameterName, TValue defaultValue) { throw null; }
        public void SetParameterProperties(object target) { }
        public System.Collections.Generic.IReadOnlyDictionary<string, object> ToDictionary() { throw null; }
        public bool TryGetValue<TValue>(string parameterName, out TValue result) { throw null; }
        [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
        public partial struct Enumerator
        {
            private object _dummy;
            private int _dummyPrimitive;
            public Microsoft.AspNetCore.Components.ParameterValue Current { get { throw null; } }
            public bool MoveNext() { throw null; }
        }
    }
    public delegate void RenderFragment(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder builder);
    public delegate Microsoft.AspNetCore.Components.RenderFragment RenderFragment<TValue>(TValue value);
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
        public string Template { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
    }
    public sealed partial class RouteData
    {
        public RouteData(System.Type pageType, System.Collections.Generic.IReadOnlyDictionary<string, object> routeValues) { }
        public System.Type PageType { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public System.Collections.Generic.IReadOnlyDictionary<string, object> RouteValues { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
    }
    public partial class RouteView : Microsoft.AspNetCore.Components.IComponent
    {
        public RouteView() { }
        [Microsoft.AspNetCore.Components.ParameterAttribute]
        public System.Type DefaultLayout { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        [Microsoft.AspNetCore.Components.ParameterAttribute]
        public Microsoft.AspNetCore.Components.RouteData RouteData { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public void Attach(Microsoft.AspNetCore.Components.RenderHandle renderHandle) { }
        protected virtual void Render(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder builder) { }
        public System.Threading.Tasks.Task SetParametersAsync(Microsoft.AspNetCore.Components.ParameterView parameters) { throw null; }
    }
}
namespace Microsoft.AspNetCore.Components.CompilerServices
{
    public static partial class RuntimeHelpers
    {
        public static Microsoft.AspNetCore.Components.EventCallback<T> CreateInferredEventCallback<T>(object receiver, System.Action<T> callback, T value) { throw null; }
        public static Microsoft.AspNetCore.Components.EventCallback<T> CreateInferredEventCallback<T>(object receiver, System.Func<T, System.Threading.Tasks.Task> callback, T value) { throw null; }
        public static T TypeCheck<T>(T value) { throw null; }
    }
}
namespace Microsoft.AspNetCore.Components.Rendering
{
    public sealed partial class RenderTreeBuilder : System.IDisposable
    {
        public RenderTreeBuilder() { }
        public void AddAttribute(int sequence, in Microsoft.AspNetCore.Components.RenderTree.RenderTreeFrame frame) { }
        public void AddAttribute(int sequence, string name, Microsoft.AspNetCore.Components.EventCallback value) { }
        public void AddAttribute(int sequence, string name, bool value) { }
        public void AddAttribute(int sequence, string name, System.MulticastDelegate value) { }
        public void AddAttribute(int sequence, string name, object value) { }
        public void AddAttribute(int sequence, string name, string value) { }
        public void AddAttribute<TArgument>(int sequence, string name, Microsoft.AspNetCore.Components.EventCallback<TArgument> value) { }
        public void AddComponentReferenceCapture(int sequence, System.Action<object> componentReferenceCaptureAction) { }
        public void AddContent(int sequence, Microsoft.AspNetCore.Components.MarkupString markupContent) { }
        public void AddContent(int sequence, Microsoft.AspNetCore.Components.RenderFragment fragment) { }
        public void AddContent(int sequence, object textContent) { }
        public void AddContent(int sequence, string textContent) { }
        public void AddContent<TValue>(int sequence, Microsoft.AspNetCore.Components.RenderFragment<TValue> fragment, TValue value) { }
        public void AddElementReferenceCapture(int sequence, System.Action<Microsoft.AspNetCore.Components.ElementReference> elementReferenceCaptureAction) { }
        public void AddMarkupContent(int sequence, string markupContent) { }
        public void AddMultipleAttributes(int sequence, System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<string, object>> attributes) { }
        public void Clear() { }
        public void CloseComponent() { }
        public void CloseElement() { }
        public void CloseRegion() { }
        public Microsoft.AspNetCore.Components.RenderTree.ArrayRange<Microsoft.AspNetCore.Components.RenderTree.RenderTreeFrame> GetFrames() { throw null; }
        public void OpenComponent(int sequence, System.Type componentType) { }
        public void OpenComponent<TComponent>(int sequence) where TComponent : Microsoft.AspNetCore.Components.IComponent { }
        public void OpenElement(int sequence, string elementName) { }
        public void OpenRegion(int sequence) { }
        public void SetKey(object value) { }
        public void SetUpdatesAttributeName(string updatesAttributeName) { }
        void System.IDisposable.Dispose() { }
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
    public partial class EventFieldInfo
    {
        public EventFieldInfo() { }
        public int ComponentId { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public object FieldValue { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
    }
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public readonly partial struct RenderBatch
    {
        private readonly object _dummy;
        private readonly int _dummyPrimitive;
        public Microsoft.AspNetCore.Components.RenderTree.ArrayRange<int> DisposedComponentIDs { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public Microsoft.AspNetCore.Components.RenderTree.ArrayRange<ulong> DisposedEventHandlerIDs { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public Microsoft.AspNetCore.Components.RenderTree.ArrayRange<Microsoft.AspNetCore.Components.RenderTree.RenderTreeFrame> ReferenceFrames { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public Microsoft.AspNetCore.Components.RenderTree.ArrayRange<Microsoft.AspNetCore.Components.RenderTree.RenderTreeDiff> UpdatedComponents { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
    }
    public abstract partial class Renderer : System.IDisposable
    {
        public Renderer(System.IServiceProvider serviceProvider, Microsoft.Extensions.Logging.ILoggerFactory loggerFactory) { }
        public abstract Microsoft.AspNetCore.Components.Dispatcher Dispatcher { get; }
        public event System.UnhandledExceptionEventHandler UnhandledSynchronizationException { add { } remove { } }
        protected internal int AssignRootComponentId(Microsoft.AspNetCore.Components.IComponent component) { throw null; }
        public virtual System.Threading.Tasks.Task DispatchEventAsync(ulong eventHandlerId, Microsoft.AspNetCore.Components.RenderTree.EventFieldInfo fieldInfo, System.EventArgs eventArgs) { throw null; }
        public void Dispose() { }
        protected virtual void Dispose(bool disposing) { }
        protected Microsoft.AspNetCore.Components.RenderTree.ArrayRange<Microsoft.AspNetCore.Components.RenderTree.RenderTreeFrame> GetCurrentRenderTreeFrames(int componentId) { throw null; }
        protected abstract void HandleException(System.Exception exception);
        protected Microsoft.AspNetCore.Components.IComponent InstantiateComponent(System.Type componentType) { throw null; }
        protected virtual void ProcessPendingRender() { }
        protected System.Threading.Tasks.Task RenderRootComponentAsync(int componentId) { throw null; }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        protected System.Threading.Tasks.Task RenderRootComponentAsync(int componentId, Microsoft.AspNetCore.Components.ParameterView initialParameters) { throw null; }
        protected abstract System.Threading.Tasks.Task UpdateDisplayAsync(in Microsoft.AspNetCore.Components.RenderTree.RenderBatch renderBatch);
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
    public enum RenderTreeFrameType : short
    {
        None = (short)0,
        Element = (short)1,
        Text = (short)2,
        Attribute = (short)3,
        Component = (short)4,
        Region = (short)5,
        ElementReferenceCapture = (short)6,
        ComponentReferenceCapture = (short)7,
        Markup = (short)8,
    }
}
namespace Microsoft.AspNetCore.Components.Routing
{
    public partial interface IHostEnvironmentNavigationManager
    {
        void Initialize(string baseUri, string uri);
    }
    public partial interface INavigationInterception
    {
        System.Threading.Tasks.Task EnableNavigationInterceptionAsync();
    }
    public partial class LocationChangedEventArgs : System.EventArgs
    {
        public LocationChangedEventArgs(string location, bool isNavigationIntercepted) { }
        public bool IsNavigationIntercepted { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public string Location { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
    }
    public partial class Router : Microsoft.AspNetCore.Components.IComponent, Microsoft.AspNetCore.Components.IHandleAfterRender, System.IDisposable
    {
        public Router() { }
        [Microsoft.AspNetCore.Components.ParameterAttribute]
        public System.Collections.Generic.IEnumerable<System.Reflection.Assembly> AdditionalAssemblies { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        [Microsoft.AspNetCore.Components.ParameterAttribute]
        public System.Reflection.Assembly AppAssembly { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        [Microsoft.AspNetCore.Components.ParameterAttribute]
        public Microsoft.AspNetCore.Components.RenderFragment<Microsoft.AspNetCore.Components.RouteData> Found { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        [Microsoft.AspNetCore.Components.ParameterAttribute]
        public Microsoft.AspNetCore.Components.RenderFragment NotFound { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public void Attach(Microsoft.AspNetCore.Components.RenderHandle renderHandle) { }
        public void Dispose() { }
        System.Threading.Tasks.Task Microsoft.AspNetCore.Components.IHandleAfterRender.OnAfterRenderAsync() { throw null; }
        public System.Threading.Tasks.Task SetParametersAsync(Microsoft.AspNetCore.Components.ParameterView parameters) { throw null; }
    }
}
