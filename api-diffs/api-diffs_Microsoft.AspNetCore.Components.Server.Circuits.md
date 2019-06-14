# Microsoft.AspNetCore.Components.Server.Circuits

``` diff
+namespace Microsoft.AspNetCore.Components.Server.Circuits {
+    public sealed class Circuit {
+        public string Id { get; }
+    }
+    public abstract class CircuitHandler {
+        protected CircuitHandler();
+        public virtual int Order { get; }
+        public virtual Task OnCircuitClosedAsync(Circuit circuit, CancellationToken cancellationToken);
+        public virtual Task OnCircuitOpenedAsync(Circuit circuit, CancellationToken cancellationToken);
+        public virtual Task OnConnectionDownAsync(Circuit circuit, CancellationToken cancellationToken);
+        public virtual Task OnConnectionUpAsync(Circuit circuit, CancellationToken cancellationToken);
+    }
+    public class RemoteUriHelper : UriHelperBase {
+        public RemoteUriHelper(ILogger<RemoteUriHelper> logger);
+        public bool HasAttachedJSRuntime { get; }
+        public override void InitializeState(string uriAbsolute, string baseUriAbsolute);
+        protected override void NavigateToCore(string uri, bool forceLoad);
+        public static void NotifyLocationChanged(string uriAbsolute, bool isInterceptedLink);
+    }
+}
```

