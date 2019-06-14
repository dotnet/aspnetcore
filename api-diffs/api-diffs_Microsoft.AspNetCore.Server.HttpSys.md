# Microsoft.AspNetCore.Server.HttpSys

``` diff
 namespace Microsoft.AspNetCore.Server.HttpSys {
     public static class HttpSysDefaults {
-        public static readonly string AuthenticationScheme;
+        public const string AuthenticationScheme = "Windows";
     }
 }
```

