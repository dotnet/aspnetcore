# Microsoft.EntityFrameworkCore.SqlServer.Storage.Internal

``` diff
-namespace Microsoft.EntityFrameworkCore.SqlServer.Storage.Internal {
 {
-    public interface ISqlServerConnection : IDbContextTransactionManager, IDisposable, IRelationalConnection, IRelationalTransactionManager, IResettableService {
 {
-        ISqlServerConnection CreateMasterConnection();

-    }
-    public class SqlServerByteArrayTypeMapping : ByteArrayTypeMapping {
 {
-        protected SqlServerByteArrayTypeMapping(RelationalTypeMapping.RelationalTypeMappingParameters parameters);

-        public SqlServerByteArrayTypeMapping(string storeType = null, Nullable<int> size = default(Nullable<int>), bool fixedLength = false, ValueComparer comparer = null, Nullable<StoreTypePostfix> storeTypePostfix = default(Nullable<StoreTypePostfix>));

-        protected override RelationalTypeMapping Clone(RelationalTypeMapping.RelationalTypeMappingParameters parameters);

-        protected override void ConfigureParameter(DbParameter parameter);

-        protected override string GenerateNonNullSqlLiteral(object value);

-    }
-    public class SqlServerByteTypeMapping : ByteTypeMapping {
 {
-        protected SqlServerByteTypeMapping(RelationalTypeMapping.RelationalTypeMappingParameters parameters);

-        public SqlServerByteTypeMapping(string storeType, Nullable<DbType> dbType = default(Nullable<DbType>));

-        protected override RelationalTypeMapping Clone(RelationalTypeMapping.RelationalTypeMappingParameters parameters);

-        protected override string GenerateNonNullSqlLiteral(object value);

-    }
-    public class SqlServerConnection : RelationalConnection, IDbContextTransactionManager, IDisposable, IRelationalConnection, IRelationalTransactionManager, IResettableService, ISqlServerConnection {
 {
-        public SqlServerConnection(RelationalConnectionDependencies dependencies);

-        public override bool IsMultipleActiveResultSetsEnabled { get; }

-        protected override bool SupportsAmbientTransactions { get; }

-        protected override DbConnection CreateDbConnection();

-        public virtual ISqlServerConnection CreateMasterConnection();

-    }
-    public class SqlServerDatabaseCreator : RelationalDatabaseCreator {
 {
-        public SqlServerDatabaseCreator(RelationalDatabaseCreatorDependencies dependencies, ISqlServerConnection connection, IRawSqlCommandBuilder rawSqlCommandBuilder);

-        public virtual TimeSpan RetryDelay { get; set; }

-        public virtual TimeSpan RetryTimeout { get; set; }

-        public override void Create();

-        public override Task CreateAsync(CancellationToken cancellationToken = default(CancellationToken));

-        public override void Delete();

-        public override Task DeleteAsync(CancellationToken cancellationToken = default(CancellationToken));

-        public override bool Exists();

-        public override Task<bool> ExistsAsync(CancellationToken cancellationToken = default(CancellationToken));

-        protected override bool HasTables();

-        protected override Task<bool> HasTablesAsync(CancellationToken cancellationToken = default(CancellationToken));

-    }
-    public class SqlServerDateTimeOffsetTypeMapping : DateTimeOffsetTypeMapping {
 {
-        protected SqlServerDateTimeOffsetTypeMapping(RelationalTypeMapping.RelationalTypeMappingParameters parameters);

-        public SqlServerDateTimeOffsetTypeMapping(string storeType, Nullable<DbType> dbType = 27);

-        protected override string SqlLiteralFormatString { get; }

-        protected override RelationalTypeMapping Clone(RelationalTypeMapping.RelationalTypeMappingParameters parameters);

-        protected override void ConfigureParameter(DbParameter parameter);

-    }
-    public class SqlServerDateTimeTypeMapping : DateTimeTypeMapping {
 {
-        protected SqlServerDateTimeTypeMapping(RelationalTypeMapping.RelationalTypeMappingParameters parameters);

-        public SqlServerDateTimeTypeMapping(string storeType, Nullable<DbType> dbType = default(Nullable<DbType>));

-        protected override string SqlLiteralFormatString { get; }

-        protected override RelationalTypeMapping Clone(RelationalTypeMapping.RelationalTypeMappingParameters parameters);

-        protected override void ConfigureParameter(DbParameter parameter);

-    }
-    public class SqlServerDecimalTypeMapping : DecimalTypeMapping {
 {
-        protected SqlServerDecimalTypeMapping(RelationalTypeMapping.RelationalTypeMappingParameters parameters);

-        public SqlServerDecimalTypeMapping(string storeType, Nullable<DbType> dbType = default(Nullable<DbType>), Nullable<int> precision = default(Nullable<int>), Nullable<int> scale = default(Nullable<int>), StoreTypePostfix storeTypePostfix = StoreTypePostfix.None);

-        protected override RelationalTypeMapping Clone(RelationalTypeMapping.RelationalTypeMappingParameters parameters);

-        protected override void ConfigureParameter(DbParameter parameter);

-    }
-    public class SqlServerDoubleTypeMapping : DoubleTypeMapping {
 {
-        protected SqlServerDoubleTypeMapping(RelationalTypeMapping.RelationalTypeMappingParameters parameters);

-        public SqlServerDoubleTypeMapping(string storeType, Nullable<DbType> dbType = default(Nullable<DbType>));

-        protected override RelationalTypeMapping Clone(RelationalTypeMapping.RelationalTypeMappingParameters parameters);

-        protected override void ConfigureParameter(DbParameter parameter);

-        protected override string GenerateNonNullSqlLiteral(object value);

-    }
-    public class SqlServerExecutionStrategy : IExecutionStrategy {
 {
-        public SqlServerExecutionStrategy(ExecutionStrategyDependencies dependencies);

-        public virtual bool RetriesOnFailure { get; }

-        public virtual TResult Execute<TState, TResult>(TState state, Func<DbContext, TState, TResult> operation, Func<DbContext, TState, ExecutionResult<TResult>> verifySucceeded);

-        public virtual Task<TResult> ExecuteAsync<TState, TResult>(TState state, Func<DbContext, TState, CancellationToken, Task<TResult>> operation, Func<DbContext, TState, CancellationToken, Task<ExecutionResult<TResult>>> verifySucceeded, CancellationToken cancellationToken);

-    }
-    public class SqlServerExecutionStrategyFactory : RelationalExecutionStrategyFactory {
 {
-        public SqlServerExecutionStrategyFactory(ExecutionStrategyDependencies dependencies);

-        protected override IExecutionStrategy CreateDefaultStrategy(ExecutionStrategyDependencies dependencies);

-    }
-    public class SqlServerFloatTypeMapping : FloatTypeMapping {
 {
-        protected SqlServerFloatTypeMapping(RelationalTypeMapping.RelationalTypeMappingParameters parameters);

-        public SqlServerFloatTypeMapping(string storeType, Nullable<DbType> dbType = default(Nullable<DbType>));

-        protected override RelationalTypeMapping Clone(RelationalTypeMapping.RelationalTypeMappingParameters parameters);

-        protected override void ConfigureParameter(DbParameter parameter);

-        protected override string GenerateNonNullSqlLiteral(object value);

-    }
-    public class SqlServerLongTypeMapping : LongTypeMapping {
 {
-        protected SqlServerLongTypeMapping(RelationalTypeMapping.RelationalTypeMappingParameters parameters);

-        public SqlServerLongTypeMapping(string storeType, Nullable<DbType> dbType = default(Nullable<DbType>));

-        protected override RelationalTypeMapping Clone(RelationalTypeMapping.RelationalTypeMappingParameters parameters);

-        protected override string GenerateNonNullSqlLiteral(object value);

-    }
-    public class SqlServerShortTypeMapping : ShortTypeMapping {
 {
-        protected SqlServerShortTypeMapping(RelationalTypeMapping.RelationalTypeMappingParameters parameters);

-        public SqlServerShortTypeMapping(string storeType, Nullable<DbType> dbType = default(Nullable<DbType>));

-        protected override RelationalTypeMapping Clone(RelationalTypeMapping.RelationalTypeMappingParameters parameters);

-        protected override string GenerateNonNullSqlLiteral(object value);

-    }
-    public class SqlServerSqlGenerationHelper : RelationalSqlGenerationHelper {
 {
-        public SqlServerSqlGenerationHelper(RelationalSqlGenerationHelperDependencies dependencies);

-        public override string BatchTerminator { get; }

-        public override string DelimitIdentifier(string identifier);

-        public override void DelimitIdentifier(StringBuilder builder, string identifier);

-        public override string EscapeIdentifier(string identifier);

-        public override void EscapeIdentifier(StringBuilder builder, string identifier);

-    }
-    public class SqlServerSqlVariantTypeMapping : RelationalTypeMapping {
 {
-        protected SqlServerSqlVariantTypeMapping(RelationalTypeMapping.RelationalTypeMappingParameters parameters);

-        public SqlServerSqlVariantTypeMapping(string storeType);

-        protected override RelationalTypeMapping Clone(RelationalTypeMapping.RelationalTypeMappingParameters parameters);

-    }
-    public class SqlServerStringTypeMapping : StringTypeMapping {
 {
-        protected SqlServerStringTypeMapping(RelationalTypeMapping.RelationalTypeMappingParameters parameters);

-        public SqlServerStringTypeMapping(string storeType = null, bool unicode = false, Nullable<int> size = default(Nullable<int>), bool fixedLength = false, Nullable<StoreTypePostfix> storeTypePostfix = default(Nullable<StoreTypePostfix>));

-        protected override RelationalTypeMapping Clone(RelationalTypeMapping.RelationalTypeMappingParameters parameters);

-        protected override void ConfigureParameter(DbParameter parameter);

-        protected override string GenerateNonNullSqlLiteral(object value);

-    }
-    public class SqlServerTimeSpanTypeMapping : TimeSpanTypeMapping {
 {
-        protected SqlServerTimeSpanTypeMapping(RelationalTypeMapping.RelationalTypeMappingParameters parameters);

-        public SqlServerTimeSpanTypeMapping(string storeType, Nullable<DbType> dbType = default(Nullable<DbType>));

-        protected override RelationalTypeMapping Clone(RelationalTypeMapping.RelationalTypeMappingParameters parameters);

-        protected override void ConfigureParameter(DbParameter parameter);

-    }
-    public static class SqlServerTransientExceptionDetector {
 {
-        public static bool ShouldRetryOn(Exception ex);

-    }
-    public class SqlServerTypeMappingSource : RelationalTypeMappingSource {
 {
-        public SqlServerTypeMappingSource(TypeMappingSourceDependencies dependencies, RelationalTypeMappingSourceDependencies relationalDependencies);

-        protected override RelationalTypeMapping FindMapping(in RelationalTypeMappingInfo mappingInfo);

-        protected override void ValidateMapping(CoreTypeMapping mapping, IProperty property);

-    }
-    public class SqlServerUdtTypeMapping : RelationalTypeMapping {
 {
-        protected SqlServerUdtTypeMapping(RelationalTypeMapping.RelationalTypeMappingParameters parameters, Func<object, Expression> literalGenerator, string udtTypeName);

-        public SqlServerUdtTypeMapping(Type clrType, string storeType, Func<object, Expression> literalGenerator, StoreTypePostfix storeTypePostfix = StoreTypePostfix.None, string udtTypeName = null, ValueConverter converter = null, ValueComparer comparer = null, ValueComparer keyComparer = null, Nullable<DbType> dbType = default(Nullable<DbType>), bool unicode = false, Nullable<int> size = default(Nullable<int>), bool fixedLength = false, Nullable<int> precision = default(Nullable<int>), Nullable<int> scale = default(Nullable<int>));

-        public virtual Func<object, Expression> LiteralGenerator { get; }

-        public virtual string UdtTypeName { get; }

-        protected override RelationalTypeMapping Clone(RelationalTypeMapping.RelationalTypeMappingParameters parameters);

-        protected override void ConfigureParameter(DbParameter parameter);

-        public static SqlServerUdtTypeMapping CreateSqlHierarchyIdMapping(Type udtType);

-        public static SqlServerUdtTypeMapping CreateSqlSpatialMapping(Type udtType, string storeName);

-        public override Expression GenerateCodeLiteral(object value);

-    }
-}
```

