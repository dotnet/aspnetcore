# Microsoft.EntityFrameworkCore.InMemory.Storage.Internal

``` diff
-namespace Microsoft.EntityFrameworkCore.InMemory.Storage.Internal {
 {
-    public interface IInMemoryDatabase : IDatabase {
 {
-        IInMemoryStore Store { get; }

-        bool EnsureDatabaseCreated(StateManagerDependencies stateManagerDependencies);

-    }
-    public interface IInMemoryStore {
 {
-        bool Clear();

-        bool EnsureCreated(StateManagerDependencies stateManagerDependencies, IDiagnosticsLogger<DbLoggerCategory.Update> updateLogger);

-        int ExecuteTransaction(IReadOnlyList<IUpdateEntry> entries, IDiagnosticsLogger<DbLoggerCategory.Update> updateLogger);

-        IReadOnlyList<InMemoryTableSnapshot> GetTables(IEntityType entityType);

-    }
-    public interface IInMemoryStoreCache {
 {
-        IInMemoryStore GetStore(string name);

-    }
-    public interface IInMemoryTable {
 {
-        void Create(IUpdateEntry entry);

-        void Delete(IUpdateEntry entry);

-        IReadOnlyList<object[]> SnapshotRows();

-        void Update(IUpdateEntry entry);

-    }
-    public interface IInMemoryTableFactory {
 {
-        IInMemoryTable Create(IEntityType entityType);

-    }
-    public class InMemoryDatabase : Database, IDatabase, IInMemoryDatabase {
 {
-        public InMemoryDatabase(DatabaseDependencies dependencies, IInMemoryStoreCache storeCache, IDbContextOptions options, IDiagnosticsLogger<DbLoggerCategory.Update> updateLogger);

-        public virtual IInMemoryStore Store { get; }

-        public override Func<QueryContext, IAsyncEnumerable<TResult>> CompileAsyncQuery<TResult>(QueryModel queryModel);

-        public virtual bool EnsureDatabaseCreated(StateManagerDependencies stateManagerDependencies);

-        public override int SaveChanges(IReadOnlyList<IUpdateEntry> entries);

-        public override Task<int> SaveChangesAsync(IReadOnlyList<IUpdateEntry> entries, CancellationToken cancellationToken = default(CancellationToken));

-    }
-    public class InMemoryDatabaseCreator : IDatabaseCreator, IDatabaseCreatorWithCanConnect {
 {
-        public InMemoryDatabaseCreator(StateManagerDependencies stateManagerDependencies);

-        protected virtual IInMemoryDatabase Database { get; }

-        public virtual bool CanConnect();

-        public virtual Task<bool> CanConnectAsync(CancellationToken cancellationToken = default(CancellationToken));

-        public virtual bool EnsureCreated();

-        public virtual Task<bool> EnsureCreatedAsync(CancellationToken cancellationToken = default(CancellationToken));

-        public virtual bool EnsureDeleted();

-        public virtual Task<bool> EnsureDeletedAsync(CancellationToken cancellationToken = default(CancellationToken));

-    }
-    public class InMemoryStore : IInMemoryStore {
 {
-        public InMemoryStore(IInMemoryTableFactory tableFactory);

-        public InMemoryStore(IInMemoryTableFactory tableFactory, bool useNameMatching);

-        public virtual bool Clear();

-        public virtual bool EnsureCreated(StateManagerDependencies stateManagerDependencies, IDiagnosticsLogger<DbLoggerCategory.Update> updateLogger);

-        public virtual int ExecuteTransaction(IReadOnlyList<IUpdateEntry> entries, IDiagnosticsLogger<DbLoggerCategory.Update> updateLogger);

-        public virtual IReadOnlyList<InMemoryTableSnapshot> GetTables(IEntityType entityType);

-    }
-    public class InMemoryStoreCache : IInMemoryStoreCache {
 {
-        public InMemoryStoreCache(IInMemoryTableFactory tableFactory);

-        public InMemoryStoreCache(IInMemoryTableFactory tableFactory, IInMemorySingletonOptions options);

-        public virtual IInMemoryStore GetStore(string name);

-    }
-    public static class InMemoryStoreCacheExtensions {
 {
-        public static IInMemoryStore GetStore(this IInMemoryStoreCache storeCache, IDbContextOptions options);

-    }
-    public class InMemoryTable<TKey> : IInMemoryTable {
 {
-        public InMemoryTable(IPrincipalKeyValueFactory<TKey> keyValueFactory, bool sensitiveLoggingEnabled);

-        public virtual void Create(IUpdateEntry entry);

-        public virtual void Delete(IUpdateEntry entry);

-        public virtual IReadOnlyList<object[]> SnapshotRows();

-        protected virtual void ThrowUpdateConcurrencyException(IUpdateEntry entry, Dictionary<IProperty, object> concurrencyConflicts);

-        public virtual void Update(IUpdateEntry entry);

-    }
-    public class InMemoryTableFactory : IdentityMapFactoryFactoryBase, IInMemoryTableFactory {
 {
-        public InMemoryTableFactory(ILoggingOptions loggingOptions);

-        public virtual IInMemoryTable Create(IEntityType entityType);

-    }
-    public class InMemoryTableSnapshot {
 {
-        public InMemoryTableSnapshot(IEntityType entityType, IReadOnlyList<object[]> rows);

-        public virtual IEntityType EntityType { get; }

-        public virtual IReadOnlyList<object[]> Rows { get; }

-    }
-    public class InMemoryTransaction : IDbContextTransaction, IDisposable {
 {
-        public InMemoryTransaction();

-        public virtual Guid TransactionId { get; }

-        public virtual void Commit();

-        public virtual void Dispose();

-        public virtual void Rollback();

-    }
-    public class InMemoryTransactionManager : IDbContextTransactionManager, IResettableService, ITransactionEnlistmentManager {
 {
-        public InMemoryTransactionManager(IDiagnosticsLogger<DbLoggerCategory.Database.Transaction> logger);

-        public virtual IDbContextTransaction CurrentTransaction { get; }

-        public virtual Transaction EnlistedTransaction { get; }

-        public virtual IDbContextTransaction BeginTransaction();

-        public virtual Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default(CancellationToken));

-        public virtual void CommitTransaction();

-        public virtual void EnlistTransaction(Transaction transaction);

-        public virtual void ResetState();

-        public virtual void RollbackTransaction();

-    }
-    public class InMemoryTypeMapping : CoreTypeMapping {
 {
-        public InMemoryTypeMapping(Type clrType, ValueComparer comparer = null, ValueComparer keyComparer = null, ValueComparer structuralComparer = null);

-        public override CoreTypeMapping Clone(ValueConverter converter);

-    }
-    public class InMemoryTypeMappingSource : TypeMappingSource {
 {
-        public InMemoryTypeMappingSource(TypeMappingSourceDependencies dependencies);

-        protected override CoreTypeMapping FindMapping(in TypeMappingInfo mappingInfo);

-    }
-}
```

