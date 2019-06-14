# Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets.Internal

``` diff
 namespace Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets.Internal {
-    public class IOQueue : PipeScheduler {
+    public class IOQueue : PipeScheduler, IThreadPoolWorkItem {
+        void System.Threading.IThreadPoolWorkItem.Execute();
     }
 }
```

