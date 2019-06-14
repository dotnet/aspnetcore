# Microsoft.AspNetCore.Http

``` diff
 namespace Microsoft.AspNetCore.Http {
-    public class DefaultHttpContext : HttpContext {
+    public sealed class DefaultHttpContext : HttpContext {
-        public override AuthenticationManager Authentication { get; }

+        public FormOptions FormOptions { get; set; }
+        public HttpContext HttpContext { get; }
+        public IServiceScopeFactory ServiceScopeFactory { get; set; }
-        public virtual void Initialize(IFeatureCollection features);
+        public void Initialize(IFeatureCollection features);
-        protected virtual AuthenticationManager InitializeAuthenticationManager();

-        protected virtual ConnectionInfo InitializeConnectionInfo();

-        protected virtual HttpRequest InitializeHttpRequest();

-        protected virtual HttpResponse InitializeHttpResponse();

-        protected virtual WebSocketManager InitializeWebSocketManager();

-        public virtual void Uninitialize();
+        public void Uninitialize();
-        protected virtual void UninitializeAuthenticationManager(AuthenticationManager instance);

-        protected virtual void UninitializeConnectionInfo(ConnectionInfo instance);

-        protected virtual void UninitializeHttpRequest(HttpRequest instance);

-        protected virtual void UninitializeHttpResponse(HttpResponse instance);

-        protected virtual void UninitializeWebSocketManager(WebSocketManager instance);

     }
+    public class DefaultHttpContextFactory : IHttpContextFactory {
+        public DefaultHttpContextFactory(IServiceProvider serviceProvider);
+        public HttpContext Create(IFeatureCollection featureCollection);
+        public void Dispose(HttpContext httpContext);
+    }
+    public static class EndpointHttpContextExtensions {
+        public static Endpoint GetEndpoint(this HttpContext context);
+        public static void SetEndpoint(this HttpContext context, Endpoint endpoint);
+    }
     public sealed class EndpointMetadataCollection : IEnumerable, IEnumerable<object>, IReadOnlyCollection<object>, IReadOnlyList<object> {
-        public IEnumerable<T> GetOrderedMetadata<T>() where T : class;

+        public IReadOnlyList<T> GetOrderedMetadata<T>() where T : class;
     }
-    public struct FragmentString : IEquatable<FragmentString>
+    public readonly struct FragmentString : IEquatable<FragmentString>
-    public struct HostString : IEquatable<HostString>
+    public readonly struct HostString : IEquatable<HostString>
     public abstract class HttpContext {
-        public abstract AuthenticationManager Authentication { get; }

     }
     public class HttpContextFactory : IHttpContextFactory {
+        public HttpContextFactory(IOptions<FormOptions> formOptions, IServiceScopeFactory serviceScopeFactory);
+        public HttpContextFactory(IOptions<FormOptions> formOptions, IServiceScopeFactory serviceScopeFactory, IHttpContextAccessor httpContextAccessor);
     }
+    public static class HttpContextServerVariableExtensions {
+        public static string GetServerVariable(this HttpContext context, string variableName);
+    }
     public abstract class HttpRequest {
+        public virtual PipeReader BodyReader { get; }
+        public virtual RouteValueDictionary RouteValues { get; set; }
     }
     public abstract class HttpResponse {
+        public virtual PipeWriter BodyWriter { get; }
+        public virtual void RegisterForDisposeAsync(IAsyncDisposable disposable);
+        public virtual Task StartAsync(CancellationToken cancellationToken = default(CancellationToken));
     }
+    public interface IDefaultHttpContextContainer {
+        DefaultHttpContext HttpContext { get; }
+    }
-    public struct PathString : IEquatable<PathString>
+    public readonly struct PathString : IEquatable<PathString>
-    public struct QueryString : IEquatable<QueryString>
+    public readonly struct QueryString : IEquatable<QueryString>
+    public static class RequestTrailerExtensions {
+        public static bool CheckTrailersAvailable(this HttpRequest request);
+        public static StringValues GetDeclaredTrailers(this HttpRequest request);
+        public static StringValues GetTrailer(this HttpRequest request, string trailerName);
+        public static bool SupportsTrailers(this HttpRequest request);
+    }
     public static class ResponseExtensions {
+        public static void Redirect(this HttpResponse response, string location, bool permanent, bool preserveMethod);
     }
 }
```

