# Newtonsoft.Json.Serialization

``` diff
-namespace Newtonsoft.Json.Serialization {
 {
-    public class CamelCaseNamingStrategy : NamingStrategy {
 {
-        public CamelCaseNamingStrategy();

-        public CamelCaseNamingStrategy(bool processDictionaryKeys, bool overrideSpecifiedNames);

-        public CamelCaseNamingStrategy(bool processDictionaryKeys, bool overrideSpecifiedNames, bool processExtensionDataNames);

-        protected override string ResolvePropertyName(string name);

-    }
-    public class CamelCasePropertyNamesContractResolver : DefaultContractResolver {
 {
-        public CamelCasePropertyNamesContractResolver();

-        public override JsonContract ResolveContract(Type type);

-    }
-    public class DefaultContractResolver : IContractResolver {
 {
-        public DefaultContractResolver();

-        public BindingFlags DefaultMembersSearchFlags { get; set; }

-        public bool DynamicCodeGeneration { get; }

-        public bool IgnoreIsSpecifiedMembers { get; set; }

-        public bool IgnoreSerializableAttribute { get; set; }

-        public bool IgnoreSerializableInterface { get; set; }

-        public bool IgnoreShouldSerializeMembers { get; set; }

-        public NamingStrategy NamingStrategy { get; set; }

-        public bool SerializeCompilerGeneratedMembers { get; set; }

-        protected virtual JsonArrayContract CreateArrayContract(Type objectType);

-        protected virtual IList<JsonProperty> CreateConstructorParameters(ConstructorInfo constructor, JsonPropertyCollection memberProperties);

-        protected virtual JsonContract CreateContract(Type objectType);

-        protected virtual JsonDictionaryContract CreateDictionaryContract(Type objectType);

-        protected virtual JsonDynamicContract CreateDynamicContract(Type objectType);

-        protected virtual JsonISerializableContract CreateISerializableContract(Type objectType);

-        protected virtual JsonLinqContract CreateLinqContract(Type objectType);

-        protected virtual IValueProvider CreateMemberValueProvider(MemberInfo member);

-        protected virtual JsonObjectContract CreateObjectContract(Type objectType);

-        protected virtual JsonPrimitiveContract CreatePrimitiveContract(Type objectType);

-        protected virtual IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization);

-        protected virtual JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization);

-        protected virtual JsonProperty CreatePropertyFromConstructorParameter(JsonProperty matchingMemberProperty, ParameterInfo parameterInfo);

-        protected virtual JsonStringContract CreateStringContract(Type objectType);

-        public string GetResolvedPropertyName(string propertyName);

-        protected virtual List<MemberInfo> GetSerializableMembers(Type objectType);

-        public virtual JsonContract ResolveContract(Type type);

-        protected virtual JsonConverter ResolveContractConverter(Type objectType);

-        protected virtual string ResolveDictionaryKey(string dictionaryKey);

-        protected virtual string ResolveExtensionDataName(string extensionDataName);

-        protected virtual string ResolvePropertyName(string propertyName);

-    }
-    public class DefaultNamingStrategy : NamingStrategy {
 {
-        public DefaultNamingStrategy();

-        protected override string ResolvePropertyName(string name);

-    }
-    public class DefaultSerializationBinder : SerializationBinder, ISerializationBinder {
 {
-        public DefaultSerializationBinder();

-        public override void BindToName(Type serializedType, out string assemblyName, out string typeName);

-        public override Type BindToType(string assemblyName, string typeName);

-    }
-    public class DiagnosticsTraceWriter : ITraceWriter {
 {
-        public DiagnosticsTraceWriter();

-        public TraceLevel LevelFilter { get; set; }

-        public void Trace(TraceLevel level, string message, Exception ex);

-    }
-    public class ErrorContext {
 {
-        public Exception Error { get; }

-        public bool Handled { get; set; }

-        public object Member { get; }

-        public object OriginalObject { get; }

-        public string Path { get; }

-    }
-    public class ErrorEventArgs : EventArgs {
 {
-        public ErrorEventArgs(object currentObject, ErrorContext errorContext);

-        public object CurrentObject { get; }

-        public ErrorContext ErrorContext { get; }

-    }
-    public class ExpressionValueProvider : IValueProvider {
 {
-        public ExpressionValueProvider(MemberInfo memberInfo);

-        public object GetValue(object target);

-        public void SetValue(object target, object value);

-    }
-    public delegate IEnumerable<KeyValuePair<object, object>> ExtensionDataGetter(object o);

-    public delegate void ExtensionDataSetter(object o, string key, object value);

-    public interface IAttributeProvider {
 {
-        IList<Attribute> GetAttributes(bool inherit);

-        IList<Attribute> GetAttributes(Type attributeType, bool inherit);

-    }
-    public interface IContractResolver {
 {
-        JsonContract ResolveContract(Type type);

-    }
-    public interface IReferenceResolver {
 {
-        void AddReference(object context, string reference, object value);

-        string GetReference(object context, object value);

-        bool IsReferenced(object context, object value);

-        object ResolveReference(object context, string reference);

-    }
-    public interface ISerializationBinder {
 {
-        void BindToName(Type serializedType, out string assemblyName, out string typeName);

-        Type BindToType(string assemblyName, string typeName);

-    }
-    public interface ITraceWriter {
 {
-        TraceLevel LevelFilter { get; }

-        void Trace(TraceLevel level, string message, Exception ex);

-    }
-    public interface IValueProvider {
 {
-        object GetValue(object target);

-        void SetValue(object target, object value);

-    }
-    public class JsonArrayContract : JsonContainerContract {
 {
-        public JsonArrayContract(Type underlyingType);

-        public Type CollectionItemType { get; }

-        public bool HasParameterizedCreator { get; set; }

-        public bool IsMultidimensionalArray { get; }

-        public ObjectConstructor<object> OverrideCreator { get; set; }

-    }
-    public class JsonContainerContract : JsonContract {
 {
-        public JsonConverter ItemConverter { get; set; }

-        public Nullable<bool> ItemIsReference { get; set; }

-        public Nullable<ReferenceLoopHandling> ItemReferenceLoopHandling { get; set; }

-        public Nullable<TypeNameHandling> ItemTypeNameHandling { get; set; }

-    }
-    public abstract class JsonContract {
 {
-        public JsonConverter Converter { get; set; }

-        public Type CreatedType { get; set; }

-        public Func<object> DefaultCreator { get; set; }

-        public bool DefaultCreatorNonPublic { get; set; }

-        public Nullable<bool> IsReference { get; set; }

-        public IList<SerializationCallback> OnDeserializedCallbacks { get; }

-        public IList<SerializationCallback> OnDeserializingCallbacks { get; }

-        public IList<SerializationErrorCallback> OnErrorCallbacks { get; }

-        public IList<SerializationCallback> OnSerializedCallbacks { get; }

-        public IList<SerializationCallback> OnSerializingCallbacks { get; }

-        public Type UnderlyingType { get; }

-    }
-    public class JsonDictionaryContract : JsonContainerContract {
 {
-        public JsonDictionaryContract(Type underlyingType);

-        public Func<string, string> DictionaryKeyResolver { get; set; }

-        public Type DictionaryKeyType { get; }

-        public Type DictionaryValueType { get; }

-        public bool HasParameterizedCreator { get; set; }

-        public ObjectConstructor<object> OverrideCreator { get; set; }

-    }
-    public class JsonDynamicContract : JsonContainerContract {
 {
-        public JsonDynamicContract(Type underlyingType);

-        public JsonPropertyCollection Properties { get; }

-        public Func<string, string> PropertyNameResolver { get; set; }

-    }
-    public class JsonISerializableContract : JsonContainerContract {
 {
-        public JsonISerializableContract(Type underlyingType);

-        public ObjectConstructor<object> ISerializableCreator { get; set; }

-    }
-    public class JsonLinqContract : JsonContract {
 {
-        public JsonLinqContract(Type underlyingType);

-    }
-    public class JsonObjectContract : JsonContainerContract {
 {
-        public JsonObjectContract(Type underlyingType);

-        public JsonPropertyCollection CreatorParameters { get; }

-        public ExtensionDataGetter ExtensionDataGetter { get; set; }

-        public Func<string, string> ExtensionDataNameResolver { get; set; }

-        public ExtensionDataSetter ExtensionDataSetter { get; set; }

-        public Type ExtensionDataValueType { get; set; }

-        public Nullable<NullValueHandling> ItemNullValueHandling { get; set; }

-        public Nullable<Required> ItemRequired { get; set; }

-        public MemberSerialization MemberSerialization { get; set; }

-        public ObjectConstructor<object> OverrideCreator { get; set; }

-        public JsonPropertyCollection Properties { get; }

-    }
-    public class JsonPrimitiveContract : JsonContract {
 {
-        public JsonPrimitiveContract(Type underlyingType);

-    }
-    public class JsonProperty {
 {
-        public JsonProperty();

-        public IAttributeProvider AttributeProvider { get; set; }

-        public JsonConverter Converter { get; set; }

-        public Type DeclaringType { get; set; }

-        public object DefaultValue { get; set; }

-        public Nullable<DefaultValueHandling> DefaultValueHandling { get; set; }

-        public Predicate<object> GetIsSpecified { get; set; }

-        public bool HasMemberAttribute { get; set; }

-        public bool Ignored { get; set; }

-        public Nullable<bool> IsReference { get; set; }

-        public JsonConverter ItemConverter { get; set; }

-        public Nullable<bool> ItemIsReference { get; set; }

-        public Nullable<ReferenceLoopHandling> ItemReferenceLoopHandling { get; set; }

-        public Nullable<TypeNameHandling> ItemTypeNameHandling { get; set; }

-        public JsonConverter MemberConverter { get; set; }

-        public Nullable<NullValueHandling> NullValueHandling { get; set; }

-        public Nullable<ObjectCreationHandling> ObjectCreationHandling { get; set; }

-        public Nullable<int> Order { get; set; }

-        public string PropertyName { get; set; }

-        public Type PropertyType { get; set; }

-        public bool Readable { get; set; }

-        public Nullable<ReferenceLoopHandling> ReferenceLoopHandling { get; set; }

-        public Required Required { get; set; }

-        public Action<object, object> SetIsSpecified { get; set; }

-        public Predicate<object> ShouldDeserialize { get; set; }

-        public Predicate<object> ShouldSerialize { get; set; }

-        public Nullable<TypeNameHandling> TypeNameHandling { get; set; }

-        public string UnderlyingName { get; set; }

-        public IValueProvider ValueProvider { get; set; }

-        public bool Writable { get; set; }

-        public override string ToString();

-    }
-    public class JsonPropertyCollection : KeyedCollection<string, JsonProperty> {
 {
-        public JsonPropertyCollection(Type type);

-        public void AddProperty(JsonProperty property);

-        public JsonProperty GetClosestMatchProperty(string propertyName);

-        protected override string GetKeyForItem(JsonProperty item);

-        public JsonProperty GetProperty(string propertyName, StringComparison comparisonType);

-    }
-    public class JsonStringContract : JsonPrimitiveContract {
 {
-        public JsonStringContract(Type underlyingType);

-    }
-    public class MemoryTraceWriter : ITraceWriter {
 {
-        public MemoryTraceWriter();

-        public TraceLevel LevelFilter { get; set; }

-        public IEnumerable<string> GetTraceMessages();

-        public override string ToString();

-        public void Trace(TraceLevel level, string message, Exception ex);

-    }
-    public abstract class NamingStrategy {
 {
-        protected NamingStrategy();

-        public bool OverrideSpecifiedNames { get; set; }

-        public bool ProcessDictionaryKeys { get; set; }

-        public bool ProcessExtensionDataNames { get; set; }

-        public virtual string GetDictionaryKey(string key);

-        public virtual string GetExtensionDataName(string name);

-        public virtual string GetPropertyName(string name, bool hasSpecifiedName);

-        protected abstract string ResolvePropertyName(string name);

-    }
-    public delegate object ObjectConstructor<T>(params object[] args);

-    public sealed class OnErrorAttribute : Attribute {
 {
-        public OnErrorAttribute();

-    }
-    public class ReflectionAttributeProvider : IAttributeProvider {
 {
-        public ReflectionAttributeProvider(object attributeProvider);

-        public IList<Attribute> GetAttributes(bool inherit);

-        public IList<Attribute> GetAttributes(Type attributeType, bool inherit);

-    }
-    public class ReflectionValueProvider : IValueProvider {
 {
-        public ReflectionValueProvider(MemberInfo memberInfo);

-        public object GetValue(object target);

-        public void SetValue(object target, object value);

-    }
-    public delegate void SerializationCallback(object o, StreamingContext context);

-    public delegate void SerializationErrorCallback(object o, StreamingContext context, ErrorContext errorContext);

-    public class SnakeCaseNamingStrategy : NamingStrategy {
 {
-        public SnakeCaseNamingStrategy();

-        public SnakeCaseNamingStrategy(bool processDictionaryKeys, bool overrideSpecifiedNames);

-        public SnakeCaseNamingStrategy(bool processDictionaryKeys, bool overrideSpecifiedNames, bool processExtensionDataNames);

-        protected override string ResolvePropertyName(string name);

-    }
-}
```

