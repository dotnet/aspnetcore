# Microsoft.EntityFrameworkCore.Design.Internal

``` diff
-namespace Microsoft.EntityFrameworkCore.Design.Internal {
 {
-    public class AppServiceProviderFactory {
 {
-        public AppServiceProviderFactory(Assembly startupAssembly, IOperationReporter reporter);

-        public virtual IServiceProvider Create(string[] args);

-        protected virtual Type FindProgramClass();

-    }
-    public class ContextInfo {
 {
-        public ContextInfo();

-        public virtual string DatabaseName { get; set; }

-        public virtual string DataSource { get; set; }

-        public virtual string Options { get; set; }

-        public virtual string ProviderName { get; set; }

-    }
-    public class CSharpHelper : ICSharpHelper {
 {
-        public CSharpHelper(IRelationalTypeMappingSource relationalTypeMappingSource);

-        public virtual string Fragment(MethodCallCodeFragment fragment);

-        protected virtual string GetCompositeEnumValue(Type type, Enum flags);

-        protected virtual string GetSimpleEnumValue(Type type, string name);

-        public virtual string Identifier(string name, ICollection<string> scope = null);

-        public virtual string Lambda(IReadOnlyList<string> properties);

-        public virtual string Literal(bool value);

-        public virtual string Literal(byte value);

-        public virtual string Literal(byte[] values);

-        public virtual string Literal(char value);

-        public virtual string Literal(IReadOnlyList<object> values);

-        public virtual string Literal(IReadOnlyList<object> values, bool vertical);

-        public virtual string Literal(DateTime value);

-        public virtual string Literal(DateTimeOffset value);

-        public virtual string Literal(Decimal value);

-        public virtual string Literal(double value);

-        public virtual string Literal(Enum value);

-        public virtual string Literal(Guid value);

-        public virtual string Literal(short value);

-        public virtual string Literal(int value);

-        public virtual string Literal(long value);

-        public virtual string Literal(BigInteger value);

-        public virtual string Literal(object[,] values);

-        public virtual string Literal(sbyte value);

-        public virtual string Literal(float value);

-        public virtual string Literal(string value);

-        public virtual string Literal(TimeSpan value);

-        public virtual string Literal(ushort value);

-        public virtual string Literal(uint value);

-        public virtual string Literal(ulong value);

-        public virtual string Literal<T>(IReadOnlyList<T> values);

-        public virtual string Literal<T>(Nullable<T> value) where T : struct, ValueType;

-        public virtual string Namespace(params string[] name);

-        public virtual string Reference(Type type);

-        public virtual string UnknownLiteral(object value);

-    }
-    public class DatabaseOperations {
 {
-        public DatabaseOperations(IOperationReporter reporter, Assembly assembly, Assembly startupAssembly, string projectDir, string rootNamespace, string language, string[] args);

-        public virtual SavedModelFiles ScaffoldContext(string provider, string connectionString, string outputDir, string outputContextDir, string dbContextClassName, IEnumerable<string> schemas, IEnumerable<string> tables, bool useDataAnnotations, bool overwriteFiles, bool useDatabaseNames);

-    }
-    public class DbContextOperations {
 {
-        public DbContextOperations(IOperationReporter reporter, Assembly assembly, Assembly startupAssembly, string[] args);

-        protected DbContextOperations(IOperationReporter reporter, Assembly assembly, Assembly startupAssembly, string[] args, AppServiceProviderFactory appServicesFactory);

-        public virtual DbContext CreateContext(string contextType);

-        public virtual void DropDatabase(string contextType);

-        public virtual ContextInfo GetContextInfo(string contextType);

-        public virtual Type GetContextType(string name);

-        public virtual IEnumerable<Type> GetContextTypes();

-    }
-    public class DesignTimeConnectionStringResolver : NamedConnectionStringResolverBase {
 {
-        public DesignTimeConnectionStringResolver(Func<IServiceProvider> applicationServiceProviderAccessor);

-        protected override IServiceProvider ApplicationServiceProvider { get; }

-    }
-    public class DesignTimeServicesBuilder {
 {
-        public DesignTimeServicesBuilder(Assembly assembly, Assembly startupAssembly, IOperationReporter reporter, string[] args);

-        public virtual IServiceProvider Build(DbContext context);

-        public virtual IServiceProvider Build(string provider);

-    }
-    public static class ForwardingProxy {
 {
-        public static T Unwrap<T>(object target) where T : class;

-    }
-    public interface IOperationReporter {
 {
-        void WriteError(string message);

-        void WriteInformation(string message);

-        void WriteVerbose(string message);

-        void WriteWarning(string message);

-    }
-    public abstract class LanguageBasedSelector<T> where T : ILanguageBasedService {
 {
-        protected LanguageBasedSelector(IEnumerable<T> services);

-        protected virtual IEnumerable<T> Services { get; }

-        public virtual T Select(string language);

-    }
-    public class MigrationInfo {
 {
-        public MigrationInfo();

-        public virtual string Id { get; set; }

-        public virtual string Name { get; set; }

-    }
-    public class MigrationsOperations {
 {
-        public MigrationsOperations(IOperationReporter reporter, Assembly assembly, Assembly startupAssembly, string projectDir, string rootNamespace, string language, string[] args);

-        public virtual MigrationFiles AddMigration(string name, string outputDir, string contextType);

-        public virtual IEnumerable<MigrationInfo> GetMigrations(string contextType);

-        public virtual MigrationFiles RemoveMigration(string contextType, bool force);

-        public virtual string ScriptMigration(string fromMigration, string toMigration, bool idempotent, string contextType);

-        public virtual void UpdateDatabase(string targetMigration, string contextType);

-    }
-    public class NamespaceComparer : IComparer<string> {
 {
-        public NamespaceComparer();

-        public virtual int Compare(string x, string y);

-    }
-    public class NullPluralizer : IPluralizer {
 {
-        public NullPluralizer();

-        public virtual string Pluralize(string identifier);

-        public virtual string Singularize(string identifier);

-    }
-    public class OperationLogger : ILogger {
 {
-        public OperationLogger(string categoryName, IOperationReporter reporter);

-        public virtual IDisposable BeginScope<TState>(TState state);

-        public virtual bool IsEnabled(LogLevel logLevel);

-        public virtual void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter);

-    }
-    public class OperationLoggerProvider : IDisposable, ILoggerProvider {
 {
-        public OperationLoggerProvider(IOperationReporter reporter);

-        public virtual ILogger CreateLogger(string categoryName);

-        public virtual void Dispose();

-    }
-    public class OperationReporter : IOperationReporter {
 {
-        public OperationReporter(IOperationReportHandler handler);

-        public virtual void WriteError(string message);

-        public virtual void WriteInformation(string message);

-        public virtual void WriteVerbose(string message);

-        public virtual void WriteWarning(string message);

-    }
-}
```

