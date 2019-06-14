# Microsoft.AspNetCore.Http.Connections

``` diff
 namespace Microsoft.AspNetCore.Http.Connections {
+    public class ConnectionOptions {
+        public ConnectionOptions();
+        public Nullable<TimeSpan> DisconnectTimeout { get; set; }
+    }
+    public class ConnectionOptionsSetup : IConfigureOptions<ConnectionOptions> {
+        public static TimeSpan DefaultDisconectTimeout;
+        public ConnectionOptionsSetup();
+        public void Configure(ConnectionOptions options);
+    }
+    public class NegotiateMetadata {
+        public NegotiateMetadata();
+    }
     public static class NegotiateProtocol {
+        public static NegotiationResponse ParseResponse(ReadOnlySpan<byte> content);
     }
 }
```

