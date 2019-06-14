# Microsoft.AspNetCore.Server.IIS

``` diff
 namespace Microsoft.AspNetCore.Server.IIS {
+    public sealed class BadHttpRequestException : IOException {
+        public int StatusCode { get; }
+    }
 }
```

