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
     public enum ForwardedHeaders {
         All = 7,
         None = 0,
         XForwardedFor = 1,
         XForwardedHost = 2,
         XForwardedProto = 4,
     }
     public static class ForwardedHeadersDefaults {
         public static string XForwardedForHeaderName { get; }
         public static string XForwardedHostHeaderName { get; }
         public static string XForwardedProtoHeaderName { get; }
         public static string XOriginalForHeaderName { get; }
         public static string XOriginalHostHeaderName { get; }
         public static string XOriginalProtoHeaderName { get; }
     }
     public class ForwardedHeadersMiddleware {
         public ForwardedHeadersMiddleware(RequestDelegate next, ILoggerFactory loggerFactory, IOptions<ForwardedHeadersOptions> options);
         public void ApplyForwarders(HttpContext context);
         public Task Invoke(HttpContext context);
     }
     public class HttpMethodOverrideMiddleware {
         public HttpMethodOverrideMiddleware(RequestDelegate next, IOptions<HttpMethodOverrideOptions> options);
         public Task Invoke(HttpContext context);
     }
     public class IPNetwork {
         public IPNetwork(IPAddress prefix, int prefixLength);
         public IPAddress Prefix { get; }
         public int PrefixLength { get; }
         public bool Contains(IPAddress address);
     }
 }
```

