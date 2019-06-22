# System.Data.SqlClient

``` diff
 namespace System.Data.SqlClient {
-    public enum ApplicationIntent {
 {
-        ReadOnly = 1,

-        ReadWrite = 0,

-    }
-    public delegate void OnChangeEventHandler(object sender, SqlNotificationEventArgs e);

-    public enum SortOrder {
 {
-        Ascending = 0,

-        Descending = 1,

-        Unspecified = -1,

-    }
-    public sealed class SqlBulkCopy : IDisposable {
 {
-        public SqlBulkCopy(SqlConnection connection);

-        public SqlBulkCopy(SqlConnection connection, SqlBulkCopyOptions copyOptions, SqlTransaction externalTransaction);

-        public SqlBulkCopy(string connectionString);

-        public SqlBulkCopy(string connectionString, SqlBulkCopyOptions copyOptions);

-        public int BatchSize { get; set; }

-        public int BulkCopyTimeout { get; set; }

-        public SqlBulkCopyColumnMappingCollection ColumnMappings { get; }

-        public string DestinationTableName { get; set; }

-        public bool EnableStreaming { get; set; }

-        public int NotifyAfter { get; set; }

-        public event SqlRowsCopiedEventHandler SqlRowsCopied;

-        public void Close();

-        void System.IDisposable.Dispose();

-        public void WriteToServer(DbDataReader reader);

-        public void WriteToServer(DataRow[] rows);

-        public void WriteToServer(DataTable table);

-        public void WriteToServer(DataTable table, DataRowState rowState);

-        public void WriteToServer(IDataReader reader);

-        public Task WriteToServerAsync(DbDataReader reader);

-        public Task WriteToServerAsync(DbDataReader reader, CancellationToken cancellationToken);

-        public Task WriteToServerAsync(DataRow[] rows);

-        public Task WriteToServerAsync(DataRow[] rows, CancellationToken cancellationToken);

-        public Task WriteToServerAsync(DataTable table);

-        public Task WriteToServerAsync(DataTable table, DataRowState rowState);

-        public Task WriteToServerAsync(DataTable table, DataRowState rowState, CancellationToken cancellationToken);

-        public Task WriteToServerAsync(DataTable table, CancellationToken cancellationToken);

-        public Task WriteToServerAsync(IDataReader reader);

-        public Task WriteToServerAsync(IDataReader reader, CancellationToken cancellationToken);

-    }
-    public sealed class SqlBulkCopyColumnMapping {
 {
-        public SqlBulkCopyColumnMapping();

-        public SqlBulkCopyColumnMapping(int sourceColumnOrdinal, int destinationOrdinal);

-        public SqlBulkCopyColumnMapping(int sourceColumnOrdinal, string destinationColumn);

-        public SqlBulkCopyColumnMapping(string sourceColumn, int destinationOrdinal);

-        public SqlBulkCopyColumnMapping(string sourceColumn, string destinationColumn);

-        public string DestinationColumn { get; set; }

-        public int DestinationOrdinal { get; set; }

-        public string SourceColumn { get; set; }

-        public int SourceOrdinal { get; set; }

-    }
-    public sealed class SqlBulkCopyColumnMappingCollection : CollectionBase {
 {
-        public SqlBulkCopyColumnMapping this[int index] { get; }

-        public SqlBulkCopyColumnMapping Add(SqlBulkCopyColumnMapping bulkCopyColumnMapping);

-        public SqlBulkCopyColumnMapping Add(int sourceColumnIndex, int destinationColumnIndex);

-        public SqlBulkCopyColumnMapping Add(int sourceColumnIndex, string destinationColumn);

-        public SqlBulkCopyColumnMapping Add(string sourceColumn, int destinationColumnIndex);

-        public SqlBulkCopyColumnMapping Add(string sourceColumn, string destinationColumn);

-        public void Clear();

-        public bool Contains(SqlBulkCopyColumnMapping value);

-        public void CopyTo(SqlBulkCopyColumnMapping[] array, int index);

-        public int IndexOf(SqlBulkCopyColumnMapping value);

-        public void Insert(int index, SqlBulkCopyColumnMapping value);

-        public void Remove(SqlBulkCopyColumnMapping value);

-        public void RemoveAt(int index);

-    }
-    public enum SqlBulkCopyOptions {
 {
-        CheckConstraints = 2,

-        Default = 0,

-        FireTriggers = 16,

-        KeepIdentity = 1,

-        KeepNulls = 8,

-        TableLock = 4,

-        UseInternalTransaction = 32,

-    }
-    public sealed class SqlClientFactory : DbProviderFactory {
 {
-        public static readonly SqlClientFactory Instance;

-        public override DbCommand CreateCommand();

-        public override DbCommandBuilder CreateCommandBuilder();

-        public override DbConnection CreateConnection();

-        public override DbConnectionStringBuilder CreateConnectionStringBuilder();

-        public override DbDataAdapter CreateDataAdapter();

-        public override DbParameter CreateParameter();

-    }
-    public static class SqlClientMetaDataCollectionNames {
 {
-        public static readonly string Columns;

-        public static readonly string Databases;

-        public static readonly string ForeignKeys;

-        public static readonly string IndexColumns;

-        public static readonly string Indexes;

-        public static readonly string Parameters;

-        public static readonly string ProcedureColumns;

-        public static readonly string Procedures;

-        public static readonly string Tables;

-        public static readonly string UserDefinedTypes;

-        public static readonly string Users;

-        public static readonly string ViewColumns;

-        public static readonly string Views;

-    }
     public sealed class SqlClientPermission : DBDataPermission {
         public SqlClientPermission();
         public SqlClientPermission(PermissionState state);
         public SqlClientPermission(PermissionState state, bool allowBlankPassword);
         public override void Add(string connectionString, string restrictions, KeyRestrictionBehavior behavior);
         public override IPermission Copy();
     }
     public sealed class SqlClientPermissionAttribute : DBDataPermissionAttribute {
         public SqlClientPermissionAttribute(SecurityAction action);
         public override IPermission CreatePermission();
     }
-    public sealed class SqlCommand : DbCommand, ICloneable {
 {
-        public SqlCommand();

-        public SqlCommand(string cmdText);

-        public SqlCommand(string cmdText, SqlConnection connection);

-        public SqlCommand(string cmdText, SqlConnection connection, SqlTransaction transaction);

-        public override string CommandText { get; set; }

-        public override int CommandTimeout { get; set; }

-        public override CommandType CommandType { get; set; }

-        public SqlConnection Connection { get; set; }

-        protected override DbConnection DbConnection { get; set; }

-        protected override DbParameterCollection DbParameterCollection { get; }

-        protected override DbTransaction DbTransaction { get; set; }

-        public override bool DesignTimeVisible { get; set; }

-        public SqlNotificationRequest Notification { get; set; }

-        public SqlParameterCollection Parameters { get; }

-        public SqlTransaction Transaction { get; set; }

-        public override UpdateRowSource UpdatedRowSource { get; set; }

-        public event StatementCompletedEventHandler StatementCompleted;

-        public IAsyncResult BeginExecuteNonQuery();

-        public IAsyncResult BeginExecuteNonQuery(AsyncCallback callback, object stateObject);

-        public IAsyncResult BeginExecuteXmlReader();

-        public IAsyncResult BeginExecuteXmlReader(AsyncCallback callback, object stateObject);

-        public override void Cancel();

-        public SqlCommand Clone();

-        protected override DbParameter CreateDbParameter();

-        public SqlParameter CreateParameter();

-        protected override void Dispose(bool disposing);

-        public int EndExecuteNonQuery(IAsyncResult asyncResult);

-        public XmlReader EndExecuteXmlReader(IAsyncResult asyncResult);

-        protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior);

-        protected override Task<DbDataReader> ExecuteDbDataReaderAsync(CommandBehavior behavior, CancellationToken cancellationToken);

-        public override int ExecuteNonQuery();

-        public override Task<int> ExecuteNonQueryAsync(CancellationToken cancellationToken);

-        public SqlDataReader ExecuteReader();

-        public SqlDataReader ExecuteReader(CommandBehavior behavior);

-        public Task<SqlDataReader> ExecuteReaderAsync();

-        public Task<SqlDataReader> ExecuteReaderAsync(CommandBehavior behavior);

-        public Task<SqlDataReader> ExecuteReaderAsync(CommandBehavior behavior, CancellationToken cancellationToken);

-        public Task<SqlDataReader> ExecuteReaderAsync(CancellationToken cancellationToken);

-        public override object ExecuteScalar();

-        public override Task<object> ExecuteScalarAsync(CancellationToken cancellationToken);

-        public XmlReader ExecuteXmlReader();

-        public Task<XmlReader> ExecuteXmlReaderAsync();

-        public Task<XmlReader> ExecuteXmlReaderAsync(CancellationToken cancellationToken);

-        public override void Prepare();

-        public void ResetCommandTimeout();

-        object System.ICloneable.Clone();

-    }
-    public sealed class SqlCommandBuilder : DbCommandBuilder {
 {
-        public SqlCommandBuilder();

-        public SqlCommandBuilder(SqlDataAdapter adapter);

-        public override CatalogLocation CatalogLocation { get; set; }

-        public override string CatalogSeparator { get; set; }

-        public SqlDataAdapter DataAdapter { get; set; }

-        public override string QuotePrefix { get; set; }

-        public override string QuoteSuffix { get; set; }

-        public override string SchemaSeparator { get; set; }

-        protected override void ApplyParameterInfo(DbParameter parameter, DataRow datarow, StatementType statementType, bool whereClause);

-        public static void DeriveParameters(SqlCommand command);

-        public SqlCommand GetDeleteCommand();

-        public SqlCommand GetDeleteCommand(bool useColumnsForParameterNames);

-        public SqlCommand GetInsertCommand();

-        public SqlCommand GetInsertCommand(bool useColumnsForParameterNames);

-        protected override string GetParameterName(int parameterOrdinal);

-        protected override string GetParameterName(string parameterName);

-        protected override string GetParameterPlaceholder(int parameterOrdinal);

-        protected override DataTable GetSchemaTable(DbCommand srcCommand);

-        public SqlCommand GetUpdateCommand();

-        public SqlCommand GetUpdateCommand(bool useColumnsForParameterNames);

-        protected override DbCommand InitializeCommand(DbCommand command);

-        public override string QuoteIdentifier(string unquotedIdentifier);

-        protected override void SetRowUpdatingHandler(DbDataAdapter adapter);

-        public override string UnquoteIdentifier(string quotedIdentifier);

-    }
-    public sealed class SqlConnection : DbConnection, ICloneable {
 {
-        public SqlConnection();

-        public SqlConnection(string connectionString);

-        public SqlConnection(string connectionString, SqlCredential credential);

-        public string AccessToken { get; set; }

-        public Guid ClientConnectionId { get; }

-        public override string ConnectionString { get; set; }

-        public override int ConnectionTimeout { get; }

-        public SqlCredential Credential { get; set; }

-        public override string Database { get; }

-        public override string DataSource { get; }

-        protected override DbProviderFactory DbProviderFactory { get; }

-        public bool FireInfoMessageEventOnUserErrors { get; set; }

-        public int PacketSize { get; }

-        public override string ServerVersion { get; }

-        public override ConnectionState State { get; }

-        public bool StatisticsEnabled { get; set; }

-        public string WorkstationId { get; }

-        public event SqlInfoMessageEventHandler InfoMessage;

-        protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel);

-        public SqlTransaction BeginTransaction();

-        public SqlTransaction BeginTransaction(IsolationLevel iso);

-        public SqlTransaction BeginTransaction(IsolationLevel iso, string transactionName);

-        public SqlTransaction BeginTransaction(string transactionName);

-        public override void ChangeDatabase(string database);

-        public static void ChangePassword(string connectionString, SqlCredential credential, SecureString newSecurePassword);

-        public static void ChangePassword(string connectionString, string newPassword);

-        public static void ClearAllPools();

-        public static void ClearPool(SqlConnection connection);

-        public override void Close();

-        public SqlCommand CreateCommand();

-        protected override DbCommand CreateDbCommand();

-        protected override void Dispose(bool disposing);

-        public override void EnlistTransaction(Transaction transaction);

-        public override DataTable GetSchema();

-        public override DataTable GetSchema(string collectionName);

-        public override DataTable GetSchema(string collectionName, string[] restrictionValues);

-        protected override void OnStateChange(StateChangeEventArgs stateChange);

-        public override void Open();

-        public override Task OpenAsync(CancellationToken cancellationToken);

-        public void ResetStatistics();

-        public IDictionary RetrieveStatistics();

-        object System.ICloneable.Clone();

-    }
-    public sealed class SqlConnectionStringBuilder : DbConnectionStringBuilder {
 {
-        public SqlConnectionStringBuilder();

-        public SqlConnectionStringBuilder(string connectionString);

-        public ApplicationIntent ApplicationIntent { get; set; }

-        public string ApplicationName { get; set; }

-        public string AttachDBFilename { get; set; }

-        public int ConnectRetryCount { get; set; }

-        public int ConnectRetryInterval { get; set; }

-        public int ConnectTimeout { get; set; }

-        public string CurrentLanguage { get; set; }

-        public string DataSource { get; set; }

-        public bool Encrypt { get; set; }

-        public bool Enlist { get; set; }

-        public string FailoverPartner { get; set; }

-        public string InitialCatalog { get; set; }

-        public bool IntegratedSecurity { get; set; }

-        public override ICollection Keys { get; }

-        public int LoadBalanceTimeout { get; set; }

-        public int MaxPoolSize { get; set; }

-        public int MinPoolSize { get; set; }

-        public bool MultipleActiveResultSets { get; set; }

-        public bool MultiSubnetFailover { get; set; }

-        public int PacketSize { get; set; }

-        public string Password { get; set; }

-        public bool PersistSecurityInfo { get; set; }

-        public bool Pooling { get; set; }

-        public bool Replication { get; set; }

-        public override object this[string keyword] { get; set; }

-        public string TransactionBinding { get; set; }

-        public bool TrustServerCertificate { get; set; }

-        public string TypeSystemVersion { get; set; }

-        public string UserID { get; set; }

-        public bool UserInstance { get; set; }

-        public override ICollection Values { get; }

-        public string WorkstationID { get; set; }

-        public override void Clear();

-        public override bool ContainsKey(string keyword);

-        public override bool Remove(string keyword);

-        public override bool ShouldSerialize(string keyword);

-        public override bool TryGetValue(string keyword, out object value);

-    }
-    public sealed class SqlCredential {
 {
-        public SqlCredential(string userId, SecureString password);

-        public SecureString Password { get; }

-        public string UserId { get; }

-    }
-    public sealed class SqlDataAdapter : DbDataAdapter, ICloneable, IDataAdapter, IDbDataAdapter {
 {
-        public SqlDataAdapter();

-        public SqlDataAdapter(SqlCommand selectCommand);

-        public SqlDataAdapter(string selectCommandText, SqlConnection selectConnection);

-        public SqlDataAdapter(string selectCommandText, string selectConnectionString);

-        public SqlCommand DeleteCommand { get; set; }

-        public SqlCommand InsertCommand { get; set; }

-        public SqlCommand SelectCommand { get; set; }

-        IDbCommand System.Data.IDbDataAdapter.DeleteCommand { get; set; }

-        IDbCommand System.Data.IDbDataAdapter.InsertCommand { get; set; }

-        IDbCommand System.Data.IDbDataAdapter.SelectCommand { get; set; }

-        IDbCommand System.Data.IDbDataAdapter.UpdateCommand { get; set; }

-        public override int UpdateBatchSize { get; set; }

-        public SqlCommand UpdateCommand { get; set; }

-        public event SqlRowUpdatedEventHandler RowUpdated;

-        public event SqlRowUpdatingEventHandler RowUpdating;

-        protected override int AddToBatch(IDbCommand command);

-        protected override void ClearBatch();

-        protected override RowUpdatedEventArgs CreateRowUpdatedEvent(DataRow dataRow, IDbCommand command, StatementType statementType, DataTableMapping tableMapping);

-        protected override RowUpdatingEventArgs CreateRowUpdatingEvent(DataRow dataRow, IDbCommand command, StatementType statementType, DataTableMapping tableMapping);

-        protected override int ExecuteBatch();

-        protected override IDataParameter GetBatchedParameter(int commandIdentifier, int parameterIndex);

-        protected override bool GetBatchedRecordsAffected(int commandIdentifier, out int recordsAffected, out Exception error);

-        protected override void InitializeBatching();

-        protected override void OnRowUpdated(RowUpdatedEventArgs value);

-        protected override void OnRowUpdating(RowUpdatingEventArgs value);

-        object System.ICloneable.Clone();

-        protected override void TerminateBatching();

-    }
-    public class SqlDataReader : DbDataReader, IDataReader, IDataRecord, IDbColumnSchemaGenerator, IDisposable {
 {
-        protected SqlConnection Connection { get; }

-        public override int Depth { get; }

-        public override int FieldCount { get; }

-        public override bool HasRows { get; }

-        public override bool IsClosed { get; }

-        public override int RecordsAffected { get; }

-        public override object this[int i] { get; }

-        public override object this[string name] { get; }

-        public override int VisibleFieldCount { get; }

-        public override void Close();

-        protected override void Dispose(bool disposing);

-        public override bool GetBoolean(int i);

-        public override byte GetByte(int i);

-        public override long GetBytes(int i, long dataIndex, byte[] buffer, int bufferIndex, int length);

-        public override char GetChar(int i);

-        public override long GetChars(int i, long dataIndex, char[] buffer, int bufferIndex, int length);

-        public ReadOnlyCollection<DbColumn> GetColumnSchema();

-        public override string GetDataTypeName(int i);

-        public override DateTime GetDateTime(int i);

-        public virtual DateTimeOffset GetDateTimeOffset(int i);

-        public override decimal GetDecimal(int i);

-        public override double GetDouble(int i);

-        public override IEnumerator GetEnumerator();

-        public override Type GetFieldType(int i);

-        public override T GetFieldValue<T>(int i);

-        public override Task<T> GetFieldValueAsync<T>(int i, CancellationToken cancellationToken);

-        public override float GetFloat(int i);

-        public override Guid GetGuid(int i);

-        public override short GetInt16(int i);

-        public override int GetInt32(int i);

-        public override long GetInt64(int i);

-        public override string GetName(int i);

-        public override int GetOrdinal(string name);

-        public override Type GetProviderSpecificFieldType(int i);

-        public override object GetProviderSpecificValue(int i);

-        public override int GetProviderSpecificValues(object[] values);

-        public override DataTable GetSchemaTable();

-        public virtual SqlBinary GetSqlBinary(int i);

-        public virtual SqlBoolean GetSqlBoolean(int i);

-        public virtual SqlByte GetSqlByte(int i);

-        public virtual SqlBytes GetSqlBytes(int i);

-        public virtual SqlChars GetSqlChars(int i);

-        public virtual SqlDateTime GetSqlDateTime(int i);

-        public virtual SqlDecimal GetSqlDecimal(int i);

-        public virtual SqlDouble GetSqlDouble(int i);

-        public virtual SqlGuid GetSqlGuid(int i);

-        public virtual SqlInt16 GetSqlInt16(int i);

-        public virtual SqlInt32 GetSqlInt32(int i);

-        public virtual SqlInt64 GetSqlInt64(int i);

-        public virtual SqlMoney GetSqlMoney(int i);

-        public virtual SqlSingle GetSqlSingle(int i);

-        public virtual SqlString GetSqlString(int i);

-        public virtual object GetSqlValue(int i);

-        public virtual int GetSqlValues(object[] values);

-        public virtual SqlXml GetSqlXml(int i);

-        public override Stream GetStream(int i);

-        public override string GetString(int i);

-        public override TextReader GetTextReader(int i);

-        public virtual TimeSpan GetTimeSpan(int i);

-        public override object GetValue(int i);

-        public override int GetValues(object[] values);

-        public virtual XmlReader GetXmlReader(int i);

-        protected internal bool IsCommandBehavior(CommandBehavior condition);

-        public override bool IsDBNull(int i);

-        public override Task<bool> IsDBNullAsync(int i, CancellationToken cancellationToken);

-        public override bool NextResult();

-        public override Task<bool> NextResultAsync(CancellationToken cancellationToken);

-        public override bool Read();

-        public override Task<bool> ReadAsync(CancellationToken cancellationToken);

-    }
-    public sealed class SqlDependency {
 {
-        public SqlDependency();

-        public SqlDependency(SqlCommand command);

-        public SqlDependency(SqlCommand command, string options, int timeout);

-        public bool HasChanges { get; }

-        public string Id { get; }

-        public event OnChangeEventHandler OnChange;

-        public void AddCommandDependency(SqlCommand command);

-        public static bool Start(string connectionString);

-        public static bool Start(string connectionString, string queue);

-        public static bool Stop(string connectionString);

-        public static bool Stop(string connectionString, string queue);

-    }
-    public sealed class SqlError {
 {
-        public byte Class { get; }

-        public int LineNumber { get; }

-        public string Message { get; }

-        public int Number { get; }

-        public string Procedure { get; }

-        public string Server { get; }

-        public string Source { get; }

-        public byte State { get; }

-        public override string ToString();

-    }
-    public sealed class SqlErrorCollection : ICollection, IEnumerable {
 {
-        public int Count { get; }

-        bool System.Collections.ICollection.IsSynchronized { get; }

-        object System.Collections.ICollection.SyncRoot { get; }

-        public SqlError this[int index] { get; }

-        public void CopyTo(Array array, int index);

-        public void CopyTo(SqlError[] array, int index);

-        public IEnumerator GetEnumerator();

-    }
-    public sealed class SqlException : DbException {
 {
-        public byte Class { get; }

-        public Guid ClientConnectionId { get; }

-        public SqlErrorCollection Errors { get; }

-        public int LineNumber { get; }

-        public int Number { get; }

-        public string Procedure { get; }

-        public string Server { get; }

-        public override string Source { get; }

-        public byte State { get; }

-        public override void GetObjectData(SerializationInfo si, StreamingContext context);

-        public override string ToString();

-    }
-    public sealed class SqlInfoMessageEventArgs : EventArgs {
 {
-        public SqlErrorCollection Errors { get; }

-        public string Message { get; }

-        public string Source { get; }

-        public override string ToString();

-    }
-    public delegate void SqlInfoMessageEventHandler(object sender, SqlInfoMessageEventArgs e);

-    public class SqlNotificationEventArgs : EventArgs {
 {
-        public SqlNotificationEventArgs(SqlNotificationType type, SqlNotificationInfo info, SqlNotificationSource source);

-        public SqlNotificationInfo Info { get; }

-        public SqlNotificationSource Source { get; }

-        public SqlNotificationType Type { get; }

-    }
-    public enum SqlNotificationInfo {
 {
-        AlreadyChanged = -2,

-        Alter = 5,

-        Delete = 3,

-        Drop = 4,

-        Error = 7,

-        Expired = 12,

-        Insert = 1,

-        Invalid = 9,

-        Isolation = 11,

-        Merge = 16,

-        Options = 10,

-        PreviousFire = 14,

-        Query = 8,

-        Resource = 13,

-        Restart = 6,

-        TemplateLimit = 15,

-        Truncate = 0,

-        Unknown = -1,

-        Update = 2,

-    }
-    public enum SqlNotificationSource {
 {
-        Client = -2,

-        Data = 0,

-        Database = 3,

-        Environment = 6,

-        Execution = 7,

-        Object = 2,

-        Owner = 8,

-        Statement = 5,

-        System = 4,

-        Timeout = 1,

-        Unknown = -1,

-    }
-    public enum SqlNotificationType {
 {
-        Change = 0,

-        Subscribe = 1,

-        Unknown = -1,

-    }
-    public sealed class SqlParameter : DbParameter, ICloneable, IDataParameter, IDbDataParameter {
 {
-        public SqlParameter();

-        public SqlParameter(string parameterName, SqlDbType dbType);

-        public SqlParameter(string parameterName, SqlDbType dbType, int size);

-        public SqlParameter(string parameterName, SqlDbType dbType, int size, ParameterDirection direction, bool isNullable, byte precision, byte scale, string sourceColumn, DataRowVersion sourceVersion, object value);

-        public SqlParameter(string parameterName, SqlDbType dbType, int size, ParameterDirection direction, byte precision, byte scale, string sourceColumn, DataRowVersion sourceVersion, bool sourceColumnNullMapping, object value, string xmlSchemaCollectionDatabase, string xmlSchemaCollectionOwningSchema, string xmlSchemaCollectionName);

-        public SqlParameter(string parameterName, SqlDbType dbType, int size, string sourceColumn);

-        public SqlParameter(string parameterName, object value);

-        public SqlCompareOptions CompareInfo { get; set; }

-        public override DbType DbType { get; set; }

-        public override ParameterDirection Direction { get; set; }

-        public override bool IsNullable { get; set; }

-        public int LocaleId { get; set; }

-        public int Offset { get; set; }

-        public override string ParameterName { get; set; }

-        public byte Precision { get; set; }

-        public byte Scale { get; set; }

-        public override int Size { get; set; }

-        public override string SourceColumn { get; set; }

-        public override bool SourceColumnNullMapping { get; set; }

-        public override DataRowVersion SourceVersion { get; set; }

-        public SqlDbType SqlDbType { get; set; }

-        public object SqlValue { get; set; }

-        public string TypeName { get; set; }

-        public string UdtTypeName { get; set; }

-        public override object Value { get; set; }

-        public string XmlSchemaCollectionDatabase { get; set; }

-        public string XmlSchemaCollectionName { get; set; }

-        public string XmlSchemaCollectionOwningSchema { get; set; }

-        public override void ResetDbType();

-        public void ResetSqlDbType();

-        object System.ICloneable.Clone();

-        public override string ToString();

-    }
-    public sealed class SqlParameterCollection : DbParameterCollection {
 {
-        public override int Count { get; }

-        public override bool IsFixedSize { get; }

-        public override bool IsReadOnly { get; }

-        public override object SyncRoot { get; }

-        public SqlParameter this[int index] { get; set; }

-        public SqlParameter this[string parameterName] { get; set; }

-        public SqlParameter Add(SqlParameter value);

-        public override int Add(object value);

-        public SqlParameter Add(string parameterName, SqlDbType sqlDbType);

-        public SqlParameter Add(string parameterName, SqlDbType sqlDbType, int size);

-        public SqlParameter Add(string parameterName, SqlDbType sqlDbType, int size, string sourceColumn);

-        public override void AddRange(Array values);

-        public void AddRange(SqlParameter[] values);

-        public SqlParameter AddWithValue(string parameterName, object value);

-        public override void Clear();

-        public bool Contains(SqlParameter value);

-        public override bool Contains(object value);

-        public override bool Contains(string value);

-        public override void CopyTo(Array array, int index);

-        public void CopyTo(SqlParameter[] array, int index);

-        public override IEnumerator GetEnumerator();

-        protected override DbParameter GetParameter(int index);

-        protected override DbParameter GetParameter(string parameterName);

-        public int IndexOf(SqlParameter value);

-        public override int IndexOf(object value);

-        public override int IndexOf(string parameterName);

-        public void Insert(int index, SqlParameter value);

-        public override void Insert(int index, object value);

-        public void Remove(SqlParameter value);

-        public override void Remove(object value);

-        public override void RemoveAt(int index);

-        public override void RemoveAt(string parameterName);

-        protected override void SetParameter(int index, DbParameter value);

-        protected override void SetParameter(string parameterName, DbParameter value);

-    }
-    public class SqlRowsCopiedEventArgs : EventArgs {
 {
-        public SqlRowsCopiedEventArgs(long rowsCopied);

-        public bool Abort { get; set; }

-        public long RowsCopied { get; }

-    }
-    public delegate void SqlRowsCopiedEventHandler(object sender, SqlRowsCopiedEventArgs e);

-    public sealed class SqlRowUpdatedEventArgs : RowUpdatedEventArgs {
 {
-        public SqlRowUpdatedEventArgs(DataRow row, IDbCommand command, StatementType statementType, DataTableMapping tableMapping);

-        public SqlCommand Command { get; }

-    }
-    public delegate void SqlRowUpdatedEventHandler(object sender, SqlRowUpdatedEventArgs e);

-    public sealed class SqlRowUpdatingEventArgs : RowUpdatingEventArgs {
 {
-        public SqlRowUpdatingEventArgs(DataRow row, IDbCommand command, StatementType statementType, DataTableMapping tableMapping);

-        protected override IDbCommand BaseCommand { get; set; }

-        public SqlCommand Command { get; set; }

-    }
-    public delegate void SqlRowUpdatingEventHandler(object sender, SqlRowUpdatingEventArgs e);

-    public sealed class SqlTransaction : DbTransaction {
 {
-        public SqlConnection Connection { get; }

-        protected override DbConnection DbConnection { get; }

-        public override IsolationLevel IsolationLevel { get; }

-        public override void Commit();

-        protected override void Dispose(bool disposing);

-        public override void Rollback();

-        public void Rollback(string transactionName);

-        public void Save(string savePointName);

-    }
 }
```

