# Microsoft.EntityFrameworkCore.Query.ExpressionVisitors.Internal

``` diff
-namespace Microsoft.EntityFrameworkCore.Query.ExpressionVisitors.Internal {
 {
-    public class AdditionalFromClauseOptimizingQueryModelVisitor : QueryModelVisitorBase {
 {
-        public AdditionalFromClauseOptimizingQueryModelVisitor(QueryCompilationContext queryCompilationContext);

-        public override void VisitAdditionalFromClause(AdditionalFromClause fromClause, QueryModel queryModel, int index);

-        public override void VisitQueryModel(QueryModel queryModel);

-    }
-    public class AllAnyToContainsRewritingExpressionVisitor : ExpressionVisitorBase {
 {
-        public AllAnyToContainsRewritingExpressionVisitor();

-        protected override Expression VisitSubQuery(SubQueryExpression expression);

-    }
-    public class BufferedEntityShaper<TEntity> : EntityShaper, IShaper<TEntity> where TEntity : class {
 {
-        public BufferedEntityShaper(IQuerySource querySource, bool trackingQuery, IKey key, Func<MaterializationContext, object> materializer, Dictionary<Type, int[]> typeIndexMap);

-        public override Type Type { get; }

-        public override IShaper<TDerived> Cast<TDerived>();

-        public virtual TEntity Shape(QueryContext queryContext, in ValueBuffer valueBuffer);

-        public override string ToString();

-        public override Shaper WithOffset(int offset);

-    }
-    public class BufferedOffsetEntityShaper<TEntity> : BufferedEntityShaper<TEntity> where TEntity : class {
 {
-        public BufferedOffsetEntityShaper(IQuerySource querySource, bool trackingQuery, IKey key, Func<MaterializationContext, object> materializer, Dictionary<Type, int[]> typeIndexMap);

-        public override TEntity Shape(QueryContext queryContext, in ValueBuffer valueBuffer);

-        public override string ToString();

-    }
-    public class CollectionNavigationIncludeExpressionRewriter : ExpressionVisitorBase {
 {
-        public static readonly MethodInfo ProjectCollectionNavigationMethodInfo;

-        public CollectionNavigationIncludeExpressionRewriter(EntityQueryModelVisitor queryModelVisitor);

-        public virtual List<IQueryAnnotation> CollectionNavigationIncludeResultOperators { get; }

-        protected override Expression VisitMember(MemberExpression node);

-        protected override Expression VisitMethodCall(MethodCallExpression node);

-        protected override Expression VisitSubQuery(SubQueryExpression subQueryExpression);

-    }
-    public class CollectionNavigationSetOperatorSubqueryInjector : CollectionNavigationSubqueryInjector {
 {
-        public CollectionNavigationSetOperatorSubqueryInjector(EntityQueryModelVisitor queryModelVisitor, bool shouldInject = false);

-        protected override Expression VisitSubQuery(SubQueryExpression expression);

-    }
-    public class CollectionNavigationSubqueryInjector : RelinqExpressionVisitor {
 {
-        public static readonly MethodInfo MaterializeCollectionNavigationMethodInfo;

-        public CollectionNavigationSubqueryInjector(EntityQueryModelVisitor queryModelVisitor, bool shouldInject = false);

-        protected virtual bool ShouldInject { get; set; }

-        protected override Expression VisitMember(MemberExpression memberExpression);

-        protected override Expression VisitMethodCall(MethodCallExpression methodCallExpression);

-    }
-    public class CompositePredicateExpressionVisitor : RelinqExpressionVisitor {
 {
-        public CompositePredicateExpressionVisitor(bool useRelationalNulls);

-        protected override Expression VisitExtension(Expression expression);

-    }
-    public class CompositePredicateExpressionVisitorFactory : ICompositePredicateExpressionVisitorFactory {
 {
-        public CompositePredicateExpressionVisitorFactory(IDbContextOptions contextOptions);

-        public virtual ExpressionVisitor Create();

-    }
-    public static class CompositeShaper {
 {
-        public static Shaper Create(IQuerySource querySource, Shaper outerShaper, Shaper innerShaper, Delegate materializer);

-        public static Shaper Create(IQuerySource querySource, Shaper outerShaper, Shaper innerShaper, LambdaExpression materializer, bool storeMaterializerExpression);

-    }
-    public class ConditionalOptimizingExpressionVisitor : ExpressionVisitorBase {
 {
-        public ConditionalOptimizingExpressionVisitor();

-        protected override Expression VisitConditional(ConditionalExpression conditionalExpression);

-    }
-    public class ConditionalRemovingExpressionVisitor : ExpressionVisitorBase {
 {
-        public ConditionalRemovingExpressionVisitor();

-        public override Expression Visit(Expression node);

-    }
-    public class ConditionalRemovingExpressionVisitorFactory : IConditionalRemovingExpressionVisitorFactory {
 {
-        public ConditionalRemovingExpressionVisitorFactory();

-        public virtual ExpressionVisitor Create();

-    }
-    public class CorrelatedCollectionFindingExpressionVisitor : RelinqExpressionVisitor {
 {
-        public CorrelatedCollectionFindingExpressionVisitor(EntityQueryModelVisitor queryModelVisitor, bool trackingQuery);

-        protected override Expression VisitMethodCall(MethodCallExpression node);

-        protected override Expression VisitSubQuery(SubQueryExpression expression);

-    }
-    public class CorrelatedCollectionOptimizingVisitor : ExpressionVisitorBase {
 {
-        public CorrelatedCollectionOptimizingVisitor(EntityQueryModelVisitor queryModelVisitor, QueryModel parentQueryModel);

-        public virtual IReadOnlyList<Ordering> ParentOrderings { get; }

-        public static bool IsCorrelatedCollectionMethod(MethodCallExpression methodCallExpression);

-        protected override Expression VisitMethodCall(MethodCallExpression methodCallExpression);

-        protected override Expression VisitSubQuery(SubQueryExpression subQueryExpression);

-    }
-    public class DefaultQueryExpressionVisitor : ExpressionVisitorBase {
 {
-        public static readonly MethodInfo GetParameterValueMethodInfo;

-        public DefaultQueryExpressionVisitor(EntityQueryModelVisitor entityQueryModelVisitor);

-        public virtual EntityQueryModelVisitor QueryModelVisitor { get; }

-        protected virtual EntityQueryModelVisitor CreateQueryModelVisitor();

-        protected override Expression VisitExtension(Expression node);

-        protected override Expression VisitParameter(ParameterExpression node);

-        protected override Expression VisitSubQuery(SubQueryExpression expression);

-    }
-    public class EagerLoadingExpressionVisitor : QueryModelVisitorBase {
 {
-        public EagerLoadingExpressionVisitor(QueryCompilationContext queryCompilationContext, IQuerySourceTracingExpressionVisitorFactory querySourceTracingExpressionVisitorFactory);

-        public virtual bool ShouldInclude(INavigation navigation);

-        protected override void VisitBodyClauses(ObservableCollection<IBodyClause> bodyClauses, QueryModel queryModel);

-        public override void VisitMainFromClause(MainFromClause fromClause, QueryModel queryModel);

-        public virtual void WalkNavigations(Expression querySourceReferenceExpression, IEntityType entityType, Stack<INavigation> stack);

-    }
-    public class EagerLoadingExpressionVisitorFactory : IEagerLoadingExpressionVisitorFactory {
 {
-        public EagerLoadingExpressionVisitorFactory();

-        public virtual EagerLoadingExpressionVisitor Create(QueryCompilationContext queryCompilationContext, IQuerySourceTracingExpressionVisitorFactory querySourceTracingExpressionVisitorFactory);

-    }
-    public class EntityEqualityRewritingExpressionVisitor : ExpressionVisitorBase {
 {
-        public EntityEqualityRewritingExpressionVisitor(QueryCompilationContext queryCompilationContext);

-        protected override Expression VisitBinary(BinaryExpression binaryExpression);

-        protected override Expression VisitMethodCall(MethodCallExpression methodCallExpression);

-    }
-    public class EntityResultFindingExpressionVisitor : ExpressionVisitorBase {
 {
-        public EntityResultFindingExpressionVisitor(IModel model, IEntityTrackingInfoFactory entityTrackingInfoFactory, QueryCompilationContext queryCompilationContext);

-        public virtual IReadOnlyCollection<EntityTrackingInfo> FindEntitiesInResult(Expression expression);

-        protected override Expression VisitBinary(BinaryExpression node);

-        protected override Expression VisitConditional(ConditionalExpression node);

-        protected override Expression VisitInvocation(InvocationExpression node);

-        protected override Expression VisitLambda<T>(Expression<T> node);

-        protected override Expression VisitMember(MemberExpression node);

-        protected override Expression VisitMethodCall(MethodCallExpression node);

-        protected override Expression VisitNew(NewExpression expression);

-        protected override Expression VisitQuerySourceReference(QuerySourceReferenceExpression expression);

-        protected override Expression VisitSubQuery(SubQueryExpression expression);

-        protected override Expression VisitTypeBinary(TypeBinaryExpression node);

-    }
-    public class EntityResultFindingExpressionVisitorFactory : IEntityResultFindingExpressionVisitorFactory {
 {
-        public EntityResultFindingExpressionVisitorFactory(IModel model, IEntityTrackingInfoFactory entityTrackingInfoFactory);

-        public virtual EntityResultFindingExpressionVisitor Create(QueryCompilationContext queryCompilationContext);

-    }
-    public abstract class EntityShaper : Shaper {
 {
-        protected EntityShaper(IQuerySource querySource, bool trackingQuery, IKey key, Func<MaterializationContext, object> materializer, Expression materializerExpression);

-        protected bool AllowNullResult { get; private set; }

-        protected bool IsTrackingQuery { get; }

-        protected IKey Key { get; }

-        protected Func<MaterializationContext, object> Materializer { get; }

-        public override Shaper AddOffset(int offset);

-        public abstract IShaper<TDerived> Cast<TDerived>() where TDerived : class;

-    }
-    public class EqualityPredicateExpandingVisitor : RelinqExpressionVisitor {
 {
-        public EqualityPredicateExpandingVisitor();

-        protected override Expression VisitBinary(BinaryExpression node);

-    }
-    public class EqualityPredicateInExpressionOptimizer : RelinqExpressionVisitor {
 {
-        public EqualityPredicateInExpressionOptimizer();

-        protected override Expression VisitBinary(BinaryExpression binaryExpression);

-    }
-    public class ExistsToAnyRewritingExpressionVisitor : ExpressionVisitorBase {
 {
-        public ExistsToAnyRewritingExpressionVisitor();

-        protected override Expression VisitMethodCall(MethodCallExpression methodCallExpression);

-    }
-    public class ExpressionTransformingQueryModelVisitor<TVisitor> : QueryModelVisitorBase where TVisitor : RelinqExpressionVisitor {
 {
-        public ExpressionTransformingQueryModelVisitor(TVisitor transformingVisitor);

-        protected virtual TVisitor TransformingVisitor { get; }

-        public override void VisitAdditionalFromClause(AdditionalFromClause fromClause, QueryModel queryModel, int index);

-        public override void VisitJoinClause(JoinClause joinClause, QueryModel queryModel, GroupJoinClause groupJoinClause);

-        public override void VisitJoinClause(JoinClause joinClause, QueryModel queryModel, int index);

-        public override void VisitMainFromClause(MainFromClause fromClause, QueryModel queryModel);

-        public override void VisitOrderByClause(OrderByClause orderByClause, QueryModel queryModel, int index);

-        public override void VisitOrdering(Ordering ordering, QueryModel queryModel, OrderByClause orderByClause, int index);

-        public override void VisitResultOperator(ResultOperatorBase resultOperator, QueryModel queryModel, int index);

-        public override void VisitSelectClause(SelectClause selectClause, QueryModel queryModel);

-        public override void VisitWhereClause(WhereClause whereClause, QueryModel queryModel, int index);

-    }
-    public interface ICompositePredicateExpressionVisitorFactory {
 {
-        ExpressionVisitor Create();

-    }
-    public interface IConditionalRemovingExpressionVisitorFactory {
 {
-        ExpressionVisitor Create();

-    }
-    public sealed class IdentityShaper : IShaper<ValueBuffer> {
 {
-        public static readonly IdentityShaper Instance;

-        public Expression GetAccessorExpression(IQuerySource querySource);

-        public bool IsShaperForQuerySource(IQuerySource querySource);

-        ValueBuffer Microsoft.EntityFrameworkCore.Query.ExpressionVisitors.Internal.IShaper<Microsoft.EntityFrameworkCore.Storage.ValueBuffer>.Shape(QueryContext queryContext, in ValueBuffer valueBuffer);

-        public void SaveAccessorExpression(QuerySourceMapping querySourceMapping);

-        public ValueBuffer Shape(QueryContext queryContext, in ValueBuffer valueBuffer);

-    }
-    public interface IEagerLoadingExpressionVisitorFactory {
 {
-        EagerLoadingExpressionVisitor Create(QueryCompilationContext queryCompilationContext, IQuerySourceTracingExpressionVisitorFactory querySourceTracingExpressionVisitorFactory);

-    }
-    public interface IEntityResultFindingExpressionVisitorFactory {
 {
-        EntityResultFindingExpressionVisitor Create(QueryCompilationContext queryCompilationContext);

-    }
-    public interface IMaterializerFactory {
 {
-        LambdaExpression CreateMaterializer(IEntityType entityType, SelectExpression selectExpression, Func<IProperty, SelectExpression, int> projectionAdder, out Dictionary<Type, int[]> typeIndexMap);

-    }
-    public interface IMemberAccessBindingExpressionVisitorFactory {
 {
-        ExpressionVisitor Create(QuerySourceMapping querySourceMapping, EntityQueryModelVisitor queryModelVisitor, bool inProjection);

-    }
-    public interface INavigationRewritingExpressionVisitorFactory {
 {
-        NavigationRewritingExpressionVisitor Create(EntityQueryModelVisitor queryModelVisitor);

-    }
-    public interface IQuerySourceTracingExpressionVisitorFactory {
 {
-        QuerySourceTracingExpressionVisitor Create();

-    }
-    public interface IRequiresMaterializationExpressionVisitorFactory {
 {
-        RequiresMaterializationExpressionVisitor Create(EntityQueryModelVisitor queryModelVisitor);

-    }
-    public interface IShaper<out T> {
 {
-        Expression GetAccessorExpression(IQuerySource querySource);

-        bool IsShaperForQuerySource(IQuerySource querySource);

-        void SaveAccessorExpression(QuerySourceMapping querySourceMapping);

-        T Shape(QueryContext queryContext, in ValueBuffer valueBuffer);

-    }
-    public class IsNullExpressionBuildingVisitor : RelinqExpressionVisitor {
 {
-        public IsNullExpressionBuildingVisitor();

-        public virtual Expression ResultExpression { get; private set; }

-        protected override Expression VisitBinary(BinaryExpression binaryExpression);

-        protected override Expression VisitConditional(ConditionalExpression conditionalExpression);

-        protected override Expression VisitConstant(ConstantExpression constantExpression);

-        protected override Expression VisitExtension(Expression extensionExpression);

-    }
-    public interface ITaskBlockingExpressionVisitor {
 {
-        Expression Visit(Expression expression);

-    }
-    public class MaterializerFactory : IMaterializerFactory {
 {
-        public MaterializerFactory(IEntityMaterializerSource entityMaterializerSource);

-        public virtual LambdaExpression CreateMaterializer(IEntityType entityType, SelectExpression selectExpression, Func<IProperty, SelectExpression, int> projectionAdder, out Dictionary<Type, int[]> typeIndexMap);

-    }
-    public class MemberAccessBindingExpressionVisitor : RelinqExpressionVisitor {
 {
-        public MemberAccessBindingExpressionVisitor(QuerySourceMapping querySourceMapping, EntityQueryModelVisitor queryModelVisitor, bool inProjection);

-        public static IEntityType GetEntityType(Expression expression, QueryCompilationContext queryCompilationContext);

-        public static List<IPropertyBase> GetPropertyPath(Expression expression, QueryCompilationContext queryCompilationContext, out QuerySourceReferenceExpression querySourceReferenceExpression);

-        protected override Expression VisitBinary(BinaryExpression node);

-        protected override Expression VisitExtension(Expression node);

-        protected override Expression VisitMember(MemberExpression node);

-        protected override Expression VisitMethodCall(MethodCallExpression methodCallExpression);

-        protected override Expression VisitNew(NewExpression expression);

-        protected override Expression VisitQuerySourceReference(QuerySourceReferenceExpression expression);

-        protected override Expression VisitSubQuery(SubQueryExpression expression);

-        protected override Expression VisitUnary(UnaryExpression node);

-    }
-    public class MemberAccessBindingExpressionVisitorFactory : IMemberAccessBindingExpressionVisitorFactory {
 {
-        public MemberAccessBindingExpressionVisitorFactory();

-        public virtual ExpressionVisitor Create(QuerySourceMapping querySourceMapping, EntityQueryModelVisitor queryModelVisitor, bool inProjection);

-    }
-    public class ModelExpressionApplyingExpressionVisitor : RelinqExpressionVisitor {
 {
-        public ModelExpressionApplyingExpressionVisitor(QueryCompilationContext queryCompilationContext, IQueryModelGenerator queryModelGenerator, EntityQueryModelVisitor entityQueryModelVisitor);

-        public virtual IReadOnlyDictionary<string, object> ContextParameters { get; }

-        public virtual bool IsViewTypeQuery { get; private set; }

-        public virtual void ApplyModelExpressions(QueryModel queryModel);

-        protected override Expression VisitConstant(ConstantExpression constantExpression);

-        protected override Expression VisitSubQuery(SubQueryExpression subQueryExpression);

-    }
-    public class NavigationRewritingExpressionVisitor : RelinqExpressionVisitor {
 {
-        public NavigationRewritingExpressionVisitor(EntityQueryModelVisitor queryModelVisitor);

-        public NavigationRewritingExpressionVisitor(EntityQueryModelVisitor queryModelVisitor, bool navigationExpansionSubquery);

-        protected virtual QueryModel ParentQueryModel { get; set; }

-        protected virtual QueryModel QueryModel { get; set; }

-        protected virtual EntityQueryModelVisitor QueryModelVisitor { get; }

-        public virtual NavigationRewritingExpressionVisitor CreateVisitorForSubQuery(bool navigationExpansionSubquery);

-        public virtual void InjectSubqueryToCollectionsInProjection(QueryModel queryModel);

-        public virtual void Rewrite(QueryModel queryModel, QueryModel parentQueryModel);

-        protected virtual bool ShouldRewrite(INavigation navigation);

-        protected override Expression VisitBinary(BinaryExpression node);

-        protected override Expression VisitConditional(ConditionalExpression node);

-        protected override ElementInit VisitElementInit(ElementInit node);

-        protected override Expression VisitMember(MemberExpression node);

-        protected override MemberAssignment VisitMemberAssignment(MemberAssignment node);

-        protected override Expression VisitMethodCall(MethodCallExpression node);

-        protected override Expression VisitNewArray(NewArrayExpression node);

-        protected override Expression VisitSubQuery(SubQueryExpression expression);

-        protected override Expression VisitUnary(UnaryExpression node);

-    }
-    public class NavigationRewritingExpressionVisitorFactory : INavigationRewritingExpressionVisitorFactory {
 {
-        public NavigationRewritingExpressionVisitorFactory();

-        public virtual NavigationRewritingExpressionVisitor Create(EntityQueryModelVisitor queryModelVisitor);

-    }
-    public class ParameterExtractingExpressionVisitor : ExpressionVisitor {
 {
-        public ParameterExtractingExpressionVisitor(IEvaluatableExpressionFilter evaluatableExpressionFilter, IParameterValues parameterValues, IDiagnosticsLogger<DbLoggerCategory.Query> logger, DbContext context, bool parameterize, bool generateContextAccessors = false);

-        public virtual object Evaluate(Expression expression, out string parameterName);

-        public virtual Expression ExtractParameters(Expression expression);

-        protected override Expression VisitBinary(BinaryExpression binaryExpression);

-        protected override Expression VisitConditional(ConditionalExpression conditionalExpression);

-        protected override Expression VisitConstant(ConstantExpression constantExpression);

-        protected override Expression VisitLambda<T>(Expression<T> node);

-        protected override Expression VisitMember(MemberExpression memberExpression);

-        protected override Expression VisitMethodCall(MethodCallExpression methodCallExpression);

-        protected override Expression VisitNew(NewExpression node);

-        protected override Expression VisitUnary(UnaryExpression unaryExpression);

-    }
-    public class PredicateNegationExpressionOptimizer : RelinqExpressionVisitor {
 {
-        public PredicateNegationExpressionOptimizer();

-        protected override Expression VisitBinary(BinaryExpression binaryExpression);

-        protected override Expression VisitUnary(UnaryExpression unaryExpression);

-    }
-    public class PredicateReductionExpressionOptimizer : RelinqExpressionVisitor {
 {
-        public PredicateReductionExpressionOptimizer();

-        protected override Expression VisitBinary(BinaryExpression node);

-    }
-    public class ProjectionExpressionVisitorFactory : IProjectionExpressionVisitorFactory {
 {
-        public ProjectionExpressionVisitorFactory();

-        public virtual ExpressionVisitor Create(EntityQueryModelVisitor entityQueryModelVisitor, IQuerySource querySource);

-    }
-    public static class ProjectionShaper {
 {
-        public static Shaper Create(Shaper originalShaper, LambdaExpression materializer, bool storeMaterializerExpression);

-    }
-    public class QuerySourceReferenceFindingExpressionVisitor : RelinqExpressionVisitor {
 {
-        public QuerySourceReferenceFindingExpressionVisitor();

-        public virtual QuerySourceReferenceExpression QuerySourceReferenceExpression { get; private set; }

-        protected override Expression VisitQuerySourceReference(QuerySourceReferenceExpression querySourceReferenceExpression);

-    }
-    public class QuerySourceTracingExpressionVisitor : ExpressionVisitorBase {
 {
-        public QuerySourceTracingExpressionVisitor();

-        public virtual QueryModel OriginGroupByQueryModel { get; }

-        public virtual QuerySourceReferenceExpression FindResultQuerySourceReferenceExpression(Expression expression, IQuerySource targetQuerySource);

-        protected override Expression VisitBinary(BinaryExpression node);

-        protected override Expression VisitConditional(ConditionalExpression node);

-        protected override Expression VisitInvocation(InvocationExpression node);

-        protected override Expression VisitLambda<T>(Expression<T> node);

-        protected override Expression VisitMember(MemberExpression node);

-        protected override Expression VisitMethodCall(MethodCallExpression node);

-        protected override Expression VisitQuerySourceReference(QuerySourceReferenceExpression expression);

-        protected override Expression VisitSubQuery(SubQueryExpression subQueryExpression);

-        protected override Expression VisitTypeBinary(TypeBinaryExpression node);

-    }
-    public class QuerySourceTracingExpressionVisitorFactory : IQuerySourceTracingExpressionVisitorFactory {
 {
-        public QuerySourceTracingExpressionVisitorFactory();

-        public virtual QuerySourceTracingExpressionVisitor Create();

-    }
-    public class ReducingExpressionVisitor : ExpressionVisitorBase {
 {
-        public ReducingExpressionVisitor();

-        public override Expression Visit(Expression node);

-        protected override Expression VisitSubQuery(SubQueryExpression expression);

-    }
-    public class RelationalNullsExpandingVisitor : RelationalNullsExpressionVisitorBase {
 {
-        public RelationalNullsExpandingVisitor();

-        protected override Expression VisitBinary(BinaryExpression node);

-        protected override Expression VisitExtension(Expression node);

-    }
-    public abstract class RelationalNullsExpressionVisitorBase : RelinqExpressionVisitor {
 {
-        protected RelationalNullsExpressionVisitorBase();

-        protected virtual Expression BuildIsNullExpression(Expression expression);

-        protected override Expression VisitExtension(Expression node);

-    }
-    public class RelationalNullsOptimizedExpandingVisitor : RelationalNullsExpressionVisitorBase {
 {
-        public RelationalNullsOptimizedExpandingVisitor();

-        public virtual bool IsOptimalExpansion { get; private set; }

-        protected override Expression VisitBinary(BinaryExpression node);

-        protected override Expression VisitUnary(UnaryExpression node);

-    }
-    public class RequiresMaterializationExpressionVisitor : ExpressionVisitorBase {
 {
-        public RequiresMaterializationExpressionVisitor(IModel model, EntityQueryModelVisitor queryModelVisitor);

-        public virtual ISet<IQuerySource> FindQuerySourcesRequiringMaterialization(QueryModel queryModel);

-        public static IEnumerable<Expression> GetSetResultOperatorSourceExpressions(IEnumerable<ResultOperatorBase> resultOperators);

-        public static void HandleUnderlyingQuerySources(IQuerySource querySource, Action<IQuerySource> action);

-        protected override Expression VisitBinary(BinaryExpression node);

-        protected override Expression VisitExtension(Expression node);

-        protected override Expression VisitMember(MemberExpression node);

-        protected override Expression VisitMethodCall(MethodCallExpression node);

-        protected override Expression VisitQuerySourceReference(QuerySourceReferenceExpression expression);

-        protected override Expression VisitSubQuery(SubQueryExpression expression);

-    }
-    public class RequiresMaterializationExpressionVisitorFactory : IRequiresMaterializationExpressionVisitorFactory {
 {
-        public RequiresMaterializationExpressionVisitorFactory(IModel model);

-        public virtual RequiresMaterializationExpressionVisitor Create(EntityQueryModelVisitor queryModelVisitor);

-    }
-    public class ResultTransformingExpressionVisitor<TResult> : ExpressionVisitorBase {
 {
-        public ResultTransformingExpressionVisitor(RelationalQueryCompilationContext relationalQueryCompilationContext, bool throwOnNullResult);

-        protected override Expression VisitMethodCall(MethodCallExpression node);

-    }
-    public abstract class Shaper {
 {
-        protected Shaper(IQuerySource querySource, Expression materializerExpression);

-        public virtual Expression MaterializerExpression { get; }

-        public virtual IQuerySource QuerySource { get; }

-        public abstract Type Type { get; }

-        public virtual int ValueBufferOffset { get; private set; }

-        public virtual Shaper AddOffset(int offset);

-        public virtual Expression GetAccessorExpression(IQuerySource querySource);

-        public virtual bool IsShaperForQuerySource(IQuerySource querySource);

-        public virtual void SaveAccessorExpression(QuerySourceMapping querySourceMapping);

-        public virtual Shaper Unwrap(IQuerySource querySource);

-        public virtual void UpdateQuerySource(IQuerySource querySource);

-        public abstract Shaper WithOffset(int offset);

-    }
-    public class SubQueryMemberPushDownExpressionVisitor : ExpressionVisitorBase {
 {
-        public SubQueryMemberPushDownExpressionVisitor(QueryCompilationContext queryCompilationContext);

-        protected override Expression VisitMember(MemberExpression memberExpression);

-        protected override Expression VisitMethodCall(MethodCallExpression methodCallExpression);

-        protected override Expression VisitUnary(UnaryExpression unaryExpression);

-    }
-    public class SubqueryProjectingSingleValueOptimizingExpressionVisitor : ExpressionVisitorBase {
 {
-        public SubqueryProjectingSingleValueOptimizingExpressionVisitor();

-        protected override Expression VisitSubQuery(SubQueryExpression subQueryExpression);

-    }
-    public class TaskBlockingExpressionVisitor : ExpressionVisitorBase, ITaskBlockingExpressionVisitor {
 {
-        public TaskBlockingExpressionVisitor();

-        public override Expression Visit(Expression expression);

-    }
-    public class TaskLiftingExpressionVisitor : RelinqExpressionVisitor {
 {
-        public TaskLiftingExpressionVisitor();

-        public virtual ParameterExpression CancellationTokenParameter { get; private set; }

-        public virtual Expression LiftTasks(Expression expression);

-        protected override Expression VisitBlock(BlockExpression node);

-        protected override Expression VisitLambda<T>(Expression<T> node);

-        protected override Expression VisitMember(MemberExpression memberExpression);

-        protected override Expression VisitMethodCall(MethodCallExpression methodCallExpression);

-        protected override Expression VisitParameter(ParameterExpression parameterExpression);

-    }
-    public class TransformingQueryModelExpressionVisitor<TVisitor> : ExpressionVisitorBase where TVisitor : IQueryModelVisitor {
 {
-        public TransformingQueryModelExpressionVisitor(TVisitor transformingQueryModelVisitor);

-        protected virtual TVisitor TransformingQueryModelVisitor { get; }

-        protected override Expression VisitSubQuery(SubQueryExpression expression);

-    }
-    public class UnbufferedEntityShaper<TEntity> : EntityShaper, IShaper<TEntity> where TEntity : class {
 {
-        public UnbufferedEntityShaper(IQuerySource querySource, bool trackingQuery, IKey key, Func<MaterializationContext, object> materializer, Expression materializerExpression);

-        public override Type Type { get; }

-        public override IShaper<TDerived> Cast<TDerived>();

-        public virtual TEntity Shape(QueryContext queryContext, in ValueBuffer valueBuffer);

-        public override string ToString();

-        public override Shaper WithOffset(int offset);

-    }
-    public class UnbufferedOffsetEntityShaper<TEntity> : UnbufferedEntityShaper<TEntity> where TEntity : class {
 {
-        public UnbufferedOffsetEntityShaper(IQuerySource querySource, bool trackingQuery, IKey key, Func<MaterializationContext, object> materializer);

-        public override TEntity Shape(QueryContext queryContext, in ValueBuffer valueBuffer);

-        public override string ToString();

-    }
-    public class ValueBufferShaper : Shaper, IShaper<ValueBuffer> {
 {
-        public ValueBufferShaper(IQuerySource querySource);

-        public override Type Type { get; }

-        public virtual ValueBuffer Shape(QueryContext queryContext, in ValueBuffer valueBuffer);

-        public override Shaper WithOffset(int offset);

-    }
-}
```

