// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Http
{
    public partial class CookieOptions
    {
        public CookieOptions() { }
        public string Domain { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public System.DateTimeOffset? Expires { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public bool HttpOnly { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public bool IsEssential { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public System.TimeSpan? MaxAge { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public string Path { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public Microsoft.AspNetCore.Http.SameSiteMode SameSite { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public bool Secure { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
    }
    public partial interface IFormCollection : System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<string, Microsoft.Extensions.Primitives.StringValues>>, System.Collections.IEnumerable
    {
        int Count { get; }
        Microsoft.AspNetCore.Http.IFormFileCollection Files { get; }
        Microsoft.Extensions.Primitives.StringValues this[string key] { get; }
        System.Collections.Generic.ICollection<string> Keys { get; }
        bool ContainsKey(string key);
        bool TryGetValue(string key, out Microsoft.Extensions.Primitives.StringValues value);
    }
    public partial interface IFormFile
    {
        string ContentDisposition { get; }
        string ContentType { get; }
        string FileName { get; }
        Microsoft.AspNetCore.Http.IHeaderDictionary Headers { get; }
        long Length { get; }
        string Name { get; }
        void CopyTo(System.IO.Stream target);
        System.Threading.Tasks.Task CopyToAsync(System.IO.Stream target, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken));
        System.IO.Stream OpenReadStream();
    }
    public partial interface IFormFileCollection : System.Collections.Generic.IEnumerable<Microsoft.AspNetCore.Http.IFormFile>, System.Collections.Generic.IReadOnlyCollection<Microsoft.AspNetCore.Http.IFormFile>, System.Collections.Generic.IReadOnlyList<Microsoft.AspNetCore.Http.IFormFile>, System.Collections.IEnumerable
    {
        Microsoft.AspNetCore.Http.IFormFile this[string name] { get; }
        Microsoft.AspNetCore.Http.IFormFile GetFile(string name);
        System.Collections.Generic.IReadOnlyList<Microsoft.AspNetCore.Http.IFormFile> GetFiles(string name);
    }
    public partial interface IHeaderDictionary : System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<string, Microsoft.Extensions.Primitives.StringValues>>, System.Collections.Generic.IDictionary<string, Microsoft.Extensions.Primitives.StringValues>, System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<string, Microsoft.Extensions.Primitives.StringValues>>, System.Collections.IEnumerable
    {
        long? ContentLength { get; set; }
        new Microsoft.Extensions.Primitives.StringValues this[string key] { get; set; }
    }
    public partial interface IQueryCollection : System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<string, Microsoft.Extensions.Primitives.StringValues>>, System.Collections.IEnumerable
    {
        int Count { get; }
        Microsoft.Extensions.Primitives.StringValues this[string key] { get; }
        System.Collections.Generic.ICollection<string> Keys { get; }
        bool ContainsKey(string key);
        bool TryGetValue(string key, out Microsoft.Extensions.Primitives.StringValues value);
    }
    public partial interface IRequestCookieCollection : System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<string, string>>, System.Collections.IEnumerable
    {
        int Count { get; }
        string this[string key] { get; }
        System.Collections.Generic.ICollection<string> Keys { get; }
        bool ContainsKey(string key);
        bool TryGetValue(string key, out string value);
    }
    public partial interface IResponseCookies
    {
        void Append(string key, string value);
        void Append(string key, string value, Microsoft.AspNetCore.Http.CookieOptions options);
        void Delete(string key);
        void Delete(string key, Microsoft.AspNetCore.Http.CookieOptions options);
    }
    public partial interface ISession
    {
        string Id { get; }
        bool IsAvailable { get; }
        System.Collections.Generic.IEnumerable<string> Keys { get; }
        void Clear();
        System.Threading.Tasks.Task CommitAsync(System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken));
        System.Threading.Tasks.Task LoadAsync(System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken));
        void Remove(string key);
        void Set(string key, byte[] value);
        bool TryGetValue(string key, out byte[] value);
    }
    public enum SameSiteMode
    {
        Unspecified = -1,
        None = 0,
        Lax = 1,
        Strict = 2,
    }
    public partial class WebSocketAcceptContext
    {
        public WebSocketAcceptContext() { }
        public virtual string SubProtocol { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
    }
}
namespace Microsoft.AspNetCore.Http.Features
{
    public partial class FeatureCollection : Microsoft.AspNetCore.Http.Features.IFeatureCollection, System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<System.Type, object>>, System.Collections.IEnumerable
    {
        public FeatureCollection() { }
        public FeatureCollection(Microsoft.AspNetCore.Http.Features.IFeatureCollection defaults) { }
        public bool IsReadOnly { get { throw null; } }
        public object this[System.Type key] { get { throw null; } set { } }
        public virtual int Revision { get { throw null; } }
        public System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<System.Type, object>> GetEnumerator() { throw null; }
        public TFeature Get<TFeature>() { throw null; }
        public void Set<TFeature>(TFeature instance) { }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw null; }
    }
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public partial struct FeatureReferences<TCache>
    {
        private object _dummy;
        private int _dummyPrimitive;
        public TCache Cache;
        public FeatureReferences(Microsoft.AspNetCore.Http.Features.IFeatureCollection collection) { throw null; }
        public Microsoft.AspNetCore.Http.Features.IFeatureCollection Collection { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public int Revision { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public TFeature Fetch<TFeature>(ref TFeature cached, System.Func<Microsoft.AspNetCore.Http.Features.IFeatureCollection, TFeature> factory) where TFeature : class { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]public TFeature Fetch<TFeature, TState>(ref TFeature cached, TState state, System.Func<TState, TFeature> factory) where TFeature : class { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]public void Initalize(Microsoft.AspNetCore.Http.Features.IFeatureCollection collection) { }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]public void Initalize(Microsoft.AspNetCore.Http.Features.IFeatureCollection collection, int revision) { }
    }
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public partial struct FeatureReference<T>
    {
        private T _feature;
        private int _dummyPrimitive;
        public static readonly Microsoft.AspNetCore.Http.Features.FeatureReference<T> Default;
        public T Fetch(Microsoft.AspNetCore.Http.Features.IFeatureCollection features) { throw null; }
        public T Update(Microsoft.AspNetCore.Http.Features.IFeatureCollection features, T feature) { throw null; }
    }
    public enum HttpsCompressionMode
    {
        Default = 0,
        DoNotCompress = 1,
        Compress = 2,
    }
    public partial interface IFeatureCollection : System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<System.Type, object>>, System.Collections.IEnumerable
    {
        bool IsReadOnly { get; }
        object this[System.Type key] { get; set; }
        int Revision { get; }
        TFeature Get<TFeature>();
        void Set<TFeature>(TFeature instance);
    }
    public partial interface IFormFeature
    {
        Microsoft.AspNetCore.Http.IFormCollection Form { get; set; }
        bool HasFormContentType { get; }
        Microsoft.AspNetCore.Http.IFormCollection ReadForm();
        System.Threading.Tasks.Task<Microsoft.AspNetCore.Http.IFormCollection> ReadFormAsync(System.Threading.CancellationToken cancellationToken);
    }
    public partial interface IHttpBodyControlFeature
    {
        bool AllowSynchronousIO { get; set; }
    }
    [System.ObsoleteAttribute("See IHttpRequestBodyFeature or IHttpResponseBodyFeature DisableBuffering", true)]
    public partial interface IHttpBufferingFeature
    {
        void DisableRequestBuffering();
        void DisableResponseBuffering();
    }
    public partial interface IHttpConnectionFeature
    {
        string ConnectionId { get; set; }
        System.Net.IPAddress LocalIpAddress { get; set; }
        int LocalPort { get; set; }
        System.Net.IPAddress RemoteIpAddress { get; set; }
        int RemotePort { get; set; }
    }
    public partial interface IHttpMaxRequestBodySizeFeature
    {
        bool IsReadOnly { get; }
        long? MaxRequestBodySize { get; set; }
    }
    public partial interface IHttpRequestFeature
    {
        System.IO.Stream Body { get; set; }
        Microsoft.AspNetCore.Http.IHeaderDictionary Headers { get; set; }
        string Method { get; set; }
        string Path { get; set; }
        string PathBase { get; set; }
        string Protocol { get; set; }
        string QueryString { get; set; }
        string RawTarget { get; set; }
        string Scheme { get; set; }
    }
    public partial interface IHttpRequestIdentifierFeature
    {
        string TraceIdentifier { get; set; }
    }
    public partial interface IHttpRequestLifetimeFeature
    {
        System.Threading.CancellationToken RequestAborted { get; set; }
        void Abort();
    }
    public partial interface IHttpRequestTrailersFeature
    {
        bool Available { get; }
        Microsoft.AspNetCore.Http.IHeaderDictionary Trailers { get; }
    }
    public partial interface IHttpResetFeature
    {
        void Reset(int errorCode);
    }
    public partial interface IHttpResponseBodyFeature
    {
        System.IO.Stream Stream { get; }
        System.IO.Pipelines.PipeWriter Writer { get; }
        System.Threading.Tasks.Task CompleteAsync();
        void DisableBuffering();
        System.Threading.Tasks.Task SendFileAsync(string path, long offset, long? count, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken));
        System.Threading.Tasks.Task StartAsync(System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken));
    }
    public partial interface IHttpResponseFeature
    {
        [System.ObsoleteAttribute("Use IHttpResponseBodyFeature.Stream instead.", false)]
        System.IO.Stream Body { get; set; }
        bool HasStarted { get; }
        Microsoft.AspNetCore.Http.IHeaderDictionary Headers { get; set; }
        string ReasonPhrase { get; set; }
        int StatusCode { get; set; }
        void OnCompleted(System.Func<object, System.Threading.Tasks.Task> callback, object state);
        void OnStarting(System.Func<object, System.Threading.Tasks.Task> callback, object state);
    }
    public partial interface IHttpResponseTrailersFeature
    {
        Microsoft.AspNetCore.Http.IHeaderDictionary Trailers { get; set; }
    }
    public partial interface IHttpsCompressionFeature
    {
        Microsoft.AspNetCore.Http.Features.HttpsCompressionMode Mode { get; set; }
    }
    [System.ObsoleteAttribute("Use IHttpResponseBodyFeature instead.", true)]
    public partial interface IHttpSendFileFeature
    {
        System.Threading.Tasks.Task SendFileAsync(string path, long offset, long? count, System.Threading.CancellationToken cancellation);
    }
    public partial interface IHttpUpgradeFeature
    {
        bool IsUpgradableRequest { get; }
        System.Threading.Tasks.Task<System.IO.Stream> UpgradeAsync();
    }
    public partial interface IHttpWebSocketFeature
    {
        bool IsWebSocketRequest { get; }
        System.Threading.Tasks.Task<System.Net.WebSockets.WebSocket> AcceptAsync(Microsoft.AspNetCore.Http.WebSocketAcceptContext context);
    }
    public partial interface IItemsFeature
    {
        System.Collections.Generic.IDictionary<object, object> Items { get; set; }
    }
    public partial interface IQueryFeature
    {
        Microsoft.AspNetCore.Http.IQueryCollection Query { get; set; }
    }
    public partial interface IRequestBodyPipeFeature
    {
        System.IO.Pipelines.PipeReader Reader { get; }
    }
    public partial interface IRequestCookiesFeature
    {
        Microsoft.AspNetCore.Http.IRequestCookieCollection Cookies { get; set; }
    }
    public partial interface IResponseCookiesFeature
    {
        Microsoft.AspNetCore.Http.IResponseCookies Cookies { get; }
    }
    public partial interface IServerVariablesFeature
    {
        string this[string variableName] { get; set; }
    }
    public partial interface IServiceProvidersFeature
    {
        System.IServiceProvider RequestServices { get; set; }
    }
    public partial interface ISessionFeature
    {
        Microsoft.AspNetCore.Http.ISession Session { get; set; }
    }
    public partial interface ITlsConnectionFeature
    {
        System.Security.Cryptography.X509Certificates.X509Certificate2 ClientCertificate { get; set; }
        System.Threading.Tasks.Task<System.Security.Cryptography.X509Certificates.X509Certificate2> GetClientCertificateAsync(System.Threading.CancellationToken cancellationToken);
    }
    public partial interface ITlsTokenBindingFeature
    {
        byte[] GetProvidedTokenBindingId();
        byte[] GetReferredTokenBindingId();
    }
    public partial interface ITrackingConsentFeature
    {
        bool CanTrack { get; }
        bool HasConsent { get; }
        bool IsConsentNeeded { get; }
        string CreateConsentCookie();
        void GrantConsent();
        void WithdrawConsent();
    }
}
namespace Microsoft.AspNetCore.Http.Features.Authentication
{
    public partial interface IHttpAuthenticationFeature
    {
        System.Security.Claims.ClaimsPrincipal User { get; set; }
    }
}
