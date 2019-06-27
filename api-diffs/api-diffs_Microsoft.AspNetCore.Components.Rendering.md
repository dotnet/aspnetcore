# Microsoft.AspNetCore.Components.Rendering

``` diff
+namespace Microsoft.AspNetCore.Components.Rendering {
+    public readonly struct ComponentRenderedText {
+        public int ComponentId { get; }
+        public IEnumerable<string> Tokens { get; }
+    }
+    public class EventFieldInfo {
+        public EventFieldInfo();
+        public int ComponentId { get; set; }
+        public object FieldValue { get; set; }
+    }
+    public class HtmlRenderer : Renderer {
+        public HtmlRenderer(IServiceProvider serviceProvider, Func<string, string> htmlEncoder, IDispatcher dispatcher);
+        protected override void HandleException(Exception exception);
+        public Task<ComponentRenderedText> RenderComponentAsync(Type componentType, ParameterCollection initialParameters);
+        public Task<ComponentRenderedText> RenderComponentAsync<TComponent>(ParameterCollection initialParameters) where TComponent : IComponent;
+        protected override Task UpdateDisplayAsync(in RenderBatch renderBatch);
+    }
+    public interface IDispatcher {
+        Task Invoke(Action action);
+        Task<TResult> Invoke<TResult>(Func<TResult> function);
+        Task InvokeAsync(Func<Task> asyncAction);
+        Task<TResult> InvokeAsync<TResult>(Func<Task<TResult>> asyncFunction);
+    }
+    public readonly struct RenderBatch {
+        public ArrayRange<int> DisposedComponentIDs { get; }
+        public ArrayRange<int> DisposedEventHandlerIDs { get; }
+        public ArrayRange<RenderTreeFrame> ReferenceFrames { get; }
+        public ArrayRange<RenderTreeDiff> UpdatedComponents { get; }
+    }
+    public abstract class Renderer : IDisposable {
+        public Renderer(IServiceProvider serviceProvider);
+        public Renderer(IServiceProvider serviceProvider, IDispatcher dispatcher);
+        public event UnhandledExceptionEventHandler UnhandledSynchronizationException;
+        protected internal virtual void AddToRenderQueue(int componentId, RenderFragment renderFragment);
+        protected internal int AssignRootComponentId(IComponent component);
+        public static IDispatcher CreateDefaultDispatcher();
+        public virtual Task DispatchEventAsync(int eventHandlerId, EventFieldInfo fieldInfo, UIEventArgs eventArgs);
+        public void Dispose();
+        protected virtual void Dispose(bool disposing);
+        protected abstract void HandleException(Exception exception);
+        protected IComponent InstantiateComponent(Type componentType);
+        public virtual Task Invoke(Action workItem);
+        public virtual Task InvokeAsync(Func<Task> workItem);
+        protected Task RenderRootComponentAsync(int componentId);
+        protected Task RenderRootComponentAsync(int componentId, ParameterCollection initialParameters);
+        protected abstract Task UpdateDisplayAsync(in RenderBatch renderBatch);
+    }
+}
```

