# Microsoft.EntityFrameworkCore.Migrations.Internal

``` diff
-namespace Microsoft.EntityFrameworkCore.Migrations.Internal {
 {
-    public interface ISnapshotModelProcessor {
 {
-        IModel Process(IModel model);

-    }
-    public class MigrationCommandExecutor : IMigrationCommandExecutor {
 {
-        public MigrationCommandExecutor();

-        public virtual void ExecuteNonQuery(IEnumerable<MigrationCommand> migrationCommands, IRelationalConnection connection);

-        public virtual Task ExecuteNonQueryAsync(IEnumerable<MigrationCommand> migrationCommands, IRelationalConnection connection, CancellationToken cancellationToken = default(CancellationToken));

-    }
-    public static class MigrationExtensions {
 {
-        public static string GetId(this Migration migration);

-    }
-    public class MigrationsAssembly : IMigrationsAssembly {
 {
-        public MigrationsAssembly(ICurrentDbContext currentContext, IDbContextOptions options, IMigrationsIdGenerator idGenerator, IDiagnosticsLogger<DbLoggerCategory.Migrations> logger);

-        public virtual Assembly Assembly { get; }

-        public virtual IReadOnlyDictionary<string, TypeInfo> Migrations { get; }

-        public virtual ModelSnapshot ModelSnapshot { get; }

-        public virtual Migration CreateMigration(TypeInfo migrationClass, string activeProvider);

-        public virtual string FindMigrationId(string nameOrId);

-    }
-    public class MigrationsCodeGeneratorSelector : LanguageBasedSelector<IMigrationsCodeGenerator>, IMigrationsCodeGeneratorSelector {
 {
-        public MigrationsCodeGeneratorSelector(IEnumerable<IMigrationsCodeGenerator> services);

-        public virtual IMigrationsCodeGenerator Override { get; set; }

-        public override IMigrationsCodeGenerator Select(string language);

-    }
-    public class MigrationsIdGenerator : IMigrationsIdGenerator {
 {
-        public MigrationsIdGenerator();

-        public virtual string GenerateId(string name);

-        public virtual string GetName(string id);

-        public virtual bool IsValidId(string value);

-    }
-    public class MigrationsModelDiffer : IMigrationsModelDiffer {
 {
-        public MigrationsModelDiffer(IRelationalTypeMappingSource typeMappingSource, IMigrationsAnnotationProvider migrationsAnnotations, IChangeDetector changeDetector, StateManagerDependencies stateManagerDependencies, CommandBatchPreparerDependencies commandBatchPreparerDependencies);

-        protected virtual IChangeDetector ChangeDetector { get; }

-        protected virtual CommandBatchPreparerDependencies CommandBatchPreparerDependencies { get; }

-        protected virtual IMigrationsAnnotationProvider MigrationsAnnotations { get; }

-        protected virtual StateManagerDependencies StateManagerDependencies { get; }

-        protected virtual IRelationalTypeMappingSource TypeMappingSource { get; }

-        protected virtual IEnumerable<MigrationOperation> Add(IForeignKey target, MigrationsModelDiffer.DiffContext diffContext);

-        protected virtual IEnumerable<MigrationOperation> Add(IIndex target, MigrationsModelDiffer.DiffContext diffContext);

-        protected virtual IEnumerable<MigrationOperation> Add(IKey target, MigrationsModelDiffer.DiffContext diffContext);

-        protected virtual IEnumerable<MigrationOperation> Add(IModel target, MigrationsModelDiffer.DiffContext diffContext);

-        protected virtual IEnumerable<MigrationOperation> Add(TableMapping target, MigrationsModelDiffer.DiffContext diffContext);

-        protected virtual IEnumerable<MigrationOperation> Add(IProperty target, MigrationsModelDiffer.DiffContext diffContext, bool inline = false);

-        protected virtual IEnumerable<MigrationOperation> Add(ISequence target, MigrationsModelDiffer.DiffContext diffContext);

-        protected virtual IEnumerable<MigrationOperation> Add(string target, MigrationsModelDiffer.DiffContext diffContext);

-        protected virtual IEnumerable<MigrationOperation> Diff(IForeignKey source, IForeignKey target, MigrationsModelDiffer.DiffContext diffContext);

-        protected virtual IEnumerable<MigrationOperation> Diff(IIndex source, IIndex target, MigrationsModelDiffer.DiffContext diffContext);

-        protected virtual IEnumerable<MigrationOperation> Diff(IKey source, IKey target, MigrationsModelDiffer.DiffContext diffContext);

-        protected virtual IEnumerable<MigrationOperation> Diff(IModel source, IModel target, MigrationsModelDiffer.DiffContext diffContext);

-        protected virtual IEnumerable<MigrationOperation> Diff(TableMapping source, TableMapping target, MigrationsModelDiffer.DiffContext diffContext);

-        protected virtual IEnumerable<MigrationOperation> Diff(IProperty source, IProperty target, MigrationsModelDiffer.DiffContext diffContext);

-        protected virtual IEnumerable<MigrationOperation> Diff(ISequence source, ISequence target, MigrationsModelDiffer.DiffContext diffContext);

-        protected virtual IEnumerable<MigrationOperation> Diff(IEnumerable<IForeignKey> source, IEnumerable<IForeignKey> target, MigrationsModelDiffer.DiffContext diffContext);

-        protected virtual IEnumerable<MigrationOperation> Diff(IEnumerable<IIndex> source, IEnumerable<IIndex> target, MigrationsModelDiffer.DiffContext diffContext);

-        protected virtual IEnumerable<MigrationOperation> Diff(IEnumerable<IKey> source, IEnumerable<IKey> target, MigrationsModelDiffer.DiffContext diffContext);

-        protected virtual IEnumerable<MigrationOperation> Diff(IEnumerable<TableMapping> source, IEnumerable<TableMapping> target, MigrationsModelDiffer.DiffContext diffContext);

-        protected virtual IEnumerable<MigrationOperation> Diff(IEnumerable<IProperty> source, IEnumerable<IProperty> target, MigrationsModelDiffer.DiffContext diffContext);

-        protected virtual IEnumerable<MigrationOperation> Diff(IEnumerable<ISequence> source, IEnumerable<ISequence> target, MigrationsModelDiffer.DiffContext diffContext);

-        protected virtual IEnumerable<MigrationOperation> Diff(IEnumerable<string> source, IEnumerable<string> target, MigrationsModelDiffer.DiffContext diffContext);

-        protected virtual IEnumerable<MigrationOperation> Diff(string source, string target, MigrationsModelDiffer.DiffContext diffContext);

-        protected virtual IEnumerable<MigrationOperation> DiffCollection<T>(IEnumerable<T> sources, IEnumerable<T> targets, MigrationsModelDiffer.DiffContext diffContext, Func<T, T, MigrationsModelDiffer.DiffContext, IEnumerable<MigrationOperation>> diff, Func<T, MigrationsModelDiffer.DiffContext, IEnumerable<MigrationOperation>> add, Func<T, MigrationsModelDiffer.DiffContext, IEnumerable<MigrationOperation>> remove, params Func<T, T, MigrationsModelDiffer.DiffContext, bool>[] predicates);

-        protected virtual void DiffData(TableMapping source, TableMapping target, MigrationsModelDiffer.DiffContext diffContext);

-        protected virtual string[] GetColumns(IEnumerable<IProperty> properties);

-        protected virtual IEnumerable<MigrationOperation> GetDataOperations();

-        protected virtual object GetDefaultValue(Type type);

-        public virtual IReadOnlyList<MigrationOperation> GetDifferences(IModel source, IModel target);

-        protected virtual IEnumerable<string> GetSchemas(IModel model);

-        public virtual bool HasDifferences(IModel source, IModel target);

-        protected virtual bool HasDifferences(IEnumerable<IAnnotation> source, IEnumerable<IAnnotation> target);

-        protected virtual IEnumerable<MigrationOperation> Remove(IForeignKey source, MigrationsModelDiffer.DiffContext diffContext);

-        protected virtual IEnumerable<MigrationOperation> Remove(IIndex source, MigrationsModelDiffer.DiffContext diffContext);

-        protected virtual IEnumerable<MigrationOperation> Remove(IKey source, MigrationsModelDiffer.DiffContext diffContext);

-        protected virtual IEnumerable<MigrationOperation> Remove(IModel source, MigrationsModelDiffer.DiffContext diffContext);

-        protected virtual IEnumerable<MigrationOperation> Remove(TableMapping source, MigrationsModelDiffer.DiffContext diffContext);

-        protected virtual IEnumerable<MigrationOperation> Remove(IProperty source, MigrationsModelDiffer.DiffContext diffContext);

-        protected virtual IEnumerable<MigrationOperation> Remove(ISequence source, MigrationsModelDiffer.DiffContext diffContext);

-        protected virtual IEnumerable<MigrationOperation> Remove(string source, MigrationsModelDiffer.DiffContext diffContext);

-        protected virtual IReadOnlyList<MigrationOperation> Sort(IEnumerable<MigrationOperation> operations, MigrationsModelDiffer.DiffContext diffContext);

-        protected virtual void TrackData(IModel source, IModel target);

-        protected class DiffContext {
 {
-            public DiffContext(IModel source, IModel target);

-            public virtual void AddCreate(IEntityType target, CreateTableOperation operation);

-            public virtual void AddDrop(TableMapping source, DropTableOperation operation);

-            public virtual void AddMapping<T>(T source, T target);

-            public virtual CreateTableOperation FindCreate(IEntityType target);

-            public virtual DropTableOperation FindDrop(IEntityType source);

-            public virtual IProperty FindSource(IProperty target);

-            public virtual T FindSource<T>(T target);

-            public virtual TableMapping FindSourceTable(IEntityType entityType);

-            public virtual TableMapping FindTable(DropTableOperation operation);

-            public virtual TableMapping FindTargetTable(IEntityType entityType);

-            public virtual IEnumerable<TableMapping> GetSourceTables();

-            public virtual IEnumerable<TableMapping> GetTargetTables();

-        }
-    }
-    public class Migrator : IMigrator {
 {
-        public Migrator(IMigrationsAssembly migrationsAssembly, IHistoryRepository historyRepository, IDatabaseCreator databaseCreator, IMigrationsSqlGenerator migrationsSqlGenerator, IRawSqlCommandBuilder rawSqlCommandBuilder, IMigrationCommandExecutor migrationCommandExecutor, IRelationalConnection connection, ISqlGenerationHelper sqlGenerationHelper, IDiagnosticsLogger<DbLoggerCategory.Migrations> logger, IDatabaseProvider databaseProvider);

-        protected virtual IReadOnlyList<MigrationCommand> GenerateDownSql(Migration migration, Migration previousMigration);

-        public virtual string GenerateScript(string fromMigration = null, string toMigration = null, bool idempotent = false);

-        protected virtual IReadOnlyList<MigrationCommand> GenerateUpSql(Migration migration);

-        public virtual void Migrate(string targetMigration = null);

-        public virtual Task MigrateAsync(string targetMigration = null, CancellationToken cancellationToken = default(CancellationToken));

-        protected virtual void PopulateMigrations(IEnumerable<string> appliedMigrationEntries, string targetMigration, out IReadOnlyList<Migration> migrationsToApply, out IReadOnlyList<Migration> migrationsToRevert, out Migration actualTargetMigration);

-    }
-    public class SnapshotModelProcessor : ISnapshotModelProcessor {
 {
-        public SnapshotModelProcessor(IOperationReporter operationReporter);

-        public virtual IModel Process(IModel model);

-    }
-}
```

