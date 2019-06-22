# Microsoft.EntityFrameworkCore.Migrations.Operations

``` diff
-namespace Microsoft.EntityFrameworkCore.Migrations.Operations {
 {
-    public class AddColumnOperation : ColumnOperation {
 {
-        public AddColumnOperation();

-        public virtual string Name { get; set; }

-        public virtual string Schema { get; set; }

-        public virtual string Table { get; set; }

-    }
-    public class AddForeignKeyOperation : MigrationOperation {
 {
-        public AddForeignKeyOperation();

-        public virtual string[] Columns { get; set; }

-        public virtual string Name { get; set; }

-        public virtual ReferentialAction OnDelete { get; set; }

-        public virtual ReferentialAction OnUpdate { get; set; }

-        public virtual string[] PrincipalColumns { get; set; }

-        public virtual string PrincipalSchema { get; set; }

-        public virtual string PrincipalTable { get; set; }

-        public virtual string Schema { get; set; }

-        public virtual string Table { get; set; }

-    }
-    public class AddPrimaryKeyOperation : MigrationOperation {
 {
-        public AddPrimaryKeyOperation();

-        public virtual string[] Columns { get; set; }

-        public virtual string Name { get; set; }

-        public virtual string Schema { get; set; }

-        public virtual string Table { get; set; }

-    }
-    public class AddUniqueConstraintOperation : MigrationOperation {
 {
-        public AddUniqueConstraintOperation();

-        public virtual string[] Columns { get; set; }

-        public virtual string Name { get; set; }

-        public virtual string Schema { get; set; }

-        public virtual string Table { get; set; }

-    }
-    public class AlterColumnOperation : ColumnOperation, IAlterMigrationOperation {
 {
-        public AlterColumnOperation();

-        IMutableAnnotatable Microsoft.EntityFrameworkCore.Migrations.Operations.IAlterMigrationOperation.OldAnnotations { get; }

-        public virtual string Name { get; set; }

-        public virtual ColumnOperation OldColumn { get; set; }

-        public virtual string Schema { get; set; }

-        public virtual string Table { get; set; }

-    }
-    public class AlterDatabaseOperation : MigrationOperation, IAlterMigrationOperation {
 {
-        public AlterDatabaseOperation();

-        IMutableAnnotatable Microsoft.EntityFrameworkCore.Migrations.Operations.IAlterMigrationOperation.OldAnnotations { get; }

-        public virtual Annotatable OldDatabase { get; }

-    }
-    public class AlterSequenceOperation : SequenceOperation, IAlterMigrationOperation {
 {
-        public AlterSequenceOperation();

-        IMutableAnnotatable Microsoft.EntityFrameworkCore.Migrations.Operations.IAlterMigrationOperation.OldAnnotations { get; }

-        public virtual string Name { get; set; }

-        public virtual SequenceOperation OldSequence { get; set; }

-        public virtual string Schema { get; set; }

-    }
-    public class AlterTableOperation : MigrationOperation, IAlterMigrationOperation {
 {
-        public AlterTableOperation();

-        IMutableAnnotatable Microsoft.EntityFrameworkCore.Migrations.Operations.IAlterMigrationOperation.OldAnnotations { get; }

-        public virtual string Name { get; set; }

-        public virtual Annotatable OldTable { get; set; }

-        public virtual string Schema { get; set; }

-    }
-    public class ColumnOperation : MigrationOperation {
 {
-        public ColumnOperation();

-        public virtual Type ClrType { get; set; }

-        public virtual string ColumnType { get; set; }

-        public virtual string ComputedColumnSql { get; set; }

-        public virtual object DefaultValue { get; set; }

-        public virtual string DefaultValueSql { get; set; }

-        public virtual Nullable<bool> IsFixedLength { get; set; }

-        public virtual bool IsNullable { get; set; }

-        public virtual bool IsRowVersion { get; set; }

-        public virtual Nullable<bool> IsUnicode { get; set; }

-        public virtual Nullable<int> MaxLength { get; set; }

-    }
-    public class CreateIndexOperation : MigrationOperation {
 {
-        public CreateIndexOperation();

-        public virtual string[] Columns { get; set; }

-        public virtual string Filter { get; set; }

-        public virtual bool IsUnique { get; set; }

-        public virtual string Name { get; set; }

-        public virtual string Schema { get; set; }

-        public virtual string Table { get; set; }

-    }
-    public class CreateSequenceOperation : SequenceOperation {
 {
-        public CreateSequenceOperation();

-        public virtual Type ClrType { get; set; }

-        public virtual string Name { get; set; }

-        public virtual string Schema { get; set; }

-        public virtual long StartValue { get; set; }

-    }
-    public class CreateTableOperation : MigrationOperation {
 {
-        public CreateTableOperation();

-        public virtual List<AddColumnOperation> Columns { get; }

-        public virtual List<AddForeignKeyOperation> ForeignKeys { get; }

-        public virtual string Name { get; set; }

-        public virtual AddPrimaryKeyOperation PrimaryKey { get; set; }

-        public virtual string Schema { get; set; }

-        public virtual List<AddUniqueConstraintOperation> UniqueConstraints { get; }

-    }
-    public class DeleteDataOperation : MigrationOperation {
 {
-        public DeleteDataOperation();

-        public virtual string[] KeyColumns { get; set; }

-        public virtual object[,] KeyValues { get; set; }

-        public virtual string Schema { get; set; }

-        public virtual string Table { get; set; }

-        public virtual IEnumerable<ModificationCommand> GenerateModificationCommands(IModel model);

-    }
-    public class DropColumnOperation : MigrationOperation {
 {
-        public DropColumnOperation();

-        public virtual string Name { get; set; }

-        public virtual string Schema { get; set; }

-        public virtual string Table { get; set; }

-    }
-    public class DropForeignKeyOperation : MigrationOperation {
 {
-        public DropForeignKeyOperation();

-        public virtual string Name { get; set; }

-        public virtual string Schema { get; set; }

-        public virtual string Table { get; set; }

-    }
-    public class DropIndexOperation : MigrationOperation {
 {
-        public DropIndexOperation();

-        public virtual string Name { get; set; }

-        public virtual string Schema { get; set; }

-        public virtual string Table { get; set; }

-    }
-    public class DropPrimaryKeyOperation : MigrationOperation {
 {
-        public DropPrimaryKeyOperation();

-        public virtual string Name { get; set; }

-        public virtual string Schema { get; set; }

-        public virtual string Table { get; set; }

-    }
-    public class DropSchemaOperation : MigrationOperation {
 {
-        public DropSchemaOperation();

-        public virtual string Name { get; set; }

-    }
-    public class DropSequenceOperation : MigrationOperation {
 {
-        public DropSequenceOperation();

-        public virtual string Name { get; set; }

-        public virtual string Schema { get; set; }

-    }
-    public class DropTableOperation : MigrationOperation {
 {
-        public DropTableOperation();

-        public virtual string Name { get; set; }

-        public virtual string Schema { get; set; }

-    }
-    public class DropUniqueConstraintOperation : MigrationOperation {
 {
-        public DropUniqueConstraintOperation();

-        public virtual string Name { get; set; }

-        public virtual string Schema { get; set; }

-        public virtual string Table { get; set; }

-    }
-    public class EnsureSchemaOperation : MigrationOperation {
 {
-        public EnsureSchemaOperation();

-        public virtual string Name { get; set; }

-    }
-    public interface IAlterMigrationOperation {
 {
-        IMutableAnnotatable OldAnnotations { get; }

-    }
-    public class InsertDataOperation : MigrationOperation {
 {
-        public InsertDataOperation();

-        public virtual string[] Columns { get; set; }

-        public virtual string Schema { get; set; }

-        public virtual string Table { get; set; }

-        public virtual object[,] Values { get; set; }

-        public virtual IEnumerable<ModificationCommand> GenerateModificationCommands(IModel model);

-    }
-    public abstract class MigrationOperation : Annotatable {
 {
-        protected MigrationOperation();

-        public virtual bool IsDestructiveChange { get; set; }

-    }
-    public class RenameColumnOperation : MigrationOperation {
 {
-        public RenameColumnOperation();

-        public virtual string Name { get; set; }

-        public virtual string NewName { get; set; }

-        public virtual string Schema { get; set; }

-        public virtual string Table { get; set; }

-    }
-    public class RenameIndexOperation : MigrationOperation {
 {
-        public RenameIndexOperation();

-        public virtual string Name { get; set; }

-        public virtual string NewName { get; set; }

-        public virtual string Schema { get; set; }

-        public virtual string Table { get; set; }

-    }
-    public class RenameSequenceOperation : MigrationOperation {
 {
-        public RenameSequenceOperation();

-        public virtual string Name { get; set; }

-        public virtual string NewName { get; set; }

-        public virtual string NewSchema { get; set; }

-        public virtual string Schema { get; set; }

-    }
-    public class RenameTableOperation : MigrationOperation {
 {
-        public RenameTableOperation();

-        public virtual string Name { get; set; }

-        public virtual string NewName { get; set; }

-        public virtual string NewSchema { get; set; }

-        public virtual string Schema { get; set; }

-    }
-    public class RestartSequenceOperation : MigrationOperation {
 {
-        public RestartSequenceOperation();

-        public virtual string Name { get; set; }

-        public virtual string Schema { get; set; }

-        public virtual long StartValue { get; set; }

-    }
-    public class SequenceOperation : MigrationOperation {
 {
-        public SequenceOperation();

-        public virtual int IncrementBy { get; set; }

-        public virtual bool IsCyclic { get; set; }

-        public virtual Nullable<long> MaxValue { get; set; }

-        public virtual Nullable<long> MinValue { get; set; }

-    }
-    public class SqlOperation : MigrationOperation {
 {
-        public SqlOperation();

-        public virtual string Sql { get; set; }

-        public virtual bool SuppressTransaction { get; set; }

-    }
-    public class SqlServerCreateDatabaseOperation : MigrationOperation {
 {
-        public SqlServerCreateDatabaseOperation();

-        public virtual string FileName { get; set; }

-        public virtual string Name { get; set; }

-    }
-    public class SqlServerDropDatabaseOperation : MigrationOperation {
 {
-        public SqlServerDropDatabaseOperation();

-        public virtual string Name { get; set; }

-    }
-    public class UpdateDataOperation : MigrationOperation {
 {
-        public UpdateDataOperation();

-        public virtual string[] Columns { get; set; }

-        public virtual string[] KeyColumns { get; set; }

-        public virtual object[,] KeyValues { get; set; }

-        public virtual string Schema { get; set; }

-        public virtual string Table { get; set; }

-        public virtual object[,] Values { get; set; }

-        public virtual IEnumerable<ModificationCommand> GenerateModificationCommands(IModel model);

-    }
-}
```

