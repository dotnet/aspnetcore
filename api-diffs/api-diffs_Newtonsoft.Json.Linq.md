# Newtonsoft.Json.Linq

``` diff
-namespace Newtonsoft.Json.Linq {
 {
-    public enum CommentHandling {
 {
-        Ignore = 0,

-        Load = 1,

-    }
-    public static class Extensions {
 {
-        public static IJEnumerable<JToken> Ancestors<T>(this IEnumerable<T> source) where T : JToken;

-        public static IJEnumerable<JToken> AncestorsAndSelf<T>(this IEnumerable<T> source) where T : JToken;

-        public static IJEnumerable<JToken> AsJEnumerable(this IEnumerable<JToken> source);

-        public static IJEnumerable<T> AsJEnumerable<T>(this IEnumerable<T> source) where T : JToken;

-        public static IEnumerable<U> Children<T, U>(this IEnumerable<T> source) where T : JToken;

-        public static IJEnumerable<JToken> Children<T>(this IEnumerable<T> source) where T : JToken;

-        public static IJEnumerable<JToken> Descendants<T>(this IEnumerable<T> source) where T : JContainer;

-        public static IJEnumerable<JToken> DescendantsAndSelf<T>(this IEnumerable<T> source) where T : JContainer;

-        public static IJEnumerable<JProperty> Properties(this IEnumerable<JObject> source);

-        public static U Value<T, U>(this IEnumerable<T> value) where T : JToken;

-        public static U Value<U>(this IEnumerable<JToken> value);

-        public static IJEnumerable<JToken> Values(this IEnumerable<JToken> source);

-        public static IJEnumerable<JToken> Values(this IEnumerable<JToken> source, object key);

-        public static IEnumerable<U> Values<U>(this IEnumerable<JToken> source);

-        public static IEnumerable<U> Values<U>(this IEnumerable<JToken> source, object key);

-    }
-    public interface IJEnumerable<out T> : IEnumerable, IEnumerable<T> where T : JToken {
 {
-        IJEnumerable<JToken> this[object key] { get; }

-    }
-    public class JArray : JContainer, ICollection<JToken>, IEnumerable, IEnumerable<JToken>, IList<JToken> {
 {
-        public JArray();

-        public JArray(JArray other);

-        public JArray(object content);

-        public JArray(params object[] content);

-        protected override IList<JToken> ChildrenTokens { get; }

-        public bool IsReadOnly { get; }

-        public JToken this[int index] { get; set; }

-        public override JToken this[object key] { get; set; }

-        public override JTokenType Type { get; }

-        public void Add(JToken item);

-        public void Clear();

-        public bool Contains(JToken item);

-        public void CopyTo(JToken[] array, int arrayIndex);

-        public static new JArray FromObject(object o);

-        public static new JArray FromObject(object o, JsonSerializer jsonSerializer);

-        public IEnumerator<JToken> GetEnumerator();

-        public int IndexOf(JToken item);

-        public void Insert(int index, JToken item);

-        public static new JArray Load(JsonReader reader);

-        public static new JArray Load(JsonReader reader, JsonLoadSettings settings);

-        public static new Task<JArray> LoadAsync(JsonReader reader, JsonLoadSettings settings, CancellationToken cancellationToken = default(CancellationToken));

-        public static new Task<JArray> LoadAsync(JsonReader reader, CancellationToken cancellationToken = default(CancellationToken));

-        public static new JArray Parse(string json);

-        public static new JArray Parse(string json, JsonLoadSettings settings);

-        public bool Remove(JToken item);

-        public void RemoveAt(int index);

-        public override void WriteTo(JsonWriter writer, params JsonConverter[] converters);

-        public override Task WriteToAsync(JsonWriter writer, CancellationToken cancellationToken, params JsonConverter[] converters);

-    }
-    public class JConstructor : JContainer {
 {
-        public JConstructor();

-        public JConstructor(JConstructor other);

-        public JConstructor(string name);

-        public JConstructor(string name, object content);

-        public JConstructor(string name, params object[] content);

-        protected override IList<JToken> ChildrenTokens { get; }

-        public string Name { get; set; }

-        public override JToken this[object key] { get; set; }

-        public override JTokenType Type { get; }

-        public static new JConstructor Load(JsonReader reader);

-        public static new JConstructor Load(JsonReader reader, JsonLoadSettings settings);

-        public static new Task<JConstructor> LoadAsync(JsonReader reader, JsonLoadSettings settings, CancellationToken cancellationToken = default(CancellationToken));

-        public static new Task<JConstructor> LoadAsync(JsonReader reader, CancellationToken cancellationToken = default(CancellationToken));

-        public override void WriteTo(JsonWriter writer, params JsonConverter[] converters);

-        public override Task WriteToAsync(JsonWriter writer, CancellationToken cancellationToken, params JsonConverter[] converters);

-    }
-    public abstract class JContainer : JToken, IBindingList, ICollection, ICollection<JToken>, IEnumerable, IEnumerable<JToken>, IList, IList<JToken>, INotifyCollectionChanged, ITypedList {
 {
-        protected abstract IList<JToken> ChildrenTokens { get; }

-        public int Count { get; }

-        public override JToken First { get; }

-        public override bool HasValues { get; }

-        public override JToken Last { get; }

-        bool System.Collections.Generic.ICollection<Newtonsoft.Json.Linq.JToken>.IsReadOnly { get; }

-        JToken System.Collections.Generic.IList<Newtonsoft.Json.Linq.JToken>.this[int index] { get; set; }

-        bool System.Collections.ICollection.IsSynchronized { get; }

-        object System.Collections.ICollection.SyncRoot { get; }

-        bool System.Collections.IList.IsFixedSize { get; }

-        bool System.Collections.IList.IsReadOnly { get; }

-        object System.Collections.IList.this[int index] { get; set; }

-        bool System.ComponentModel.IBindingList.AllowEdit { get; }

-        bool System.ComponentModel.IBindingList.AllowNew { get; }

-        bool System.ComponentModel.IBindingList.AllowRemove { get; }

-        bool System.ComponentModel.IBindingList.IsSorted { get; }

-        ListSortDirection System.ComponentModel.IBindingList.SortDirection { get; }

-        PropertyDescriptor System.ComponentModel.IBindingList.SortProperty { get; }

-        bool System.ComponentModel.IBindingList.SupportsChangeNotification { get; }

-        bool System.ComponentModel.IBindingList.SupportsSearching { get; }

-        bool System.ComponentModel.IBindingList.SupportsSorting { get; }

-        public event AddingNewEventHandler AddingNew;

-        public event NotifyCollectionChangedEventHandler CollectionChanged;

-        public event ListChangedEventHandler ListChanged;

-        public virtual void Add(object content);

-        public void AddFirst(object content);

-        public override JEnumerable<JToken> Children();

-        public JsonWriter CreateWriter();

-        public IEnumerable<JToken> Descendants();

-        public IEnumerable<JToken> DescendantsAndSelf();

-        public void Merge(object content);

-        public void Merge(object content, JsonMergeSettings settings);

-        protected virtual void OnAddingNew(AddingNewEventArgs e);

-        protected virtual void OnCollectionChanged(NotifyCollectionChangedEventArgs e);

-        protected virtual void OnListChanged(ListChangedEventArgs e);

-        public void RemoveAll();

-        public void ReplaceAll(object content);

-        void System.Collections.Generic.ICollection<Newtonsoft.Json.Linq.JToken>.Add(JToken item);

-        void System.Collections.Generic.ICollection<Newtonsoft.Json.Linq.JToken>.Clear();

-        bool System.Collections.Generic.ICollection<Newtonsoft.Json.Linq.JToken>.Contains(JToken item);

-        void System.Collections.Generic.ICollection<Newtonsoft.Json.Linq.JToken>.CopyTo(JToken[] array, int arrayIndex);

-        bool System.Collections.Generic.ICollection<Newtonsoft.Json.Linq.JToken>.Remove(JToken item);

-        int System.Collections.Generic.IList<Newtonsoft.Json.Linq.JToken>.IndexOf(JToken item);

-        void System.Collections.Generic.IList<Newtonsoft.Json.Linq.JToken>.Insert(int index, JToken item);

-        void System.Collections.Generic.IList<Newtonsoft.Json.Linq.JToken>.RemoveAt(int index);

-        void System.Collections.ICollection.CopyTo(Array array, int index);

-        int System.Collections.IList.Add(object value);

-        void System.Collections.IList.Clear();

-        bool System.Collections.IList.Contains(object value);

-        int System.Collections.IList.IndexOf(object value);

-        void System.Collections.IList.Insert(int index, object value);

-        void System.Collections.IList.Remove(object value);

-        void System.Collections.IList.RemoveAt(int index);

-        void System.ComponentModel.IBindingList.AddIndex(PropertyDescriptor property);

-        object System.ComponentModel.IBindingList.AddNew();

-        void System.ComponentModel.IBindingList.ApplySort(PropertyDescriptor property, ListSortDirection direction);

-        int System.ComponentModel.IBindingList.Find(PropertyDescriptor property, object key);

-        void System.ComponentModel.IBindingList.RemoveIndex(PropertyDescriptor property);

-        void System.ComponentModel.IBindingList.RemoveSort();

-        PropertyDescriptorCollection System.ComponentModel.ITypedList.GetItemProperties(PropertyDescriptor[] listAccessors);

-        string System.ComponentModel.ITypedList.GetListName(PropertyDescriptor[] listAccessors);

-        public override IEnumerable<T> Values<T>();

-    }
-    public readonly struct JEnumerable<T> : IEnumerable, IEnumerable<T>, IEquatable<JEnumerable<T>>, IJEnumerable<T> where T : JToken {
 {
-        public static readonly JEnumerable<T> Empty;

-        public JEnumerable(IEnumerable<T> enumerable);

-        public IJEnumerable<JToken> this[object key] { get; }

-        public bool Equals(JEnumerable<T> other);

-        public override bool Equals(object obj);

-        public IEnumerator<T> GetEnumerator();

-        public override int GetHashCode();

-        IEnumerator System.Collections.IEnumerable.GetEnumerator();

-    }
-    public class JObject : JContainer, ICollection<KeyValuePair<string, JToken>>, ICustomTypeDescriptor, IDictionary<string, JToken>, IEnumerable, IEnumerable<KeyValuePair<string, JToken>>, INotifyPropertyChanged, INotifyPropertyChanging {
 {
-        public JObject();

-        public JObject(JObject other);

-        public JObject(object content);

-        public JObject(params object[] content);

-        protected override IList<JToken> ChildrenTokens { get; }

-        bool System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<System.String,Newtonsoft.Json.Linq.JToken>>.IsReadOnly { get; }

-        ICollection<string> System.Collections.Generic.IDictionary<System.String,Newtonsoft.Json.Linq.JToken>.Keys { get; }

-        ICollection<JToken> System.Collections.Generic.IDictionary<System.String,Newtonsoft.Json.Linq.JToken>.Values { get; }

-        public override JToken this[object key] { get; set; }

-        public JToken this[string propertyName] { get; set; }

-        public override JTokenType Type { get; }

-        public event PropertyChangedEventHandler PropertyChanged;

-        public event PropertyChangingEventHandler PropertyChanging;

-        public void Add(string propertyName, JToken value);

-        public bool ContainsKey(string propertyName);

-        public static new JObject FromObject(object o);

-        public static new JObject FromObject(object o, JsonSerializer jsonSerializer);

-        public IEnumerator<KeyValuePair<string, JToken>> GetEnumerator();

-        protected override DynamicMetaObject GetMetaObject(Expression parameter);

-        public JToken GetValue(string propertyName);

-        public JToken GetValue(string propertyName, StringComparison comparison);

-        public static new JObject Load(JsonReader reader);

-        public static new JObject Load(JsonReader reader, JsonLoadSettings settings);

-        public static new Task<JObject> LoadAsync(JsonReader reader, JsonLoadSettings settings, CancellationToken cancellationToken = default(CancellationToken));

-        public static new Task<JObject> LoadAsync(JsonReader reader, CancellationToken cancellationToken = default(CancellationToken));

-        protected virtual void OnPropertyChanged(string propertyName);

-        protected virtual void OnPropertyChanging(string propertyName);

-        public static new JObject Parse(string json);

-        public static new JObject Parse(string json, JsonLoadSettings settings);

-        public IEnumerable<JProperty> Properties();

-        public JProperty Property(string name);

-        public JEnumerable<JToken> PropertyValues();

-        public bool Remove(string propertyName);

-        void System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<System.String,Newtonsoft.Json.Linq.JToken>>.Add(KeyValuePair<string, JToken> item);

-        void System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<System.String,Newtonsoft.Json.Linq.JToken>>.Clear();

-        bool System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<System.String,Newtonsoft.Json.Linq.JToken>>.Contains(KeyValuePair<string, JToken> item);

-        void System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<System.String,Newtonsoft.Json.Linq.JToken>>.CopyTo(KeyValuePair<string, JToken>[] array, int arrayIndex);

-        bool System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<System.String,Newtonsoft.Json.Linq.JToken>>.Remove(KeyValuePair<string, JToken> item);

-        AttributeCollection System.ComponentModel.ICustomTypeDescriptor.GetAttributes();

-        string System.ComponentModel.ICustomTypeDescriptor.GetClassName();

-        string System.ComponentModel.ICustomTypeDescriptor.GetComponentName();

-        TypeConverter System.ComponentModel.ICustomTypeDescriptor.GetConverter();

-        EventDescriptor System.ComponentModel.ICustomTypeDescriptor.GetDefaultEvent();

-        PropertyDescriptor System.ComponentModel.ICustomTypeDescriptor.GetDefaultProperty();

-        object System.ComponentModel.ICustomTypeDescriptor.GetEditor(Type editorBaseType);

-        EventDescriptorCollection System.ComponentModel.ICustomTypeDescriptor.GetEvents();

-        EventDescriptorCollection System.ComponentModel.ICustomTypeDescriptor.GetEvents(Attribute[] attributes);

-        PropertyDescriptorCollection System.ComponentModel.ICustomTypeDescriptor.GetProperties();

-        PropertyDescriptorCollection System.ComponentModel.ICustomTypeDescriptor.GetProperties(Attribute[] attributes);

-        object System.ComponentModel.ICustomTypeDescriptor.GetPropertyOwner(PropertyDescriptor pd);

-        public bool TryGetValue(string propertyName, out JToken value);

-        public bool TryGetValue(string propertyName, StringComparison comparison, out JToken value);

-        public override void WriteTo(JsonWriter writer, params JsonConverter[] converters);

-        public override Task WriteToAsync(JsonWriter writer, CancellationToken cancellationToken, params JsonConverter[] converters);

-    }
-    public class JProperty : JContainer {
 {
-        public JProperty(JProperty other);

-        public JProperty(string name, object content);

-        public JProperty(string name, params object[] content);

-        protected override IList<JToken> ChildrenTokens { get; }

-        public string Name { get; }

-        public override JTokenType Type { get; }

-        public JToken Value { get; set; }

-        public static new JProperty Load(JsonReader reader);

-        public static new JProperty Load(JsonReader reader, JsonLoadSettings settings);

-        public static new Task<JProperty> LoadAsync(JsonReader reader, JsonLoadSettings settings, CancellationToken cancellationToken = default(CancellationToken));

-        public static new Task<JProperty> LoadAsync(JsonReader reader, CancellationToken cancellationToken = default(CancellationToken));

-        public override void WriteTo(JsonWriter writer, params JsonConverter[] converters);

-        public override Task WriteToAsync(JsonWriter writer, CancellationToken cancellationToken, params JsonConverter[] converters);

-    }
-    public class JPropertyDescriptor : PropertyDescriptor {
 {
-        public JPropertyDescriptor(string name);

-        public override Type ComponentType { get; }

-        public override bool IsReadOnly { get; }

-        protected override int NameHashCode { get; }

-        public override Type PropertyType { get; }

-        public override bool CanResetValue(object component);

-        public override object GetValue(object component);

-        public override void ResetValue(object component);

-        public override void SetValue(object component, object value);

-        public override bool ShouldSerializeValue(object component);

-    }
-    public class JRaw : JValue {
 {
-        public JRaw(JRaw other);

-        public JRaw(object rawJson);

-        public static JRaw Create(JsonReader reader);

-        public static Task<JRaw> CreateAsync(JsonReader reader, CancellationToken cancellationToken = default(CancellationToken));

-    }
-    public class JsonLoadSettings {
 {
-        public JsonLoadSettings();

-        public CommentHandling CommentHandling { get; set; }

-        public LineInfoHandling LineInfoHandling { get; set; }

-    }
-    public class JsonMergeSettings {
 {
-        public JsonMergeSettings();

-        public MergeArrayHandling MergeArrayHandling { get; set; }

-        public MergeNullValueHandling MergeNullValueHandling { get; set; }

-    }
-    public abstract class JToken : ICloneable, IDynamicMetaObjectProvider, IEnumerable, IEnumerable<JToken>, IJEnumerable<JToken>, IJsonLineInfo {
 {
-        public static JTokenEqualityComparer EqualityComparer { get; }

-        public virtual JToken First { get; }

-        public abstract bool HasValues { get; }

-        public virtual JToken Last { get; }

-        int Newtonsoft.Json.IJsonLineInfo.LineNumber { get; }

-        int Newtonsoft.Json.IJsonLineInfo.LinePosition { get; }

-        IJEnumerable<JToken> Newtonsoft.Json.Linq.IJEnumerable<Newtonsoft.Json.Linq.JToken>.this[object key] { get; }

-        public JToken Next { get; internal set; }

-        public JContainer Parent { get; internal set; }

-        public string Path { get; }

-        public JToken Previous { get; internal set; }

-        public JToken Root { get; }

-        public virtual JToken this[object key] { get; set; }

-        public abstract JTokenType Type { get; }

-        public void AddAfterSelf(object content);

-        public void AddAnnotation(object annotation);

-        public void AddBeforeSelf(object content);

-        public IEnumerable<JToken> AfterSelf();

-        public IEnumerable<JToken> Ancestors();

-        public IEnumerable<JToken> AncestorsAndSelf();

-        public object Annotation(Type type);

-        public T Annotation<T>() where T : class;

-        public IEnumerable<object> Annotations(Type type);

-        public IEnumerable<T> Annotations<T>() where T : class;

-        public IEnumerable<JToken> BeforeSelf();

-        public virtual JEnumerable<JToken> Children();

-        public JEnumerable<T> Children<T>() where T : JToken;

-        public JsonReader CreateReader();

-        public JToken DeepClone();

-        public static bool DeepEquals(JToken t1, JToken t2);

-        public static JToken FromObject(object o);

-        public static JToken FromObject(object o, JsonSerializer jsonSerializer);

-        protected virtual DynamicMetaObject GetMetaObject(Expression parameter);

-        public static JToken Load(JsonReader reader);

-        public static JToken Load(JsonReader reader, JsonLoadSettings settings);

-        public static Task<JToken> LoadAsync(JsonReader reader, JsonLoadSettings settings, CancellationToken cancellationToken = default(CancellationToken));

-        public static Task<JToken> LoadAsync(JsonReader reader, CancellationToken cancellationToken = default(CancellationToken));

-        bool Newtonsoft.Json.IJsonLineInfo.HasLineInfo();

-        public static explicit operator bool (JToken value);

-        public static explicit operator DateTimeOffset (JToken value);

-        public static explicit operator Nullable<bool> (JToken value);

-        public static explicit operator long (JToken value);

-        public static explicit operator Nullable<DateTime> (JToken value);

-        public static explicit operator Nullable<DateTimeOffset> (JToken value);

-        public static explicit operator Nullable<Decimal> (JToken value);

-        public static explicit operator Nullable<double> (JToken value);

-        public static explicit operator Nullable<char> (JToken value);

-        public static explicit operator int (JToken value);

-        public static explicit operator short (JToken value);

-        public static explicit operator ushort (JToken value);

-        public static explicit operator char (JToken value);

-        public static explicit operator byte (JToken value);

-        public static explicit operator sbyte (JToken value);

-        public static explicit operator Nullable<int> (JToken value);

-        public static explicit operator Nullable<short> (JToken value);

-        public static explicit operator Nullable<ushort> (JToken value);

-        public static explicit operator Nullable<byte> (JToken value);

-        public static explicit operator Nullable<sbyte> (JToken value);

-        public static explicit operator DateTime (JToken value);

-        public static explicit operator Nullable<long> (JToken value);

-        public static explicit operator Nullable<float> (JToken value);

-        public static explicit operator Decimal (JToken value);

-        public static explicit operator Nullable<uint> (JToken value);

-        public static explicit operator Nullable<ulong> (JToken value);

-        public static explicit operator double (JToken value);

-        public static explicit operator float (JToken value);

-        public static explicit operator string (JToken value);

-        public static explicit operator uint (JToken value);

-        public static explicit operator ulong (JToken value);

-        public static explicit operator byte[] (JToken value);

-        public static explicit operator Guid (JToken value);

-        public static explicit operator Nullable<Guid> (JToken value);

-        public static explicit operator TimeSpan (JToken value);

-        public static explicit operator Nullable<TimeSpan> (JToken value);

-        public static explicit operator Uri (JToken value);

-        public static implicit operator JToken (bool value);

-        public static implicit operator JToken (byte value);

-        public static implicit operator JToken (byte[] value);

-        public static implicit operator JToken (DateTime value);

-        public static implicit operator JToken (DateTimeOffset value);

-        public static implicit operator JToken (Decimal value);

-        public static implicit operator JToken (double value);

-        public static implicit operator JToken (Guid value);

-        public static implicit operator JToken (short value);

-        public static implicit operator JToken (int value);

-        public static implicit operator JToken (long value);

-        public static implicit operator JToken (Nullable<bool> value);

-        public static implicit operator JToken (Nullable<byte> value);

-        public static implicit operator JToken (Nullable<DateTime> value);

-        public static implicit operator JToken (Nullable<DateTimeOffset> value);

-        public static implicit operator JToken (Nullable<Decimal> value);

-        public static implicit operator JToken (Nullable<double> value);

-        public static implicit operator JToken (Nullable<Guid> value);

-        public static implicit operator JToken (Nullable<short> value);

-        public static implicit operator JToken (Nullable<int> value);

-        public static implicit operator JToken (Nullable<long> value);

-        public static implicit operator JToken (Nullable<sbyte> value);

-        public static implicit operator JToken (Nullable<float> value);

-        public static implicit operator JToken (Nullable<TimeSpan> value);

-        public static implicit operator JToken (Nullable<ushort> value);

-        public static implicit operator JToken (Nullable<uint> value);

-        public static implicit operator JToken (Nullable<ulong> value);

-        public static implicit operator JToken (sbyte value);

-        public static implicit operator JToken (float value);

-        public static implicit operator JToken (string value);

-        public static implicit operator JToken (TimeSpan value);

-        public static implicit operator JToken (ushort value);

-        public static implicit operator JToken (uint value);

-        public static implicit operator JToken (ulong value);

-        public static implicit operator JToken (Uri value);

-        public static JToken Parse(string json);

-        public static JToken Parse(string json, JsonLoadSettings settings);

-        public static JToken ReadFrom(JsonReader reader);

-        public static JToken ReadFrom(JsonReader reader, JsonLoadSettings settings);

-        public static Task<JToken> ReadFromAsync(JsonReader reader, JsonLoadSettings settings, CancellationToken cancellationToken = default(CancellationToken));

-        public static Task<JToken> ReadFromAsync(JsonReader reader, CancellationToken cancellationToken = default(CancellationToken));

-        public void Remove();

-        public void RemoveAnnotations(Type type);

-        public void RemoveAnnotations<T>() where T : class;

-        public void Replace(JToken value);

-        public JToken SelectToken(string path);

-        public JToken SelectToken(string path, bool errorWhenNoMatch);

-        public IEnumerable<JToken> SelectTokens(string path);

-        public IEnumerable<JToken> SelectTokens(string path, bool errorWhenNoMatch);

-        IEnumerator<JToken> System.Collections.Generic.IEnumerable<Newtonsoft.Json.Linq.JToken>.GetEnumerator();

-        IEnumerator System.Collections.IEnumerable.GetEnumerator();

-        DynamicMetaObject System.Dynamic.IDynamicMetaObjectProvider.GetMetaObject(Expression parameter);

-        object System.ICloneable.Clone();

-        public object ToObject(Type objectType);

-        public object ToObject(Type objectType, JsonSerializer jsonSerializer);

-        public T ToObject<T>();

-        public T ToObject<T>(JsonSerializer jsonSerializer);

-        public override string ToString();

-        public string ToString(Formatting formatting, params JsonConverter[] converters);

-        public virtual T Value<T>(object key);

-        public virtual IEnumerable<T> Values<T>();

-        public abstract void WriteTo(JsonWriter writer, params JsonConverter[] converters);

-        public Task WriteToAsync(JsonWriter writer, params JsonConverter[] converters);

-        public virtual Task WriteToAsync(JsonWriter writer, CancellationToken cancellationToken, params JsonConverter[] converters);

-    }
-    public class JTokenEqualityComparer : IEqualityComparer<JToken> {
 {
-        public JTokenEqualityComparer();

-        public bool Equals(JToken x, JToken y);

-        public int GetHashCode(JToken obj);

-    }
-    public class JTokenReader : JsonReader, IJsonLineInfo {
 {
-        public JTokenReader(JToken token);

-        public JToken CurrentToken { get; }

-        int Newtonsoft.Json.IJsonLineInfo.LineNumber { get; }

-        int Newtonsoft.Json.IJsonLineInfo.LinePosition { get; }

-        public override string Path { get; }

-        bool Newtonsoft.Json.IJsonLineInfo.HasLineInfo();

-        public override bool Read();

-    }
-    public enum JTokenType {
 {
-        Array = 2,

-        Boolean = 9,

-        Bytes = 14,

-        Comment = 5,

-        Constructor = 3,

-        Date = 12,

-        Float = 7,

-        Guid = 15,

-        Integer = 6,

-        None = 0,

-        Null = 10,

-        Object = 1,

-        Property = 4,

-        Raw = 13,

-        String = 8,

-        TimeSpan = 17,

-        Undefined = 11,

-        Uri = 16,

-    }
-    public class JTokenWriter : JsonWriter {
 {
-        public JTokenWriter();

-        public JTokenWriter(JContainer container);

-        public JToken CurrentToken { get; }

-        public JToken Token { get; }

-        public override void Close();

-        public override void Flush();

-        public override void WriteComment(string text);

-        protected override void WriteEnd(JsonToken token);

-        public override void WriteNull();

-        public override void WritePropertyName(string name);

-        public override void WriteRaw(string json);

-        public override void WriteStartArray();

-        public override void WriteStartConstructor(string name);

-        public override void WriteStartObject();

-        public override void WriteUndefined();

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

-        public override void WriteValue(object value);

-        public override void WriteValue(sbyte value);

-        public override void WriteValue(float value);

-        public override void WriteValue(string value);

-        public override void WriteValue(TimeSpan value);

-        public override void WriteValue(ushort value);

-        public override void WriteValue(uint value);

-        public override void WriteValue(ulong value);

-        public override void WriteValue(Uri value);

-    }
-    public class JValue : JToken, IComparable, IComparable<JValue>, IConvertible, IEquatable<JValue>, IFormattable {
 {
-        public JValue(JValue other);

-        public JValue(bool value);

-        public JValue(char value);

-        public JValue(DateTime value);

-        public JValue(DateTimeOffset value);

-        public JValue(Decimal value);

-        public JValue(double value);

-        public JValue(Guid value);

-        public JValue(long value);

-        public JValue(object value);

-        public JValue(float value);

-        public JValue(string value);

-        public JValue(TimeSpan value);

-        public JValue(ulong value);

-        public JValue(Uri value);

-        public override bool HasValues { get; }

-        public override JTokenType Type { get; }

-        public object Value { get; set; }

-        public int CompareTo(JValue obj);

-        public static JValue CreateComment(string value);

-        public static JValue CreateNull();

-        public static JValue CreateString(string value);

-        public static JValue CreateUndefined();

-        public bool Equals(JValue other);

-        public override bool Equals(object obj);

-        public override int GetHashCode();

-        protected override DynamicMetaObject GetMetaObject(Expression parameter);

-        int System.IComparable.CompareTo(object obj);

-        TypeCode System.IConvertible.GetTypeCode();

-        bool System.IConvertible.ToBoolean(IFormatProvider provider);

-        byte System.IConvertible.ToByte(IFormatProvider provider);

-        char System.IConvertible.ToChar(IFormatProvider provider);

-        DateTime System.IConvertible.ToDateTime(IFormatProvider provider);

-        Decimal System.IConvertible.ToDecimal(IFormatProvider provider);

-        double System.IConvertible.ToDouble(IFormatProvider provider);

-        short System.IConvertible.ToInt16(IFormatProvider provider);

-        int System.IConvertible.ToInt32(IFormatProvider provider);

-        long System.IConvertible.ToInt64(IFormatProvider provider);

-        sbyte System.IConvertible.ToSByte(IFormatProvider provider);

-        float System.IConvertible.ToSingle(IFormatProvider provider);

-        object System.IConvertible.ToType(Type conversionType, IFormatProvider provider);

-        ushort System.IConvertible.ToUInt16(IFormatProvider provider);

-        uint System.IConvertible.ToUInt32(IFormatProvider provider);

-        ulong System.IConvertible.ToUInt64(IFormatProvider provider);

-        public override string ToString();

-        public string ToString(IFormatProvider formatProvider);

-        public string ToString(string format);

-        public string ToString(string format, IFormatProvider formatProvider);

-        public override void WriteTo(JsonWriter writer, params JsonConverter[] converters);

-        public override Task WriteToAsync(JsonWriter writer, CancellationToken cancellationToken, params JsonConverter[] converters);

-    }
-    public enum LineInfoHandling {
 {
-        Ignore = 0,

-        Load = 1,

-    }
-    public enum MergeArrayHandling {
 {
-        Concat = 0,

-        Merge = 3,

-        Replace = 2,

-        Union = 1,

-    }
-    public enum MergeNullValueHandling {
 {
-        Ignore = 0,

-        Merge = 1,

-    }
-}
```

