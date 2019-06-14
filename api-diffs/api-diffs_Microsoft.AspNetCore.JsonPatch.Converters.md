# Microsoft.AspNetCore.JsonPatch.Converters

``` diff
-namespace Microsoft.AspNetCore.JsonPatch.Converters {
 {
-    public class JsonPatchDocumentConverter : JsonConverter {
 {
-        public JsonPatchDocumentConverter();

-        public override bool CanConvert(Type objectType);

-        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer);

-        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer);

-    }
-    public class TypedJsonPatchDocumentConverter : JsonPatchDocumentConverter {
 {
-        public TypedJsonPatchDocumentConverter();

-        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer);

-    }
-}
```

