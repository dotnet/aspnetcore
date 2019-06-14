# Microsoft.AspNetCore.Http.Internal

``` diff
 namespace Microsoft.AspNetCore.Http.Internal {
     public static class BufferingHelper {
-        public static string TempDirectory { get; }

     }
-    public class DefaultConnectionInfo : ConnectionInfo {
+    public sealed class DefaultConnectionInfo : ConnectionInfo {
-        public virtual void Initialize(IFeatureCollection features);
+        public void Initialize(IFeatureCollection features);
+        public void Initialize(IFeatureCollection features, int revision);
-        public virtual void Uninitialize();
+        public void Uninitialize();
     }
-    public class DefaultHttpRequest : HttpRequest {
+    public sealed class DefaultHttpRequest : HttpRequest {
+        public DefaultHttpRequest(DefaultHttpContext context);
-        public DefaultHttpRequest(HttpContext context);

+        public override PipeReader BodyReader { get; }
+        public override RouteValueDictionary RouteValues { get; set; }
+        public void Initialize();
-        public virtual void Initialize(HttpContext context);

+        public void Initialize(int revision);
-        public virtual void Uninitialize();
+        public void Uninitialize();
     }
-    public class DefaultHttpResponse : HttpResponse {
+    public sealed class DefaultHttpResponse : HttpResponse {
+        public DefaultHttpResponse(DefaultHttpContext context);
-        public DefaultHttpResponse(HttpContext context);

+        public override PipeWriter BodyWriter { get; }
+        public void Initialize();
-        public virtual void Initialize(HttpContext context);

+        public void Initialize(int revision);
+        public override Task StartAsync(CancellationToken cancellationToken = default(CancellationToken));
-        public virtual void Uninitialize();
+        public void Uninitialize();
     }
-    public class DefaultWebSocketManager : WebSocketManager {
+    public sealed class DefaultWebSocketManager : WebSocketManager {
-        public virtual void Initialize(IFeatureCollection features);
+        public void Initialize(IFeatureCollection features);
+        public void Initialize(IFeatureCollection features, int revision);
-        public virtual void Uninitialize();
+        public void Uninitialize();
     }
-    public struct HeaderSegment : IEquatable<HeaderSegment>
+    public readonly struct HeaderSegment : IEquatable<HeaderSegment>
-    public struct HeaderSegmentCollection : IEnumerable, IEnumerable<HeaderSegment>, IEquatable<HeaderSegmentCollection>
+    public readonly struct HeaderSegmentCollection : IEnumerable, IEnumerable<HeaderSegment>, IEquatable<HeaderSegmentCollection>
 }
```

