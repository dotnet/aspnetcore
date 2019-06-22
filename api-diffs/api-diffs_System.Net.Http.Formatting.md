# System.Net.Http.Formatting

``` diff
-namespace System.Net.Http.Formatting {
 {
-    public abstract class BaseJsonMediaTypeFormatter : MediaTypeFormatter {
 {
-        protected BaseJsonMediaTypeFormatter();

-        protected BaseJsonMediaTypeFormatter(BaseJsonMediaTypeFormatter formatter);

-        public virtual int MaxDepth { get; set; }

-        public JsonSerializerSettings SerializerSettings { get; set; }

-        public override bool CanReadType(Type type);

-        public override bool CanWriteType(Type type);

-        public JsonSerializerSettings CreateDefaultSerializerSettings();

-        public abstract JsonReader CreateJsonReader(Type type, Stream readStream, Encoding effectiveEncoding);

-        public virtual JsonSerializer CreateJsonSerializer();

-        public abstract JsonWriter CreateJsonWriter(Type type, Stream writeStream, Encoding effectiveEncoding);

-        public virtual object ReadFromStream(Type type, Stream readStream, Encoding effectiveEncoding, IFormatterLogger formatterLogger);

-        public override Task<object> ReadFromStreamAsync(Type type, Stream readStream, HttpContent content, IFormatterLogger formatterLogger);

-        public virtual void WriteToStream(Type type, object value, Stream writeStream, Encoding effectiveEncoding);

-        public override Task WriteToStreamAsync(Type type, object value, Stream writeStream, HttpContent content, TransportContext transportContext, CancellationToken cancellationToken);

-    }
-    public class BsonMediaTypeFormatter : BaseJsonMediaTypeFormatter {
 {
-        public BsonMediaTypeFormatter();

-        protected BsonMediaTypeFormatter(BsonMediaTypeFormatter formatter);

-        public static MediaTypeHeaderValue DefaultMediaType { get; }

-        public sealed override int MaxDepth { get; set; }

-        public override JsonReader CreateJsonReader(Type type, Stream readStream, Encoding effectiveEncoding);

-        public override JsonWriter CreateJsonWriter(Type type, Stream writeStream, Encoding effectiveEncoding);

-        public override object ReadFromStream(Type type, Stream readStream, Encoding effectiveEncoding, IFormatterLogger formatterLogger);

-        public override Task<object> ReadFromStreamAsync(Type type, Stream readStream, HttpContent content, IFormatterLogger formatterLogger);

-        public override void WriteToStream(Type type, object value, Stream writeStream, Encoding effectiveEncoding);

-    }
-    public abstract class BufferedMediaTypeFormatter : MediaTypeFormatter {
 {
-        protected BufferedMediaTypeFormatter();

-        protected BufferedMediaTypeFormatter(BufferedMediaTypeFormatter formatter);

-        public int BufferSize { get; set; }

-        public virtual object ReadFromStream(Type type, Stream readStream, HttpContent content, IFormatterLogger formatterLogger);

-        public virtual object ReadFromStream(Type type, Stream readStream, HttpContent content, IFormatterLogger formatterLogger, CancellationToken cancellationToken);

-        public sealed override Task<object> ReadFromStreamAsync(Type type, Stream readStream, HttpContent content, IFormatterLogger formatterLogger);

-        public sealed override Task<object> ReadFromStreamAsync(Type type, Stream readStream, HttpContent content, IFormatterLogger formatterLogger, CancellationToken cancellationToken);

-        public virtual void WriteToStream(Type type, object value, Stream writeStream, HttpContent content);

-        public virtual void WriteToStream(Type type, object value, Stream writeStream, HttpContent content, CancellationToken cancellationToken);

-        public sealed override Task WriteToStreamAsync(Type type, object value, Stream writeStream, HttpContent content, TransportContext transportContext);

-        public sealed override Task WriteToStreamAsync(Type type, object value, Stream writeStream, HttpContent content, TransportContext transportContext, CancellationToken cancellationToken);

-    }
-    public class ContentNegotiationResult {
 {
-        public ContentNegotiationResult(MediaTypeFormatter formatter, MediaTypeHeaderValue mediaType);

-        public MediaTypeFormatter Formatter { get; set; }

-        public MediaTypeHeaderValue MediaType { get; set; }

-    }
-    public class DefaultContentNegotiator : IContentNegotiator {
 {
-        public DefaultContentNegotiator();

-        public DefaultContentNegotiator(bool excludeMatchOnTypeOnly);

-        public bool ExcludeMatchOnTypeOnly { get; private set; }

-        protected virtual Collection<MediaTypeFormatterMatch> ComputeFormatterMatches(Type type, HttpRequestMessage request, IEnumerable<MediaTypeFormatter> formatters);

-        protected virtual MediaTypeFormatterMatch MatchAcceptHeader(IEnumerable<MediaTypeWithQualityHeaderValue> sortedAcceptValues, MediaTypeFormatter formatter);

-        protected virtual MediaTypeFormatterMatch MatchMediaTypeMapping(HttpRequestMessage request, MediaTypeFormatter formatter);

-        protected virtual MediaTypeFormatterMatch MatchRequestMediaType(HttpRequestMessage request, MediaTypeFormatter formatter);

-        protected virtual MediaTypeFormatterMatch MatchType(Type type, MediaTypeFormatter formatter);

-        public virtual ContentNegotiationResult Negotiate(Type type, HttpRequestMessage request, IEnumerable<MediaTypeFormatter> formatters);

-        protected virtual Encoding SelectResponseCharacterEncoding(HttpRequestMessage request, MediaTypeFormatter formatter);

-        protected virtual MediaTypeFormatterMatch SelectResponseMediaTypeFormatter(ICollection<MediaTypeFormatterMatch> matches);

-        protected virtual bool ShouldMatchOnType(IEnumerable<MediaTypeWithQualityHeaderValue> sortedAcceptValues);

-        protected virtual IEnumerable<MediaTypeWithQualityHeaderValue> SortMediaTypeWithQualityHeaderValuesByQFactor(ICollection<MediaTypeWithQualityHeaderValue> headerValues);

-        protected virtual IEnumerable<StringWithQualityHeaderValue> SortStringWithQualityHeaderValuesByQFactor(ICollection<StringWithQualityHeaderValue> headerValues);

-        protected virtual MediaTypeFormatterMatch UpdateBestMatch(MediaTypeFormatterMatch current, MediaTypeFormatterMatch potentialReplacement);

-    }
-    public sealed class DelegatingEnumerable<T> : IEnumerable, IEnumerable<T> {
 {
-        public DelegatingEnumerable();

-        public DelegatingEnumerable(IEnumerable<T> source);

-        public void Add(object item);

-        public IEnumerator<T> GetEnumerator();

-        IEnumerator System.Collections.IEnumerable.GetEnumerator();

-    }
-    public class FormDataCollection : IEnumerable, IEnumerable<KeyValuePair<string, string>> {
 {
-        public FormDataCollection(IEnumerable<KeyValuePair<string, string>> pairs);

-        public FormDataCollection(string query);

-        public FormDataCollection(Uri uri);

-        public string this[string name] { get; }

-        public string Get(string key);

-        public IEnumerator<KeyValuePair<string, string>> GetEnumerator();

-        public string[] GetValues(string key);

-        public NameValueCollection ReadAsNameValueCollection();

-        IEnumerator System.Collections.IEnumerable.GetEnumerator();

-    }
-    public class FormUrlEncodedMediaTypeFormatter : MediaTypeFormatter {
 {
-        public FormUrlEncodedMediaTypeFormatter();

-        protected FormUrlEncodedMediaTypeFormatter(FormUrlEncodedMediaTypeFormatter formatter);

-        public static MediaTypeHeaderValue DefaultMediaType { get; }

-        public int MaxDepth { get; set; }

-        public int ReadBufferSize { get; set; }

-        public override bool CanReadType(Type type);

-        public override bool CanWriteType(Type type);

-        public override Task<object> ReadFromStreamAsync(Type type, Stream readStream, HttpContent content, IFormatterLogger formatterLogger);

-    }
-    public interface IContentNegotiator {
 {
-        ContentNegotiationResult Negotiate(Type type, HttpRequestMessage request, IEnumerable<MediaTypeFormatter> formatters);

-    }
-    public interface IFormatterLogger {
 {
-        void LogError(string errorPath, Exception exception);

-        void LogError(string errorPath, string errorMessage);

-    }
-    public interface IRequiredMemberSelector {
 {
-        bool IsRequiredMember(MemberInfo member);

-    }
-    public class JsonContractResolver : DefaultContractResolver {
 {
-        public JsonContractResolver(MediaTypeFormatter formatter);

-        protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization);

-    }
-    public class JsonMediaTypeFormatter : BaseJsonMediaTypeFormatter {
 {
-        public JsonMediaTypeFormatter();

-        protected JsonMediaTypeFormatter(JsonMediaTypeFormatter formatter);

-        public static MediaTypeHeaderValue DefaultMediaType { get; }

-        public bool Indent { get; set; }

-        public sealed override int MaxDepth { get; set; }

-        public bool UseDataContractJsonSerializer { get; set; }

-        public override bool CanReadType(Type type);

-        public override bool CanWriteType(Type type);

-        public virtual DataContractJsonSerializer CreateDataContractSerializer(Type type);

-        public override JsonReader CreateJsonReader(Type type, Stream readStream, Encoding effectiveEncoding);

-        public override JsonWriter CreateJsonWriter(Type type, Stream writeStream, Encoding effectiveEncoding);

-        public override object ReadFromStream(Type type, Stream readStream, Encoding effectiveEncoding, IFormatterLogger formatterLogger);

-        public override void WriteToStream(Type type, object value, Stream writeStream, Encoding effectiveEncoding);

-        public override Task WriteToStreamAsync(Type type, object value, Stream writeStream, HttpContent content, TransportContext transportContext, CancellationToken cancellationToken);

-    }
-    public abstract class MediaTypeFormatter {
 {
-        protected MediaTypeFormatter();

-        protected MediaTypeFormatter(MediaTypeFormatter formatter);

-        public static int MaxHttpCollectionKeys { get; set; }

-        public Collection<MediaTypeMapping> MediaTypeMappings { get; private set; }

-        public virtual IRequiredMemberSelector RequiredMemberSelector { get; set; }

-        public Collection<Encoding> SupportedEncodings { get; private set; }

-        public Collection<MediaTypeHeaderValue> SupportedMediaTypes { get; private set; }

-        public abstract bool CanReadType(Type type);

-        public abstract bool CanWriteType(Type type);

-        public static object GetDefaultValueForType(Type type);

-        public virtual MediaTypeFormatter GetPerRequestFormatterInstance(Type type, HttpRequestMessage request, MediaTypeHeaderValue mediaType);

-        public virtual Task<object> ReadFromStreamAsync(Type type, Stream readStream, HttpContent content, IFormatterLogger formatterLogger);

-        public virtual Task<object> ReadFromStreamAsync(Type type, Stream readStream, HttpContent content, IFormatterLogger formatterLogger, CancellationToken cancellationToken);

-        public Encoding SelectCharacterEncoding(HttpContentHeaders contentHeaders);

-        public virtual void SetDefaultContentHeaders(Type type, HttpContentHeaders headers, MediaTypeHeaderValue mediaType);

-        public virtual Task WriteToStreamAsync(Type type, object value, Stream writeStream, HttpContent content, TransportContext transportContext);

-        public virtual Task WriteToStreamAsync(Type type, object value, Stream writeStream, HttpContent content, TransportContext transportContext, CancellationToken cancellationToken);

-    }
-    public class MediaTypeFormatterCollection : Collection<MediaTypeFormatter> {
 {
-        public MediaTypeFormatterCollection();

-        public MediaTypeFormatterCollection(IEnumerable<MediaTypeFormatter> formatters);

-        public FormUrlEncodedMediaTypeFormatter FormUrlEncodedFormatter { get; }

-        public JsonMediaTypeFormatter JsonFormatter { get; }

-        public XmlMediaTypeFormatter XmlFormatter { get; }

-        public void AddRange(IEnumerable<MediaTypeFormatter> items);

-        protected override void ClearItems();

-        public MediaTypeFormatter FindReader(Type type, MediaTypeHeaderValue mediaType);

-        public MediaTypeFormatter FindWriter(Type type, MediaTypeHeaderValue mediaType);

-        protected override void InsertItem(int index, MediaTypeFormatter item);

-        public void InsertRange(int index, IEnumerable<MediaTypeFormatter> items);

-        public static bool IsTypeExcludedFromValidation(Type type);

-        protected override void RemoveItem(int index);

-        protected override void SetItem(int index, MediaTypeFormatter item);

-    }
-    public static class MediaTypeFormatterExtensions {
 {
-        public static void AddQueryStringMapping(this MediaTypeFormatter formatter, string queryStringParameterName, string queryStringParameterValue, MediaTypeHeaderValue mediaType);

-        public static void AddQueryStringMapping(this MediaTypeFormatter formatter, string queryStringParameterName, string queryStringParameterValue, string mediaType);

-        public static void AddRequestHeaderMapping(this MediaTypeFormatter formatter, string headerName, string headerValue, StringComparison valueComparison, bool isValueSubstring, MediaTypeHeaderValue mediaType);

-        public static void AddRequestHeaderMapping(this MediaTypeFormatter formatter, string headerName, string headerValue, StringComparison valueComparison, bool isValueSubstring, string mediaType);

-    }
-    public class MediaTypeFormatterMatch {
 {
-        public MediaTypeFormatterMatch(MediaTypeFormatter formatter, MediaTypeHeaderValue mediaType, Nullable<double> quality, MediaTypeFormatterMatchRanking ranking);

-        public MediaTypeFormatter Formatter { get; private set; }

-        public MediaTypeHeaderValue MediaType { get; private set; }

-        public double Quality { get; private set; }

-        public MediaTypeFormatterMatchRanking Ranking { get; private set; }

-    }
-    public enum MediaTypeFormatterMatchRanking {
 {
-        MatchOnCanWriteType = 1,

-        MatchOnRequestAcceptHeaderAllMediaRange = 4,

-        MatchOnRequestAcceptHeaderLiteral = 2,

-        MatchOnRequestAcceptHeaderSubtypeMediaRange = 3,

-        MatchOnRequestMediaType = 6,

-        MatchOnRequestWithMediaTypeMapping = 5,

-        None = 0,

-    }
-    public abstract class MediaTypeMapping {
 {
-        protected MediaTypeMapping(MediaTypeHeaderValue mediaType);

-        protected MediaTypeMapping(string mediaType);

-        public MediaTypeHeaderValue MediaType { get; private set; }

-        public abstract double TryMatchMediaType(HttpRequestMessage request);

-    }
-    public class QueryStringMapping : MediaTypeMapping {
 {
-        public QueryStringMapping(string queryStringParameterName, string queryStringParameterValue, MediaTypeHeaderValue mediaType);

-        public QueryStringMapping(string queryStringParameterName, string queryStringParameterValue, string mediaType);

-        public string QueryStringParameterName { get; private set; }

-        public string QueryStringParameterValue { get; private set; }

-        public override double TryMatchMediaType(HttpRequestMessage request);

-    }
-    public class RequestHeaderMapping : MediaTypeMapping {
 {
-        public RequestHeaderMapping(string headerName, string headerValue, StringComparison valueComparison, bool isValueSubstring, MediaTypeHeaderValue mediaType);

-        public RequestHeaderMapping(string headerName, string headerValue, StringComparison valueComparison, bool isValueSubstring, string mediaType);

-        public string HeaderName { get; private set; }

-        public string HeaderValue { get; private set; }

-        public StringComparison HeaderValueComparison { get; private set; }

-        public bool IsValueSubstring { get; private set; }

-        public override double TryMatchMediaType(HttpRequestMessage request);

-    }
-    public class XmlHttpRequestHeaderMapping : RequestHeaderMapping {
 {
-        public XmlHttpRequestHeaderMapping();

-        public override double TryMatchMediaType(HttpRequestMessage request);

-    }
-    public class XmlMediaTypeFormatter : MediaTypeFormatter {
 {
-        public XmlMediaTypeFormatter();

-        protected XmlMediaTypeFormatter(XmlMediaTypeFormatter formatter);

-        public static MediaTypeHeaderValue DefaultMediaType { get; }

-        public bool Indent { get; set; }

-        public int MaxDepth { get; set; }

-        public bool UseXmlSerializer { get; set; }

-        public XmlWriterSettings WriterSettings { get; private set; }

-        public override bool CanReadType(Type type);

-        public override bool CanWriteType(Type type);

-        public virtual DataContractSerializer CreateDataContractSerializer(Type type);

-        protected internal virtual XmlReader CreateXmlReader(Stream readStream, HttpContent content);

-        public virtual XmlSerializer CreateXmlSerializer(Type type);

-        protected internal virtual XmlWriter CreateXmlWriter(Stream writeStream, HttpContent content);

-        protected internal virtual object GetDeserializer(Type type, HttpContent content);

-        protected internal virtual object GetSerializer(Type type, object value, HttpContent content);

-        public XmlReader InvokeCreateXmlReader(Stream readStream, HttpContent content);

-        public XmlWriter InvokeCreateXmlWriter(Stream writeStream, HttpContent content);

-        public object InvokeGetDeserializer(Type type, HttpContent content);

-        public object InvokeGetSerializer(Type type, object value, HttpContent content);

-        public override Task<object> ReadFromStreamAsync(Type type, Stream readStream, HttpContent content, IFormatterLogger formatterLogger);

-        public bool RemoveSerializer(Type type);

-        public void SetSerializer(Type type, XmlObjectSerializer serializer);

-        public void SetSerializer(Type type, XmlSerializer serializer);

-        public void SetSerializer<T>(XmlObjectSerializer serializer);

-        public void SetSerializer<T>(XmlSerializer serializer);

-        public override Task WriteToStreamAsync(Type type, object value, Stream writeStream, HttpContent content, TransportContext transportContext, CancellationToken cancellationToken);

-    }
-}
```

