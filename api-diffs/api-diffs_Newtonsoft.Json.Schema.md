# Newtonsoft.Json.Schema

``` diff
-namespace Newtonsoft.Json.Schema {
 {
-    public static class Extensions {
 {
-        public static bool IsValid(this JToken source, JsonSchema schema);

-        public static bool IsValid(this JToken source, JsonSchema schema, out IList<string> errorMessages);

-        public static void Validate(this JToken source, JsonSchema schema);

-        public static void Validate(this JToken source, JsonSchema schema, ValidationEventHandler validationEventHandler);

-    }
-    public class JsonSchema {
 {
-        public JsonSchema();

-        public JsonSchema AdditionalItems { get; set; }

-        public JsonSchema AdditionalProperties { get; set; }

-        public bool AllowAdditionalItems { get; set; }

-        public bool AllowAdditionalProperties { get; set; }

-        public JToken Default { get; set; }

-        public string Description { get; set; }

-        public Nullable<JsonSchemaType> Disallow { get; set; }

-        public Nullable<double> DivisibleBy { get; set; }

-        public IList<JToken> Enum { get; set; }

-        public Nullable<bool> ExclusiveMaximum { get; set; }

-        public Nullable<bool> ExclusiveMinimum { get; set; }

-        public IList<JsonSchema> Extends { get; set; }

-        public string Format { get; set; }

-        public Nullable<bool> Hidden { get; set; }

-        public string Id { get; set; }

-        public IList<JsonSchema> Items { get; set; }

-        public Nullable<double> Maximum { get; set; }

-        public Nullable<int> MaximumItems { get; set; }

-        public Nullable<int> MaximumLength { get; set; }

-        public Nullable<double> Minimum { get; set; }

-        public Nullable<int> MinimumItems { get; set; }

-        public Nullable<int> MinimumLength { get; set; }

-        public string Pattern { get; set; }

-        public IDictionary<string, JsonSchema> PatternProperties { get; set; }

-        public bool PositionalItemsValidation { get; set; }

-        public IDictionary<string, JsonSchema> Properties { get; set; }

-        public Nullable<bool> ReadOnly { get; set; }

-        public Nullable<bool> Required { get; set; }

-        public string Requires { get; set; }

-        public string Title { get; set; }

-        public Nullable<bool> Transient { get; set; }

-        public Nullable<JsonSchemaType> Type { get; set; }

-        public bool UniqueItems { get; set; }

-        public static JsonSchema Parse(string json);

-        public static JsonSchema Parse(string json, JsonSchemaResolver resolver);

-        public static JsonSchema Read(JsonReader reader);

-        public static JsonSchema Read(JsonReader reader, JsonSchemaResolver resolver);

-        public override string ToString();

-        public void WriteTo(JsonWriter writer);

-        public void WriteTo(JsonWriter writer, JsonSchemaResolver resolver);

-    }
-    public class JsonSchemaException : JsonException {
 {
-        public JsonSchemaException();

-        public JsonSchemaException(SerializationInfo info, StreamingContext context);

-        public JsonSchemaException(string message);

-        public JsonSchemaException(string message, Exception innerException);

-        public int LineNumber { get; }

-        public int LinePosition { get; }

-        public string Path { get; }

-    }
-    public class JsonSchemaGenerator {
 {
-        public JsonSchemaGenerator();

-        public IContractResolver ContractResolver { get; set; }

-        public UndefinedSchemaIdHandling UndefinedSchemaIdHandling { get; set; }

-        public JsonSchema Generate(Type type);

-        public JsonSchema Generate(Type type, JsonSchemaResolver resolver);

-        public JsonSchema Generate(Type type, JsonSchemaResolver resolver, bool rootSchemaNullable);

-        public JsonSchema Generate(Type type, bool rootSchemaNullable);

-    }
-    public class JsonSchemaResolver {
 {
-        public JsonSchemaResolver();

-        public IList<JsonSchema> LoadedSchemas { get; protected set; }

-        public virtual JsonSchema GetSchema(string reference);

-    }
-    public enum JsonSchemaType {
 {
-        Any = 127,

-        Array = 32,

-        Boolean = 8,

-        Float = 2,

-        Integer = 4,

-        None = 0,

-        Null = 64,

-        Object = 16,

-        String = 1,

-    }
-    public enum UndefinedSchemaIdHandling {
 {
-        None = 0,

-        UseAssemblyQualifiedName = 2,

-        UseTypeName = 1,

-    }
-    public class ValidationEventArgs : EventArgs {
 {
-        public JsonSchemaException Exception { get; }

-        public string Message { get; }

-        public string Path { get; }

-    }
-    public delegate void ValidationEventHandler(object sender, ValidationEventArgs e);

-}
```

