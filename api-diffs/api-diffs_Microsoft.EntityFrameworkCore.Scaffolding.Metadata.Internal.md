# Microsoft.EntityFrameworkCore.Scaffolding.Metadata.Internal

``` diff
-namespace Microsoft.EntityFrameworkCore.Scaffolding.Metadata.Internal {
 {
-    public static class DatabaseColumnExtensions {
 {
-        public static string DisplayName(this DatabaseColumn column);

-        public static bool IsKeyOrIndex(this DatabaseColumn column);

-        public static bool IsRowVersion(this DatabaseColumn column);

-    }
-    public static class DatabaseForeignKeyExtensions {
 {
-        public static string DisplayName(this DatabaseForeignKey foreignKey);

-    }
-    public static class DatabaseTableExtensions {
 {
-        public static string DisplayName(this DatabaseTable table);

-    }
-    public static class ScaffoldingAnnotationNames {
 {
-        public const string ConcurrencyToken = "ConcurrencyToken";

-        public const string UnderlyingStoreType = "UnderlyingStoreType";

-    }
-}
```

