# Microsoft.AspNetCore.Server.Kestrel.Https

``` diff
 namespace Microsoft.AspNetCore.Server.Kestrel.Https {
     public enum ClientCertificateMode {
         AllowCertificate = 1,
         NoCertificate = 0,
         RequireCertificate = 2,
     }
     public class HttpsConnectionAdapterOptions {
         public HttpsConnectionAdapterOptions();
         public bool CheckCertificateRevocation { get; set; }
         public ClientCertificateMode ClientCertificateMode { get; set; }
         public Func<X509Certificate2, X509Chain, SslPolicyErrors, bool> ClientCertificateValidation { get; set; }
         public TimeSpan HandshakeTimeout { get; set; }
+        public Action<ConnectionContext, SslServerAuthenticationOptions> OnAuthenticate { get; set; }
         public X509Certificate2 ServerCertificate { get; set; }
         public Func<ConnectionContext, string, X509Certificate2> ServerCertificateSelector { get; set; }
         public SslProtocols SslProtocols { get; set; }
+        public void AllowAnyClientCertificate();
     }
 }
```

