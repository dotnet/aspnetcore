# Microsoft.AspNetCore.Server.Kestrel.Https.Internal

``` diff
 namespace Microsoft.AspNetCore.Server.Kestrel.Https.Internal {
     public static class CertificateLoader {
         public static X509Certificate2 LoadFromStoreCert(string subject, string storeName, StoreLocation storeLocation, bool allowInvalid);
     }
-    public class HttpsConnectionAdapter : IConnectionAdapter {
 {
-        public HttpsConnectionAdapter(HttpsConnectionAdapterOptions options);

-        public HttpsConnectionAdapter(HttpsConnectionAdapterOptions options, ILoggerFactory loggerFactory);

-        public bool IsHttps { get; }

-        public Task<IAdaptedConnection> OnConnectionAsync(ConnectionAdapterContext context);

-    }
 }
```

