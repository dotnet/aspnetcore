# Microsoft.EntityFrameworkCore.Query.ExpressionVisitors

``` diff
-namespace Microsoft.EntityFrameworkCore.Query.ExpressionVisitors {
 {
-    public abstract class EntityQueryableExpressionVisitor : DefaultQueryExpressionVisitor {
 {
-        protected EntityQueryableExpressionVisitor(EntityQueryModelVisitor entityQueryModelVisitor);

-        protected override Expression VisitConstant(ConstantExpression constantExpression);

-        protected abstract Expression VisitEntityQueryable(Type elementType);

-    }
-    public abstract class ExpressionVisitorBase : RelinqExpressionVisitor {
 {
-        protected ExpressionVisitorBase();

-        protected override Expression VisitExtension(Expression extensionExpression);

-        protected override Expression VisitLambda<T>(Expression<T> node);

-        protected override Expression VisitSubQuery(SubQueryExpression subQueryExpression);

-    }
-    public interface IEntityQueryableExpressionVisitorFactory {
 {
-        ExpressionVisitor Create(EntityQueryModelVisitor entityQueryModelVisitor, IQuerySource querySource);

-    }
-    public interface IProjectionExpressionVisitorFactory {
 {
-        ExpressionVisitor Create(EntityQueryModelVisitor entityQueryModelVisitor, IQuerySource querySource);

-    }
-    public interface ISqlTranslatingExpressionVisitorFactory {
 {
-        SqlTranslatingExpressionVisitor Create(RelationalQueryModelVisitor queryModelVisitor, SelectExpression targetSelectExpression = null, Expression topLevelPredicate = null, bool inProjection = false);

-    }
-    public class ProjectionExpressionVisitor : DefaultQueryExpressionVisitor {
 {
-        public ProjectionExpressionVisitor(EntityQueryModelVisitor entityQueryModelVisitor);

-        protected override Expression VisitMethodCall(MethodCallExpression methodCallExpression);

-        protected override Expression VisitSubQuery(SubQueryExpression expression);

-        protected override Expression VisitUnary(UnaryExpression unaryExpression);

-    }
-    public class RelationalEntityQueryableExpressionVisitor : EntityQueryableExpressionVisitor {
 {
-        public RelationalEntityQueryableExpressionVisitor(RelationalEntityQueryableExpressionVisitorDependencies dependencies, RelationalQueryModelVisitor queryModelVisitor, IQuerySource querySource);

-        protected override Expression VisitEntityQueryable(Type elementType);

-        protected override Expression VisitMember(MemberExpression node);

-        protected override Expression VisitMethodCall(MethodCallExpression node);

-        protected override Expression VisitSubQuery(SubQueryExpression expression);

-    }
-    public sealed class RelationalEntityQueryableExpressionVisitorDependencies {
 {
-        public RelationalEntityQueryableExpressionVisitorDependencies(IModel model, ISelectExpressionFactory selectExpressionFactory, IMaterializerFactory materializerFactory, IShaperCommandContextFactory shaperCommandContextFactory);

-        public IMaterializerFactory MaterializerFactory { get; }

-        public IModel Model { get; }

-        public ISelectExpressionFactory SelectExpressionFactory { get; }

-        public IShaperCommandContextFactory ShaperCommandContextFactory { get; }

-        public RelationalEntityQueryableExpressionVisitorDependencies With(IModel model);

-        public RelationalEntityQueryableExpressionVisitorDependencies With(ISelectExpressionFactory selectExpressionFactory);

-        public RelationalEntityQueryableExpressionVisitorDependencies With(IMaterializerFactory materializerFactory);

-        public RelationalEntityQueryableExpressionVisitorDependencies With(IShaperCommandContextFactory shaperCommandContextFactory);

-    }
-    public class RelationalEntityQueryableExpressionVisitorFactory : IEntityQueryableExpressionVisitorFactory {
 {
-        public RelationalEntityQueryableExpressionVisitorFactory(RelationalEntityQueryableExpressionVisitorDependencies dependencies);

-        protected virtual RelationalEntityQueryableExpressionVisitorDependencies Dependencies { get; }

-        public virtual ExpressionVisitor Create(EntityQueryModelVisitor queryModelVisitor, IQuerySource querySource);

-    }
-    public class RelationalProjectionExpressionVisitor : ProjectionExpressionVisitor {
 {
-        public RelationalProjectionExpressionVisitor(RelationalProjectionExpressionVisitorDependencies dependencies, RelationalQueryModelVisitor queryModelVisitor, IQuerySource querySource);

-        public override Expression Visit(Expression expression);

-        protected override Expression VisitMemberInit(MemberInitExpression memberInitExpression);

-        protected override Expression VisitNew(NewExpression newExpression);

-    }
-    public sealed class RelationalProjectionExpressionVisitorDependencies {
 {
-        public RelationalProjectionExpressionVisitorDependencies(ISqlTranslatingExpressionVisitorFactory sqlTranslatingExpressionVisitorFactory, IEntityMaterializerSource entityMaterializerSource);

-        public IEntityMaterializerSource EntityMaterializerSource { get; }

-        public ISqlTranslatingExpressionVisitorFactory SqlTranslatingExpressionVisitorFactory { get; }

-        public RelationalProjectionExpressionVisitorDependencies With(IEntityMaterializerSource entityMaterializerSource);

-        public RelationalProjectionExpressionVisitorDependencies With(ISqlTranslatingExpressionVisitorFactory sqlTranslatingExpressionVisitorFactory);

-    }
-    public class RelationalProjectionExpressionVisitorFactory : IProjectionExpressionVisitorFactory {
 {
-        public RelationalProjectionExpressionVisitorFactory(RelationalProjectionExpressionVisitorDependencies dependencies);

-        protected virtual RelationalProjectionExpressionVisitorDependencies Dependencies { get; }

-        public virtual ExpressionVisitor Create(EntityQueryModelVisitor entityQueryModelVisitor, IQuerySource querySource);

-    }
-    public class SqlTranslatingExpressionVisitor : ThrowingExpressionVisitor {
 {
-        public SqlTranslatingExpressionVisitor(SqlTranslatingExpressionVisitorDependencies dependencies, RelationalQueryModelVisitor queryModelVisitor, SelectExpression targetSelectExpression = null, Expression topLevelPredicate = null, bool inProjection = false);

-        public virtual Expression ClientEvalPredicate { get; private set; }

-        protected override Exception CreateUnhandledItemException<T>(T unhandledItem, string visitMethod);

-        public override Expression Visit(Expression expression);

-        protected override Expression VisitBinary(BinaryExpression expression);

-        protected override Expression VisitConditional(ConditionalExpression expression);

-        protected override Expression VisitConstant(ConstantExpression expression);

-        protected override Expression VisitExtension(Expression expression);

-        protected override Expression VisitMember(MemberExpression memberExpression);

-        protected override Expression VisitMethodCall(MethodCallExpression methodCallExpression);

-        protected override Expression VisitNew(NewExpression expression);

-        protected override Expression VisitParameter(ParameterExpression expression);

-        protected override Expression VisitQuerySourceReference(QuerySourceReferenceExpression expression);

-        protected override Expression VisitSubQuery(SubQueryExpression expression);

-        protected override Expression VisitUnary(UnaryExpression expression);

-        protected override TResult VisitUnhandledItem<TItem, TResult>(TItem unhandledItem, string visitMethod, Func<TItem, TResult> baseBehavior);

-    }
-    public sealed class SqlTranslatingExpressionVisitorDependencies {
 {
-        public SqlTranslatingExpressionVisitorDependencies(IExpressionFragmentTranslator compositeExpressionFragmentTranslator, ICompositeMethodCallTranslator methodCallTranslator, IMemberTranslator memberTranslator, IRelationalTypeMapper relationalTypeMapper, IRelationalTypeMappingSource typeMappingSource);

-        public IExpressionFragmentTranslator CompositeExpressionFragmentTranslator { get; }

-        public IMemberTranslator MemberTranslator { get; }

-        public ICompositeMethodCallTranslator MethodCallTranslator { get; }

-        public IRelationalTypeMapper RelationalTypeMapper { get; }

-        public IRelationalTypeMappingSource TypeMappingSource { get; }

-        public SqlTranslatingExpressionVisitorDependencies With(ICompositeMethodCallTranslator methodCallTranslator);

-        public SqlTranslatingExpressionVisitorDependencies With(IExpressionFragmentTranslator compositeExpressionFragmentTranslator);

-        public SqlTranslatingExpressionVisitorDependencies With(IMemberTranslator memberTranslator);

-        public SqlTranslatingExpressionVisitorDependencies With(IRelationalTypeMapper relationalTypeMapper);

-        public SqlTranslatingExpressionVisitorDependencies With(IRelationalTypeMappingSource typeMappingSource);

-    }
-    public class SqlTranslatingExpressionVisitorFactory : ISqlTranslatingExpressionVisitorFactory {
 {
-        public SqlTranslatingExpressionVisitorFactory(SqlTranslatingExpressionVisitorDependencies dependencies);

-        protected virtual SqlTranslatingExpressionVisitorDependencies Dependencies { get; }

-        public virtual SqlTranslatingExpressionVisitor Create(RelationalQueryModelVisitor queryModelVisitor, SelectExpression targetSelectExpression = null, Expression topLevelPredicate = null, bool inProjection = false);

-    }
-}
```

