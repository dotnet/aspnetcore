# Microsoft.AspNetCore.Mvc.Formatters

``` diff
 namespace Microsoft.AspNetCore.Mvc.Formatters {
-    public class FormatFilter : IFilterMetadata, IFormatFilter, IResourceFilter, IResultFilter {
+    public class FormatFilter : IFilterMetadata, IResourceFilter, IResultFilter {
-        public FormatFilter(IOptions<MvcOptions> options);

         public FormatFilter(IOptions<MvcOptions> options, ILoggerFactory loggerFactory);
         public virtual string GetFormat(ActionContext context);
         public void OnResourceExecuted(ResourceExecutedContext context);
         public void OnResourceExecuting(ResourceExecutingContext context);
         public void OnResultExecuted(ResultExecutedContext context);
         public void OnResultExecuting(ResultExecutingContext context);
     }
     public class FormatterCollection<TFormatter> : Collection<TFormatter> {
         public FormatterCollection();
         public FormatterCollection(IList<TFormatter> list);
         public void RemoveType(Type formatterType);
         public void RemoveType<T>() where T : TFormatter;
     }
     public class FormatterMappings {
         public FormatterMappings();
         public bool ClearMediaTypeMappingForFormat(string format);
         public string GetMediaTypeMappingForFormat(string format);
         public void SetMediaTypeMappingForFormat(string format, MediaTypeHeaderValue contentType);
         public void SetMediaTypeMappingForFormat(string format, string contentType);
     }
     public class HttpNoContentOutputFormatter : IOutputFormatter {
         public HttpNoContentOutputFormatter();
         public bool TreatNullValueAsNoContent { get; set; }
         public bool CanWriteResult(OutputFormatterCanWriteContext context);
         public Task WriteAsync(OutputFormatterWriteContext context);
     }
     public interface IInputFormatter {
         bool CanRead(InputFormatterContext context);
         Task<InputFormatterResult> ReadAsync(InputFormatterContext context);
     }
     public interface IInputFormatterExceptionPolicy {
         InputFormatterExceptionPolicy ExceptionPolicy { get; }
     }
     public abstract class InputFormatter : IApiRequestFormatMetadataProvider, IInputFormatter {
         protected InputFormatter();
         public MediaTypeCollection SupportedMediaTypes { get; }
         public virtual bool CanRead(InputFormatterContext context);
         protected virtual bool CanReadType(Type type);
         protected virtual object GetDefaultValueForType(Type modelType);
         public virtual IReadOnlyList<string> GetSupportedContentTypes(string contentType, Type objectType);
         public virtual Task<InputFormatterResult> ReadAsync(InputFormatterContext context);
         public abstract Task<InputFormatterResult> ReadRequestBodyAsync(InputFormatterContext context);
     }
     public class InputFormatterContext {
         public InputFormatterContext(HttpContext httpContext, string modelName, ModelStateDictionary modelState, ModelMetadata metadata, Func<Stream, Encoding, TextReader> readerFactory);
         public InputFormatterContext(HttpContext httpContext, string modelName, ModelStateDictionary modelState, ModelMetadata metadata, Func<Stream, Encoding, TextReader> readerFactory, bool treatEmptyInputAsDefaultValue);
         public HttpContext HttpContext { get; }
         public ModelMetadata Metadata { get; }
         public string ModelName { get; }
         public ModelStateDictionary ModelState { get; }
         public Type ModelType { get; }
         public Func<Stream, Encoding, TextReader> ReaderFactory { get; }
         public bool TreatEmptyInputAsDefaultValue { get; }
     }
     public class InputFormatterException : Exception {
         public InputFormatterException();
         public InputFormatterException(string message);
         public InputFormatterException(string message, Exception innerException);
     }
     public enum InputFormatterExceptionPolicy {
         AllExceptions = 0,
         MalformedInputExceptions = 1,
     }
     public class InputFormatterResult {
         public bool HasError { get; }
         public bool IsModelSet { get; }
         public object Model { get; }
         public static InputFormatterResult Failure();
         public static Task<InputFormatterResult> FailureAsync();
         public static InputFormatterResult NoValue();
         public static Task<InputFormatterResult> NoValueAsync();
         public static InputFormatterResult Success(object model);
         public static Task<InputFormatterResult> SuccessAsync(object model);
     }
     public interface IOutputFormatter {
         bool CanWriteResult(OutputFormatterCanWriteContext context);
         Task WriteAsync(OutputFormatterWriteContext context);
     }
-    public class JsonInputFormatter : TextInputFormatter, IInputFormatterExceptionPolicy {
 {
-        public JsonInputFormatter(ILogger logger, JsonSerializerSettings serializerSettings, ArrayPool<char> charPool, ObjectPoolProvider objectPoolProvider);

-        public JsonInputFormatter(ILogger logger, JsonSerializerSettings serializerSettings, ArrayPool<char> charPool, ObjectPoolProvider objectPoolProvider, MvcOptions options, MvcJsonOptions jsonOptions);

-        public JsonInputFormatter(ILogger logger, JsonSerializerSettings serializerSettings, ArrayPool<char> charPool, ObjectPoolProvider objectPoolProvider, bool suppressInputFormatterBuffering);

-        public JsonInputFormatter(ILogger logger, JsonSerializerSettings serializerSettings, ArrayPool<char> charPool, ObjectPoolProvider objectPoolProvider, bool suppressInputFormatterBuffering, bool allowInputFormatterExceptionMessages);

-        public virtual InputFormatterExceptionPolicy ExceptionPolicy { get; }

-        protected JsonSerializerSettings SerializerSettings { get; }

-        protected virtual JsonSerializer CreateJsonSerializer();

-        public override Task<InputFormatterResult> ReadRequestBodyAsync(InputFormatterContext context, Encoding encoding);

-        protected virtual void ReleaseJsonSerializer(JsonSerializer serializer);

-    }
-    public class JsonOutputFormatter : TextOutputFormatter {
 {
-        public JsonOutputFormatter(JsonSerializerSettings serializerSettings, ArrayPool<char> charPool);

-        public JsonSerializerSettings PublicSerializerSettings { get; }

-        protected JsonSerializerSettings SerializerSettings { get; }

-        protected virtual JsonSerializer CreateJsonSerializer();

-        protected virtual JsonWriter CreateJsonWriter(TextWriter writer);

-        public void WriteObject(TextWriter writer, object value);

-        public override Task WriteResponseBodyAsync(OutputFormatterWriteContext context, Encoding selectedEncoding);

-    }
-    public class JsonPatchInputFormatter : JsonInputFormatter {
 {
-        public JsonPatchInputFormatter(ILogger logger, JsonSerializerSettings serializerSettings, ArrayPool<char> charPool, ObjectPoolProvider objectPoolProvider);

-        public JsonPatchInputFormatter(ILogger logger, JsonSerializerSettings serializerSettings, ArrayPool<char> charPool, ObjectPoolProvider objectPoolProvider, MvcOptions options, MvcJsonOptions jsonOptions);

-        public JsonPatchInputFormatter(ILogger logger, JsonSerializerSettings serializerSettings, ArrayPool<char> charPool, ObjectPoolProvider objectPoolProvider, bool suppressInputFormatterBuffering);

-        public JsonPatchInputFormatter(ILogger logger, JsonSerializerSettings serializerSettings, ArrayPool<char> charPool, ObjectPoolProvider objectPoolProvider, bool suppressInputFormatterBuffering, bool allowInputFormatterExceptionMessages);

-        public override InputFormatterExceptionPolicy ExceptionPolicy { get; }

-        public override bool CanRead(InputFormatterContext context);

-        public override Task<InputFormatterResult> ReadRequestBodyAsync(InputFormatterContext context, Encoding encoding);

-    }
-    public static class JsonSerializerSettingsProvider {
 {
-        public static JsonSerializerSettings CreateSerializerSettings();

-    }
     public readonly struct MediaType {
         public MediaType(StringSegment mediaType);
         public MediaType(string mediaType);
         public MediaType(string mediaType, int offset, Nullable<int> length);
         public StringSegment Charset { get; }
         public Encoding Encoding { get; }
         public bool HasWildcard { get; }
         public bool MatchesAllSubTypes { get; }
         public bool MatchesAllSubTypesWithoutSuffix { get; }
         public bool MatchesAllTypes { get; }
         public StringSegment SubType { get; }
         public StringSegment SubTypeSuffix { get; }
         public StringSegment SubTypeWithoutSuffix { get; }
         public StringSegment Type { get; }
-        public static MediaTypeSegmentWithQuality CreateMediaTypeSegmentWithQuality(string mediaType, int start);

+        public static MediaTypeSegmentWithQuality CreateMediaTypeSegmentWithQuality(string mediaType, int start);
         public static Encoding GetEncoding(StringSegment mediaType);
         public static Encoding GetEncoding(string mediaType);
         public StringSegment GetParameter(StringSegment parameterName);
         public StringSegment GetParameter(string parameterName);
         public bool IsSubsetOf(MediaType @set);
         public static string ReplaceEncoding(StringSegment mediaType, Encoding encoding);
         public static string ReplaceEncoding(string mediaType, Encoding encoding);
     }
     public class MediaTypeCollection : Collection<string> {
         public MediaTypeCollection();
         public void Add(MediaTypeHeaderValue item);
         public void Insert(int index, MediaTypeHeaderValue item);
         public bool Remove(MediaTypeHeaderValue item);
     }
+    public readonly struct MediaTypeSegmentWithQuality {
+        public MediaTypeSegmentWithQuality(StringSegment mediaType, double quality);
+        public StringSegment MediaType { get; }
+        public double Quality { get; }
+        public override string ToString();
+    }
     public abstract class OutputFormatter : IApiResponseTypeMetadataProvider, IOutputFormatter {
         protected OutputFormatter();
         public MediaTypeCollection SupportedMediaTypes { get; }
         public virtual bool CanWriteResult(OutputFormatterCanWriteContext context);
         protected virtual bool CanWriteType(Type type);
         public virtual IReadOnlyList<string> GetSupportedContentTypes(string contentType, Type objectType);
         public virtual Task WriteAsync(OutputFormatterWriteContext context);
         public abstract Task WriteResponseBodyAsync(OutputFormatterWriteContext context);
         public virtual void WriteResponseHeaders(OutputFormatterWriteContext context);
     }
     public abstract class OutputFormatterCanWriteContext {
-        protected OutputFormatterCanWriteContext();

         protected OutputFormatterCanWriteContext(HttpContext httpContext);
         public virtual StringSegment ContentType { get; set; }
         public virtual bool ContentTypeIsServerDefined { get; set; }
         public virtual HttpContext HttpContext { get; protected set; }
         public virtual object Object { get; protected set; }
         public virtual Type ObjectType { get; protected set; }
     }
     public class OutputFormatterWriteContext : OutputFormatterCanWriteContext {
         public OutputFormatterWriteContext(HttpContext httpContext, Func<Stream, Encoding, TextWriter> writerFactory, Type objectType, object @object);
         public virtual Func<Stream, Encoding, TextWriter> WriterFactory { get; protected set; }
     }
     public class StreamOutputFormatter : IOutputFormatter {
         public StreamOutputFormatter();
         public bool CanWriteResult(OutputFormatterCanWriteContext context);
         public Task WriteAsync(OutputFormatterWriteContext context);
     }
     public class StringOutputFormatter : TextOutputFormatter {
         public StringOutputFormatter();
         public override bool CanWriteResult(OutputFormatterCanWriteContext context);
         public override Task WriteResponseBodyAsync(OutputFormatterWriteContext context, Encoding encoding);
     }
+    public class SystemTextJsonInputFormatter : TextInputFormatter, IInputFormatterExceptionPolicy {
+        public SystemTextJsonInputFormatter(JsonOptions options);
+        InputFormatterExceptionPolicy Microsoft.AspNetCore.Mvc.Formatters.IInputFormatterExceptionPolicy.ExceptionPolicy { get; }
+        public JsonSerializerOptions SerializerOptions { get; }
+        public sealed override Task<InputFormatterResult> ReadRequestBodyAsync(InputFormatterContext context, Encoding encoding);
+    }
+    public class SystemTextJsonOutputFormatter : TextOutputFormatter {
+        public SystemTextJsonOutputFormatter(JsonOptions options);
+        public JsonSerializerOptions SerializerOptions { get; }
+        public sealed override Task WriteResponseBodyAsync(OutputFormatterWriteContext context, Encoding selectedEncoding);
+    }
     public abstract class TextInputFormatter : InputFormatter {
         protected static readonly Encoding UTF16EncodingLittleEndian;
         protected static readonly Encoding UTF8EncodingWithoutBOM;
         protected TextInputFormatter();
         public IList<Encoding> SupportedEncodings { get; }
         public override Task<InputFormatterResult> ReadRequestBodyAsync(InputFormatterContext context);
         public abstract Task<InputFormatterResult> ReadRequestBodyAsync(InputFormatterContext context, Encoding encoding);
         protected Encoding SelectCharacterEncoding(InputFormatterContext context);
     }
     public abstract class TextOutputFormatter : OutputFormatter {
         protected TextOutputFormatter();
         public IList<Encoding> SupportedEncodings { get; }
         public virtual Encoding SelectCharacterEncoding(OutputFormatterWriteContext context);
         public override Task WriteAsync(OutputFormatterWriteContext context);
         public sealed override Task WriteResponseBodyAsync(OutputFormatterWriteContext context);
         public abstract Task WriteResponseBodyAsync(OutputFormatterWriteContext context, Encoding selectedEncoding);
     }
     public class XmlDataContractSerializerInputFormatter : TextInputFormatter, IInputFormatterExceptionPolicy {
-        public XmlDataContractSerializerInputFormatter();

         public XmlDataContractSerializerInputFormatter(MvcOptions options);
-        public XmlDataContractSerializerInputFormatter(bool suppressInputFormatterBuffering);

         public virtual InputFormatterExceptionPolicy ExceptionPolicy { get; }
         public int MaxDepth { get; set; }
         public DataContractSerializerSettings SerializerSettings { get; set; }
         public IList<IWrapperProviderFactory> WrapperProviderFactories { get; }
         public XmlDictionaryReaderQuotas XmlDictionaryReaderQuotas { get; }
         protected override bool CanReadType(Type type);
         protected virtual DataContractSerializer CreateSerializer(Type type);
         protected virtual XmlReader CreateXmlReader(Stream readStream, Encoding encoding);
         protected virtual DataContractSerializer GetCachedSerializer(Type type);
         protected virtual Type GetSerializableType(Type declaredType);
         public override Task<InputFormatterResult> ReadRequestBodyAsync(InputFormatterContext context, Encoding encoding);
     }
     public class XmlDataContractSerializerOutputFormatter : TextOutputFormatter {
         public XmlDataContractSerializerOutputFormatter();
         public XmlDataContractSerializerOutputFormatter(ILoggerFactory loggerFactory);
         public XmlDataContractSerializerOutputFormatter(XmlWriterSettings writerSettings);
         public XmlDataContractSerializerOutputFormatter(XmlWriterSettings writerSettings, ILoggerFactory loggerFactory);
         public DataContractSerializerSettings SerializerSettings { get; set; }
         public IList<IWrapperProviderFactory> WrapperProviderFactories { get; }
         public XmlWriterSettings WriterSettings { get; }
         protected override bool CanWriteType(Type type);
         protected virtual DataContractSerializer CreateSerializer(Type type);
         public virtual XmlWriter CreateXmlWriter(OutputFormatterWriteContext context, TextWriter writer, XmlWriterSettings xmlWriterSettings);
         public virtual XmlWriter CreateXmlWriter(TextWriter writer, XmlWriterSettings xmlWriterSettings);
         protected virtual DataContractSerializer GetCachedSerializer(Type type);
         protected virtual Type GetSerializableType(Type type);
         public override Task WriteResponseBodyAsync(OutputFormatterWriteContext context, Encoding selectedEncoding);
     }
     public class XmlSerializerInputFormatter : TextInputFormatter, IInputFormatterExceptionPolicy {
-        public XmlSerializerInputFormatter();

         public XmlSerializerInputFormatter(MvcOptions options);
-        public XmlSerializerInputFormatter(bool suppressInputFormatterBuffering);

         public virtual InputFormatterExceptionPolicy ExceptionPolicy { get; }
         public int MaxDepth { get; set; }
         public IList<IWrapperProviderFactory> WrapperProviderFactories { get; }
         public XmlDictionaryReaderQuotas XmlDictionaryReaderQuotas { get; }
         protected override bool CanReadType(Type type);
         protected virtual XmlSerializer CreateSerializer(Type type);
         protected virtual XmlReader CreateXmlReader(Stream readStream, Encoding encoding);
         protected virtual XmlSerializer GetCachedSerializer(Type type);
         protected virtual Type GetSerializableType(Type declaredType);
         public override Task<InputFormatterResult> ReadRequestBodyAsync(InputFormatterContext context, Encoding encoding);
     }
     public class XmlSerializerOutputFormatter : TextOutputFormatter {
         public XmlSerializerOutputFormatter();
         public XmlSerializerOutputFormatter(ILoggerFactory loggerFactory);
         public XmlSerializerOutputFormatter(XmlWriterSettings writerSettings);
         public XmlSerializerOutputFormatter(XmlWriterSettings writerSettings, ILoggerFactory loggerFactory);
         public IList<IWrapperProviderFactory> WrapperProviderFactories { get; }
         public XmlWriterSettings WriterSettings { get; }
         protected override bool CanWriteType(Type type);
         protected virtual XmlSerializer CreateSerializer(Type type);
         public virtual XmlWriter CreateXmlWriter(OutputFormatterWriteContext context, TextWriter writer, XmlWriterSettings xmlWriterSettings);
         public virtual XmlWriter CreateXmlWriter(TextWriter writer, XmlWriterSettings xmlWriterSettings);
         protected virtual XmlSerializer GetCachedSerializer(Type type);
         protected virtual Type GetSerializableType(Type type);
         protected virtual void Serialize(XmlSerializer xmlSerializer, XmlWriter xmlWriter, object value);
         public override Task WriteResponseBodyAsync(OutputFormatterWriteContext context, Encoding selectedEncoding);
     }
 }
```

