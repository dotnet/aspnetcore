# Microsoft.AspNetCore.Mvc.Infrastructure

``` diff
 namespace Microsoft.AspNetCore.Mvc.Infrastructure {
+    public sealed class ActionResultObjectValueAttribute : Attribute {
+        public ActionResultObjectValueAttribute();
+    }
+    public sealed class ActionResultStatusCodeAttribute : Attribute {
+        public ActionResultStatusCodeAttribute();
+    }
-    public class CompatibilitySwitch<TValue> : ICompatibilitySwitch where TValue : struct, ValueType
+    public class CompatibilitySwitch<TValue> : ICompatibilitySwitch where TValue : struct
+    public interface IApiBehaviorMetadata : IFilterMetadata
     public class VirtualFileResultExecutor : FileResultExecutorBase, IActionResultExecutor<VirtualFileResult> {
-        public VirtualFileResultExecutor(ILoggerFactory loggerFactory, IHostingEnvironment hostingEnvironment);

+        public VirtualFileResultExecutor(ILoggerFactory loggerFactory, IWebHostEnvironment hostingEnvironment);
     }
 }
```

