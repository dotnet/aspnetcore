# Microsoft.EntityFrameworkCore.SqlServer.Infrastructure.Internal

``` diff
-namespace Microsoft.EntityFrameworkCore.SqlServer.Infrastructure.Internal {
 {
-    public interface ISqlServerOptions : ISingletonOptions {
 {
-        bool RowNumberPagingEnabled { get; }

-    }
-    public class SqlServerOptionsExtension : RelationalOptionsExtension, IDbContextOptionsExtension, IDbContextOptionsExtensionWithDebugInfo {
 {
-        public SqlServerOptionsExtension();

-        protected SqlServerOptionsExtension(SqlServerOptionsExtension copyFrom);

-        public override string LogFragment { get; }

-        public virtual Nullable<bool> RowNumberPaging { get; }

-        public override bool ApplyServices(IServiceCollection services);

-        protected override RelationalOptionsExtension Clone();

-        public override long GetServiceProviderHashCode();

-        public virtual void PopulateDebugInfo(IDictionary<string, string> debugInfo);

-        public virtual SqlServerOptionsExtension WithRowNumberPaging(bool rowNumberPaging);

-    }
-}
```

