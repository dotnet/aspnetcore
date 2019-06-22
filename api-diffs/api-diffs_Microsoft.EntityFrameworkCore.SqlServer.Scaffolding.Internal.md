# Microsoft.EntityFrameworkCore.SqlServer.Scaffolding.Internal

``` diff
-namespace Microsoft.EntityFrameworkCore.SqlServer.Scaffolding.Internal {
 {
-    public static class SqlDataReaderExtension {
 {
-        public static T GetValueOrDefault<T>(this DbDataReader reader, string name);

-        public static T GetValueOrDefault<T>(this DbDataRecord record, string name);

-    }
-    public class SqlServerCodeGenerator : ProviderCodeGenerator {
 {
-        public SqlServerCodeGenerator(ProviderCodeGeneratorDependencies dependencies);

-        public override MethodCallCodeFragment GenerateUseProvider(string connectionString, MethodCallCodeFragment providerOptions);

-    }
-    public class SqlServerDatabaseModelFactory : IDatabaseModelFactory {
 {
-        public SqlServerDatabaseModelFactory(IDiagnosticsLogger<DbLoggerCategory.Scaffolding> logger);

-        public virtual DatabaseModel Create(DbConnection connection, IEnumerable<string> tables, IEnumerable<string> schemas);

-        public virtual DatabaseModel Create(string connectionString, IEnumerable<string> tables, IEnumerable<string> schemas);

-    }
-}
```

