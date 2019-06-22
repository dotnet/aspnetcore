# Microsoft.EntityFrameworkCore.Storage

``` diff
-namespace Microsoft.EntityFrameworkCore.Storage {
 {
-    public class BoolTypeMapping : RelationalTypeMapping {
 {
-        protected BoolTypeMapping(RelationalTypeMapping.RelationalTypeMappingParameters parameters);

-        public BoolTypeMapping(string storeType, Nullable<DbType> dbType = default(Nullable<DbType>));

-        protected override RelationalTypeMapping Clone(RelationalTypeMapping.RelationalTypeMappingParameters parameters);

-        protected override string GenerateNonNullSqlLiteral(object value);

-    }
-    public class ByteArrayRelationalTypeMapper : IByteArrayRelationalTypeMapper {
 {
-        public ByteArrayRelationalTypeMapper(int maxBoundedLength, RelationalTypeMapping defaultMapping, RelationalTypeMapping unboundedMapping, RelationalTypeMapping keyMapping, RelationalTypeMapping rowVersionMapping, Func<int, RelationalTypeMapping> createBoundedMapping);

-        public virtual Func<int, RelationalTypeMapping> CreateBoundedMapping { get; }

-        public virtual RelationalTypeMapping DefaultMapping { get; }

-        public virtual RelationalTypeMapping KeyMapping { get; }

-        public virtual int MaxBoundedLength { get; }

-        public virtual RelationalTypeMapping RowVersionMapping { get; }

-        public virtual RelationalTypeMapping UnboundedMapping { get; }

-        public virtual RelationalTypeMapping FindMapping(bool rowVersion, bool keyOrIndex, Nullable<int> size);

-    }
-    public class ByteArrayTypeMapping : RelationalTypeMapping {
 {
-        protected ByteArrayTypeMapping(RelationalTypeMapping.RelationalTypeMappingParameters parameters);

-        public ByteArrayTypeMapping(string storeType, Nullable<DbType> dbType = default(Nullable<DbType>), Nullable<int> size = default(Nullable<int>));

-        protected override RelationalTypeMapping Clone(RelationalTypeMapping.RelationalTypeMappingParameters parameters);

-        protected override string GenerateNonNullSqlLiteral(object value);

-    }
-    public class ByteTypeMapping : RelationalTypeMapping {
 {
-        protected ByteTypeMapping(RelationalTypeMapping.RelationalTypeMappingParameters parameters);

-        public ByteTypeMapping(string storeType, Nullable<DbType> dbType = default(Nullable<DbType>));

-        protected override RelationalTypeMapping Clone(RelationalTypeMapping.RelationalTypeMappingParameters parameters);

-    }
-    public class CharTypeMapping : RelationalTypeMapping {
 {
-        protected CharTypeMapping(RelationalTypeMapping.RelationalTypeMappingParameters parameters);

-        public CharTypeMapping(string storeType, Nullable<DbType> dbType = default(Nullable<DbType>));

-        protected override string SqlLiteralFormatString { get; }

-        protected override RelationalTypeMapping Clone(RelationalTypeMapping.RelationalTypeMappingParameters parameters);

-    }
-    public class CoreTypeMapper : ITypeMapper {
 {
-        public CoreTypeMapper(CoreTypeMapperDependencies dependencies);

-        protected virtual CoreTypeMapperDependencies Dependencies { get; }

-        public virtual bool IsTypeMapped(Type type);

-    }
-    public sealed class CoreTypeMapperDependencies {
 {
-        public CoreTypeMapperDependencies(IValueConverterSelector valueConverterSelector);

-        public IValueConverterSelector ValueConverterSelector { get; }

-        public CoreTypeMapperDependencies With(IValueConverterSelector valueConverterSelector);

-    }
-    public abstract class CoreTypeMapping {
 {
-        protected CoreTypeMapping(CoreTypeMapping.CoreTypeMappingParameters parameters);

-        public virtual Type ClrType { get; }

-        public virtual ValueComparer Comparer { get; }

-        public virtual ValueConverter Converter { get; }

-        public virtual ValueComparer KeyComparer { get; }

-        protected virtual CoreTypeMapping.CoreTypeMappingParameters Parameters { get; }

-        public virtual ValueComparer StructuralComparer { get; }

-        public virtual Func<IProperty, IEntityType, ValueGenerator> ValueGeneratorFactory { get; }

-        public abstract CoreTypeMapping Clone(ValueConverter converter);

-        public virtual Expression GenerateCodeLiteral(object value);

-        protected readonly struct CoreTypeMappingParameters {
 {
-            public CoreTypeMappingParameters(Type clrType, ValueConverter converter, ValueComparer comparer, ValueComparer keyComparer, ValueComparer structuralComparer, Func<IProperty, IEntityType, ValueGenerator> valueGeneratorFactory);

-            public CoreTypeMappingParameters(Type clrType, ValueConverter converter = null, ValueComparer comparer = null, ValueComparer keyComparer = null, Func<IProperty, IEntityType, ValueGenerator> valueGeneratorFactory = null);

-            public Type ClrType { get; }

-            public ValueComparer Comparer { get; }

-            public ValueConverter Converter { get; }

-            public ValueComparer KeyComparer { get; }

-            public ValueComparer StructuralComparer { get; }

-            public Func<IProperty, IEntityType, ValueGenerator> ValueGeneratorFactory { get; }

-            public CoreTypeMapping.CoreTypeMappingParameters WithComposedConverter(ValueConverter converter);

-        }
-    }
-    public abstract class Database : IDatabase {
 {
-        protected Database(DatabaseDependencies dependencies);

-        protected virtual DatabaseDependencies Dependencies { get; }

-        public virtual Func<QueryContext, IAsyncEnumerable<TResult>> CompileAsyncQuery<TResult>(QueryModel queryModel);

-        public virtual Func<QueryContext, IEnumerable<TResult>> CompileQuery<TResult>(QueryModel queryModel);

-        public abstract int SaveChanges(IReadOnlyList<IUpdateEntry> entries);

-        public abstract Task<int> SaveChangesAsync(IReadOnlyList<IUpdateEntry> entries, CancellationToken cancellationToken = default(CancellationToken));

-    }
-    public sealed class DatabaseDependencies {
 {
-        public DatabaseDependencies(IQueryCompilationContextFactory queryCompilationContextFactory);

-        public IQueryCompilationContextFactory QueryCompilationContextFactory { get; }

-        public DatabaseDependencies With(IQueryCompilationContextFactory queryCompilationContextFactory);

-    }
-    public class DatabaseProvider<TOptionsExtension> : IDatabaseProvider where TOptionsExtension : class, IDbContextOptionsExtension {
 {
-        public DatabaseProvider(DatabaseProviderDependencies dependencies);

-        public virtual string Name { get; }

-        public virtual bool IsConfigured(IDbContextOptions options);

-    }
-    public sealed class DatabaseProviderDependencies {
 {
-        public DatabaseProviderDependencies();

-    }
-    public class DateTimeOffsetTypeMapping : RelationalTypeMapping {
 {
-        protected DateTimeOffsetTypeMapping(RelationalTypeMapping.RelationalTypeMappingParameters parameters);

-        public DateTimeOffsetTypeMapping(string storeType, Nullable<DbType> dbType = default(Nullable<DbType>));

-        protected override string SqlLiteralFormatString { get; }

-        protected override RelationalTypeMapping Clone(RelationalTypeMapping.RelationalTypeMappingParameters parameters);

-    }
-    public class DateTimeTypeMapping : RelationalTypeMapping {
 {
-        protected DateTimeTypeMapping(RelationalTypeMapping.RelationalTypeMappingParameters parameters);

-        public DateTimeTypeMapping(string storeType, Nullable<DbType> dbType = default(Nullable<DbType>));

-        protected override string SqlLiteralFormatString { get; }

-        protected override RelationalTypeMapping Clone(RelationalTypeMapping.RelationalTypeMappingParameters parameters);

-    }
-    public static class DbContextTransactionExtensions {
 {
-        public static DbTransaction GetDbTransaction(this IDbContextTransaction dbContextTransaction);

-    }
-    public class DecimalTypeMapping : RelationalTypeMapping {
 {
-        protected DecimalTypeMapping(RelationalTypeMapping.RelationalTypeMappingParameters parameters);

-        public DecimalTypeMapping(string storeType, Nullable<DbType> dbType = default(Nullable<DbType>));

-        protected override string SqlLiteralFormatString { get; }

-        protected override RelationalTypeMapping Clone(RelationalTypeMapping.RelationalTypeMappingParameters parameters);

-    }
-    public class DoubleTypeMapping : RelationalTypeMapping {
 {
-        protected DoubleTypeMapping(RelationalTypeMapping.RelationalTypeMappingParameters parameters);

-        public DoubleTypeMapping(string storeType, Nullable<DbType> dbType = default(Nullable<DbType>));

-        protected override RelationalTypeMapping Clone(RelationalTypeMapping.RelationalTypeMappingParameters parameters);

-        protected override string GenerateNonNullSqlLiteral(object value);

-    }
-    public class ExecutionResult<TResult> {
 {
-        public ExecutionResult(bool successful, TResult result);

-        public virtual bool IsSuccessful { get; }

-        public virtual TResult Result { get; }

-    }
-    public abstract class ExecutionStrategy : IExecutionStrategy {
 {
-        protected static readonly int DefaultMaxRetryCount;

-        protected static readonly TimeSpan DefaultMaxDelay;

-        protected ExecutionStrategy(DbContext context, int maxRetryCount, TimeSpan maxRetryDelay);

-        protected ExecutionStrategy(ExecutionStrategyDependencies dependencies, int maxRetryCount, TimeSpan maxRetryDelay);

-        protected virtual ExecutionStrategyDependencies Dependencies { get; }

-        protected virtual List<Exception> ExceptionsEncountered { get; }

-        protected virtual int MaxRetryCount { get; }

-        protected virtual TimeSpan MaxRetryDelay { get; }

-        protected virtual Random Random { get; }

-        public virtual bool RetriesOnFailure { get; }

-        protected static bool Suspended { get; set; }

-        public static TResult CallOnWrappedException<TResult>(Exception exception, Func<Exception, TResult> exceptionHandler);

-        public virtual TResult Execute<TState, TResult>(TState state, Func<DbContext, TState, TResult> operation, Func<DbContext, TState, ExecutionResult<TResult>> verifySucceeded);

-        public virtual Task<TResult> ExecuteAsync<TState, TResult>(TState state, Func<DbContext, TState, CancellationToken, Task<TResult>> operation, Func<DbContext, TState, CancellationToken, Task<ExecutionResult<TResult>>> verifySucceeded, CancellationToken cancellationToken = default(CancellationToken));

-        protected virtual Nullable<TimeSpan> GetNextDelay(Exception lastException);

-        protected virtual void OnFirstExecution();

-        protected virtual void OnRetry();

-        protected internal abstract bool ShouldRetryOn(Exception exception);

-        protected internal virtual bool ShouldVerifySuccessOn(Exception exception);

-    }
-    public sealed class ExecutionStrategyDependencies {
 {
-        public ExecutionStrategyDependencies(ICurrentDbContext currentDbContext, IDbContextOptions options, IDiagnosticsLogger<DbLoggerCategory.Infrastructure> logger);

-        public ICurrentDbContext CurrentDbContext { get; }

-        public IDiagnosticsLogger<DbLoggerCategory.Infrastructure> Logger { get; }

-        public IDbContextOptions Options { get; }

-        public ExecutionStrategyDependencies With(IDiagnosticsLogger<DbLoggerCategory.Infrastructure> logger);

-        public ExecutionStrategyDependencies With(IDbContextOptions options);

-        public ExecutionStrategyDependencies With(ICurrentDbContext currentDbContext);

-    }
-    public class FloatTypeMapping : RelationalTypeMapping {
 {
-        protected FloatTypeMapping(RelationalTypeMapping.RelationalTypeMappingParameters parameters);

-        public FloatTypeMapping(string storeType, Nullable<DbType> dbType = default(Nullable<DbType>));

-        protected override RelationalTypeMapping Clone(RelationalTypeMapping.RelationalTypeMappingParameters parameters);

-        protected override string GenerateNonNullSqlLiteral(object value);

-    }
-    public class GuidTypeMapping : RelationalTypeMapping {
 {
-        protected GuidTypeMapping(RelationalTypeMapping.RelationalTypeMappingParameters parameters);

-        public GuidTypeMapping(string storeType, Nullable<DbType> dbType = default(Nullable<DbType>));

-        protected override string SqlLiteralFormatString { get; }

-        protected override RelationalTypeMapping Clone(RelationalTypeMapping.RelationalTypeMappingParameters parameters);

-    }
-    public interface IByteArrayRelationalTypeMapper {
 {
-        RelationalTypeMapping FindMapping(bool rowVersion, bool keyOrIndex, Nullable<int> size);

-    }
-    public interface IDatabase {
 {
-        Func<QueryContext, IAsyncEnumerable<TResult>> CompileAsyncQuery<TResult>(QueryModel queryModel);

-        Func<QueryContext, IEnumerable<TResult>> CompileQuery<TResult>(QueryModel queryModel);

-        int SaveChanges(IReadOnlyList<IUpdateEntry> entries);

-        Task<int> SaveChangesAsync(IReadOnlyList<IUpdateEntry> entries, CancellationToken cancellationToken = default(CancellationToken));

-    }
-    public interface IDatabaseCreator {
 {
-        bool EnsureCreated();

-        Task<bool> EnsureCreatedAsync(CancellationToken cancellationToken = default(CancellationToken));

-        bool EnsureDeleted();

-        Task<bool> EnsureDeletedAsync(CancellationToken cancellationToken = default(CancellationToken));

-    }
-    public interface IDatabaseCreatorWithCanConnect : IDatabaseCreator {
 {
-        bool CanConnect();

-        Task<bool> CanConnectAsync(CancellationToken cancellationToken = default(CancellationToken));

-    }
-    public interface IDatabaseProvider {
 {
-        string Name { get; }

-        bool IsConfigured(IDbContextOptions options);

-    }
-    public interface IDbContextTransaction : IDisposable {
 {
-        Guid TransactionId { get; }

-        void Commit();

-        void Rollback();

-    }
-    public interface IDbContextTransactionManager : IResettableService {
 {
-        IDbContextTransaction CurrentTransaction { get; }

-        IDbContextTransaction BeginTransaction();

-        Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default(CancellationToken));

-        void CommitTransaction();

-        void RollbackTransaction();

-    }
-    public interface IExecutionStrategy {
 {
-        bool RetriesOnFailure { get; }

-        TResult Execute<TState, TResult>(TState state, Func<DbContext, TState, TResult> operation, Func<DbContext, TState, ExecutionResult<TResult>> verifySucceeded);

-        Task<TResult> ExecuteAsync<TState, TResult>(TState state, Func<DbContext, TState, CancellationToken, Task<TResult>> operation, Func<DbContext, TState, CancellationToken, Task<ExecutionResult<TResult>>> verifySucceeded, CancellationToken cancellationToken = default(CancellationToken));

-    }
-    public interface IExecutionStrategyFactory {
 {
-        IExecutionStrategy Create();

-    }
-    public sealed class InMemoryDatabaseRoot {
 {
-        public object Instance;

-        public InMemoryDatabaseRoot();

-    }
-    public class IntTypeMapping : RelationalTypeMapping {
 {
-        protected IntTypeMapping(RelationalTypeMapping.RelationalTypeMappingParameters parameters);

-        public IntTypeMapping(string storeType, Nullable<DbType> dbType = default(Nullable<DbType>));

-        protected override RelationalTypeMapping Clone(RelationalTypeMapping.RelationalTypeMappingParameters parameters);

-    }
-    public interface IParameterNameGeneratorFactory {
 {
-        ParameterNameGenerator Create();

-    }
-    public interface IRawSqlCommandBuilder {
 {
-        IRelationalCommand Build(string sql);

-        RawSqlCommand Build(string sql, IEnumerable<object> parameters);

-    }
-    public interface IRelationalCommand {
 {
-        string CommandText { get; }

-        IReadOnlyList<IRelationalParameter> Parameters { get; }

-        int ExecuteNonQuery(IRelationalConnection connection, IReadOnlyDictionary<string, object> parameterValues);

-        Task<int> ExecuteNonQueryAsync(IRelationalConnection connection, IReadOnlyDictionary<string, object> parameterValues, CancellationToken cancellationToken = default(CancellationToken));

-        RelationalDataReader ExecuteReader(IRelationalConnection connection, IReadOnlyDictionary<string, object> parameterValues);

-        Task<RelationalDataReader> ExecuteReaderAsync(IRelationalConnection connection, IReadOnlyDictionary<string, object> parameterValues, CancellationToken cancellationToken = default(CancellationToken));

-        object ExecuteScalar(IRelationalConnection connection, IReadOnlyDictionary<string, object> parameterValues);

-        Task<object> ExecuteScalarAsync(IRelationalConnection connection, IReadOnlyDictionary<string, object> parameterValues, CancellationToken cancellationToken = default(CancellationToken));

-    }
-    public interface IRelationalCommandBuilder : IInfrastructure<IndentedStringBuilder> {
 {
-        IRelationalParameterBuilder ParameterBuilder { get; }

-        IRelationalCommand Build();

-    }
-    public interface IRelationalCommandBuilderFactory {
 {
-        IRelationalCommandBuilder Create();

-    }
-    public interface IRelationalConnection : IDbContextTransactionManager, IDisposable, IRelationalTransactionManager, IResettableService {
 {
-        Nullable<int> CommandTimeout { get; set; }

-        Guid ConnectionId { get; }

-        string ConnectionString { get; }

-        new IDbContextTransaction CurrentTransaction { get; }

-        DbConnection DbConnection { get; }

-        bool IsMultipleActiveResultSetsEnabled { get; }

-        SemaphoreSlim Semaphore { get; }

-        bool Close();

-        bool Open(bool errorsExpected = false);

-        Task<bool> OpenAsync(CancellationToken cancellationToken, bool errorsExpected = false);

-        void RegisterBufferable(IBufferable bufferable);

-        Task RegisterBufferableAsync(IBufferable bufferable, CancellationToken cancellationToken);

-    }
-    public interface IRelationalDatabaseCreator : IDatabaseCreator {
 {
-        void Create();

-        Task CreateAsync(CancellationToken cancellationToken = default(CancellationToken));

-        void CreateTables();

-        Task CreateTablesAsync(CancellationToken cancellationToken = default(CancellationToken));

-        void Delete();

-        Task DeleteAsync(CancellationToken cancellationToken = default(CancellationToken));

-        bool Exists();

-        Task<bool> ExistsAsync(CancellationToken cancellationToken = default(CancellationToken));

-        string GenerateCreateScript();

-    }
-    public interface IRelationalParameter {
 {
-        string InvariantName { get; }

-        void AddDbParameter(DbCommand command, IReadOnlyDictionary<string, object> parameterValues);

-        void AddDbParameter(DbCommand command, object value);

-    }
-    public interface IRelationalParameterBuilder {
 {
-        IReadOnlyList<IRelationalParameter> Parameters { get; }

-        void AddCompositeParameter(string invariantName, Action<IRelationalParameterBuilder> buildAction);

-        void AddParameter(string invariantName, string name);

-        void AddParameter(string invariantName, string name, IProperty property);

-        void AddParameter(string invariantName, string name, RelationalTypeMapping typeMapping, bool nullable);

-        void AddPropertyParameter(string invariantName, string name, IProperty property);

-        void AddRawParameter(string invariantName, DbParameter dbParameter);

-    }
-    public interface IRelationalTransactionFactory {
 {
-        RelationalTransaction Create(IRelationalConnection connection, DbTransaction transaction, IDiagnosticsLogger<DbLoggerCategory.Database.Transaction> logger, bool transactionOwned);

-    }
-    public interface IRelationalTransactionManager : IDbContextTransactionManager, IResettableService {
 {
-        IDbContextTransaction BeginTransaction(IsolationLevel isolationLevel);

-        Task<IDbContextTransaction> BeginTransactionAsync(IsolationLevel isolationLevel, CancellationToken cancellationToken = default(CancellationToken));

-        IDbContextTransaction UseTransaction(DbTransaction transaction);

-    }
-    public interface IRelationalTypeMapper : ITypeMapper {
 {
-        IByteArrayRelationalTypeMapper ByteArrayMapper { get; }

-        IStringRelationalTypeMapper StringMapper { get; }

-        RelationalTypeMapping FindMapping(IProperty property);

-        RelationalTypeMapping FindMapping(string storeType);

-        RelationalTypeMapping FindMapping(Type clrType);

-        void ValidateTypeName(string storeType);

-    }
-    public interface IRelationalTypeMappingSource : ITypeMappingSource {
 {
-        new RelationalTypeMapping FindMapping(IProperty property);

-        new RelationalTypeMapping FindMapping(MemberInfo member);

-        RelationalTypeMapping FindMapping(string storeTypeName);

-        new RelationalTypeMapping FindMapping(Type type);

-        RelationalTypeMapping FindMapping(Type type, string storeTypeName, bool keyOrIndex = false, Nullable<bool> unicode = default(Nullable<bool>), Nullable<int> size = default(Nullable<int>), Nullable<bool> rowVersion = default(Nullable<bool>), Nullable<bool> fixedLength = default(Nullable<bool>), Nullable<int> precision = default(Nullable<int>), Nullable<int> scale = default(Nullable<int>));

-    }
-    public interface IRelationalTypeMappingSourcePlugin {
 {
-        RelationalTypeMapping FindMapping(in RelationalTypeMappingInfo mappingInfo);

-    }
-    public interface IRelationalValueBufferFactory {
 {
-        ValueBuffer Create(DbDataReader dataReader);

-    }
-    public interface IRelationalValueBufferFactoryFactory {
 {
-        IRelationalValueBufferFactory Create(IReadOnlyList<TypeMaterializationInfo> types);

-        IRelationalValueBufferFactory Create(IReadOnlyList<Type> valueTypes, IReadOnlyList<int> indexMap);

-    }
-    public interface ISqlGenerationHelper {
 {
-        string BatchTerminator { get; }

-        string StatementTerminator { get; }

-        string DelimitIdentifier(string identifier);

-        string DelimitIdentifier(string name, string schema);

-        void DelimitIdentifier(StringBuilder builder, string identifier);

-        void DelimitIdentifier(StringBuilder builder, string name, string schema);

-        string EscapeIdentifier(string identifier);

-        void EscapeIdentifier(StringBuilder builder, string identifier);

-        string EscapeLiteral(string literal);

-        void EscapeLiteral(StringBuilder builder, string literal);

-        string GenerateParameterName(string name);

-        void GenerateParameterName(StringBuilder builder, string name);

-        string GenerateParameterNamePlaceholder(string name);

-        void GenerateParameterNamePlaceholder(StringBuilder builder, string name);

-    }
-    public interface IStringRelationalTypeMapper {
 {
-        RelationalTypeMapping FindMapping(bool unicode, bool keyOrIndex, Nullable<int> maxLength);

-    }
-    public interface ITransactionEnlistmentManager {
 {
-        Transaction EnlistedTransaction { get; }

-        void EnlistTransaction(Transaction transaction);

-    }
-    public interface ITypeMapper {
 {
-        bool IsTypeMapped(Type clrType);

-    }
-    public interface ITypeMappingSource {
 {
-        CoreTypeMapping FindMapping(IProperty property);

-        CoreTypeMapping FindMapping(MemberInfo member);

-        CoreTypeMapping FindMapping(Type type);

-    }
-    public interface ITypeMappingSourcePlugin {
 {
-        CoreTypeMapping FindMapping(in TypeMappingInfo mappingInfo);

-    }
-    public class LongTypeMapping : RelationalTypeMapping {
 {
-        protected LongTypeMapping(RelationalTypeMapping.RelationalTypeMappingParameters parameters);

-        public LongTypeMapping(string storeType, Nullable<DbType> dbType = default(Nullable<DbType>));

-        protected override RelationalTypeMapping Clone(RelationalTypeMapping.RelationalTypeMappingParameters parameters);

-    }
-    public readonly struct MaterializationContext {
 {
-        public static readonly MethodInfo GetValueBufferMethod;

-        public MaterializationContext(ValueBuffer valueBuffer);

-        public MaterializationContext(in ValueBuffer valueBuffer, DbContext context);

-        public DbContext Context { get; }

-        public ValueBuffer ValueBuffer { get; }

-    }
-    public class ParameterNameGenerator {
 {
-        public ParameterNameGenerator();

-        public virtual string GenerateNext();

-        public virtual void Reset();

-    }
-    public sealed class ParameterNameGeneratorDependencies {
 {
-        public ParameterNameGeneratorDependencies();

-    }
-    public class ParameterNameGeneratorFactory : IParameterNameGeneratorFactory {
 {
-        public ParameterNameGeneratorFactory(ParameterNameGeneratorDependencies dependencies);

-        public virtual ParameterNameGenerator Create();

-    }
-    public class RawSqlCommand {
 {
-        public RawSqlCommand(IRelationalCommand relationalCommand, IReadOnlyDictionary<string, object> parameterValues);

-        public virtual IReadOnlyDictionary<string, object> ParameterValues { get; }

-        public virtual IRelationalCommand RelationalCommand { get; }

-    }
-    public static class RelationalCommandBuilderExtensions {
 {
-        public static IRelationalCommandBuilder AddCompositeParameter(this IRelationalCommandBuilder commandBuilder, string invariantName, Action<IRelationalParameterBuilder> buildAction);

-        public static IRelationalCommandBuilder AddParameter(this IRelationalCommandBuilder commandBuilder, string invariantName, string name);

-        public static IRelationalCommandBuilder AddParameter(this IRelationalCommandBuilder commandBuilder, string invariantName, string name, IProperty property);

-        public static IRelationalCommandBuilder AddParameter(this IRelationalCommandBuilder commandBuilder, string invariantName, string name, RelationalTypeMapping typeMapping, bool nullable);

-        public static IRelationalCommandBuilder AddPropertyParameter(this IRelationalCommandBuilder commandBuilder, string invariantName, string name, IProperty property);

-        public static IRelationalCommandBuilder AddRawParameter(this IRelationalCommandBuilder commandBuilder, string invariantName, DbParameter dbParameter);

-        public static IRelationalCommandBuilder Append(this IRelationalCommandBuilder commandBuilder, object o);

-        public static IRelationalCommandBuilder AppendLine(this IRelationalCommandBuilder commandBuilder);

-        public static IRelationalCommandBuilder AppendLine(this IRelationalCommandBuilder commandBuilder, object o);

-        public static IRelationalCommandBuilder AppendLines(this IRelationalCommandBuilder commandBuilder, object o);

-        public static IRelationalCommandBuilder DecrementIndent(this IRelationalCommandBuilder commandBuilder);

-        public static int GetLength(this IRelationalCommandBuilder commandBuilder);

-        public static IRelationalCommandBuilder IncrementIndent(this IRelationalCommandBuilder commandBuilder);

-        public static IDisposable Indent(this IRelationalCommandBuilder commandBuilder);

-    }
-    public static class RelationalCommandExtensions {
 {
-        public static int ExecuteNonQuery(this IRelationalCommand command, IRelationalConnection connection);

-        public static Task<int> ExecuteNonQueryAsync(this IRelationalCommand command, IRelationalConnection connection, CancellationToken cancellationToken = default(CancellationToken));

-        public static RelationalDataReader ExecuteReader(this IRelationalCommand command, IRelationalConnection connection);

-        public static Task<RelationalDataReader> ExecuteReaderAsync(this IRelationalCommand command, IRelationalConnection connection, CancellationToken cancellationToken = default(CancellationToken));

-        public static object ExecuteScalar(this IRelationalCommand command, IRelationalConnection connection);

-        public static Task<object> ExecuteScalarAsync(this IRelationalCommand command, IRelationalConnection connection, CancellationToken cancellationToken = default(CancellationToken));

-    }
-    public abstract class RelationalConnection : IDbContextTransactionManager, IDisposable, IRelationalConnection, IRelationalTransactionManager, IResettableService, ITransactionEnlistmentManager {
 {
-        protected RelationalConnection(RelationalConnectionDependencies dependencies);

-        public virtual Nullable<int> CommandTimeout { get; set; }

-        public virtual Guid ConnectionId { get; }

-        public virtual string ConnectionString { get; }

-        public virtual IDbContextTransaction CurrentTransaction { get; protected set; }

-        public virtual DbConnection DbConnection { get; }

-        protected virtual RelationalConnectionDependencies Dependencies { get; }

-        public virtual Transaction EnlistedTransaction { get; protected set; }

-        public virtual bool IsMultipleActiveResultSetsEnabled { get; }

-        public virtual SemaphoreSlim Semaphore { get; }

-        protected virtual bool SupportsAmbientTransactions { get; }

-        public virtual IDbContextTransaction BeginTransaction();

-        public virtual IDbContextTransaction BeginTransaction(IsolationLevel isolationLevel);

-        public virtual Task<IDbContextTransaction> BeginTransactionAsync(IsolationLevel isolationLevel, CancellationToken cancellationToken = default(CancellationToken));

-        public virtual Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default(CancellationToken));

-        public virtual bool Close();

-        public virtual void CommitTransaction();

-        protected abstract DbConnection CreateDbConnection();

-        public virtual void Dispose();

-        public virtual void EnlistTransaction(Transaction transaction);

-        void Microsoft.EntityFrameworkCore.Infrastructure.IResettableService.ResetState();

-        void Microsoft.EntityFrameworkCore.Storage.IRelationalConnection.RegisterBufferable(IBufferable bufferable);

-        Task Microsoft.EntityFrameworkCore.Storage.IRelationalConnection.RegisterBufferableAsync(IBufferable bufferable, CancellationToken cancellationToken);

-        public virtual bool Open(bool errorsExpected = false);

-        public virtual Task<bool> OpenAsync(CancellationToken cancellationToken, bool errorsExpected = false);

-        public virtual void RollbackTransaction();

-        public virtual IDbContextTransaction UseTransaction(DbTransaction transaction);

-    }
-    public sealed class RelationalConnectionDependencies {
 {
-        public RelationalConnectionDependencies(IDbContextOptions contextOptions, IDiagnosticsLogger<DbLoggerCategory.Database.Transaction> transactionLogger, IDiagnosticsLogger<DbLoggerCategory.Database.Connection> connectionLogger, INamedConnectionStringResolver connectionStringResolver, IRelationalTransactionFactory relationalTransactionFactory);

-        public IDiagnosticsLogger<DbLoggerCategory.Database.Connection> ConnectionLogger { get; }

-        public INamedConnectionStringResolver ConnectionStringResolver { get; }

-        public IDbContextOptions ContextOptions { get; }

-        public IRelationalTransactionFactory RelationalTransactionFactory { get; }

-        public IDiagnosticsLogger<DbLoggerCategory.Database.Transaction> TransactionLogger { get; }

-        public RelationalConnectionDependencies With(IDiagnosticsLogger<DbLoggerCategory.Database.Connection> connectionLogger);

-        public RelationalConnectionDependencies With(IDiagnosticsLogger<DbLoggerCategory.Database.Transaction> transactionLogger);

-        public RelationalConnectionDependencies With(IDbContextOptions contextOptions);

-        public RelationalConnectionDependencies With(INamedConnectionStringResolver connectionStringResolver);

-        public RelationalConnectionDependencies With(IRelationalTransactionFactory relationalTransactionFactory);

-    }
-    public class RelationalDatabase : Database {
 {
-        public RelationalDatabase(DatabaseDependencies dependencies, RelationalDatabaseDependencies relationalDependencies);

-        protected virtual RelationalDatabaseDependencies RelationalDependencies { get; }

-        public override int SaveChanges(IReadOnlyList<IUpdateEntry> entries);

-        public override Task<int> SaveChangesAsync(IReadOnlyList<IUpdateEntry> entries, CancellationToken cancellationToken = default(CancellationToken));

-    }
-    public abstract class RelationalDatabaseCreator : IDatabaseCreator, IDatabaseCreatorWithCanConnect, IRelationalDatabaseCreator {
 {
-        protected RelationalDatabaseCreator(RelationalDatabaseCreatorDependencies dependencies);

-        protected virtual RelationalDatabaseCreatorDependencies Dependencies { get; }

-        public virtual bool CanConnect();

-        public virtual Task<bool> CanConnectAsync(CancellationToken cancellationToken = default(CancellationToken));

-        public abstract void Create();

-        public virtual Task CreateAsync(CancellationToken cancellationToken = default(CancellationToken));

-        public virtual void CreateTables();

-        public virtual Task CreateTablesAsync(CancellationToken cancellationToken = default(CancellationToken));

-        public abstract void Delete();

-        public virtual Task DeleteAsync(CancellationToken cancellationToken = default(CancellationToken));

-        public virtual bool EnsureCreated();

-        public virtual Task<bool> EnsureCreatedAsync(CancellationToken cancellationToken = default(CancellationToken));

-        public virtual bool EnsureDeleted();

-        public virtual Task<bool> EnsureDeletedAsync(CancellationToken cancellationToken = default(CancellationToken));

-        public abstract bool Exists();

-        public virtual Task<bool> ExistsAsync(CancellationToken cancellationToken = default(CancellationToken));

-        public virtual string GenerateCreateScript();

-        protected virtual IReadOnlyList<MigrationCommand> GetCreateTablesCommands();

-        protected abstract bool HasTables();

-        protected virtual Task<bool> HasTablesAsync(CancellationToken cancellationToken = default(CancellationToken));

-    }
-    public sealed class RelationalDatabaseCreatorDependencies {
 {
-        public RelationalDatabaseCreatorDependencies(IModel model, IRelationalConnection connection, IMigrationsModelDiffer modelDiffer, IMigrationsSqlGenerator migrationsSqlGenerator, IMigrationCommandExecutor migrationCommandExecutor, ISqlGenerationHelper sqlGenerationHelper, IExecutionStrategyFactory executionStrategyFactory);

-        public IRelationalConnection Connection { get; }

-        public IExecutionStrategyFactory ExecutionStrategyFactory { get; }

-        public IMigrationCommandExecutor MigrationCommandExecutor { get; }

-        public IMigrationsSqlGenerator MigrationsSqlGenerator { get; }

-        public IModel Model { get; }

-        public IMigrationsModelDiffer ModelDiffer { get; }

-        public ISqlGenerationHelper SqlGenerationHelper { get; }

-        public RelationalDatabaseCreatorDependencies With(IModel model);

-        public RelationalDatabaseCreatorDependencies With(IMigrationCommandExecutor migrationCommandExecutor);

-        public RelationalDatabaseCreatorDependencies With(IMigrationsModelDiffer modelDiffer);

-        public RelationalDatabaseCreatorDependencies With(IMigrationsSqlGenerator migrationsSqlGenerator);

-        public RelationalDatabaseCreatorDependencies With(IExecutionStrategyFactory executionStrategyFactory);

-        public RelationalDatabaseCreatorDependencies With(IRelationalConnection connection);

-        public RelationalDatabaseCreatorDependencies With(ISqlGenerationHelper sqlGenerationHelper);

-    }
-    public sealed class RelationalDatabaseDependencies {
 {
-        public RelationalDatabaseDependencies(ICommandBatchPreparer batchPreparer, IBatchExecutor batchExecutor, IRelationalConnection connection);

-        public IBatchExecutor BatchExecutor { get; }

-        public ICommandBatchPreparer BatchPreparer { get; }

-        public IRelationalConnection Connection { get; }

-        public RelationalDatabaseDependencies With(IRelationalConnection connection);

-        public RelationalDatabaseDependencies With(IBatchExecutor batchExecutor);

-        public RelationalDatabaseDependencies With(ICommandBatchPreparer batchPreparer);

-    }
-    public class RelationalDataReader : IDisposable {
 {
-        public RelationalDataReader(IRelationalConnection connection, DbCommand command, DbDataReader reader, Guid commandId, IDiagnosticsLogger<DbLoggerCategory.Database.Command> logger);

-        protected RelationalDataReader(DbCommand command, DbDataReader reader, IDiagnosticsLogger<DbLoggerCategory.Database.Command> logger);

-        protected RelationalDataReader(DbDataReader reader);

-        public virtual DbCommand DbCommand { get; }

-        public virtual DbDataReader DbDataReader { get; }

-        public virtual void Dispose();

-        public virtual bool Read();

-        public virtual Task<bool> ReadAsync(CancellationToken cancellationToken = default(CancellationToken));

-    }
-    public static class RelationalExecutionStrategyExtensions {
 {
-        public static void ExecuteInTransaction(this IExecutionStrategy strategy, Action operation, Func<bool> verifySucceeded, IsolationLevel isolationLevel);

-        public static TResult ExecuteInTransaction<TResult>(this IExecutionStrategy strategy, Func<TResult> operation, Func<bool> verifySucceeded, IsolationLevel isolationLevel);

-        public static TResult ExecuteInTransaction<TState, TResult>(this IExecutionStrategy strategy, TState state, Func<TState, TResult> operation, Func<TState, bool> verifySucceeded, IsolationLevel isolationLevel);

-        public static void ExecuteInTransaction<TState>(this IExecutionStrategy strategy, TState state, Action<TState> operation, Func<TState, bool> verifySucceeded, IsolationLevel isolationLevel);

-        public static Task ExecuteInTransactionAsync(this IExecutionStrategy strategy, Func<CancellationToken, Task> operation, Func<CancellationToken, Task<bool>> verifySucceeded, IsolationLevel isolationLevel, CancellationToken cancellationToken = default(CancellationToken));

-        public static Task ExecuteInTransactionAsync(this IExecutionStrategy strategy, Func<Task> operation, Func<Task<bool>> verifySucceeded, IsolationLevel isolationLevel);

-        public static Task<TResult> ExecuteInTransactionAsync<TResult>(this IExecutionStrategy strategy, Func<CancellationToken, Task<TResult>> operation, Func<CancellationToken, Task<bool>> verifySucceeded, IsolationLevel isolationLevel, CancellationToken cancellationToken = default(CancellationToken));

-        public static Task<TResult> ExecuteInTransactionAsync<TState, TResult>(this IExecutionStrategy strategy, TState state, Func<TState, CancellationToken, Task<TResult>> operation, Func<TState, CancellationToken, Task<bool>> verifySucceeded, IsolationLevel isolationLevel, CancellationToken cancellationToken = default(CancellationToken));

-        public static Task ExecuteInTransactionAsync<TState>(this IExecutionStrategy strategy, TState state, Func<TState, CancellationToken, Task> operation, Func<TState, CancellationToken, Task<bool>> verifySucceeded, IsolationLevel isolationLevel, CancellationToken cancellationToken = default(CancellationToken));

-    }
-    public class RelationalExecutionStrategyFactory : IExecutionStrategyFactory {
 {
-        public RelationalExecutionStrategyFactory(ExecutionStrategyDependencies dependencies);

-        protected virtual ExecutionStrategyDependencies Dependencies { get; }

-        public virtual IExecutionStrategy Create();

-        protected virtual IExecutionStrategy CreateDefaultStrategy(ExecutionStrategyDependencies dependencies);

-    }
-    public abstract class RelationalGeometryTypeMapping<TGeometry, TProvider> : RelationalTypeMapping {
 {
-        protected RelationalGeometryTypeMapping(ValueConverter<TGeometry, TProvider> converter, string storeType);

-        protected RelationalGeometryTypeMapping(RelationalTypeMapping.RelationalTypeMappingParameters parameters, ValueConverter<TGeometry, TProvider> converter);

-        protected virtual ValueConverter<TGeometry, TProvider> SpatialConverter { get; }

-        protected abstract Type WKTReaderType { get; }

-        protected abstract string AsText(object value);

-        public override DbParameter CreateParameter(DbCommand command, string name, object value, Nullable<bool> nullable = default(Nullable<bool>));

-        public override Expression CustomizeDataReaderExpression(Expression expression);

-        public override Expression GenerateCodeLiteral(object value);

-        protected abstract int GetSrid(object value);

-    }
-    public class RelationalSqlGenerationHelper : ISqlGenerationHelper {
 {
-        public RelationalSqlGenerationHelper(RelationalSqlGenerationHelperDependencies dependencies);

-        public virtual string BatchTerminator { get; }

-        public virtual string StatementTerminator { get; }

-        public virtual string DelimitIdentifier(string identifier);

-        public virtual string DelimitIdentifier(string name, string schema);

-        public virtual void DelimitIdentifier(StringBuilder builder, string identifier);

-        public virtual void DelimitIdentifier(StringBuilder builder, string name, string schema);

-        public virtual string EscapeIdentifier(string identifier);

-        public virtual void EscapeIdentifier(StringBuilder builder, string identifier);

-        public virtual string EscapeLiteral(string literal);

-        public virtual void EscapeLiteral(StringBuilder builder, string literal);

-        public virtual string GenerateParameterName(string name);

-        public virtual void GenerateParameterName(StringBuilder builder, string name);

-        public virtual string GenerateParameterNamePlaceholder(string name);

-        public virtual void GenerateParameterNamePlaceholder(StringBuilder builder, string name);

-    }
-    public sealed class RelationalSqlGenerationHelperDependencies {
 {
-        public RelationalSqlGenerationHelperDependencies();

-    }
-    public class RelationalTransaction : IDbContextTransaction, IDisposable, IInfrastructure<DbTransaction> {
 {
-        public RelationalTransaction(IRelationalConnection connection, DbTransaction transaction, IDiagnosticsLogger<DbLoggerCategory.Database.Transaction> logger, bool transactionOwned);

-        protected virtual IRelationalConnection Connection { get; }

-        protected virtual IDiagnosticsLogger<DbLoggerCategory.Database.Transaction> Logger { get; }

-        public virtual Guid TransactionId { get; }

-        protected virtual void ClearTransaction();

-        public virtual void Commit();

-        public virtual void Dispose();

-        public virtual void Rollback();

-    }
-    public class RelationalTransactionFactory : IRelationalTransactionFactory {
 {
-        public RelationalTransactionFactory(RelationalTransactionFactoryDependencies dependencies);

-        protected virtual RelationalTransactionFactoryDependencies Dependencies { get; }

-        public virtual RelationalTransaction Create(IRelationalConnection connection, DbTransaction transaction, IDiagnosticsLogger<DbLoggerCategory.Database.Transaction> logger, bool transactionOwned);

-    }
-    public sealed class RelationalTransactionFactoryDependencies {
 {
-        public RelationalTransactionFactoryDependencies();

-    }
-    public abstract class RelationalTypeMapper : IRelationalTypeMapper, ITypeMapper {
 {
-        protected RelationalTypeMapper(RelationalTypeMapperDependencies dependencies);

-        public virtual IByteArrayRelationalTypeMapper ByteArrayMapper { get; }

-        public virtual IStringRelationalTypeMapper StringMapper { get; }

-        protected virtual RelationalTypeMapping CreateMappingFromStoreType(string storeType);

-        protected virtual RelationalTypeMapping FindCustomMapping(IProperty property);

-        public virtual RelationalTypeMapping FindMapping(IProperty property);

-        public virtual RelationalTypeMapping FindMapping(string storeType);

-        public virtual RelationalTypeMapping FindMapping(Type clrType);

-        protected virtual RelationalTypeMapping GetByteArrayMapping(IProperty property);

-        protected abstract IReadOnlyDictionary<Type, RelationalTypeMapping> GetClrTypeMappings();

-        protected virtual string GetColumnType(IProperty property);

-        protected abstract IReadOnlyDictionary<string, RelationalTypeMapping> GetStoreTypeMappings();

-        protected virtual RelationalTypeMapping GetStringMapping(IProperty property);

-        public virtual bool IsTypeMapped(Type clrType);

-        protected virtual bool RequiresKeyMapping(IProperty property);

-        public virtual void ValidateTypeName(string storeType);

-    }
-    public sealed class RelationalTypeMapperDependencies {
 {
-        public RelationalTypeMapperDependencies();

-    }
-    public static class RelationalTypeMapperExtensions {
 {
-        public static RelationalTypeMapping GetMapping(this IRelationalTypeMapper typeMapper, IProperty property);

-        public static RelationalTypeMapping GetMapping(this IRelationalTypeMapper typeMapper, string typeName);

-        public static RelationalTypeMapping GetMapping(this IRelationalTypeMapper typeMapper, Type clrType);

-        public static RelationalTypeMapping GetMappingForValue(this IRelationalTypeMapper typeMapper, object value);

-    }
-    public abstract class RelationalTypeMapping : CoreTypeMapping {
 {
-        public static readonly RelationalTypeMapping NullMapping;

-        protected RelationalTypeMapping(RelationalTypeMapping.RelationalTypeMappingParameters parameters);

-        protected RelationalTypeMapping(string storeType, Type clrType, Nullable<DbType> dbType = default(Nullable<DbType>), bool unicode = false, Nullable<int> size = default(Nullable<int>));

-        public virtual Nullable<DbType> DbType { get; }

-        public virtual bool IsFixedLength { get; }

-        public virtual bool IsUnicode { get; }

-        protected virtual new RelationalTypeMapping.RelationalTypeMappingParameters Parameters { get; }

-        public virtual Nullable<int> Size { get; }

-        protected virtual string SqlLiteralFormatString { get; }

-        public virtual string StoreType { get; }

-        public virtual string StoreTypeNameBase { get; }

-        public virtual StoreTypePostfix StoreTypePostfix { get; }

-        public virtual RelationalTypeMapping Clone(in RelationalTypeMappingInfo mappingInfo);

-        public override CoreTypeMapping Clone(ValueConverter converter);

-        protected virtual RelationalTypeMapping Clone(RelationalTypeMapping.RelationalTypeMappingParameters parameters);

-        public virtual RelationalTypeMapping Clone(Nullable<int> precision, Nullable<int> scale);

-        public virtual RelationalTypeMapping Clone(string storeType, Nullable<int> size);

-        protected virtual void ConfigureParameter(DbParameter parameter);

-        public virtual DbParameter CreateParameter(DbCommand command, string name, object value, Nullable<bool> nullable = default(Nullable<bool>));

-        public virtual Expression CustomizeDataReaderExpression(Expression expression);

-        protected virtual string GenerateNonNullSqlLiteral(object value);

-        public virtual string GenerateProviderValueSqlLiteral(object value);

-        public virtual string GenerateSqlLiteral(object value);

-        public virtual MethodInfo GetDataReaderMethod();

-        protected readonly struct RelationalTypeMappingParameters {
 {
-            public RelationalTypeMappingParameters(CoreTypeMapping.CoreTypeMappingParameters coreParameters, string storeType, StoreTypePostfix storeTypePostfix = StoreTypePostfix.None, Nullable<DbType> dbType = default(Nullable<DbType>), bool unicode = false, Nullable<int> size = default(Nullable<int>), bool fixedLength = false, Nullable<int> precision = default(Nullable<int>), Nullable<int> scale = default(Nullable<int>));

-            public CoreTypeMapping.CoreTypeMappingParameters CoreParameters { get; }

-            public Nullable<DbType> DbType { get; }

-            public bool FixedLength { get; }

-            public Nullable<int> Precision { get; }

-            public bool PrecisionAndScaleOverriden { get; }

-            public Nullable<int> Scale { get; }

-            public Nullable<int> Size { get; }

-            public string StoreType { get; }

-            public StoreTypePostfix StoreTypePostfix { get; }

-            public bool Unicode { get; }

-            public RelationalTypeMapping.RelationalTypeMappingParameters WithComposedConverter(ValueConverter converter);

-            public RelationalTypeMapping.RelationalTypeMappingParameters WithPrecisionAndScale(Nullable<int> precision, Nullable<int> scale);

-            public RelationalTypeMapping.RelationalTypeMappingParameters WithStoreTypeAndSize(string storeType, Nullable<int> size, Nullable<StoreTypePostfix> storeTypePostfix = default(Nullable<StoreTypePostfix>));

-            public RelationalTypeMapping.RelationalTypeMappingParameters WithTypeMappingInfo(in RelationalTypeMappingInfo mappingInfo);

-        }
-    }
-    public readonly struct RelationalTypeMappingInfo : IEquatable<RelationalTypeMappingInfo> {
 {
-        public RelationalTypeMappingInfo(IProperty property);

-        public RelationalTypeMappingInfo(in RelationalTypeMappingInfo source, in ValueConverterInfo converter);

-        public RelationalTypeMappingInfo(IReadOnlyList<IProperty> principals);

-        public RelationalTypeMappingInfo(MemberInfo member);

-        public RelationalTypeMappingInfo(string storeTypeName);

-        public RelationalTypeMappingInfo(Type type);

-        public RelationalTypeMappingInfo(Type type, string storeTypeName, bool keyOrIndex, Nullable<bool> unicode, Nullable<int> size, Nullable<bool> rowVersion, Nullable<bool> fixedLength, Nullable<int> precision, Nullable<int> scale);

-        public Type ClrType { get; }

-        public Nullable<bool> IsFixedLength { get; }

-        public bool IsKeyOrIndex { get; }

-        public Nullable<bool> IsRowVersion { get; }

-        public Nullable<bool> IsUnicode { get; }

-        public Nullable<int> Precision { get; }

-        public Nullable<int> Scale { get; }

-        public Nullable<int> Size { get; }

-        public string StoreTypeName { get; }

-        public string StoreTypeNameBase { get; }

-        public bool StoreTypeNameSizeIsMax { get; }

-        public bool Equals(RelationalTypeMappingInfo other);

-        public override bool Equals(object obj);

-        public override int GetHashCode();

-        public RelationalTypeMappingInfo WithConverter(in ValueConverterInfo converterInfo);

-    }
-    public abstract class RelationalTypeMappingSource : TypeMappingSourceBase, IRelationalTypeMappingSource, ITypeMappingSource {
 {
-        protected RelationalTypeMappingSource(TypeMappingSourceDependencies dependencies, RelationalTypeMappingSourceDependencies relationalDependencies);

-        protected virtual RelationalTypeMappingSourceDependencies RelationalDependencies { get; }

-        public override CoreTypeMapping FindMapping(IProperty property);

-        protected virtual RelationalTypeMapping FindMapping(in RelationalTypeMappingInfo mappingInfo);

-        protected override CoreTypeMapping FindMapping(in TypeMappingInfo mappingInfo);

-        public override CoreTypeMapping FindMapping(MemberInfo member);

-        public virtual RelationalTypeMapping FindMapping(string storeTypeName);

-        public override CoreTypeMapping FindMapping(Type type);

-        public virtual RelationalTypeMapping FindMapping(Type type, string storeTypeName, bool keyOrIndex = false, Nullable<bool> unicode = default(Nullable<bool>), Nullable<int> size = default(Nullable<int>), Nullable<bool> rowVersion = default(Nullable<bool>), Nullable<bool> fixedLength = default(Nullable<bool>), Nullable<int> precision = default(Nullable<int>), Nullable<int> scale = default(Nullable<int>));

-        protected virtual RelationalTypeMapping FindMappingWithConversion(in RelationalTypeMappingInfo mappingInfo, IReadOnlyList<IProperty> principals);

-        RelationalTypeMapping Microsoft.EntityFrameworkCore.Storage.IRelationalTypeMappingSource.FindMapping(IProperty property);

-        RelationalTypeMapping Microsoft.EntityFrameworkCore.Storage.IRelationalTypeMappingSource.FindMapping(MemberInfo member);

-        RelationalTypeMapping Microsoft.EntityFrameworkCore.Storage.IRelationalTypeMappingSource.FindMapping(Type type);

-    }
-    public sealed class RelationalTypeMappingSourceDependencies {
 {
-        public RelationalTypeMappingSourceDependencies(IEnumerable<IRelationalTypeMappingSourcePlugin> plugins);

-        public IEnumerable<IRelationalTypeMappingSourcePlugin> Plugins { get; }

-        public RelationalTypeMappingSourceDependencies With(IEnumerable<IRelationalTypeMappingSourcePlugin> plugins);

-    }
-    public static class RelationalTypeMappingSourceExtensions {
 {
-        public static RelationalTypeMapping GetMapping(this IRelationalTypeMappingSource typeMappingSource, IProperty property);

-        public static RelationalTypeMapping GetMapping(this IRelationalTypeMappingSource typeMappingSource, string typeName);

-        public static RelationalTypeMapping GetMapping(this IRelationalTypeMappingSource typeMappingSource, Type clrType);

-        public static RelationalTypeMapping GetMappingForValue(this IRelationalTypeMappingSource typeMappingSource, object value);

-    }
-    public sealed class RelationalValueBufferFactoryDependencies {
 {
-        public RelationalValueBufferFactoryDependencies(IRelationalTypeMappingSource typeMappingSource, ICoreSingletonOptions coreOptions);

-        public ICoreSingletonOptions CoreOptions { get; }

-        public IRelationalTypeMappingSource TypeMappingSource { get; }

-        public RelationalValueBufferFactoryDependencies With(ICoreSingletonOptions coreOptions);

-        public RelationalValueBufferFactoryDependencies With(IRelationalTypeMappingSource typeMappingSource);

-    }
-    public class RetryLimitExceededException : Exception {
 {
-        public RetryLimitExceededException(string message);

-        public RetryLimitExceededException(string message, Exception innerException);

-    }
-    public class SByteTypeMapping : RelationalTypeMapping {
 {
-        protected SByteTypeMapping(RelationalTypeMapping.RelationalTypeMappingParameters parameters);

-        public SByteTypeMapping(string storeType, Nullable<DbType> dbType = default(Nullable<DbType>));

-        protected override RelationalTypeMapping Clone(RelationalTypeMapping.RelationalTypeMappingParameters parameters);

-    }
-    public class ShortTypeMapping : RelationalTypeMapping {
 {
-        protected ShortTypeMapping(RelationalTypeMapping.RelationalTypeMappingParameters parameters);

-        public ShortTypeMapping(string storeType, Nullable<DbType> dbType = default(Nullable<DbType>));

-        protected override RelationalTypeMapping Clone(RelationalTypeMapping.RelationalTypeMappingParameters parameters);

-    }
-    public enum StoreTypePostfix {
 {
-        None = 0,

-        Precision = 2,

-        PrecisionAndScale = 3,

-        Size = 1,

-    }
-    public class StringRelationalTypeMapper : IStringRelationalTypeMapper {
 {
-        public StringRelationalTypeMapper(int maxBoundedAnsiLength, RelationalTypeMapping defaultAnsiMapping, RelationalTypeMapping unboundedAnsiMapping, RelationalTypeMapping keyAnsiMapping, Func<int, RelationalTypeMapping> createBoundedAnsiMapping, int maxBoundedUnicodeLength, RelationalTypeMapping defaultUnicodeMapping, RelationalTypeMapping unboundedUnicodeMapping, RelationalTypeMapping keyUnicodeMapping, Func<int, RelationalTypeMapping> createBoundedUnicodeMapping);

-        public virtual Func<int, RelationalTypeMapping> CreateBoundedAnsiMapping { get; }

-        public virtual Func<int, RelationalTypeMapping> CreateBoundedUnicodeMapping { get; }

-        public virtual RelationalTypeMapping DefaultAnsiMapping { get; }

-        public virtual RelationalTypeMapping DefaultUnicodeMapping { get; }

-        public virtual RelationalTypeMapping KeyAnsiMapping { get; }

-        public virtual RelationalTypeMapping KeyUnicodeMapping { get; }

-        public virtual int MaxBoundedAnsiLength { get; }

-        public virtual int MaxBoundedUnicodeLength { get; }

-        public virtual RelationalTypeMapping UnboundedAnsiMapping { get; }

-        public virtual RelationalTypeMapping UnboundedUnicodeMapping { get; }

-        public virtual RelationalTypeMapping FindMapping(bool unicode, bool keyOrIndex, Nullable<int> maxLength);

-    }
-    public class StringTypeMapping : RelationalTypeMapping {
 {
-        protected StringTypeMapping(RelationalTypeMapping.RelationalTypeMappingParameters parameters);

-        public StringTypeMapping(string storeType, Nullable<DbType> dbType = default(Nullable<DbType>), bool unicode = false, Nullable<int> size = default(Nullable<int>));

-        protected override RelationalTypeMapping Clone(RelationalTypeMapping.RelationalTypeMappingParameters parameters);

-        protected virtual string EscapeSqlLiteral(string literal);

-        protected override string GenerateNonNullSqlLiteral(object value);

-    }
-    public class TimeSpanTypeMapping : RelationalTypeMapping {
 {
-        protected TimeSpanTypeMapping(RelationalTypeMapping.RelationalTypeMappingParameters parameters);

-        public TimeSpanTypeMapping(string storeType, Nullable<DbType> dbType = default(Nullable<DbType>));

-        protected override string SqlLiteralFormatString { get; }

-        protected override RelationalTypeMapping Clone(RelationalTypeMapping.RelationalTypeMappingParameters parameters);

-    }
-    public class TypedRelationalValueBufferFactoryFactory : IRelationalValueBufferFactoryFactory {
 {
-        public static readonly ParameterExpression DataReaderParameter;

-        public TypedRelationalValueBufferFactoryFactory(RelationalValueBufferFactoryDependencies dependencies);

-        protected virtual RelationalValueBufferFactoryDependencies Dependencies { get; }

-        public virtual IRelationalValueBufferFactory Create(IReadOnlyList<TypeMaterializationInfo> types);

-        public virtual IRelationalValueBufferFactory Create(IReadOnlyList<Type> valueTypes, IReadOnlyList<int> indexMap);

-        public virtual IReadOnlyList<Expression> CreateAssignmentExpressions(IReadOnlyList<TypeMaterializationInfo> types);

-    }
-    public readonly struct TypeMappingInfo : IEquatable<TypeMappingInfo> {
 {
-        public TypeMappingInfo(IProperty property);

-        public TypeMappingInfo(TypeMappingInfo source, ValueConverterInfo converter, Nullable<bool> unicode = default(Nullable<bool>), Nullable<int> size = default(Nullable<int>), Nullable<int> precision = default(Nullable<int>), Nullable<int> scale = default(Nullable<int>));

-        public TypeMappingInfo(IReadOnlyList<IProperty> principals);

-        public TypeMappingInfo(MemberInfo member);

-        public TypeMappingInfo(Type type);

-        public TypeMappingInfo(Type type, bool keyOrIndex, Nullable<bool> unicode = default(Nullable<bool>), Nullable<int> size = default(Nullable<int>), Nullable<bool> rowVersion = default(Nullable<bool>), Nullable<int> precision = default(Nullable<int>), Nullable<int> scale = default(Nullable<int>));

-        public Type ClrType { get; }

-        public bool IsKeyOrIndex { get; }

-        public Nullable<bool> IsRowVersion { get; }

-        public Nullable<bool> IsUnicode { get; }

-        public Nullable<int> Precision { get; }

-        public Nullable<int> Scale { get; }

-        public Nullable<int> Size { get; }

-        public bool Equals(TypeMappingInfo other);

-        public override bool Equals(object obj);

-        public override int GetHashCode();

-        public TypeMappingInfo WithConverter(in ValueConverterInfo converterInfo);

-    }
-    public abstract class TypeMappingSource : TypeMappingSourceBase {
 {
-        protected TypeMappingSource(TypeMappingSourceDependencies dependencies);

-        public override CoreTypeMapping FindMapping(IProperty property);

-        public override CoreTypeMapping FindMapping(MemberInfo member);

-        public override CoreTypeMapping FindMapping(Type type);

-    }
-    public abstract class TypeMappingSourceBase : ITypeMappingSource {
 {
-        protected TypeMappingSourceBase(TypeMappingSourceDependencies dependencies);

-        protected virtual TypeMappingSourceDependencies Dependencies { get; }

-        public abstract CoreTypeMapping FindMapping(IProperty property);

-        protected virtual CoreTypeMapping FindMapping(in TypeMappingInfo mappingInfo);

-        public abstract CoreTypeMapping FindMapping(MemberInfo member);

-        public abstract CoreTypeMapping FindMapping(Type type);

-        protected virtual void ValidateMapping(CoreTypeMapping mapping, IProperty property);

-    }
-    public sealed class TypeMappingSourceDependencies {
 {
-        public TypeMappingSourceDependencies(IValueConverterSelector valueConverterSelector, IEnumerable<ITypeMappingSourcePlugin> plugins);

-        public IEnumerable<ITypeMappingSourcePlugin> Plugins { get; }

-        public IValueConverterSelector ValueConverterSelector { get; }

-        public TypeMappingSourceDependencies With(IValueConverterSelector valueConverterSelector);

-        public TypeMappingSourceDependencies With(IEnumerable<ITypeMappingSourcePlugin> plugins);

-    }
-    public class TypeMaterializationInfo {
 {
-        public TypeMaterializationInfo(Type modelClrType, IProperty property, IRelationalTypeMappingSource typeMappingSource, int index = -1);

-        public TypeMaterializationInfo(Type modelClrType, IProperty property, IRelationalTypeMappingSource typeMappingSource, Nullable<bool> fromLeftOuterJoin, int index);

-        public TypeMaterializationInfo(Type modelClrType, IProperty property, IRelationalTypeMappingSource typeMappingSource, Nullable<bool> fromLeftOuterJoin, int index = -1, RelationalTypeMapping mapping = null);

-        public virtual int Index { get; }

-        public virtual Nullable<bool> IsFromLeftOuterJoin { get; }

-        public virtual RelationalTypeMapping Mapping { get; }

-        public virtual Type ModelClrType { get; }

-        public virtual IProperty Property { get; }

-        public virtual Type ProviderClrType { get; }

-        protected virtual bool Equals(TypeMaterializationInfo other);

-        public override bool Equals(object obj);

-        public override int GetHashCode();

-    }
-    public class UIntTypeMapping : RelationalTypeMapping {
 {
-        protected UIntTypeMapping(RelationalTypeMapping.RelationalTypeMappingParameters parameters);

-        public UIntTypeMapping(string storeType, Nullable<DbType> dbType = default(Nullable<DbType>));

-        protected override RelationalTypeMapping Clone(RelationalTypeMapping.RelationalTypeMappingParameters parameters);

-    }
-    public class ULongTypeMapping : RelationalTypeMapping {
 {
-        protected ULongTypeMapping(RelationalTypeMapping.RelationalTypeMappingParameters parameters);

-        public ULongTypeMapping(string storeType, Nullable<DbType> dbType = default(Nullable<DbType>));

-        protected override RelationalTypeMapping Clone(RelationalTypeMapping.RelationalTypeMappingParameters parameters);

-    }
-    public class UntypedRelationalValueBufferFactoryFactory : IRelationalValueBufferFactoryFactory {
 {
-        public UntypedRelationalValueBufferFactoryFactory(RelationalValueBufferFactoryDependencies dependencies);

-        protected virtual RelationalValueBufferFactoryDependencies Dependencies { get; }

-        public virtual IRelationalValueBufferFactory Create(IReadOnlyList<TypeMaterializationInfo> types);

-        public virtual IRelationalValueBufferFactory Create(IReadOnlyList<Type> valueTypes, IReadOnlyList<int> indexMap);

-    }
-    public class UShortTypeMapping : RelationalTypeMapping {
 {
-        protected UShortTypeMapping(RelationalTypeMapping.RelationalTypeMappingParameters parameters);

-        public UShortTypeMapping(string storeType, Nullable<DbType> dbType = default(Nullable<DbType>));

-        protected override RelationalTypeMapping Clone(RelationalTypeMapping.RelationalTypeMappingParameters parameters);

-    }
-    public readonly struct ValueBuffer {
 {
-        public static readonly ValueBuffer Empty;

-        public ValueBuffer(object[] values);

-        public ValueBuffer(object[] values, int offset);

-        public int Count { get; }

-        public bool IsEmpty { get; }

-        public object this[int index] { get; set; }

-        public override bool Equals(object obj);

-        public override int GetHashCode();

-        public ValueBuffer WithOffset(int offset);

-    }
-}
```

