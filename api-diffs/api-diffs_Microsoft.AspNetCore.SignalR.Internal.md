# Microsoft.AspNetCore.SignalR.Internal

``` diff
 namespace Microsoft.AspNetCore.SignalR.Internal {
     public class DefaultHubDispatcher<THub> : HubDispatcher<THub> where THub : Hub {
-        public override Type GetReturnType(string invocationId);

     }
-    public abstract class HubDispatcher<THub> : IInvocationBinder where THub : Hub {
+    public abstract class HubDispatcher<THub> where THub : Hub {
-        public abstract IReadOnlyList<Type> GetParameterTypes(string methodName);
+        public abstract IReadOnlyList<Type> GetParameterTypes(string name);
-        public abstract Type GetReturnType(string invocationId);

     }
 }
```

