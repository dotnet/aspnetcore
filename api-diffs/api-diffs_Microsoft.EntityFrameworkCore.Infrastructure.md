# Microsoft.EntityFrameworkCore.Infrastructure

``` diff
-namespace Microsoft.EntityFrameworkCore.Infrastructure {
 {
-    public static class AccessorExtensions {
 {
-        public static T GetInfrastructure<T>(this IInfrastructure<T> accessor);

-        public static TService GetService<TService>(this IInfrastructure<IServiceProvider> accessor);

-    }
-    public class Annotatable : IAnnotatable, IMutableAnnotatable {
 {
-        public Annotatable();

-        public virtual object this[string name] { get; set; }

-        protected virtual Annotation AddAnnotation(string name, Annotation annotation);

-        public virtual Annotation AddAnnotation(string name, object value);

-        protected virtual Annotation CreateAnnotation(string name, object value);

-        public virtual Annotation FindAnnotation(string name);

-        public virtual IEnumerable<Annotation> GetAnnotations();

-        public virtual Annotation GetOrAddAnnotation(string name, object value);

-        IAnnotation Microsoft.EntityFrameworkCore.Infrastructure.IAnnotatable.FindAnnotation(string name);

-        IEnumerable<IAnnotation> Microsoft.EntityFrameworkCore.Infrastructure.IAnnotatable.GetAnnotations();

-        protected virtual Annotation OnAnnotationSet(string name, Annotation annotation, Annotation oldAnnotation);

-        public virtual Annotation RemoveAnnotation(string name);

-        protected virtual Annotation SetAnnotation(string name, Annotation annotation);

-        public virtual void SetAnnotation(string name, object value);

-    }
-    public static class AnnotatableExtensions {
 {
-        public static IAnnotation GetAnnotation(this IAnnotatable annotatable, string annotationName);

-    }
-    public class Annotation : IAnnotation {
 {
-        public Annotation(string name, object value);

-        public virtual string Name { get; }

-        public virtual object Value { get; }

-    }
-    public class CoreOptionsExtension : IDbContextOptionsExtension, IDbContextOptionsExtensionWithDebugInfo {
 {
-        public CoreOptionsExtension();

-        protected CoreOptionsExtension(CoreOptionsExtension copyFrom);

-        public virtual IServiceProvider ApplicationServiceProvider { get; }

-        public virtual bool DetailedErrorsEnabled { get; }

-        public virtual IServiceProvider InternalServiceProvider { get; }

-        public virtual bool IsSensitiveDataLoggingEnabled { get; }

-        public virtual string LogFragment { get; }

-        public virtual ILoggerFactory LoggerFactory { get; }

-        public virtual Nullable<int> MaxPoolSize { get; }

-        public virtual IMemoryCache MemoryCache { get; }

-        public virtual IModel Model { get; }

-        public virtual QueryTrackingBehavior QueryTrackingBehavior { get; }

-        public virtual IReadOnlyDictionary<Type, Type> ReplacedServices { get; }

-        public virtual WarningsConfiguration WarningsConfiguration { get; }

-        public virtual bool ApplyServices(IServiceCollection services);

-        protected virtual CoreOptionsExtension Clone();

-        public virtual long GetServiceProviderHashCode();

-        public virtual void PopulateDebugInfo(IDictionary<string, string> debugInfo);

-        public virtual void Validate(IDbContextOptions options);

-        public virtual CoreOptionsExtension WithApplicationServiceProvider(IServiceProvider applicationServiceProvider);

-        public virtual CoreOptionsExtension WithDetailedErrorsEnabled(bool detailedErrorsEnabled);

-        public virtual CoreOptionsExtension WithInternalServiceProvider(IServiceProvider internalServiceProvider);

-        public virtual CoreOptionsExtension WithLoggerFactory(ILoggerFactory loggerFactory);

-        public virtual CoreOptionsExtension WithMaxPoolSize(Nullable<int> maxPoolSize);

-        public virtual CoreOptionsExtension WithMemoryCache(IMemoryCache memoryCache);

-        public virtual CoreOptionsExtension WithModel(IModel model);

-        public virtual CoreOptionsExtension WithQueryTrackingBehavior(QueryTrackingBehavior queryTrackingBehavior);

-        public virtual CoreOptionsExtension WithReplacedService(Type serviceType, Type implementationType);

-        public virtual CoreOptionsExtension WithSensitiveDataLoggingEnabled(bool sensitiveDataLoggingEnabled);

-        public virtual CoreOptionsExtension WithWarningsConfiguration(WarningsConfiguration warningsConfiguration);

-    }
-    public class DatabaseFacade : IInfrastructure<IServiceProvider> {
 {
-        public DatabaseFacade(DbContext context);

-        public virtual bool AutoTransactionsEnabled { get; set; }

-        public virtual IDbContextTransaction CurrentTransaction { get; }

-        public virtual string ProviderName { get; }

-        public virtual IDbContextTransaction BeginTransaction();

-        public virtual Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default(CancellationToken));

-        public virtual bool CanConnect();

-        public virtual Task<bool> CanConnectAsync(CancellationToken cancellationToken = default(CancellationToken));

-        public virtual void CommitTransaction();

-        public virtual IExecutionStrategy CreateExecutionStrategy();

-        public virtual bool EnsureCreated();

-        public virtual Task<bool> EnsureCreatedAsync(CancellationToken cancellationToken = default(CancellationToken));

-        public virtual bool EnsureDeleted();

-        public virtual Task<bool> EnsureDeletedAsync(CancellationToken cancellationToken = default(CancellationToken));

-        public override bool Equals(object obj);

-        public override int GetHashCode();

-        public virtual void RollbackTransaction();

-        public override string ToString();

-    }
-    public sealed class DbContextAttribute : Attribute {
 {
-        public DbContextAttribute(Type contextType);

-        public Type ContextType { get; }

-    }
-    public class DbContextFactoryOptions {
 {
-        public DbContextFactoryOptions();

-        public virtual string ApplicationBasePath { get; set; }

-        public virtual string ContentRootPath { get; set; }

-        public virtual string EnvironmentName { get; set; }

-    }
-    public class EntityFrameworkRelationalServicesBuilder : EntityFrameworkServicesBuilder {
 {
-        public static readonly IDictionary<Type, EntityFrameworkServicesBuilder.ServiceCharacteristics> RelationalServices;

-        public EntityFrameworkRelationalServicesBuilder(IServiceCollection serviceCollection);

-        protected override EntityFrameworkServicesBuilder.ServiceCharacteristics GetServiceCharacteristics(Type serviceType);

-        public override EntityFrameworkServicesBuilder TryAddCoreServices();

-    }
-    public static class EntityFrameworkServiceCollectionExtensions {
 {
-        public static IServiceCollection AddEntityFramework(this IServiceCollection serviceCollection);

-    }
-    public class EntityFrameworkServicesBuilder {
 {
-        public static readonly IDictionary<Type, EntityFrameworkServicesBuilder.ServiceCharacteristics> CoreServices;

-        public EntityFrameworkServicesBuilder(IServiceCollection serviceCollection);

-        protected virtual ServiceCollectionMap ServiceCollectionMap { get; }

-        protected virtual EntityFrameworkServicesBuilder.ServiceCharacteristics GetServiceCharacteristics(Type serviceType);

-        public virtual EntityFrameworkServicesBuilder TryAdd(Type serviceType, object implementation);

-        public virtual EntityFrameworkServicesBuilder TryAdd(Type serviceType, Type implementationType);

-        public virtual EntityFrameworkServicesBuilder TryAdd(Type serviceType, Type implementationType, Func<IServiceProvider, object> factory);

-        public virtual EntityFrameworkServicesBuilder TryAdd<TService, TImplementation>() where TService : class where TImplementation : class, TService;

-        public virtual EntityFrameworkServicesBuilder TryAdd<TService, TImplementation>(Func<IServiceProvider, TImplementation> factory) where TService : class where TImplementation : class, TService;

-        public virtual EntityFrameworkServicesBuilder TryAdd<TService>(Func<IServiceProvider, TService> factory) where TService : class;

-        public virtual EntityFrameworkServicesBuilder TryAdd<TService>(TService implementation) where TService : class;

-        public virtual EntityFrameworkServicesBuilder TryAddCoreServices();

-        public virtual EntityFrameworkServicesBuilder TryAddProviderSpecificServices(Action<ServiceCollectionMap> serviceMap);

-        public readonly struct ServiceCharacteristics {
 {
-            public ServiceCharacteristics(ServiceLifetime lifetime, bool multipleRegistrations = false);

-            public ServiceLifetime Lifetime { get; }

-            public bool MultipleRegistrations { get; }

-        }
-    }
-    public interface IAnnotatable {
 {
-        object this[string name] { get; }

-        IAnnotation FindAnnotation(string name);

-        IEnumerable<IAnnotation> GetAnnotations();

-    }
-    public interface IAnnotation {
 {
-        string Name { get; }

-        object Value { get; }

-    }
-    public interface ICoreSingletonOptions : ISingletonOptions {
 {
-        bool AreDetailedErrorsEnabled { get; }

-    }
-    public interface IDbContextFactory<out TContext> where TContext : DbContext {
 {
-        TContext Create(DbContextFactoryOptions options);

-    }
-    public interface IDbContextOptions {
 {
-        IEnumerable<IDbContextOptionsExtension> Extensions { get; }

-        TExtension FindExtension<TExtension>() where TExtension : class, IDbContextOptionsExtension;

-    }
-    public interface IDbContextOptionsBuilderInfrastructure {
 {
-        void AddOrUpdateExtension<TExtension>(TExtension extension) where TExtension : class, IDbContextOptionsExtension;

-    }
-    public interface IDbContextOptionsExtension {
 {
-        string LogFragment { get; }

-        bool ApplyServices(IServiceCollection services);

-        long GetServiceProviderHashCode();

-        void Validate(IDbContextOptions options);

-    }
-    public interface IDbContextOptionsExtensionWithDebugInfo : IDbContextOptionsExtension {
 {
-        void PopulateDebugInfo(IDictionary<string, string> debugInfo);

-    }
-    public interface IInfrastructure<out T> {
 {
-        T Instance { get; }

-    }
-    public interface ILazyLoader {
 {
-        void Load(object entity, string navigationName = null);

-        Task LoadAsync(object entity, CancellationToken cancellationToken = default(CancellationToken), string navigationName = null);

-    }
-    public interface IModelCacheKeyFactory {
 {
-        object Create(DbContext context);

-    }
-    public interface IModelCustomizer {
 {
-        void Customize(ModelBuilder modelBuilder, DbContext context);

-    }
-    public interface IModelSource {
 {
-        IModel GetModel(DbContext context, IConventionSetBuilder conventionSetBuilder, IModelValidator validator);

-    }
-    public interface IModelValidator {
 {
-        void Validate(IModel model);

-    }
-    public class InMemoryDbContextOptionsBuilder {
 {
-        public InMemoryDbContextOptionsBuilder(DbContextOptionsBuilder optionsBuilder);

-        protected virtual DbContextOptionsBuilder OptionsBuilder { get; }

-        public override bool Equals(object obj);

-        public override int GetHashCode();

-        public override string ToString();

-    }
-    public interface IRelationalDbContextOptionsBuilderInfrastructure {
 {
-        DbContextOptionsBuilder OptionsBuilder { get; }

-    }
-    public interface IResettableService {
 {
-        void ResetState();

-    }
-    public interface ISingletonOptions {
 {
-        void Initialize(IDbContextOptions options);

-        void Validate(IDbContextOptions options);

-    }
-    public static class LazyLoaderExtensions {
 {
-        public static TRelated Load<TRelated>(this ILazyLoader loader, object entity, ref TRelated navigationField, string navigationName = null) where TRelated : class;

-    }
-    public class ModelCacheKey {
 {
-        public ModelCacheKey(DbContext context);

-        protected virtual bool Equals(ModelCacheKey other);

-        public override bool Equals(object obj);

-        public override int GetHashCode();

-    }
-    public class ModelCacheKeyFactory : IModelCacheKeyFactory {
 {
-        public ModelCacheKeyFactory(ModelCacheKeyFactoryDependencies dependencies);

-        public virtual object Create(DbContext context);

-    }
-    public sealed class ModelCacheKeyFactoryDependencies {
 {
-        public ModelCacheKeyFactoryDependencies();

-    }
-    public class ModelCustomizer : IModelCustomizer {
 {
-        public ModelCustomizer(ModelCustomizerDependencies dependencies);

-        protected virtual ModelCustomizerDependencies Dependencies { get; }

-        public virtual void Customize(ModelBuilder modelBuilder, DbContext context);

-        protected virtual void FindSets(ModelBuilder modelBuilder, DbContext context);

-    }
-    public sealed class ModelCustomizerDependencies {
 {
-        public ModelCustomizerDependencies(IDbSetFinder setFinder);

-        public IDbSetFinder SetFinder { get; }

-        public ModelCustomizerDependencies With(IDbSetFinder setFinder);

-    }
-    public abstract class ModelSnapshot {
 {
-        protected ModelSnapshot();

-        public virtual IModel Model { get; }

-        protected abstract void BuildModel(ModelBuilder modelBuilder);

-    }
-    public class ModelSource : IModelSource {
 {
-        public ModelSource(ModelSourceDependencies dependencies);

-        protected virtual ModelSourceDependencies Dependencies { get; }

-        protected virtual ConventionSet CreateConventionSet(IConventionSetBuilder conventionSetBuilder);

-        protected virtual IModel CreateModel(DbContext context, IConventionSetBuilder conventionSetBuilder, IModelValidator validator);

-        public virtual IModel GetModel(DbContext context, IConventionSetBuilder conventionSetBuilder, IModelValidator validator);

-    }
-    public sealed class ModelSourceDependencies {
 {
-        public ModelSourceDependencies(ICoreConventionSetBuilder coreConventionSetBuilder, IModelCustomizer modelCustomizer, IModelCacheKeyFactory modelCacheKeyFactory);

-        public ICoreConventionSetBuilder CoreConventionSetBuilder { get; }

-        public IModelCacheKeyFactory ModelCacheKeyFactory { get; }

-        public IModelCustomizer ModelCustomizer { get; }

-        public ModelSourceDependencies With(IModelCacheKeyFactory modelCacheKeyFactory);

-        public ModelSourceDependencies With(IModelCustomizer modelCustomizer);

-        public ModelSourceDependencies With(ICoreConventionSetBuilder coreConventionSetBuilder);

-    }
-    public class ModelValidator : IModelValidator {
 {
-        public ModelValidator(ModelValidatorDependencies dependencies);

-        protected virtual ModelValidatorDependencies Dependencies { get; }

-        protected virtual void LogShadowProperties(IModel model);

-        public virtual void Validate(IModel model);

-        protected virtual void ValidateChangeTrackingStrategy(IModel model);

-        protected virtual void ValidateClrInheritance(IModel model);

-        protected virtual void ValidateClrInheritance(IModel model, IEntityType entityType, HashSet<IEntityType> validEntityTypes);

-        protected virtual void ValidateData(IModel model);

-        protected virtual void ValidateDefiningNavigations(IModel model);

-        protected virtual void ValidateFieldMapping(IModel model);

-        protected virtual void ValidateForeignKeys(IModel model);

-        protected virtual void ValidateNoCycles(IModel model);

-        protected virtual void ValidateNoMutableKeys(IModel model);

-        protected virtual void ValidateNonNullPrimaryKeys(IModel model);

-        protected virtual void ValidateNoShadowEntities(IModel model);

-        protected virtual void ValidateNoShadowKeys(IModel model);

-        protected virtual void ValidateOwnership(IModel model);

-        protected virtual void ValidateQueryFilters(IModel model);

-        protected virtual void ValidateQueryTypes(IModel model);

-    }
-    public sealed class ModelValidatorDependencies {
 {
-        public ModelValidatorDependencies(IDiagnosticsLogger<DbLoggerCategory.Model.Validation> logger, IDiagnosticsLogger<DbLoggerCategory.Model> modelLogger);

-        public IDiagnosticsLogger<DbLoggerCategory.Model.Validation> Logger { get; }

-        public IDiagnosticsLogger<DbLoggerCategory.Model> ModelLogger { get; }

-        public ModelValidatorDependencies With(IDiagnosticsLogger<DbLoggerCategory.Model> modelLogger);

-        public ModelValidatorDependencies With(IDiagnosticsLogger<DbLoggerCategory.Model.Validation> logger);

-    }
-    public abstract class RelationalDbContextOptionsBuilder<TBuilder, TExtension> : IRelationalDbContextOptionsBuilderInfrastructure where TBuilder : RelationalDbContextOptionsBuilder<TBuilder, TExtension> where TExtension : RelationalOptionsExtension, new() {
 {
-        protected RelationalDbContextOptionsBuilder(DbContextOptionsBuilder optionsBuilder);

-        DbContextOptionsBuilder Microsoft.EntityFrameworkCore.Infrastructure.IRelationalDbContextOptionsBuilderInfrastructure.OptionsBuilder { get; }

-        protected virtual DbContextOptionsBuilder OptionsBuilder { get; }

-        public virtual TBuilder CommandTimeout(Nullable<int> commandTimeout);

-        public override bool Equals(object obj);

-        public virtual TBuilder ExecutionStrategy(Func<ExecutionStrategyDependencies, IExecutionStrategy> getExecutionStrategy);

-        public override int GetHashCode();

-        public virtual TBuilder MaxBatchSize(int maxBatchSize);

-        public virtual TBuilder MigrationsAssembly(string assemblyName);

-        public virtual TBuilder MigrationsHistoryTable(string tableName, string schema = null);

-        public virtual TBuilder MinBatchSize(int minBatchSize);

-        public override string ToString();

-        public virtual TBuilder UseRelationalNulls(bool useRelationalNulls = true);

-        protected virtual TBuilder WithOption(Func<TExtension, TExtension> setAction);

-    }
-    public class RelationalModelCustomizer : ModelCustomizer {
 {
-        public RelationalModelCustomizer(ModelCustomizerDependencies dependencies);

-        public override void Customize(ModelBuilder modelBuilder, DbContext context);

-        protected virtual void FindDbFunctions(ModelBuilder modelBuilder, DbContext context);

-        protected override void FindSets(ModelBuilder modelBuilder, DbContext context);

-    }
-    public class RelationalModelValidator : ModelValidator {
 {
-        public RelationalModelValidator(ModelValidatorDependencies dependencies, RelationalModelValidatorDependencies relationalDependencies);

-        protected virtual RelationalModelValidatorDependencies RelationalDependencies { get; }

-        protected virtual IRelationalTypeMapper TypeMapper { get; }

-        public override void Validate(IModel model);

-        protected virtual void ValidateBoolsWithDefaults(IModel model);

-        protected virtual void ValidateDataTypes(IModel model);

-        protected virtual void ValidateDbFunctions(IModel model);

-        protected virtual void ValidateDefaultValuesOnKeys(IModel model);

-        protected virtual void ValidateInheritanceMapping(IModel model);

-        protected virtual void ValidateSharedColumnsCompatibility(IReadOnlyList<IEntityType> mappedTypes, string tableName);

-        protected virtual void ValidateSharedForeignKeysCompatibility(IReadOnlyList<IEntityType> mappedTypes, string tableName);

-        protected virtual void ValidateSharedIndexesCompatibility(IReadOnlyList<IEntityType> mappedTypes, string tableName);

-        protected virtual void ValidateSharedKeysCompatibility(IReadOnlyList<IEntityType> mappedTypes, string tableName);

-        protected virtual void ValidateSharedTableCompatibility(IModel model);

-        protected virtual void ValidateSharedTableCompatibility(IReadOnlyList<IEntityType> mappedTypes, string tableName);

-    }
-    public sealed class RelationalModelValidatorDependencies {
 {
-        public RelationalModelValidatorDependencies(IRelationalTypeMapper typeMapper, IRelationalTypeMappingSource typeMappingSource);

-        public IRelationalTypeMapper TypeMapper { get; }

-        public IRelationalTypeMappingSource TypeMappingSource { get; }

-        public RelationalModelValidatorDependencies With(IRelationalTypeMapper typeMapper);

-        public RelationalModelValidatorDependencies With(IRelationalTypeMappingSource typeMappingSource);

-    }
-    public abstract class RelationalOptionsExtension : IDbContextOptionsExtension {
 {
-        protected RelationalOptionsExtension();

-        protected RelationalOptionsExtension(RelationalOptionsExtension copyFrom);

-        public virtual Nullable<int> CommandTimeout { get; }

-        public virtual DbConnection Connection { get; }

-        public virtual string ConnectionString { get; }

-        public virtual Func<ExecutionStrategyDependencies, IExecutionStrategy> ExecutionStrategyFactory { get; }

-        public virtual string LogFragment { get; }

-        public virtual Nullable<int> MaxBatchSize { get; }

-        public virtual string MigrationsAssembly { get; }

-        public virtual string MigrationsHistoryTableName { get; }

-        public virtual string MigrationsHistoryTableSchema { get; }

-        public virtual Nullable<int> MinBatchSize { get; }

-        public virtual bool UseRelationalNulls { get; }

-        public abstract bool ApplyServices(IServiceCollection services);

-        protected abstract RelationalOptionsExtension Clone();

-        public static RelationalOptionsExtension Extract(IDbContextOptions options);

-        public virtual long GetServiceProviderHashCode();

-        public virtual void Validate(IDbContextOptions options);

-        public virtual RelationalOptionsExtension WithCommandTimeout(Nullable<int> commandTimeout);

-        public virtual RelationalOptionsExtension WithConnection(DbConnection connection);

-        public virtual RelationalOptionsExtension WithConnectionString(string connectionString);

-        public virtual RelationalOptionsExtension WithExecutionStrategyFactory(Func<ExecutionStrategyDependencies, IExecutionStrategy> executionStrategyFactory);

-        public virtual RelationalOptionsExtension WithMaxBatchSize(Nullable<int> maxBatchSize);

-        public virtual RelationalOptionsExtension WithMigrationsAssembly(string migrationsAssembly);

-        public virtual RelationalOptionsExtension WithMigrationsHistoryTableName(string migrationsHistoryTableName);

-        public virtual RelationalOptionsExtension WithMigrationsHistoryTableSchema(string migrationsHistoryTableSchema);

-        public virtual RelationalOptionsExtension WithMinBatchSize(Nullable<int> minBatchSize);

-        public virtual RelationalOptionsExtension WithUseRelationalNulls(bool useRelationalNulls);

-    }
-    public class ServiceCollectionMap : IInfrastructure<InternalServiceCollectionMap> {
 {
-        public ServiceCollectionMap(IServiceCollection serviceCollection);

-        InternalServiceCollectionMap Microsoft.EntityFrameworkCore.Infrastructure.IInfrastructure<Microsoft.EntityFrameworkCore.Internal.InternalServiceCollectionMap>.Instance { get; }

-        public virtual IServiceCollection ServiceCollection { get; }

-        public virtual ServiceCollectionMap TryAdd(Type serviceType, Func<IServiceProvider, object> factory, ServiceLifetime lifetime);

-        public virtual ServiceCollectionMap TryAdd(Type serviceType, Type implementationType, ServiceLifetime lifetime);

-        public virtual ServiceCollectionMap TryAddEnumerable(Type serviceType, Type implementationType, ServiceLifetime lifetime);

-        public virtual ServiceCollectionMap TryAddEnumerable(Type serviceType, Type implementationType, Func<IServiceProvider, object> factory, ServiceLifetime lifetime);

-        public virtual ServiceCollectionMap TryAddScoped(Type serviceType, Func<IServiceProvider, object> factory);

-        public virtual ServiceCollectionMap TryAddScoped(Type serviceType, Type implementationType);

-        public virtual ServiceCollectionMap TryAddScoped<TService, TImplementation>() where TService : class where TImplementation : class, TService;

-        public virtual ServiceCollectionMap TryAddScoped<TService, TImplementation>(Func<IServiceProvider, TImplementation> factory) where TService : class where TImplementation : class, TService;

-        public virtual ServiceCollectionMap TryAddScoped<TService>(Func<IServiceProvider, TService> factory) where TService : class;

-        public virtual ServiceCollectionMap TryAddScopedEnumerable(Type serviceType, Type implementationType);

-        public virtual ServiceCollectionMap TryAddScopedEnumerable<TService, TImplementation>() where TService : class where TImplementation : class, TService;

-        public virtual ServiceCollectionMap TryAddScopedEnumerable<TService, TImplementation>(Func<IServiceProvider, TImplementation> factory) where TService : class where TImplementation : class, TService;

-        public virtual ServiceCollectionMap TryAddSingleton(Type serviceType, Func<IServiceProvider, object> factory);

-        public virtual ServiceCollectionMap TryAddSingleton(Type serviceType, object implementation);

-        public virtual ServiceCollectionMap TryAddSingleton(Type serviceType, Type implementationType);

-        public virtual ServiceCollectionMap TryAddSingleton<TService, TImplementation>() where TService : class where TImplementation : class, TService;

-        public virtual ServiceCollectionMap TryAddSingleton<TService, TImplementation>(Func<IServiceProvider, TImplementation> factory) where TService : class where TImplementation : class, TService;

-        public virtual ServiceCollectionMap TryAddSingleton<TService>(Func<IServiceProvider, TService> factory) where TService : class;

-        public virtual ServiceCollectionMap TryAddSingleton<TService>(TService implementation) where TService : class;

-        public virtual ServiceCollectionMap TryAddSingletonEnumerable(Type serviceType, object implementation);

-        public virtual ServiceCollectionMap TryAddSingletonEnumerable(Type serviceType, Type implementationType);

-        public virtual ServiceCollectionMap TryAddSingletonEnumerable<TService, TImplementation>() where TService : class where TImplementation : class, TService;

-        public virtual ServiceCollectionMap TryAddSingletonEnumerable<TService, TImplementation>(Func<IServiceProvider, TImplementation> factory) where TService : class where TImplementation : class, TService;

-        public virtual ServiceCollectionMap TryAddSingletonEnumerable<TService>(TService implementation) where TService : class;

-        public virtual ServiceCollectionMap TryAddTransient(Type serviceType, Func<IServiceProvider, object> factory);

-        public virtual ServiceCollectionMap TryAddTransient(Type serviceType, Type implementationType);

-        public virtual ServiceCollectionMap TryAddTransient<TService, TImplementation>() where TService : class where TImplementation : class, TService;

-        public virtual ServiceCollectionMap TryAddTransient<TService, TImplementation>(Func<IServiceProvider, TImplementation> factory) where TService : class where TImplementation : class, TService;

-        public virtual ServiceCollectionMap TryAddTransient<TService>(Func<IServiceProvider, TService> factory) where TService : class;

-        public virtual ServiceCollectionMap TryAddTransientEnumerable(Type serviceType, Type implementationType);

-        public virtual ServiceCollectionMap TryAddTransientEnumerable<TService, TImplementation>() where TService : class where TImplementation : class, TService;

-        public virtual ServiceCollectionMap TryAddTransientEnumerable<TService, TImplementation>(Func<IServiceProvider, TImplementation> factory) where TService : class where TImplementation : class, TService;

-    }
-    public class SqlServerDbContextOptionsBuilder : RelationalDbContextOptionsBuilder<SqlServerDbContextOptionsBuilder, SqlServerOptionsExtension> {
 {
-        public SqlServerDbContextOptionsBuilder(DbContextOptionsBuilder optionsBuilder);

-        public virtual SqlServerDbContextOptionsBuilder EnableRetryOnFailure();

-        public virtual SqlServerDbContextOptionsBuilder EnableRetryOnFailure(int maxRetryCount);

-        public virtual SqlServerDbContextOptionsBuilder EnableRetryOnFailure(int maxRetryCount, TimeSpan maxRetryDelay, ICollection<int> errorNumbersToAdd);

-        public virtual void UseRowNumberForPaging(bool useRowNumberForPaging = true);

-    }
-}
```

