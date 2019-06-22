# Microsoft.EntityFrameworkCore.Query.ExpressionTranslators

``` diff
-namespace Microsoft.EntityFrameworkCore.Query.ExpressionTranslators {
 {
-    public interface ICompositeMethodCallTranslator {
 {
-        Expression Translate(MethodCallExpression methodCallExpression, IModel model);

-    }
-    public interface IExpressionFragmentTranslator {
 {
-        Expression Translate(Expression expression);

-    }
-    public interface IMemberTranslator {
 {
-        Expression Translate(MemberExpression memberExpression);

-    }
-    public interface IMemberTranslatorPlugin {
 {
-        IEnumerable<IMemberTranslator> Translators { get; }

-    }
-    public interface IMethodCallTranslator {
 {
-        Expression Translate(MethodCallExpression methodCallExpression);

-    }
-    public interface IMethodCallTranslatorPlugin {
 {
-        IEnumerable<IMethodCallTranslator> Translators { get; }

-    }
-    public abstract class MultipleOverloadStaticMethodCallTranslator : IMethodCallTranslator {
 {
-        protected MultipleOverloadStaticMethodCallTranslator(Type declaringType, string clrMethodName, string sqlFunctionName);

-        public virtual Expression Translate(MethodCallExpression methodCallExpression);

-    }
-    public abstract class ParameterlessInstanceMethodCallTranslator : IMethodCallTranslator {
 {
-        protected ParameterlessInstanceMethodCallTranslator(Type declaringType, string clrMethodName, string sqlFunctionName);

-        public virtual Expression Translate(MethodCallExpression methodCallExpression);

-    }
-    public class RelationalCompositeExpressionFragmentTranslator : IExpressionFragmentTranslator {
 {
-        public RelationalCompositeExpressionFragmentTranslator(RelationalCompositeExpressionFragmentTranslatorDependencies dependencies);

-        protected virtual void AddTranslators(IEnumerable<IExpressionFragmentTranslator> translators);

-        public virtual Expression Translate(Expression expression);

-    }
-    public sealed class RelationalCompositeExpressionFragmentTranslatorDependencies {
 {
-        public RelationalCompositeExpressionFragmentTranslatorDependencies();

-    }
-    public abstract class RelationalCompositeMemberTranslator : IMemberTranslator {
 {
-        protected RelationalCompositeMemberTranslator(RelationalCompositeMemberTranslatorDependencies dependencies);

-        protected virtual void AddTranslators(IEnumerable<IMemberTranslator> translators);

-        public virtual Expression Translate(MemberExpression memberExpression);

-    }
-    public sealed class RelationalCompositeMemberTranslatorDependencies {
 {
-        public RelationalCompositeMemberTranslatorDependencies(IEnumerable<IMemberTranslatorPlugin> plugins);

-        public IEnumerable<IMemberTranslatorPlugin> Plugins { get; }

-        public RelationalCompositeMemberTranslatorDependencies With(IEnumerable<IMemberTranslatorPlugin> plugins);

-    }
-    public abstract class RelationalCompositeMethodCallTranslator : ICompositeMethodCallTranslator {
 {
-        protected RelationalCompositeMethodCallTranslator(RelationalCompositeMethodCallTranslatorDependencies dependencies);

-        protected virtual RelationalCompositeMethodCallTranslatorDependencies Dependencies { get; }

-        protected virtual void AddTranslators(IEnumerable<IMethodCallTranslator> translators);

-        public virtual Expression Translate(MethodCallExpression methodCallExpression, IModel model);

-    }
-    public sealed class RelationalCompositeMethodCallTranslatorDependencies {
 {
-        public RelationalCompositeMethodCallTranslatorDependencies(IDiagnosticsLogger<DbLoggerCategory.Query> logger, IEnumerable<IMethodCallTranslatorPlugin> plugins);

-        public IDiagnosticsLogger<DbLoggerCategory.Query> Logger { get; }

-        public IEnumerable<IMethodCallTranslatorPlugin> Plugins { get; }

-        public RelationalCompositeMethodCallTranslatorDependencies With(IDiagnosticsLogger<DbLoggerCategory.Query> logger);

-        public RelationalCompositeMethodCallTranslatorDependencies With(IEnumerable<IMethodCallTranslatorPlugin> plugins);

-    }
-    public abstract class SingleOverloadStaticMethodCallTranslator : IMethodCallTranslator {
 {
-        protected SingleOverloadStaticMethodCallTranslator(Type declaringType, string clrMethodName, string sqlFunctionName);

-        public virtual Expression Translate(MethodCallExpression methodCallExpression);

-    }
-}
```

