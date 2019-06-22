# Microsoft.EntityFrameworkCore.InMemory.Infrastructure.Internal

``` diff
-namespace Microsoft.EntityFrameworkCore.InMemory.Infrastructure.Internal {
 {
-    public interface IInMemorySingletonOptions : ISingletonOptions {
 {
-        InMemoryDatabaseRoot DatabaseRoot { get; }

-    }
-    public class InMemoryOptionsExtension : IDbContextOptionsExtension, IDbContextOptionsExtensionWithDebugInfo {
 {
-        public InMemoryOptionsExtension();

-        protected InMemoryOptionsExtension(InMemoryOptionsExtension copyFrom);

-        public virtual InMemoryDatabaseRoot DatabaseRoot { get; }

-        public virtual string LogFragment { get; }

-        public virtual string StoreName { get; }

-        public virtual bool ApplyServices(IServiceCollection services);

-        protected virtual InMemoryOptionsExtension Clone();

-        public virtual long GetServiceProviderHashCode();

-        public virtual void PopulateDebugInfo(IDictionary<string, string> debugInfo);

-        public virtual void Validate(IDbContextOptions options);

-        public virtual InMemoryOptionsExtension WithDatabaseRoot(InMemoryDatabaseRoot databaseRoot);

-        public virtual InMemoryOptionsExtension WithStoreName(string storeName);

-    }
-    public class InMemorySingletonOptions : IInMemorySingletonOptions, ISingletonOptions {
 {
-        public InMemorySingletonOptions();

-        public virtual InMemoryDatabaseRoot DatabaseRoot { get; private set; }

-        public virtual void Initialize(IDbContextOptions options);

-        public virtual void Validate(IDbContextOptions options);

-    }
-}
```

