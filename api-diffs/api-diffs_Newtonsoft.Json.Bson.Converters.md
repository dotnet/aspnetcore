# Newtonsoft.Json.Bson.Converters

``` diff
-namespace Newtonsoft.Json.Bson.Converters {
 {
-    public class BsonDataObjectIdConverter : JsonConverter {
 {
-        public BsonDataObjectIdConverter();

-        public override bool CanConvert(Type objectType);

-        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer);

-        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer);

-    }
-    public class BsonDataRegexConverter : JsonConverter {
 {
-        public BsonDataRegexConverter();

-        public override bool CanConvert(Type objectType);

-        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer);

-        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer);

-    }
-}
```

