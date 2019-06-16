# Microsoft.AspNetCore.Server.IIS

``` diff
 namespace Microsoft.AspNetCore.Server.IIS {
+    public sealed class BadHttpRequestException : IOException {
+        public int StatusCode { get; }
+    }
     public static class HttpContextExtensions {
         public static string GetIISServerVariable(this HttpContext context, string variableName);
     }
     public class IISServerDefaults {
         public const string AuthenticationScheme = "Windows";
         public IISServerDefaults();
     }
 }
```

