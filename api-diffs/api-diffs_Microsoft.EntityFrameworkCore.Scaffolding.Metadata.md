# Microsoft.EntityFrameworkCore.Scaffolding.Metadata

``` diff
-namespace Microsoft.EntityFrameworkCore.Scaffolding.Metadata {
 {
-    public class DatabaseColumn : Annotatable {
 {
-        public DatabaseColumn();

-        public virtual string ComputedColumnSql { get; set; }

-        public virtual string DefaultValueSql { get; set; }

-        public virtual bool IsNullable { get; set; }

-        public virtual string Name { get; set; }

-        public virtual string StoreType { get; set; }

-        public virtual DatabaseTable Table { get; set; }

-        public virtual Nullable<ValueGenerated> ValueGenerated { get; set; }

-    }
-    public static class DatabaseColumnExtensions {
 {
-        public static string GetUnderlyingStoreType(this DatabaseColumn column);

-        public static void SetUnderlyingStoreType(this DatabaseColumn column, string value);

-    }
-    public class DatabaseForeignKey : Annotatable {
 {
-        public DatabaseForeignKey();

-        public virtual IList<DatabaseColumn> Columns { get; }

-        public virtual string Name { get; set; }

-        public virtual Nullable<ReferentialAction> OnDelete { get; set; }

-        public virtual IList<DatabaseColumn> PrincipalColumns { get; }

-        public virtual DatabaseTable PrincipalTable { get; set; }

-        public virtual DatabaseTable Table { get; set; }

-    }
-    public class DatabaseIndex : Annotatable {
 {
-        public DatabaseIndex();

-        public virtual IList<DatabaseColumn> Columns { get; }

-        public virtual string Filter { get; set; }

-        public virtual bool IsUnique { get; set; }

-        public virtual string Name { get; set; }

-        public virtual DatabaseTable Table { get; set; }

-    }
-    public class DatabaseModel : Annotatable {
 {
-        public DatabaseModel();

-        public virtual string DatabaseName { get; set; }

-        public virtual string DefaultSchema { get; set; }

-        public virtual IList<DatabaseSequence> Sequences { get; }

-        public virtual IList<DatabaseTable> Tables { get; }

-    }
-    public class DatabasePrimaryKey : Annotatable {
 {
-        public DatabasePrimaryKey();

-        public virtual IList<DatabaseColumn> Columns { get; }

-        public virtual string Name { get; set; }

-        public virtual DatabaseTable Table { get; set; }

-    }
-    public class DatabaseSequence : Annotatable {
 {
-        public DatabaseSequence();

-        public virtual DatabaseModel Database { get; set; }

-        public virtual Nullable<int> IncrementBy { get; set; }

-        public virtual Nullable<bool> IsCyclic { get; set; }

-        public virtual Nullable<long> MaxValue { get; set; }

-        public virtual Nullable<long> MinValue { get; set; }

-        public virtual string Name { get; set; }

-        public virtual string Schema { get; set; }

-        public virtual Nullable<long> StartValue { get; set; }

-        public virtual string StoreType { get; set; }

-    }
-    public class DatabaseTable : Annotatable {
 {
-        public DatabaseTable();

-        public virtual IList<DatabaseColumn> Columns { get; }

-        public virtual DatabaseModel Database { get; set; }

-        public virtual IList<DatabaseForeignKey> ForeignKeys { get; }

-        public virtual IList<DatabaseIndex> Indexes { get; }

-        public virtual string Name { get; set; }

-        public virtual DatabasePrimaryKey PrimaryKey { get; set; }

-        public virtual string Schema { get; set; }

-        public virtual IList<DatabaseUniqueConstraint> UniqueConstraints { get; }

-    }
-    public class DatabaseUniqueConstraint : Annotatable {
 {
-        public DatabaseUniqueConstraint();

-        public virtual IList<DatabaseColumn> Columns { get; }

-        public virtual string Name { get; set; }

-        public virtual DatabaseTable Table { get; set; }

-    }
-}
```

