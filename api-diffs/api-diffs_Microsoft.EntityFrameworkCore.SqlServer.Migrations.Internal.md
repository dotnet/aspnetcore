# Microsoft.EntityFrameworkCore.SqlServer.Migrations.Internal

``` diff
-namespace Microsoft.EntityFrameworkCore.SqlServer.Migrations.Internal {
 {
-    public class SqlServerHistoryRepository : HistoryRepository {
 {
-        public SqlServerHistoryRepository(HistoryRepositoryDependencies dependencies);

-        protected override string ExistsSql { get; }

-        public override string GetBeginIfExistsScript(string migrationId);

-        public override string GetBeginIfNotExistsScript(string migrationId);

-        public override string GetCreateIfNotExistsScript();

-        public override string GetEndIfScript();

-        protected override bool InterpretExistsResult(object value);

-    }
-    public class SqlServerMigrationsAnnotationProvider : MigrationsAnnotationProvider {
 {
-        public SqlServerMigrationsAnnotationProvider(MigrationsAnnotationProviderDependencies dependencies);

-        public override IEnumerable<IAnnotation> For(IEntityType entityType);

-        public override IEnumerable<IAnnotation> For(IIndex index);

-        public override IEnumerable<IAnnotation> For(IKey key);

-        public override IEnumerable<IAnnotation> For(IModel model);

-        public override IEnumerable<IAnnotation> For(IProperty property);

-        public override IEnumerable<IAnnotation> ForRemove(IEntityType entityType);

-        public override IEnumerable<IAnnotation> ForRemove(IModel model);

-    }
-}
```

