# Microsoft.EntityFrameworkCore.SqlServer.Metadata.Internal

``` diff
-namespace Microsoft.EntityFrameworkCore.SqlServer.Metadata.Internal {
 {
-    public static class SqlServerAnnotationNames {
 {
-        public const string Clustered = "SqlServer:Clustered";

-        public const string HiLoSequenceName = "SqlServer:HiLoSequenceName";

-        public const string HiLoSequenceSchema = "SqlServer:HiLoSequenceSchema";

-        public const string Include = "SqlServer:Include";

-        public const string MemoryOptimized = "SqlServer:MemoryOptimized";

-        public const string Prefix = "SqlServer:";

-        public const string ValueGenerationStrategy = "SqlServer:ValueGenerationStrategy";

-    }
-    public class SqlServerEntityTypeBuilderAnnotations : SqlServerEntityTypeAnnotations {
 {
-        public SqlServerEntityTypeBuilderAnnotations(InternalEntityTypeBuilder internalBuilder, ConfigurationSource configurationSource);

-        public virtual bool IsMemoryOptimized(bool value);

-        public virtual bool ToSchema(string name);

-        public virtual bool ToTable(string name);

-        public virtual bool ToTable(string name, string schema);

-    }
-    public class SqlServerIndexBuilderAnnotations : SqlServerIndexAnnotations {
 {
-        public SqlServerIndexBuilderAnnotations(InternalIndexBuilder internalBuilder, ConfigurationSource configurationSource);

-        public virtual bool HasFilter(string value);

-        public virtual bool Include(string[] value);

-        public virtual bool IsClustered(Nullable<bool> value);

-        public virtual bool Name(string value);

-    }
-    public static class SqlServerInternalMetadataBuilderExtensions {
 {
-        public static SqlServerEntityTypeBuilderAnnotations SqlServer(this InternalEntityTypeBuilder builder, ConfigurationSource configurationSource);

-        public static SqlServerIndexBuilderAnnotations SqlServer(this InternalIndexBuilder builder, ConfigurationSource configurationSource);

-        public static SqlServerKeyBuilderAnnotations SqlServer(this InternalKeyBuilder builder, ConfigurationSource configurationSource);

-        public static SqlServerModelBuilderAnnotations SqlServer(this InternalModelBuilder builder, ConfigurationSource configurationSource);

-        public static SqlServerPropertyBuilderAnnotations SqlServer(this InternalPropertyBuilder builder, ConfigurationSource configurationSource);

-        public static RelationalForeignKeyBuilderAnnotations SqlServer(this InternalRelationshipBuilder builder, ConfigurationSource configurationSource);

-    }
-    public class SqlServerKeyBuilderAnnotations : SqlServerKeyAnnotations {
 {
-        public SqlServerKeyBuilderAnnotations(InternalKeyBuilder internalBuilder, ConfigurationSource configurationSource);

-        public virtual bool IsClustered(Nullable<bool> value);

-        public virtual bool Name(string value);

-    }
-    public class SqlServerModelBuilderAnnotations : SqlServerModelAnnotations {
 {
-        public SqlServerModelBuilderAnnotations(InternalModelBuilder internalBuilder, ConfigurationSource configurationSource);

-        public virtual bool HiLoSequenceName(string value);

-        public virtual bool HiLoSequenceSchema(string value);

-        public virtual bool ValueGenerationStrategy(Nullable<SqlServerValueGenerationStrategy> value);

-    }
-    public class SqlServerPropertyBuilderAnnotations : SqlServerPropertyAnnotations {
 {
-        public SqlServerPropertyBuilderAnnotations(InternalPropertyBuilder internalBuilder, ConfigurationSource configurationSource);

-        protected virtual new RelationalAnnotationsBuilder Annotations { get; }

-        protected override bool ShouldThrowOnConflict { get; }

-        protected override bool ShouldThrowOnInvalidConfiguration { get; }

-        public virtual bool ColumnName(string value);

-        public virtual bool ColumnType(string value);

-        public virtual bool ComputedColumnSql(string value);

-        public virtual bool DefaultValue(object value);

-        public virtual bool DefaultValueSql(string value);

-        public virtual bool HiLoSequenceName(string value);

-        public virtual bool HiLoSequenceSchema(string value);

-        public virtual bool ValueGenerationStrategy(Nullable<SqlServerValueGenerationStrategy> value);

-    }
-}
```

