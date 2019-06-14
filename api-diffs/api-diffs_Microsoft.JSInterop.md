# Microsoft.JSInterop

``` diff
+namespace Microsoft.JSInterop {
+    public static class DotNetDispatcher {
+        public static void BeginInvoke(string callId, string assemblyName, string methodIdentifier, long dotNetObjectId, string argsJson);
+        public static void EndInvoke(long asyncHandle, bool succeeded, JSAsyncCallResult result);
+        public static string Invoke(string assemblyName, string methodIdentifier, long dotNetObjectId, string argsJson);
+        public static void ReleaseDotNetObject(long dotNetObjectId);
+    }
+    public static class DotNetObjectRef {
+        public static DotNetObjectRef<TValue> Create<TValue>(TValue value) where TValue : class;
+    }
+    public sealed class DotNetObjectRef<TValue> : IDisposable, IDotNetObjectRef where TValue : class {
+        public DotNetObjectRef();
+        public TValue Value { get; private set; }
+        public long __dotNetObject { get; set; }
+        public void Dispose();
+    }
+    public interface IJSInProcessRuntime : IJSRuntime {
+        T Invoke<T>(string identifier, params object[] args);
+    }
+    public interface IJSRuntime {
+        Task<TValue> InvokeAsync<TValue>(string identifier, params object[] args);
+    }
+    public class JSException : Exception {
+        public JSException(string message);
+    }
+    public abstract class JSInProcessRuntimeBase : JSRuntimeBase, IJSInProcessRuntime, IJSRuntime {
+        protected JSInProcessRuntimeBase();
+        public TValue Invoke<TValue>(string identifier, params object[] args);
+        protected abstract string InvokeJS(string identifier, string argsJson);
+    }
+    public class JSInvokableAttribute : Attribute {
+        public JSInvokableAttribute();
+        public JSInvokableAttribute(string identifier);
+        public string Identifier { get; }
+    }
+    public static class JSRuntime {
+        public static void SetCurrentJSRuntime(IJSRuntime instance);
+    }
+    public abstract class JSRuntimeBase : IJSRuntime {
+        protected JSRuntimeBase();
+        protected abstract void BeginInvokeJS(long asyncHandle, string identifier, string argsJson);
+        public Task<T> InvokeAsync<T>(string identifier, params object[] args);
+    }
+}
```

