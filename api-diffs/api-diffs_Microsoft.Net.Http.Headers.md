# Microsoft.Net.Http.Headers

``` diff
 namespace Microsoft.Net.Http.Headers {
     public class CacheControlHeaderValue {
         public static readonly string MaxAgeString;
         public static readonly string MaxStaleString;
         public static readonly string MinFreshString;
         public static readonly string MustRevalidateString;
         public static readonly string NoCacheString;
         public static readonly string NoStoreString;
         public static readonly string NoTransformString;
         public static readonly string OnlyIfCachedString;
         public static readonly string PrivateString;
         public static readonly string ProxyRevalidateString;
         public static readonly string PublicString;
         public static readonly string SharedMaxAgeString;
         public CacheControlHeaderValue();
         public IList<NameValueHeaderValue> Extensions { get; }
         public Nullable<TimeSpan> MaxAge { get; set; }
         public bool MaxStale { get; set; }
         public Nullable<TimeSpan> MaxStaleLimit { get; set; }
         public Nullable<TimeSpan> MinFresh { get; set; }
         public bool MustRevalidate { get; set; }
         public bool NoCache { get; set; }
         public ICollection<StringSegment> NoCacheHeaders { get; }
         public bool NoStore { get; set; }
         public bool NoTransform { get; set; }
         public bool OnlyIfCached { get; set; }
         public bool Private { get; set; }
         public ICollection<StringSegment> PrivateHeaders { get; }
         public bool ProxyRevalidate { get; set; }
         public bool Public { get; set; }
         public Nullable<TimeSpan> SharedMaxAge { get; set; }
         public override bool Equals(object obj);
         public override int GetHashCode();
         public static CacheControlHeaderValue Parse(StringSegment input);
         public override string ToString();
         public static bool TryParse(StringSegment input, out CacheControlHeaderValue parsedValue);
     }
     public class ContentDispositionHeaderValue {
         public ContentDispositionHeaderValue(StringSegment dispositionType);
         public Nullable<DateTimeOffset> CreationDate { get; set; }
         public StringSegment DispositionType { get; set; }
         public StringSegment FileName { get; set; }
         public StringSegment FileNameStar { get; set; }
         public Nullable<DateTimeOffset> ModificationDate { get; set; }
         public StringSegment Name { get; set; }
         public IList<NameValueHeaderValue> Parameters { get; }
         public Nullable<DateTimeOffset> ReadDate { get; set; }
         public Nullable<long> Size { get; set; }
         public override bool Equals(object obj);
         public override int GetHashCode();
         public static ContentDispositionHeaderValue Parse(StringSegment input);
         public void SetHttpFileName(StringSegment fileName);
         public void SetMimeFileName(StringSegment fileName);
         public override string ToString();
         public static bool TryParse(StringSegment input, out ContentDispositionHeaderValue parsedValue);
     }
     public static class ContentDispositionHeaderValueIdentityExtensions {
         public static bool IsFileDisposition(this ContentDispositionHeaderValue header);
         public static bool IsFormDisposition(this ContentDispositionHeaderValue header);
     }
     public class ContentRangeHeaderValue {
         public ContentRangeHeaderValue(long length);
         public ContentRangeHeaderValue(long from, long to);
         public ContentRangeHeaderValue(long from, long to, long length);
         public Nullable<long> From { get; }
         public bool HasLength { get; }
         public bool HasRange { get; }
         public Nullable<long> Length { get; }
         public Nullable<long> To { get; }
         public StringSegment Unit { get; set; }
         public override bool Equals(object obj);
         public override int GetHashCode();
         public static ContentRangeHeaderValue Parse(StringSegment input);
         public override string ToString();
         public static bool TryParse(StringSegment input, out ContentRangeHeaderValue parsedValue);
     }
     public class CookieHeaderValue {
         public CookieHeaderValue(StringSegment name);
         public CookieHeaderValue(StringSegment name, StringSegment value);
         public StringSegment Name { get; set; }
         public StringSegment Value { get; set; }
         public override bool Equals(object obj);
         public override int GetHashCode();
         public static CookieHeaderValue Parse(StringSegment input);
         public static IList<CookieHeaderValue> ParseList(IList<string> inputs);
         public static IList<CookieHeaderValue> ParseStrictList(IList<string> inputs);
         public override string ToString();
         public static bool TryParse(StringSegment input, out CookieHeaderValue parsedValue);
         public static bool TryParseList(IList<string> inputs, out IList<CookieHeaderValue> parsedValues);
         public static bool TryParseStrictList(IList<string> inputs, out IList<CookieHeaderValue> parsedValues);
     }
     public class EntityTagHeaderValue {
         public EntityTagHeaderValue(StringSegment tag);
         public EntityTagHeaderValue(StringSegment tag, bool isWeak);
         public static EntityTagHeaderValue Any { get; }
         public bool IsWeak { get; }
         public StringSegment Tag { get; }
         public bool Compare(EntityTagHeaderValue other, bool useStrongComparison);
         public override bool Equals(object obj);
         public override int GetHashCode();
         public static EntityTagHeaderValue Parse(StringSegment input);
         public static IList<EntityTagHeaderValue> ParseList(IList<string> inputs);
         public static IList<EntityTagHeaderValue> ParseStrictList(IList<string> inputs);
         public override string ToString();
         public static bool TryParse(StringSegment input, out EntityTagHeaderValue parsedValue);
         public static bool TryParseList(IList<string> inputs, out IList<EntityTagHeaderValue> parsedValues);
         public static bool TryParseStrictList(IList<string> inputs, out IList<EntityTagHeaderValue> parsedValues);
     }
     public static class HeaderNames {
-        public const string Accept = "Accept";
+        public static readonly string Accept;
-        public const string AcceptCharset = "Accept-Charset";
+        public static readonly string AcceptCharset;
-        public const string AcceptEncoding = "Accept-Encoding";
+        public static readonly string AcceptEncoding;
-        public const string AcceptLanguage = "Accept-Language";
+        public static readonly string AcceptLanguage;
-        public const string AcceptRanges = "Accept-Ranges";
+        public static readonly string AcceptRanges;
-        public const string AccessControlAllowCredentials = "Access-Control-Allow-Credentials";
+        public static readonly string AccessControlAllowCredentials;
-        public const string AccessControlAllowHeaders = "Access-Control-Allow-Headers";
+        public static readonly string AccessControlAllowHeaders;
-        public const string AccessControlAllowMethods = "Access-Control-Allow-Methods";
+        public static readonly string AccessControlAllowMethods;
-        public const string AccessControlAllowOrigin = "Access-Control-Allow-Origin";
+        public static readonly string AccessControlAllowOrigin;
-        public const string AccessControlExposeHeaders = "Access-Control-Expose-Headers";
+        public static readonly string AccessControlExposeHeaders;
-        public const string AccessControlMaxAge = "Access-Control-Max-Age";
+        public static readonly string AccessControlMaxAge;
-        public const string AccessControlRequestHeaders = "Access-Control-Request-Headers";
+        public static readonly string AccessControlRequestHeaders;
-        public const string AccessControlRequestMethod = "Access-Control-Request-Method";
+        public static readonly string AccessControlRequestMethod;
-        public const string Age = "Age";
+        public static readonly string Age;
-        public const string Allow = "Allow";
+        public static readonly string Allow;
-        public const string Authority = ":authority";
+        public static readonly string Authority;
-        public const string Authorization = "Authorization";
+        public static readonly string Authorization;
-        public const string CacheControl = "Cache-Control";
+        public static readonly string CacheControl;
-        public const string Connection = "Connection";
+        public static readonly string Connection;
-        public const string ContentDisposition = "Content-Disposition";
+        public static readonly string ContentDisposition;
-        public const string ContentEncoding = "Content-Encoding";
+        public static readonly string ContentEncoding;
-        public const string ContentLanguage = "Content-Language";
+        public static readonly string ContentLanguage;
-        public const string ContentLength = "Content-Length";
+        public static readonly string ContentLength;
-        public const string ContentLocation = "Content-Location";
+        public static readonly string ContentLocation;
-        public const string ContentMD5 = "Content-MD5";
+        public static readonly string ContentMD5;
-        public const string ContentRange = "Content-Range";
+        public static readonly string ContentRange;
-        public const string ContentSecurityPolicy = "Content-Security-Policy";
+        public static readonly string ContentSecurityPolicy;
-        public const string ContentSecurityPolicyReportOnly = "Content-Security-Policy-Report-Only";
+        public static readonly string ContentSecurityPolicyReportOnly;
-        public const string ContentType = "Content-Type";
+        public static readonly string ContentType;
-        public const string Cookie = "Cookie";
+        public static readonly string Cookie;
+        public static readonly string CorrelationContext;
-        public const string Date = "Date";
+        public static readonly string Date;
+        public static readonly string DNT;
-        public const string ETag = "ETag";
+        public static readonly string ETag;
-        public const string Expect = "Expect";
+        public static readonly string Expect;
-        public const string Expires = "Expires";
+        public static readonly string Expires;
-        public const string From = "From";
+        public static readonly string From;
-        public const string Host = "Host";
+        public static readonly string Host;
-        public const string IfMatch = "If-Match";
+        public static readonly string IfMatch;
-        public const string IfModifiedSince = "If-Modified-Since";
+        public static readonly string IfModifiedSince;
-        public const string IfNoneMatch = "If-None-Match";
+        public static readonly string IfNoneMatch;
-        public const string IfRange = "If-Range";
+        public static readonly string IfRange;
-        public const string IfUnmodifiedSince = "If-Unmodified-Since";
+        public static readonly string IfUnmodifiedSince;
+        public static readonly string KeepAlive;
-        public const string LastModified = "Last-Modified";
+        public static readonly string LastModified;
-        public const string Location = "Location";
+        public static readonly string Location;
-        public const string MaxForwards = "Max-Forwards";
+        public static readonly string MaxForwards;
-        public const string Method = ":method";
+        public static readonly string Method;
-        public const string Origin = "Origin";
+        public static readonly string Origin;
-        public const string Path = ":path";
+        public static readonly string Path;
-        public const string Pragma = "Pragma";
+        public static readonly string Pragma;
-        public const string ProxyAuthenticate = "Proxy-Authenticate";
+        public static readonly string ProxyAuthenticate;
-        public const string ProxyAuthorization = "Proxy-Authorization";
+        public static readonly string ProxyAuthorization;
-        public const string Range = "Range";
+        public static readonly string Range;
-        public const string Referer = "Referer";
+        public static readonly string Referer;
+        public static readonly string RequestId;
-        public const string RetryAfter = "Retry-After";
+        public static readonly string RetryAfter;
-        public const string Scheme = ":scheme";
+        public static readonly string Scheme;
+        public static readonly string SecWebSocketAccept;
+        public static readonly string SecWebSocketKey;
+        public static readonly string SecWebSocketProtocol;
+        public static readonly string SecWebSocketVersion;
-        public const string Server = "Server";
+        public static readonly string Server;
-        public const string SetCookie = "Set-Cookie";
+        public static readonly string SetCookie;
-        public const string Status = ":status";
+        public static readonly string Status;
-        public const string StrictTransportSecurity = "Strict-Transport-Security";
+        public static readonly string StrictTransportSecurity;
-        public const string TE = "TE";
+        public static readonly string TE;
+        public static readonly string TraceParent;
+        public static readonly string TraceState;
-        public const string Trailer = "Trailer";
+        public static readonly string Trailer;
-        public const string TransferEncoding = "Transfer-Encoding";
+        public static readonly string TransferEncoding;
+        public static readonly string Translate;
-        public const string Upgrade = "Upgrade";
+        public static readonly string Upgrade;
+        public static readonly string UpgradeInsecureRequests;
-        public const string UserAgent = "User-Agent";
+        public static readonly string UserAgent;
-        public const string Vary = "Vary";
+        public static readonly string Vary;
-        public const string Via = "Via";
+        public static readonly string Via;
-        public const string Warning = "Warning";
+        public static readonly string Warning;
-        public const string WebSocketSubProtocols = "Sec-WebSocket-Protocol";
+        public static readonly string WebSocketSubProtocols;
-        public const string WWWAuthenticate = "WWW-Authenticate";
+        public static readonly string WWWAuthenticate;
+        public static readonly string XFrameOptions;
     }
     public static class HeaderQuality {
         public const double Match = 1;
         public const double NoMatch = 0;
     }
     public static class HeaderUtilities {
         public static bool ContainsCacheDirective(StringValues cacheControlDirectives, string targetDirectives);
         public static StringSegment EscapeAsQuotedString(StringSegment input);
         public static string FormatDate(DateTimeOffset dateTime);
         public static string FormatDate(DateTimeOffset dateTime, bool quoted);
         public static string FormatNonNegativeInt64(long value);
         public static bool IsQuoted(StringSegment input);
         public static StringSegment RemoveQuotes(StringSegment input);
         public static bool TryParseDate(StringSegment input, out DateTimeOffset result);
         public static bool TryParseNonNegativeInt32(StringSegment value, out int result);
         public static bool TryParseNonNegativeInt64(StringSegment value, out long result);
         public static bool TryParseSeconds(StringValues headerValues, string targetValue, out Nullable<TimeSpan> value);
         public static StringSegment UnescapeAsQuotedString(StringSegment input);
     }
     public class MediaTypeHeaderValue {
         public MediaTypeHeaderValue(StringSegment mediaType);
         public MediaTypeHeaderValue(StringSegment mediaType, double quality);
         public StringSegment Boundary { get; set; }
         public StringSegment Charset { get; set; }
         public Encoding Encoding { get; set; }
         public IEnumerable<StringSegment> Facets { get; }
         public bool IsReadOnly { get; }
         public bool MatchesAllSubTypes { get; }
         public bool MatchesAllSubTypesWithoutSuffix { get; }
         public bool MatchesAllTypes { get; }
         public StringSegment MediaType { get; set; }
         public IList<NameValueHeaderValue> Parameters { get; }
         public Nullable<double> Quality { get; set; }
         public StringSegment SubType { get; }
         public StringSegment SubTypeWithoutSuffix { get; }
         public StringSegment Suffix { get; }
         public StringSegment Type { get; }
         public MediaTypeHeaderValue Copy();
         public MediaTypeHeaderValue CopyAsReadOnly();
         public override bool Equals(object obj);
         public override int GetHashCode();
         public bool IsSubsetOf(MediaTypeHeaderValue otherMediaType);
         public static MediaTypeHeaderValue Parse(StringSegment input);
         public static IList<MediaTypeHeaderValue> ParseList(IList<string> inputs);
         public static IList<MediaTypeHeaderValue> ParseStrictList(IList<string> inputs);
         public override string ToString();
         public static bool TryParse(StringSegment input, out MediaTypeHeaderValue parsedValue);
         public static bool TryParseList(IList<string> inputs, out IList<MediaTypeHeaderValue> parsedValues);
         public static bool TryParseStrictList(IList<string> inputs, out IList<MediaTypeHeaderValue> parsedValues);
     }
     public class MediaTypeHeaderValueComparer : IComparer<MediaTypeHeaderValue> {
         public static MediaTypeHeaderValueComparer QualityComparer { get; }
         public int Compare(MediaTypeHeaderValue mediaType1, MediaTypeHeaderValue mediaType2);
     }
     public class NameValueHeaderValue {
         public NameValueHeaderValue(StringSegment name);
         public NameValueHeaderValue(StringSegment name, StringSegment value);
         public bool IsReadOnly { get; }
         public StringSegment Name { get; }
         public StringSegment Value { get; set; }
         public NameValueHeaderValue Copy();
         public NameValueHeaderValue CopyAsReadOnly();
         public override bool Equals(object obj);
         public static NameValueHeaderValue Find(IList<NameValueHeaderValue> values, StringSegment name);
         public override int GetHashCode();
         public StringSegment GetUnescapedValue();
         public static NameValueHeaderValue Parse(StringSegment input);
         public static IList<NameValueHeaderValue> ParseList(IList<string> input);
         public static IList<NameValueHeaderValue> ParseStrictList(IList<string> input);
         public void SetAndEscapeValue(StringSegment value);
         public override string ToString();
         public static bool TryParse(StringSegment input, out NameValueHeaderValue parsedValue);
         public static bool TryParseList(IList<string> input, out IList<NameValueHeaderValue> parsedValues);
         public static bool TryParseStrictList(IList<string> input, out IList<NameValueHeaderValue> parsedValues);
     }
     public class RangeConditionHeaderValue {
         public RangeConditionHeaderValue(EntityTagHeaderValue entityTag);
         public RangeConditionHeaderValue(DateTimeOffset lastModified);
         public RangeConditionHeaderValue(string entityTag);
         public EntityTagHeaderValue EntityTag { get; }
         public Nullable<DateTimeOffset> LastModified { get; }
         public override bool Equals(object obj);
         public override int GetHashCode();
         public static RangeConditionHeaderValue Parse(StringSegment input);
         public override string ToString();
         public static bool TryParse(StringSegment input, out RangeConditionHeaderValue parsedValue);
     }
     public class RangeHeaderValue {
         public RangeHeaderValue();
         public RangeHeaderValue(Nullable<long> from, Nullable<long> to);
         public ICollection<RangeItemHeaderValue> Ranges { get; }
         public StringSegment Unit { get; set; }
         public override bool Equals(object obj);
         public override int GetHashCode();
         public static RangeHeaderValue Parse(StringSegment input);
         public override string ToString();
         public static bool TryParse(StringSegment input, out RangeHeaderValue parsedValue);
     }
     public class RangeItemHeaderValue {
         public RangeItemHeaderValue(Nullable<long> from, Nullable<long> to);
         public Nullable<long> From { get; }
         public Nullable<long> To { get; }
         public override bool Equals(object obj);
         public override int GetHashCode();
         public override string ToString();
     }
     public enum SameSiteMode {
         Lax = 1,
         None = 0,
         Strict = 2,
     }
     public class SetCookieHeaderValue {
         public SetCookieHeaderValue(StringSegment name);
         public SetCookieHeaderValue(StringSegment name, StringSegment value);
         public StringSegment Domain { get; set; }
         public Nullable<DateTimeOffset> Expires { get; set; }
         public bool HttpOnly { get; set; }
         public Nullable<TimeSpan> MaxAge { get; set; }
         public StringSegment Name { get; set; }
         public StringSegment Path { get; set; }
         public SameSiteMode SameSite { get; set; }
         public bool Secure { get; set; }
         public StringSegment Value { get; set; }
         public void AppendToStringBuilder(StringBuilder builder);
         public override bool Equals(object obj);
         public override int GetHashCode();
         public static SetCookieHeaderValue Parse(StringSegment input);
         public static IList<SetCookieHeaderValue> ParseList(IList<string> inputs);
         public static IList<SetCookieHeaderValue> ParseStrictList(IList<string> inputs);
         public override string ToString();
         public static bool TryParse(StringSegment input, out SetCookieHeaderValue parsedValue);
         public static bool TryParseList(IList<string> inputs, out IList<SetCookieHeaderValue> parsedValues);
         public static bool TryParseStrictList(IList<string> inputs, out IList<SetCookieHeaderValue> parsedValues);
     }
     public class StringWithQualityHeaderValue {
         public StringWithQualityHeaderValue(StringSegment value);
         public StringWithQualityHeaderValue(StringSegment value, double quality);
         public Nullable<double> Quality { get; }
         public StringSegment Value { get; }
         public override bool Equals(object obj);
         public override int GetHashCode();
         public static StringWithQualityHeaderValue Parse(StringSegment input);
         public static IList<StringWithQualityHeaderValue> ParseList(IList<string> input);
         public static IList<StringWithQualityHeaderValue> ParseStrictList(IList<string> input);
         public override string ToString();
         public static bool TryParse(StringSegment input, out StringWithQualityHeaderValue parsedValue);
         public static bool TryParseList(IList<string> input, out IList<StringWithQualityHeaderValue> parsedValues);
         public static bool TryParseStrictList(IList<string> input, out IList<StringWithQualityHeaderValue> parsedValues);
     }
     public class StringWithQualityHeaderValueComparer : IComparer<StringWithQualityHeaderValue> {
         public static StringWithQualityHeaderValueComparer QualityComparer { get; }
         public int Compare(StringWithQualityHeaderValue stringWithQuality1, StringWithQualityHeaderValue stringWithQuality2);
     }
 }
```

