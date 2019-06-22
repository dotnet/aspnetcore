# Microsoft.EntityFrameworkCore.Migrations.Design

``` diff
-namespace Microsoft.EntityFrameworkCore.Migrations.Design {
 {
-    public class CSharpMigrationOperationGenerator : ICSharpMigrationOperationGenerator {
 {
-        public CSharpMigrationOperationGenerator(CSharpMigrationOperationGeneratorDependencies dependencies);

-        protected virtual CSharpMigrationOperationGeneratorDependencies Dependencies { get; }

-        protected virtual void Annotations(IEnumerable<Annotation> annotations, IndentedStringBuilder builder);

-        protected virtual void Generate(AddColumnOperation operation, IndentedStringBuilder builder);

-        protected virtual void Generate(AddForeignKeyOperation operation, IndentedStringBuilder builder);

-        protected virtual void Generate(AddPrimaryKeyOperation operation, IndentedStringBuilder builder);

-        protected virtual void Generate(AddUniqueConstraintOperation operation, IndentedStringBuilder builder);

-        protected virtual void Generate(AlterColumnOperation operation, IndentedStringBuilder builder);

-        protected virtual void Generate(AlterDatabaseOperation operation, IndentedStringBuilder builder);

-        protected virtual void Generate(AlterSequenceOperation operation, IndentedStringBuilder builder);

-        protected virtual void Generate(AlterTableOperation operation, IndentedStringBuilder builder);

-        protected virtual void Generate(CreateIndexOperation operation, IndentedStringBuilder builder);

-        protected virtual void Generate(CreateSequenceOperation operation, IndentedStringBuilder builder);

-        protected virtual void Generate(CreateTableOperation operation, IndentedStringBuilder builder);

-        protected virtual void Generate(DeleteDataOperation operation, IndentedStringBuilder builder);

-        protected virtual void Generate(DropColumnOperation operation, IndentedStringBuilder builder);

-        protected virtual void Generate(DropForeignKeyOperation operation, IndentedStringBuilder builder);

-        protected virtual void Generate(DropIndexOperation operation, IndentedStringBuilder builder);

-        protected virtual void Generate(DropPrimaryKeyOperation operation, IndentedStringBuilder builder);

-        protected virtual void Generate(DropSchemaOperation operation, IndentedStringBuilder builder);

-        protected virtual void Generate(DropSequenceOperation operation, IndentedStringBuilder builder);

-        protected virtual void Generate(DropTableOperation operation, IndentedStringBuilder builder);

-        protected virtual void Generate(DropUniqueConstraintOperation operation, IndentedStringBuilder builder);

-        protected virtual void Generate(EnsureSchemaOperation operation, IndentedStringBuilder builder);

-        protected virtual void Generate(InsertDataOperation operation, IndentedStringBuilder builder);

-        protected virtual void Generate(MigrationOperation operation, IndentedStringBuilder builder);

-        protected virtual void Generate(RenameColumnOperation operation, IndentedStringBuilder builder);

-        protected virtual void Generate(RenameIndexOperation operation, IndentedStringBuilder builder);

-        protected virtual void Generate(RenameSequenceOperation operation, IndentedStringBuilder builder);

-        protected virtual void Generate(RenameTableOperation operation, IndentedStringBuilder builder);

-        protected virtual void Generate(RestartSequenceOperation operation, IndentedStringBuilder builder);

-        protected virtual void Generate(SqlOperation operation, IndentedStringBuilder builder);

-        protected virtual void Generate(UpdateDataOperation operation, IndentedStringBuilder builder);

-        public virtual void Generate(string builderName, IReadOnlyList<MigrationOperation> operations, IndentedStringBuilder builder);

-        protected virtual void OldAnnotations(IEnumerable<Annotation> annotations, IndentedStringBuilder builder);

-    }
-    public sealed class CSharpMigrationOperationGeneratorDependencies {
 {
-        public CSharpMigrationOperationGeneratorDependencies(ICSharpHelper csharpHelper);

-        public ICSharpHelper CSharpHelper { get; }

-        public CSharpMigrationOperationGeneratorDependencies With(ICSharpHelper csharpHelper);

-    }
-    public class CSharpMigrationsGenerator : MigrationsCodeGenerator {
 {
-        public CSharpMigrationsGenerator(MigrationsCodeGeneratorDependencies dependencies, CSharpMigrationsGeneratorDependencies csharpDependencies);

-        protected virtual CSharpMigrationsGeneratorDependencies CSharpDependencies { get; }

-        public override string FileExtension { get; }

-        public override string Language { get; }

-        public override string GenerateMetadata(string migrationNamespace, Type contextType, string migrationName, string migrationId, IModel targetModel);

-        public override string GenerateMigration(string migrationNamespace, string migrationName, IReadOnlyList<MigrationOperation> upOperations, IReadOnlyList<MigrationOperation> downOperations);

-        public override string GenerateSnapshot(string modelSnapshotNamespace, Type contextType, string modelSnapshotName, IModel model);

-    }
-    public sealed class CSharpMigrationsGeneratorDependencies {
 {
-        public CSharpMigrationsGeneratorDependencies(ICSharpHelper csharpHelper, ICSharpMigrationOperationGenerator csharpMigrationOperationGenerator, ICSharpSnapshotGenerator csharpSnapshotGenerator);

-        public ICSharpHelper CSharpHelper { get; }

-        public ICSharpMigrationOperationGenerator CSharpMigrationOperationGenerator { get; }

-        public ICSharpSnapshotGenerator CSharpSnapshotGenerator { get; }

-        public CSharpMigrationsGeneratorDependencies With(ICSharpHelper csharpHelper);

-        public CSharpMigrationsGeneratorDependencies With(ICSharpMigrationOperationGenerator csharpMigrationOperationGenerator);

-        public CSharpMigrationsGeneratorDependencies With(ICSharpSnapshotGenerator csharpSnapshotGenerator);

-    }
-    public class CSharpSnapshotGenerator : ICSharpSnapshotGenerator {
 {
-        public CSharpSnapshotGenerator(CSharpSnapshotGeneratorDependencies dependencies);

-        protected virtual CSharpSnapshotGeneratorDependencies Dependencies { get; }

-        public virtual void Generate(string builderName, IModel model, IndentedStringBuilder stringBuilder);

-        protected virtual void GenerateAnnotation(IAnnotation annotation, IndentedStringBuilder stringBuilder);

-        protected virtual void GenerateAnnotations(IReadOnlyList<IAnnotation> annotations, IndentedStringBuilder stringBuilder);

-        protected virtual void GenerateBaseType(string builderName, IEntityType baseType, IndentedStringBuilder stringBuilder);

-        protected virtual void GenerateData(string builderName, IEnumerable<IProperty> properties, IEnumerable<IDictionary<string, object>> data, IndentedStringBuilder stringBuilder);

-        protected virtual void GenerateEntityType(string builderName, IEntityType entityType, IndentedStringBuilder stringBuilder);

-        protected virtual void GenerateEntityTypeAnnotations(string builderName, IEntityType entityType, IndentedStringBuilder stringBuilder);

-        protected virtual void GenerateEntityTypeRelationships(string builderName, IEntityType entityType, IndentedStringBuilder stringBuilder);

-        protected virtual void GenerateEntityTypes(string builderName, IReadOnlyList<IEntityType> entityTypes, IndentedStringBuilder stringBuilder);

-        protected virtual void GenerateFluentApiForAnnotation(ref List<IAnnotation> annotations, string annotationName, Func<IAnnotation, object> annotationValueFunc, string fluentApiMethodName, IndentedStringBuilder stringBuilder);

-        protected virtual void GenerateFluentApiForAnnotation(ref List<IAnnotation> annotations, string annotationName, string fluentApiMethodName, IndentedStringBuilder stringBuilder);

-        protected virtual void GenerateForeignKey(string builderName, IForeignKey foreignKey, IndentedStringBuilder stringBuilder);

-        protected virtual void GenerateForeignKeyAnnotations(IForeignKey foreignKey, IndentedStringBuilder stringBuilder);

-        protected virtual void GenerateForeignKeys(string builderName, IEnumerable<IForeignKey> foreignKeys, IndentedStringBuilder stringBuilder);

-        protected virtual void GenerateIndex(string builderName, IIndex index, IndentedStringBuilder stringBuilder);

-        protected virtual void GenerateIndexes(string builderName, IEnumerable<IIndex> indexes, IndentedStringBuilder stringBuilder);

-        protected virtual void GenerateKey(string builderName, IKey key, IndentedStringBuilder stringBuilder, bool primary = false);

-        protected virtual void GenerateKeys(string builderName, IEnumerable<IKey> keys, IKey primaryKey, IndentedStringBuilder stringBuilder);

-        protected virtual void GenerateOwnedType(string builderName, IForeignKey ownership, IndentedStringBuilder stringBuilder);

-        protected virtual void GenerateOwnedTypes(string builderName, IEnumerable<IForeignKey> ownerships, IndentedStringBuilder stringBuilder);

-        protected virtual void GenerateProperties(string builderName, IEnumerable<IProperty> properties, IndentedStringBuilder stringBuilder);

-        protected virtual void GenerateProperty(string builderName, IProperty property, IndentedStringBuilder stringBuilder);

-        protected virtual void GeneratePropertyAnnotations(IProperty property, IndentedStringBuilder stringBuilder);

-        protected virtual void GenerateRelationships(string builderName, IEntityType entityType, IndentedStringBuilder stringBuilder);

-        protected virtual void IgnoreAnnotations(IList<IAnnotation> annotations, params string[] annotationNames);

-        protected virtual void IgnoreAnnotationTypes(IList<IAnnotation> annotations, params string[] annotationPrefixes);

-    }
-    public sealed class CSharpSnapshotGeneratorDependencies {
 {
-        public CSharpSnapshotGeneratorDependencies(ICSharpHelper csharpHelper);

-        public ICSharpHelper CSharpHelper { get; }

-        public CSharpSnapshotGeneratorDependencies With(ICSharpHelper csharpHelper);

-    }
-    public interface ICSharpMigrationOperationGenerator {
 {
-        void Generate(string builderName, IReadOnlyList<MigrationOperation> operations, IndentedStringBuilder builder);

-    }
-    public interface ICSharpSnapshotGenerator {
 {
-        void Generate(string builderName, IModel model, IndentedStringBuilder stringBuilder);

-    }
-    public interface IMigrationsCodeGenerator : ILanguageBasedService {
 {
-        string FileExtension { get; }

-        string GenerateMetadata(string migrationNamespace, Type contextType, string migrationName, string migrationId, IModel targetModel);

-        string GenerateMigration(string migrationNamespace, string migrationName, IReadOnlyList<MigrationOperation> upOperations, IReadOnlyList<MigrationOperation> downOperations);

-        string GenerateSnapshot(string modelSnapshotNamespace, Type contextType, string modelSnapshotName, IModel model);

-    }
-    public interface IMigrationsCodeGeneratorSelector {
 {
-        IMigrationsCodeGenerator Select(string language);

-    }
-    public interface IMigrationsScaffolder {
 {
-        MigrationFiles RemoveMigration(string projectDir, string rootNamespace, bool force, string language);

-        MigrationFiles Save(string projectDir, ScaffoldedMigration migration, string outputDir);

-        ScaffoldedMigration ScaffoldMigration(string migrationName, string rootNamespace, string subNamespace = null, string language = null);

-    }
-    public class MigrationFiles {
 {
-        public MigrationFiles();

-        public virtual string MetadataFile { get; set; }

-        public virtual string MigrationFile { get; set; }

-        public virtual string SnapshotFile { get; set; }

-    }
-    public abstract class MigrationsCodeGenerator : ILanguageBasedService, IMigrationsCodeGenerator {
 {
-        public MigrationsCodeGenerator(MigrationsCodeGeneratorDependencies dependencies);

-        protected virtual MigrationsCodeGeneratorDependencies Dependencies { get; }

-        public abstract string FileExtension { get; }

-        public virtual string Language { get; }

-        public abstract string GenerateMetadata(string migrationNamespace, Type contextType, string migrationName, string migrationId, IModel targetModel);

-        public abstract string GenerateMigration(string migrationNamespace, string migrationName, IReadOnlyList<MigrationOperation> upOperations, IReadOnlyList<MigrationOperation> downOperations);

-        public abstract string GenerateSnapshot(string modelSnapshotNamespace, Type contextType, string modelSnapshotName, IModel model);

-        protected virtual IEnumerable<string> GetNamespaces(IModel model);

-        protected virtual IEnumerable<string> GetNamespaces(IEnumerable<MigrationOperation> operations);

-    }
-    public sealed class MigrationsCodeGeneratorDependencies {
 {
-        public MigrationsCodeGeneratorDependencies();

-    }
-    public class MigrationsScaffolder : IMigrationsScaffolder {
 {
-        public MigrationsScaffolder(MigrationsScaffolderDependencies dependencies);

-        protected virtual MigrationsScaffolderDependencies Dependencies { get; }

-        protected virtual string GetDirectory(string projectDir, string siblingFileName, string subnamespace);

-        protected virtual string GetNamespace(Type siblingType, string defaultNamespace);

-        protected virtual string GetSubNamespace(string rootNamespace, string @namespace);

-        public virtual MigrationFiles RemoveMigration(string projectDir, string rootNamespace, bool force);

-        public virtual MigrationFiles RemoveMigration(string projectDir, string rootNamespace, bool force, string language);

-        public virtual MigrationFiles Save(string projectDir, ScaffoldedMigration migration, string outputDir);

-        public virtual ScaffoldedMigration ScaffoldMigration(string migrationName, string rootNamespace, string subNamespace);

-        public virtual ScaffoldedMigration ScaffoldMigration(string migrationName, string rootNamespace, string subNamespace = null, string language = null);

-        protected virtual string TryGetProjectFile(string projectDir, string fileName);

-    }
-    public sealed class MigrationsScaffolderDependencies {
 {
-        public MigrationsScaffolderDependencies(ICurrentDbContext currentDbContext, IModel model, IMigrationsAssembly migrationsAssembly, IMigrationsModelDiffer migrationsModelDiffer, IMigrationsIdGenerator migrationsIdGenerator, IMigrationsCodeGeneratorSelector migrationsCodeGeneratorSelector, IHistoryRepository historyRepository, IOperationReporter operationReporter, IDatabaseProvider databaseProvider, ISnapshotModelProcessor snapshotModelProcessor, IMigrator migrator);

-        public ICurrentDbContext CurrentDbContext { get; }

-        public IDatabaseProvider DatabaseProvider { get; }

-        public IHistoryRepository HistoryRepository { get; }

-        public IMigrationsCodeGenerator MigrationCodeGenerator { get; }

-        public IMigrationsAssembly MigrationsAssembly { get; }

-        public IMigrationsCodeGeneratorSelector MigrationsCodeGeneratorSelector { get; }

-        public IMigrationsIdGenerator MigrationsIdGenerator { get; }

-        public IMigrationsModelDiffer MigrationsModelDiffer { get; }

-        public IMigrator Migrator { get; }

-        public IModel Model { get; }

-        public IOperationReporter OperationReporter { get; }

-        public ISnapshotModelProcessor SnapshotModelProcessor { get; }

-        public MigrationsScaffolderDependencies With(IOperationReporter operationReporter);

-        public MigrationsScaffolderDependencies With(ICurrentDbContext currentDbContext);

-        public MigrationsScaffolderDependencies With(IModel model);

-        public MigrationsScaffolderDependencies With(IMigrationsCodeGenerator migrationCodeGenerator);

-        public MigrationsScaffolderDependencies With(IMigrationsCodeGeneratorSelector migrationsCodeGeneratorSelector);

-        public MigrationsScaffolderDependencies With(IHistoryRepository historyRepository);

-        public MigrationsScaffolderDependencies With(IMigrationsAssembly migrationsAssembly);

-        public MigrationsScaffolderDependencies With(IMigrationsIdGenerator migrationsIdGenerator);

-        public MigrationsScaffolderDependencies With(IMigrationsModelDiffer migrationsModelDiffer);

-        public MigrationsScaffolderDependencies With(IMigrator migrator);

-        public MigrationsScaffolderDependencies With(ISnapshotModelProcessor snapshotModelProcessor);

-        public MigrationsScaffolderDependencies With(IDatabaseProvider databaseProvider);

-    }
-    public class ScaffoldedMigration {
 {
-        public ScaffoldedMigration(string fileExtension, string previousMigrationId, string migrationCode, string migrationId, string metadataCode, string migrationSubNamespace, string snapshotCode, string snapshotName, string snapshotSubNamespace);

-        public virtual string FileExtension { get; }

-        public virtual string MetadataCode { get; }

-        public virtual string MigrationCode { get; }

-        public virtual string MigrationId { get; }

-        public virtual string MigrationSubNamespace { get; }

-        public virtual string PreviousMigrationId { get; }

-        public virtual string SnapshotCode { get; }

-        public virtual string SnapshotName { get; }

-        public virtual string SnapshotSubnamespace { get; }

-    }
-}
```

