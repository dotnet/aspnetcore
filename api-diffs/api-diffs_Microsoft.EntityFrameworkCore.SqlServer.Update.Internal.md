# Microsoft.EntityFrameworkCore.SqlServer.Update.Internal

``` diff
-namespace Microsoft.EntityFrameworkCore.SqlServer.Update.Internal {
 {
-    public interface ISqlServerUpdateSqlGenerator : ISingletonUpdateSqlGenerator, IUpdateSqlGenerator {
 {
-        ResultSetMapping AppendBulkInsertOperation(StringBuilder commandStringBuilder, IReadOnlyList<ModificationCommand> modificationCommands, int commandPosition);

-    }
-    public class SqlServerModificationCommandBatch : AffectedCountModificationCommandBatch {
 {
-        public SqlServerModificationCommandBatch(IRelationalCommandBuilderFactory commandBuilderFactory, ISqlGenerationHelper sqlGenerationHelper, ISqlServerUpdateSqlGenerator updateSqlGenerator, IRelationalValueBufferFactoryFactory valueBufferFactoryFactory, Nullable<int> maxBatchSize);

-        protected virtual new ISqlServerUpdateSqlGenerator UpdateSqlGenerator { get; }

-        protected override bool CanAddCommand(ModificationCommand modificationCommand);

-        protected override string GetCommandText();

-        protected override int GetParameterCount();

-        protected override bool IsCommandTextValid();

-        protected override void ResetCommandText();

-        protected override void UpdateCachedCommandText(int commandPosition);

-    }
-    public class SqlServerModificationCommandBatchFactory : IModificationCommandBatchFactory {
 {
-        public SqlServerModificationCommandBatchFactory(IRelationalCommandBuilderFactory commandBuilderFactory, ISqlGenerationHelper sqlGenerationHelper, ISqlServerUpdateSqlGenerator updateSqlGenerator, IRelationalValueBufferFactoryFactory valueBufferFactoryFactory, IDbContextOptions options);

-        public virtual ModificationCommandBatch Create();

-    }
-    public class SqlServerUpdateSqlGenerator : UpdateSqlGenerator, ISingletonUpdateSqlGenerator, ISqlServerUpdateSqlGenerator, IUpdateSqlGenerator {
 {
-        public SqlServerUpdateSqlGenerator(UpdateSqlGeneratorDependencies dependencies);

-        public override void AppendBatchHeader(StringBuilder commandStringBuilder);

-        public virtual ResultSetMapping AppendBulkInsertOperation(StringBuilder commandStringBuilder, IReadOnlyList<ModificationCommand> modificationCommands, int commandPosition);

-        protected override void AppendIdentityWhereCondition(StringBuilder commandStringBuilder, ColumnModification columnModification);

-        protected override void AppendRowsAffectedWhereCondition(StringBuilder commandStringBuilder, int expectedRowsAffected);

-        protected override ResultSetMapping AppendSelectAffectedCountCommand(StringBuilder commandStringBuilder, string name, string schema, int commandPosition);

-    }
-}
```

