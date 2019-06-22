# Microsoft.EntityFrameworkCore.Diagnostics

``` diff
-namespace Microsoft.EntityFrameworkCore.Diagnostics {
 {
-    public class BatchEventData : EventData {
 {
-        public BatchEventData(EventDefinitionBase eventDefinition, Func<EventDefinitionBase, EventData, string> messageGenerator, IEnumerable<IUpdateEntry> entries, int commandCount);

-        public virtual int CommandCount { get; }

-        public virtual IEnumerable<IUpdateEntry> Entries { get; }

-    }
-    public class BinaryExpressionEventData : EventData {
 {
-        public BinaryExpressionEventData(EventDefinitionBase eventDefinition, Func<EventDefinitionBase, EventData, string> messageGenerator, Expression left, Expression right);

-        public virtual Expression Left { get; }

-        public virtual Expression Right { get; }

-    }
-    public class CascadeDeleteEventData : EntityEntryEventData {
 {
-        public CascadeDeleteEventData(EventDefinitionBase eventDefinition, Func<EventDefinitionBase, EventData, string> messageGenerator, EntityEntry entityEntry, EntityEntry parentEntry, EntityState state);

-        public virtual EntityEntry ParentEntityEntry { get; }

-        public virtual EntityState State { get; }

-    }
-    public class CascadeDeleteOrphanEventData : EntityEntryEventData {
 {
-        public CascadeDeleteOrphanEventData(EventDefinitionBase eventDefinition, Func<EventDefinitionBase, EventData, string> messageGenerator, EntityEntry entityEntry, IEntityType parentEntityTypes, EntityState state);

-        public virtual IEntityType ParentEntityType { get; }

-        public virtual EntityState State { get; }

-    }
-    public class CollectionChangedEventData : NavigationEventData {
 {
-        public CollectionChangedEventData(EventDefinitionBase eventDefinition, Func<EventDefinitionBase, EventData, string> messageGenerator, EntityEntry entityEntry, INavigation navigation, IEnumerable<object> added, IEnumerable<object> removed);

-        public virtual IEnumerable<object> Added { get; }

-        public virtual EntityEntry EntityEntry { get; }

-        public virtual IEnumerable<object> Removed { get; }

-    }
-    public class CommandEndEventData : CommandEventData {
 {
-        public CommandEndEventData(EventDefinitionBase eventDefinition, Func<EventDefinitionBase, EventData, string> messageGenerator, DbCommand command, DbCommandMethod executeMethod, Guid commandId, Guid connectionId, bool async, bool logParameterValues, DateTimeOffset startTime, TimeSpan duration);

-        public virtual TimeSpan Duration { get; }

-    }
-    public class CommandErrorEventData : CommandEndEventData, IErrorEventData {
 {
-        public CommandErrorEventData(EventDefinitionBase eventDefinition, Func<EventDefinitionBase, EventData, string> messageGenerator, DbCommand command, DbCommandMethod executeMethod, Guid commandId, Guid connectionId, Exception exception, bool async, bool logParameterValues, DateTimeOffset startTime, TimeSpan duration);

-        public virtual Exception Exception { get; }

-    }
-    public class CommandEventData : EventData {
 {
-        public CommandEventData(EventDefinitionBase eventDefinition, Func<EventDefinitionBase, EventData, string> messageGenerator, DbCommand command, DbCommandMethod executeMethod, Guid commandId, Guid connectionId, bool async, bool logParameterValues, DateTimeOffset startTime);

-        public virtual DbCommand Command { get; }

-        public virtual Guid CommandId { get; }

-        public virtual Guid ConnectionId { get; }

-        public virtual DbCommandMethod ExecuteMethod { get; }

-        public virtual bool IsAsync { get; }

-        public virtual bool LogParameterValues { get; }

-        public virtual DateTimeOffset StartTime { get; }

-    }
-    public class CommandExecutedEventData : CommandEndEventData {
 {
-        public CommandExecutedEventData(EventDefinitionBase eventDefinition, Func<EventDefinitionBase, EventData, string> messageGenerator, DbCommand command, DbCommandMethod executeMethod, Guid commandId, Guid connectionId, object result, bool async, bool logParameterValues, DateTimeOffset startTime, TimeSpan duration);

-        public virtual object Result { get; }

-    }
-    public class ConnectionEndEventData : ConnectionEventData {
 {
-        public ConnectionEndEventData(EventDefinitionBase eventDefinition, Func<EventDefinitionBase, EventData, string> messageGenerator, DbConnection connection, Guid connectionId, bool async, DateTimeOffset startTime, TimeSpan duration);

-        public virtual TimeSpan Duration { get; }

-    }
-    public class ConnectionErrorEventData : ConnectionEndEventData, IErrorEventData {
 {
-        public ConnectionErrorEventData(EventDefinitionBase eventDefinition, Func<EventDefinitionBase, EventData, string> messageGenerator, DbConnection connection, Guid connectionId, Exception exception, bool async, DateTimeOffset startTime, TimeSpan duration);

-        public virtual Exception Exception { get; }

-    }
-    public class ConnectionEventData : EventData {
 {
-        public ConnectionEventData(EventDefinitionBase eventDefinition, Func<EventDefinitionBase, EventData, string> messageGenerator, DbConnection connection, Guid connectionId, bool async, DateTimeOffset startTime);

-        public virtual DbConnection Connection { get; }

-        public virtual Guid ConnectionId { get; }

-        public virtual bool IsAsync { get; }

-        public virtual DateTimeOffset StartTime { get; }

-    }
-    public class ContextInitializedEventData : EventData {
 {
-        public ContextInitializedEventData(EventDefinitionBase eventDefinition, Func<EventDefinitionBase, EventData, string> messageGenerator, DbContext context, DbContextOptions contextOptions);

-        public virtual DbContext Context { get; }

-        public virtual DbContextOptions ContextOptions { get; }

-    }
-    public static class CoreEventId {
 {
-        public static readonly EventId CascadeDelete;

-        public static readonly EventId CascadeDeleteOrphan;

-        public static readonly EventId CollectionChangeDetected;

-        public static readonly EventId ConflictingForeignKeyAttributesOnNavigationAndPropertyWarning;

-        public static readonly EventId ConflictingShadowForeignKeysWarning;

-        public static readonly EventId ContextDisposed;

-        public static readonly EventId ContextInitialized;

-        public static readonly EventId DetachedLazyLoadingWarning;

-        public static readonly EventId DetectChangesCompleted;

-        public static readonly EventId DetectChangesStarting;

-        public static readonly EventId DuplicateDependentEntityTypeInstanceWarning;

-        public static readonly EventId ExecutionStrategyRetrying;

-        public static readonly EventId FirstWithoutOrderByAndFilterWarning;

-        public static readonly EventId ForeignKeyAttributesOnBothNavigationsWarning;

-        public static readonly EventId ForeignKeyAttributesOnBothPropertiesWarning;

-        public static readonly EventId ForeignKeyChangeDetected;

-        public static readonly EventId IncludeIgnoredWarning;

-        public static readonly EventId IncompatibleMatchingForeignKeyProperties;

-        public static readonly EventId LazyLoadOnDisposedContextWarning;

-        public static readonly EventId ManyServiceProvidersCreatedWarning;

-        public static readonly EventId MultipleInversePropertiesSameTargetWarning;

-        public static readonly EventId MultipleNavigationProperties;

-        public static readonly EventId MultiplePrimaryKeyCandidates;

-        public static readonly EventId NavigationIncluded;

-        public static readonly EventId NavigationLazyLoading;

-        public static readonly EventId NonDefiningInverseNavigationWarning;

-        public static readonly EventId NonOwnershipInverseNavigationWarning;

-        public static readonly EventId OptimisticConcurrencyException;

-        public static readonly EventId PossibleUnintendedCollectionNavigationNullComparisonWarning;

-        public static readonly EventId PossibleUnintendedReferenceComparisonWarning;

-        public static readonly EventId PropertyChangeDetected;

-        public static readonly EventId QueryExecutionPlanned;

-        public static readonly EventId QueryIterationFailed;

-        public static readonly EventId QueryModelCompiling;

-        public static readonly EventId QueryModelOptimized;

-        public static readonly EventId RedundantForeignKeyWarning;

-        public static readonly EventId RedundantIndexRemoved;

-        public static readonly EventId ReferenceChangeDetected;

-        public static readonly EventId RequiredAttributeOnBothNavigations;

-        public static readonly EventId RequiredAttributeOnDependent;

-        public static readonly EventId RowLimitingOperationWithoutOrderByWarning;

-        public static readonly EventId SaveChangesCompleted;

-        public static readonly EventId SaveChangesFailed;

-        public static readonly EventId SaveChangesStarting;

-        public static readonly EventId SensitiveDataLoggingEnabledWarning;

-        public static readonly EventId ServiceProviderCreated;

-        public static readonly EventId ServiceProviderDebugInfo;

-        public static readonly EventId ShadowPropertyCreated;

-        public static readonly EventId StartedTracking;

-        public static readonly EventId StateChanged;

-        public static readonly EventId ValueGenerated;

-        public const int CoreBaseId = 10000;

-        public const int ProviderBaseId = 30000;

-        public const int ProviderDesignBaseId = 35000;

-        public const int RelationalBaseId = 20000;

-    }
-    public class DataReaderDisposingEventData : EventData {
 {
-        public DataReaderDisposingEventData(EventDefinitionBase eventDefinition, Func<EventDefinitionBase, EventData, string> messageGenerator, DbCommand command, DbDataReader dataReader, Guid commandId, Guid connectionId, int recordsAffected, int readCount, DateTimeOffset startTime, TimeSpan duration);

-        public virtual DbCommand Command { get; }

-        public virtual Guid CommandId { get; }

-        public virtual Guid ConnectionId { get; }

-        public virtual DbDataReader DataReader { get; }

-        public virtual TimeSpan Duration { get; }

-        public virtual int ReadCount { get; }

-        public virtual int RecordsAffected { get; }

-        public virtual DateTimeOffset StartTime { get; }

-    }
-    public enum DbCommandMethod {
 {
-        ExecuteNonQuery = 0,

-        ExecuteReader = 2,

-        ExecuteScalar = 1,

-    }
-    public class DbContextErrorEventData : DbContextEventData, IErrorEventData {
 {
-        public DbContextErrorEventData(EventDefinitionBase eventDefinition, Func<EventDefinitionBase, EventData, string> messageGenerator, DbContext context, Exception exception);

-        public virtual Exception Exception { get; }

-    }
-    public class DbContextEventData : EventData {
 {
-        public DbContextEventData(EventDefinitionBase eventDefinition, Func<EventDefinitionBase, EventData, string> messageGenerator, DbContext context);

-        public virtual DbContext Context { get; }

-    }
-    public class DbContextTypeErrorEventData : DbContextTypeEventData, IErrorEventData {
 {
-        public DbContextTypeErrorEventData(EventDefinitionBase eventDefinition, Func<EventDefinitionBase, EventData, string> messageGenerator, Type contextType, Exception exception);

-        public virtual Exception Exception { get; }

-    }
-    public class DbContextTypeEventData : EventData {
 {
-        public DbContextTypeEventData(EventDefinitionBase eventDefinition, Func<EventDefinitionBase, EventData, string> messageGenerator, Type contextType);

-        public virtual Type ContextType { get; }

-    }
-    public class EntityEntryEventData : EventData {
 {
-        public EntityEntryEventData(EventDefinitionBase eventDefinition, Func<EventDefinitionBase, EventData, string> messageGenerator, EntityEntry entityEntry);

-        public virtual EntityEntry EntityEntry { get; }

-    }
-    public class EntityTypeSchemaEventData : EventData {
 {
-        public EntityTypeSchemaEventData(EventDefinitionBase eventDefinition, Func<EventDefinitionBase, EventData, string> messageGenerator, IEntityType entityType, string schema);

-        public virtual IEntityType EntityType { get; }

-        public virtual string Schema { get; }

-    }
-    public class EventData {
 {
-        public EventData(EventDefinitionBase eventDefinition, Func<EventDefinitionBase, EventData, string> messageGenerator);

-        public virtual EventId EventId { get; }

-        public virtual LogLevel LogLevel { get; }

-        public override string ToString();

-    }
-    public class EventDefinition : EventDefinitionBase {
 {
-        public EventDefinition(EventId eventId, LogLevel level, Action<ILogger, Exception> logAction);

-        public EventDefinition(EventId eventId, LogLevel level, string eventIdCode, Action<ILogger, Exception> logAction);

-        public virtual string GenerateMessage(Exception exception = null);

-        public virtual void Log<TLoggerCategory>(IDiagnosticsLogger<TLoggerCategory> logger, WarningBehavior warningBehavior, Exception exception = null) where TLoggerCategory : LoggerCategory<TLoggerCategory>, new();

-        public virtual void Log<TLoggerCategory>(IDiagnosticsLogger<TLoggerCategory> logger, Exception exception = null) where TLoggerCategory : LoggerCategory<TLoggerCategory>, new();

-    }
-    public class EventDefinition<TParam> : EventDefinitionBase {
 {
-        public EventDefinition(EventId eventId, LogLevel level, Action<ILogger, TParam, Exception> logAction);

-        public EventDefinition(EventId eventId, LogLevel level, string eventIdCode, Action<ILogger, TParam, Exception> logAction);

-        public virtual string GenerateMessage(TParam arg, Exception exception = null);

-        public virtual void Log<TLoggerCategory>(IDiagnosticsLogger<TLoggerCategory> logger, WarningBehavior warningBehavior, TParam arg, Exception exception = null) where TLoggerCategory : LoggerCategory<TLoggerCategory>, new();

-        public virtual void Log<TLoggerCategory>(IDiagnosticsLogger<TLoggerCategory> logger, TParam arg, Exception exception = null) where TLoggerCategory : LoggerCategory<TLoggerCategory>, new();

-    }
-    public class EventDefinition<TParam1, TParam2> : EventDefinitionBase {
 {
-        public EventDefinition(EventId eventId, LogLevel level, Action<ILogger, TParam1, TParam2, Exception> logAction);

-        public EventDefinition(EventId eventId, LogLevel level, string eventIdCode, Action<ILogger, TParam1, TParam2, Exception> logAction);

-        public virtual string GenerateMessage(TParam1 arg1, TParam2 arg2, Exception exception = null);

-        public virtual void Log<TLoggerCategory>(IDiagnosticsLogger<TLoggerCategory> logger, WarningBehavior warningBehavior, TParam1 arg1, TParam2 arg2, Exception exception = null) where TLoggerCategory : LoggerCategory<TLoggerCategory>, new();

-        public virtual void Log<TLoggerCategory>(IDiagnosticsLogger<TLoggerCategory> logger, TParam1 arg1, TParam2 arg2, Exception exception = null) where TLoggerCategory : LoggerCategory<TLoggerCategory>, new();

-    }
-    public class EventDefinition<TParam1, TParam2, TParam3> : EventDefinitionBase {
 {
-        public EventDefinition(EventId eventId, LogLevel level, Action<ILogger, TParam1, TParam2, TParam3, Exception> logAction);

-        public EventDefinition(EventId eventId, LogLevel level, string eventIdCode, Action<ILogger, TParam1, TParam2, TParam3, Exception> logAction);

-        public virtual string GenerateMessage(TParam1 arg1, TParam2 arg2, TParam3 arg3, Exception exception = null);

-        public virtual void Log<TLoggerCategory>(IDiagnosticsLogger<TLoggerCategory> logger, WarningBehavior warningBehavior, TParam1 arg1, TParam2 arg2, TParam3 arg3, Exception exception = null) where TLoggerCategory : LoggerCategory<TLoggerCategory>, new();

-        public virtual void Log<TLoggerCategory>(IDiagnosticsLogger<TLoggerCategory> logger, TParam1 arg1, TParam2 arg2, TParam3 arg3, Exception exception = null) where TLoggerCategory : LoggerCategory<TLoggerCategory>, new();

-    }
-    public class EventDefinition<TParam1, TParam2, TParam3, TParam4> : EventDefinitionBase {
 {
-        public EventDefinition(EventId eventId, LogLevel level, Action<ILogger, TParam1, TParam2, TParam3, TParam4, Exception> logAction);

-        public EventDefinition(EventId eventId, LogLevel level, string eventIdCode, Action<ILogger, TParam1, TParam2, TParam3, TParam4, Exception> logAction);

-        public virtual string GenerateMessage(TParam1 arg1, TParam2 arg2, TParam3 arg3, TParam4 arg4, Exception exception = null);

-        public virtual void Log<TLoggerCategory>(IDiagnosticsLogger<TLoggerCategory> logger, WarningBehavior warningBehavior, TParam1 arg1, TParam2 arg2, TParam3 arg3, TParam4 arg4, Exception exception = null) where TLoggerCategory : LoggerCategory<TLoggerCategory>, new();

-        public virtual void Log<TLoggerCategory>(IDiagnosticsLogger<TLoggerCategory> logger, TParam1 arg1, TParam2 arg2, TParam3 arg3, TParam4 arg4, Exception exception = null) where TLoggerCategory : LoggerCategory<TLoggerCategory>, new();

-    }
-    public class EventDefinition<TParam1, TParam2, TParam3, TParam4, TParam5> : EventDefinitionBase {
 {
-        public EventDefinition(EventId eventId, LogLevel level, Action<ILogger, TParam1, TParam2, TParam3, TParam4, TParam5, Exception> logAction);

-        public EventDefinition(EventId eventId, LogLevel level, string eventIdCode, Action<ILogger, TParam1, TParam2, TParam3, TParam4, TParam5, Exception> logAction);

-        public virtual string GenerateMessage(TParam1 arg1, TParam2 arg2, TParam3 arg3, TParam4 arg4, TParam5 arg5, Exception exception = null);

-        public virtual void Log<TLoggerCategory>(IDiagnosticsLogger<TLoggerCategory> logger, WarningBehavior warningBehavior, TParam1 arg1, TParam2 arg2, TParam3 arg3, TParam4 arg4, TParam5 arg5, Exception exception = null) where TLoggerCategory : LoggerCategory<TLoggerCategory>, new();

-        public virtual void Log<TLoggerCategory>(IDiagnosticsLogger<TLoggerCategory> logger, TParam1 arg1, TParam2 arg2, TParam3 arg3, TParam4 arg4, TParam5 arg5, Exception exception = null) where TLoggerCategory : LoggerCategory<TLoggerCategory>, new();

-    }
-    public class EventDefinition<TParam1, TParam2, TParam3, TParam4, TParam5, TParam6> : EventDefinitionBase {
 {
-        public EventDefinition(EventId eventId, LogLevel level, Action<ILogger, TParam1, TParam2, TParam3, TParam4, TParam5, TParam6, Exception> logAction);

-        public EventDefinition(EventId eventId, LogLevel level, string eventIdCode, Action<ILogger, TParam1, TParam2, TParam3, TParam4, TParam5, TParam6, Exception> logAction);

-        public virtual string GenerateMessage(TParam1 arg1, TParam2 arg2, TParam3 arg3, TParam4 arg4, TParam5 arg5, TParam6 arg6, Exception exception = null);

-        public virtual void Log<TLoggerCategory>(IDiagnosticsLogger<TLoggerCategory> logger, WarningBehavior warningBehavior, TParam1 arg1, TParam2 arg2, TParam3 arg3, TParam4 arg4, TParam5 arg5, TParam6 arg6, Exception exception = null) where TLoggerCategory : LoggerCategory<TLoggerCategory>, new();

-        public virtual void Log<TLoggerCategory>(IDiagnosticsLogger<TLoggerCategory> logger, TParam1 arg1, TParam2 arg2, TParam3 arg3, TParam4 arg4, TParam5 arg5, TParam6 arg6, Exception exception = null) where TLoggerCategory : LoggerCategory<TLoggerCategory>, new();

-    }
-    public abstract class EventDefinitionBase {
 {
-        protected EventDefinitionBase(EventId eventId, LogLevel level);

-        protected EventDefinitionBase(EventId eventId, LogLevel level, string eventIdCode);

-        public virtual EventId EventId { get; }

-        public virtual string EventIdCode { get; }

-        public virtual LogLevel Level { get; }

-        public virtual WarningBehavior GetLogBehavior<TLoggerCategory>(IDiagnosticsLogger<TLoggerCategory> logger) where TLoggerCategory : LoggerCategory<TLoggerCategory>, new();

-        protected virtual Exception WarningAsError(string message);

-        protected sealed class MessageExtractingLogger : ILogger {
 {
-            public MessageExtractingLogger();

-            public string Message { get; private set; }

-            IDisposable Microsoft.Extensions.Logging.ILogger.BeginScope<TState>(TState state);

-            bool Microsoft.Extensions.Logging.ILogger.IsEnabled(LogLevel logLevel);

-            void Microsoft.Extensions.Logging.ILogger.Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter);

-        }
-    }
-    public class ExecutionStrategyEventData : EventData {
 {
-        public ExecutionStrategyEventData(EventDefinitionBase eventDefinition, Func<EventDefinitionBase, EventData, string> messageGenerator, IReadOnlyList<Exception> exceptionsEncountered, TimeSpan delay, bool async);

-        public virtual TimeSpan Delay { get; }

-        public virtual IReadOnlyList<Exception> ExceptionsEncountered { get; }

-        public virtual bool IsAsync { get; }

-    }
-    public class ExpressionEventData : EventData {
 {
-        public ExpressionEventData(EventDefinitionBase eventDefinition, Func<EventDefinitionBase, EventData, string> messageGenerator, Expression expression);

-        public virtual Expression Expression { get; }

-    }
-    public class FallbackEventDefinition : EventDefinitionBase {
 {
-        public FallbackEventDefinition(EventId eventId, LogLevel level, string messageFormat);

-        public FallbackEventDefinition(EventId eventId, LogLevel level, string eventIdCode, string messageFormat);

-        public virtual string MessageFormat { get; }

-        public virtual string GenerateMessage(Action<ILogger> logAction);

-        public virtual void Log<TLoggerCategory>(IDiagnosticsLogger<TLoggerCategory> logger, WarningBehavior warningBehavior, Action<ILogger> logAction) where TLoggerCategory : LoggerCategory<TLoggerCategory>, new();

-        public virtual void Log<TLoggerCategory>(IDiagnosticsLogger<TLoggerCategory> logger, Action<ILogger> logAction) where TLoggerCategory : LoggerCategory<TLoggerCategory>, new();

-    }
-    public class ForeignKeyEventData : EventData {
 {
-        public ForeignKeyEventData(EventDefinitionBase eventDefinition, Func<EventDefinitionBase, EventData, string> messageGenerator, IForeignKey foreignKey);

-        public virtual IForeignKey ForeignKey { get; }

-    }
-    public interface IDiagnosticsLogger<TLoggerCategory> where TLoggerCategory : LoggerCategory<TLoggerCategory>, new() {
 {
-        DiagnosticSource DiagnosticSource { get; }

-        ILogger Logger { get; }

-        ILoggingOptions Options { get; }

-        WarningBehavior GetLogBehavior(EventId eventId, LogLevel logLevel);

-        bool ShouldLogSensitiveData();

-    }
-    public interface IErrorEventData {
 {
-        Exception Exception { get; }

-    }
-    public interface ILoggingOptions : ISingletonOptions {
 {
-        bool IsSensitiveDataLoggingEnabled { get; }

-        bool IsSensitiveDataLoggingWarned { get; set; }

-        WarningsConfiguration WarningsConfiguration { get; }

-    }
-    public class IncludeEventData : EventData {
 {
-        public IncludeEventData(EventDefinitionBase eventDefinition, Func<EventDefinitionBase, EventData, string> messageGenerator, IncludeResultOperator includeResultOperator);

-        public virtual IncludeResultOperator IncludeResultOperator { get; }

-    }
-    public static class InMemoryEventId {
 {
-        public static readonly EventId ChangesSaved;

-        public static readonly EventId TransactionIgnoredWarning;

-    }
-    public class LazyLoadingEventData : DbContextEventData {
 {
-        public LazyLoadingEventData(EventDefinitionBase eventDefinition, Func<EventDefinitionBase, EventData, string> messageGenerator, DbContext context, object entity, string navigationPropertyName);

-        public virtual object Entity { get; }

-        public virtual string NavigationPropertyName { get; }

-    }
-    public abstract class LoggerCategory<T> {
 {
-        protected LoggerCategory();

-        public static string Name { get; }

-        public static implicit operator string (LoggerCategory<T> loggerCategory);

-        public override string ToString();

-    }
-    public class MigrationAssemblyEventData : MigratorEventData {
 {
-        public MigrationAssemblyEventData(EventDefinitionBase eventDefinition, Func<EventDefinitionBase, EventData, string> messageGenerator, IMigrator migrator, IMigrationsAssembly migrationsAssembly);

-        public virtual IMigrationsAssembly MigrationsAssembly { get; }

-    }
-    public class MigrationEventData : MigratorEventData {
 {
-        public MigrationEventData(EventDefinitionBase eventDefinition, Func<EventDefinitionBase, EventData, string> messageGenerator, IMigrator migrator, Migration migration);

-        public virtual Migration Migration { get; }

-    }
-    public class MigrationScriptingEventData : MigrationEventData {
 {
-        public MigrationScriptingEventData(EventDefinitionBase eventDefinition, Func<EventDefinitionBase, EventData, string> messageGenerator, IMigrator migrator, Migration migration, string fromMigration, string toMigration, bool idempotent);

-        public virtual string FromMigration { get; }

-        public virtual bool IsIdempotent { get; }

-        public virtual string ToMigration { get; }

-    }
-    public class MigrationTypeEventData : EventData {
 {
-        public MigrationTypeEventData(EventDefinitionBase eventDefinition, Func<EventDefinitionBase, EventData, string> messageGenerator, TypeInfo migrationType);

-        public virtual TypeInfo MigrationType { get; }

-    }
-    public class MigratorConnectionEventData : MigratorEventData {
 {
-        public MigratorConnectionEventData(EventDefinitionBase eventDefinition, Func<EventDefinitionBase, EventData, string> messageGenerator, IMigrator migrator, DbConnection connection, Guid connectionId);

-        public virtual DbConnection Connection { get; }

-        public virtual Guid ConnectionId { get; }

-    }
-    public class MigratorEventData : EventData {
 {
-        public MigratorEventData(EventDefinitionBase eventDefinition, Func<EventDefinitionBase, EventData, string> messageGenerator, IMigrator migrator);

-        public virtual IMigrator Migrator { get; }

-    }
-    public class MinBatchSizeEventData : BatchEventData {
 {
-        public MinBatchSizeEventData(EventDefinitionBase eventDefinition, Func<EventDefinitionBase, EventData, string> messageGenerator, IEnumerable<IUpdateEntry> entries, int commandCount, int minBatchSize);

-        public virtual int MinBatchSize { get; }

-    }
-    public class NavigationEventData : EventData {
 {
-        public NavigationEventData(EventDefinitionBase eventDefinition, Func<EventDefinitionBase, EventData, string> messageGenerator, INavigation navigation);

-        public virtual INavigation Navigation { get; }

-    }
-    public class NavigationPathEventData : EventData {
 {
-        public NavigationPathEventData(EventDefinitionBase eventDefinition, Func<EventDefinitionBase, EventData, string> messageGenerator, IReadOnlyCollection<IPropertyBase> navigationPath);

-        public virtual IReadOnlyCollection<IPropertyBase> NavigationPath { get; }

-    }
-    public class PropertyChangedEventData : PropertyEventData {
 {
-        public PropertyChangedEventData(EventDefinitionBase eventDefinition, Func<EventDefinitionBase, EventData, string> messageGenerator, EntityEntry entityEntry, IProperty property, object oldValue, object newValue);

-        public virtual EntityEntry EntityEntry { get; }

-        public virtual object NewValue { get; }

-        public virtual object OldValue { get; }

-    }
-    public class PropertyEventData : EventData {
 {
-        public PropertyEventData(EventDefinitionBase eventDefinition, Func<EventDefinitionBase, EventData, string> messageGenerator, IProperty property);

-        public virtual IProperty Property { get; }

-    }
-    public class PropertyValueEventData : PropertyEventData {
 {
-        public PropertyValueEventData(EventDefinitionBase eventDefinition, Func<EventDefinitionBase, EventData, string> messageGenerator, EntityEntry entityEntry, IProperty property, object value);

-        public virtual EntityEntry EntityEntry { get; }

-        public virtual object Value { get; }

-    }
-    public class QueryExpressionEventData : EventData {
 {
-        public QueryExpressionEventData(EventDefinitionBase eventDefinition, Func<EventDefinitionBase, EventData, string> messageGenerator, Expression queryExpression, IExpressionPrinter expressionPrinter);

-        public virtual Expression Expression { get; }

-        public virtual IExpressionPrinter ExpressionPrinter { get; }

-    }
-    public class QueryModelClientEvalEventData : QueryModelEventData {
 {
-        public QueryModelClientEvalEventData(EventDefinitionBase eventDefinition, Func<EventDefinitionBase, EventData, string> messageGenerator, QueryModel queryModel, object queryModelElement);

-        public virtual object QueryModelElement { get; }

-    }
-    public class QueryModelEventData : EventData {
 {
-        public QueryModelEventData(EventDefinitionBase eventDefinition, Func<EventDefinitionBase, EventData, string> messageGenerator, QueryModel queryModel);

-        public virtual QueryModel QueryModel { get; }

-    }
-    public class ReferenceChangedEventData : NavigationEventData {
 {
-        public ReferenceChangedEventData(EventDefinitionBase eventDefinition, Func<EventDefinitionBase, EventData, string> messageGenerator, EntityEntry entityEntry, INavigation navigation, object oldReferencedEntity, object newReferencedEntity);

-        public virtual EntityEntry EntityEntry { get; }

-        public virtual object NewReferencedEntity { get; }

-        public virtual object OldReferencedEntity { get; }

-    }
-    public static class RelationalEventId {
 {
-        public static readonly EventId AmbientTransactionEnlisted;

-        public static readonly EventId AmbientTransactionWarning;

-        public static readonly EventId BatchReadyForExecution;

-        public static readonly EventId BatchSmallerThanMinBatchSize;

-        public static readonly EventId BoolWithDefaultWarning;

-        public static readonly EventId CommandError;

-        public static readonly EventId CommandExecuted;

-        public static readonly EventId CommandExecuting;

-        public static readonly EventId ConnectionClosed;

-        public static readonly EventId ConnectionClosing;

-        public static readonly EventId ConnectionError;

-        public static readonly EventId ConnectionOpened;

-        public static readonly EventId ConnectionOpening;

-        public static readonly EventId DataReaderDisposing;

-        public static readonly EventId ExplicitTransactionEnlisted;

-        public static readonly EventId MigrateUsingConnection;

-        public static readonly EventId MigrationApplying;

-        public static readonly EventId MigrationAttributeMissingWarning;

-        public static readonly EventId MigrationGeneratingDownScript;

-        public static readonly EventId MigrationGeneratingUpScript;

-        public static readonly EventId MigrationReverting;

-        public static readonly EventId MigrationsNotApplied;

-        public static readonly EventId MigrationsNotFound;

-        public static readonly EventId ModelValidationKeyDefaultValueWarning;

-        public static readonly EventId QueryClientEvaluationWarning;

-        public static readonly EventId QueryPossibleExceptionWithAggregateOperator;

-        public static readonly EventId QueryPossibleUnintendedUseOfEqualsWarning;

-        public static readonly EventId TransactionCommitted;

-        public static readonly EventId TransactionDisposed;

-        public static readonly EventId TransactionError;

-        public static readonly EventId TransactionRolledBack;

-        public static readonly EventId TransactionStarted;

-        public static readonly EventId TransactionUsed;

-        public static readonly EventId ValueConversionSqlLiteralWarning;

-    }
-    public class SaveChangesCompletedEventData : DbContextEventData {
 {
-        public SaveChangesCompletedEventData(EventDefinitionBase eventDefinition, Func<EventDefinitionBase, EventData, string> messageGenerator, DbContext context, int entitiesSavedCount);

-        public virtual int EntitiesSavedCount { get; }

-    }
-    public class SaveChangesEventData : EventData {
 {
-        public SaveChangesEventData(EventDefinitionBase eventDefinition, Func<EventDefinitionBase, EventData, string> messageGenerator, IEnumerable<IUpdateEntry> entries, int rowsAffected);

-        public virtual IEnumerable<IUpdateEntry> Entries { get; }

-        public virtual int RowsAffected { get; }

-    }
-    public class SequenceEventData : EventData {
 {
-        public SequenceEventData(EventDefinitionBase eventDefinition, Func<EventDefinitionBase, EventData, string> messageGenerator, ISequence sequence);

-        public virtual ISequence Sequence { get; }

-    }
-    public class ServiceProviderDebugInfoEventData : EventData {
 {
-        public ServiceProviderDebugInfoEventData(EventDefinitionBase eventDefinition, Func<EventDefinitionBase, EventData, string> messageGenerator, IDictionary<string, string> newDebugInfo, IList<IDictionary<string, string>> cachedDebugInfos);

-        public virtual IList<IDictionary<string, string>> CachedDebugInfos { get; }

-        public virtual IDictionary<string, string> NewDebugInfo { get; }

-    }
-    public class ServiceProviderEventData : EventData {
 {
-        public ServiceProviderEventData(EventDefinitionBase eventDefinition, Func<EventDefinitionBase, EventData, string> messageGenerator, IServiceProvider serviceProvider);

-        public virtual IServiceProvider ServiceProvider { get; }

-    }
-    public class ServiceProvidersEventData : EventData {
 {
-        public ServiceProvidersEventData(EventDefinitionBase eventDefinition, Func<EventDefinitionBase, EventData, string> messageGenerator, ICollection<IServiceProvider> serviceProviders);

-        public virtual ICollection<IServiceProvider> ServiceProviders { get; }

-    }
-    public class SharedDependentEntityEventData : EventData {
 {
-        public SharedDependentEntityEventData(EventDefinitionBase eventDefinition, Func<EventDefinitionBase, EventData, string> messageGenerator, IEntityType firstEntityType, IEntityType secondEntityType);

-        public virtual IEntityType FirstEntityType { get; }

-        public virtual IEntityType SecondEntityType { get; }

-    }
-    public static class SqlServerEventId {
 {
-        public static readonly EventId ByteIdentityColumnWarning;

-        public static readonly EventId ColumnFound;

-        public static readonly EventId ColumnNotNamedWarning;

-        public static readonly EventId ColumnSkipped;

-        public static readonly EventId DecimalTypeDefaultWarning;

-        public static readonly EventId DefaultSchemaFound;

-        public static readonly EventId ForeignKeyColumnFound;

-        public static readonly EventId ForeignKeyColumnMissingWarning;

-        public static readonly EventId ForeignKeyColumnNotNamedWarning;

-        public static readonly EventId ForeignKeyColumnsNotMappedWarning;

-        public static readonly EventId ForeignKeyFound;

-        public static readonly EventId ForeignKeyNotNamedWarning;

-        public static readonly EventId ForeignKeyPrincipalColumnMissingWarning;

-        public static readonly EventId ForeignKeyReferencesMissingPrincipalTableWarning;

-        public static readonly EventId ForeignKeyTableMissingWarning;

-        public static readonly EventId IndexColumnFound;

-        public static readonly EventId IndexColumnNotNamedWarning;

-        public static readonly EventId IndexColumnSkipped;

-        public static readonly EventId IndexColumnsNotMappedWarning;

-        public static readonly EventId IndexFound;

-        public static readonly EventId IndexNotNamedWarning;

-        public static readonly EventId IndexTableMissingWarning;

-        public static readonly EventId MissingSchemaWarning;

-        public static readonly EventId MissingTableWarning;

-        public static readonly EventId PrimaryKeyFound;

-        public static readonly EventId ReflexiveConstraintIgnored;

-        public static readonly EventId SequenceFound;

-        public static readonly EventId SequenceNotNamedWarning;

-        public static readonly EventId TableFound;

-        public static readonly EventId TableSkipped;

-        public static readonly EventId TypeAliasFound;

-        public static readonly EventId UniqueConstraintFound;

-    }
-    public class StateChangedEventData : EntityEntryEventData {
 {
-        public StateChangedEventData(EventDefinitionBase eventDefinition, Func<EventDefinitionBase, EventData, string> messageGenerator, EntityEntry entityEntry, EntityState oldState, EntityState newState);

-        public virtual EntityState NewState { get; }

-        public virtual EntityState OldState { get; }

-    }
-    public class TransactionEndEventData : TransactionEventData {
 {
-        public TransactionEndEventData(EventDefinitionBase eventDefinition, Func<EventDefinitionBase, EventData, string> messageGenerator, DbTransaction transaction, Guid transactionId, Guid connectionId, DateTimeOffset startTime, TimeSpan duration);

-        public virtual TimeSpan Duration { get; }

-    }
-    public class TransactionEnlistedEventData : EventData {
 {
-        public TransactionEnlistedEventData(EventDefinitionBase eventDefinition, Func<EventDefinitionBase, EventData, string> messageGenerator, Transaction transaction, DbConnection connection, Guid connectionId);

-        public virtual DbConnection Connection { get; }

-        public virtual Guid ConnectionId { get; }

-        public virtual Transaction Transaction { get; }

-    }
-    public class TransactionErrorEventData : TransactionEndEventData, IErrorEventData {
 {
-        public TransactionErrorEventData(EventDefinitionBase eventDefinition, Func<EventDefinitionBase, EventData, string> messageGenerator, DbTransaction transaction, Guid transactionId, Guid connectionId, string action, Exception exception, DateTimeOffset startTime, TimeSpan duration);

-        public virtual string Action { get; }

-        public virtual Exception Exception { get; }

-    }
-    public class TransactionEventData : EventData {
 {
-        public TransactionEventData(EventDefinitionBase eventDefinition, Func<EventDefinitionBase, EventData, string> messageGenerator, DbTransaction transaction, Guid transactionId, Guid connectionId, DateTimeOffset startTime);

-        public virtual Guid ConnectionId { get; }

-        public virtual DateTimeOffset StartTime { get; }

-        public virtual DbTransaction Transaction { get; }

-        public virtual Guid TransactionId { get; }

-    }
-    public class TwoPropertyBaseCollectionsEventData : EventData {
 {
-        public TwoPropertyBaseCollectionsEventData(EventDefinitionBase eventDefinition, Func<EventDefinitionBase, EventData, string> messageGenerator, IReadOnlyList<IPropertyBase> firstPropertyCollection, IReadOnlyList<IPropertyBase> secondPropertyCollection);

-        public virtual IReadOnlyList<IPropertyBase> FirstPropertyCollection { get; }

-        public virtual IReadOnlyList<IPropertyBase> SecondPropertyCollection { get; }

-    }
-    public class TwoUnmappedPropertyCollectionsEventData : EventData {
 {
-        public TwoUnmappedPropertyCollectionsEventData(EventDefinitionBase eventDefinition, Func<EventDefinitionBase, EventData, string> messageGenerator, IEnumerable<Tuple<MemberInfo, Type>> firstPropertyCollection, IEnumerable<Tuple<MemberInfo, Type>> secondPropertyCollection);

-        public virtual IEnumerable<Tuple<MemberInfo, Type>> FirstPropertyCollection { get; }

-        public virtual IEnumerable<Tuple<MemberInfo, Type>> SecondPropertyCollection { get; }

-    }
-    public class ValueConverterEventData : EventData {
 {
-        public ValueConverterEventData(EventDefinitionBase eventDefinition, Func<EventDefinitionBase, EventData, string> messageGenerator, Type mappingClrType, ValueConverter valueConverter);

-        public virtual Type MappingClrType { get; }

-        public virtual ValueConverter ValueConverter { get; }

-    }
-    public class WarningsConfiguration {
 {
-        public WarningsConfiguration();

-        protected WarningsConfiguration(WarningsConfiguration copyFrom);

-        public virtual WarningBehavior DefaultBehavior { get; }

-        protected virtual WarningsConfiguration Clone();

-        public virtual Nullable<WarningBehavior> GetBehavior(EventId eventId);

-        public virtual long GetServiceProviderHashCode();

-        public virtual WarningsConfiguration TryWithExplicit(EventId eventId, WarningBehavior warningBehavior);

-        public virtual WarningsConfiguration WithDefaultBehavior(WarningBehavior warningBehavior);

-        public virtual WarningsConfiguration WithExplicit(IEnumerable<EventId> eventIds, WarningBehavior warningBehavior);

-    }
-    public class WarningsConfigurationBuilder {
 {
-        public WarningsConfigurationBuilder(DbContextOptionsBuilder optionsBuilder);

-        public virtual WarningsConfigurationBuilder Default(WarningBehavior warningBehavior);

-        public virtual WarningsConfigurationBuilder Ignore(params EventId[] eventIds);

-        public virtual WarningsConfigurationBuilder Log(params EventId[] eventIds);

-        public virtual WarningsConfigurationBuilder Throw(params EventId[] eventIds);

-    }
-}
```

