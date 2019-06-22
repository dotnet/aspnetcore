# Microsoft.SqlServer.Server

``` diff
-namespace Microsoft.SqlServer.Server {
 {
-    public enum DataAccessKind {
 {
-        None = 0,

-        Read = 1,

-    }
-    public enum Format {
 {
-        Native = 1,

-        Unknown = 0,

-        UserDefined = 2,

-    }
-    public interface IBinarySerialize {
 {
-        void Read(BinaryReader r);

-        void Write(BinaryWriter w);

-    }
-    public sealed class InvalidUdtException : SystemException {
 {
-        public override void GetObjectData(SerializationInfo si, StreamingContext context);

-    }
-    public class SqlDataRecord : IDataRecord {
 {
-        public SqlDataRecord(params SqlMetaData[] metaData);

-        public virtual int FieldCount { get; }

-        public virtual object this[int ordinal] { get; }

-        public virtual object this[string name] { get; }

-        public virtual bool GetBoolean(int ordinal);

-        public virtual byte GetByte(int ordinal);

-        public virtual long GetBytes(int ordinal, long fieldOffset, byte[] buffer, int bufferOffset, int length);

-        public virtual char GetChar(int ordinal);

-        public virtual long GetChars(int ordinal, long fieldOffset, char[] buffer, int bufferOffset, int length);

-        public virtual string GetDataTypeName(int ordinal);

-        public virtual DateTime GetDateTime(int ordinal);

-        public virtual DateTimeOffset GetDateTimeOffset(int ordinal);

-        public virtual decimal GetDecimal(int ordinal);

-        public virtual double GetDouble(int ordinal);

-        public virtual Type GetFieldType(int ordinal);

-        public virtual float GetFloat(int ordinal);

-        public virtual Guid GetGuid(int ordinal);

-        public virtual short GetInt16(int ordinal);

-        public virtual int GetInt32(int ordinal);

-        public virtual long GetInt64(int ordinal);

-        public virtual string GetName(int ordinal);

-        public virtual int GetOrdinal(string name);

-        public virtual SqlBinary GetSqlBinary(int ordinal);

-        public virtual SqlBoolean GetSqlBoolean(int ordinal);

-        public virtual SqlByte GetSqlByte(int ordinal);

-        public virtual SqlBytes GetSqlBytes(int ordinal);

-        public virtual SqlChars GetSqlChars(int ordinal);

-        public virtual SqlDateTime GetSqlDateTime(int ordinal);

-        public virtual SqlDecimal GetSqlDecimal(int ordinal);

-        public virtual SqlDouble GetSqlDouble(int ordinal);

-        public virtual Type GetSqlFieldType(int ordinal);

-        public virtual SqlGuid GetSqlGuid(int ordinal);

-        public virtual SqlInt16 GetSqlInt16(int ordinal);

-        public virtual SqlInt32 GetSqlInt32(int ordinal);

-        public virtual SqlInt64 GetSqlInt64(int ordinal);

-        public virtual SqlMetaData GetSqlMetaData(int ordinal);

-        public virtual SqlMoney GetSqlMoney(int ordinal);

-        public virtual SqlSingle GetSqlSingle(int ordinal);

-        public virtual SqlString GetSqlString(int ordinal);

-        public virtual object GetSqlValue(int ordinal);

-        public virtual int GetSqlValues(object[] values);

-        public virtual SqlXml GetSqlXml(int ordinal);

-        public virtual string GetString(int ordinal);

-        public virtual TimeSpan GetTimeSpan(int ordinal);

-        public virtual object GetValue(int ordinal);

-        public virtual int GetValues(object[] values);

-        public virtual bool IsDBNull(int ordinal);

-        public virtual void SetBoolean(int ordinal, bool value);

-        public virtual void SetByte(int ordinal, byte value);

-        public virtual void SetBytes(int ordinal, long fieldOffset, byte[] buffer, int bufferOffset, int length);

-        public virtual void SetChar(int ordinal, char value);

-        public virtual void SetChars(int ordinal, long fieldOffset, char[] buffer, int bufferOffset, int length);

-        public virtual void SetDateTime(int ordinal, DateTime value);

-        public virtual void SetDateTimeOffset(int ordinal, DateTimeOffset value);

-        public virtual void SetDBNull(int ordinal);

-        public virtual void SetDecimal(int ordinal, decimal value);

-        public virtual void SetDouble(int ordinal, double value);

-        public virtual void SetFloat(int ordinal, float value);

-        public virtual void SetGuid(int ordinal, Guid value);

-        public virtual void SetInt16(int ordinal, short value);

-        public virtual void SetInt32(int ordinal, int value);

-        public virtual void SetInt64(int ordinal, long value);

-        public virtual void SetSqlBinary(int ordinal, SqlBinary value);

-        public virtual void SetSqlBoolean(int ordinal, SqlBoolean value);

-        public virtual void SetSqlByte(int ordinal, SqlByte value);

-        public virtual void SetSqlBytes(int ordinal, SqlBytes value);

-        public virtual void SetSqlChars(int ordinal, SqlChars value);

-        public virtual void SetSqlDateTime(int ordinal, SqlDateTime value);

-        public virtual void SetSqlDecimal(int ordinal, SqlDecimal value);

-        public virtual void SetSqlDouble(int ordinal, SqlDouble value);

-        public virtual void SetSqlGuid(int ordinal, SqlGuid value);

-        public virtual void SetSqlInt16(int ordinal, SqlInt16 value);

-        public virtual void SetSqlInt32(int ordinal, SqlInt32 value);

-        public virtual void SetSqlInt64(int ordinal, SqlInt64 value);

-        public virtual void SetSqlMoney(int ordinal, SqlMoney value);

-        public virtual void SetSqlSingle(int ordinal, SqlSingle value);

-        public virtual void SetSqlString(int ordinal, SqlString value);

-        public virtual void SetSqlXml(int ordinal, SqlXml value);

-        public virtual void SetString(int ordinal, string value);

-        public virtual void SetTimeSpan(int ordinal, TimeSpan value);

-        public virtual void SetValue(int ordinal, object value);

-        public virtual int SetValues(params object[] values);

-        IDataReader System.Data.IDataRecord.GetData(int ordinal);

-    }
-    public class SqlFunctionAttribute : Attribute {
 {
-        public SqlFunctionAttribute();

-        public DataAccessKind DataAccess { get; set; }

-        public string FillRowMethodName { get; set; }

-        public bool IsDeterministic { get; set; }

-        public bool IsPrecise { get; set; }

-        public string Name { get; set; }

-        public SystemDataAccessKind SystemDataAccess { get; set; }

-        public string TableDefinition { get; set; }

-    }
-    public sealed class SqlMetaData {
 {
-        public SqlMetaData(string name, SqlDbType dbType);

-        public SqlMetaData(string name, SqlDbType dbType, bool useServerDefault, bool isUniqueKey, SortOrder columnSortOrder, int sortOrdinal);

-        public SqlMetaData(string name, SqlDbType dbType, byte precision, byte scale);

-        public SqlMetaData(string name, SqlDbType dbType, byte precision, byte scale, bool useServerDefault, bool isUniqueKey, SortOrder columnSortOrder, int sortOrdinal);

-        public SqlMetaData(string name, SqlDbType dbType, long maxLength);

-        public SqlMetaData(string name, SqlDbType dbType, long maxLength, bool useServerDefault, bool isUniqueKey, SortOrder columnSortOrder, int sortOrdinal);

-        public SqlMetaData(string name, SqlDbType dbType, long maxLength, byte precision, byte scale, long locale, SqlCompareOptions compareOptions, Type userDefinedType);

-        public SqlMetaData(string name, SqlDbType dbType, long maxLength, byte precision, byte scale, long localeId, SqlCompareOptions compareOptions, Type userDefinedType, bool useServerDefault, bool isUniqueKey, SortOrder columnSortOrder, int sortOrdinal);

-        public SqlMetaData(string name, SqlDbType dbType, long maxLength, long locale, SqlCompareOptions compareOptions);

-        public SqlMetaData(string name, SqlDbType dbType, long maxLength, long locale, SqlCompareOptions compareOptions, bool useServerDefault, bool isUniqueKey, SortOrder columnSortOrder, int sortOrdinal);

-        public SqlMetaData(string name, SqlDbType dbType, string database, string owningSchema, string objectName);

-        public SqlMetaData(string name, SqlDbType dbType, string database, string owningSchema, string objectName, bool useServerDefault, bool isUniqueKey, SortOrder columnSortOrder, int sortOrdinal);

-        public SqlMetaData(string name, SqlDbType dbType, Type userDefinedType);

-        public SqlMetaData(string name, SqlDbType dbType, Type userDefinedType, string serverTypeName);

-        public SqlMetaData(string name, SqlDbType dbType, Type userDefinedType, string serverTypeName, bool useServerDefault, bool isUniqueKey, SortOrder columnSortOrder, int sortOrdinal);

-        public SqlCompareOptions CompareOptions { get; }

-        public DbType DbType { get; }

-        public bool IsUniqueKey { get; }

-        public long LocaleId { get; }

-        public static long Max { get; }

-        public long MaxLength { get; }

-        public string Name { get; }

-        public byte Precision { get; }

-        public byte Scale { get; }

-        public SortOrder SortOrder { get; }

-        public int SortOrdinal { get; }

-        public SqlDbType SqlDbType { get; }

-        public Type Type { get; }

-        public string TypeName { get; }

-        public bool UseServerDefault { get; }

-        public string XmlSchemaCollectionDatabase { get; }

-        public string XmlSchemaCollectionName { get; }

-        public string XmlSchemaCollectionOwningSchema { get; }

-        public bool Adjust(bool value);

-        public byte Adjust(byte value);

-        public byte[] Adjust(byte[] value);

-        public char Adjust(char value);

-        public char[] Adjust(char[] value);

-        public SqlBinary Adjust(SqlBinary value);

-        public SqlBoolean Adjust(SqlBoolean value);

-        public SqlByte Adjust(SqlByte value);

-        public SqlBytes Adjust(SqlBytes value);

-        public SqlChars Adjust(SqlChars value);

-        public SqlDateTime Adjust(SqlDateTime value);

-        public SqlDecimal Adjust(SqlDecimal value);

-        public SqlDouble Adjust(SqlDouble value);

-        public SqlGuid Adjust(SqlGuid value);

-        public SqlInt16 Adjust(SqlInt16 value);

-        public SqlInt32 Adjust(SqlInt32 value);

-        public SqlInt64 Adjust(SqlInt64 value);

-        public SqlMoney Adjust(SqlMoney value);

-        public SqlSingle Adjust(SqlSingle value);

-        public SqlString Adjust(SqlString value);

-        public SqlXml Adjust(SqlXml value);

-        public DateTime Adjust(DateTime value);

-        public DateTimeOffset Adjust(DateTimeOffset value);

-        public decimal Adjust(decimal value);

-        public double Adjust(double value);

-        public Guid Adjust(Guid value);

-        public short Adjust(short value);

-        public int Adjust(int value);

-        public long Adjust(long value);

-        public object Adjust(object value);

-        public float Adjust(float value);

-        public string Adjust(string value);

-        public TimeSpan Adjust(TimeSpan value);

-        public static SqlMetaData InferFromValue(object value, string name);

-    }
-    public sealed class SqlMethodAttribute : SqlFunctionAttribute {
 {
-        public SqlMethodAttribute();

-        public bool InvokeIfReceiverIsNull { get; set; }

-        public bool IsMutator { get; set; }

-        public bool OnNullCall { get; set; }

-    }
-    public sealed class SqlUserDefinedAggregateAttribute : Attribute {
 {
-        public const int MaxByteSizeValue = 8000;

-        public SqlUserDefinedAggregateAttribute(Format format);

-        public Format Format { get; }

-        public bool IsInvariantToDuplicates { get; set; }

-        public bool IsInvariantToNulls { get; set; }

-        public bool IsInvariantToOrder { get; set; }

-        public bool IsNullIfEmpty { get; set; }

-        public int MaxByteSize { get; set; }

-        public string Name { get; set; }

-    }
-    public sealed class SqlUserDefinedTypeAttribute : Attribute {
 {
-        public SqlUserDefinedTypeAttribute(Format format);

-        public Format Format { get; }

-        public bool IsByteOrdered { get; set; }

-        public bool IsFixedLength { get; set; }

-        public int MaxByteSize { get; set; }

-        public string Name { get; set; }

-        public string ValidationMethodName { get; set; }

-    }
-    public enum SystemDataAccessKind {
 {
-        None = 0,

-        Read = 1,

-    }
-}
```

