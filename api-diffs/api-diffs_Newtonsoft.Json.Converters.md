# Newtonsoft.Json.Converters

``` diff
-namespace Newtonsoft.Json.Converters {
 {
-    public class BinaryConverter : JsonConverter {
 {
-        public BinaryConverter();

-        public override bool CanConvert(Type objectType);

-        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer);

-        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer);

-    }
-    public class BsonObjectIdConverter : JsonConverter {
 {
-        public BsonObjectIdConverter();

-        public override bool CanConvert(Type objectType);

-        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer);

-        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer);

-    }
-    public abstract class CustomCreationConverter<T> : JsonConverter {
 {
-        protected CustomCreationConverter();

-        public override bool CanWrite { get; }

-        public override bool CanConvert(Type objectType);

-        public abstract T Create(Type objectType);

-        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer);

-        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer);

-    }
-    public class DataSetConverter : JsonConverter {
 {
-        public DataSetConverter();

-        public override bool CanConvert(Type valueType);

-        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer);

-        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer);

-    }
-    public class DataTableConverter : JsonConverter {
 {
-        public DataTableConverter();

-        public override bool CanConvert(Type valueType);

-        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer);

-        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer);

-    }
-    public abstract class DateTimeConverterBase : JsonConverter {
 {
-        protected DateTimeConverterBase();

-        public override bool CanConvert(Type objectType);

-    }
-    public class DiscriminatedUnionConverter : JsonConverter {
 {
-        public DiscriminatedUnionConverter();

-        public override bool CanConvert(Type objectType);

-        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer);

-        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer);

-    }
-    public class EntityKeyMemberConverter : JsonConverter {
 {
-        public EntityKeyMemberConverter();

-        public override bool CanConvert(Type objectType);

-        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer);

-        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer);

-    }
-    public class ExpandoObjectConverter : JsonConverter {
 {
-        public ExpandoObjectConverter();

-        public override bool CanWrite { get; }

-        public override bool CanConvert(Type objectType);

-        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer);

-        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer);

-    }
-    public class IsoDateTimeConverter : DateTimeConverterBase {
 {
-        public IsoDateTimeConverter();

-        public CultureInfo Culture { get; set; }

-        public string DateTimeFormat { get; set; }

-        public DateTimeStyles DateTimeStyles { get; set; }

-        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer);

-        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer);

-    }
-    public class JavaScriptDateTimeConverter : DateTimeConverterBase {
 {
-        public JavaScriptDateTimeConverter();

-        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer);

-        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer);

-    }
-    public class KeyValuePairConverter : JsonConverter {
 {
-        public KeyValuePairConverter();

-        public override bool CanConvert(Type objectType);

-        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer);

-        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer);

-    }
-    public class RegexConverter : JsonConverter {
 {
-        public RegexConverter();

-        public override bool CanConvert(Type objectType);

-        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer);

-        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer);

-    }
-    public class StringEnumConverter : JsonConverter {
 {
-        public StringEnumConverter();

-        public StringEnumConverter(bool camelCaseText);

-        public bool AllowIntegerValues { get; set; }

-        public bool CamelCaseText { get; set; }

-        public override bool CanConvert(Type objectType);

-        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer);

-        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer);

-    }
-    public class UnixDateTimeConverter : DateTimeConverterBase {
 {
-        public UnixDateTimeConverter();

-        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer);

-        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer);

-    }
-    public class VersionConverter : JsonConverter {
 {
-        public VersionConverter();

-        public override bool CanConvert(Type objectType);

-        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer);

-        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer);

-    }
-    public class XmlNodeConverter : JsonConverter {
 {
-        public XmlNodeConverter();

-        public string DeserializeRootElementName { get; set; }

-        public bool OmitRootObject { get; set; }

-        public bool WriteArrayAttribute { get; set; }

-        public override bool CanConvert(Type valueType);

-        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer);

-        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer);

-    }
-}
```

