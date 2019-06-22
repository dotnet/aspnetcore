# Newtonsoft.Json.Bson

``` diff
-namespace Newtonsoft.Json.Bson {
 {
-    public class BsonDataObjectId {
 {
-        public BsonDataObjectId(byte[] value);

-        public byte[] Value { get; private set; }

-    }
-    public class BsonDataReader : JsonReader {
 {
-        public BsonDataReader(BinaryReader reader);

-        public BsonDataReader(BinaryReader reader, bool readRootValueAsArray, DateTimeKind dateTimeKindHandling);

-        public BsonDataReader(Stream stream);

-        public BsonDataReader(Stream stream, bool readRootValueAsArray, DateTimeKind dateTimeKindHandling);

-        public DateTimeKind DateTimeKindHandling { get; set; }

-        public bool JsonNet35BinaryCompatibility { get; set; }

-        public bool ReadRootValueAsArray { get; set; }

-        public override void Close();

-        public override bool Read();

-        public override Task<Nullable<bool>> ReadAsBooleanAsync(CancellationToken cancellationToken = default(CancellationToken));

-        public override Task<byte[]> ReadAsBytesAsync(CancellationToken cancellationToken = default(CancellationToken));

-        public override Task<Nullable<DateTime>> ReadAsDateTimeAsync(CancellationToken cancellationToken = default(CancellationToken));

-        public override Task<Nullable<DateTimeOffset>> ReadAsDateTimeOffsetAsync(CancellationToken cancellationToken = default(CancellationToken));

-        public override Task<Nullable<decimal>> ReadAsDecimalAsync(CancellationToken cancellationToken = default(CancellationToken));

-        public override Task<Nullable<double>> ReadAsDoubleAsync(CancellationToken cancellationToken = default(CancellationToken));

-        public override Task<Nullable<int>> ReadAsInt32Async(CancellationToken cancellationToken = default(CancellationToken));

-        public override Task<string> ReadAsStringAsync(CancellationToken cancellationToken = default(CancellationToken));

-        public override Task<bool> ReadAsync(CancellationToken cancellationToken = default(CancellationToken));

-    }
-    public class BsonDataWriter : JsonWriter {
 {
-        public BsonDataWriter(BinaryWriter writer);

-        public BsonDataWriter(Stream stream);

-        public DateTimeKind DateTimeKindHandling { get; set; }

-        public override void Close();

-        public override Task CloseAsync(CancellationToken cancellationToken = default(CancellationToken));

-        public override void Flush();

-        public override Task FlushAsync(CancellationToken cancellationToken = default(CancellationToken));

-        public override void WriteComment(string text);

-        protected override void WriteEnd(JsonToken token);

-        public override Task WriteEndArrayAsync(CancellationToken cancellationToken = default(CancellationToken));

-        public override Task WriteEndAsync(CancellationToken cancellationToken = default(CancellationToken));

-        public override Task WriteEndObjectAsync(CancellationToken cancellationToken = default(CancellationToken));

-        public override void WriteNull();

-        public void WriteObjectId(byte[] value);

-        public override void WritePropertyName(string name);

-        public override void WriteRaw(string json);

-        public override void WriteRawValue(string json);

-        public void WriteRegex(string pattern, string options);

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

-        public override void WriteValue(decimal value);

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
-    public class BsonObjectId {
 {
-        public BsonObjectId(byte[] value);

-        public byte[] Value { get; }

-    }
-    public class BsonReader : JsonReader {
 {
-        public BsonReader(BinaryReader reader);

-        public BsonReader(BinaryReader reader, bool readRootValueAsArray, DateTimeKind dateTimeKindHandling);

-        public BsonReader(Stream stream);

-        public BsonReader(Stream stream, bool readRootValueAsArray, DateTimeKind dateTimeKindHandling);

-        public DateTimeKind DateTimeKindHandling { get; set; }

-        public bool JsonNet35BinaryCompatibility { get; set; }

-        public bool ReadRootValueAsArray { get; set; }

-        public override void Close();

-        public override bool Read();

-    }
-    public class BsonWriter : JsonWriter {
 {
-        public BsonWriter(BinaryWriter writer);

-        public BsonWriter(Stream stream);

-        public DateTimeKind DateTimeKindHandling { get; set; }

-        public override void Close();

-        public override void Flush();

-        public override void WriteComment(string text);

-        protected override void WriteEnd(JsonToken token);

-        public override void WriteNull();

-        public void WriteObjectId(byte[] value);

-        public override void WritePropertyName(string name);

-        public override void WriteRaw(string json);

-        public override void WriteRawValue(string json);

-        public void WriteRegex(string pattern, string options);

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
-}
```

