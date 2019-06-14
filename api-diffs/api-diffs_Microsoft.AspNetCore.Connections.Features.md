# Microsoft.AspNetCore.Connections.Features

``` diff
 namespace Microsoft.AspNetCore.Connections.Features {
+    public interface IConnectionCompleteFeature {
+        void OnCompleted(Func<object, Task> callback, object state);
+    }
+    public interface IConnectionEndPointFeature {
+        EndPoint LocalEndPoint { get; set; }
+        EndPoint RemoteEndPoint { get; set; }
+    }
 }
```

