# Microsoft.AspNetCore.Mvc.Formatters

``` diff
 namespace Microsoft.AspNetCore.Mvc.Formatters {
     public class FormatFilter : IFilterMetadata, IFormatFilter, IResourceFilter, IResultFilter {
-        public FormatFilter(IOptions<MvcOptions> options);

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
-        public static MediaTypeSegmentWithQuality CreateMediaTypeSegmentWithQuality(string mediaType, int start);

+        public static MediaTypeSegmentWithQuality CreateMediaTypeSegmentWithQuality(string mediaType, int start);
     }
+    public readonly struct MediaTypeSegmentWithQuality {
+        public MediaTypeSegmentWithQuality(StringSegment mediaType, double quality);
+        public StringSegment MediaType { get; }
+        public double Quality { get; }
+        public override string ToString();
+    }
     public abstract class OutputFormatterCanWriteContext {
-        protected OutputFormatterCanWriteContext();

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
     public class XmlDataContractSerializerInputFormatter : TextInputFormatter, IInputFormatterExceptionPolicy {
-        public XmlDataContractSerializerInputFormatter();

-        public XmlDataContractSerializerInputFormatter(bool suppressInputFormatterBuffering);

     }
     public class XmlSerializerInputFormatter : TextInputFormatter, IInputFormatterExceptionPolicy {
-        public XmlSerializerInputFormatter();

-        public XmlSerializerInputFormatter(bool suppressInputFormatterBuffering);

     }
 }
```

