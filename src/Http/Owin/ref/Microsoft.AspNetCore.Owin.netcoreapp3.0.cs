// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Builder
{
    public static partial class OwinExtensions
    {
        public static Microsoft.AspNetCore.Builder.IApplicationBuilder UseBuilder(this System.Action<System.Func<System.Func<System.Collections.Generic.IDictionary<string, object>, System.Threading.Tasks.Task>, System.Func<System.Collections.Generic.IDictionary<string, object>, System.Threading.Tasks.Task>>> app) { throw null; }
        public static System.Action<System.Func<System.Func<System.Collections.Generic.IDictionary<string, object>, System.Threading.Tasks.Task>, System.Func<System.Collections.Generic.IDictionary<string, object>, System.Threading.Tasks.Task>>> UseBuilder(this System.Action<System.Func<System.Func<System.Collections.Generic.IDictionary<string, object>, System.Threading.Tasks.Task>, System.Func<System.Collections.Generic.IDictionary<string, object>, System.Threading.Tasks.Task>>> app, System.Action<Microsoft.AspNetCore.Builder.IApplicationBuilder> pipeline) { throw null; }
        public static System.Action<System.Func<System.Func<System.Collections.Generic.IDictionary<string, object>, System.Threading.Tasks.Task>, System.Func<System.Collections.Generic.IDictionary<string, object>, System.Threading.Tasks.Task>>> UseBuilder(this System.Action<System.Func<System.Func<System.Collections.Generic.IDictionary<string, object>, System.Threading.Tasks.Task>, System.Func<System.Collections.Generic.IDictionary<string, object>, System.Threading.Tasks.Task>>> app, System.Action<Microsoft.AspNetCore.Builder.IApplicationBuilder> pipeline, System.IServiceProvider serviceProvider) { throw null; }
        public static Microsoft.AspNetCore.Builder.IApplicationBuilder UseBuilder(this System.Action<System.Func<System.Func<System.Collections.Generic.IDictionary<string, object>, System.Threading.Tasks.Task>, System.Func<System.Collections.Generic.IDictionary<string, object>, System.Threading.Tasks.Task>>> app, System.IServiceProvider serviceProvider) { throw null; }
        public static System.Action<System.Func<System.Func<System.Collections.Generic.IDictionary<string, object>, System.Threading.Tasks.Task>, System.Func<System.Collections.Generic.IDictionary<string, object>, System.Threading.Tasks.Task>>> UseOwin(this Microsoft.AspNetCore.Builder.IApplicationBuilder builder) { throw null; }
        public static Microsoft.AspNetCore.Builder.IApplicationBuilder UseOwin(this Microsoft.AspNetCore.Builder.IApplicationBuilder builder, System.Action<System.Action<System.Func<System.Func<System.Collections.Generic.IDictionary<string, object>, System.Threading.Tasks.Task>, System.Func<System.Collections.Generic.IDictionary<string, object>, System.Threading.Tasks.Task>>>> pipeline) { throw null; }
    }
}
namespace Microsoft.AspNetCore.Owin
{
    public partial interface IOwinEnvironmentFeature
    {
        System.Collections.Generic.IDictionary<string, object> Environment { get; set; }
    }
    public partial class OwinEnvironment : System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<string, object>>, System.Collections.Generic.IDictionary<string, object>, System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<string, object>>, System.Collections.IEnumerable
    {
        public OwinEnvironment(Microsoft.AspNetCore.Http.HttpContext context) { }
        public System.Collections.Generic.IDictionary<string, Microsoft.AspNetCore.Owin.OwinEnvironment.FeatureMap> FeatureMaps { get { throw null; } }
        int System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<System.String,System.Object>>.Count { get { throw null; } }
        bool System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<System.String,System.Object>>.IsReadOnly { get { throw null; } }
        object System.Collections.Generic.IDictionary<System.String,System.Object>.this[string key] { get { throw null; } set { } }
        System.Collections.Generic.ICollection<string> System.Collections.Generic.IDictionary<System.String,System.Object>.Keys { get { throw null; } }
        System.Collections.Generic.ICollection<object> System.Collections.Generic.IDictionary<System.String,System.Object>.Values { get { throw null; } }
        public System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<string, object>> GetEnumerator() { throw null; }
        void System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<System.String,System.Object>>.Add(System.Collections.Generic.KeyValuePair<string, object> item) { }
        void System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<System.String,System.Object>>.Clear() { }
        bool System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<System.String,System.Object>>.Contains(System.Collections.Generic.KeyValuePair<string, object> item) { throw null; }
        void System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<System.String,System.Object>>.CopyTo(System.Collections.Generic.KeyValuePair<string, object>[] array, int arrayIndex) { }
        bool System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<System.String,System.Object>>.Remove(System.Collections.Generic.KeyValuePair<string, object> item) { throw null; }
        void System.Collections.Generic.IDictionary<System.String,System.Object>.Add(string key, object value) { }
        bool System.Collections.Generic.IDictionary<System.String,System.Object>.ContainsKey(string key) { throw null; }
        bool System.Collections.Generic.IDictionary<System.String,System.Object>.Remove(string key) { throw null; }
        bool System.Collections.Generic.IDictionary<System.String,System.Object>.TryGetValue(string key, out object value) { throw null; }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw null; }
        public partial class FeatureMap
        {
            public FeatureMap(System.Type featureInterface, System.Func<object, object> getter) { }
            public FeatureMap(System.Type featureInterface, System.Func<object, object> getter, System.Action<object, object> setter) { }
            public FeatureMap(System.Type featureInterface, System.Func<object, object> getter, System.Func<object> defaultFactory) { }
            public FeatureMap(System.Type featureInterface, System.Func<object, object> getter, System.Func<object> defaultFactory, System.Action<object, object> setter) { }
            public FeatureMap(System.Type featureInterface, System.Func<object, object> getter, System.Func<object> defaultFactory, System.Action<object, object> setter, System.Func<object> featureFactory) { }
            public bool CanSet { get { throw null; } }
        }
        public partial class FeatureMap<TFeature> : Microsoft.AspNetCore.Owin.OwinEnvironment.FeatureMap
        {
            public FeatureMap(System.Func<TFeature, object> getter) : base (default(System.Type), default(System.Func<object, object>)) { }
            public FeatureMap(System.Func<TFeature, object> getter, System.Action<TFeature, object> setter) : base (default(System.Type), default(System.Func<object, object>)) { }
            public FeatureMap(System.Func<TFeature, object> getter, System.Func<object> defaultFactory) : base (default(System.Type), default(System.Func<object, object>)) { }
            public FeatureMap(System.Func<TFeature, object> getter, System.Func<object> defaultFactory, System.Action<TFeature, object> setter) : base (default(System.Type), default(System.Func<object, object>)) { }
            public FeatureMap(System.Func<TFeature, object> getter, System.Func<object> defaultFactory, System.Action<TFeature, object> setter, System.Func<TFeature> featureFactory) : base (default(System.Type), default(System.Func<object, object>)) { }
        }
    }
    public partial class OwinEnvironmentFeature : Microsoft.AspNetCore.Owin.IOwinEnvironmentFeature
    {
        public OwinEnvironmentFeature() { }
        public System.Collections.Generic.IDictionary<string, object> Environment { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
    }
    public partial class OwinFeatureCollection : Microsoft.AspNetCore.Http.Features.Authentication.IHttpAuthenticationFeature, Microsoft.AspNetCore.Http.Features.IFeatureCollection, Microsoft.AspNetCore.Http.Features.IHttpConnectionFeature, Microsoft.AspNetCore.Http.Features.IHttpRequestFeature, Microsoft.AspNetCore.Http.Features.IHttpRequestIdentifierFeature, Microsoft.AspNetCore.Http.Features.IHttpRequestLifetimeFeature, Microsoft.AspNetCore.Http.Features.IHttpResponseBodyFeature, Microsoft.AspNetCore.Http.Features.IHttpResponseFeature, Microsoft.AspNetCore.Http.Features.IHttpWebSocketFeature, Microsoft.AspNetCore.Http.Features.ITlsConnectionFeature, Microsoft.AspNetCore.Owin.IOwinEnvironmentFeature, System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<System.Type, object>>, System.Collections.IEnumerable
    {
        public OwinFeatureCollection(System.Collections.Generic.IDictionary<string, object> environment) { }
        public System.Collections.Generic.IDictionary<string, object> Environment { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public bool IsReadOnly { get { throw null; } }
        public object this[System.Type key] { get { throw null; } set { } }
        System.Security.Claims.ClaimsPrincipal Microsoft.AspNetCore.Http.Features.Authentication.IHttpAuthenticationFeature.User { get { throw null; } set { } }
        string Microsoft.AspNetCore.Http.Features.IHttpConnectionFeature.ConnectionId { get { throw null; } set { } }
        System.Net.IPAddress Microsoft.AspNetCore.Http.Features.IHttpConnectionFeature.LocalIpAddress { get { throw null; } set { } }
        int Microsoft.AspNetCore.Http.Features.IHttpConnectionFeature.LocalPort { get { throw null; } set { } }
        System.Net.IPAddress Microsoft.AspNetCore.Http.Features.IHttpConnectionFeature.RemoteIpAddress { get { throw null; } set { } }
        int Microsoft.AspNetCore.Http.Features.IHttpConnectionFeature.RemotePort { get { throw null; } set { } }
        System.IO.Stream Microsoft.AspNetCore.Http.Features.IHttpRequestFeature.Body { get { throw null; } set { } }
        Microsoft.AspNetCore.Http.IHeaderDictionary Microsoft.AspNetCore.Http.Features.IHttpRequestFeature.Headers { get { throw null; } set { } }
        string Microsoft.AspNetCore.Http.Features.IHttpRequestFeature.Method { get { throw null; } set { } }
        string Microsoft.AspNetCore.Http.Features.IHttpRequestFeature.Path { get { throw null; } set { } }
        string Microsoft.AspNetCore.Http.Features.IHttpRequestFeature.PathBase { get { throw null; } set { } }
        string Microsoft.AspNetCore.Http.Features.IHttpRequestFeature.Protocol { get { throw null; } set { } }
        string Microsoft.AspNetCore.Http.Features.IHttpRequestFeature.QueryString { get { throw null; } set { } }
        string Microsoft.AspNetCore.Http.Features.IHttpRequestFeature.RawTarget { get { throw null; } set { } }
        string Microsoft.AspNetCore.Http.Features.IHttpRequestFeature.Scheme { get { throw null; } set { } }
        string Microsoft.AspNetCore.Http.Features.IHttpRequestIdentifierFeature.TraceIdentifier { get { throw null; } set { } }
        System.Threading.CancellationToken Microsoft.AspNetCore.Http.Features.IHttpRequestLifetimeFeature.RequestAborted { get { throw null; } set { } }
        System.IO.Stream Microsoft.AspNetCore.Http.Features.IHttpResponseBodyFeature.Stream { get { throw null; } }
        System.IO.Pipelines.PipeWriter Microsoft.AspNetCore.Http.Features.IHttpResponseBodyFeature.Writer { get { throw null; } }
        System.IO.Stream Microsoft.AspNetCore.Http.Features.IHttpResponseFeature.Body { get { throw null; } set { } }
        bool Microsoft.AspNetCore.Http.Features.IHttpResponseFeature.HasStarted { get { throw null; } }
        Microsoft.AspNetCore.Http.IHeaderDictionary Microsoft.AspNetCore.Http.Features.IHttpResponseFeature.Headers { get { throw null; } set { } }
        string Microsoft.AspNetCore.Http.Features.IHttpResponseFeature.ReasonPhrase { get { throw null; } set { } }
        int Microsoft.AspNetCore.Http.Features.IHttpResponseFeature.StatusCode { get { throw null; } set { } }
        bool Microsoft.AspNetCore.Http.Features.IHttpWebSocketFeature.IsWebSocketRequest { get { throw null; } }
        System.Security.Cryptography.X509Certificates.X509Certificate2 Microsoft.AspNetCore.Http.Features.ITlsConnectionFeature.ClientCertificate { get { throw null; } set { } }
        public int Revision { get { throw null; } }
        public bool SupportsWebSockets { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public void Dispose() { }
        public object Get(System.Type key) { throw null; }
        public System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<System.Type, object>> GetEnumerator() { throw null; }
        public TFeature Get<TFeature>() { throw null; }
        void Microsoft.AspNetCore.Http.Features.IHttpRequestLifetimeFeature.Abort() { }
        System.Threading.Tasks.Task Microsoft.AspNetCore.Http.Features.IHttpResponseBodyFeature.CompleteAsync() { throw null; }
        void Microsoft.AspNetCore.Http.Features.IHttpResponseBodyFeature.DisableBuffering() { }
        System.Threading.Tasks.Task Microsoft.AspNetCore.Http.Features.IHttpResponseBodyFeature.SendFileAsync(string path, long offset, long? length, System.Threading.CancellationToken cancellation) { throw null; }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        System.Threading.Tasks.Task Microsoft.AspNetCore.Http.Features.IHttpResponseBodyFeature.StartAsync(System.Threading.CancellationToken cancellationToken) { throw null; }
        void Microsoft.AspNetCore.Http.Features.IHttpResponseFeature.OnCompleted(System.Func<object, System.Threading.Tasks.Task> callback, object state) { }
        void Microsoft.AspNetCore.Http.Features.IHttpResponseFeature.OnStarting(System.Func<object, System.Threading.Tasks.Task> callback, object state) { }
        System.Threading.Tasks.Task<System.Net.WebSockets.WebSocket> Microsoft.AspNetCore.Http.Features.IHttpWebSocketFeature.AcceptAsync(Microsoft.AspNetCore.Http.WebSocketAcceptContext context) { throw null; }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        System.Threading.Tasks.Task<System.Security.Cryptography.X509Certificates.X509Certificate2> Microsoft.AspNetCore.Http.Features.ITlsConnectionFeature.GetClientCertificateAsync(System.Threading.CancellationToken cancellationToken) { throw null; }
        public void Set(System.Type key, object value) { }
        public void Set<TFeature>(TFeature instance) { }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw null; }
    }
    public partial class OwinWebSocketAcceptAdapter
    {
        internal OwinWebSocketAcceptAdapter() { }
        public static System.Func<System.Collections.Generic.IDictionary<string, object>, System.Threading.Tasks.Task> AdaptWebSockets(System.Func<System.Collections.Generic.IDictionary<string, object>, System.Threading.Tasks.Task> next) { throw null; }
    }
    public partial class OwinWebSocketAcceptContext : Microsoft.AspNetCore.Http.WebSocketAcceptContext
    {
        public OwinWebSocketAcceptContext() { }
        public OwinWebSocketAcceptContext(System.Collections.Generic.IDictionary<string, object> options) { }
        public System.Collections.Generic.IDictionary<string, object> Options { get { throw null; } }
        public override string SubProtocol { get { throw null; } set { } }
    }
    public partial class OwinWebSocketAdapter : System.Net.WebSockets.WebSocket
    {
        public OwinWebSocketAdapter(System.Collections.Generic.IDictionary<string, object> websocketContext, string subProtocol) { }
        public override System.Net.WebSockets.WebSocketCloseStatus? CloseStatus { get { throw null; } }
        public override string CloseStatusDescription { get { throw null; } }
        public override System.Net.WebSockets.WebSocketState State { get { throw null; } }
        public override string SubProtocol { get { throw null; } }
        public override void Abort() { }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public override System.Threading.Tasks.Task CloseAsync(System.Net.WebSockets.WebSocketCloseStatus closeStatus, string statusDescription, System.Threading.CancellationToken cancellationToken) { throw null; }
        public override System.Threading.Tasks.Task CloseOutputAsync(System.Net.WebSockets.WebSocketCloseStatus closeStatus, string statusDescription, System.Threading.CancellationToken cancellationToken) { throw null; }
        public override void Dispose() { }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public override System.Threading.Tasks.Task<System.Net.WebSockets.WebSocketReceiveResult> ReceiveAsync(System.ArraySegment<byte> buffer, System.Threading.CancellationToken cancellationToken) { throw null; }
        public override System.Threading.Tasks.Task SendAsync(System.ArraySegment<byte> buffer, System.Net.WebSockets.WebSocketMessageType messageType, bool endOfMessage, System.Threading.CancellationToken cancellationToken) { throw null; }
    }
    public partial class WebSocketAcceptAdapter
    {
        public WebSocketAcceptAdapter(System.Collections.Generic.IDictionary<string, object> env, System.Func<Microsoft.AspNetCore.Http.WebSocketAcceptContext, System.Threading.Tasks.Task<System.Net.WebSockets.WebSocket>> accept) { }
        public static System.Func<System.Collections.Generic.IDictionary<string, object>, System.Threading.Tasks.Task> AdaptWebSockets(System.Func<System.Collections.Generic.IDictionary<string, object>, System.Threading.Tasks.Task> next) { throw null; }
    }
    public partial class WebSocketAdapter
    {
        internal WebSocketAdapter() { }
    }
}
