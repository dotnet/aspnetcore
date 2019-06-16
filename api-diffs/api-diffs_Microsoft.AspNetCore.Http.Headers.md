# Microsoft.AspNetCore.Http.Headers

``` diff
 namespace Microsoft.AspNetCore.Http.Headers {
     public class RequestHeaders {
         public RequestHeaders(IHeaderDictionary headers);
         public IList<MediaTypeHeaderValue> Accept { get; set; }
         public IList<StringWithQualityHeaderValue> AcceptCharset { get; set; }
         public IList<StringWithQualityHeaderValue> AcceptEncoding { get; set; }
         public IList<StringWithQualityHeaderValue> AcceptLanguage { get; set; }
         public CacheControlHeaderValue CacheControl { get; set; }
         public ContentDispositionHeaderValue ContentDisposition { get; set; }
         public Nullable<long> ContentLength { get; set; }
         public ContentRangeHeaderValue ContentRange { get; set; }
         public MediaTypeHeaderValue ContentType { get; set; }
         public IList<CookieHeaderValue> Cookie { get; set; }
         public Nullable<DateTimeOffset> Date { get; set; }
         public Nullable<DateTimeOffset> Expires { get; set; }
         public IHeaderDictionary Headers { get; }
         public HostString Host { get; set; }
         public IList<EntityTagHeaderValue> IfMatch { get; set; }
         public Nullable<DateTimeOffset> IfModifiedSince { get; set; }
         public IList<EntityTagHeaderValue> IfNoneMatch { get; set; }
         public RangeConditionHeaderValue IfRange { get; set; }
         public Nullable<DateTimeOffset> IfUnmodifiedSince { get; set; }
         public Nullable<DateTimeOffset> LastModified { get; set; }
         public RangeHeaderValue Range { get; set; }
         public Uri Referer { get; set; }
         public void Append(string name, object value);
         public void AppendList<T>(string name, IList<T> values);
         public T Get<T>(string name);
         public IList<T> GetList<T>(string name);
         public void Set(string name, object value);
         public void SetList<T>(string name, IList<T> values);
     }
     public class ResponseHeaders {
         public ResponseHeaders(IHeaderDictionary headers);
         public CacheControlHeaderValue CacheControl { get; set; }
         public ContentDispositionHeaderValue ContentDisposition { get; set; }
         public Nullable<long> ContentLength { get; set; }
         public ContentRangeHeaderValue ContentRange { get; set; }
         public MediaTypeHeaderValue ContentType { get; set; }
         public Nullable<DateTimeOffset> Date { get; set; }
         public EntityTagHeaderValue ETag { get; set; }
         public Nullable<DateTimeOffset> Expires { get; set; }
         public IHeaderDictionary Headers { get; }
         public Nullable<DateTimeOffset> LastModified { get; set; }
         public Uri Location { get; set; }
         public IList<SetCookieHeaderValue> SetCookie { get; set; }
         public void Append(string name, object value);
         public void AppendList<T>(string name, IList<T> values);
         public T Get<T>(string name);
         public IList<T> GetList<T>(string name);
         public void Set(string name, object value);
         public void SetList<T>(string name, IList<T> values);
     }
 }
```

