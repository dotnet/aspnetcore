# Microsoft.EntityFrameworkCore

``` diff
-namespace Microsoft.EntityFrameworkCore {
 {
-    public enum ChangeTrackingStrategy {
 {
-        ChangedNotifications = 1,

-        ChangingAndChangedNotifications = 2,

-        ChangingAndChangedNotificationsWithOriginalValues = 3,

-        Snapshot = 0,

-    }
-    public class DbContext : IDbContextDependencies, IDbContextPoolable, IDbQueryCache, IDbSetCache, IDisposable, IInfrastructure<IServiceProvider> {
 {
-        protected DbContext();

-        public DbContext(DbContextOptions options);

-        public virtual ChangeTracker ChangeTracker { get; }

-        public virtual DatabaseFacade Database { get; }

-        IChangeDetector Microsoft.EntityFrameworkCore.Internal.IDbContextDependencies.ChangeDetector { get; }

-        IEntityFinderFactory Microsoft.EntityFrameworkCore.Internal.IDbContextDependencies.EntityFinderFactory { get; }

-        IEntityGraphAttacher Microsoft.EntityFrameworkCore.Internal.IDbContextDependencies.EntityGraphAttacher { get; }

-        IDiagnosticsLogger<DbLoggerCategory.Infrastructure> Microsoft.EntityFrameworkCore.Internal.IDbContextDependencies.InfrastructureLogger { get; }

-        IAsyncQueryProvider Microsoft.EntityFrameworkCore.Internal.IDbContextDependencies.QueryProvider { get; }

-        IDbQuerySource Microsoft.EntityFrameworkCore.Internal.IDbContextDependencies.QuerySource { get; }

-        IDbSetSource Microsoft.EntityFrameworkCore.Internal.IDbContextDependencies.SetSource { get; }

-        IStateManager Microsoft.EntityFrameworkCore.Internal.IDbContextDependencies.StateManager { get; }

-        IDiagnosticsLogger<DbLoggerCategory.Update> Microsoft.EntityFrameworkCore.Internal.IDbContextDependencies.UpdateLogger { get; }

-        public virtual IModel Model { get; }

-        public virtual EntityEntry Add(object entity);

-        public virtual EntityEntry<TEntity> Add<TEntity>(TEntity entity) where TEntity : class;

-        public virtual Task<EntityEntry> AddAsync(object entity, CancellationToken cancellationToken = default(CancellationToken));

-        public virtual Task<EntityEntry<TEntity>> AddAsync<TEntity>(TEntity entity, CancellationToken cancellationToken = default(CancellationToken)) where TEntity : class;

-        public virtual void AddRange(IEnumerable<object> entities);

-        public virtual void AddRange(params object[] entities);

-        public virtual Task AddRangeAsync(IEnumerable<object> entities, CancellationToken cancellationToken = default(CancellationToken));

-        public virtual Task AddRangeAsync(params object[] entities);

-        public virtual EntityEntry Attach(object entity);

-        public virtual EntityEntry<TEntity> Attach<TEntity>(TEntity entity) where TEntity : class;

-        public virtual void AttachRange(IEnumerable<object> entities);

-        public virtual void AttachRange(params object[] entities);

-        public virtual void Dispose();

-        public virtual EntityEntry Entry(object entity);

-        public virtual EntityEntry<TEntity> Entry<TEntity>(TEntity entity) where TEntity : class;

-        public override bool Equals(object obj);

-        public virtual object Find(Type entityType, params object[] keyValues);

-        public virtual TEntity Find<TEntity>(params object[] keyValues) where TEntity : class;

-        public virtual Task<object> FindAsync(Type entityType, params object[] keyValues);

-        public virtual Task<object> FindAsync(Type entityType, object[] keyValues, CancellationToken cancellationToken);

-        public virtual Task<TEntity> FindAsync<TEntity>(params object[] keyValues) where TEntity : class;

-        public virtual Task<TEntity> FindAsync<TEntity>(object[] keyValues, CancellationToken cancellationToken) where TEntity : class;

-        public override int GetHashCode();

-        void Microsoft.EntityFrameworkCore.Internal.IDbContextPoolable.ResetState();

-        void Microsoft.EntityFrameworkCore.Internal.IDbContextPoolable.Resurrect(DbContextPoolConfigurationSnapshot configurationSnapshot);

-        void Microsoft.EntityFrameworkCore.Internal.IDbContextPoolable.SetPool(IDbContextPool contextPool);

-        DbContextPoolConfigurationSnapshot Microsoft.EntityFrameworkCore.Internal.IDbContextPoolable.SnapshotConfiguration();

-        object Microsoft.EntityFrameworkCore.Internal.IDbQueryCache.GetOrAddQuery(IDbQuerySource source, Type type);

-        object Microsoft.EntityFrameworkCore.Internal.IDbSetCache.GetOrAddSet(IDbSetSource source, Type type);

-        protected internal virtual void OnConfiguring(DbContextOptionsBuilder optionsBuilder);

-        protected internal virtual void OnModelCreating(ModelBuilder modelBuilder);

-        public virtual DbQuery<TQuery> Query<TQuery>() where TQuery : class;

-        public virtual EntityEntry Remove(object entity);

-        public virtual EntityEntry<TEntity> Remove<TEntity>(TEntity entity) where TEntity : class;

-        public virtual void RemoveRange(IEnumerable<object> entities);

-        public virtual void RemoveRange(params object[] entities);

-        public virtual int SaveChanges();

-        public virtual int SaveChanges(bool acceptAllChangesOnSuccess);

-        public virtual Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default(CancellationToken));

-        public virtual Task<int> SaveChangesAsync(CancellationToken cancellationToken = default(CancellationToken));

-        public virtual DbSet<TEntity> Set<TEntity>() where TEntity : class;

-        public override string ToString();

-        public virtual EntityEntry Update(object entity);

-        public virtual EntityEntry<TEntity> Update<TEntity>(TEntity entity) where TEntity : class;

-        public virtual void UpdateRange(IEnumerable<object> entities);

-        public virtual void UpdateRange(params object[] entities);

-    }
-    public abstract class DbContextOptions : IDbContextOptions {
 {
-        protected DbContextOptions(IReadOnlyDictionary<Type, IDbContextOptionsExtension> extensions);

-        public abstract Type ContextType { get; }

-        public virtual IEnumerable<IDbContextOptionsExtension> Extensions { get; }

-        public virtual bool IsFrozen { get; private set; }

-        public virtual TExtension FindExtension<TExtension>() where TExtension : class, IDbContextOptionsExtension;

-        public virtual void Freeze();

-        public virtual TExtension GetExtension<TExtension>() where TExtension : class, IDbContextOptionsExtension;

-        public abstract DbContextOptions WithExtension<TExtension>(TExtension extension) where TExtension : class, IDbContextOptionsExtension;

-    }
-    public class DbContextOptions<TContext> : DbContextOptions where TContext : DbContext {
 {
-        public DbContextOptions();

-        public DbContextOptions(IReadOnlyDictionary<Type, IDbContextOptionsExtension> extensions);

-        public override Type ContextType { get; }

-        public override DbContextOptions WithExtension<TExtension>(TExtension extension);

-    }
-    public class DbContextOptionsBuilder : IDbContextOptionsBuilderInfrastructure {
 {
-        public DbContextOptionsBuilder();

-        public DbContextOptionsBuilder(DbContextOptions options);

-        public virtual bool IsConfigured { get; }

-        public virtual DbContextOptions Options { get; }

-        public virtual DbContextOptionsBuilder ConfigureWarnings(Action<WarningsConfigurationBuilder> warningsConfigurationBuilderAction);

-        public virtual DbContextOptionsBuilder EnableDetailedErrors(bool detailedErrorsEnabled = true);

-        public virtual DbContextOptionsBuilder EnableSensitiveDataLogging(bool sensitiveDataLoggingEnabled = true);

-        public override bool Equals(object obj);

-        public override int GetHashCode();

-        void Microsoft.EntityFrameworkCore.Infrastructure.IDbContextOptionsBuilderInfrastructure.AddOrUpdateExtension<TExtension>(TExtension extension);

-        public virtual DbContextOptionsBuilder ReplaceService<TService, TImplementation>() where TImplementation : TService;

-        public override string ToString();

-        public virtual DbContextOptionsBuilder UseApplicationServiceProvider(IServiceProvider serviceProvider);

-        public virtual DbContextOptionsBuilder UseInternalServiceProvider(IServiceProvider serviceProvider);

-        public virtual DbContextOptionsBuilder UseLoggerFactory(ILoggerFactory loggerFactory);

-        public virtual DbContextOptionsBuilder UseMemoryCache(IMemoryCache memoryCache);

-        public virtual DbContextOptionsBuilder UseModel(IModel model);

-        public virtual DbContextOptionsBuilder UseQueryTrackingBehavior(QueryTrackingBehavior queryTrackingBehavior);

-    }
-    public class DbContextOptionsBuilder<TContext> : DbContextOptionsBuilder where TContext : DbContext {
 {
-        public DbContextOptionsBuilder();

-        public DbContextOptionsBuilder(DbContextOptions<TContext> options);

-        public virtual new DbContextOptions<TContext> Options { get; }

-        public virtual new DbContextOptionsBuilder<TContext> ConfigureWarnings(Action<WarningsConfigurationBuilder> warningsConfigurationBuilderAction);

-        public virtual new DbContextOptionsBuilder<TContext> EnableDetailedErrors(bool detailedErrorsEnabled = true);

-        public virtual new DbContextOptionsBuilder<TContext> EnableSensitiveDataLogging(bool sensitiveDataLoggingEnabled = true);

-        public virtual new DbContextOptionsBuilder<TContext> ReplaceService<TService, TImplementation>() where TImplementation : TService;

-        public virtual new DbContextOptionsBuilder<TContext> UseApplicationServiceProvider(IServiceProvider serviceProvider);

-        public virtual new DbContextOptionsBuilder<TContext> UseInternalServiceProvider(IServiceProvider serviceProvider);

-        public virtual new DbContextOptionsBuilder<TContext> UseLoggerFactory(ILoggerFactory loggerFactory);

-        public virtual new DbContextOptionsBuilder<TContext> UseMemoryCache(IMemoryCache memoryCache);

-        public virtual new DbContextOptionsBuilder<TContext> UseModel(IModel model);

-        public virtual new DbContextOptionsBuilder<TContext> UseQueryTrackingBehavior(QueryTrackingBehavior queryTrackingBehavior);

-    }
-    public class DbFunctionAttribute : Attribute {
 {
-        public DbFunctionAttribute();

-        public DbFunctionAttribute(string functionName, string schema = null);

-        public virtual string FunctionName { get; set; }

-        public virtual string Schema { get; set; }

-    }
-    public class DbFunctions {
 {
-        public override bool Equals(object obj);

-        public override int GetHashCode();

-        public override string ToString();

-    }
-    public static class DbFunctionsExtensions {
 {
-        public static bool Like(this DbFunctions _, string matchExpression, string pattern);

-        public static bool Like(this DbFunctions _, string matchExpression, string pattern, string escapeCharacter);

-    }
-    public static class DbLoggerCategory {
 {
-        public const string Name = "Microsoft.EntityFrameworkCore";

-        public class ChangeTracking : LoggerCategory<DbLoggerCategory.ChangeTracking> {
 {
-            public ChangeTracking();

-        }
-        public class Database : LoggerCategory<DbLoggerCategory.Database> {
 {
-            public Database();

-            public class Command : LoggerCategory<DbLoggerCategory.Database.Command> {
 {
-                public Command();

-            }
-            public class Connection : LoggerCategory<DbLoggerCategory.Database.Connection> {
 {
-                public Connection();

-            }
-            public class Transaction : LoggerCategory<DbLoggerCategory.Database.Transaction> {
 {
-                public Transaction();

-            }
-        }
-        public class Infrastructure : LoggerCategory<DbLoggerCategory.Infrastructure> {
 {
-            public Infrastructure();

-        }
-        public class Migrations : LoggerCategory<DbLoggerCategory.Migrations> {
 {
-            public Migrations();

-        }
-        public class Model : LoggerCategory<DbLoggerCategory.Model> {
 {
-            public Model();

-            public class Validation : LoggerCategory<DbLoggerCategory.Model.Validation> {
 {
-                public Validation();

-            }
-        }
-        public class Query : LoggerCategory<DbLoggerCategory.Query> {
 {
-            public Query();

-        }
-        public class Scaffolding : LoggerCategory<DbLoggerCategory.Scaffolding> {
 {
-            public Scaffolding();

-        }
-        public class Update : LoggerCategory<DbLoggerCategory.Update> {
 {
-            public Update();

-        }
-    }
-    public abstract class DbQuery<TQuery> : IAsyncEnumerableAccessor<TQuery>, IEnumerable, IEnumerable<TQuery>, IInfrastructure<IServiceProvider>, IQueryable, IQueryable<TQuery> where TQuery : class {
 {
-        protected DbQuery();

-        IAsyncEnumerable<TQuery> Microsoft.EntityFrameworkCore.Query.Internal.IAsyncEnumerableAccessor<TQuery>.AsyncEnumerable { get; }

-        Type System.Linq.IQueryable.ElementType { get; }

-        Expression System.Linq.IQueryable.Expression { get; }

-        IQueryProvider System.Linq.IQueryable.Provider { get; }

-        IEnumerator<TQuery> System.Collections.Generic.IEnumerable<TQuery>.GetEnumerator();

-        IEnumerator System.Collections.IEnumerable.GetEnumerator();

-    }
-    public abstract class DbSet<TEntity> : IAsyncEnumerableAccessor<TEntity>, IEnumerable, IEnumerable<TEntity>, IInfrastructure<IServiceProvider>, IListSource, IQueryable, IQueryable<TEntity> where TEntity : class {
 {
-        protected DbSet();

-        public virtual LocalView<TEntity> Local { get; }

-        IAsyncEnumerable<TEntity> Microsoft.EntityFrameworkCore.Query.Internal.IAsyncEnumerableAccessor<TEntity>.AsyncEnumerable { get; }

-        bool System.ComponentModel.IListSource.ContainsListCollection { get; }

-        Type System.Linq.IQueryable.ElementType { get; }

-        Expression System.Linq.IQueryable.Expression { get; }

-        IQueryProvider System.Linq.IQueryable.Provider { get; }

-        public virtual EntityEntry<TEntity> Add(TEntity entity);

-        public virtual Task<EntityEntry<TEntity>> AddAsync(TEntity entity, CancellationToken cancellationToken = default(CancellationToken));

-        public virtual void AddRange(IEnumerable<TEntity> entities);

-        public virtual void AddRange(params TEntity[] entities);

-        public virtual Task AddRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default(CancellationToken));

-        public virtual Task AddRangeAsync(params TEntity[] entities);

-        public virtual EntityEntry<TEntity> Attach(TEntity entity);

-        public virtual void AttachRange(IEnumerable<TEntity> entities);

-        public virtual void AttachRange(params TEntity[] entities);

-        public override bool Equals(object obj);

-        public virtual TEntity Find(params object[] keyValues);

-        public virtual Task<TEntity> FindAsync(params object[] keyValues);

-        public virtual Task<TEntity> FindAsync(object[] keyValues, CancellationToken cancellationToken);

-        public override int GetHashCode();

-        public virtual EntityEntry<TEntity> Remove(TEntity entity);

-        public virtual void RemoveRange(IEnumerable<TEntity> entities);

-        public virtual void RemoveRange(params TEntity[] entities);

-        IEnumerator<TEntity> System.Collections.Generic.IEnumerable<TEntity>.GetEnumerator();

-        IEnumerator System.Collections.IEnumerable.GetEnumerator();

-        IList System.ComponentModel.IListSource.GetList();

-        public override string ToString();

-        public virtual EntityEntry<TEntity> Update(TEntity entity);

-        public virtual void UpdateRange(IEnumerable<TEntity> entities);

-        public virtual void UpdateRange(params TEntity[] entities);

-    }
-    public class DbUpdateConcurrencyException : DbUpdateException {
 {
-        public DbUpdateConcurrencyException(string message, IReadOnlyList<IUpdateEntry> entries);

-    }
-    public class DbUpdateException : Exception {
 {
-        public DbUpdateException(string message, IReadOnlyList<IUpdateEntry> entries);

-        public DbUpdateException(string message, Exception innerException);

-        public DbUpdateException(string message, Exception innerException, IReadOnlyList<IUpdateEntry> entries);

-        public virtual IReadOnlyList<EntityEntry> Entries { get; }

-    }
-    public enum DeleteBehavior {
 {
-        Cascade = 3,

-        ClientSetNull = 0,

-        Restrict = 1,

-        SetNull = 2,

-    }
-    public static class EF {
 {
-        public static DbFunctions Functions { get; }

-        public static Func<TContext, TParam1, TParam2, TParam3, TParam4, TParam5, AsyncEnumerable<TResult>> CompileAsyncQuery<TContext, TParam1, TParam2, TParam3, TParam4, TParam5, TResult>(Expression<Func<TContext, TParam1, TParam2, TParam3, TParam4, TParam5, IQueryable<TResult>>> queryExpression) where TContext : DbContext;

-        public static Func<TContext, TParam1, TParam2, TParam3, TParam4, TParam5, CancellationToken, Task<TResult>> CompileAsyncQuery<TContext, TParam1, TParam2, TParam3, TParam4, TParam5, TResult>(Expression<Func<TContext, TParam1, TParam2, TParam3, TParam4, TParam5, CancellationToken, TResult>> queryExpression) where TContext : DbContext;

-        public static Func<TContext, TParam1, TParam2, TParam3, TParam4, TParam5, Task<TResult>> CompileAsyncQuery<TContext, TParam1, TParam2, TParam3, TParam4, TParam5, TResult>(Expression<Func<TContext, TParam1, TParam2, TParam3, TParam4, TParam5, TResult>> queryExpression) where TContext : DbContext;

-        public static Func<TContext, TParam1, TParam2, TParam3, TParam4, AsyncEnumerable<TResult>> CompileAsyncQuery<TContext, TParam1, TParam2, TParam3, TParam4, TResult>(Expression<Func<TContext, TParam1, TParam2, TParam3, TParam4, IQueryable<TResult>>> queryExpression) where TContext : DbContext;

-        public static Func<TContext, TParam1, TParam2, TParam3, TParam4, CancellationToken, Task<TResult>> CompileAsyncQuery<TContext, TParam1, TParam2, TParam3, TParam4, TResult>(Expression<Func<TContext, TParam1, TParam2, TParam3, TParam4, CancellationToken, TResult>> queryExpression) where TContext : DbContext;

-        public static Func<TContext, TParam1, TParam2, TParam3, TParam4, Task<TResult>> CompileAsyncQuery<TContext, TParam1, TParam2, TParam3, TParam4, TResult>(Expression<Func<TContext, TParam1, TParam2, TParam3, TParam4, TResult>> queryExpression) where TContext : DbContext;

-        public static Func<TContext, TParam1, TParam2, TParam3, AsyncEnumerable<TResult>> CompileAsyncQuery<TContext, TParam1, TParam2, TParam3, TResult>(Expression<Func<TContext, TParam1, TParam2, TParam3, IQueryable<TResult>>> queryExpression) where TContext : DbContext;

-        public static Func<TContext, TParam1, TParam2, TParam3, CancellationToken, Task<TResult>> CompileAsyncQuery<TContext, TParam1, TParam2, TParam3, TResult>(Expression<Func<TContext, TParam1, TParam2, TParam3, CancellationToken, TResult>> queryExpression) where TContext : DbContext;

-        public static Func<TContext, TParam1, TParam2, TParam3, Task<TResult>> CompileAsyncQuery<TContext, TParam1, TParam2, TParam3, TResult>(Expression<Func<TContext, TParam1, TParam2, TParam3, TResult>> queryExpression) where TContext : DbContext;

-        public static Func<TContext, TParam1, TParam2, AsyncEnumerable<TResult>> CompileAsyncQuery<TContext, TParam1, TParam2, TResult>(Expression<Func<TContext, TParam1, TParam2, IQueryable<TResult>>> queryExpression) where TContext : DbContext;

-        public static Func<TContext, TParam1, TParam2, CancellationToken, Task<TResult>> CompileAsyncQuery<TContext, TParam1, TParam2, TResult>(Expression<Func<TContext, TParam1, TParam2, CancellationToken, TResult>> queryExpression) where TContext : DbContext;

-        public static Func<TContext, TParam1, TParam2, Task<TResult>> CompileAsyncQuery<TContext, TParam1, TParam2, TResult>(Expression<Func<TContext, TParam1, TParam2, TResult>> queryExpression) where TContext : DbContext;

-        public static Func<TContext, TParam1, AsyncEnumerable<TResult>> CompileAsyncQuery<TContext, TParam1, TResult>(Expression<Func<TContext, TParam1, IQueryable<TResult>>> queryExpression) where TContext : DbContext;

-        public static Func<TContext, TParam1, CancellationToken, Task<TResult>> CompileAsyncQuery<TContext, TParam1, TResult>(Expression<Func<TContext, TParam1, CancellationToken, TResult>> queryExpression) where TContext : DbContext;

-        public static Func<TContext, TParam1, Task<TResult>> CompileAsyncQuery<TContext, TParam1, TResult>(Expression<Func<TContext, TParam1, TResult>> queryExpression) where TContext : DbContext;

-        public static Func<TContext, AsyncEnumerable<TResult>> CompileAsyncQuery<TContext, TResult>(Expression<Func<TContext, DbQuery<TResult>>> queryExpression) where TContext : DbContext where TResult : class;

-        public static Func<TContext, AsyncEnumerable<TResult>> CompileAsyncQuery<TContext, TResult>(Expression<Func<TContext, DbSet<TResult>>> queryExpression) where TContext : DbContext where TResult : class;

-        public static Func<TContext, AsyncEnumerable<TResult>> CompileAsyncQuery<TContext, TResult>(Expression<Func<TContext, IQueryable<TResult>>> queryExpression) where TContext : DbContext;

-        public static Func<TContext, CancellationToken, Task<TResult>> CompileAsyncQuery<TContext, TResult>(Expression<Func<TContext, CancellationToken, TResult>> queryExpression) where TContext : DbContext;

-        public static Func<TContext, Task<TResult>> CompileAsyncQuery<TContext, TResult>(Expression<Func<TContext, TResult>> queryExpression) where TContext : DbContext;

-        public static Func<TContext, TParam1, TParam2, TParam3, TParam4, TParam5, IEnumerable<TResult>> CompileQuery<TContext, TParam1, TParam2, TParam3, TParam4, TParam5, TResult, TProperty>(Expression<Func<TContext, TParam1, TParam2, TParam3, TParam4, TParam5, IIncludableQueryable<TResult, TProperty>>> queryExpression) where TContext : DbContext;

-        public static Func<TContext, TParam1, TParam2, TParam3, TParam4, TParam5, IEnumerable<TResult>> CompileQuery<TContext, TParam1, TParam2, TParam3, TParam4, TParam5, TResult>(Expression<Func<TContext, TParam1, TParam2, TParam3, TParam4, TParam5, IQueryable<TResult>>> queryExpression) where TContext : DbContext;

-        public static Func<TContext, TParam1, TParam2, TParam3, TParam4, TParam5, TResult> CompileQuery<TContext, TParam1, TParam2, TParam3, TParam4, TParam5, TResult>(Expression<Func<TContext, TParam1, TParam2, TParam3, TParam4, TParam5, TResult>> queryExpression) where TContext : DbContext;

-        public static Func<TContext, TParam1, TParam2, TParam3, TParam4, IEnumerable<TResult>> CompileQuery<TContext, TParam1, TParam2, TParam3, TParam4, TResult, TProperty>(Expression<Func<TContext, TParam1, TParam2, TParam3, TParam4, IIncludableQueryable<TResult, TProperty>>> queryExpression) where TContext : DbContext;

-        public static Func<TContext, TParam1, TParam2, TParam3, TParam4, IEnumerable<TResult>> CompileQuery<TContext, TParam1, TParam2, TParam3, TParam4, TResult>(Expression<Func<TContext, TParam1, TParam2, TParam3, TParam4, IQueryable<TResult>>> queryExpression) where TContext : DbContext;

-        public static Func<TContext, TParam1, TParam2, TParam3, TParam4, TResult> CompileQuery<TContext, TParam1, TParam2, TParam3, TParam4, TResult>(Expression<Func<TContext, TParam1, TParam2, TParam3, TParam4, TResult>> queryExpression) where TContext : DbContext;

-        public static Func<TContext, TParam1, TParam2, TParam3, IEnumerable<TResult>> CompileQuery<TContext, TParam1, TParam2, TParam3, TResult, TProperty>(Expression<Func<TContext, TParam1, TParam2, TParam3, IIncludableQueryable<TResult, TProperty>>> queryExpression) where TContext : DbContext;

-        public static Func<TContext, TParam1, TParam2, TParam3, IEnumerable<TResult>> CompileQuery<TContext, TParam1, TParam2, TParam3, TResult>(Expression<Func<TContext, TParam1, TParam2, TParam3, IQueryable<TResult>>> queryExpression) where TContext : DbContext;

-        public static Func<TContext, TParam1, TParam2, TParam3, TResult> CompileQuery<TContext, TParam1, TParam2, TParam3, TResult>(Expression<Func<TContext, TParam1, TParam2, TParam3, TResult>> queryExpression) where TContext : DbContext;

-        public static Func<TContext, TParam1, TParam2, IEnumerable<TResult>> CompileQuery<TContext, TParam1, TParam2, TResult, TProperty>(Expression<Func<TContext, TParam1, TParam2, IIncludableQueryable<TResult, TProperty>>> queryExpression) where TContext : DbContext;

-        public static Func<TContext, TParam1, TParam2, IEnumerable<TResult>> CompileQuery<TContext, TParam1, TParam2, TResult>(Expression<Func<TContext, TParam1, TParam2, IQueryable<TResult>>> queryExpression) where TContext : DbContext;

-        public static Func<TContext, TParam1, TParam2, TResult> CompileQuery<TContext, TParam1, TParam2, TResult>(Expression<Func<TContext, TParam1, TParam2, TResult>> queryExpression) where TContext : DbContext;

-        public static Func<TContext, TParam1, IEnumerable<TResult>> CompileQuery<TContext, TParam1, TResult, TProperty>(Expression<Func<TContext, TParam1, IIncludableQueryable<TResult, TProperty>>> queryExpression) where TContext : DbContext;

-        public static Func<TContext, TParam1, IEnumerable<TResult>> CompileQuery<TContext, TParam1, TResult>(Expression<Func<TContext, TParam1, IQueryable<TResult>>> queryExpression) where TContext : DbContext;

-        public static Func<TContext, TParam1, TResult> CompileQuery<TContext, TParam1, TResult>(Expression<Func<TContext, TParam1, TResult>> queryExpression) where TContext : DbContext;

-        public static Func<TContext, IEnumerable<TResult>> CompileQuery<TContext, TResult, TProperty>(Expression<Func<TContext, IIncludableQueryable<TResult, TProperty>>> queryExpression) where TContext : DbContext;

-        public static Func<TContext, IEnumerable<TResult>> CompileQuery<TContext, TResult>(Expression<Func<TContext, DbQuery<TResult>>> queryExpression) where TContext : DbContext where TResult : class;

-        public static Func<TContext, IEnumerable<TResult>> CompileQuery<TContext, TResult>(Expression<Func<TContext, DbSet<TResult>>> queryExpression) where TContext : DbContext where TResult : class;

-        public static Func<TContext, IEnumerable<TResult>> CompileQuery<TContext, TResult>(Expression<Func<TContext, IQueryable<TResult>>> queryExpression) where TContext : DbContext;

-        public static Func<TContext, TResult> CompileQuery<TContext, TResult>(Expression<Func<TContext, TResult>> queryExpression) where TContext : DbContext;

-        public static TProperty Property<TProperty>(object entity, string propertyName);

-    }
-    public static class EntityFrameworkQueryableExtensions {
 {
-        public static Task<bool> AllAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, bool>> predicate, CancellationToken cancellationToken = default(CancellationToken));

-        public static Task<bool> AnyAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, bool>> predicate, CancellationToken cancellationToken = default(CancellationToken));

-        public static Task<bool> AnyAsync<TSource>(this IQueryable<TSource> source, CancellationToken cancellationToken = default(CancellationToken));

-        public static IQueryable<TEntity> AsNoTracking<TEntity>(this IQueryable<TEntity> source) where TEntity : class;

-        public static IQueryable<TEntity> AsTracking<TEntity>(this IQueryable<TEntity> source) where TEntity : class;

-        public static Task<Decimal> AverageAsync(this IQueryable<Decimal> source, CancellationToken cancellationToken = default(CancellationToken));

-        public static Task<double> AverageAsync(this IQueryable<double> source, CancellationToken cancellationToken = default(CancellationToken));

-        public static Task<double> AverageAsync(this IQueryable<int> source, CancellationToken cancellationToken = default(CancellationToken));

-        public static Task<double> AverageAsync(this IQueryable<long> source, CancellationToken cancellationToken = default(CancellationToken));

-        public static Task<Nullable<Decimal>> AverageAsync(this IQueryable<Nullable<Decimal>> source, CancellationToken cancellationToken = default(CancellationToken));

-        public static Task<Nullable<double>> AverageAsync(this IQueryable<Nullable<double>> source, CancellationToken cancellationToken = default(CancellationToken));

-        public static Task<Nullable<double>> AverageAsync(this IQueryable<Nullable<int>> source, CancellationToken cancellationToken = default(CancellationToken));

-        public static Task<Nullable<double>> AverageAsync(this IQueryable<Nullable<long>> source, CancellationToken cancellationToken = default(CancellationToken));

-        public static Task<Nullable<float>> AverageAsync(this IQueryable<Nullable<float>> source, CancellationToken cancellationToken = default(CancellationToken));

-        public static Task<float> AverageAsync(this IQueryable<float> source, CancellationToken cancellationToken = default(CancellationToken));

-        public static Task<Decimal> AverageAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, Decimal>> selector, CancellationToken cancellationToken = default(CancellationToken));

-        public static Task<double> AverageAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, double>> selector, CancellationToken cancellationToken = default(CancellationToken));

-        public static Task<double> AverageAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, int>> selector, CancellationToken cancellationToken = default(CancellationToken));

-        public static Task<double> AverageAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, long>> selector, CancellationToken cancellationToken = default(CancellationToken));

-        public static Task<Nullable<Decimal>> AverageAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, Nullable<Decimal>>> selector, CancellationToken cancellationToken = default(CancellationToken));

-        public static Task<Nullable<double>> AverageAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, Nullable<double>>> selector, CancellationToken cancellationToken = default(CancellationToken));

-        public static Task<Nullable<double>> AverageAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, Nullable<int>>> selector, CancellationToken cancellationToken = default(CancellationToken));

-        public static Task<Nullable<double>> AverageAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, Nullable<long>>> selector, CancellationToken cancellationToken = default(CancellationToken));

-        public static Task<Nullable<float>> AverageAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, Nullable<float>>> selector, CancellationToken cancellationToken = default(CancellationToken));

-        public static Task<float> AverageAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, float>> selector, CancellationToken cancellationToken = default(CancellationToken));

-        public static Task<bool> ContainsAsync<TSource>(this IQueryable<TSource> source, TSource item, CancellationToken cancellationToken = default(CancellationToken));

-        public static Task<int> CountAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, bool>> predicate, CancellationToken cancellationToken = default(CancellationToken));

-        public static Task<int> CountAsync<TSource>(this IQueryable<TSource> source, CancellationToken cancellationToken = default(CancellationToken));

-        public static Task<TSource> FirstAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, bool>> predicate, CancellationToken cancellationToken = default(CancellationToken));

-        public static Task<TSource> FirstAsync<TSource>(this IQueryable<TSource> source, CancellationToken cancellationToken = default(CancellationToken));

-        public static Task<TSource> FirstOrDefaultAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, bool>> predicate, CancellationToken cancellationToken = default(CancellationToken));

-        public static Task<TSource> FirstOrDefaultAsync<TSource>(this IQueryable<TSource> source, CancellationToken cancellationToken = default(CancellationToken));

-        public static Task ForEachAsync<T>(this IQueryable<T> source, Action<T> action, CancellationToken cancellationToken = default(CancellationToken));

-        public static IQueryable<TEntity> IgnoreQueryFilters<TEntity>(this IQueryable<TEntity> source) where TEntity : class;

-        public static IIncludableQueryable<TEntity, TProperty> Include<TEntity, TProperty>(this IQueryable<TEntity> source, Expression<Func<TEntity, TProperty>> navigationPropertyPath) where TEntity : class;

-        public static IQueryable<TEntity> Include<TEntity>(this IQueryable<TEntity> source, string navigationPropertyPath) where TEntity : class;

-        public static Task<TSource> LastAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, bool>> predicate, CancellationToken cancellationToken = default(CancellationToken));

-        public static Task<TSource> LastAsync<TSource>(this IQueryable<TSource> source, CancellationToken cancellationToken = default(CancellationToken));

-        public static Task<TSource> LastOrDefaultAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, bool>> predicate, CancellationToken cancellationToken = default(CancellationToken));

-        public static Task<TSource> LastOrDefaultAsync<TSource>(this IQueryable<TSource> source, CancellationToken cancellationToken = default(CancellationToken));

-        public static void Load<TSource>(this IQueryable<TSource> source);

-        public static Task LoadAsync<TSource>(this IQueryable<TSource> source, CancellationToken cancellationToken = default(CancellationToken));

-        public static Task<long> LongCountAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, bool>> predicate, CancellationToken cancellationToken = default(CancellationToken));

-        public static Task<long> LongCountAsync<TSource>(this IQueryable<TSource> source, CancellationToken cancellationToken = default(CancellationToken));

-        public static Task<TResult> MaxAsync<TSource, TResult>(this IQueryable<TSource> source, Expression<Func<TSource, TResult>> selector, CancellationToken cancellationToken = default(CancellationToken));

-        public static Task<TSource> MaxAsync<TSource>(this IQueryable<TSource> source, CancellationToken cancellationToken = default(CancellationToken));

-        public static Task<TResult> MinAsync<TSource, TResult>(this IQueryable<TSource> source, Expression<Func<TSource, TResult>> selector, CancellationToken cancellationToken = default(CancellationToken));

-        public static Task<TSource> MinAsync<TSource>(this IQueryable<TSource> source, CancellationToken cancellationToken = default(CancellationToken));

-        public static Task<TSource> SingleAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, bool>> predicate, CancellationToken cancellationToken = default(CancellationToken));

-        public static Task<TSource> SingleAsync<TSource>(this IQueryable<TSource> source, CancellationToken cancellationToken = default(CancellationToken));

-        public static Task<TSource> SingleOrDefaultAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, bool>> predicate, CancellationToken cancellationToken = default(CancellationToken));

-        public static Task<TSource> SingleOrDefaultAsync<TSource>(this IQueryable<TSource> source, CancellationToken cancellationToken = default(CancellationToken));

-        public static Task<Decimal> SumAsync(this IQueryable<Decimal> source, CancellationToken cancellationToken = default(CancellationToken));

-        public static Task<double> SumAsync(this IQueryable<double> source, CancellationToken cancellationToken = default(CancellationToken));

-        public static Task<int> SumAsync(this IQueryable<int> source, CancellationToken cancellationToken = default(CancellationToken));

-        public static Task<long> SumAsync(this IQueryable<long> source, CancellationToken cancellationToken = default(CancellationToken));

-        public static Task<Nullable<Decimal>> SumAsync(this IQueryable<Nullable<Decimal>> source, CancellationToken cancellationToken = default(CancellationToken));

-        public static Task<Nullable<double>> SumAsync(this IQueryable<Nullable<double>> source, CancellationToken cancellationToken = default(CancellationToken));

-        public static Task<Nullable<int>> SumAsync(this IQueryable<Nullable<int>> source, CancellationToken cancellationToken = default(CancellationToken));

-        public static Task<Nullable<long>> SumAsync(this IQueryable<Nullable<long>> source, CancellationToken cancellationToken = default(CancellationToken));

-        public static Task<Nullable<float>> SumAsync(this IQueryable<Nullable<float>> source, CancellationToken cancellationToken = default(CancellationToken));

-        public static Task<float> SumAsync(this IQueryable<float> source, CancellationToken cancellationToken = default(CancellationToken));

-        public static Task<Decimal> SumAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, Decimal>> selector, CancellationToken cancellationToken = default(CancellationToken));

-        public static Task<double> SumAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, double>> selector, CancellationToken cancellationToken = default(CancellationToken));

-        public static Task<int> SumAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, int>> selector, CancellationToken cancellationToken = default(CancellationToken));

-        public static Task<long> SumAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, long>> selector, CancellationToken cancellationToken = default(CancellationToken));

-        public static Task<Nullable<Decimal>> SumAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, Nullable<Decimal>>> selector, CancellationToken cancellationToken = default(CancellationToken));

-        public static Task<Nullable<double>> SumAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, Nullable<double>>> selector, CancellationToken cancellationToken = default(CancellationToken));

-        public static Task<Nullable<int>> SumAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, Nullable<int>>> selector, CancellationToken cancellationToken = default(CancellationToken));

-        public static Task<Nullable<long>> SumAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, Nullable<long>>> selector, CancellationToken cancellationToken = default(CancellationToken));

-        public static Task<Nullable<float>> SumAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, Nullable<float>>> selector, CancellationToken cancellationToken = default(CancellationToken));

-        public static Task<float> SumAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, float>> selector, CancellationToken cancellationToken = default(CancellationToken));

-        public static IQueryable<T> TagWith<T>(this IQueryable<T> source, string tag);

-        public static IIncludableQueryable<TEntity, TProperty> ThenInclude<TEntity, TPreviousProperty, TProperty>(this IIncludableQueryable<TEntity, IEnumerable<TPreviousProperty>> source, Expression<Func<TPreviousProperty, TProperty>> navigationPropertyPath) where TEntity : class;

-        public static IIncludableQueryable<TEntity, TProperty> ThenInclude<TEntity, TPreviousProperty, TProperty>(this IIncludableQueryable<TEntity, TPreviousProperty> source, Expression<Func<TPreviousProperty, TProperty>> navigationPropertyPath) where TEntity : class;

-        public static Task<TSource[]> ToArrayAsync<TSource>(this IQueryable<TSource> source, CancellationToken cancellationToken = default(CancellationToken));

-        public static Task<Dictionary<TKey, TElement>> ToDictionaryAsync<TSource, TKey, TElement>(this IQueryable<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TElement> elementSelector, IEqualityComparer<TKey> comparer, CancellationToken cancellationToken = default(CancellationToken));

-        public static Task<Dictionary<TKey, TElement>> ToDictionaryAsync<TSource, TKey, TElement>(this IQueryable<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TElement> elementSelector, CancellationToken cancellationToken = default(CancellationToken));

-        public static Task<Dictionary<TKey, TSource>> ToDictionaryAsync<TSource, TKey>(this IQueryable<TSource> source, Func<TSource, TKey> keySelector, IEqualityComparer<TKey> comparer, CancellationToken cancellationToken = default(CancellationToken));

-        public static Task<Dictionary<TKey, TSource>> ToDictionaryAsync<TSource, TKey>(this IQueryable<TSource> source, Func<TSource, TKey> keySelector, CancellationToken cancellationToken = default(CancellationToken));

-        public static Task<List<TSource>> ToListAsync<TSource>(this IQueryable<TSource> source, CancellationToken cancellationToken = default(CancellationToken));

-    }
-    public enum EntityState {
 {
-        Added = 4,

-        Deleted = 2,

-        Detached = 0,

-        Modified = 3,

-        Unchanged = 1,

-    }
-    public static class EntityTypeExtensions {
 {
-        public static IForeignKey FindForeignKey(this IEntityType entityType, IProperty property, IKey principalKey, IEntityType principalEntityType);

-        public static IEnumerable<IForeignKey> FindForeignKeys(this IEntityType entityType, IProperty property);

-        public static IEnumerable<IForeignKey> FindForeignKeys(this IEntityType entityType, IReadOnlyList<IProperty> properties);

-        public static IIndex FindIndex(this IEntityType entityType, IProperty property);

-        public static IKey FindKey(this IEntityType entityType, IProperty property);

-        public static INavigation FindNavigation(this IEntityType entityType, PropertyInfo propertyInfo);

-        public static INavigation FindNavigation(this IEntityType entityType, string name);

-        public static IProperty FindProperty(this IEntityType entityType, PropertyInfo propertyInfo);

-        public static ChangeTrackingStrategy GetChangeTrackingStrategy(this IEntityType entityType);

-        public static IEnumerable<IEntityType> GetDerivedTypes(this IEntityType entityType);

-        public static IEnumerable<INavigation> GetNavigations(this IEntityType entityType);

-        public static IEnumerable<IForeignKey> GetReferencingForeignKeys(this IEntityType entityType);

-        public static bool HasDefiningNavigation(this IEntityType entityType);

-        public static bool IsAssignableFrom(this IEntityType entityType, IEntityType derivedType);

-        public static bool IsOwned(this IEntityType entityType);

-        public static IEntityType LeastDerivedType(this IEntityType entityType, IEntityType otherEntityType);

-        public static IEntityType RootType(this IEntityType entityType);

-    }
-    public static class ExecutionStrategyExtensions {
 {
-        public static void Execute(this IExecutionStrategy strategy, Action operation);

-        public static TResult Execute<TResult>(this IExecutionStrategy strategy, Func<TResult> operation);

-        public static TResult Execute<TState, TResult>(this IExecutionStrategy strategy, Func<TState, TResult> operation, Func<TState, ExecutionResult<TResult>> verifySucceeded, TState state);

-        public static TResult Execute<TState, TResult>(this IExecutionStrategy strategy, TState state, Func<TState, TResult> operation);

-        public static void Execute<TState>(this IExecutionStrategy strategy, TState state, Action<TState> operation);

-        public static Task ExecuteAsync(this IExecutionStrategy strategy, Func<CancellationToken, Task> operation, CancellationToken cancellationToken);

-        public static Task ExecuteAsync(this IExecutionStrategy strategy, Func<Task> operation);

-        public static Task<TResult> ExecuteAsync<TResult>(this IExecutionStrategy strategy, Func<CancellationToken, Task<TResult>> operation, CancellationToken cancellationToken);

-        public static Task<TResult> ExecuteAsync<TResult>(this IExecutionStrategy strategy, Func<Task<TResult>> operation);

-        public static Task<TResult> ExecuteAsync<TState, TResult>(this IExecutionStrategy strategy, TState state, Func<TState, CancellationToken, Task<TResult>> operation, Func<TState, CancellationToken, Task<ExecutionResult<TResult>>> verifySucceeded, CancellationToken cancellationToken = default(CancellationToken));

-        public static Task<TResult> ExecuteAsync<TState, TResult>(this IExecutionStrategy strategy, TState state, Func<TState, CancellationToken, Task<TResult>> operation, CancellationToken cancellationToken);

-        public static Task<TResult> ExecuteAsync<TState, TResult>(this IExecutionStrategy strategy, TState state, Func<TState, Task<TResult>> operation);

-        public static Task ExecuteAsync<TState>(this IExecutionStrategy strategy, TState state, Func<TState, CancellationToken, Task> operation, CancellationToken cancellationToken);

-        public static Task ExecuteAsync<TState>(this IExecutionStrategy strategy, TState state, Func<TState, Task> operation);

-        public static void ExecuteInTransaction(this IExecutionStrategy strategy, Action operation, Func<bool> verifySucceeded);

-        public static TResult ExecuteInTransaction<TResult>(this IExecutionStrategy strategy, Func<TResult> operation, Func<bool> verifySucceeded);

-        public static TResult ExecuteInTransaction<TState, TResult>(this IExecutionStrategy strategy, TState state, Func<TState, TResult> operation, Func<TState, bool> verifySucceeded);

-        public static TResult ExecuteInTransaction<TState, TResult>(IExecutionStrategy strategy, TState state, Func<TState, TResult> operation, Func<TState, bool> verifySucceeded, Func<DbContext, IDbContextTransaction> beginTransaction);

-        public static void ExecuteInTransaction<TState>(this IExecutionStrategy strategy, TState state, Action<TState> operation, Func<TState, bool> verifySucceeded);

-        public static Task ExecuteInTransactionAsync(this IExecutionStrategy strategy, Func<CancellationToken, Task> operation, Func<CancellationToken, Task<bool>> verifySucceeded, CancellationToken cancellationToken = default(CancellationToken));

-        public static Task ExecuteInTransactionAsync(this IExecutionStrategy strategy, Func<Task> operation, Func<Task<bool>> verifySucceeded);

-        public static Task<TResult> ExecuteInTransactionAsync<TResult>(this IExecutionStrategy strategy, Func<CancellationToken, Task<TResult>> operation, Func<CancellationToken, Task<bool>> verifySucceeded, CancellationToken cancellationToken = default(CancellationToken));

-        public static Task<TResult> ExecuteInTransactionAsync<TState, TResult>(IExecutionStrategy strategy, TState state, Func<TState, CancellationToken, Task<TResult>> operation, Func<TState, CancellationToken, Task<bool>> verifySucceeded, Func<DbContext, CancellationToken, Task<IDbContextTransaction>> beginTransaction, CancellationToken cancellationToken = default(CancellationToken));

-        public static Task<TResult> ExecuteInTransactionAsync<TState, TResult>(this IExecutionStrategy strategy, TState state, Func<TState, CancellationToken, Task<TResult>> operation, Func<TState, CancellationToken, Task<bool>> verifySucceeded, CancellationToken cancellationToken = default(CancellationToken));

-        public static Task ExecuteInTransactionAsync<TState>(this IExecutionStrategy strategy, TState state, Func<TState, CancellationToken, Task> operation, Func<TState, CancellationToken, Task<bool>> verifySucceeded, CancellationToken cancellationToken = default(CancellationToken));

-    }
-    public interface IEntityTypeConfiguration<TEntity> where TEntity : class {
 {
-        void Configure(EntityTypeBuilder<TEntity> builder);

-    }
-    public static class InMemoryDatabaseFacadeExtensions {
 {
-        public static bool IsInMemory(this DatabaseFacade database);

-    }
-    public static class InMemoryDbContextOptionsExtensions {
 {
-        public static DbContextOptionsBuilder UseInMemoryDatabase(this DbContextOptionsBuilder optionsBuilder, Action<InMemoryDbContextOptionsBuilder> inMemoryOptionsAction = null);

-        public static DbContextOptionsBuilder UseInMemoryDatabase(this DbContextOptionsBuilder optionsBuilder, string databaseName, InMemoryDatabaseRoot databaseRoot, Action<InMemoryDbContextOptionsBuilder> inMemoryOptionsAction = null);

-        public static DbContextOptionsBuilder UseInMemoryDatabase(this DbContextOptionsBuilder optionsBuilder, string databaseName, Action<InMemoryDbContextOptionsBuilder> inMemoryOptionsAction = null);

-        public static DbContextOptionsBuilder<TContext> UseInMemoryDatabase<TContext>(this DbContextOptionsBuilder<TContext> optionsBuilder, Action<InMemoryDbContextOptionsBuilder> inMemoryOptionsAction = null) where TContext : DbContext;

-        public static DbContextOptionsBuilder<TContext> UseInMemoryDatabase<TContext>(this DbContextOptionsBuilder<TContext> optionsBuilder, string databaseName, InMemoryDatabaseRoot databaseRoot, Action<InMemoryDbContextOptionsBuilder> inMemoryOptionsAction = null) where TContext : DbContext;

-        public static DbContextOptionsBuilder<TContext> UseInMemoryDatabase<TContext>(this DbContextOptionsBuilder<TContext> optionsBuilder, string databaseName, Action<InMemoryDbContextOptionsBuilder> inMemoryOptionsAction = null) where TContext : DbContext;

-    }
-    public interface IQueryTypeConfiguration<TQuery> where TQuery : class {
 {
-        void Configure(QueryTypeBuilder<TQuery> builder);

-    }
-    public static class KeyExtensions {
 {
-        public static IEnumerable<IForeignKey> GetReferencingForeignKeys(this IKey key);

-    }
-    public class ModelBuilder : IInfrastructure<InternalModelBuilder> {
 {
-        public ModelBuilder(ConventionSet conventions);

-        InternalModelBuilder Microsoft.EntityFrameworkCore.Infrastructure.IInfrastructure<Microsoft.EntityFrameworkCore.Metadata.Internal.InternalModelBuilder>.Instance { get; }

-        public virtual IMutableModel Model { get; }

-        public virtual ModelBuilder ApplyConfiguration<TEntity>(IEntityTypeConfiguration<TEntity> configuration) where TEntity : class;

-        public virtual ModelBuilder ApplyConfiguration<TQuery>(IQueryTypeConfiguration<TQuery> configuration) where TQuery : class;

-        public virtual ModelBuilder ApplyConfigurationsFromAssembly(Assembly assembly, Func<Type, bool> predicate = null);

-        public virtual EntityTypeBuilder Entity(string name);

-        public virtual ModelBuilder Entity(string name, Action<EntityTypeBuilder> buildAction);

-        public virtual EntityTypeBuilder Entity(Type type);

-        public virtual ModelBuilder Entity(Type type, Action<EntityTypeBuilder> buildAction);

-        public virtual EntityTypeBuilder<TEntity> Entity<TEntity>() where TEntity : class;

-        public virtual ModelBuilder Entity<TEntity>(Action<EntityTypeBuilder<TEntity>> buildAction) where TEntity : class;

-        public override bool Equals(object obj);

-        public virtual IModel FinalizeModel();

-        public override int GetHashCode();

-        public virtual ModelBuilder HasAnnotation(string annotation, object value);

-        public virtual ModelBuilder HasChangeTrackingStrategy(ChangeTrackingStrategy changeTrackingStrategy);

-        public virtual ModelBuilder Ignore(Type type);

-        public virtual ModelBuilder Ignore<TEntity>() where TEntity : class;

-        public virtual OwnedEntityTypeBuilder Owned(Type type);

-        public virtual OwnedEntityTypeBuilder<T> Owned<T>() where T : class;

-        public virtual QueryTypeBuilder Query(Type type);

-        public virtual ModelBuilder Query(Type type, Action<QueryTypeBuilder> buildAction);

-        public virtual QueryTypeBuilder<TQuery> Query<TQuery>() where TQuery : class;

-        public virtual ModelBuilder Query<TQuery>(Action<QueryTypeBuilder<TQuery>> buildAction) where TQuery : class;

-        public override string ToString();

-        public virtual ModelBuilder UsePropertyAccessMode(PropertyAccessMode propertyAccessMode);

-    }
-    public static class ModelExtensions {
 {
-        public static IEntityType FindEntityType(this IModel model, Type type);

-        public static IEntityType FindEntityType(this IModel model, Type type, string definingNavigationName, IEntityType definingEntityType);

-        public static IEntityType FindRuntimeEntityType(this IModel model, Type type);

-        public static ChangeTrackingStrategy GetChangeTrackingStrategy(this IModel model);

-        public static IReadOnlyCollection<EntityType> GetEntityTypes(this IModel model, string name);

-        public static IReadOnlyCollection<EntityType> GetEntityTypes(this IModel model, Type type);

-        public static Nullable<PropertyAccessMode> GetPropertyAccessMode(this IModel model);

-        public static bool HasEntityTypeWithDefiningNavigation(this IModel model, string name);

-        public static bool HasEntityTypeWithDefiningNavigation(this IModel model, Type type);

-    }
-    public static class MutableAnnotatableExtensions {
 {
-        public static void AddAnnotations(this IMutableAnnotatable annotatable, IEnumerable<IAnnotation> annotations);

-        public static Annotation GetOrAddAnnotation(this IMutableAnnotatable annotatable, string annotationName, string value);

-    }
-    public static class MutableEntityTypeExtensions {
 {
-        public static IMutableForeignKey AddForeignKey(this IMutableEntityType entityType, IMutableProperty property, IMutableKey principalKey, IMutableEntityType principalEntityType);

-        public static IMutableIndex AddIndex(this IMutableEntityType entityType, IMutableProperty property);

-        public static IMutableKey AddKey(this IMutableEntityType entityType, IMutableProperty property);

-        public static IMutableProperty AddProperty(this IMutableEntityType entityType, PropertyInfo propertyInfo);

-        public static IMutableForeignKey FindForeignKey(this IMutableEntityType entityType, IProperty property, IKey principalKey, IEntityType principalEntityType);

-        public static IEnumerable<IMutableForeignKey> FindForeignKeys(this IMutableEntityType entityType, IProperty property);

-        public static IEnumerable<IMutableForeignKey> FindForeignKeys(this IMutableEntityType entityType, IReadOnlyList<IProperty> properties);

-        public static IMutableIndex FindIndex(this IMutableEntityType entityType, IProperty property);

-        public static IMutableKey FindKey(this IMutableEntityType entityType, IProperty property);

-        public static IMutableNavigation FindNavigation(this IMutableEntityType entityType, PropertyInfo propertyInfo);

-        public static IMutableNavigation FindNavigation(this IMutableEntityType entityType, string name);

-        public static IMutableProperty FindProperty(this IMutableEntityType entityType, PropertyInfo propertyInfo);

-        public static IEnumerable<IMutableEntityType> GetDerivedTypes(this IMutableEntityType entityType);

-        public static IEnumerable<IMutableNavigation> GetNavigations(this IMutableEntityType entityType);

-        public static IMutableForeignKey GetOrAddForeignKey(this IMutableEntityType entityType, IMutableProperty property, IMutableKey principalKey, IMutableEntityType principalEntityType);

-        public static IMutableForeignKey GetOrAddForeignKey(this IMutableEntityType entityType, IReadOnlyList<IMutableProperty> properties, IMutableKey principalKey, IMutableEntityType principalEntityType);

-        public static IMutableIndex GetOrAddIndex(this IMutableEntityType entityType, IMutableProperty property);

-        public static IMutableIndex GetOrAddIndex(this IMutableEntityType entityType, IReadOnlyList<IMutableProperty> properties);

-        public static IMutableKey GetOrAddKey(this IMutableEntityType entityType, IMutableProperty property);

-        public static IMutableKey GetOrAddKey(this IMutableEntityType entityType, IReadOnlyList<IMutableProperty> properties);

-        public static IMutableProperty GetOrAddProperty(this IMutableEntityType entityType, PropertyInfo propertyInfo);

-        public static IMutableProperty GetOrAddProperty(this IMutableEntityType entityType, string name, Type propertyType);

-        public static IMutableKey GetOrSetPrimaryKey(this IMutableEntityType entityType, IMutableProperty property);

-        public static IMutableKey GetOrSetPrimaryKey(this IMutableEntityType entityType, IReadOnlyList<IMutableProperty> properties);

-        public static IEnumerable<IMutableForeignKey> GetReferencingForeignKeys(this IMutableEntityType entityType);

-        public static IMutableEntityType RootType(this IMutableEntityType entityType);

-        public static void SetChangeTrackingStrategy(this IMutableEntityType entityType, ChangeTrackingStrategy changeTrackingStrategy);

-        public static void SetNavigationAccessMode(this IMutableEntityType entityType, Nullable<PropertyAccessMode> propertyAccessMode);

-        public static IMutableKey SetPrimaryKey(this IMutableEntityType entityType, IMutableProperty property);

-        public static void SetPropertyAccessMode(this IMutableEntityType entityType, Nullable<PropertyAccessMode> propertyAccessMode);

-    }
-    public static class MutableKeyExtensions {
 {
-        public static IEnumerable<IMutableForeignKey> GetReferencingForeignKeys(this IMutableKey key);

-    }
-    public static class MutableModelExtensions {
 {
-        public static IMutableEntityType FindEntityType(this IMutableModel model, Type type);

-        public static IMutableEntityType FindEntityType(this IMutableModel model, Type type, string definingNavigationName, IMutableEntityType definingEntityType);

-        public static IMutableEntityType GetOrAddEntityType(this IMutableModel model, string name);

-        public static IMutableEntityType GetOrAddEntityType(this IMutableModel model, Type type);

-        public static IMutableEntityType RemoveEntityType(this IMutableModel model, IMutableEntityType entityType);

-        public static IMutableEntityType RemoveEntityType(this IMutableModel model, Type type);

-        public static IMutableEntityType RemoveEntityType(this IMutableModel model, Type type, string definingNavigationName, IMutableEntityType definingEntityType);

-        public static void SetChangeTrackingStrategy(this IMutableModel model, ChangeTrackingStrategy changeTrackingStrategy);

-        public static void SetPropertyAccessMode(this IMutableModel model, Nullable<PropertyAccessMode> propertyAccessMode);

-    }
-    public static class MutableNavigationExtensions {
 {
-        public static IMutableNavigation FindInverse(this IMutableNavigation navigation);

-        public static IMutableEntityType GetTargetType(this IMutableNavigation navigation);

-    }
-    public static class MutablePropertyBaseExtensions {
 {
-        public static void SetField(this IMutablePropertyBase property, string fieldName);

-        public static void SetPropertyAccessMode(this IMutablePropertyBase property, Nullable<PropertyAccessMode> propertyAccessMode);

-    }
-    public static class MutablePropertyExtensions {
 {
-        public static IEnumerable<IMutableForeignKey> GetContainingForeignKeys(this IMutableProperty property);

-        public static IEnumerable<IMutableKey> GetContainingKeys(this IMutableProperty property);

-        public static IMutableKey GetContainingPrimaryKey(this IMutableProperty property);

-        public static void IsUnicode(this IMutableProperty property, Nullable<bool> unicode);

-        public static void SetKeyValueComparer(this IMutableProperty property, ValueComparer comparer);

-        public static void SetMaxLength(this IMutableProperty property, Nullable<int> maxLength);

-        public static void SetProviderClrType(this IMutableProperty property, Type providerClrType);

-        public static void SetStructuralValueComparer(this IMutableProperty property, ValueComparer comparer);

-        public static void SetValueComparer(this IMutableProperty property, ValueComparer comparer);

-        public static void SetValueConverter(this IMutableProperty property, ValueConverter converter);

-        public static void SetValueGeneratorFactory(this IMutableProperty property, Func<IProperty, IEntityType, ValueGenerator> valueGeneratorFactory);

-    }
-    public static class NavigationExtensions {
 {
-        public static INavigation FindInverse(this INavigation navigation);

-        public static IEntityType GetTargetType(this INavigation navigation);

-        public static bool IsCollection(this INavigation navigation);

-        public static bool IsDependentToPrincipal(this INavigation navigation);

-    }
-    public static class ObservableCollectionExtensions {
 {
-        public static BindingList<T> ToBindingList<T>(this ObservableCollection<T> source) where T : class;

-    }
-    public sealed class OwnedAttribute : Attribute {
 {
-        public OwnedAttribute();

-    }
-    public enum PropertyAccessMode {
 {
-        Field = 0,

-        FieldDuringConstruction = 1,

-        Property = 2,

-    }
-    public static class PropertyBaseExtensions {
 {
-        public static string GetFieldName(this IPropertyBase propertyBase);

-        public static Nullable<PropertyAccessMode> GetPropertyAccessMode(this IPropertyBase propertyBase);

-    }
-    public static class PropertyExtensions {
 {
-        public static IEnumerable<IForeignKey> GetContainingForeignKeys(this IProperty property);

-        public static IEnumerable<IIndex> GetContainingIndexes(this IProperty property);

-        public static IEnumerable<IKey> GetContainingKeys(this IProperty property);

-        public static IKey GetContainingPrimaryKey(this IProperty property);

-        public static ValueComparer GetKeyValueComparer(this IProperty property);

-        public static Nullable<int> GetMaxLength(this IProperty property);

-        public static Type GetProviderClrType(this IProperty property);

-        public static ValueComparer GetStructuralValueComparer(this IProperty property);

-        public static ValueComparer GetValueComparer(this IProperty property);

-        public static ValueConverter GetValueConverter(this IProperty property);

-        public static Func<IProperty, IEntityType, ValueGenerator> GetValueGeneratorFactory(this IProperty property);

-        public static bool IsForeignKey(this IProperty property);

-        public static bool IsIndex(this IProperty property);

-        public static bool IsKey(this IProperty property);

-        public static bool IsPrimaryKey(this IProperty property);

-        public static Nullable<bool> IsUnicode(this IProperty property);

-    }
-    public enum QueryTrackingBehavior {
 {
-        NoTracking = 1,

-        TrackAll = 0,

-    }
-    public readonly struct RawSqlString {
 {
-        public RawSqlString(string s);

-        public string Format { get; }

-        public static implicit operator RawSqlString (FormattableString fs);

-        public static implicit operator RawSqlString (string s);

-    }
-    public static class RelationalCollectionOwnershipBuilderExtensions {
 {
-        public static CollectionOwnershipBuilder HasConstraintName(this CollectionOwnershipBuilder referenceReferenceBuilder, string name);

-        public static CollectionOwnershipBuilder<TEntity, TDependentEntity> HasConstraintName<TEntity, TDependentEntity>(this CollectionOwnershipBuilder<TEntity, TDependentEntity> referenceReferenceBuilder, string name) where TEntity : class where TDependentEntity : class;

-        public static CollectionOwnershipBuilder ToTable(this CollectionOwnershipBuilder collectionOwnershipBuilder, string name);

-        public static CollectionOwnershipBuilder ToTable(this CollectionOwnershipBuilder collectionOwnershipBuilder, string name, string schema);

-        public static CollectionOwnershipBuilder<TEntity, TDependentEntity> ToTable<TEntity, TDependentEntity>(this CollectionOwnershipBuilder<TEntity, TDependentEntity> collectionOwnershipBuilder, string name) where TEntity : class where TDependentEntity : class;

-        public static CollectionOwnershipBuilder<TEntity, TDependentEntity> ToTable<TEntity, TDependentEntity>(this CollectionOwnershipBuilder<TEntity, TDependentEntity> collectionOwnershipBuilder, string name, string schema) where TEntity : class where TDependentEntity : class;

-    }
-    public static class RelationalDatabaseFacadeExtensions {
 {
-        public static IDbContextTransaction BeginTransaction(this DatabaseFacade databaseFacade, IsolationLevel isolationLevel);

-        public static Task<IDbContextTransaction> BeginTransactionAsync(this DatabaseFacade databaseFacade, IsolationLevel isolationLevel, CancellationToken cancellationToken = default(CancellationToken));

-        public static void CloseConnection(this DatabaseFacade databaseFacade);

-        public static int ExecuteSqlCommand(this DatabaseFacade databaseFacade, RawSqlString sql, IEnumerable<object> parameters);

-        public static int ExecuteSqlCommand(this DatabaseFacade databaseFacade, RawSqlString sql, params object[] parameters);

-        public static int ExecuteSqlCommand(this DatabaseFacade databaseFacade, FormattableString sql);

-        public static Task<int> ExecuteSqlCommandAsync(this DatabaseFacade databaseFacade, RawSqlString sql, IEnumerable<object> parameters, CancellationToken cancellationToken = default(CancellationToken));

-        public static Task<int> ExecuteSqlCommandAsync(this DatabaseFacade databaseFacade, RawSqlString sql, params object[] parameters);

-        public static Task<int> ExecuteSqlCommandAsync(this DatabaseFacade databaseFacade, RawSqlString sql, CancellationToken cancellationToken = default(CancellationToken));

-        public static Task<int> ExecuteSqlCommandAsync(this DatabaseFacade databaseFacade, FormattableString sql, CancellationToken cancellationToken = default(CancellationToken));

-        public static string GenerateCreateScript(this DatabaseFacade databaseFacade);

-        public static IEnumerable<string> GetAppliedMigrations(this DatabaseFacade databaseFacade);

-        public static Task<IEnumerable<string>> GetAppliedMigrationsAsync(this DatabaseFacade databaseFacade, CancellationToken cancellationToken = default(CancellationToken));

-        public static Nullable<int> GetCommandTimeout(this DatabaseFacade databaseFacade);

-        public static DbConnection GetDbConnection(this DatabaseFacade databaseFacade);

-        public static IEnumerable<string> GetMigrations(this DatabaseFacade databaseFacade);

-        public static IEnumerable<string> GetPendingMigrations(this DatabaseFacade databaseFacade);

-        public static Task<IEnumerable<string>> GetPendingMigrationsAsync(this DatabaseFacade databaseFacade, CancellationToken cancellationToken = default(CancellationToken));

-        public static void Migrate(this DatabaseFacade databaseFacade);

-        public static Task MigrateAsync(this DatabaseFacade databaseFacade, CancellationToken cancellationToken = default(CancellationToken));

-        public static void OpenConnection(this DatabaseFacade databaseFacade);

-        public static Task OpenConnectionAsync(this DatabaseFacade databaseFacade, CancellationToken cancellationToken = default(CancellationToken));

-        public static void SetCommandTimeout(this DatabaseFacade databaseFacade, Nullable<int> timeout);

-        public static void SetCommandTimeout(this DatabaseFacade databaseFacade, TimeSpan timeout);

-        public static IDbContextTransaction UseTransaction(this DatabaseFacade databaseFacade, DbTransaction transaction);

-    }
-    public static class RelationalEntityTypeBuilderExtensions {
 {
-        public static DiscriminatorBuilder HasDiscriminator(this EntityTypeBuilder entityTypeBuilder);

-        public static DiscriminatorBuilder HasDiscriminator(this EntityTypeBuilder entityTypeBuilder, string name, Type discriminatorType);

-        public static DiscriminatorBuilder<TDiscriminator> HasDiscriminator<TDiscriminator>(this EntityTypeBuilder entityTypeBuilder, string name);

-        public static DiscriminatorBuilder<TDiscriminator> HasDiscriminator<TEntity, TDiscriminator>(this EntityTypeBuilder<TEntity> entityTypeBuilder, Expression<Func<TEntity, TDiscriminator>> propertyExpression) where TEntity : class;

-        public static EntityTypeBuilder ToTable(this EntityTypeBuilder entityTypeBuilder, string name);

-        public static EntityTypeBuilder ToTable(this EntityTypeBuilder entityTypeBuilder, string name, string schema);

-        public static EntityTypeBuilder<TEntity> ToTable<TEntity>(this EntityTypeBuilder<TEntity> entityTypeBuilder, string name) where TEntity : class;

-        public static EntityTypeBuilder<TEntity> ToTable<TEntity>(this EntityTypeBuilder<TEntity> entityTypeBuilder, string name, string schema) where TEntity : class;

-    }
-    public static class RelationalIndexBuilderExtensions {
 {
-        public static IndexBuilder HasFilter(this IndexBuilder indexBuilder, string sql);

-        public static IndexBuilder<TEntity> HasFilter<TEntity>(this IndexBuilder<TEntity> indexBuilder, string sql);

-        public static IndexBuilder HasName(this IndexBuilder indexBuilder, string name);

-        public static IndexBuilder<TEntity> HasName<TEntity>(this IndexBuilder<TEntity> indexBuilder, string name);

-    }
-    public static class RelationalKeyBuilderExtensions {
 {
-        public static KeyBuilder HasName(this KeyBuilder keyBuilder, string name);

-    }
-    public static class RelationalMetadataExtensions {
 {
-        public static IRelationalEntityTypeAnnotations Relational(this IEntityType entityType);

-        public static IRelationalForeignKeyAnnotations Relational(this IForeignKey foreignKey);

-        public static IRelationalIndexAnnotations Relational(this IIndex index);

-        public static IRelationalKeyAnnotations Relational(this IKey key);

-        public static IRelationalModelAnnotations Relational(this IModel model);

-        public static RelationalEntityTypeAnnotations Relational(this IMutableEntityType entityType);

-        public static RelationalForeignKeyAnnotations Relational(this IMutableForeignKey foreignKey);

-        public static RelationalIndexAnnotations Relational(this IMutableIndex index);

-        public static RelationalKeyAnnotations Relational(this IMutableKey key);

-        public static RelationalModelAnnotations Relational(this IMutableModel model);

-        public static RelationalPropertyAnnotations Relational(this IMutableProperty property);

-        public static IRelationalPropertyAnnotations Relational(this IProperty property);

-    }
-    public static class RelationalModelBuilderExtensions {
 {
-        public static DbFunctionBuilder HasDbFunction(this ModelBuilder modelBuilder, MethodInfo methodInfo);

-        public static ModelBuilder HasDbFunction(this ModelBuilder modelBuilder, MethodInfo methodInfo, Action<DbFunctionBuilder> builderAction);

-        public static DbFunctionBuilder HasDbFunction<TResult>(this ModelBuilder modelBuilder, Expression<Func<TResult>> expression);

-        public static ModelBuilder HasDefaultSchema(this ModelBuilder modelBuilder, string schema);

-        public static ModelBuilder HasSequence(this ModelBuilder modelBuilder, string name, Action<SequenceBuilder> builderAction);

-        public static SequenceBuilder HasSequence(this ModelBuilder modelBuilder, string name, string schema = null);

-        public static ModelBuilder HasSequence(this ModelBuilder modelBuilder, string name, string schema, Action<SequenceBuilder> builderAction);

-        public static ModelBuilder HasSequence(this ModelBuilder modelBuilder, Type clrType, string name, Action<SequenceBuilder> builderAction);

-        public static SequenceBuilder HasSequence(this ModelBuilder modelBuilder, Type clrType, string name, string schema = null);

-        public static ModelBuilder HasSequence(this ModelBuilder modelBuilder, Type clrType, string name, string schema, Action<SequenceBuilder> builderAction);

-        public static ModelBuilder HasSequence<T>(this ModelBuilder modelBuilder, string name, Action<SequenceBuilder> builderAction);

-        public static SequenceBuilder HasSequence<T>(this ModelBuilder modelBuilder, string name, string schema = null);

-        public static ModelBuilder HasSequence<T>(this ModelBuilder modelBuilder, string name, string schema, Action<SequenceBuilder> builderAction);

-    }
-    public static class RelationalPropertyBuilderExtensions {
 {
-        public static PropertyBuilder HasColumnName(this PropertyBuilder propertyBuilder, string name);

-        public static PropertyBuilder<TProperty> HasColumnName<TProperty>(this PropertyBuilder<TProperty> propertyBuilder, string name);

-        public static PropertyBuilder HasColumnType(this PropertyBuilder propertyBuilder, string typeName);

-        public static PropertyBuilder<TProperty> HasColumnType<TProperty>(this PropertyBuilder<TProperty> propertyBuilder, string typeName);

-        public static PropertyBuilder HasComputedColumnSql(this PropertyBuilder propertyBuilder, string sql);

-        public static PropertyBuilder<TProperty> HasComputedColumnSql<TProperty>(this PropertyBuilder<TProperty> propertyBuilder, string sql);

-        public static PropertyBuilder HasDefaultValue(this PropertyBuilder propertyBuilder, object value = null);

-        public static PropertyBuilder<TProperty> HasDefaultValue<TProperty>(this PropertyBuilder<TProperty> propertyBuilder, object value = null);

-        public static PropertyBuilder HasDefaultValueSql(this PropertyBuilder propertyBuilder, string sql);

-        public static PropertyBuilder<TProperty> HasDefaultValueSql<TProperty>(this PropertyBuilder<TProperty> propertyBuilder, string sql);

-        public static PropertyBuilder IsFixedLength(this PropertyBuilder propertyBuilder, bool fixedLength = true);

-        public static PropertyBuilder<TProperty> IsFixedLength<TProperty>(this PropertyBuilder<TProperty> propertyBuilder, bool fixedLength = true);

-    }
-    public static class RelationalQueryableExtensions {
 {
-        public static IQueryable<TEntity> FromSql<TEntity>(this IQueryable<TEntity> source, RawSqlString sql, params object[] parameters) where TEntity : class;

-        public static IQueryable<TEntity> FromSql<TEntity>(this IQueryable<TEntity> source, FormattableString sql) where TEntity : class;

-    }
-    public static class RelationalQueryTypeBuilderExtensions {
 {
-        public static DiscriminatorBuilder HasDiscriminator(this QueryTypeBuilder queryTypeBuilder);

-        public static DiscriminatorBuilder HasDiscriminator(this QueryTypeBuilder queryTypeBuilder, string name, Type discriminatorType);

-        public static DiscriminatorBuilder<TDiscriminator> HasDiscriminator<TDiscriminator>(this QueryTypeBuilder queryTypeBuilder, string name);

-        public static DiscriminatorBuilder<TDiscriminator> HasDiscriminator<TQuery, TDiscriminator>(this QueryTypeBuilder<TQuery> queryTypeBuilder, Expression<Func<TQuery, TDiscriminator>> propertyExpression) where TQuery : class;

-        public static QueryTypeBuilder ToView(this QueryTypeBuilder queryTypeBuilder, string name);

-        public static QueryTypeBuilder ToView(this QueryTypeBuilder queryTypeBuilder, string name, string schema);

-        public static QueryTypeBuilder<TQuery> ToView<TQuery>(this QueryTypeBuilder<TQuery> queryTypeBuilder, string name) where TQuery : class;

-        public static QueryTypeBuilder<TQuery> ToView<TQuery>(this QueryTypeBuilder<TQuery> queryTypeBuilder, string name, string schema) where TQuery : class;

-    }
-    public static class RelationalReferenceCollectionBuilderExtensions {
 {
-        public static ReferenceCollectionBuilder HasConstraintName(this ReferenceCollectionBuilder referenceCollectionBuilder, string name);

-        public static ReferenceCollectionBuilder<TEntity, TRelatedEntity> HasConstraintName<TEntity, TRelatedEntity>(this ReferenceCollectionBuilder<TEntity, TRelatedEntity> referenceCollectionBuilder, string name) where TEntity : class where TRelatedEntity : class;

-    }
-    public static class RelationalReferenceOwnershipBuilderExtensions {
 {
-        public static ReferenceOwnershipBuilder HasConstraintName(this ReferenceOwnershipBuilder referenceReferenceBuilder, string name);

-        public static ReferenceOwnershipBuilder<TEntity, TRelatedEntity> HasConstraintName<TEntity, TRelatedEntity>(this ReferenceOwnershipBuilder<TEntity, TRelatedEntity> referenceReferenceBuilder, string name) where TEntity : class where TRelatedEntity : class;

-        public static ReferenceOwnershipBuilder ToTable(this ReferenceOwnershipBuilder referenceOwnershipBuilder, string name);

-        public static ReferenceOwnershipBuilder ToTable(this ReferenceOwnershipBuilder referenceOwnershipBuilder, string name, string schema);

-        public static ReferenceOwnershipBuilder<TEntity, TRelatedEntity> ToTable<TEntity, TRelatedEntity>(this ReferenceOwnershipBuilder<TEntity, TRelatedEntity> referenceOwnershipBuilder, string name) where TEntity : class where TRelatedEntity : class;

-        public static ReferenceOwnershipBuilder<TEntity, TRelatedEntity> ToTable<TEntity, TRelatedEntity>(this ReferenceOwnershipBuilder<TEntity, TRelatedEntity> referenceOwnershipBuilder, string name, string schema) where TEntity : class where TRelatedEntity : class;

-    }
-    public static class RelationalReferenceReferenceBuilderExtensions {
 {
-        public static ReferenceReferenceBuilder HasConstraintName(this ReferenceReferenceBuilder referenceReferenceBuilder, string name);

-        public static ReferenceReferenceBuilder<TEntity, TRelatedEntity> HasConstraintName<TEntity, TRelatedEntity>(this ReferenceReferenceBuilder<TEntity, TRelatedEntity> referenceReferenceBuilder, string name) where TEntity : class where TRelatedEntity : class;

-    }
-    public static class SqlServerCollectionOwnershipBuilderExtensions {
 {
-        public static CollectionOwnershipBuilder ForSqlServerIsMemoryOptimized(this CollectionOwnershipBuilder collectionOwnershipBuilder, bool memoryOptimized = true);

-        public static CollectionOwnershipBuilder<TEntity, TRelatedEntity> ForSqlServerIsMemoryOptimized<TEntity, TRelatedEntity>(this CollectionOwnershipBuilder<TEntity, TRelatedEntity> collectionOwnershipBuilder, bool memoryOptimized = true) where TEntity : class where TRelatedEntity : class;

-    }
-    public static class SqlServerDatabaseFacadeExtensions {
 {
-        public static bool IsSqlServer(this DatabaseFacade database);

-    }
-    public static class SqlServerDbContextOptionsExtensions {
 {
-        public static DbContextOptionsBuilder UseSqlServer(this DbContextOptionsBuilder optionsBuilder, DbConnection connection, Action<SqlServerDbContextOptionsBuilder> sqlServerOptionsAction = null);

-        public static DbContextOptionsBuilder UseSqlServer(this DbContextOptionsBuilder optionsBuilder, string connectionString, Action<SqlServerDbContextOptionsBuilder> sqlServerOptionsAction = null);

-        public static DbContextOptionsBuilder<TContext> UseSqlServer<TContext>(this DbContextOptionsBuilder<TContext> optionsBuilder, DbConnection connection, Action<SqlServerDbContextOptionsBuilder> sqlServerOptionsAction = null) where TContext : DbContext;

-        public static DbContextOptionsBuilder<TContext> UseSqlServer<TContext>(this DbContextOptionsBuilder<TContext> optionsBuilder, string connectionString, Action<SqlServerDbContextOptionsBuilder> sqlServerOptionsAction = null) where TContext : DbContext;

-    }
-    public static class SqlServerDbFunctionsExtensions {
 {
-        public static bool Contains(this DbFunctions _, string propertyReference, string searchCondition);

-        public static bool Contains(this DbFunctions _, string propertyReference, string searchCondition, int languageTerm);

-        public static int DateDiffDay(this DbFunctions _, DateTime startDate, DateTime endDate);

-        public static int DateDiffDay(this DbFunctions _, DateTimeOffset startDate, DateTimeOffset endDate);

-        public static Nullable<int> DateDiffDay(this DbFunctions _, Nullable<DateTime> startDate, Nullable<DateTime> endDate);

-        public static Nullable<int> DateDiffDay(this DbFunctions _, Nullable<DateTimeOffset> startDate, Nullable<DateTimeOffset> endDate);

-        public static int DateDiffHour(this DbFunctions _, DateTime startDate, DateTime endDate);

-        public static int DateDiffHour(this DbFunctions _, DateTimeOffset startDate, DateTimeOffset endDate);

-        public static Nullable<int> DateDiffHour(this DbFunctions _, Nullable<DateTime> startDate, Nullable<DateTime> endDate);

-        public static Nullable<int> DateDiffHour(this DbFunctions _, Nullable<DateTimeOffset> startDate, Nullable<DateTimeOffset> endDate);

-        public static int DateDiffMicrosecond(this DbFunctions _, DateTime startDate, DateTime endDate);

-        public static int DateDiffMicrosecond(this DbFunctions _, DateTimeOffset startDate, DateTimeOffset endDate);

-        public static Nullable<int> DateDiffMicrosecond(this DbFunctions _, Nullable<DateTime> startDate, Nullable<DateTime> endDate);

-        public static Nullable<int> DateDiffMicrosecond(this DbFunctions _, Nullable<DateTimeOffset> startDate, Nullable<DateTimeOffset> endDate);

-        public static int DateDiffMillisecond(this DbFunctions _, DateTime startDate, DateTime endDate);

-        public static int DateDiffMillisecond(this DbFunctions _, DateTimeOffset startDate, DateTimeOffset endDate);

-        public static Nullable<int> DateDiffMillisecond(this DbFunctions _, Nullable<DateTime> startDate, Nullable<DateTime> endDate);

-        public static Nullable<int> DateDiffMillisecond(this DbFunctions _, Nullable<DateTimeOffset> startDate, Nullable<DateTimeOffset> endDate);

-        public static int DateDiffMinute(this DbFunctions _, DateTime startDate, DateTime endDate);

-        public static int DateDiffMinute(this DbFunctions _, DateTimeOffset startDate, DateTimeOffset endDate);

-        public static Nullable<int> DateDiffMinute(this DbFunctions _, Nullable<DateTime> startDate, Nullable<DateTime> endDate);

-        public static Nullable<int> DateDiffMinute(this DbFunctions _, Nullable<DateTimeOffset> startDate, Nullable<DateTimeOffset> endDate);

-        public static int DateDiffMonth(this DbFunctions _, DateTime startDate, DateTime endDate);

-        public static int DateDiffMonth(this DbFunctions _, DateTimeOffset startDate, DateTimeOffset endDate);

-        public static Nullable<int> DateDiffMonth(this DbFunctions _, Nullable<DateTime> startDate, Nullable<DateTime> endDate);

-        public static Nullable<int> DateDiffMonth(this DbFunctions _, Nullable<DateTimeOffset> startDate, Nullable<DateTimeOffset> endDate);

-        public static int DateDiffNanosecond(this DbFunctions _, DateTime startDate, DateTime endDate);

-        public static int DateDiffNanosecond(this DbFunctions _, DateTimeOffset startDate, DateTimeOffset endDate);

-        public static Nullable<int> DateDiffNanosecond(this DbFunctions _, Nullable<DateTime> startDate, Nullable<DateTime> endDate);

-        public static Nullable<int> DateDiffNanosecond(this DbFunctions _, Nullable<DateTimeOffset> startDate, Nullable<DateTimeOffset> endDate);

-        public static int DateDiffSecond(this DbFunctions _, DateTime startDate, DateTime endDate);

-        public static int DateDiffSecond(this DbFunctions _, DateTimeOffset startDate, DateTimeOffset endDate);

-        public static Nullable<int> DateDiffSecond(this DbFunctions _, Nullable<DateTime> startDate, Nullable<DateTime> endDate);

-        public static Nullable<int> DateDiffSecond(this DbFunctions _, Nullable<DateTimeOffset> startDate, Nullable<DateTimeOffset> endDate);

-        public static int DateDiffYear(this DbFunctions _, DateTime startDate, DateTime endDate);

-        public static int DateDiffYear(this DbFunctions _, DateTimeOffset startDate, DateTimeOffset endDate);

-        public static Nullable<int> DateDiffYear(this DbFunctions _, Nullable<DateTime> startDate, Nullable<DateTime> endDate);

-        public static Nullable<int> DateDiffYear(this DbFunctions _, Nullable<DateTimeOffset> startDate, Nullable<DateTimeOffset> endDate);

-        public static bool FreeText(this DbFunctions _, string propertyReference, string freeText);

-        public static bool FreeText(this DbFunctions _, string propertyReference, string freeText, int languageTerm);

-    }
-    public static class SqlServerEntityTypeBuilderExtensions {
 {
-        public static IndexBuilder<TEntity> ForSqlServerHasIndex<TEntity>(this EntityTypeBuilder<TEntity> entityTypeBuilder, Expression<Func<TEntity, object>> indexExpression) where TEntity : class;

-        public static EntityTypeBuilder ForSqlServerIsMemoryOptimized(this EntityTypeBuilder entityTypeBuilder, bool memoryOptimized = true);

-        public static EntityTypeBuilder<TEntity> ForSqlServerIsMemoryOptimized<TEntity>(this EntityTypeBuilder<TEntity> entityTypeBuilder, bool memoryOptimized = true) where TEntity : class;

-    }
-    public static class SqlServerIndexBuilderExtensions {
 {
-        public static IndexBuilder ForSqlServerInclude(this IndexBuilder indexBuilder, params string[] propertyNames);

-        public static IndexBuilder<TEntity> ForSqlServerInclude<TEntity>(this IndexBuilder<TEntity> indexBuilder, Expression<Func<TEntity, object>> includeExpression);

-        public static IndexBuilder ForSqlServerIsClustered(this IndexBuilder indexBuilder, bool clustered = true);

-        public static IndexBuilder<TEntity> ForSqlServerIsClustered<TEntity>(this IndexBuilder<TEntity> indexBuilder, bool clustered = true);

-    }
-    public static class SqlServerKeyBuilderExtensions {
 {
-        public static KeyBuilder ForSqlServerIsClustered(this KeyBuilder keyBuilder, bool clustered = true);

-    }
-    public static class SqlServerMetadataExtensions {
 {
-        public static ISqlServerEntityTypeAnnotations SqlServer(this IEntityType entityType);

-        public static ISqlServerIndexAnnotations SqlServer(this IIndex index);

-        public static ISqlServerKeyAnnotations SqlServer(this IKey key);

-        public static ISqlServerModelAnnotations SqlServer(this IModel model);

-        public static SqlServerEntityTypeAnnotations SqlServer(this IMutableEntityType entityType);

-        public static SqlServerIndexAnnotations SqlServer(this IMutableIndex index);

-        public static SqlServerKeyAnnotations SqlServer(this IMutableKey key);

-        public static SqlServerModelAnnotations SqlServer(this IMutableModel model);

-        public static SqlServerPropertyAnnotations SqlServer(this IMutableProperty property);

-        public static ISqlServerPropertyAnnotations SqlServer(this IProperty property);

-    }
-    public static class SqlServerModelBuilderExtensions {
 {
-        public static ModelBuilder ForSqlServerUseIdentityColumns(this ModelBuilder modelBuilder);

-        public static ModelBuilder ForSqlServerUseSequenceHiLo(this ModelBuilder modelBuilder, string name = null, string schema = null);

-    }
-    public static class SqlServerPropertyBuilderExtensions {
 {
-        public static PropertyBuilder ForSqlServerUseSequenceHiLo(this PropertyBuilder propertyBuilder, string name = null, string schema = null);

-        public static PropertyBuilder<TProperty> ForSqlServerUseSequenceHiLo<TProperty>(this PropertyBuilder<TProperty> propertyBuilder, string name = null, string schema = null);

-        public static PropertyBuilder UseSqlServerIdentityColumn(this PropertyBuilder propertyBuilder);

-        public static PropertyBuilder<TProperty> UseSqlServerIdentityColumn<TProperty>(this PropertyBuilder<TProperty> propertyBuilder);

-    }
-    public static class SqlServerReferenceOwnershipBuilderExtensions {
 {
-        public static ReferenceOwnershipBuilder ForSqlServerIsMemoryOptimized(this ReferenceOwnershipBuilder referenceOwnershipBuilder, bool memoryOptimized = true);

-        public static ReferenceOwnershipBuilder<TEntity, TRelatedEntity> ForSqlServerIsMemoryOptimized<TEntity, TRelatedEntity>(this ReferenceOwnershipBuilder<TEntity, TRelatedEntity> referenceOwnershipBuilder, bool memoryOptimized = true) where TEntity : class where TRelatedEntity : class;

-    }
-    public class SqlServerRetryingExecutionStrategy : ExecutionStrategy {
 {
-        public SqlServerRetryingExecutionStrategy(DbContext context);

-        public SqlServerRetryingExecutionStrategy(DbContext context, int maxRetryCount);

-        public SqlServerRetryingExecutionStrategy(DbContext context, int maxRetryCount, TimeSpan maxRetryDelay, ICollection<int> errorNumbersToAdd);

-        public SqlServerRetryingExecutionStrategy(ExecutionStrategyDependencies dependencies);

-        public SqlServerRetryingExecutionStrategy(ExecutionStrategyDependencies dependencies, int maxRetryCount);

-        public SqlServerRetryingExecutionStrategy(ExecutionStrategyDependencies dependencies, int maxRetryCount, TimeSpan maxRetryDelay, ICollection<int> errorNumbersToAdd);

-        protected override Nullable<TimeSpan> GetNextDelay(Exception lastException);

-        protected override bool ShouldRetryOn(Exception exception);

-    }
-    public static class TypeBaseExtensions {
 {
-        public static Nullable<PropertyAccessMode> GetNavigationAccessMode(this ITypeBase typeBase);

-        public static Nullable<PropertyAccessMode> GetPropertyAccessMode(this ITypeBase typeBase);

-    }
-    public enum WarningBehavior {
 {
-        Ignore = 1,

-        Log = 0,

-        Throw = 2,

-    }
-}
```

