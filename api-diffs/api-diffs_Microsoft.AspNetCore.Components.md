# Microsoft.AspNetCore.Components

``` diff
+namespace Microsoft.AspNetCore.Components {
+    public class AuthenticationState {
+        public AuthenticationState(ClaimsPrincipal user);
+        public ClaimsPrincipal User { get; }
+    }
+    public delegate void AuthenticationStateChangedHandler(Task<AuthenticationState> task);
+    public abstract class AuthenticationStateProvider {
+        protected AuthenticationStateProvider();
+        public event AuthenticationStateChangedHandler AuthenticationStateChanged;
+        public abstract Task<AuthenticationState> GetAuthenticationStateAsync();
+        protected void NotifyAuthenticationStateChanged(Task<AuthenticationState> task);
+    }
+    public class AuthorizeView : AuthorizeViewCore {
+        public AuthorizeView();
+        public string Policy { get; private set; }
+        public object Resource { get; private set; }
+        public string Roles { get; private set; }
+        protected override IAuthorizeData[] GetAuthorizeData();
+    }
+    public abstract class AuthorizeViewCore : ComponentBase {
+        public AuthorizeViewCore();
+        public RenderFragment<AuthenticationState> Authorized { get; private set; }
+        public RenderFragment Authorizing { get; private set; }
+        public RenderFragment<AuthenticationState> ChildContent { get; private set; }
+        public RenderFragment<AuthenticationState> NotAuthorized { get; private set; }
+        protected override void BuildRenderTree(RenderTreeBuilder builder);
+        protected abstract IAuthorizeData[] GetAuthorizeData();
+        protected override Task OnParametersSetAsync();
+    }
+    public static class BindAttributes
+    public sealed class BindElementAttribute : Attribute {
+        public BindElementAttribute(string element, string suffix, string valueAttribute, string changeAttribute);
+        public string ChangeAttribute { get; }
+        public string Element { get; }
+        public string Suffix { get; }
+        public string ValueAttribute { get; }
+    }
+    public sealed class BindInputElementAttribute : Attribute {
+        public BindInputElementAttribute(string type, string suffix, string valueAttribute, string changeAttribute);
+        public string ChangeAttribute { get; }
+        public string Suffix { get; }
+        public string Type { get; }
+        public string ValueAttribute { get; }
+    }
+    public static class BindMethods {
+        public static EventCallback GetEventHandlerValue<T>(EventCallback value) where T : UIEventArgs;
+        public static EventCallback<T> GetEventHandlerValue<T>(EventCallback<T> value) where T : UIEventArgs;
+        public static MulticastDelegate GetEventHandlerValue<T>(Action value) where T : UIEventArgs;
+        public static MulticastDelegate GetEventHandlerValue<T>(Action<T> value) where T : UIEventArgs;
+        public static MulticastDelegate GetEventHandlerValue<T>(Func<Task> value) where T : UIEventArgs;
+        public static MulticastDelegate GetEventHandlerValue<T>(Func<T, Task> value) where T : UIEventArgs;
+        public static string GetEventHandlerValue<T>(string value) where T : UIEventArgs;
+        public static string GetValue(DateTime value, string format);
+        public static T GetValue<T>(T value);
+        public static Action<UIEventArgs> SetValueHandler(Action<bool> setter, bool existingValue);
+        public static Action<UIEventArgs> SetValueHandler(Action<DateTime> setter, DateTime existingValue);
+        public static Action<UIEventArgs> SetValueHandler(Action<DateTime> setter, DateTime existingValue, string format);
+        public static Action<UIEventArgs> SetValueHandler(Action<Decimal> setter, Decimal existingValue);
+        public static Action<UIEventArgs> SetValueHandler(Action<double> setter, double existingValue);
+        public static Action<UIEventArgs> SetValueHandler(Action<int> setter, int existingValue);
+        public static Action<UIEventArgs> SetValueHandler(Action<long> setter, long existingValue);
+        public static Action<UIEventArgs> SetValueHandler(Action<Nullable<bool>> setter, Nullable<bool> existingValue);
+        public static Action<UIEventArgs> SetValueHandler(Action<Nullable<Decimal>> setter, Nullable<Decimal> existingValue);
+        public static Action<UIEventArgs> SetValueHandler(Action<Nullable<double>> setter, Nullable<double> existingValue);
+        public static Action<UIEventArgs> SetValueHandler(Action<Nullable<int>> setter, Nullable<int> existingValue);
+        public static Action<UIEventArgs> SetValueHandler(Action<Nullable<long>> setter, Nullable<long> existingValue);
+        public static Action<UIEventArgs> SetValueHandler(Action<Nullable<float>> setter, Nullable<float> existingValue);
+        public static Action<UIEventArgs> SetValueHandler(Action<float> setter, float existingValue);
+        public static Action<UIEventArgs> SetValueHandler(Action<string> setter, string existingValue);
+        public static Action<UIEventArgs> SetValueHandler<T>(Action<T> setter, T existingValue);
+    }
+    public class CascadingAuthenticationState : ComponentBase, IDisposable {
+        public CascadingAuthenticationState();
+        public RenderFragment ChildContent { get; private set; }
+        protected override void BuildRenderTree(RenderTreeBuilder builder);
+        protected override void OnInit();
+        void System.IDisposable.Dispose();
+    }
+    public sealed class CascadingParameterAttribute : Attribute {
+        public CascadingParameterAttribute();
+        public string Name { get; set; }
+    }
+    public class CascadingValue<T> : IComponent {
+        public CascadingValue();
+        public RenderFragment ChildContent { get; private set; }
+        public bool IsFixed { get; private set; }
+        public string Name { get; private set; }
+        public T Value { get; private set; }
+        public void Configure(RenderHandle renderHandle);
+        public Task SetParametersAsync(ParameterCollection parameters);
+    }
+    public abstract class ComponentBase : IComponent, IHandleAfterRender, IHandleEvent {
+        public ComponentBase();
+        protected virtual void BuildRenderTree(RenderTreeBuilder builder);
+        protected Task Invoke(Action workItem);
+        protected Task InvokeAsync(Func<Task> workItem);
+        void Microsoft.AspNetCore.Components.IComponent.Configure(RenderHandle renderHandle);
+        Task Microsoft.AspNetCore.Components.IHandleAfterRender.OnAfterRenderAsync();
+        Task Microsoft.AspNetCore.Components.IHandleEvent.HandleEventAsync(EventCallbackWorkItem callback, object arg);
+        protected virtual void OnAfterRender();
+        protected virtual Task OnAfterRenderAsync();
+        protected virtual void OnInit();
+        protected virtual Task OnInitAsync();
+        protected virtual void OnParametersSet();
+        protected virtual Task OnParametersSetAsync();
+        public virtual Task SetParametersAsync(ParameterCollection parameters);
+        protected virtual bool ShouldRender();
+        protected void StateHasChanged();
+    }
+    public class DataTransfer {
+        public DataTransfer();
+        public string DropEffect { get; set; }
+        public string EffectAllowed { get; set; }
+        public string[] Files { get; set; }
+        public UIDataTransferItem[] Items { get; set; }
+        public string[] Types { get; set; }
+    }
+    public readonly struct ElementRef {
+        public string __internalId { get; }
+    }
+    public readonly struct EventCallback {
+        public static readonly EventCallback Empty;
+        public static readonly EventCallbackFactory Factory;
+        public EventCallback(IHandleEvent receiver, MulticastDelegate @delegate);
+        public bool HasDelegate { get; }
+        public Task InvokeAsync(object arg);
+    }
+    public readonly struct EventCallback<T> {
+        public EventCallback(IHandleEvent receiver, MulticastDelegate @delegate);
+        public bool HasDelegate { get; }
+        public Task InvokeAsync(T arg);
+    }
+    public sealed class EventCallbackFactory {
+        public EventCallbackFactory();
+        public EventCallback Create(object receiver, EventCallback callback);
+        public EventCallback Create(object receiver, Action callback);
+        public EventCallback Create(object receiver, Action<object> callback);
+        public EventCallback Create(object receiver, Func<object, Task> callback);
+        public EventCallback Create(object receiver, Func<Task> callback);
+        public EventCallback<T> Create<T>(object receiver, EventCallback callback);
+        public EventCallback<T> Create<T>(object receiver, EventCallback<T> callback);
+        public EventCallback<T> Create<T>(object receiver, Action callback);
+        public EventCallback<T> Create<T>(object receiver, Action<T> callback);
+        public EventCallback<T> Create<T>(object receiver, Func<Task> callback);
+        public EventCallback<T> Create<T>(object receiver, Func<T, Task> callback);
+        public string Create<T>(object receiver, string callback);
+        public EventCallback<T> CreateInferred<T>(object receiver, Action<T> callback, T value);
+        public EventCallback<T> CreateInferred<T>(object receiver, Func<T, Task> callback, T value);
+    }
+    public static class EventCallbackFactoryBinderExtensions {
+        public static EventCallback<UIChangeEventArgs> CreateBinder(this EventCallbackFactory factory, object receiver, Action<bool> setter, bool existingValue);
+        public static EventCallback<UIChangeEventArgs> CreateBinder(this EventCallbackFactory factory, object receiver, Action<DateTime> setter, DateTime existingValue);
+        public static EventCallback<UIChangeEventArgs> CreateBinder(this EventCallbackFactory factory, object receiver, Action<DateTime> setter, DateTime existingValue, string format);
+        public static EventCallback<UIChangeEventArgs> CreateBinder(this EventCallbackFactory factory, object receiver, Action<Decimal> setter, Decimal existingValue);
+        public static EventCallback<UIChangeEventArgs> CreateBinder(this EventCallbackFactory factory, object receiver, Action<double> setter, double existingValue);
+        public static EventCallback<UIChangeEventArgs> CreateBinder(this EventCallbackFactory factory, object receiver, Action<int> setter, int existingValue);
+        public static EventCallback<UIChangeEventArgs> CreateBinder(this EventCallbackFactory factory, object receiver, Action<long> setter, long existingValue);
+        public static EventCallback<UIChangeEventArgs> CreateBinder(this EventCallbackFactory factory, object receiver, Action<Nullable<bool>> setter, Nullable<bool> existingValue);
+        public static EventCallback<UIChangeEventArgs> CreateBinder(this EventCallbackFactory factory, object receiver, Action<Nullable<DateTime>> setter, Nullable<DateTime> existingValue);
+        public static EventCallback<UIChangeEventArgs> CreateBinder(this EventCallbackFactory factory, object receiver, Action<Nullable<Decimal>> setter, Nullable<Decimal> existingValue);
+        public static EventCallback<UIChangeEventArgs> CreateBinder(this EventCallbackFactory factory, object receiver, Action<Nullable<double>> setter, Nullable<double> existingValue);
+        public static EventCallback<UIChangeEventArgs> CreateBinder(this EventCallbackFactory factory, object receiver, Action<Nullable<int>> setter, Nullable<int> existingValue);
+        public static EventCallback<UIChangeEventArgs> CreateBinder(this EventCallbackFactory factory, object receiver, Action<Nullable<long>> setter, Nullable<long> existingValue);
+        public static EventCallback<UIChangeEventArgs> CreateBinder(this EventCallbackFactory factory, object receiver, Action<Nullable<float>> setter, Nullable<float> existingValue);
+        public static EventCallback<UIChangeEventArgs> CreateBinder(this EventCallbackFactory factory, object receiver, Action<float> setter, float existingValue);
+        public static EventCallback<UIChangeEventArgs> CreateBinder(this EventCallbackFactory factory, object receiver, Action<string> setter, string existingValue);
+        public static EventCallback<UIChangeEventArgs> CreateBinder<T>(this EventCallbackFactory factory, object receiver, Action<T> setter, T existingValue);
+    }
+    public static class EventCallbackFactoryUIEventArgsExtensions {
+        public static EventCallback<UIChangeEventArgs> Create(this EventCallbackFactory factory, object receiver, Action<UIChangeEventArgs> callback);
+        public static EventCallback<UIClipboardEventArgs> Create(this EventCallbackFactory factory, object receiver, Action<UIClipboardEventArgs> callback);
+        public static EventCallback<UIDragEventArgs> Create(this EventCallbackFactory factory, object receiver, Action<UIDragEventArgs> callback);
+        public static EventCallback<UIErrorEventArgs> Create(this EventCallbackFactory factory, object receiver, Action<UIErrorEventArgs> callback);
+        public static EventCallback<UIEventArgs> Create(this EventCallbackFactory factory, object receiver, Action<UIEventArgs> callback);
+        public static EventCallback<UIFocusEventArgs> Create(this EventCallbackFactory factory, object receiver, Action<UIFocusEventArgs> callback);
+        public static EventCallback<UIKeyboardEventArgs> Create(this EventCallbackFactory factory, object receiver, Action<UIKeyboardEventArgs> callback);
+        public static EventCallback<UIMouseEventArgs> Create(this EventCallbackFactory factory, object receiver, Action<UIMouseEventArgs> callback);
+        public static EventCallback<UIPointerEventArgs> Create(this EventCallbackFactory factory, object receiver, Action<UIPointerEventArgs> callback);
+        public static EventCallback<UIProgressEventArgs> Create(this EventCallbackFactory factory, object receiver, Action<UIProgressEventArgs> callback);
+        public static EventCallback<UITouchEventArgs> Create(this EventCallbackFactory factory, object receiver, Action<UITouchEventArgs> callback);
+        public static EventCallback<UIWheelEventArgs> Create(this EventCallbackFactory factory, object receiver, Action<UIWheelEventArgs> callback);
+        public static EventCallback<UIChangeEventArgs> Create(this EventCallbackFactory factory, object receiver, Func<UIChangeEventArgs, Task> callback);
+        public static EventCallback<UIClipboardEventArgs> Create(this EventCallbackFactory factory, object receiver, Func<UIClipboardEventArgs, Task> callback);
+        public static EventCallback<UIDragEventArgs> Create(this EventCallbackFactory factory, object receiver, Func<UIDragEventArgs, Task> callback);
+        public static EventCallback<UIErrorEventArgs> Create(this EventCallbackFactory factory, object receiver, Func<UIErrorEventArgs, Task> callback);
+        public static EventCallback<UIEventArgs> Create(this EventCallbackFactory factory, object receiver, Func<UIEventArgs, Task> callback);
+        public static EventCallback<UIFocusEventArgs> Create(this EventCallbackFactory factory, object receiver, Func<UIFocusEventArgs, Task> callback);
+        public static EventCallback<UIKeyboardEventArgs> Create(this EventCallbackFactory factory, object receiver, Func<UIKeyboardEventArgs, Task> callback);
+        public static EventCallback<UIMouseEventArgs> Create(this EventCallbackFactory factory, object receiver, Func<UIMouseEventArgs, Task> callback);
+        public static EventCallback<UIPointerEventArgs> Create(this EventCallbackFactory factory, object receiver, Func<UIPointerEventArgs, Task> callback);
+        public static EventCallback<UIProgressEventArgs> Create(this EventCallbackFactory factory, object receiver, Func<UIProgressEventArgs, Task> callback);
+        public static EventCallback<UITouchEventArgs> Create(this EventCallbackFactory factory, object receiver, Func<UITouchEventArgs, Task> callback);
+        public static EventCallback<UIWheelEventArgs> Create(this EventCallbackFactory factory, object receiver, Func<UIWheelEventArgs, Task> callback);
+    }
+    public struct EventCallbackWorkItem {
+        public static readonly EventCallbackWorkItem Empty;
+        public EventCallbackWorkItem(MulticastDelegate @delegate);
+        public Task InvokeAsync(object arg);
+    }
+    public sealed class EventHandlerAttribute : Attribute {
+        public EventHandlerAttribute(string attributeName, Type eventArgsType);
+        public string AttributeName { get; }
+        public Type EventArgsType { get; }
+    }
+    public static class EventHandlers
+    public static class HttpClientJsonExtensions {
+        public static Task<T> GetJsonAsync<T>(this HttpClient httpClient, string requestUri);
+        public static Task PostJsonAsync(this HttpClient httpClient, string requestUri, object content);
+        public static Task<T> PostJsonAsync<T>(this HttpClient httpClient, string requestUri, object content);
+        public static Task PutJsonAsync(this HttpClient httpClient, string requestUri, object content);
+        public static Task<T> PutJsonAsync<T>(this HttpClient httpClient, string requestUri, object content);
+        public static Task SendJsonAsync(this HttpClient httpClient, HttpMethod method, string requestUri, object content);
+        public static Task<T> SendJsonAsync<T>(this HttpClient httpClient, HttpMethod method, string requestUri, object content);
+    }
+    public interface IComponent {
+        void Configure(RenderHandle renderHandle);
+        Task SetParametersAsync(ParameterCollection parameters);
+    }
+    public interface IComponentContext {
+        bool IsConnected { get; }
+    }
+    public interface IHandleAfterRender {
+        Task OnAfterRenderAsync();
+    }
+    public interface IHandleEvent {
+        Task HandleEventAsync(EventCallbackWorkItem item, object arg);
+    }
+    public class InjectAttribute : Attribute {
+        public InjectAttribute();
+    }
+    public interface IUriHelper {
+        event EventHandler<LocationChangedEventArgs> OnLocationChanged;
+        string GetAbsoluteUri();
+        string GetBaseUri();
+        void NavigateTo(string uri);
+        void NavigateTo(string uri, bool forceLoad);
+        Uri ToAbsoluteUri(string href);
+        string ToBaseRelativePath(string baseUri, string locationAbsolute);
+    }
+    public readonly struct MarkupString {
+        public MarkupString(string value);
+        public string Value { get; }
+        public static explicit operator MarkupString (string value);
+        public override string ToString();
+    }
+    public class NavigationException : Exception {
+        public NavigationException(string uri);
+        public string Location { get; }
+    }
+    public readonly struct Parameter {
+        public bool Cascading { get; }
+        public string Name { get; }
+        public object Value { get; }
+    }
+    public sealed class ParameterAttribute : Attribute {
+        public ParameterAttribute();
+        public bool CaptureUnmatchedValues { get; set; }
+    }
+    public readonly struct ParameterCollection {
+        public static ParameterCollection Empty { get; }
+        public static ParameterCollection FromDictionary(IDictionary<string, object> parameters);
+        public ParameterEnumerator GetEnumerator();
+        public T GetValueOrDefault<T>(string parameterName);
+        public T GetValueOrDefault<T>(string parameterName, T defaultValue);
+        public IReadOnlyDictionary<string, object> ToDictionary();
+        public bool TryGetValue<T>(string parameterName, out T result);
+    }
+    public static class ParameterCollectionExtensions {
+        public static void SetParameterProperties(this in ParameterCollection parameterCollection, object target);
+    }
+    public struct ParameterEnumerator {
+        public Parameter Current { get; }
+        public bool MoveNext();
+    }
+    public delegate void RenderFragment(RenderTreeBuilder builder);
+    public delegate RenderFragment RenderFragment<T>(T value);
+    public readonly struct RenderHandle {
+        public bool IsInitialized { get; }
+        public Task Invoke(Action workItem);
+        public Task InvokeAsync(Func<Task> workItem);
+        public void Render(RenderFragment renderFragment);
+    }
+    public class RouteAttribute : Attribute {
+        public RouteAttribute(string template);
+        public string Template { get; }
+    }
+    public static class RuntimeHelpers {
+        public static T TypeCheck<T>(T value);
+    }
+    public class UIChangeEventArgs : UIEventArgs {
+        public UIChangeEventArgs();
+        public object Value { get; set; }
+    }
+    public class UIClipboardEventArgs : UIEventArgs {
+        public UIClipboardEventArgs();
+    }
+    public class UIDataTransferItem {
+        public UIDataTransferItem();
+        public string Kind { get; set; }
+        public string Type { get; set; }
+    }
+    public class UIDragEventArgs : UIEventArgs {
+        public UIDragEventArgs();
+        public bool AltKey { get; set; }
+        public long Button { get; set; }
+        public long Buttons { get; set; }
+        public long ClientX { get; set; }
+        public long ClientY { get; set; }
+        public bool CtrlKey { get; set; }
+        public DataTransfer DataTransfer { get; set; }
+        public long Detail { get; set; }
+        public bool MetaKey { get; set; }
+        public long ScreenX { get; set; }
+        public long ScreenY { get; set; }
+        public bool ShiftKey { get; set; }
+    }
+    public class UIErrorEventArgs : UIEventArgs {
+        public UIErrorEventArgs();
+        public int Colno { get; set; }
+        public string Filename { get; set; }
+        public int Lineno { get; set; }
+        public string Message { get; set; }
+    }
+    public class UIEventArgs {
+        public static readonly UIEventArgs Empty;
+        public UIEventArgs();
+        public string Type { get; set; }
+    }
+    public static class UIEventArgsRenderTreeBuilderExtensions {
+        public static void AddAttribute(this RenderTreeBuilder builder, int sequence, string name, Action<UIChangeEventArgs> value);
+        public static void AddAttribute(this RenderTreeBuilder builder, int sequence, string name, Action<UIClipboardEventArgs> value);
+        public static void AddAttribute(this RenderTreeBuilder builder, int sequence, string name, Action<UIDragEventArgs> value);
+        public static void AddAttribute(this RenderTreeBuilder builder, int sequence, string name, Action<UIErrorEventArgs> value);
+        public static void AddAttribute(this RenderTreeBuilder builder, int sequence, string name, Action<UIFocusEventArgs> value);
+        public static void AddAttribute(this RenderTreeBuilder builder, int sequence, string name, Action<UIKeyboardEventArgs> value);
+        public static void AddAttribute(this RenderTreeBuilder builder, int sequence, string name, Action<UIMouseEventArgs> value);
+        public static void AddAttribute(this RenderTreeBuilder builder, int sequence, string name, Action<UIPointerEventArgs> value);
+        public static void AddAttribute(this RenderTreeBuilder builder, int sequence, string name, Action<UIProgressEventArgs> value);
+        public static void AddAttribute(this RenderTreeBuilder builder, int sequence, string name, Action<UITouchEventArgs> value);
+        public static void AddAttribute(this RenderTreeBuilder builder, int sequence, string name, Action<UIWheelEventArgs> value);
+        public static void AddAttribute(this RenderTreeBuilder builder, int sequence, string name, Func<UIChangeEventArgs, Task> value);
+        public static void AddAttribute(this RenderTreeBuilder builder, int sequence, string name, Func<UIClipboardEventArgs, Task> value);
+        public static void AddAttribute(this RenderTreeBuilder builder, int sequence, string name, Func<UIDragEventArgs, Task> value);
+        public static void AddAttribute(this RenderTreeBuilder builder, int sequence, string name, Func<UIErrorEventArgs, Task> value);
+        public static void AddAttribute(this RenderTreeBuilder builder, int sequence, string name, Func<UIFocusEventArgs, Task> value);
+        public static void AddAttribute(this RenderTreeBuilder builder, int sequence, string name, Func<UIKeyboardEventArgs, Task> value);
+        public static void AddAttribute(this RenderTreeBuilder builder, int sequence, string name, Func<UIMouseEventArgs, Task> value);
+        public static void AddAttribute(this RenderTreeBuilder builder, int sequence, string name, Func<UIPointerEventArgs, Task> value);
+        public static void AddAttribute(this RenderTreeBuilder builder, int sequence, string name, Func<UIProgressEventArgs, Task> value);
+        public static void AddAttribute(this RenderTreeBuilder builder, int sequence, string name, Func<UITouchEventArgs, Task> value);
+        public static void AddAttribute(this RenderTreeBuilder builder, int sequence, string name, Func<UIWheelEventArgs, Task> value);
+    }
+    public class UIFocusEventArgs : UIEventArgs {
+        public UIFocusEventArgs();
+    }
+    public class UIKeyboardEventArgs : UIEventArgs {
+        public UIKeyboardEventArgs();
+        public bool AltKey { get; set; }
+        public string Code { get; set; }
+        public bool CtrlKey { get; set; }
+        public string Key { get; set; }
+        public float Location { get; set; }
+        public bool MetaKey { get; set; }
+        public bool Repeat { get; set; }
+        public bool ShiftKey { get; set; }
+    }
+    public class UIMouseEventArgs : UIEventArgs {
+        public UIMouseEventArgs();
+        public bool AltKey { get; set; }
+        public long Button { get; set; }
+        public long Buttons { get; set; }
+        public long ClientX { get; set; }
+        public long ClientY { get; set; }
+        public bool CtrlKey { get; set; }
+        public long Detail { get; set; }
+        public bool MetaKey { get; set; }
+        public long ScreenX { get; set; }
+        public long ScreenY { get; set; }
+        public bool ShiftKey { get; set; }
+    }
+    public class UIPointerEventArgs : UIMouseEventArgs {
+        public UIPointerEventArgs();
+        public float Height { get; set; }
+        public bool IsPrimary { get; set; }
+        public long PointerId { get; set; }
+        public string PointerType { get; set; }
+        public float Pressure { get; set; }
+        public float TiltX { get; set; }
+        public float TiltY { get; set; }
+        public float Width { get; set; }
+    }
+    public class UIProgressEventArgs : UIEventArgs {
+        public UIProgressEventArgs();
+        public bool LengthComputable { get; set; }
+        public long Loaded { get; set; }
+        public long Total { get; set; }
+    }
+    public class UITouchEventArgs : UIEventArgs {
+        public UITouchEventArgs();
+        public bool AltKey { get; set; }
+        public UITouchPoint[] ChangedTouches { get; set; }
+        public bool CtrlKey { get; set; }
+        public long Detail { get; set; }
+        public bool MetaKey { get; set; }
+        public bool ShiftKey { get; set; }
+        public UITouchPoint[] TargetTouches { get; set; }
+        public UITouchPoint[] Touches { get; set; }
+    }
+    public class UITouchPoint {
+        public UITouchPoint();
+        public long ClientX { get; set; }
+        public long ClientY { get; set; }
+        public long Identifier { get; set; }
+        public long PageX { get; set; }
+        public long PageY { get; set; }
+        public long ScreenX { get; set; }
+        public long ScreenY { get; set; }
+    }
+    public class UIWheelEventArgs : UIMouseEventArgs {
+        public UIWheelEventArgs();
+        public long DeltaMode { get; set; }
+        public double DeltaX { get; set; }
+        public double DeltaY { get; set; }
+        public double DeltaZ { get; set; }
+    }
+    public abstract class UriHelperBase : IUriHelper {
+        protected UriHelperBase();
+        public event EventHandler<LocationChangedEventArgs> OnLocationChanged;
+        protected virtual void EnsureInitialized();
+        public string GetAbsoluteUri();
+        public virtual string GetBaseUri();
+        public virtual void InitializeState(string uriAbsolute, string baseUriAbsolute);
+        public void NavigateTo(string uri);
+        public void NavigateTo(string uri, bool forceLoad);
+        protected abstract void NavigateToCore(string uri, bool forceLoad);
+        protected void SetAbsoluteBaseUri(string baseUri);
+        protected void SetAbsoluteUri(string uri);
+        public Uri ToAbsoluteUri(string href);
+        public string ToBaseRelativePath(string baseUri, string locationAbsolute);
+        protected void TriggerOnLocationChanged(bool isinterceptedLink);
+    }
+}
```

