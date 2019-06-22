# Microsoft.EntityFrameworkCore.Design

``` diff
-namespace Microsoft.EntityFrameworkCore.Design {
 {
-    public class AnnotationCodeGenerator : IAnnotationCodeGenerator {
 {
-        public AnnotationCodeGenerator(AnnotationCodeGeneratorDependencies dependencies);

-        protected virtual AnnotationCodeGeneratorDependencies Dependencies { get; }

-        public virtual MethodCallCodeFragment GenerateFluentApi(IEntityType entityType, IAnnotation annotation);

-        public virtual string GenerateFluentApi(IEntityType entityType, IAnnotation annotation, string language);

-        public virtual MethodCallCodeFragment GenerateFluentApi(IForeignKey foreignKey, IAnnotation annotation);

-        public virtual string GenerateFluentApi(IForeignKey foreignKey, IAnnotation annotation, string language);

-        public virtual MethodCallCodeFragment GenerateFluentApi(IIndex index, IAnnotation annotation);

-        public virtual string GenerateFluentApi(IIndex index, IAnnotation annotation, string language);

-        public virtual MethodCallCodeFragment GenerateFluentApi(IKey key, IAnnotation annotation);

-        public virtual string GenerateFluentApi(IKey key, IAnnotation annotation, string language);

-        public virtual MethodCallCodeFragment GenerateFluentApi(IModel model, IAnnotation annotation);

-        public virtual string GenerateFluentApi(IModel model, IAnnotation annotation, string language);

-        public virtual MethodCallCodeFragment GenerateFluentApi(IProperty property, IAnnotation annotation);

-        public virtual string GenerateFluentApi(IProperty property, IAnnotation annotation, string language);

-        public virtual bool IsHandledByConvention(IEntityType entityType, IAnnotation annotation);

-        public virtual bool IsHandledByConvention(IForeignKey foreignKey, IAnnotation annotation);

-        public virtual bool IsHandledByConvention(IIndex index, IAnnotation annotation);

-        public virtual bool IsHandledByConvention(IKey key, IAnnotation annotation);

-        public virtual bool IsHandledByConvention(IModel model, IAnnotation annotation);

-        public virtual bool IsHandledByConvention(IProperty property, IAnnotation annotation);

-    }
-    public sealed class AnnotationCodeGeneratorDependencies {
 {
-        public AnnotationCodeGeneratorDependencies();

-    }
-    public static class DbContextActivator {
 {
-        public static DbContext CreateInstance(Type contextType, Assembly startupAssembly = null, IOperationReportHandler reportHandler = null);

-    }
-    public sealed class DesignTimeProviderServicesAttribute : Attribute {
 {
-        public DesignTimeProviderServicesAttribute(string typeName);

-        public string TypeName { get; }

-    }
-    public static class DesignTimeServiceCollectionExtensions {
 {
-        public static IServiceCollection AddDbContextDesignTimeServices(this IServiceCollection services, DbContext context);

-        public static IServiceCollection AddEntityFrameworkDesignTimeServices(this IServiceCollection services, IOperationReporter reporter = null, Func<IServiceProvider> applicationServiceProviderAccessor = null);

-    }
-    public sealed class DesignTimeServicesReferenceAttribute : Attribute {
 {
-        public DesignTimeServicesReferenceAttribute(string typeName);

-        public DesignTimeServicesReferenceAttribute(string typeName, string forProvider);

-        public string ForProvider { get; }

-        public string TypeName { get; }

-    }
-    public interface IAnnotationCodeGenerator {
 {
-        MethodCallCodeFragment GenerateFluentApi(IEntityType entityType, IAnnotation annotation);

-        string GenerateFluentApi(IEntityType entityType, IAnnotation annotation, string language);

-        MethodCallCodeFragment GenerateFluentApi(IForeignKey foreignKey, IAnnotation annotation);

-        string GenerateFluentApi(IForeignKey foreignKey, IAnnotation annotation, string language);

-        MethodCallCodeFragment GenerateFluentApi(IIndex index, IAnnotation annotation);

-        string GenerateFluentApi(IIndex index, IAnnotation annotation, string language);

-        MethodCallCodeFragment GenerateFluentApi(IKey key, IAnnotation annotation);

-        string GenerateFluentApi(IKey key, IAnnotation annotation, string language);

-        MethodCallCodeFragment GenerateFluentApi(IModel model, IAnnotation annotation);

-        string GenerateFluentApi(IModel model, IAnnotation annotation, string language);

-        MethodCallCodeFragment GenerateFluentApi(IProperty property, IAnnotation annotation);

-        string GenerateFluentApi(IProperty property, IAnnotation annotation, string language);

-        bool IsHandledByConvention(IEntityType entityType, IAnnotation annotation);

-        bool IsHandledByConvention(IForeignKey foreignKey, IAnnotation annotation);

-        bool IsHandledByConvention(IIndex index, IAnnotation annotation);

-        bool IsHandledByConvention(IKey key, IAnnotation annotation);

-        bool IsHandledByConvention(IModel model, IAnnotation annotation);

-        bool IsHandledByConvention(IProperty property, IAnnotation annotation);

-    }
-    public interface ICSharpHelper {
 {
-        string Fragment(MethodCallCodeFragment fragment);

-        string Identifier(string name, ICollection<string> scope = null);

-        string Lambda(IReadOnlyList<string> properties);

-        string Literal(bool value);

-        string Literal(byte value);

-        string Literal(byte[] values);

-        string Literal(char value);

-        string Literal(IReadOnlyList<object> values);

-        string Literal(IReadOnlyList<object> values, bool vertical);

-        string Literal(DateTime value);

-        string Literal(DateTimeOffset value);

-        string Literal(Decimal value);

-        string Literal(double value);

-        string Literal(Enum value);

-        string Literal(Guid value);

-        string Literal(short value);

-        string Literal(int value);

-        string Literal(long value);

-        string Literal(object[,] values);

-        string Literal(sbyte value);

-        string Literal(float value);

-        string Literal(string value);

-        string Literal(TimeSpan value);

-        string Literal(ushort value);

-        string Literal(uint value);

-        string Literal(ulong value);

-        string Literal<T>(IReadOnlyList<T> values);

-        string Literal<T>(Nullable<T> value) where T : struct, ValueType;

-        string Namespace(params string[] name);

-        string Reference(Type type);

-        string UnknownLiteral(object value);

-    }
-    public interface IDesignTimeDbContextFactory<out TContext> where TContext : DbContext {
 {
-        TContext CreateDbContext(string[] args);

-    }
-    public interface IDesignTimeServices {
 {
-        void ConfigureDesignTimeServices(IServiceCollection serviceCollection);

-    }
-    public interface ILanguageBasedService {
 {
-        string Language { get; }

-    }
-    public interface IOperationReportHandler {
 {
-        int Version { get; }

-        void OnError(string message);

-        void OnInformation(string message);

-        void OnVerbose(string message);

-        void OnWarning(string message);

-    }
-    public interface IOperationResultHandler {
 {
-        int Version { get; }

-        void OnError(string type, string message, string stackTrace);

-        void OnResult(object value);

-    }
-    public interface IPluralizer {
 {
-        string Pluralize(string identifier);

-        string Singularize(string identifier);

-    }
-    public class MethodCallCodeFragment {
 {
-        public MethodCallCodeFragment(string method, params object[] arguments);

-        public MethodCallCodeFragment(string method, object[] arguments, MethodCallCodeFragment chainedCall);

-        public virtual IReadOnlyList<object> Arguments { get; }

-        public virtual MethodCallCodeFragment ChainedCall { get; }

-        public virtual string Method { get; }

-        public virtual MethodCallCodeFragment Chain(MethodCallCodeFragment call);

-        public virtual MethodCallCodeFragment Chain(string method, params object[] arguments);

-    }
-    public class NestedClosureCodeFragment {
 {
-        public NestedClosureCodeFragment(string parameter, MethodCallCodeFragment methodCall);

-        public virtual MethodCallCodeFragment MethodCall { get; }

-        public virtual string Parameter { get; }

-    }
-    public class OperationException : Exception {
 {
-        public OperationException(string message);

-        public OperationException(string message, Exception innerException);

-    }
-    public class OperationExecutor : MarshalByRefObject {
 {
-        public OperationExecutor(object reportHandler, IDictionary args);

-        public class AddMigration : OperationExecutor.OperationBase {
 {
-            public AddMigration(OperationExecutor executor, object resultHandler, IDictionary args);

-        }
-        public class DropDatabase : OperationExecutor.OperationBase {
 {
-            public DropDatabase(OperationExecutor executor, object resultHandler, IDictionary args);

-        }
-        public class GetContextInfo : OperationExecutor.OperationBase {
 {
-            public GetContextInfo(OperationExecutor executor, object resultHandler, IDictionary args);

-        }
-        public class GetContextTypes : OperationExecutor.OperationBase {
 {
-            public GetContextTypes(OperationExecutor executor, object resultHandler, IDictionary args);

-        }
-        public class GetMigrations : OperationExecutor.OperationBase {
 {
-            public GetMigrations(OperationExecutor executor, object resultHandler, IDictionary args);

-        }
-        public abstract class OperationBase : MarshalByRefObject {
 {
-            protected OperationBase(object resultHandler);

-            public virtual void Execute(Action action);

-            public virtual void Execute<T>(Func<IEnumerable<T>> action);

-            public virtual void Execute<T>(Func<T> action);

-        }
-        public class RemoveMigration : OperationExecutor.OperationBase {
 {
-            public RemoveMigration(OperationExecutor executor, object resultHandler, IDictionary args);

-        }
-        public class ScaffoldContext : OperationExecutor.OperationBase {
 {
-            public ScaffoldContext(OperationExecutor executor, object resultHandler, IDictionary args);

-        }
-        public class ScriptMigration : OperationExecutor.OperationBase {
 {
-            public ScriptMigration(OperationExecutor executor, object resultHandler, IDictionary args);

-        }
-        public class UpdateDatabase : OperationExecutor.OperationBase {
 {
-            public UpdateDatabase(OperationExecutor executor, object resultHandler, IDictionary args);

-        }
-    }
-    public class OperationReportHandler : MarshalByRefObject, IOperationReportHandler {
 {
-        public OperationReportHandler(Action<string> errorHandler = null, Action<string> warningHandler = null, Action<string> informationHandler = null, Action<string> verboseHandler = null);

-        public virtual int Version { get; }

-        public virtual void OnError(string message);

-        public virtual void OnInformation(string message);

-        public virtual void OnVerbose(string message);

-        public virtual void OnWarning(string message);

-    }
-    public class OperationResultHandler : MarshalByRefObject, IOperationResultHandler {
 {
-        public OperationResultHandler();

-        public virtual string ErrorMessage { get; }

-        public virtual string ErrorStackTrace { get; }

-        public virtual string ErrorType { get; }

-        public virtual bool HasResult { get; }

-        public virtual object Result { get; }

-        public virtual int Version { get; }

-        public virtual void OnError(string type, string message, string stackTrace);

-        public virtual void OnResult(object value);

-    }
-}
```

