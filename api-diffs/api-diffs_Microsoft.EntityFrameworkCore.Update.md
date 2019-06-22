# Microsoft.EntityFrameworkCore.Update

``` diff
-namespace Microsoft.EntityFrameworkCore.Update {
 {
-    public abstract class AffectedCountModificationCommandBatch : ReaderModificationCommandBatch {
 {
-        protected AffectedCountModificationCommandBatch(IRelationalCommandBuilderFactory commandBuilderFactory, ISqlGenerationHelper sqlGenerationHelper, IUpdateSqlGenerator updateSqlGenerator, IRelationalValueBufferFactoryFactory valueBufferFactoryFactory);

-        protected override void Consume(RelationalDataReader reader);

-        protected override Task ConsumeAsync(RelationalDataReader reader, CancellationToken cancellationToken = default(CancellationToken));

-        protected virtual int ConsumeResultSetWithoutPropagation(int commandIndex, RelationalDataReader reader);

-        protected virtual Task<int> ConsumeResultSetWithoutPropagationAsync(int commandIndex, RelationalDataReader reader, CancellationToken cancellationToken);

-        protected virtual int ConsumeResultSetWithPropagation(int commandIndex, RelationalDataReader reader);

-        protected virtual Task<int> ConsumeResultSetWithPropagationAsync(int commandIndex, RelationalDataReader reader, CancellationToken cancellationToken);

-        protected virtual void ThrowAggregateUpdateConcurrencyException(int commandIndex, int expectedRowsAffected, int rowsAffected);

-    }
-    public class ColumnModification {
 {
-        public ColumnModification(IUpdateEntry entry, IProperty property, IRelationalPropertyAnnotations propertyAnnotations, Func<string> generateParameterName, bool isRead, bool isWrite, bool isKey, bool isCondition, bool isConcurrencyToken);

-        public ColumnModification(string columnName, object originalValue, object value, IProperty property, bool isRead, bool isWrite, bool isKey, bool isCondition);

-        public virtual string ColumnName { get; }

-        public virtual IUpdateEntry Entry { get; }

-        public virtual bool IsConcurrencyToken { get; }

-        public virtual bool IsCondition { get; }

-        public virtual bool IsKey { get; }

-        public virtual bool IsRead { get; }

-        public virtual bool IsWrite { get; }

-        public virtual string OriginalParameterName { get; }

-        public virtual object OriginalValue { get; }

-        public virtual string ParameterName { get; }

-        public virtual IProperty Property { get; }

-        public virtual bool UseCurrentValueParameter { get; }

-        public virtual bool UseOriginalValueParameter { get; }

-        public virtual object Value { get; set; }

-    }
-    public interface IBatchExecutor {
 {
-        int Execute(IEnumerable<ModificationCommandBatch> commandBatches, IRelationalConnection connection);

-        Task<int> ExecuteAsync(IEnumerable<ModificationCommandBatch> commandBatches, IRelationalConnection connection, CancellationToken cancellationToken = default(CancellationToken));

-    }
-    public interface ICommandBatchPreparer {
 {
-        IEnumerable<ModificationCommandBatch> BatchCommands(IReadOnlyList<IUpdateEntry> entries);

-    }
-    public interface IModificationCommandBatchFactory {
 {
-        ModificationCommandBatch Create();

-    }
-    public interface IUpdateEntry {
 {
-        EntityState EntityState { get; }

-        IEntityType EntityType { get; }

-        IUpdateEntry SharedIdentityEntry { get; }

-        object GetCurrentValue(IPropertyBase propertyBase);

-        TProperty GetCurrentValue<TProperty>(IPropertyBase propertyBase);

-        object GetOriginalValue(IPropertyBase propertyBase);

-        TProperty GetOriginalValue<TProperty>(IProperty property);

-        bool HasTemporaryValue(IProperty property);

-        bool IsModified(IProperty property);

-        bool IsStoreGenerated(IProperty property);

-        void SetCurrentValue(IPropertyBase propertyBase, object value);

-        EntityEntry ToEntityEntry();

-    }
-    public interface IUpdateSqlGenerator : ISingletonUpdateSqlGenerator {
 {
-        void AppendBatchHeader(StringBuilder commandStringBuilder);

-        ResultSetMapping AppendDeleteOperation(StringBuilder commandStringBuilder, ModificationCommand command, int commandPosition);

-        ResultSetMapping AppendInsertOperation(StringBuilder commandStringBuilder, ModificationCommand command, int commandPosition);

-        void AppendNextSequenceValueOperation(StringBuilder commandStringBuilder, string name, string schema);

-        ResultSetMapping AppendUpdateOperation(StringBuilder commandStringBuilder, ModificationCommand command, int commandPosition);

-        string GenerateNextSequenceValueOperation(string name, string schema);

-    }
-    public class ModificationCommand {
 {
-        public ModificationCommand(string name, string schema, IReadOnlyList<ColumnModification> columnModifications);

-        public ModificationCommand(string name, string schema, Func<string> generateParameterName, bool sensitiveLoggingEnabled, IComparer<IUpdateEntry> comparer);

-        public virtual IReadOnlyList<ColumnModification> ColumnModifications { get; }

-        public virtual EntityState EntityState { get; }

-        public virtual IReadOnlyList<IUpdateEntry> Entries { get; }

-        public virtual bool RequiresResultPropagation { get; }

-        public virtual string Schema { get; }

-        public virtual string TableName { get; }

-        public virtual void AddEntry(IUpdateEntry entry);

-        public virtual void PropagateResults(ValueBuffer valueBuffer);

-    }
-    public abstract class ModificationCommandBatch {
 {
-        protected ModificationCommandBatch();

-        public abstract IReadOnlyList<ModificationCommand> ModificationCommands { get; }

-        public abstract bool AddCommand(ModificationCommand modificationCommand);

-        public abstract void Execute(IRelationalConnection connection);

-        public abstract Task ExecuteAsync(IRelationalConnection connection, CancellationToken cancellationToken = default(CancellationToken));

-    }
-    public abstract class ReaderModificationCommandBatch : ModificationCommandBatch {
 {
-        protected ReaderModificationCommandBatch(IRelationalCommandBuilderFactory commandBuilderFactory, ISqlGenerationHelper sqlGenerationHelper, IUpdateSqlGenerator updateSqlGenerator, IRelationalValueBufferFactoryFactory valueBufferFactoryFactory);

-        protected virtual StringBuilder CachedCommandText { get; set; }

-        protected virtual IList<ResultSetMapping> CommandResultSet { get; }

-        protected virtual int LastCachedCommandIndex { get; set; }

-        public override IReadOnlyList<ModificationCommand> ModificationCommands { get; }

-        protected virtual ISqlGenerationHelper SqlGenerationHelper { get; }

-        protected virtual IUpdateSqlGenerator UpdateSqlGenerator { get; }

-        public override bool AddCommand(ModificationCommand modificationCommand);

-        protected abstract bool CanAddCommand(ModificationCommand modificationCommand);

-        protected abstract void Consume(RelationalDataReader reader);

-        protected abstract Task ConsumeAsync(RelationalDataReader reader, CancellationToken cancellationToken = default(CancellationToken));

-        protected virtual RawSqlCommand CreateStoreCommand();

-        protected virtual IRelationalValueBufferFactory CreateValueBufferFactory(IReadOnlyList<ColumnModification> columnModifications);

-        public override void Execute(IRelationalConnection connection);

-        public override Task ExecuteAsync(IRelationalConnection connection, CancellationToken cancellationToken = default(CancellationToken));

-        protected virtual string GetCommandText();

-        protected virtual int GetParameterCount();

-        protected abstract bool IsCommandTextValid();

-        protected virtual void ResetCommandText();

-        protected virtual void UpdateCachedCommandText(int commandPosition);

-    }
-    public enum ResultSetMapping {
 {
-        LastInResultSet = 2,

-        NoResultSet = 0,

-        NotLastInResultSet = 1,

-    }
-    public class SingularModificationCommandBatch : AffectedCountModificationCommandBatch {
 {
-        public SingularModificationCommandBatch(IRelationalCommandBuilderFactory commandBuilderFactory, ISqlGenerationHelper sqlGenerationHelper, IUpdateSqlGenerator updateSqlGenerator, IRelationalValueBufferFactoryFactory valueBufferFactoryFactory);

-        protected override bool CanAddCommand(ModificationCommand modificationCommand);

-        protected override bool IsCommandTextValid();

-    }
-    public abstract class UpdateSqlGenerator : ISingletonUpdateSqlGenerator, IUpdateSqlGenerator {
 {
-        protected UpdateSqlGenerator(UpdateSqlGeneratorDependencies dependencies);

-        protected virtual UpdateSqlGeneratorDependencies Dependencies { get; }

-        protected virtual ISqlGenerationHelper SqlGenerationHelper { get; }

-        public virtual void AppendBatchHeader(StringBuilder commandStringBuilder);

-        protected virtual void AppendDeleteCommand(StringBuilder commandStringBuilder, string name, string schema, IReadOnlyList<ColumnModification> conditionOperations);

-        protected virtual void AppendDeleteCommandHeader(StringBuilder commandStringBuilder, string name, string schema);

-        public virtual ResultSetMapping AppendDeleteOperation(StringBuilder commandStringBuilder, ModificationCommand command, int commandPosition);

-        protected virtual void AppendFromClause(StringBuilder commandStringBuilder, string name, string schema);

-        protected abstract void AppendIdentityWhereCondition(StringBuilder commandStringBuilder, ColumnModification columnModification);

-        protected virtual void AppendInsertCommand(StringBuilder commandStringBuilder, string name, string schema, IReadOnlyList<ColumnModification> writeOperations);

-        protected virtual void AppendInsertCommandHeader(StringBuilder commandStringBuilder, string name, string schema, IReadOnlyList<ColumnModification> operations);

-        public virtual ResultSetMapping AppendInsertOperation(StringBuilder commandStringBuilder, ModificationCommand command, int commandPosition);

-        public virtual void AppendNextSequenceValueOperation(StringBuilder commandStringBuilder, string name, string schema);

-        protected abstract void AppendRowsAffectedWhereCondition(StringBuilder commandStringBuilder, int expectedRowsAffected);

-        protected virtual ResultSetMapping AppendSelectAffectedCommand(StringBuilder commandStringBuilder, string name, string schema, IReadOnlyList<ColumnModification> readOperations, IReadOnlyList<ColumnModification> conditionOperations, int commandPosition);

-        protected virtual ResultSetMapping AppendSelectAffectedCountCommand(StringBuilder commandStringBuilder, string name, string schema, int commandPosition);

-        protected virtual void AppendSelectCommandHeader(StringBuilder commandStringBuilder, IReadOnlyList<ColumnModification> operations);

-        protected virtual void AppendUpdateCommand(StringBuilder commandStringBuilder, string name, string schema, IReadOnlyList<ColumnModification> writeOperations, IReadOnlyList<ColumnModification> conditionOperations);

-        protected virtual void AppendUpdateCommandHeader(StringBuilder commandStringBuilder, string name, string schema, IReadOnlyList<ColumnModification> operations);

-        public virtual ResultSetMapping AppendUpdateOperation(StringBuilder commandStringBuilder, ModificationCommand command, int commandPosition);

-        protected virtual void AppendValues(StringBuilder commandStringBuilder, IReadOnlyList<ColumnModification> operations);

-        protected virtual void AppendValuesHeader(StringBuilder commandStringBuilder, IReadOnlyList<ColumnModification> operations);

-        protected virtual void AppendWhereAffectedClause(StringBuilder commandStringBuilder, IReadOnlyList<ColumnModification> operations);

-        protected virtual void AppendWhereClause(StringBuilder commandStringBuilder, IReadOnlyList<ColumnModification> operations);

-        protected virtual void AppendWhereCondition(StringBuilder commandStringBuilder, ColumnModification columnModification, bool useOriginalValue);

-        public virtual string GenerateNextSequenceValueOperation(string name, string schema);

-    }
-    public sealed class UpdateSqlGeneratorDependencies {
 {
-        public UpdateSqlGeneratorDependencies(ISqlGenerationHelper sqlGenerationHelper, IRelationalTypeMappingSource typeMappingSource);

-        public ISqlGenerationHelper SqlGenerationHelper { get; }

-        public IRelationalTypeMappingSource TypeMappingSource { get; }

-        public UpdateSqlGeneratorDependencies With(IRelationalTypeMappingSource typeMappingSource);

-        public UpdateSqlGeneratorDependencies With(ISqlGenerationHelper sqlGenerationHelper);

-    }
-}
```

