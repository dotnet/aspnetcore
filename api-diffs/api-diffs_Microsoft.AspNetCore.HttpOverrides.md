# Microsoft.AspNetCore.HttpOverrides

``` diff
 namespace Microsoft.AspNetCore.HttpOverrides {
+    public class CertificateForwardingMiddleware {
+        public CertificateForwardingMiddleware(RequestDelegate next, ILoggerFactory loggerFactory, IOptions<CertificateForwardingOptions> options);
+        public Task Invoke(HttpContext httpContext);
+    }
+    public class CertificateForwardingOptions {
+        public Func<string, X509Certificate2> HeaderConverter;
+        public CertificateForwardingOptions();
+        public string CertificateHeader { get; set; }
+    }
 }
```

