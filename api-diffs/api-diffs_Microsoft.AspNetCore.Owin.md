# Microsoft.AspNetCore.Owin

``` diff
-namespace Microsoft.AspNetCore.Owin {
 {
-    public interface IOwinEnvironmentFeature {
 {
-        IDictionary<string, object> Environment { get; set; }

-    }
-    public class OwinEnvironment : ICollection<KeyValuePair<string, object>>, IDictionary<string, object>, IEnumerable, IEnumerable<KeyValuePair<string, object>> {
 {
-        public OwinEnvironment(HttpContext context);

-        public IDictionary<string, OwinEnvironment.FeatureMap> FeatureMaps { get; }

-        int System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<System.String,System.Object>>.Count { get; }

-        bool System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<System.String,System.Object>>.IsReadOnly { get; }

-        object System.Collections.Generic.IDictionary<System.String,System.Object>.this[string key] { get; set; }

-        ICollection<string> System.Collections.Generic.IDictionary<System.String,System.Object>.Keys { get; }

-        ICollection<object> System.Collections.Generic.IDictionary<System.String,System.Object>.Values { get; }

-        public IEnumerator<KeyValuePair<string, object>> GetEnumerator();

-        void System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<System.String,System.Object>>.Add(KeyValuePair<string, object> item);

-        void System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<System.String,System.Object>>.Clear();

-        bool System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<System.String,System.Object>>.Contains(KeyValuePair<string, object> item);

-        void System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<System.String,System.Object>>.CopyTo(KeyValuePair<string, object>[] array, int arrayIndex);

-        bool System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<System.String,System.Object>>.Remove(KeyValuePair<string, object> item);

-        void System.Collections.Generic.IDictionary<System.String,System.Object>.Add(string key, object value);

-        bool System.Collections.Generic.IDictionary<System.String,System.Object>.ContainsKey(string key);

-        bool System.Collections.Generic.IDictionary<System.String,System.Object>.Remove(string key);

-        bool System.Collections.Generic.IDictionary<System.String,System.Object>.TryGetValue(string key, out object value);

-        IEnumerator System.Collections.IEnumerable.GetEnumerator();

-        public class FeatureMap {
 {
-            public FeatureMap(Type featureInterface, Func<object, object> getter);

-            public FeatureMap(Type featureInterface, Func<object, object> getter, Action<object, object> setter);

-            public FeatureMap(Type featureInterface, Func<object, object> getter, Func<object> defaultFactory);

-            public FeatureMap(Type featureInterface, Func<object, object> getter, Func<object> defaultFactory, Action<object, object> setter);

-            public FeatureMap(Type featureInterface, Func<object, object> getter, Func<object> defaultFactory, Action<object, object> setter, Func<object> featureFactory);

-            public bool CanSet { get; }

-        }
-        public class FeatureMap<TFeature> : OwinEnvironment.FeatureMap {
 {
-            public FeatureMap(Func<TFeature, object> getter);

-            public FeatureMap(Func<TFeature, object> getter, Action<TFeature, object> setter);

-            public FeatureMap(Func<TFeature, object> getter, Func<object> defaultFactory);

-            public FeatureMap(Func<TFeature, object> getter, Func<object> defaultFactory, Action<TFeature, object> setter);

-            public FeatureMap(Func<TFeature, object> getter, Func<object> defaultFactory, Action<TFeature, object> setter, Func<TFeature> featureFactory);

-        }
-    }
-    public class OwinEnvironmentFeature : IOwinEnvironmentFeature {
 {
-        public OwinEnvironmentFeature();

-        public IDictionary<string, object> Environment { get; set; }

-    }
-    public class OwinFeatureCollection : IEnumerable, IEnumerable<KeyValuePair<Type, object>>, IFeatureCollection, IHttpAuthenticationFeature, IHttpConnectionFeature, IHttpRequestFeature, IHttpRequestIdentifierFeature, IHttpRequestLifetimeFeature, IHttpResponseFeature, IHttpSendFileFeature, IHttpWebSocketFeature, IOwinEnvironmentFeature, ITlsConnectionFeature {
 {
-        public OwinFeatureCollection(IDictionary<string, object> environment);

-        public IDictionary<string, object> Environment { get; set; }

-        public bool IsReadOnly { get; }

-        IAuthenticationHandler Microsoft.AspNetCore.Http.Features.Authentication.IHttpAuthenticationFeature.Handler { get; set; }

-        ClaimsPrincipal Microsoft.AspNetCore.Http.Features.Authentication.IHttpAuthenticationFeature.User { get; set; }

-        string Microsoft.AspNetCore.Http.Features.IHttpConnectionFeature.ConnectionId { get; set; }

-        IPAddress Microsoft.AspNetCore.Http.Features.IHttpConnectionFeature.LocalIpAddress { get; set; }

-        int Microsoft.AspNetCore.Http.Features.IHttpConnectionFeature.LocalPort { get; set; }

-        IPAddress Microsoft.AspNetCore.Http.Features.IHttpConnectionFeature.RemoteIpAddress { get; set; }

-        int Microsoft.AspNetCore.Http.Features.IHttpConnectionFeature.RemotePort { get; set; }

-        Stream Microsoft.AspNetCore.Http.Features.IHttpRequestFeature.Body { get; set; }

-        IHeaderDictionary Microsoft.AspNetCore.Http.Features.IHttpRequestFeature.Headers { get; set; }

-        string Microsoft.AspNetCore.Http.Features.IHttpRequestFeature.Method { get; set; }

-        string Microsoft.AspNetCore.Http.Features.IHttpRequestFeature.Path { get; set; }

-        string Microsoft.AspNetCore.Http.Features.IHttpRequestFeature.PathBase { get; set; }

-        string Microsoft.AspNetCore.Http.Features.IHttpRequestFeature.Protocol { get; set; }

-        string Microsoft.AspNetCore.Http.Features.IHttpRequestFeature.QueryString { get; set; }

-        string Microsoft.AspNetCore.Http.Features.IHttpRequestFeature.RawTarget { get; set; }

-        string Microsoft.AspNetCore.Http.Features.IHttpRequestFeature.Scheme { get; set; }

-        string Microsoft.AspNetCore.Http.Features.IHttpRequestIdentifierFeature.TraceIdentifier { get; set; }

-        CancellationToken Microsoft.AspNetCore.Http.Features.IHttpRequestLifetimeFeature.RequestAborted { get; set; }

-        Stream Microsoft.AspNetCore.Http.Features.IHttpResponseFeature.Body { get; set; }

-        bool Microsoft.AspNetCore.Http.Features.IHttpResponseFeature.HasStarted { get; }

-        IHeaderDictionary Microsoft.AspNetCore.Http.Features.IHttpResponseFeature.Headers { get; set; }

-        string Microsoft.AspNetCore.Http.Features.IHttpResponseFeature.ReasonPhrase { get; set; }

-        int Microsoft.AspNetCore.Http.Features.IHttpResponseFeature.StatusCode { get; set; }

-        bool Microsoft.AspNetCore.Http.Features.IHttpWebSocketFeature.IsWebSocketRequest { get; }

-        X509Certificate2 Microsoft.AspNetCore.Http.Features.ITlsConnectionFeature.ClientCertificate { get; set; }

-        public int Revision { get; }

-        public bool SupportsWebSockets { get; set; }

-        public object this[Type key] { get; set; }

-        public void Dispose();

-        public object Get(Type key);

-        public TFeature Get<TFeature>();

-        public IEnumerator<KeyValuePair<Type, object>> GetEnumerator();

-        void Microsoft.AspNetCore.Http.Features.IHttpRequestLifetimeFeature.Abort();

-        void Microsoft.AspNetCore.Http.Features.IHttpResponseFeature.OnCompleted(Func<object, Task> callback, object state);

-        void Microsoft.AspNetCore.Http.Features.IHttpResponseFeature.OnStarting(Func<object, Task> callback, object state);

-        Task Microsoft.AspNetCore.Http.Features.IHttpSendFileFeature.SendFileAsync(string path, long offset, Nullable<long> length, CancellationToken cancellation);

-        Task<WebSocket> Microsoft.AspNetCore.Http.Features.IHttpWebSocketFeature.AcceptAsync(WebSocketAcceptContext context);

-        Task<X509Certificate2> Microsoft.AspNetCore.Http.Features.ITlsConnectionFeature.GetClientCertificateAsync(CancellationToken cancellationToken);

-        public void Set(Type key, object value);

-        public void Set<TFeature>(TFeature instance);

-        IEnumerator System.Collections.IEnumerable.GetEnumerator();

-    }
-    public class OwinWebSocketAcceptAdapter {
 {
-        public static Func<IDictionary<string, object>, Task> AdaptWebSockets(Func<IDictionary<string, object>, Task> next);

-    }
-    public class OwinWebSocketAcceptContext : WebSocketAcceptContext {
 {
-        public OwinWebSocketAcceptContext();

-        public OwinWebSocketAcceptContext(IDictionary<string, object> options);

-        public IDictionary<string, object> Options { get; }

-        public override string SubProtocol { get; set; }

-    }
-    public class OwinWebSocketAdapter : WebSocket {
 {
-        public OwinWebSocketAdapter(IDictionary<string, object> websocketContext, string subProtocol);

-        public override Nullable<WebSocketCloseStatus> CloseStatus { get; }

-        public override string CloseStatusDescription { get; }

-        public override WebSocketState State { get; }

-        public override string SubProtocol { get; }

-        public override void Abort();

-        public override Task CloseAsync(WebSocketCloseStatus closeStatus, string statusDescription, CancellationToken cancellationToken);

-        public override Task CloseOutputAsync(WebSocketCloseStatus closeStatus, string statusDescription, CancellationToken cancellationToken);

-        public override void Dispose();

-        public override Task<WebSocketReceiveResult> ReceiveAsync(ArraySegment<byte> buffer, CancellationToken cancellationToken);

-        public override Task SendAsync(ArraySegment<byte> buffer, WebSocketMessageType messageType, bool endOfMessage, CancellationToken cancellationToken);

-    }
-    public class WebSocketAcceptAdapter {
 {
-        public WebSocketAcceptAdapter(IDictionary<string, object> env, Func<WebSocketAcceptContext, Task<WebSocket>> accept);

-        public static Func<IDictionary<string, object>, Task> AdaptWebSockets(Func<IDictionary<string, object>, Task> next);

-    }
-    public class WebSocketAdapter

-}
```

