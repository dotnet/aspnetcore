// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Http
{
    public static partial class HeaderDictionaryTypeExtensions
    {
        public static void AppendList<T>(this Microsoft.AspNetCore.Http.IHeaderDictionary Headers, string name, System.Collections.Generic.IList<T> values) { }
        public static Microsoft.AspNetCore.Http.Headers.RequestHeaders GetTypedHeaders(this Microsoft.AspNetCore.Http.HttpRequest request) { throw null; }
        public static Microsoft.AspNetCore.Http.Headers.ResponseHeaders GetTypedHeaders(this Microsoft.AspNetCore.Http.HttpResponse response) { throw null; }
    }
    public static partial class HttpContextServerVariableExtensions
    {
        public static string GetServerVariable(this Microsoft.AspNetCore.Http.HttpContext context, string variableName) { throw null; }
    }
    public static partial class ResponseExtensions
    {
        public static void Clear(this Microsoft.AspNetCore.Http.HttpResponse response) { }
        public static void Redirect(this Microsoft.AspNetCore.Http.HttpResponse response, string location, bool permanent, bool preserveMethod) { }
    }
    public static partial class SendFileResponseExtensions
    {
        public static System.Threading.Tasks.Task SendFileAsync(this Microsoft.AspNetCore.Http.HttpResponse response, Microsoft.Extensions.FileProviders.IFileInfo file, long offset, long? count, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public static System.Threading.Tasks.Task SendFileAsync(this Microsoft.AspNetCore.Http.HttpResponse response, Microsoft.Extensions.FileProviders.IFileInfo file, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public static System.Threading.Tasks.Task SendFileAsync(this Microsoft.AspNetCore.Http.HttpResponse response, string fileName, long offset, long? count, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public static System.Threading.Tasks.Task SendFileAsync(this Microsoft.AspNetCore.Http.HttpResponse response, string fileName, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
    }
    public static partial class SessionExtensions
    {
        public static byte[] Get(this Microsoft.AspNetCore.Http.ISession session, string key) { throw null; }
        public static int? GetInt32(this Microsoft.AspNetCore.Http.ISession session, string key) { throw null; }
        public static string GetString(this Microsoft.AspNetCore.Http.ISession session, string key) { throw null; }
        public static void SetInt32(this Microsoft.AspNetCore.Http.ISession session, string key, int value) { }
        public static void SetString(this Microsoft.AspNetCore.Http.ISession session, string key, string value) { }
    }
}
namespace Microsoft.AspNetCore.Http.Extensions
{
    public static partial class HttpRequestMultipartExtensions
    {
        public static string GetMultipartBoundary(this Microsoft.AspNetCore.Http.HttpRequest request) { throw null; }
    }
    public partial class QueryBuilder : System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<string, string>>, System.Collections.IEnumerable
    {
        public QueryBuilder() { }
        public QueryBuilder(System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<string, string>> parameters) { }
        public void Add(string key, System.Collections.Generic.IEnumerable<string> values) { }
        public void Add(string key, string value) { }
        public override bool Equals(object obj) { throw null; }
        public System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<string, string>> GetEnumerator() { throw null; }
        public override int GetHashCode() { throw null; }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw null; }
        public Microsoft.AspNetCore.Http.QueryString ToQueryString() { throw null; }
        public override string ToString() { throw null; }
    }
    public static partial class StreamCopyOperation
    {
        public static System.Threading.Tasks.Task CopyToAsync(System.IO.Stream source, System.IO.Stream destination, long? count, int bufferSize, System.Threading.CancellationToken cancel) { throw null; }
        public static System.Threading.Tasks.Task CopyToAsync(System.IO.Stream source, System.IO.Stream destination, long? count, System.Threading.CancellationToken cancel) { throw null; }
    }
    public static partial class UriHelper
    {
        public static string BuildAbsolute(string scheme, Microsoft.AspNetCore.Http.HostString host, Microsoft.AspNetCore.Http.PathString pathBase = default(Microsoft.AspNetCore.Http.PathString), Microsoft.AspNetCore.Http.PathString path = default(Microsoft.AspNetCore.Http.PathString), Microsoft.AspNetCore.Http.QueryString query = default(Microsoft.AspNetCore.Http.QueryString), Microsoft.AspNetCore.Http.FragmentString fragment = default(Microsoft.AspNetCore.Http.FragmentString)) { throw null; }
        public static string BuildRelative(Microsoft.AspNetCore.Http.PathString pathBase = default(Microsoft.AspNetCore.Http.PathString), Microsoft.AspNetCore.Http.PathString path = default(Microsoft.AspNetCore.Http.PathString), Microsoft.AspNetCore.Http.QueryString query = default(Microsoft.AspNetCore.Http.QueryString), Microsoft.AspNetCore.Http.FragmentString fragment = default(Microsoft.AspNetCore.Http.FragmentString)) { throw null; }
        public static string Encode(System.Uri uri) { throw null; }
        public static void FromAbsolute(string uri, out string scheme, out Microsoft.AspNetCore.Http.HostString host, out Microsoft.AspNetCore.Http.PathString path, out Microsoft.AspNetCore.Http.QueryString query, out Microsoft.AspNetCore.Http.FragmentString fragment) { throw null; }
        public static string GetDisplayUrl(this Microsoft.AspNetCore.Http.HttpRequest request) { throw null; }
        public static string GetEncodedPathAndQuery(this Microsoft.AspNetCore.Http.HttpRequest request) { throw null; }
        public static string GetEncodedUrl(this Microsoft.AspNetCore.Http.HttpRequest request) { throw null; }
    }
}
namespace Microsoft.AspNetCore.Http.Headers
{
    public partial class RequestHeaders
    {
        public RequestHeaders(Microsoft.AspNetCore.Http.IHeaderDictionary headers) { }
        public System.Collections.Generic.IList<Microsoft.Net.Http.Headers.MediaTypeHeaderValue> Accept { get { throw null; } set { } }
        public System.Collections.Generic.IList<Microsoft.Net.Http.Headers.StringWithQualityHeaderValue> AcceptCharset { get { throw null; } set { } }
        public System.Collections.Generic.IList<Microsoft.Net.Http.Headers.StringWithQualityHeaderValue> AcceptEncoding { get { throw null; } set { } }
        public System.Collections.Generic.IList<Microsoft.Net.Http.Headers.StringWithQualityHeaderValue> AcceptLanguage { get { throw null; } set { } }
        public Microsoft.Net.Http.Headers.CacheControlHeaderValue CacheControl { get { throw null; } set { } }
        public Microsoft.Net.Http.Headers.ContentDispositionHeaderValue ContentDisposition { get { throw null; } set { } }
        public long? ContentLength { get { throw null; } set { } }
        public Microsoft.Net.Http.Headers.ContentRangeHeaderValue ContentRange { get { throw null; } set { } }
        public Microsoft.Net.Http.Headers.MediaTypeHeaderValue ContentType { get { throw null; } set { } }
        public System.Collections.Generic.IList<Microsoft.Net.Http.Headers.CookieHeaderValue> Cookie { get { throw null; } set { } }
        public System.DateTimeOffset? Date { get { throw null; } set { } }
        public System.DateTimeOffset? Expires { get { throw null; } set { } }
        public Microsoft.AspNetCore.Http.IHeaderDictionary Headers { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public Microsoft.AspNetCore.Http.HostString Host { get { throw null; } set { } }
        public System.Collections.Generic.IList<Microsoft.Net.Http.Headers.EntityTagHeaderValue> IfMatch { get { throw null; } set { } }
        public System.DateTimeOffset? IfModifiedSince { get { throw null; } set { } }
        public System.Collections.Generic.IList<Microsoft.Net.Http.Headers.EntityTagHeaderValue> IfNoneMatch { get { throw null; } set { } }
        public Microsoft.Net.Http.Headers.RangeConditionHeaderValue IfRange { get { throw null; } set { } }
        public System.DateTimeOffset? IfUnmodifiedSince { get { throw null; } set { } }
        public System.DateTimeOffset? LastModified { get { throw null; } set { } }
        public Microsoft.Net.Http.Headers.RangeHeaderValue Range { get { throw null; } set { } }
        public System.Uri Referer { get { throw null; } set { } }
        public void Append(string name, object value) { }
        public void AppendList<T>(string name, System.Collections.Generic.IList<T> values) { }
        public System.Collections.Generic.IList<T> GetList<T>(string name) { throw null; }
        public T Get<T>(string name) { throw null; }
        public void Set(string name, object value) { }
        public void SetList<T>(string name, System.Collections.Generic.IList<T> values) { }
    }
    public partial class ResponseHeaders
    {
        public ResponseHeaders(Microsoft.AspNetCore.Http.IHeaderDictionary headers) { }
        public Microsoft.Net.Http.Headers.CacheControlHeaderValue CacheControl { get { throw null; } set { } }
        public Microsoft.Net.Http.Headers.ContentDispositionHeaderValue ContentDisposition { get { throw null; } set { } }
        public long? ContentLength { get { throw null; } set { } }
        public Microsoft.Net.Http.Headers.ContentRangeHeaderValue ContentRange { get { throw null; } set { } }
        public Microsoft.Net.Http.Headers.MediaTypeHeaderValue ContentType { get { throw null; } set { } }
        public System.DateTimeOffset? Date { get { throw null; } set { } }
        public Microsoft.Net.Http.Headers.EntityTagHeaderValue ETag { get { throw null; } set { } }
        public System.DateTimeOffset? Expires { get { throw null; } set { } }
        public Microsoft.AspNetCore.Http.IHeaderDictionary Headers { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public System.DateTimeOffset? LastModified { get { throw null; } set { } }
        public System.Uri Location { get { throw null; } set { } }
        public System.Collections.Generic.IList<Microsoft.Net.Http.Headers.SetCookieHeaderValue> SetCookie { get { throw null; } set { } }
        public void Append(string name, object value) { }
        public void AppendList<T>(string name, System.Collections.Generic.IList<T> values) { }
        public System.Collections.Generic.IList<T> GetList<T>(string name) { throw null; }
        public T Get<T>(string name) { throw null; }
        public void Set(string name, object value) { }
        public void SetList<T>(string name, System.Collections.Generic.IList<T> values) { }
    }
}
