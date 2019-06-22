# Newtonsoft.Json

``` diff
-namespace Newtonsoft.Json {
 {
-    public enum ConstructorHandling {
 {
-        AllowNonPublicDefaultConstructor = 1,

-        Default = 0,

-    }
-    public enum DateFormatHandling {
 {
-        IsoDateFormat = 0,

-        MicrosoftDateFormat = 1,

-    }
-    public enum DateParseHandling {
 {
-        DateTime = 1,

-        DateTimeOffset = 2,

-        None = 0,

-    }
-    public enum DateTimeZoneHandling {
 {
-        Local = 0,

-        RoundtripKind = 3,

-        Unspecified = 2,

-        Utc = 1,

-    }
-    public enum DefaultValueHandling {
 {
-        Ignore = 1,

-        IgnoreAndPopulate = 3,

-        Include = 0,

-        Populate = 2,

-    }
-    public enum FloatFormatHandling {
 {
-        DefaultValue = 2,

-        String = 0,

-        Symbol = 1,

-    }
-    public enum FloatParseHandling {
 {
-        Decimal = 1,

-        Double = 0,

-    }
-    public enum Formatting {
 {
-        Indented = 1,

-        None = 0,

-    }
-    public interface IArrayPool<T> {
 {
-        T[] Rent(int minimumLength);

-        void Return(T[] array);

-    }
-    public interface IJsonLineInfo {
 {
-        int LineNumber { get; }

-        int LinePosition { get; }

-        bool HasLineInfo();

-    }
-    public sealed class JsonArrayAttribute : JsonContainerAttribute {
 {
-        public JsonArrayAttribute();

-        public JsonArrayAttribute(bool allowNullItems);

-        public JsonArrayAttribute(string id);

-        public bool AllowNullItems { get; set; }

-    }
-    public sealed class JsonConstructorAttribute : Attribute {
 {
-        public JsonConstructorAttribute();

-    }
-    public abstract class JsonContainerAttribute : Attribute {
 {
-        protected JsonContainerAttribute();

-        protected JsonContainerAttribute(string id);

-        public string Description { get; set; }

-        public string Id { get; set; }

-        public bool IsReference { get; set; }

-        public object[] ItemConverterParameters { get; set; }

-        public Type ItemConverterType { get; set; }

-        public bool ItemIsReference { get; set; }

-        public ReferenceLoopHandling ItemReferenceLoopHandling { get; set; }

-        public TypeNameHandling ItemTypeNameHandling { get; set; }

-        public object[] NamingStrategyParameters { get; set; }

-        public Type NamingStrategyType { get; set; }

-        public string Title { get; set; }

-    }
-    public static class JsonConvert {
 {
-        public static readonly string False;

-        public static readonly string NaN;

-        public static readonly string NegativeInfinity;

-        public static readonly string Null;

-        public static readonly string PositiveInfinity;

-        public static readonly string True;

-        public static readonly string Undefined;

-        public static Func<JsonSerializerSettings> DefaultSettings { get; set; }

-        public static T DeserializeAnonymousType<T>(string value, T anonymousTypeObject);

-        public static T DeserializeAnonymousType<T>(string value, T anonymousTypeObject, JsonSerializerSettings settings);

-        public static object DeserializeObject(string value);

-        public static object DeserializeObject(string value, JsonSerializerSettings settings);

-        public static object DeserializeObject(string value, Type type);

-        public static object DeserializeObject(string value, Type type, params JsonConverter[] converters);

-        public static object DeserializeObject(string value, Type type, JsonSerializerSettings settings);

-        public static T DeserializeObject<T>(string value);

-        public static T DeserializeObject<T>(string value, params JsonConverter[] converters);

-        public static T DeserializeObject<T>(string value, JsonSerializerSettings settings);

-        public static XmlDocument DeserializeXmlNode(string value);

-        public static XmlDocument DeserializeXmlNode(string value, string deserializeRootElementName);

-        public static XmlDocument DeserializeXmlNode(string value, string deserializeRootElementName, bool writeArrayAttribute);

-        public static XDocument DeserializeXNode(string value);

-        public static XDocument DeserializeXNode(string value, string deserializeRootElementName);

-        public static XDocument DeserializeXNode(string value, string deserializeRootElementName, bool writeArrayAttribute);

-        public static void PopulateObject(string value, object target);

-        public static void PopulateObject(string value, object target, JsonSerializerSettings settings);

-        public static string SerializeObject(object value);

-        public static string SerializeObject(object value, Formatting formatting);

-        public static string SerializeObject(object value, Formatting formatting, params JsonConverter[] converters);

-        public static string SerializeObject(object value, Formatting formatting, JsonSerializerSettings settings);

-        public static string SerializeObject(object value, params JsonConverter[] converters);

-        public static string SerializeObject(object value, JsonSerializerSettings settings);

-        public static string SerializeObject(object value, Type type, Formatting formatting, JsonSerializerSettings settings);

-        public static string SerializeObject(object value, Type type, JsonSerializerSettings settings);

-        public static string SerializeXmlNode(XmlNode node);

-        public static string SerializeXmlNode(XmlNode node, Formatting formatting);

-        public static string SerializeXmlNode(XmlNode node, Formatting formatting, bool omitRootObject);

-        public static string SerializeXNode(XObject node);

-        public static string SerializeXNode(XObject node, Formatting formatting);

-        public static string SerializeXNode(XObject node, Formatting formatting, bool omitRootObject);

-        public static string ToString(bool value);

-        public static string ToString(byte value);

-        public static string ToString(char value);

-        public static string ToString(DateTime value);

-        public static string ToString(DateTime value, DateFormatHandling format, DateTimeZoneHandling timeZoneHandling);

-        public static string ToString(DateTimeOffset value);

-        public static string ToString(DateTimeOffset value, DateFormatHandling format);

-        public static string ToString(Decimal value);

-        public static string ToString(double value);

-        public static string ToString(Enum value);

-        public static string ToString(Guid value);

-        public static string ToString(short value);

-        public static string ToString(int value);

-        public static string ToString(long value);

-        public static string ToString(object value);

-        public static string ToString(sbyte value);

-        public static string ToString(float value);

-        public static string ToString(string value);

-        public static string ToString(string value, char delimiter);

-        public static string ToString(string value, char delimiter, StringEscapeHandling stringEscapeHandling);

-        public static string ToString(TimeSpan value);

-        public static string ToString(ushort value);

-        public static string ToString(uint value);

-        public static string ToString(ulong value);

-        public static string ToString(Uri value);

-    }
-    public abstract class JsonConverter {
 {
-        protected JsonConverter();

-        public virtual bool CanRead { get; }

-        public virtual bool CanWrite { get; }

-        public abstract bool CanConvert(Type objectType);

-        public abstract object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer);

-        public abstract void WriteJson(JsonWriter writer, object value, JsonSerializer serializer);

-    }
-    public abstract class JsonConverter<T> : JsonConverter {
 {
-        protected JsonConverter();

-        public sealed override bool CanConvert(Type objectType);

-        public sealed override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer);

-        public abstract T ReadJson(JsonReader reader, Type objectType, T existingValue, bool hasExistingValue, JsonSerializer serializer);

-        public sealed override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer);

-        public abstract void WriteJson(JsonWriter writer, T value, JsonSerializer serializer);

-    }
-    public sealed class JsonConverterAttribute : Attribute {
 {
-        public JsonConverterAttribute(Type converterType);

-        public JsonConverterAttribute(Type converterType, params object[] converterParameters);

-        public object[] ConverterParameters { get; }

-        public Type ConverterType { get; }

-    }
-    public class JsonConverterCollection : Collection<JsonConverter> {
 {
-        public JsonConverterCollection();

-    }
-    public sealed class JsonDictionaryAttribute : JsonContainerAttribute {
 {
-        public JsonDictionaryAttribute();

-        public JsonDictionaryAttribute(string id);

-    }
-    public class JsonException : Exception {
 {
-        public JsonException();

-        public JsonException(SerializationInfo info, StreamingContext context);

-        public JsonException(string message);

-        public JsonException(string message, Exception innerException);

-    }
-    public class JsonExtensionDataAttribute : Attribute {
 {
-        public JsonExtensionDataAttribute();

-        public bool ReadData { get; set; }

-        public bool WriteData { get; set; }

-    }
-    public sealed class JsonIgnoreAttribute : Attribute {
 {
-        public JsonIgnoreAttribute();

-    }
-    public sealed class JsonObjectAttribute : JsonContainerAttribute {
 {
-        public JsonObjectAttribute();

-        public JsonObjectAttribute(MemberSerialization memberSerialization);

-        public JsonObjectAttribute(string id);

-        public NullValueHandling ItemNullValueHandling { get; set; }

-        public Required ItemRequired { get; set; }

-        public MemberSerialization MemberSerialization { get; set; }

-    }
-    public sealed class JsonPropertyAttribute : Attribute {
 {
-        public JsonPropertyAttribute();

-        public JsonPropertyAttribute(string propertyName);

-        public DefaultValueHandling DefaultValueHandling { get; set; }

-        public bool IsReference { get; set; }

-        public object[] ItemConverterParameters { get; set; }

-        public Type ItemConverterType { get; set; }

-        public bool ItemIsReference { get; set; }

-        public ReferenceLoopHandling ItemReferenceLoopHandling { get; set; }

-        public TypeNameHandling ItemTypeNameHandling { get; set; }

-        public object[] NamingStrategyParameters { get; set; }

-        public Type NamingStrategyType { get; set; }

-        public NullValueHandling NullValueHandling { get; set; }

-        public ObjectCreationHandling ObjectCreationHandling { get; set; }

-        public int Order { get; set; }

-        public string PropertyName { get; set; }

-        public ReferenceLoopHandling ReferenceLoopHandling { get; set; }

-        public Required Required { get; set; }

-        public TypeNameHandling TypeNameHandling { get; set; }

-    }
-    public abstract class JsonReader : IDisposable {
 {
-        protected JsonReader();

-        public bool CloseInput { get; set; }

-        public CultureInfo Culture { get; set; }

-        protected JsonReader.State CurrentState { get; }

-        public string DateFormatString { get; set; }

-        public DateParseHandling DateParseHandling { get; set; }

-        public DateTimeZoneHandling DateTimeZoneHandling { get; set; }

-        public virtual int Depth { get; }

-        public FloatParseHandling FloatParseHandling { get; set; }

-        public Nullable<int> MaxDepth { get; set; }

-        public virtual string Path { get; }

-        public virtual char QuoteChar { get; protected internal set; }

-        public bool SupportMultipleContent { get; set; }

-        public virtual JsonToken TokenType { get; }

-        public virtual object Value { get; }

-        public virtual Type ValueType { get; }

-        public virtual void Close();

-        protected virtual void Dispose(bool disposing);

-        public abstract bool Read();

-        public virtual Nullable<bool> ReadAsBoolean();

-        public virtual Task<Nullable<bool>> ReadAsBooleanAsync(CancellationToken cancellationToken = default(CancellationToken));

-        public virtual byte[] ReadAsBytes();

-        public virtual Task<byte[]> ReadAsBytesAsync(CancellationToken cancellationToken = default(CancellationToken));

-        public virtual Nullable<DateTime> ReadAsDateTime();

-        public virtual Task<Nullable<DateTime>> ReadAsDateTimeAsync(CancellationToken cancellationToken = default(CancellationToken));

-        public virtual Nullable<DateTimeOffset> ReadAsDateTimeOffset();

-        public virtual Task<Nullable<DateTimeOffset>> ReadAsDateTimeOffsetAsync(CancellationToken cancellationToken = default(CancellationToken));

-        public virtual Nullable<Decimal> ReadAsDecimal();

-        public virtual Task<Nullable<Decimal>> ReadAsDecimalAsync(CancellationToken cancellationToken = default(CancellationToken));

-        public virtual Nullable<double> ReadAsDouble();

-        public virtual Task<Nullable<double>> ReadAsDoubleAsync(CancellationToken cancellationToken = default(CancellationToken));

-        public virtual Nullable<int> ReadAsInt32();

-        public virtual Task<Nullable<int>> ReadAsInt32Async(CancellationToken cancellationToken = default(CancellationToken));

-        public virtual string ReadAsString();

-        public virtual Task<string> ReadAsStringAsync(CancellationToken cancellationToken = default(CancellationToken));

-        public virtual Task<bool> ReadAsync(CancellationToken cancellationToken = default(CancellationToken));

-        protected void SetStateBasedOnCurrent();

-        protected void SetToken(JsonToken newToken);

-        protected void SetToken(JsonToken newToken, object value);

-        protected void SetToken(JsonToken newToken, object value, bool updateIndex);

-        public void Skip();

-        public Task SkipAsync(CancellationToken cancellationToken = default(CancellationToken));

-        void System.IDisposable.Dispose();

-        protected internal enum State {
 {
-            Array = 6,

-            ArrayStart = 5,

-            Closed = 7,

-            Complete = 1,

-            Constructor = 10,

-            ConstructorStart = 9,

-            Error = 11,

-            Finished = 12,

-            Object = 4,

-            ObjectStart = 3,

-            PostValue = 8,

-            Property = 2,

-            Start = 0,

-        }
-    }
-    public class JsonReaderException : JsonException {
 {
-        public JsonReaderException();

-        public JsonReaderException(SerializationInfo info, StreamingContext context);

-        public JsonReaderException(string message);

-        public JsonReaderException(string message, Exception innerException);

-        public JsonReaderException(string message, string path, int lineNumber, int linePosition, Exception innerException);

-        public int LineNumber { get; }

-        public int LinePosition { get; }

-        public string Path { get; }

-    }
-    public sealed class JsonRequiredAttribute : Attribute {
 {
-        public JsonRequiredAttribute();

-    }
-    public class JsonSerializationException : JsonException {
 {
-        public JsonSerializationException();

-        public JsonSerializationException(SerializationInfo info, StreamingContext context);

-        public JsonSerializationException(string message);

-        public JsonSerializationException(string message, Exception innerException);

-    }
-    public class JsonSerializer {
 {
-        public JsonSerializer();

-        public virtual SerializationBinder Binder { get; set; }

-        public virtual bool CheckAdditionalContent { get; set; }

-        public virtual ConstructorHandling ConstructorHandling { get; set; }

-        public virtual StreamingContext Context { get; set; }

-        public virtual IContractResolver ContractResolver { get; set; }

-        public virtual JsonConverterCollection Converters { get; }

-        public virtual CultureInfo Culture { get; set; }

-        public virtual DateFormatHandling DateFormatHandling { get; set; }

-        public virtual string DateFormatString { get; set; }

-        public virtual DateParseHandling DateParseHandling { get; set; }

-        public virtual DateTimeZoneHandling DateTimeZoneHandling { get; set; }

-        public virtual DefaultValueHandling DefaultValueHandling { get; set; }

-        public virtual IEqualityComparer EqualityComparer { get; set; }

-        public virtual FloatFormatHandling FloatFormatHandling { get; set; }

-        public virtual FloatParseHandling FloatParseHandling { get; set; }

-        public virtual Formatting Formatting { get; set; }

-        public virtual Nullable<int> MaxDepth { get; set; }

-        public virtual MetadataPropertyHandling MetadataPropertyHandling { get; set; }

-        public virtual MissingMemberHandling MissingMemberHandling { get; set; }

-        public virtual NullValueHandling NullValueHandling { get; set; }

-        public virtual ObjectCreationHandling ObjectCreationHandling { get; set; }

-        public virtual PreserveReferencesHandling PreserveReferencesHandling { get; set; }

-        public virtual ReferenceLoopHandling ReferenceLoopHandling { get; set; }

-        public virtual IReferenceResolver ReferenceResolver { get; set; }

-        public virtual ISerializationBinder SerializationBinder { get; set; }

-        public virtual StringEscapeHandling StringEscapeHandling { get; set; }

-        public virtual ITraceWriter TraceWriter { get; set; }

-        public virtual FormatterAssemblyStyle TypeNameAssemblyFormat { get; set; }

-        public virtual TypeNameAssemblyFormatHandling TypeNameAssemblyFormatHandling { get; set; }

-        public virtual TypeNameHandling TypeNameHandling { get; set; }

-        public virtual event EventHandler<ErrorEventArgs> Error;

-        public static JsonSerializer Create();

-        public static JsonSerializer Create(JsonSerializerSettings settings);

-        public static JsonSerializer CreateDefault();

-        public static JsonSerializer CreateDefault(JsonSerializerSettings settings);

-        public object Deserialize(JsonReader reader);

-        public object Deserialize(JsonReader reader, Type objectType);

-        public object Deserialize(TextReader reader, Type objectType);

-        public T Deserialize<T>(JsonReader reader);

-        public void Populate(JsonReader reader, object target);

-        public void Populate(TextReader reader, object target);

-        public void Serialize(JsonWriter jsonWriter, object value);

-        public void Serialize(JsonWriter jsonWriter, object value, Type objectType);

-        public void Serialize(TextWriter textWriter, object value);

-        public void Serialize(TextWriter textWriter, object value, Type objectType);

-    }
-    public class JsonSerializerSettings {
 {
-        public JsonSerializerSettings();

-        public SerializationBinder Binder { get; set; }

-        public bool CheckAdditionalContent { get; set; }

-        public ConstructorHandling ConstructorHandling { get; set; }

-        public StreamingContext Context { get; set; }

-        public IContractResolver ContractResolver { get; set; }

-        public IList<JsonConverter> Converters { get; set; }

-        public CultureInfo Culture { get; set; }

-        public DateFormatHandling DateFormatHandling { get; set; }

-        public string DateFormatString { get; set; }

-        public DateParseHandling DateParseHandling { get; set; }

-        public DateTimeZoneHandling DateTimeZoneHandling { get; set; }

-        public DefaultValueHandling DefaultValueHandling { get; set; }

-        public IEqualityComparer EqualityComparer { get; set; }

-        public EventHandler<ErrorEventArgs> Error { get; set; }

-        public FloatFormatHandling FloatFormatHandling { get; set; }

-        public FloatParseHandling FloatParseHandling { get; set; }

-        public Formatting Formatting { get; set; }

-        public Nullable<int> MaxDepth { get; set; }

-        public MetadataPropertyHandling MetadataPropertyHandling { get; set; }

-        public MissingMemberHandling MissingMemberHandling { get; set; }

-        public NullValueHandling NullValueHandling { get; set; }

-        public ObjectCreationHandling ObjectCreationHandling { get; set; }

-        public PreserveReferencesHandling PreserveReferencesHandling { get; set; }

-        public ReferenceLoopHandling ReferenceLoopHandling { get; set; }

-        public IReferenceResolver ReferenceResolver { get; set; }

-        public Func<IReferenceResolver> ReferenceResolverProvider { get; set; }

-        public ISerializationBinder SerializationBinder { get; set; }

-        public StringEscapeHandling StringEscapeHandling { get; set; }

-        public ITraceWriter TraceWriter { get; set; }

-        public FormatterAssemblyStyle TypeNameAssemblyFormat { get; set; }

-        public TypeNameAssemblyFormatHandling TypeNameAssemblyFormatHandling { get; set; }

-        public TypeNameHandling TypeNameHandling { get; set; }

-    }
-    public class JsonTextReader : JsonReader, IJsonLineInfo {
 {
-        public JsonTextReader(TextReader reader);

-        public IArrayPool<char> ArrayPool { get; set; }

-        public int LineNumber { get; }

-        public int LinePosition { get; }

-        public override void Close();

-        public bool HasLineInfo();

-        public override bool Read();

-        public override Nullable<bool> ReadAsBoolean();

-        public override Task<Nullable<bool>> ReadAsBooleanAsync(CancellationToken cancellationToken = default(CancellationToken));

-        public override byte[] ReadAsBytes();

-        public override Task<byte[]> ReadAsBytesAsync(CancellationToken cancellationToken = default(CancellationToken));

-        public override Nullable<DateTime> ReadAsDateTime();

-        public override Task<Nullable<DateTime>> ReadAsDateTimeAsync(CancellationToken cancellationToken = default(CancellationToken));

-        public override Nullable<DateTimeOffset> ReadAsDateTimeOffset();

-        public override Task<Nullable<DateTimeOffset>> ReadAsDateTimeOffsetAsync(CancellationToken cancellationToken = default(CancellationToken));

-        public override Nullable<Decimal> ReadAsDecimal();

-        public override Task<Nullable<Decimal>> ReadAsDecimalAsync(CancellationToken cancellationToken = default(CancellationToken));

-        public override Nullable<double> ReadAsDouble();

-        public override Task<Nullable<double>> ReadAsDoubleAsync(CancellationToken cancellationToken = default(CancellationToken));

-        public override Nullable<int> ReadAsInt32();

-        public override Task<Nullable<int>> ReadAsInt32Async(CancellationToken cancellationToken = default(CancellationToken));

-        public override string ReadAsString();

-        public override Task<string> ReadAsStringAsync(CancellationToken cancellationToken = default(CancellationToken));

-        public override Task<bool> ReadAsync(CancellationToken cancellationToken = default(CancellationToken));

-    }
-    public class JsonTextWriter : JsonWriter {
 {
-        public JsonTextWriter(TextWriter textWriter);

-        public IArrayPool<char> ArrayPool { get; set; }

-        public int Indentation { get; set; }

-        public char IndentChar { get; set; }

-        public char QuoteChar { get; set; }

-        public bool QuoteName { get; set; }

-        public override void Close();

-        public override Task CloseAsync(CancellationToken cancellationToken = default(CancellationToken));

-        public override void Flush();

-        public override Task FlushAsync(CancellationToken cancellationToken = default(CancellationToken));

-        public override void WriteComment(string text);

-        public override Task WriteCommentAsync(string text, CancellationToken cancellationToken = default(CancellationToken));

-        protected override void WriteEnd(JsonToken token);

-        public override Task WriteEndArrayAsync(CancellationToken cancellationToken = default(CancellationToken));

-        protected override Task WriteEndAsync(JsonToken token, CancellationToken cancellationToken);

-        public override Task WriteEndAsync(CancellationToken cancellationToken = default(CancellationToken));

-        public override Task WriteEndConstructorAsync(CancellationToken cancellationToken = default(CancellationToken));

-        public override Task WriteEndObjectAsync(CancellationToken cancellationToken = default(CancellationToken));

-        protected override void WriteIndent();

-        protected override Task WriteIndentAsync(CancellationToken cancellationToken);

-        protected override void WriteIndentSpace();

-        protected override Task WriteIndentSpaceAsync(CancellationToken cancellationToken);

-        public override void WriteNull();

-        public override Task WriteNullAsync(CancellationToken cancellationToken = default(CancellationToken));

-        public override void WritePropertyName(string name);

-        public override void WritePropertyName(string name, bool escape);

-        public override Task WritePropertyNameAsync(string name, bool escape, CancellationToken cancellationToken = default(CancellationToken));

-        public override Task WritePropertyNameAsync(string name, CancellationToken cancellationToken = default(CancellationToken));

-        public override void WriteRaw(string json);

-        public override Task WriteRawAsync(string json, CancellationToken cancellationToken = default(CancellationToken));

-        public override Task WriteRawValueAsync(string json, CancellationToken cancellationToken = default(CancellationToken));

-        public override void WriteStartArray();

-        public override Task WriteStartArrayAsync(CancellationToken cancellationToken = default(CancellationToken));

-        public override void WriteStartConstructor(string name);

-        public override Task WriteStartConstructorAsync(string name, CancellationToken cancellationToken = default(CancellationToken));

-        public override void WriteStartObject();

-        public override Task WriteStartObjectAsync(CancellationToken cancellationToken = default(CancellationToken));

-        public override void WriteUndefined();

-        public override Task WriteUndefinedAsync(CancellationToken cancellationToken = default(CancellationToken));

-        public override void WriteValue(bool value);

-        public override void WriteValue(byte value);

-        public override void WriteValue(byte[] value);

-        public override void WriteValue(char value);

-        public override void WriteValue(DateTime value);

-        public override void WriteValue(DateTimeOffset value);

-        public override void WriteValue(Decimal value);

-        public override void WriteValue(double value);

-        public override void WriteValue(Guid value);

-        public override void WriteValue(short value);

-        public override void WriteValue(int value);

-        public override void WriteValue(long value);

-        public override void WriteValue(Nullable<double> value);

-        public override void WriteValue(Nullable<float> value);

-        public override void WriteValue(object value);

-        public override void WriteValue(sbyte value);

-        public override void WriteValue(float value);

-        public override void WriteValue(string value);

-        public override void WriteValue(TimeSpan value);

-        public override void WriteValue(ushort value);

-        public override void WriteValue(uint value);

-        public override void WriteValue(ulong value);

-        public override void WriteValue(Uri value);

-        public override Task WriteValueAsync(bool value, CancellationToken cancellationToken = default(CancellationToken));

-        public override Task WriteValueAsync(byte value, CancellationToken cancellationToken = default(CancellationToken));

-        public override Task WriteValueAsync(byte[] value, CancellationToken cancellationToken = default(CancellationToken));

-        public override Task WriteValueAsync(char value, CancellationToken cancellationToken = default(CancellationToken));

-        public override Task WriteValueAsync(DateTime value, CancellationToken cancellationToken = default(CancellationToken));

-        public override Task WriteValueAsync(DateTimeOffset value, CancellationToken cancellationToken = default(CancellationToken));

-        public override Task WriteValueAsync(Decimal value, CancellationToken cancellationToken = default(CancellationToken));

-        public override Task WriteValueAsync(double value, CancellationToken cancellationToken = default(CancellationToken));

-        public override Task WriteValueAsync(Guid value, CancellationToken cancellationToken = default(CancellationToken));

-        public override Task WriteValueAsync(short value, CancellationToken cancellationToken = default(CancellationToken));

-        public override Task WriteValueAsync(int value, CancellationToken cancellationToken = default(CancellationToken));

-        public override Task WriteValueAsync(long value, CancellationToken cancellationToken = default(CancellationToken));

-        public override Task WriteValueAsync(Nullable<bool> value, CancellationToken cancellationToken = default(CancellationToken));

-        public override Task WriteValueAsync(Nullable<byte> value, CancellationToken cancellationToken = default(CancellationToken));

-        public override Task WriteValueAsync(Nullable<char> value, CancellationToken cancellationToken = default(CancellationToken));

-        public override Task WriteValueAsync(Nullable<DateTime> value, CancellationToken cancellationToken = default(CancellationToken));

-        public override Task WriteValueAsync(Nullable<DateTimeOffset> value, CancellationToken cancellationToken = default(CancellationToken));

-        public override Task WriteValueAsync(Nullable<Decimal> value, CancellationToken cancellationToken = default(CancellationToken));

-        public override Task WriteValueAsync(Nullable<double> value, CancellationToken cancellationToken = default(CancellationToken));

-        public override Task WriteValueAsync(Nullable<Guid> value, CancellationToken cancellationToken = default(CancellationToken));

-        public override Task WriteValueAsync(Nullable<short> value, CancellationToken cancellationToken = default(CancellationToken));

-        public override Task WriteValueAsync(Nullable<int> value, CancellationToken cancellationToken = default(CancellationToken));

-        public override Task WriteValueAsync(Nullable<long> value, CancellationToken cancellationToken = default(CancellationToken));

-        public override Task WriteValueAsync(Nullable<sbyte> value, CancellationToken cancellationToken = default(CancellationToken));

-        public override Task WriteValueAsync(Nullable<float> value, CancellationToken cancellationToken = default(CancellationToken));

-        public override Task WriteValueAsync(Nullable<TimeSpan> value, CancellationToken cancellationToken = default(CancellationToken));

-        public override Task WriteValueAsync(Nullable<ushort> value, CancellationToken cancellationToken = default(CancellationToken));

-        public override Task WriteValueAsync(Nullable<uint> value, CancellationToken cancellationToken = default(CancellationToken));

-        public override Task WriteValueAsync(Nullable<ulong> value, CancellationToken cancellationToken = default(CancellationToken));

-        public override Task WriteValueAsync(object value, CancellationToken cancellationToken = default(CancellationToken));

-        public override Task WriteValueAsync(sbyte value, CancellationToken cancellationToken = default(CancellationToken));

-        public override Task WriteValueAsync(float value, CancellationToken cancellationToken = default(CancellationToken));

-        public override Task WriteValueAsync(string value, CancellationToken cancellationToken = default(CancellationToken));

-        public override Task WriteValueAsync(TimeSpan value, CancellationToken cancellationToken = default(CancellationToken));

-        public override Task WriteValueAsync(ushort value, CancellationToken cancellationToken = default(CancellationToken));

-        public override Task WriteValueAsync(uint value, CancellationToken cancellationToken = default(CancellationToken));

-        public override Task WriteValueAsync(ulong value, CancellationToken cancellationToken = default(CancellationToken));

-        public override Task WriteValueAsync(Uri value, CancellationToken cancellationToken = default(CancellationToken));

-        protected override void WriteValueDelimiter();

-        protected override Task WriteValueDelimiterAsync(CancellationToken cancellationToken);

-        public override void WriteWhitespace(string ws);

-        public override Task WriteWhitespaceAsync(string ws, CancellationToken cancellationToken = default(CancellationToken));

-    }
-    public enum JsonToken {
 {
-        Boolean = 10,

-        Bytes = 17,

-        Comment = 5,

-        Date = 16,

-        EndArray = 14,

-        EndConstructor = 15,

-        EndObject = 13,

-        Float = 8,

-        Integer = 7,

-        None = 0,

-        Null = 11,

-        PropertyName = 4,

-        Raw = 6,

-        StartArray = 2,

-        StartConstructor = 3,

-        StartObject = 1,

-        String = 9,

-        Undefined = 12,

-    }
-    public class JsonValidatingReader : JsonReader, IJsonLineInfo {
 {
-        public JsonValidatingReader(JsonReader reader);

-        public override int Depth { get; }

-        int Newtonsoft.Json.IJsonLineInfo.LineNumber { get; }

-        int Newtonsoft.Json.IJsonLineInfo.LinePosition { get; }

-        public override string Path { get; }

-        public override char QuoteChar { get; protected internal set; }

-        public JsonReader Reader { get; }

-        public JsonSchema Schema { get; set; }

-        public override JsonToken TokenType { get; }

-        public override object Value { get; }

-        public override Type ValueType { get; }

-        public event ValidationEventHandler ValidationEventHandler;

-        public override void Close();

-        bool Newtonsoft.Json.IJsonLineInfo.HasLineInfo();

-        public override bool Read();

-        public override Nullable<bool> ReadAsBoolean();

-        public override byte[] ReadAsBytes();

-        public override Nullable<DateTime> ReadAsDateTime();

-        public override Nullable<DateTimeOffset> ReadAsDateTimeOffset();

-        public override Nullable<Decimal> ReadAsDecimal();

-        public override Nullable<double> ReadAsDouble();

-        public override Nullable<int> ReadAsInt32();

-        public override string ReadAsString();

-    }
-    public abstract class JsonWriter : IDisposable {
 {
-        protected JsonWriter();

-        public bool AutoCompleteOnClose { get; set; }

-        public bool CloseOutput { get; set; }

-        public CultureInfo Culture { get; set; }

-        public DateFormatHandling DateFormatHandling { get; set; }

-        public string DateFormatString { get; set; }

-        public DateTimeZoneHandling DateTimeZoneHandling { get; set; }

-        public FloatFormatHandling FloatFormatHandling { get; set; }

-        public Formatting Formatting { get; set; }

-        public string Path { get; }

-        public StringEscapeHandling StringEscapeHandling { get; set; }

-        protected internal int Top { get; }

-        public WriteState WriteState { get; }

-        public virtual void Close();

-        public virtual Task CloseAsync(CancellationToken cancellationToken = default(CancellationToken));

-        protected virtual void Dispose(bool disposing);

-        public abstract void Flush();

-        public virtual Task FlushAsync(CancellationToken cancellationToken = default(CancellationToken));

-        protected void SetWriteState(JsonToken token, object value);

-        protected Task SetWriteStateAsync(JsonToken token, object value, CancellationToken cancellationToken);

-        void System.IDisposable.Dispose();

-        public virtual void WriteComment(string text);

-        public virtual Task WriteCommentAsync(string text, CancellationToken cancellationToken = default(CancellationToken));

-        public virtual void WriteEnd();

-        protected virtual void WriteEnd(JsonToken token);

-        public virtual void WriteEndArray();

-        public virtual Task WriteEndArrayAsync(CancellationToken cancellationToken = default(CancellationToken));

-        protected virtual Task WriteEndAsync(JsonToken token, CancellationToken cancellationToken);

-        public virtual Task WriteEndAsync(CancellationToken cancellationToken = default(CancellationToken));

-        public virtual void WriteEndConstructor();

-        public virtual Task WriteEndConstructorAsync(CancellationToken cancellationToken = default(CancellationToken));

-        public virtual void WriteEndObject();

-        public virtual Task WriteEndObjectAsync(CancellationToken cancellationToken = default(CancellationToken));

-        protected virtual void WriteIndent();

-        protected virtual Task WriteIndentAsync(CancellationToken cancellationToken);

-        protected virtual void WriteIndentSpace();

-        protected virtual Task WriteIndentSpaceAsync(CancellationToken cancellationToken);

-        public virtual void WriteNull();

-        public virtual Task WriteNullAsync(CancellationToken cancellationToken = default(CancellationToken));

-        public virtual void WritePropertyName(string name);

-        public virtual void WritePropertyName(string name, bool escape);

-        public virtual Task WritePropertyNameAsync(string name, bool escape, CancellationToken cancellationToken = default(CancellationToken));

-        public virtual Task WritePropertyNameAsync(string name, CancellationToken cancellationToken = default(CancellationToken));

-        public virtual void WriteRaw(string json);

-        public virtual Task WriteRawAsync(string json, CancellationToken cancellationToken = default(CancellationToken));

-        public virtual void WriteRawValue(string json);

-        public virtual Task WriteRawValueAsync(string json, CancellationToken cancellationToken = default(CancellationToken));

-        public virtual void WriteStartArray();

-        public virtual Task WriteStartArrayAsync(CancellationToken cancellationToken = default(CancellationToken));

-        public virtual void WriteStartConstructor(string name);

-        public virtual Task WriteStartConstructorAsync(string name, CancellationToken cancellationToken = default(CancellationToken));

-        public virtual void WriteStartObject();

-        public virtual Task WriteStartObjectAsync(CancellationToken cancellationToken = default(CancellationToken));

-        public void WriteToken(JsonReader reader);

-        public void WriteToken(JsonReader reader, bool writeChildren);

-        public void WriteToken(JsonToken token);

-        public void WriteToken(JsonToken token, object value);

-        public Task WriteTokenAsync(JsonReader reader, bool writeChildren, CancellationToken cancellationToken = default(CancellationToken));

-        public Task WriteTokenAsync(JsonReader reader, CancellationToken cancellationToken = default(CancellationToken));

-        public Task WriteTokenAsync(JsonToken token, object value, CancellationToken cancellationToken = default(CancellationToken));

-        public Task WriteTokenAsync(JsonToken token, CancellationToken cancellationToken = default(CancellationToken));

-        public virtual void WriteUndefined();

-        public virtual Task WriteUndefinedAsync(CancellationToken cancellationToken = default(CancellationToken));

-        public virtual void WriteValue(bool value);

-        public virtual void WriteValue(byte value);

-        public virtual void WriteValue(byte[] value);

-        public virtual void WriteValue(char value);

-        public virtual void WriteValue(DateTime value);

-        public virtual void WriteValue(DateTimeOffset value);

-        public virtual void WriteValue(Decimal value);

-        public virtual void WriteValue(double value);

-        public virtual void WriteValue(Guid value);

-        public virtual void WriteValue(short value);

-        public virtual void WriteValue(int value);

-        public virtual void WriteValue(long value);

-        public virtual void WriteValue(Nullable<bool> value);

-        public virtual void WriteValue(Nullable<byte> value);

-        public virtual void WriteValue(Nullable<char> value);

-        public virtual void WriteValue(Nullable<DateTime> value);

-        public virtual void WriteValue(Nullable<DateTimeOffset> value);

-        public virtual void WriteValue(Nullable<Decimal> value);

-        public virtual void WriteValue(Nullable<double> value);

-        public virtual void WriteValue(Nullable<Guid> value);

-        public virtual void WriteValue(Nullable<short> value);

-        public virtual void WriteValue(Nullable<int> value);

-        public virtual void WriteValue(Nullable<long> value);

-        public virtual void WriteValue(Nullable<sbyte> value);

-        public virtual void WriteValue(Nullable<float> value);

-        public virtual void WriteValue(Nullable<TimeSpan> value);

-        public virtual void WriteValue(Nullable<ushort> value);

-        public virtual void WriteValue(Nullable<uint> value);

-        public virtual void WriteValue(Nullable<ulong> value);

-        public virtual void WriteValue(object value);

-        public virtual void WriteValue(sbyte value);

-        public virtual void WriteValue(float value);

-        public virtual void WriteValue(string value);

-        public virtual void WriteValue(TimeSpan value);

-        public virtual void WriteValue(ushort value);

-        public virtual void WriteValue(uint value);

-        public virtual void WriteValue(ulong value);

-        public virtual void WriteValue(Uri value);

-        public virtual Task WriteValueAsync(bool value, CancellationToken cancellationToken = default(CancellationToken));

-        public virtual Task WriteValueAsync(byte value, CancellationToken cancellationToken = default(CancellationToken));

-        public virtual Task WriteValueAsync(byte[] value, CancellationToken cancellationToken = default(CancellationToken));

-        public virtual Task WriteValueAsync(char value, CancellationToken cancellationToken = default(CancellationToken));

-        public virtual Task WriteValueAsync(DateTime value, CancellationToken cancellationToken = default(CancellationToken));

-        public virtual Task WriteValueAsync(DateTimeOffset value, CancellationToken cancellationToken = default(CancellationToken));

-        public virtual Task WriteValueAsync(Decimal value, CancellationToken cancellationToken = default(CancellationToken));

-        public virtual Task WriteValueAsync(double value, CancellationToken cancellationToken = default(CancellationToken));

-        public virtual Task WriteValueAsync(Guid value, CancellationToken cancellationToken = default(CancellationToken));

-        public virtual Task WriteValueAsync(short value, CancellationToken cancellationToken = default(CancellationToken));

-        public virtual Task WriteValueAsync(int value, CancellationToken cancellationToken = default(CancellationToken));

-        public virtual Task WriteValueAsync(long value, CancellationToken cancellationToken = default(CancellationToken));

-        public virtual Task WriteValueAsync(Nullable<bool> value, CancellationToken cancellationToken = default(CancellationToken));

-        public virtual Task WriteValueAsync(Nullable<byte> value, CancellationToken cancellationToken = default(CancellationToken));

-        public virtual Task WriteValueAsync(Nullable<char> value, CancellationToken cancellationToken = default(CancellationToken));

-        public virtual Task WriteValueAsync(Nullable<DateTime> value, CancellationToken cancellationToken = default(CancellationToken));

-        public virtual Task WriteValueAsync(Nullable<DateTimeOffset> value, CancellationToken cancellationToken = default(CancellationToken));

-        public virtual Task WriteValueAsync(Nullable<Decimal> value, CancellationToken cancellationToken = default(CancellationToken));

-        public virtual Task WriteValueAsync(Nullable<double> value, CancellationToken cancellationToken = default(CancellationToken));

-        public virtual Task WriteValueAsync(Nullable<Guid> value, CancellationToken cancellationToken = default(CancellationToken));

-        public virtual Task WriteValueAsync(Nullable<short> value, CancellationToken cancellationToken = default(CancellationToken));

-        public virtual Task WriteValueAsync(Nullable<int> value, CancellationToken cancellationToken = default(CancellationToken));

-        public virtual Task WriteValueAsync(Nullable<long> value, CancellationToken cancellationToken = default(CancellationToken));

-        public virtual Task WriteValueAsync(Nullable<sbyte> value, CancellationToken cancellationToken = default(CancellationToken));

-        public virtual Task WriteValueAsync(Nullable<float> value, CancellationToken cancellationToken = default(CancellationToken));

-        public virtual Task WriteValueAsync(Nullable<TimeSpan> value, CancellationToken cancellationToken = default(CancellationToken));

-        public virtual Task WriteValueAsync(Nullable<ushort> value, CancellationToken cancellationToken = default(CancellationToken));

-        public virtual Task WriteValueAsync(Nullable<uint> value, CancellationToken cancellationToken = default(CancellationToken));

-        public virtual Task WriteValueAsync(Nullable<ulong> value, CancellationToken cancellationToken = default(CancellationToken));

-        public virtual Task WriteValueAsync(object value, CancellationToken cancellationToken = default(CancellationToken));

-        public virtual Task WriteValueAsync(sbyte value, CancellationToken cancellationToken = default(CancellationToken));

-        public virtual Task WriteValueAsync(float value, CancellationToken cancellationToken = default(CancellationToken));

-        public virtual Task WriteValueAsync(string value, CancellationToken cancellationToken = default(CancellationToken));

-        public virtual Task WriteValueAsync(TimeSpan value, CancellationToken cancellationToken = default(CancellationToken));

-        public virtual Task WriteValueAsync(ushort value, CancellationToken cancellationToken = default(CancellationToken));

-        public virtual Task WriteValueAsync(uint value, CancellationToken cancellationToken = default(CancellationToken));

-        public virtual Task WriteValueAsync(ulong value, CancellationToken cancellationToken = default(CancellationToken));

-        public virtual Task WriteValueAsync(Uri value, CancellationToken cancellationToken = default(CancellationToken));

-        protected virtual void WriteValueDelimiter();

-        protected virtual Task WriteValueDelimiterAsync(CancellationToken cancellationToken);

-        public virtual void WriteWhitespace(string ws);

-        public virtual Task WriteWhitespaceAsync(string ws, CancellationToken cancellationToken = default(CancellationToken));

-    }
-    public class JsonWriterException : JsonException {
 {
-        public JsonWriterException();

-        public JsonWriterException(SerializationInfo info, StreamingContext context);

-        public JsonWriterException(string message);

-        public JsonWriterException(string message, Exception innerException);

-        public JsonWriterException(string message, string path, Exception innerException);

-        public string Path { get; }

-    }
-    public enum MemberSerialization {
 {
-        Fields = 2,

-        OptIn = 1,

-        OptOut = 0,

-    }
-    public enum MetadataPropertyHandling {
 {
-        Default = 0,

-        Ignore = 2,

-        ReadAhead = 1,

-    }
-    public enum MissingMemberHandling {
 {
-        Error = 1,

-        Ignore = 0,

-    }
-    public enum NullValueHandling {
 {
-        Ignore = 1,

-        Include = 0,

-    }
-    public enum ObjectCreationHandling {
 {
-        Auto = 0,

-        Replace = 2,

-        Reuse = 1,

-    }
-    public enum PreserveReferencesHandling {
 {
-        All = 3,

-        Arrays = 2,

-        None = 0,

-        Objects = 1,

-    }
-    public enum ReferenceLoopHandling {
 {
-        Error = 0,

-        Ignore = 1,

-        Serialize = 2,

-    }
-    public enum Required {
 {
-        AllowNull = 1,

-        Always = 2,

-        Default = 0,

-        DisallowNull = 3,

-    }
-    public enum StringEscapeHandling {
 {
-        Default = 0,

-        EscapeHtml = 2,

-        EscapeNonAscii = 1,

-    }
-    public enum TypeNameAssemblyFormatHandling {
 {
-        Full = 1,

-        Simple = 0,

-    }
-    public enum TypeNameHandling {
 {
-        All = 3,

-        Arrays = 2,

-        Auto = 4,

-        None = 0,

-        Objects = 1,

-    }
-    public enum WriteState {
 {
-        Array = 3,

-        Closed = 1,

-        Constructor = 4,

-        Error = 0,

-        Object = 2,

-        Property = 5,

-        Start = 6,

-    }
-}
```

