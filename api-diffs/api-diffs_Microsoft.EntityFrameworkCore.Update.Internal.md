# Microsoft.EntityFrameworkCore.Update.Internal

``` diff
-namespace Microsoft.EntityFrameworkCore.Update.Internal {
 {
-    public class BatchExecutor : IBatchExecutor {
 {
-        public BatchExecutor(ICurrentDbContext currentContext, IExecutionStrategyFactory executionStrategyFactory);

-        public virtual ICurrentDbContext CurrentContext { get; }

-        protected virtual IExecutionStrategyFactory ExecutionStrategyFactory { get; }

-        public virtual int Execute(IEnumerable<ModificationCommandBatch> commandBatches, IRelationalConnection connection);

-        public virtual Task<int> ExecuteAsync(IEnumerable<ModificationCommandBatch> commandBatches, IRelationalConnection connection, CancellationToken cancellationToken = default(CancellationToken));

-    }
-    public class CommandBatchPreparer : ICommandBatchPreparer {
 {
-        public CommandBatchPreparer(CommandBatchPreparerDependencies dependencies);

-        public virtual IEnumerable<ModificationCommandBatch> BatchCommands(IReadOnlyList<IUpdateEntry> entries);

-        protected virtual IEnumerable<ModificationCommand> CreateModificationCommands(IReadOnlyList<IUpdateEntry> entries, Func<string> generateParameterName);

-        protected virtual IReadOnlyList<List<ModificationCommand>> TopologicalSort(IEnumerable<ModificationCommand> commands);

-    }
-    public sealed class CommandBatchPreparerDependencies {
 {
-        public CommandBatchPreparerDependencies(IModificationCommandBatchFactory modificationCommandBatchFactory, IParameterNameGeneratorFactory parameterNameGeneratorFactory, IComparer<ModificationCommand> modificationCommandComparer, IKeyValueIndexFactorySource keyValueIndexFactorySource, Func<IStateManager> stateManager, ILoggingOptions loggingOptions, IDiagnosticsLogger<DbLoggerCategory.Update> updateLogger, IDbContextOptions options);

-        public IKeyValueIndexFactorySource KeyValueIndexFactorySource { get; }

-        public ILoggingOptions LoggingOptions { get; }

-        public IModificationCommandBatchFactory ModificationCommandBatchFactory { get; }

-        public IComparer<ModificationCommand> ModificationCommandComparer { get; }

-        public IDbContextOptions Options { get; }

-        public IParameterNameGeneratorFactory ParameterNameGeneratorFactory { get; }

-        public Func<IStateManager> StateManager { get; }

-        public IDiagnosticsLogger<DbLoggerCategory.Update> UpdateLogger { get; }

-        public CommandBatchPreparerDependencies With(IDiagnosticsLogger<DbLoggerCategory.Update> updateLogger);

-        public CommandBatchPreparerDependencies With(ILoggingOptions loggingOptions);

-        public CommandBatchPreparerDependencies With(IDbContextOptions options);

-        public CommandBatchPreparerDependencies With(IParameterNameGeneratorFactory parameterNameGeneratorFactory);

-        public CommandBatchPreparerDependencies With(IModificationCommandBatchFactory modificationCommandBatchFactory);

-        public CommandBatchPreparerDependencies With(IKeyValueIndexFactorySource keyValueIndexFactorySource);

-        public CommandBatchPreparerDependencies With(IComparer<ModificationCommand> modificationCommandComparer);

-        public CommandBatchPreparerDependencies With(Func<IStateManager> stateManager);

-    }
-    public interface IKeyValueIndex {
 {
-        IKeyValueIndex WithOriginalValuesFlag();

-    }
-    public interface IKeyValueIndexFactory {
 {
-        IKeyValueIndex CreateDependentKeyValue(InternalEntityEntry entry, IForeignKey foreignKey);

-        IKeyValueIndex CreateDependentKeyValueFromOriginalValues(InternalEntityEntry entry, IForeignKey foreignKey);

-        IKeyValueIndex CreatePrincipalKeyValue(InternalEntityEntry entry, IForeignKey foreignKey);

-        IKeyValueIndex CreatePrincipalKeyValueFromOriginalValues(InternalEntityEntry entry, IForeignKey foreignKey);

-    }
-    public interface IKeyValueIndexFactorySource {
 {
-        IKeyValueIndexFactory GetKeyValueIndexFactory(IKey key);

-    }
-    public interface ISingletonUpdateSqlGenerator

-    public sealed class KeyValueIndex<TKey> : IKeyValueIndex {
 {
-        public KeyValueIndex(IForeignKey foreignKey, TKey keyValue, IEqualityComparer<TKey> keyComparer, bool fromOriginalValues);

-        public override bool Equals(object obj);

-        public override int GetHashCode();

-        public IKeyValueIndex WithOriginalValuesFlag();

-    }
-    public class KeyValueIndexFactory<TKey> : IKeyValueIndexFactory {
 {
-        public KeyValueIndexFactory(IPrincipalKeyValueFactory<TKey> principalKeyValueFactory);

-        public virtual IKeyValueIndex CreateDependentKeyValue(InternalEntityEntry entry, IForeignKey foreignKey);

-        public virtual IKeyValueIndex CreateDependentKeyValueFromOriginalValues(InternalEntityEntry entry, IForeignKey foreignKey);

-        public virtual IKeyValueIndex CreatePrincipalKeyValue(InternalEntityEntry entry, IForeignKey foreignKey);

-        public virtual IKeyValueIndex CreatePrincipalKeyValueFromOriginalValues(InternalEntityEntry entry, IForeignKey foreignKey);

-    }
-    public class KeyValueIndexFactorySource : IdentityMapFactoryFactoryBase, IKeyValueIndexFactorySource {
 {
-        public KeyValueIndexFactorySource();

-        public virtual IKeyValueIndexFactory Create(IKey key);

-        public virtual IKeyValueIndexFactory GetKeyValueIndexFactory(IKey key);

-    }
-    public class ModificationCommandComparer : IComparer<ModificationCommand> {
 {
-        public ModificationCommandComparer();

-        public virtual int Compare(ModificationCommand x, ModificationCommand y);

-        protected virtual Func<object, object, int> GetComparer(Type type);

-    }
-    public class SharedTableEntryMap<TValue> {
 {
-        public SharedTableEntryMap(IStateManager stateManager, IReadOnlyDictionary<IEntityType, IReadOnlyList<IEntityType>> principals, IReadOnlyDictionary<IEntityType, IReadOnlyList<IEntityType>> dependents, string name, string schema, SharedTableEntryValueFactory<TValue> createElement);

-        public virtual IEnumerable<TValue> Values { get; }

-        public static Dictionary<ValueTuple<string, string>, SharedTableEntryMapFactory<TValue>> CreateSharedTableEntryMapFactories(IModel model, IStateManager stateManager);

-        public static SharedTableEntryMapFactory<TValue> CreateSharedTableEntryMapFactory(IReadOnlyList<IEntityType> entityTypes, IStateManager stateManager, string tableName, string schema);

-        public virtual IReadOnlyList<IEntityType> GetDependents(IEntityType entityType);

-        public virtual TValue GetOrAddValue(IUpdateEntry entry);

-        public virtual IReadOnlyList<IEntityType> GetPrincipals(IEntityType entityType);

-    }
-    public delegate SharedTableEntryMap<TValue> SharedTableEntryMapFactory<TValue>(SharedTableEntryValueFactory<TValue> valueFactory);

-    public delegate TValue SharedTableEntryValueFactory<out TValue>(string tableName, string schema, IComparer<IUpdateEntry> comparer);

-    public static class UpdateEntryExtensions {
 {
-        public static string BuildCurrentValuesString(this IUpdateEntry entry, IEnumerable<IPropertyBase> properties);

-        public static string BuildOriginalValuesString(this IUpdateEntry entry, IEnumerable<IPropertyBase> properties);

-    }
-}
```

