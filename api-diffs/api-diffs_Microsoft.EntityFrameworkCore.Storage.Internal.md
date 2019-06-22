# Microsoft.EntityFrameworkCore.Storage.Internal

``` diff
-namespace Microsoft.EntityFrameworkCore.Storage.Internal {
 {
-    public class CompositeRelationalParameter : RelationalParameterBase {
 {
-        public CompositeRelationalParameter(string invariantName, IReadOnlyList<IRelationalParameter> relationalParameters);

-        public override string InvariantName { get; }

-        public virtual IReadOnlyList<IRelationalParameter> RelationalParameters { get; }

-        public override void AddDbParameter(DbCommand command, object value);

-    }
-    public static class DbParameterCollectionExtensions {
 {
-        public static string FormatParameter(string name, object value, bool hasValue, ParameterDirection direction, DbType dbType, bool nullable, int size, byte precision, byte scale);

-        public static string FormatParameters(this DbParameterCollection parameters, bool logParameterValues);

-    }
-    public class DynamicRelationalParameter : RelationalParameterBase {
 {
-        public DynamicRelationalParameter(string invariantName, string name, IRelationalTypeMappingSource typeMappingSource);

-        public override string InvariantName { get; }

-        public virtual string Name { get; }

-        public override void AddDbParameter(DbCommand command, object value);

-    }
-    public class ExecutionStrategyFactory : IExecutionStrategyFactory {
 {
-        public ExecutionStrategyFactory(ExecutionStrategyDependencies dependencies);

-        protected virtual ExecutionStrategyDependencies Dependencies { get; }

-        public virtual IExecutionStrategy Create();

-    }
-    public class FallbackRelationalTypeMappingSource : RelationalTypeMappingSource {
 {
-        public FallbackRelationalTypeMappingSource(TypeMappingSourceDependencies dependencies, RelationalTypeMappingSourceDependencies relationalDependencies, IRelationalTypeMapper typeMapper);

-        protected override RelationalTypeMapping FindMapping(in RelationalTypeMappingInfo mappingInfo);

-        protected override RelationalTypeMapping FindMappingWithConversion(in RelationalTypeMappingInfo mappingInfo, IReadOnlyList<IProperty> principals);

-    }
-    public class FallbackTypeMappingSource : TypeMappingSource {
 {
-        public FallbackTypeMappingSource(TypeMappingSourceDependencies dependencies, ITypeMapper typeMapper);

-        protected override CoreTypeMapping FindMapping(in TypeMappingInfo mappingInfo);

-    }
-    public interface INamedConnectionStringResolver {
 {
-        string ResolveConnectionString(string connectionString);

-    }
-    public class NamedConnectionStringResolver : NamedConnectionStringResolverBase {
 {
-        public NamedConnectionStringResolver(IDbContextOptions options);

-        protected override IServiceProvider ApplicationServiceProvider { get; }

-    }
-    public abstract class NamedConnectionStringResolverBase : INamedConnectionStringResolver {
 {
-        protected NamedConnectionStringResolverBase();

-        protected abstract IServiceProvider ApplicationServiceProvider { get; }

-        public virtual string ResolveConnectionString(string connectionString);

-    }
-    public class NoopExecutionStrategy : IExecutionStrategy {
 {
-        public NoopExecutionStrategy(ExecutionStrategyDependencies dependencies);

-        public virtual bool RetriesOnFailure { get; }

-        public virtual TResult Execute<TState, TResult>(TState state, Func<DbContext, TState, TResult> operation, Func<DbContext, TState, ExecutionResult<TResult>> verifySucceeded);

-        public virtual Task<TResult> ExecuteAsync<TState, TResult>(TState state, Func<DbContext, TState, CancellationToken, Task<TResult>> operation, Func<DbContext, TState, CancellationToken, Task<ExecutionResult<TResult>>> verifySucceeded, CancellationToken cancellationToken = default(CancellationToken));

-    }
-    public class ObsoleteRelationalTypeMapper : IRelationalTypeMapper, ITypeMapper {
 {
-        public ObsoleteRelationalTypeMapper();

-        public virtual IByteArrayRelationalTypeMapper ByteArrayMapper { get; }

-        public virtual IStringRelationalTypeMapper StringMapper { get; }

-        public virtual RelationalTypeMapping FindMapping(IProperty property);

-        public virtual RelationalTypeMapping FindMapping(string storeType);

-        public virtual RelationalTypeMapping FindMapping(Type clrType);

-        public virtual bool IsTypeMapped(Type clrType);

-        public virtual void ValidateTypeName(string storeType);

-    }
-    public class RawRelationalParameter : RelationalParameterBase {
 {
-        public RawRelationalParameter(string invariantName, DbParameter parameter);

-        public override string InvariantName { get; }

-        public override void AddDbParameter(DbCommand command, IReadOnlyDictionary<string, object> parameterValues);

-        public override void AddDbParameter(DbCommand command, object value);

-    }
-    public class RawSqlCommandBuilder : IRawSqlCommandBuilder {
 {
-        public RawSqlCommandBuilder(IRelationalCommandBuilderFactory relationalCommandBuilderFactory, ISqlGenerationHelper sqlGenerationHelper, IParameterNameGeneratorFactory parameterNameGeneratorFactory);

-        public virtual IRelationalCommand Build(string sql);

-        public virtual RawSqlCommand Build(string sql, IEnumerable<object> parameters);

-    }
-    public class RelationalCommand : IRelationalCommand {
 {
-        public RelationalCommand(IDiagnosticsLogger<DbLoggerCategory.Database.Command> logger, string commandText, IReadOnlyList<IRelationalParameter> parameters);

-        public virtual string CommandText { get; }

-        protected virtual IDiagnosticsLogger<DbLoggerCategory.Database.Command> Logger { get; }

-        public virtual IReadOnlyList<IRelationalParameter> Parameters { get; }

-        protected virtual string AdjustCommandText(string commandText);

-        protected virtual void ConfigureCommand(DbCommand command);

-        protected virtual DbCommand CreateCommand(IRelationalConnection connection, IReadOnlyDictionary<string, object> parameterValues);

-        protected virtual object Execute(IRelationalConnection connection, DbCommandMethod executeMethod, IReadOnlyDictionary<string, object> parameterValues);

-        protected virtual Task<object> ExecuteAsync(IRelationalConnection connection, DbCommandMethod executeMethod, IReadOnlyDictionary<string, object> parameterValues, CancellationToken cancellationToken = default(CancellationToken));

-        public virtual int ExecuteNonQuery(IRelationalConnection connection, IReadOnlyDictionary<string, object> parameterValues);

-        public virtual Task<int> ExecuteNonQueryAsync(IRelationalConnection connection, IReadOnlyDictionary<string, object> parameterValues, CancellationToken cancellationToken = default(CancellationToken));

-        public virtual RelationalDataReader ExecuteReader(IRelationalConnection connection, IReadOnlyDictionary<string, object> parameterValues);

-        public virtual Task<RelationalDataReader> ExecuteReaderAsync(IRelationalConnection connection, IReadOnlyDictionary<string, object> parameterValues, CancellationToken cancellationToken = default(CancellationToken));

-        public virtual object ExecuteScalar(IRelationalConnection connection, IReadOnlyDictionary<string, object> parameterValues);

-        public virtual Task<object> ExecuteScalarAsync(IRelationalConnection connection, IReadOnlyDictionary<string, object> parameterValues, CancellationToken cancellationToken = default(CancellationToken));

-    }
-    public class RelationalCommandBuilder : IInfrastructure<IndentedStringBuilder>, IRelationalCommandBuilder {
 {
-        public RelationalCommandBuilder(IDiagnosticsLogger<DbLoggerCategory.Database.Command> logger, IRelationalTypeMappingSource typeMappingSource);

-        IndentedStringBuilder Microsoft.EntityFrameworkCore.Infrastructure.IInfrastructure<Microsoft.EntityFrameworkCore.Internal.IndentedStringBuilder>.Instance { get; }

-        public virtual IRelationalParameterBuilder ParameterBuilder { get; }

-        public virtual IRelationalCommand Build();

-        protected virtual IRelationalCommand BuildCore(IDiagnosticsLogger<DbLoggerCategory.Database.Command> logger, string commandText, IReadOnlyList<IRelationalParameter> parameters);

-        public override string ToString();

-    }
-    public class RelationalCommandBuilderFactory : IRelationalCommandBuilderFactory {
 {
-        public RelationalCommandBuilderFactory(IDiagnosticsLogger<DbLoggerCategory.Database.Command> logger, IRelationalTypeMappingSource typeMappingSource);

-        public virtual IRelationalCommandBuilder Create();

-        protected virtual IRelationalCommandBuilder CreateCore(IDiagnosticsLogger<DbLoggerCategory.Database.Command> logger, IRelationalTypeMappingSource relationalTypeMappingSource);

-    }
-    public abstract class RelationalParameterBase : IRelationalParameter {
 {
-        protected RelationalParameterBase();

-        public virtual string InvariantName { get; }

-        public virtual void AddDbParameter(DbCommand command, IReadOnlyDictionary<string, object> parameterValues);

-        public abstract void AddDbParameter(DbCommand command, object value);

-    }
-    public class RelationalParameterBuilder : IRelationalParameterBuilder {
 {
-        public RelationalParameterBuilder(IRelationalTypeMappingSource typeMappingSource);

-        public virtual IReadOnlyList<IRelationalParameter> Parameters { get; }

-        protected virtual IRelationalTypeMappingSource TypeMappingSource { get; }

-        public virtual void AddCompositeParameter(string invariantName, Action<IRelationalParameterBuilder> buildAction);

-        public virtual void AddParameter(string invariantName, string name);

-        public virtual void AddParameter(string invariantName, string name, IProperty property);

-        public virtual void AddParameter(string invariantName, string name, RelationalTypeMapping typeMapping, bool nullable);

-        public virtual void AddPropertyParameter(string invariantName, string name, IProperty property);

-        public virtual void AddRawParameter(string invariantName, DbParameter dbParameter);

-        protected virtual RelationalParameterBuilder Create();

-    }
-    public class RemappingUntypedRelationalValueBufferFactory : IRelationalValueBufferFactory {
 {
-        public RemappingUntypedRelationalValueBufferFactory(RelationalValueBufferFactoryDependencies dependencies, IReadOnlyList<TypeMaterializationInfo> mappingInfo, Action<object[]> processValuesAction);

-        public virtual ValueBuffer Create(DbDataReader dataReader);

-    }
-    public sealed class TypedRelationalValueBufferFactory : IRelationalValueBufferFactory {
 {
-        public TypedRelationalValueBufferFactory(RelationalValueBufferFactoryDependencies dependencies, Func<DbDataReader, object[]> valueFactory);

-        public ValueBuffer Create(DbDataReader dataReader);

-    }
-    public class TypeMappedPropertyRelationalParameter : TypeMappedRelationalParameter {
 {
-        public TypeMappedPropertyRelationalParameter(string invariantName, string name, RelationalTypeMapping relationalTypeMapping, IProperty property);

-        public override void AddDbParameter(DbCommand command, object value);

-    }
-    public class TypeMappedRelationalParameter : RelationalParameterBase {
 {
-        public TypeMappedRelationalParameter(string invariantName, string name, RelationalTypeMapping relationalTypeMapping, Nullable<bool> nullable);

-        public override string InvariantName { get; }

-        public virtual string Name { get; }

-        public override void AddDbParameter(DbCommand command, object value);

-    }
-    public class UntypedRelationalValueBufferFactory : IRelationalValueBufferFactory {
 {
-        public UntypedRelationalValueBufferFactory(RelationalValueBufferFactoryDependencies dependencies, IReadOnlyList<TypeMaterializationInfo> mappingInfo, Action<object[]> processValuesAction);

-        public virtual ValueBuffer Create(DbDataReader dataReader);

-    }
-}
```

