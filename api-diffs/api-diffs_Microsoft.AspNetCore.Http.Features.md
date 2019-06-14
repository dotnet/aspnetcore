# Microsoft.AspNetCore.Http.Features

``` diff
 namespace Microsoft.AspNetCore.Http.Features {
     public struct FeatureReferences<TCache> {
+        public void Initalize(IFeatureCollection collection);
+        public void Initalize(IFeatureCollection collection, int revision);
     }
+    public enum HttpsCompressionMode {
+        Compress = 2,
+        Default = 0,
+        DoNotCompress = 1,
+    }
+    public interface IHttpRequestTrailersFeature {
+        bool Available { get; }
+        IHeaderDictionary Trailers { get; }
+    }
+    public interface IHttpResponseStartFeature {
+        Task StartAsync(CancellationToken token = default(CancellationToken));
+    }
+    public interface IHttpsCompressionFeature {
+        HttpsCompressionMode Mode { get; set; }
+    }
+    public interface IRequestBodyPipeFeature {
+        PipeReader Reader { get; }
+    }
+    public interface IResponseBodyPipeFeature {
+        PipeWriter Writer { get; }
+    }
+    public class RequestBodyPipeFeature : IRequestBodyPipeFeature {
+        public RequestBodyPipeFeature(HttpContext context);
+        public PipeReader Reader { get; }
+    }
+    public class RequestServicesFeature : IAsyncDisposable, IDisposable, IServiceProvidersFeature {
+        public RequestServicesFeature(HttpContext context, IServiceScopeFactory scopeFactory);
+        public IServiceProvider RequestServices { get; set; }
+        public void Dispose();
+        public ValueTask DisposeAsync();
+    }
+    public class ResponseBodyPipeFeature : IResponseBodyPipeFeature {
+        public ResponseBodyPipeFeature(HttpContext context);
+        public PipeWriter Writer { get; }
+    }
+    public class RouteValuesFeature : IRouteValuesFeature {
+        public RouteValuesFeature();
+        public RouteValueDictionary RouteValues { get; set; }
+    }
 }
```

