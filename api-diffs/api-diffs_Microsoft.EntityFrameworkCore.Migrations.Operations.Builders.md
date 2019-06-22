# Microsoft.EntityFrameworkCore.Migrations.Operations.Builders

``` diff
-namespace Microsoft.EntityFrameworkCore.Migrations.Operations.Builders {
 {
-    public class AlterOperationBuilder<TOperation> : OperationBuilder<TOperation> where TOperation : MigrationOperation, IAlterMigrationOperation {
 {
-        public AlterOperationBuilder(TOperation operation);

-        public virtual new AlterOperationBuilder<TOperation> Annotation(string name, object value);

-        public virtual AlterOperationBuilder<TOperation> OldAnnotation(string name, object value);

-    }
-    public class ColumnsBuilder {
 {
-        public ColumnsBuilder(CreateTableOperation createTableOperation);

-        public virtual OperationBuilder<AddColumnOperation> Column<T>(string type, Nullable<bool> unicode, Nullable<int> maxLength, bool rowVersion, string name, bool nullable, object defaultValue, string defaultValueSql, string computedColumnSql);

-        public virtual OperationBuilder<AddColumnOperation> Column<T>(string type = null, Nullable<bool> unicode = default(Nullable<bool>), Nullable<int> maxLength = default(Nullable<int>), bool rowVersion = false, string name = null, bool nullable = false, object defaultValue = null, string defaultValueSql = null, string computedColumnSql = null, Nullable<bool> fixedLength = default(Nullable<bool>));

-        public override bool Equals(object obj);

-        public override int GetHashCode();

-        public override string ToString();

-    }
-    public class CreateTableBuilder<TColumns> : OperationBuilder<CreateTableOperation> {
 {
-        public CreateTableBuilder(CreateTableOperation operation, IReadOnlyDictionary<PropertyInfo, AddColumnOperation> columnMap);

-        public virtual new CreateTableBuilder<TColumns> Annotation(string name, object value);

-        public virtual OperationBuilder<AddForeignKeyOperation> ForeignKey(string name, Expression<Func<TColumns, object>> column, string principalTable, string principalColumn, string principalSchema = null, ReferentialAction onUpdate = ReferentialAction.NoAction, ReferentialAction onDelete = ReferentialAction.NoAction);

-        public virtual OperationBuilder<AddForeignKeyOperation> ForeignKey(string name, Expression<Func<TColumns, object>> columns, string principalTable, string[] principalColumns, string principalSchema = null, ReferentialAction onUpdate = ReferentialAction.NoAction, ReferentialAction onDelete = ReferentialAction.NoAction);

-        public virtual OperationBuilder<AddPrimaryKeyOperation> PrimaryKey(string name, Expression<Func<TColumns, object>> columns);

-        public virtual OperationBuilder<AddUniqueConstraintOperation> UniqueConstraint(string name, Expression<Func<TColumns, object>> columns);

-    }
-    public class OperationBuilder<TOperation> : IInfrastructure<TOperation> where TOperation : MigrationOperation {
 {
-        public OperationBuilder(TOperation operation);

-        TOperation Microsoft.EntityFrameworkCore.Infrastructure.IInfrastructure<TOperation>.Instance { get; }

-        protected virtual TOperation Operation { get; }

-        public virtual OperationBuilder<TOperation> Annotation(string name, object value);

-        public override bool Equals(object obj);

-        public override int GetHashCode();

-        public override string ToString();

-    }
-}
```

