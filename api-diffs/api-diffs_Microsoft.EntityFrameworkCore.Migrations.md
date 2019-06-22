# Microsoft.EntityFrameworkCore.Migrations

``` diff
-namespace Microsoft.EntityFrameworkCore.Migrations {
 {
-    public abstract class HistoryRepository : IHistoryRepository {
 {
-        public const string DefaultTableName = "__EFMigrationsHistory";

-        protected HistoryRepository(HistoryRepositoryDependencies dependencies);

-        protected virtual HistoryRepositoryDependencies Dependencies { get; }

-        protected abstract string ExistsSql { get; }

-        protected virtual string GetAppliedMigrationsSql { get; }

-        protected virtual string MigrationIdColumnName { get; }

-        protected virtual string ProductVersionColumnName { get; }

-        protected virtual ISqlGenerationHelper SqlGenerationHelper { get; }

-        protected virtual string TableName { get; }

-        protected virtual string TableSchema { get; }

-        protected virtual void ConfigureTable(EntityTypeBuilder<HistoryRow> history);

-        public virtual bool Exists();

-        public virtual Task<bool> ExistsAsync(CancellationToken cancellationToken = default(CancellationToken));

-        public virtual IReadOnlyList<HistoryRow> GetAppliedMigrations();

-        public virtual Task<IReadOnlyList<HistoryRow>> GetAppliedMigrationsAsync(CancellationToken cancellationToken = default(CancellationToken));

-        public abstract string GetBeginIfExistsScript(string migrationId);

-        public abstract string GetBeginIfNotExistsScript(string migrationId);

-        public abstract string GetCreateIfNotExistsScript();

-        public virtual string GetCreateScript();

-        public virtual string GetDeleteScript(string migrationId);

-        public abstract string GetEndIfScript();

-        public virtual string GetInsertScript(HistoryRow row);

-        protected abstract bool InterpretExistsResult(object value);

-    }
-    public sealed class HistoryRepositoryDependencies {
 {
-        public HistoryRepositoryDependencies(IRelationalDatabaseCreator databaseCreator, IRawSqlCommandBuilder rawSqlCommandBuilder, IRelationalConnection connection, IDbContextOptions options, IMigrationsModelDiffer modelDiffer, IMigrationsSqlGenerator migrationsSqlGenerator, ISqlGenerationHelper sqlGenerationHelper, ICoreConventionSetBuilder coreConventionSetBuilder, IEnumerable<IConventionSetBuilder> conventionSetBuilders, IRelationalTypeMappingSource typeMappingSource);

-        public IRelationalConnection Connection { get; }

-        public IConventionSetBuilder ConventionSetBuilder { get; }

-        public ICoreConventionSetBuilder CoreConventionSetBuilder { get; }

-        public IRelationalDatabaseCreator DatabaseCreator { get; }

-        public IMigrationsSqlGenerator MigrationsSqlGenerator { get; }

-        public IMigrationsModelDiffer ModelDiffer { get; }

-        public IDbContextOptions Options { get; }

-        public IRawSqlCommandBuilder RawSqlCommandBuilder { get; }

-        public ISqlGenerationHelper SqlGenerationHelper { get; }

-        public IRelationalTypeMappingSource TypeMappingSource { get; }

-        public HistoryRepositoryDependencies With(IDbContextOptions options);

-        public HistoryRepositoryDependencies With(IConventionSetBuilder conventionSetBuilder);

-        public HistoryRepositoryDependencies With(ICoreConventionSetBuilder coreConventionSetBuilder);

-        public HistoryRepositoryDependencies With(IMigrationsModelDiffer modelDiffer);

-        public HistoryRepositoryDependencies With(IMigrationsSqlGenerator migrationsSqlGenerator);

-        public HistoryRepositoryDependencies With(IRawSqlCommandBuilder rawSqlCommandBuilder);

-        public HistoryRepositoryDependencies With(IRelationalConnection connection);

-        public HistoryRepositoryDependencies With(IRelationalDatabaseCreator databaseCreator);

-        public HistoryRepositoryDependencies With(IRelationalTypeMappingSource typeMappingSource);

-        public HistoryRepositoryDependencies With(ISqlGenerationHelper sqlGenerationHelper);

-    }
-    public class HistoryRow {
 {
-        public HistoryRow(string migrationId, string productVersion);

-        public virtual string MigrationId { get; }

-        public virtual string ProductVersion { get; }

-    }
-    public interface IHistoryRepository {
 {
-        bool Exists();

-        Task<bool> ExistsAsync(CancellationToken cancellationToken = default(CancellationToken));

-        IReadOnlyList<HistoryRow> GetAppliedMigrations();

-        Task<IReadOnlyList<HistoryRow>> GetAppliedMigrationsAsync(CancellationToken cancellationToken = default(CancellationToken));

-        string GetBeginIfExistsScript(string migrationId);

-        string GetBeginIfNotExistsScript(string migrationId);

-        string GetCreateIfNotExistsScript();

-        string GetCreateScript();

-        string GetDeleteScript(string migrationId);

-        string GetEndIfScript();

-        string GetInsertScript(HistoryRow row);

-    }
-    public interface IMigrationCommandExecutor {
 {
-        void ExecuteNonQuery(IEnumerable<MigrationCommand> migrationCommands, IRelationalConnection connection);

-        Task ExecuteNonQueryAsync(IEnumerable<MigrationCommand> migrationCommands, IRelationalConnection connection, CancellationToken cancellationToken = default(CancellationToken));

-    }
-    public interface IMigrationsAnnotationProvider {
 {
-        IEnumerable<IAnnotation> For(IEntityType entityType);

-        IEnumerable<IAnnotation> For(IForeignKey foreignKey);

-        IEnumerable<IAnnotation> For(IIndex index);

-        IEnumerable<IAnnotation> For(IKey key);

-        IEnumerable<IAnnotation> For(IModel model);

-        IEnumerable<IAnnotation> For(IProperty property);

-        IEnumerable<IAnnotation> For(ISequence sequence);

-        IEnumerable<IAnnotation> ForRemove(IEntityType entityType);

-        IEnumerable<IAnnotation> ForRemove(IForeignKey foreignKey);

-        IEnumerable<IAnnotation> ForRemove(IIndex index);

-        IEnumerable<IAnnotation> ForRemove(IKey key);

-        IEnumerable<IAnnotation> ForRemove(IModel model);

-        IEnumerable<IAnnotation> ForRemove(IProperty property);

-        IEnumerable<IAnnotation> ForRemove(ISequence sequence);

-    }
-    public interface IMigrationsAssembly {
 {
-        Assembly Assembly { get; }

-        IReadOnlyDictionary<string, TypeInfo> Migrations { get; }

-        ModelSnapshot ModelSnapshot { get; }

-        Migration CreateMigration(TypeInfo migrationClass, string activeProvider);

-        string FindMigrationId(string nameOrId);

-    }
-    public interface IMigrationsIdGenerator {
 {
-        string GenerateId(string name);

-        string GetName(string id);

-        bool IsValidId(string value);

-    }
-    public interface IMigrationsModelDiffer {
 {
-        IReadOnlyList<MigrationOperation> GetDifferences(IModel source, IModel target);

-        bool HasDifferences(IModel source, IModel target);

-    }
-    public interface IMigrationsSqlGenerator {
 {
-        IReadOnlyList<MigrationCommand> Generate(IReadOnlyList<MigrationOperation> operations, IModel model = null);

-    }
-    public interface IMigrator {
 {
-        string GenerateScript(string fromMigration = null, string toMigration = null, bool idempotent = false);

-        void Migrate(string targetMigration = null);

-        Task MigrateAsync(string targetMigration = null, CancellationToken cancellationToken = default(CancellationToken));

-    }
-    public abstract class Migration {
 {
-        public const string InitialDatabase = "0";

-        protected Migration();

-        public virtual string ActiveProvider { get; set; }

-        public virtual IReadOnlyList<MigrationOperation> DownOperations { get; }

-        public virtual IModel TargetModel { get; }

-        public virtual IReadOnlyList<MigrationOperation> UpOperations { get; }

-        protected virtual void BuildTargetModel(ModelBuilder modelBuilder);

-        protected virtual void Down(MigrationBuilder migrationBuilder);

-        protected abstract void Up(MigrationBuilder migrationBuilder);

-    }
-    public sealed class MigrationAttribute : Attribute {
 {
-        public MigrationAttribute(string id);

-        public string Id { get; }

-    }
-    public class MigrationBuilder {
 {
-        public MigrationBuilder(string activeProvider);

-        public virtual string ActiveProvider { get; }

-        public virtual List<MigrationOperation> Operations { get; }

-        public virtual OperationBuilder<AddColumnOperation> AddColumn<T>(string name, string table, string type, Nullable<bool> unicode, Nullable<int> maxLength, bool rowVersion, string schema, bool nullable, object defaultValue, string defaultValueSql, string computedColumnSql);

-        public virtual OperationBuilder<AddColumnOperation> AddColumn<T>(string name, string table, string type = null, Nullable<bool> unicode = default(Nullable<bool>), Nullable<int> maxLength = default(Nullable<int>), bool rowVersion = false, string schema = null, bool nullable = false, object defaultValue = null, string defaultValueSql = null, string computedColumnSql = null, Nullable<bool> fixedLength = default(Nullable<bool>));

-        public virtual OperationBuilder<AddForeignKeyOperation> AddForeignKey(string name, string table, string column, string principalTable, string schema = null, string principalSchema = null, string principalColumn = null, ReferentialAction onUpdate = ReferentialAction.NoAction, ReferentialAction onDelete = ReferentialAction.NoAction);

-        public virtual OperationBuilder<AddForeignKeyOperation> AddForeignKey(string name, string table, string[] columns, string principalTable, string schema = null, string principalSchema = null, string[] principalColumns = null, ReferentialAction onUpdate = ReferentialAction.NoAction, ReferentialAction onDelete = ReferentialAction.NoAction);

-        public virtual OperationBuilder<AddPrimaryKeyOperation> AddPrimaryKey(string name, string table, string column, string schema = null);

-        public virtual OperationBuilder<AddPrimaryKeyOperation> AddPrimaryKey(string name, string table, string[] columns, string schema = null);

-        public virtual OperationBuilder<AddUniqueConstraintOperation> AddUniqueConstraint(string name, string table, string column, string schema = null);

-        public virtual OperationBuilder<AddUniqueConstraintOperation> AddUniqueConstraint(string name, string table, string[] columns, string schema = null);

-        public virtual AlterOperationBuilder<AlterColumnOperation> AlterColumn<T>(string name, string table, string type, Nullable<bool> unicode, Nullable<int> maxLength, bool rowVersion, string schema, bool nullable, object defaultValue, string defaultValueSql, string computedColumnSql, Type oldClrType, string oldType, Nullable<bool> oldUnicode, Nullable<int> oldMaxLength, bool oldRowVersion, bool oldNullable, object oldDefaultValue, string oldDefaultValueSql, string oldComputedColumnSql);

-        public virtual AlterOperationBuilder<AlterColumnOperation> AlterColumn<T>(string name, string table, string type = null, Nullable<bool> unicode = default(Nullable<bool>), Nullable<int> maxLength = default(Nullable<int>), bool rowVersion = false, string schema = null, bool nullable = false, object defaultValue = null, string defaultValueSql = null, string computedColumnSql = null, Type oldClrType = null, string oldType = null, Nullable<bool> oldUnicode = default(Nullable<bool>), Nullable<int> oldMaxLength = default(Nullable<int>), bool oldRowVersion = false, bool oldNullable = false, object oldDefaultValue = null, string oldDefaultValueSql = null, string oldComputedColumnSql = null, Nullable<bool> fixedLength = default(Nullable<bool>), Nullable<bool> oldFixedLength = default(Nullable<bool>));

-        public virtual AlterOperationBuilder<AlterDatabaseOperation> AlterDatabase();

-        public virtual AlterOperationBuilder<AlterSequenceOperation> AlterSequence(string name, string schema = null, int incrementBy = 1, Nullable<long> minValue = default(Nullable<long>), Nullable<long> maxValue = default(Nullable<long>), bool cyclic = false, int oldIncrementBy = 1, Nullable<long> oldMinValue = default(Nullable<long>), Nullable<long> oldMaxValue = default(Nullable<long>), bool oldCyclic = false);

-        public virtual AlterOperationBuilder<AlterTableOperation> AlterTable(string name, string schema = null);

-        public virtual OperationBuilder<CreateIndexOperation> CreateIndex(string name, string table, string column, string schema = null, bool unique = false, string filter = null);

-        public virtual OperationBuilder<CreateIndexOperation> CreateIndex(string name, string table, string[] columns, string schema = null, bool unique = false, string filter = null);

-        public virtual OperationBuilder<CreateSequenceOperation> CreateSequence(string name, string schema = null, long startValue = (long)1, int incrementBy = 1, Nullable<long> minValue = default(Nullable<long>), Nullable<long> maxValue = default(Nullable<long>), bool cyclic = false);

-        public virtual OperationBuilder<CreateSequenceOperation> CreateSequence<T>(string name, string schema = null, long startValue = (long)1, int incrementBy = 1, Nullable<long> minValue = default(Nullable<long>), Nullable<long> maxValue = default(Nullable<long>), bool cyclic = false);

-        public virtual CreateTableBuilder<TColumns> CreateTable<TColumns>(string name, Func<ColumnsBuilder, TColumns> columns, string schema = null, Action<CreateTableBuilder<TColumns>> constraints = null);

-        public virtual OperationBuilder<DeleteDataOperation> DeleteData(string table, string keyColumn, object keyValue, string schema = null);

-        public virtual OperationBuilder<DeleteDataOperation> DeleteData(string table, string keyColumn, object[] keyValues, string schema = null);

-        public virtual OperationBuilder<DeleteDataOperation> DeleteData(string table, string[] keyColumns, object[,] keyValues, string schema = null);

-        public virtual OperationBuilder<DeleteDataOperation> DeleteData(string table, string[] keyColumns, object[] keyValues, string schema = null);

-        public virtual OperationBuilder<DropColumnOperation> DropColumn(string name, string table, string schema = null);

-        public virtual OperationBuilder<DropForeignKeyOperation> DropForeignKey(string name, string table, string schema = null);

-        public virtual OperationBuilder<DropIndexOperation> DropIndex(string name, string table = null, string schema = null);

-        public virtual OperationBuilder<DropPrimaryKeyOperation> DropPrimaryKey(string name, string table, string schema = null);

-        public virtual OperationBuilder<DropSchemaOperation> DropSchema(string name);

-        public virtual OperationBuilder<DropSequenceOperation> DropSequence(string name, string schema = null);

-        public virtual OperationBuilder<DropTableOperation> DropTable(string name, string schema = null);

-        public virtual OperationBuilder<DropUniqueConstraintOperation> DropUniqueConstraint(string name, string table, string schema = null);

-        public virtual OperationBuilder<EnsureSchemaOperation> EnsureSchema(string name);

-        public override bool Equals(object obj);

-        public override int GetHashCode();

-        public virtual OperationBuilder<InsertDataOperation> InsertData(string table, string column, object value, string schema = null);

-        public virtual OperationBuilder<InsertDataOperation> InsertData(string table, string column, object[] values, string schema = null);

-        public virtual OperationBuilder<InsertDataOperation> InsertData(string table, string[] columns, object[,] values, string schema = null);

-        public virtual OperationBuilder<InsertDataOperation> InsertData(string table, string[] columns, object[] values, string schema = null);

-        public virtual OperationBuilder<RenameColumnOperation> RenameColumn(string name, string table, string newName, string schema = null);

-        public virtual OperationBuilder<RenameIndexOperation> RenameIndex(string name, string newName, string table = null, string schema = null);

-        public virtual OperationBuilder<RenameSequenceOperation> RenameSequence(string name, string schema = null, string newName = null, string newSchema = null);

-        public virtual OperationBuilder<RenameTableOperation> RenameTable(string name, string schema = null, string newName = null, string newSchema = null);

-        public virtual OperationBuilder<RestartSequenceOperation> RestartSequence(string name, long startValue = (long)1, string schema = null);

-        public virtual OperationBuilder<SqlOperation> Sql(string sql, bool suppressTransaction = false);

-        public override string ToString();

-        public virtual OperationBuilder<UpdateDataOperation> UpdateData(string table, string keyColumn, object keyValue, string column, object value, string schema = null);

-        public virtual OperationBuilder<UpdateDataOperation> UpdateData(string table, string keyColumn, object keyValue, string[] columns, object[] values, string schema = null);

-        public virtual OperationBuilder<UpdateDataOperation> UpdateData(string table, string keyColumn, object[] keyValues, string column, object[] values, string schema = null);

-        public virtual OperationBuilder<UpdateDataOperation> UpdateData(string table, string keyColumn, object[] keyValues, string[] columns, object[,] values, string schema = null);

-        public virtual OperationBuilder<UpdateDataOperation> UpdateData(string table, string[] keyColumns, object[,] keyValues, string column, object[] values, string schema = null);

-        public virtual OperationBuilder<UpdateDataOperation> UpdateData(string table, string[] keyColumns, object[,] keyValues, string[] columns, object[,] values, string schema = null);

-        public virtual OperationBuilder<UpdateDataOperation> UpdateData(string table, string[] keyColumns, object[] keyValues, string column, object value, string schema = null);

-        public virtual OperationBuilder<UpdateDataOperation> UpdateData(string table, string[] keyColumns, object[] keyValues, string[] columns, object[] values, string schema = null);

-    }
-    public class MigrationCommand {
 {
-        public MigrationCommand(IRelationalCommand relationalCommand, bool transactionSuppressed = false);

-        public virtual string CommandText { get; }

-        public virtual bool TransactionSuppressed { get; }

-        public virtual int ExecuteNonQuery(IRelationalConnection connection, IReadOnlyDictionary<string, object> parameterValues = null);

-        public virtual Task<int> ExecuteNonQueryAsync(IRelationalConnection connection, IReadOnlyDictionary<string, object> parameterValues = null, CancellationToken cancellationToken = default(CancellationToken));

-    }
-    public class MigrationCommandListBuilder {
 {
-        public MigrationCommandListBuilder(IRelationalCommandBuilderFactory commandBuilderFactory);

-        public virtual MigrationCommandListBuilder Append(object o);

-        public virtual MigrationCommandListBuilder AppendLine();

-        public virtual MigrationCommandListBuilder AppendLine(object o);

-        public virtual MigrationCommandListBuilder AppendLines(object o);

-        public virtual MigrationCommandListBuilder DecrementIndent();

-        public virtual MigrationCommandListBuilder EndCommand(bool suppressTransaction = false);

-        public virtual IReadOnlyList<MigrationCommand> GetCommandList();

-        public virtual MigrationCommandListBuilder IncrementIndent();

-        public virtual IDisposable Indent();

-    }
-    public class MigrationsAnnotationProvider : IMigrationsAnnotationProvider {
 {
-        public MigrationsAnnotationProvider(MigrationsAnnotationProviderDependencies dependencies);

-        public virtual IEnumerable<IAnnotation> For(IEntityType entityType);

-        public virtual IEnumerable<IAnnotation> For(IForeignKey foreignKey);

-        public virtual IEnumerable<IAnnotation> For(IIndex index);

-        public virtual IEnumerable<IAnnotation> For(IKey key);

-        public virtual IEnumerable<IAnnotation> For(IModel model);

-        public virtual IEnumerable<IAnnotation> For(IProperty property);

-        public virtual IEnumerable<IAnnotation> For(ISequence sequence);

-        public virtual IEnumerable<IAnnotation> ForRemove(IEntityType entityType);

-        public virtual IEnumerable<IAnnotation> ForRemove(IForeignKey foreignKey);

-        public virtual IEnumerable<IAnnotation> ForRemove(IIndex index);

-        public virtual IEnumerable<IAnnotation> ForRemove(IKey key);

-        public virtual IEnumerable<IAnnotation> ForRemove(IModel model);

-        public virtual IEnumerable<IAnnotation> ForRemove(IProperty property);

-        public virtual IEnumerable<IAnnotation> ForRemove(ISequence sequence);

-    }
-    public sealed class MigrationsAnnotationProviderDependencies {
 {
-        public MigrationsAnnotationProviderDependencies();

-    }
-    public static class MigrationsAssemblyExtensions {
 {
-        public static string GetMigrationId(this IMigrationsAssembly assembly, string nameOrId);

-    }
-    public class MigrationsSqlGenerator : IMigrationsSqlGenerator {
 {
-        public MigrationsSqlGenerator(MigrationsSqlGeneratorDependencies dependencies);

-        protected virtual MigrationsSqlGeneratorDependencies Dependencies { get; }

-        protected virtual IUpdateSqlGenerator SqlGenerator { get; }

-        protected virtual IComparer<string> VersionComparer { get; }

-        protected virtual void ColumnDefinition(AddColumnOperation operation, IModel model, MigrationCommandListBuilder builder);

-        protected virtual void ColumnDefinition(string schema, string table, string name, Type clrType, string type, Nullable<bool> unicode, Nullable<int> maxLength, bool rowVersion, bool nullable, object defaultValue, string defaultValueSql, string computedColumnSql, IAnnotatable annotatable, IModel model, MigrationCommandListBuilder builder);

-        protected virtual void ColumnDefinition(string schema, string table, string name, Type clrType, string type, Nullable<bool> unicode, Nullable<int> maxLength, Nullable<bool> fixedLength, bool rowVersion, bool nullable, object defaultValue, string defaultValueSql, string computedColumnSql, IAnnotatable annotatable, IModel model, MigrationCommandListBuilder builder);

-        protected virtual string ColumnList(string[] columns);

-        protected virtual void DefaultValue(object defaultValue, string defaultValueSql, MigrationCommandListBuilder builder);

-        protected virtual void EndStatement(MigrationCommandListBuilder builder, bool suppressTransaction = false);

-        protected virtual IEnumerable<IEntityType> FindEntityTypes(IModel model, string schema, string tableName);

-        protected virtual IProperty FindProperty(IModel model, string schema, string tableName, string columnName);

-        protected virtual void ForeignKeyAction(ReferentialAction referentialAction, MigrationCommandListBuilder builder);

-        protected virtual void ForeignKeyConstraint(AddForeignKeyOperation operation, IModel model, MigrationCommandListBuilder builder);

-        protected virtual void Generate(AddColumnOperation operation, IModel model, MigrationCommandListBuilder builder);

-        protected virtual void Generate(AddColumnOperation operation, IModel model, MigrationCommandListBuilder builder, bool terminate);

-        protected virtual void Generate(AddForeignKeyOperation operation, IModel model, MigrationCommandListBuilder builder);

-        protected virtual void Generate(AddForeignKeyOperation operation, IModel model, MigrationCommandListBuilder builder, bool terminate);

-        protected virtual void Generate(AddPrimaryKeyOperation operation, IModel model, MigrationCommandListBuilder builder);

-        protected virtual void Generate(AddPrimaryKeyOperation operation, IModel model, MigrationCommandListBuilder builder, bool terminate);

-        protected virtual void Generate(AddUniqueConstraintOperation operation, IModel model, MigrationCommandListBuilder builder);

-        protected virtual void Generate(AlterColumnOperation operation, IModel model, MigrationCommandListBuilder builder);

-        protected virtual void Generate(AlterDatabaseOperation operation, IModel model, MigrationCommandListBuilder builder);

-        protected virtual void Generate(AlterSequenceOperation operation, IModel model, MigrationCommandListBuilder builder);

-        protected virtual void Generate(AlterTableOperation operation, IModel model, MigrationCommandListBuilder builder);

-        protected virtual void Generate(CreateIndexOperation operation, IModel model, MigrationCommandListBuilder builder);

-        protected virtual void Generate(CreateIndexOperation operation, IModel model, MigrationCommandListBuilder builder, bool terminate);

-        protected virtual void Generate(CreateSequenceOperation operation, IModel model, MigrationCommandListBuilder builder);

-        protected virtual void Generate(CreateTableOperation operation, IModel model, MigrationCommandListBuilder builder);

-        protected virtual void Generate(CreateTableOperation operation, IModel model, MigrationCommandListBuilder builder, bool terminate);

-        protected virtual void Generate(DeleteDataOperation operation, IModel model, MigrationCommandListBuilder builder);

-        protected virtual void Generate(DropColumnOperation operation, IModel model, MigrationCommandListBuilder builder);

-        protected virtual void Generate(DropColumnOperation operation, IModel model, MigrationCommandListBuilder builder, bool terminate);

-        protected virtual void Generate(DropForeignKeyOperation operation, IModel model, MigrationCommandListBuilder builder);

-        protected virtual void Generate(DropForeignKeyOperation operation, IModel model, MigrationCommandListBuilder builder, bool terminate);

-        protected virtual void Generate(DropIndexOperation operation, IModel model, MigrationCommandListBuilder builder);

-        protected virtual void Generate(DropPrimaryKeyOperation operation, IModel model, MigrationCommandListBuilder builder);

-        protected virtual void Generate(DropPrimaryKeyOperation operation, IModel model, MigrationCommandListBuilder builder, bool terminate);

-        protected virtual void Generate(DropSchemaOperation operation, IModel model, MigrationCommandListBuilder builder);

-        protected virtual void Generate(DropSequenceOperation operation, IModel model, MigrationCommandListBuilder builder);

-        protected virtual void Generate(DropTableOperation operation, IModel model, MigrationCommandListBuilder builder);

-        protected virtual void Generate(DropTableOperation operation, IModel model, MigrationCommandListBuilder builder, bool terminate);

-        protected virtual void Generate(DropUniqueConstraintOperation operation, IModel model, MigrationCommandListBuilder builder);

-        protected virtual void Generate(EnsureSchemaOperation operation, IModel model, MigrationCommandListBuilder builder);

-        protected virtual void Generate(InsertDataOperation operation, IModel model, MigrationCommandListBuilder builder);

-        protected virtual void Generate(InsertDataOperation operation, IModel model, MigrationCommandListBuilder builder, bool terminate);

-        protected virtual void Generate(MigrationOperation operation, IModel model, MigrationCommandListBuilder builder);

-        protected virtual void Generate(RenameColumnOperation operation, IModel model, MigrationCommandListBuilder builder);

-        protected virtual void Generate(RenameIndexOperation operation, IModel model, MigrationCommandListBuilder builder);

-        protected virtual void Generate(RenameSequenceOperation operation, IModel model, MigrationCommandListBuilder builder);

-        protected virtual void Generate(RenameTableOperation operation, IModel model, MigrationCommandListBuilder builder);

-        protected virtual void Generate(RestartSequenceOperation operation, IModel model, MigrationCommandListBuilder builder);

-        protected virtual void Generate(SqlOperation operation, IModel model, MigrationCommandListBuilder builder);

-        protected virtual void Generate(UpdateDataOperation operation, IModel model, MigrationCommandListBuilder builder);

-        public virtual IReadOnlyList<MigrationCommand> Generate(IReadOnlyList<MigrationOperation> operations, IModel model = null);

-        protected virtual string GetColumnType(string schema, string table, string name, Type clrType, Nullable<bool> unicode, Nullable<int> maxLength, bool rowVersion, IModel model);

-        protected virtual string GetColumnType(string schema, string table, string name, Type clrType, Nullable<bool> unicode, Nullable<int> maxLength, Nullable<bool> fixedLength, bool rowVersion, IModel model);

-        protected virtual bool HasLegacyRenameOperations(IModel model);

-        protected virtual void IndexOptions(CreateIndexOperation operation, IModel model, MigrationCommandListBuilder builder);

-        protected virtual void IndexTraits(MigrationOperation operation, IModel model, MigrationCommandListBuilder builder);

-        protected virtual bool IsOldColumnSupported(IModel model);

-        protected virtual void PrimaryKeyConstraint(AddPrimaryKeyOperation operation, IModel model, MigrationCommandListBuilder builder);

-        protected virtual void SequenceOptions(AlterSequenceOperation operation, IModel model, MigrationCommandListBuilder builder);

-        protected virtual void SequenceOptions(CreateSequenceOperation operation, IModel model, MigrationCommandListBuilder builder);

-        protected virtual void SequenceOptions(string schema, string name, int increment, Nullable<long> minimumValue, Nullable<long> maximumValue, bool cycle, IModel model, MigrationCommandListBuilder builder);

-        protected virtual bool TryGetVersion(IModel model, out string version);

-        protected virtual void UniqueConstraint(AddUniqueConstraintOperation operation, IModel model, MigrationCommandListBuilder builder);

-    }
-    public sealed class MigrationsSqlGeneratorDependencies {
 {
-        public MigrationsSqlGeneratorDependencies(IRelationalCommandBuilderFactory commandBuilderFactory, ISingletonUpdateSqlGenerator updateSqlGenerator, ISqlGenerationHelper sqlGenerationHelper, IRelationalTypeMapper typeMapper, IRelationalTypeMappingSource typeMappingSource);

-        public IRelationalCommandBuilderFactory CommandBuilderFactory { get; }

-        public ISqlGenerationHelper SqlGenerationHelper { get; }

-        public IRelationalTypeMapper TypeMapper { get; }

-        public IRelationalTypeMappingSource TypeMappingSource { get; }

-        public ISingletonUpdateSqlGenerator UpdateSqlGenerator { get; }

-        public MigrationsSqlGeneratorDependencies With(IRelationalCommandBuilderFactory commandBuilderFactory);

-        public MigrationsSqlGeneratorDependencies With(IRelationalTypeMapper typeMapper);

-        public MigrationsSqlGeneratorDependencies With(IRelationalTypeMappingSource typeMappingSource);

-        public MigrationsSqlGeneratorDependencies With(ISqlGenerationHelper sqlGenerationHelper);

-        public MigrationsSqlGeneratorDependencies With(ISingletonUpdateSqlGenerator updateSqlGenerator);

-    }
-    public enum ReferentialAction {
 {
-        Cascade = 2,

-        NoAction = 0,

-        Restrict = 1,

-        SetDefault = 4,

-        SetNull = 3,

-    }
-    public class SqlServerMigrationsSqlGenerator : MigrationsSqlGenerator {
 {
-        public SqlServerMigrationsSqlGenerator(MigrationsSqlGeneratorDependencies dependencies, IMigrationsAnnotationProvider migrationsAnnotations);

-        protected override void ColumnDefinition(AddColumnOperation operation, IModel model, MigrationCommandListBuilder builder);

-        protected virtual void ColumnDefinition(string schema, string table, string name, Type clrType, string type, Nullable<bool> unicode, Nullable<int> maxLength, bool rowVersion, bool nullable, object defaultValue, string defaultValueSql, string computedColumnSql, bool identity, IAnnotatable annotatable, IModel model, MigrationCommandListBuilder builder);

-        protected override void ColumnDefinition(string schema, string table, string name, Type clrType, string type, Nullable<bool> unicode, Nullable<int> maxLength, Nullable<bool> fixedLength, bool rowVersion, bool nullable, object defaultValue, string defaultValueSql, string computedColumnSql, IAnnotatable annotatable, IModel model, MigrationCommandListBuilder builder);

-        protected virtual void ColumnDefinition(string schema, string table, string name, Type clrType, string type, Nullable<bool> unicode, Nullable<int> maxLength, Nullable<bool> fixedLength, bool rowVersion, bool nullable, object defaultValue, string defaultValueSql, string computedColumnSql, bool identity, IAnnotatable annotatable, IModel model, MigrationCommandListBuilder builder);

-        protected virtual void CreateIndexes(IEnumerable<IIndex> indexes, MigrationCommandListBuilder builder);

-        protected virtual void DropDefaultConstraint(string schema, string tableName, string columnName, MigrationCommandListBuilder builder);

-        protected virtual void DropIndexes(IEnumerable<IIndex> indexes, MigrationCommandListBuilder builder);

-        protected override void ForeignKeyAction(ReferentialAction referentialAction, MigrationCommandListBuilder builder);

-        protected override void Generate(AddColumnOperation operation, IModel model, MigrationCommandListBuilder builder);

-        protected override void Generate(AddColumnOperation operation, IModel model, MigrationCommandListBuilder builder, bool terminate);

-        protected override void Generate(AddForeignKeyOperation operation, IModel model, MigrationCommandListBuilder builder);

-        protected override void Generate(AddPrimaryKeyOperation operation, IModel model, MigrationCommandListBuilder builder);

-        protected override void Generate(AlterColumnOperation operation, IModel model, MigrationCommandListBuilder builder);

-        protected override void Generate(AlterDatabaseOperation operation, IModel model, MigrationCommandListBuilder builder);

-        protected override void Generate(AlterTableOperation operation, IModel model, MigrationCommandListBuilder builder);

-        protected override void Generate(CreateIndexOperation operation, IModel model, MigrationCommandListBuilder builder);

-        protected override void Generate(CreateIndexOperation operation, IModel model, MigrationCommandListBuilder builder, bool terminate);

-        protected override void Generate(CreateSequenceOperation operation, IModel model, MigrationCommandListBuilder builder);

-        protected override void Generate(CreateTableOperation operation, IModel model, MigrationCommandListBuilder builder);

-        protected override void Generate(DropColumnOperation operation, IModel model, MigrationCommandListBuilder builder);

-        protected override void Generate(DropColumnOperation operation, IModel model, MigrationCommandListBuilder builder, bool terminate);

-        protected override void Generate(DropForeignKeyOperation operation, IModel model, MigrationCommandListBuilder builder);

-        protected override void Generate(DropIndexOperation operation, IModel model, MigrationCommandListBuilder builder);

-        protected virtual void Generate(DropIndexOperation operation, IModel model, MigrationCommandListBuilder builder, bool terminate);

-        protected override void Generate(DropPrimaryKeyOperation operation, IModel model, MigrationCommandListBuilder builder);

-        protected override void Generate(DropTableOperation operation, IModel model, MigrationCommandListBuilder builder);

-        protected override void Generate(EnsureSchemaOperation operation, IModel model, MigrationCommandListBuilder builder);

-        protected override void Generate(InsertDataOperation operation, IModel model, MigrationCommandListBuilder builder);

-        protected override void Generate(MigrationOperation operation, IModel model, MigrationCommandListBuilder builder);

-        protected override void Generate(RenameColumnOperation operation, IModel model, MigrationCommandListBuilder builder);

-        protected override void Generate(RenameIndexOperation operation, IModel model, MigrationCommandListBuilder builder);

-        protected override void Generate(RenameSequenceOperation operation, IModel model, MigrationCommandListBuilder builder);

-        protected override void Generate(RenameTableOperation operation, IModel model, MigrationCommandListBuilder builder);

-        protected override void Generate(RestartSequenceOperation operation, IModel model, MigrationCommandListBuilder builder);

-        protected override void Generate(SqlOperation operation, IModel model, MigrationCommandListBuilder builder);

-        protected virtual void Generate(SqlServerCreateDatabaseOperation operation, IModel model, MigrationCommandListBuilder builder);

-        protected virtual void Generate(SqlServerDropDatabaseOperation operation, IModel model, MigrationCommandListBuilder builder);

-        public override IReadOnlyList<MigrationCommand> Generate(IReadOnlyList<MigrationOperation> operations, IModel model);

-        protected virtual IEnumerable<IIndex> GetIndexesToRebuild(IProperty property, MigrationOperation currentOperation);

-        protected override void IndexOptions(CreateIndexOperation operation, IModel model, MigrationCommandListBuilder builder);

-        protected override void IndexTraits(MigrationOperation operation, IModel model, MigrationCommandListBuilder builder);

-        protected virtual void Rename(string name, string newName, MigrationCommandListBuilder builder);

-        protected virtual void Rename(string name, string newName, string type, MigrationCommandListBuilder builder);

-        protected override void SequenceOptions(string schema, string name, int increment, Nullable<long> minimumValue, Nullable<long> maximumValue, bool cycle, IModel model, MigrationCommandListBuilder builder);

-        protected virtual void Transfer(string newSchema, string schema, string name, MigrationCommandListBuilder builder);

-        protected virtual bool UseLegacyIndexFilters(IModel model);

-    }
-}
```

