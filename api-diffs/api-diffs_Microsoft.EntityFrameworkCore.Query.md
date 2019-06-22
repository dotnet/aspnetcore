# Microsoft.EntityFrameworkCore.Query

``` diff
-namespace Microsoft.EntityFrameworkCore.Query {
 {
-    public readonly struct AsyncEnumerable<TResult> : IAsyncEnumerableAccessor<TResult> {
 {
-        public AsyncEnumerable(IAsyncEnumerable<TResult> asyncEnumerable);

-        IAsyncEnumerable<TResult> Microsoft.EntityFrameworkCore.Query.Internal.IAsyncEnumerableAccessor<TResult>.AsyncEnumerable { get; }

-        public Task ForEachAsync(Action<TResult> action, CancellationToken cancellationToken = default(CancellationToken));

-        public Task LoadAsync(CancellationToken cancellationToken = default(CancellationToken));

-        public Task<TResult[]> ToArrayAsync(CancellationToken cancellationToken = default(CancellationToken));

-        public Task<Dictionary<TKey, TElement>> ToDictionaryAsync<TKey, TElement>(Func<TResult, TKey> keySelector, Func<TResult, TElement> elementSelector, IEqualityComparer<TKey> comparer, CancellationToken cancellationToken = default(CancellationToken));

-        public Task<Dictionary<TKey, TElement>> ToDictionaryAsync<TKey, TElement>(Func<TResult, TKey> keySelector, Func<TResult, TElement> elementSelector, CancellationToken cancellationToken = default(CancellationToken));

-        public Task<Dictionary<TKey, TResult>> ToDictionaryAsync<TKey>(Func<TResult, TKey> keySelector, IEqualityComparer<TKey> comparer, CancellationToken cancellationToken = default(CancellationToken));

-        public Task<Dictionary<TKey, TResult>> ToDictionaryAsync<TKey>(Func<TResult, TKey> keySelector, CancellationToken cancellationToken = default(CancellationToken));

-        public Task<List<TResult>> ToListAsync(CancellationToken cancellationToken = default(CancellationToken));

-    }
-    public class AsyncQueryMethodProvider : IQueryMethodProvider {
 {
-        public AsyncQueryMethodProvider();

-        public virtual MethodInfo DefaultIfEmptyShapedQueryMethod { get; }

-        public virtual MethodInfo FastQueryMethod { get; }

-        public virtual MethodInfo GetResultMethod { get; }

-        public virtual MethodInfo GroupByMethod { get; }

-        public virtual MethodInfo GroupJoinMethod { get; }

-        public virtual MethodInfo InjectParametersMethod { get; }

-        public virtual MethodInfo QueryMethod { get; }

-        public virtual MethodInfo ShapedQueryMethod { get; }

-    }
-    public class CompiledQueryCacheKeyGenerator : ICompiledQueryCacheKeyGenerator {
 {
-        public CompiledQueryCacheKeyGenerator(CompiledQueryCacheKeyGeneratorDependencies dependencies);

-        protected virtual CompiledQueryCacheKeyGeneratorDependencies Dependencies { get; }

-        public virtual object GenerateCacheKey(Expression query, bool async);

-        protected CompiledQueryCacheKeyGenerator.CompiledQueryCacheKey GenerateCacheKeyCore(Expression query, bool async);

-        protected readonly struct CompiledQueryCacheKey {
 {
-            public CompiledQueryCacheKey(Expression query, IModel model, QueryTrackingBehavior queryTrackingBehavior, bool async);

-            public override bool Equals(object obj);

-            public override int GetHashCode();

-        }
-    }
-    public sealed class CompiledQueryCacheKeyGeneratorDependencies {
 {
-        public CompiledQueryCacheKeyGeneratorDependencies(IModel model, ICurrentDbContext currentContext);

-        public ICurrentDbContext Context { get; }

-        public IModel Model { get; }

-        public CompiledQueryCacheKeyGeneratorDependencies With(ICurrentDbContext currentContext);

-        public CompiledQueryCacheKeyGeneratorDependencies With(IModel model);

-    }
-    public readonly struct EntityLoadInfo {
 {
-        public EntityLoadInfo(in MaterializationContext materializationContext, Func<MaterializationContext, object> materializer, Dictionary<Type, int[]> typeIndexMap = null);

-        public EntityLoadInfo(ValueBuffer valueBuffer, Func<ValueBuffer, object> materializer, Dictionary<Type, int[]> typeIndexMap = null);

-        public ValueBuffer ValueBuffer { get; }

-        public ValueBuffer ForType(Type clrType);

-        public object Materialize();

-    }
-    public abstract class EntityQueryModelVisitor : QueryModelVisitorBase {
 {
-        public static readonly ParameterExpression QueryContextParameter;

-        protected EntityQueryModelVisitor(EntityQueryModelVisitorDependencies dependencies, QueryCompilationContext queryCompilationContext);

-        public virtual ParameterExpression CurrentParameter { get; set; }

-        public virtual Expression Expression { get; set; }

-        public virtual ILinqOperatorProvider LinqOperatorProvider { get; private set; }

-        public virtual QueryCompilationContext QueryCompilationContext { get; }

-        public virtual void BindMemberExpression(MemberExpression memberExpression, Action<IProperty, IQuerySource> memberBinder);

-        public virtual TResult BindMemberExpression<TResult>(MemberExpression memberExpression, IQuerySource querySource, Func<IProperty, IQuerySource, TResult> memberBinder);

-        public virtual Expression BindMemberToValueBuffer(MemberExpression memberExpression, Expression expression);

-        public virtual void BindMethodCallExpression(MethodCallExpression methodCallExpression, Action<IProperty, IQuerySource> methodCallBinder);

-        public virtual TResult BindMethodCallExpression<TResult>(MethodCallExpression methodCallExpression, IQuerySource querySource, Func<IProperty, IQuerySource, TResult> methodCallBinder);

-        public virtual TResult BindMethodCallExpression<TResult>(MethodCallExpression methodCallExpression, Func<IProperty, IQuerySource, TResult> methodCallBinder);

-        public virtual Expression BindMethodCallToEntity(MethodCallExpression methodCallExpression, MethodCallExpression targetMethodCallExpression);

-        public virtual Expression BindMethodCallToValueBuffer(MethodCallExpression methodCallExpression, Expression expression);

-        public virtual TResult BindNavigationPathPropertyExpression<TResult>(Expression propertyExpression, Func<IReadOnlyList<IPropertyBase>, IQuerySource, TResult> propertyBinder);

-        public virtual Expression BindReadValueMethod(Type memberType, Expression expression, int index, IProperty property = null);

-        protected virtual Expression CallCreateTransparentIdentifier(Type transparentIdentifierType, Expression outerExpression, Expression innerExpression);

-        protected virtual bool CanOptimizeCorrelatedCollections();

-        protected virtual Expression CompileAdditionalFromClauseExpression(AdditionalFromClause additionalFromClause, QueryModel queryModel);

-        protected virtual Expression CompileGroupJoinInnerSequenceExpression(GroupJoinClause groupJoinClause, QueryModel queryModel);

-        protected virtual Expression CompileJoinClauseInnerSequenceExpression(JoinClause joinClause, QueryModel queryModel);

-        protected virtual Expression CompileMainFromClauseExpression(MainFromClause mainFromClause, QueryModel queryModel);

-        public virtual Func<QueryContext, IAsyncEnumerable<TResult>> CreateAsyncQueryExecutor<TResult>(QueryModel queryModel);

-        protected virtual Func<QueryContext, TResults> CreateExecutorLambda<TResults>();

-        public virtual Func<QueryContext, IEnumerable<TResult>> CreateQueryExecutor<TResult>(QueryModel queryModel);

-        protected virtual Type CreateTransparentIdentifierType(Type outerType, Type innerType);

-        protected virtual void ExtractQueryAnnotations(QueryModel queryModel);

-        protected virtual void InterceptExceptions();

-        protected virtual void IntroduceTransparentScope(IQuerySource querySource, QueryModel queryModel, int index, Type transparentIdentifierType);

-        protected virtual void OnBeforeNavigationRewrite(QueryModel queryModel);

-        protected virtual void OptimizeQueryModel(QueryModel queryModel, bool asyncQuery);

-        protected virtual void RemoveOrderings(QueryModel queryModel);

-        public virtual Expression ReplaceClauseReferences(Expression expression, IQuerySource querySource = null, bool inProjection = false);

-        protected virtual void RewriteProjectedCollectionNavigationsToIncludes(QueryModel queryModel);

-        public virtual bool ShouldApplyDefiningQuery(IEntityType entityType, IQuerySource querySource);

-        protected virtual void SingleResultToSequence(QueryModel queryModel, Type type = null);

-        protected virtual void TrackEntitiesInResults<TResult>(QueryModel queryModel);

-        public override void VisitAdditionalFromClause(AdditionalFromClause fromClause, QueryModel queryModel, int index);

-        public override void VisitGroupJoinClause(GroupJoinClause groupJoinClause, QueryModel queryModel, int index);

-        public override void VisitJoinClause(JoinClause joinClause, QueryModel queryModel, int index);

-        public override void VisitMainFromClause(MainFromClause fromClause, QueryModel queryModel);

-        public override void VisitOrdering(Ordering ordering, QueryModel queryModel, OrderByClause orderByClause, int index);

-        public override void VisitQueryModel(QueryModel queryModel);

-        public override void VisitResultOperator(ResultOperatorBase resultOperator, QueryModel queryModel, int index);

-        public override void VisitSelectClause(SelectClause selectClause, QueryModel queryModel);

-        public override void VisitWhereClause(WhereClause whereClause, QueryModel queryModel, int index);

-    }
-    public sealed class EntityQueryModelVisitorDependencies {
 {
-        public EntityQueryModelVisitorDependencies(IQueryOptimizer queryOptimizer, INavigationRewritingExpressionVisitorFactory navigationRewritingExpressionVisitorFactory, IQuerySourceTracingExpressionVisitorFactory querySourceTracingExpressionVisitorFactory, IEntityResultFindingExpressionVisitorFactory entityResultFindingExpressionVisitorFactory, IEagerLoadingExpressionVisitorFactory eagerLoadingExpressionVisitorFactory, ITaskBlockingExpressionVisitor taskBlockingExpressionVisitor, IMemberAccessBindingExpressionVisitorFactory memberAccessBindingExpressionVisitorFactory, IProjectionExpressionVisitorFactory projectionExpressionVisitorFactory, IEntityQueryableExpressionVisitorFactory entityQueryableExpressionVisitorFactory, IQueryAnnotationExtractor queryAnnotationExtractor, IResultOperatorHandler resultOperatorHandler, IEntityMaterializerSource entityMaterializerSource, IExpressionPrinter expressionPrinter, IQueryModelGenerator queryModelGenerator);

-        public IEagerLoadingExpressionVisitorFactory EagerLoadingExpressionVisitorFactory { get; }

-        public IEntityMaterializerSource EntityMaterializerSource { get; }

-        public IEntityQueryableExpressionVisitorFactory EntityQueryableExpressionVisitorFactory { get; }

-        public IEntityResultFindingExpressionVisitorFactory EntityResultFindingExpressionVisitorFactory { get; }

-        public IExpressionPrinter ExpressionPrinter { get; }

-        public IMemberAccessBindingExpressionVisitorFactory MemberAccessBindingExpressionVisitorFactory { get; }

-        public INavigationRewritingExpressionVisitorFactory NavigationRewritingExpressionVisitorFactory { get; }

-        public IProjectionExpressionVisitorFactory ProjectionExpressionVisitorFactory { get; }

-        public IQueryAnnotationExtractor QueryAnnotationExtractor { get; }

-        public IQueryModelGenerator QueryModelGenerator { get; }

-        public IQueryOptimizer QueryOptimizer { get; }

-        public IQuerySourceTracingExpressionVisitorFactory QuerySourceTracingExpressionVisitorFactory { get; }

-        public IResultOperatorHandler ResultOperatorHandler { get; }

-        public ITaskBlockingExpressionVisitor TaskBlockingExpressionVisitor { get; }

-        public EntityQueryModelVisitorDependencies With(IEntityMaterializerSource entityMaterializerSource);

-        public EntityQueryModelVisitorDependencies With(IEntityQueryableExpressionVisitorFactory entityQueryableExpressionVisitorFactory);

-        public EntityQueryModelVisitorDependencies With(IEagerLoadingExpressionVisitorFactory eagerLoadingExpressionVisitorFactory);

-        public EntityQueryModelVisitorDependencies With(IEntityResultFindingExpressionVisitorFactory entityResultFindingExpressionVisitorFactory);

-        public EntityQueryModelVisitorDependencies With(IMemberAccessBindingExpressionVisitorFactory memberAccessBindingExpressionVisitorFactory);

-        public EntityQueryModelVisitorDependencies With(INavigationRewritingExpressionVisitorFactory navigationRewritingExpressionVisitorFactory);

-        public EntityQueryModelVisitorDependencies With(IQuerySourceTracingExpressionVisitorFactory querySourceTracingExpressionVisitorFactory);

-        public EntityQueryModelVisitorDependencies With(ITaskBlockingExpressionVisitor taskBlockingExpressionVisitor);

-        public EntityQueryModelVisitorDependencies With(IProjectionExpressionVisitorFactory projectionExpressionVisitorFactory);

-        public EntityQueryModelVisitorDependencies With(IExpressionPrinter expressionPrinter);

-        public EntityQueryModelVisitorDependencies With(IQueryAnnotationExtractor queryAnnotationExtractor);

-        public EntityQueryModelVisitorDependencies With(IQueryModelGenerator queryModelGenerator);

-        public EntityQueryModelVisitorDependencies With(IQueryOptimizer queryOptimizer);

-        public EntityQueryModelVisitorDependencies With(IResultOperatorHandler resultOperatorHandler);

-    }
-    public abstract class EntityQueryModelVisitorFactory : IEntityQueryModelVisitorFactory {
 {
-        protected EntityQueryModelVisitorFactory(EntityQueryModelVisitorDependencies dependencies);

-        protected virtual EntityQueryModelVisitorDependencies Dependencies { get; }

-        public abstract EntityQueryModelVisitor Create(QueryCompilationContext queryCompilationContext, EntityQueryModelVisitor parentEntityQueryModelVisitor);

-    }
-    public interface ICompiledQueryCacheKeyGenerator {
 {
-        object GenerateCacheKey(Expression query, bool async);

-    }
-    public interface IEntityQueryModelVisitorFactory {
 {
-        EntityQueryModelVisitor Create(QueryCompilationContext queryCompilationContext, EntityQueryModelVisitor parentEntityQueryModelVisitor);

-    }
-    public interface IIncludableQueryable<out TEntity, out TProperty> : IEnumerable, IEnumerable<TEntity>, IQueryable, IQueryable<TEntity>

-    public class IncludeSpecification {
 {
-        public IncludeSpecification(IQuerySource querySource, IReadOnlyList<INavigation> navigationPath);

-        public virtual bool IsEnumerableTarget { get; set; }

-        public virtual IReadOnlyList<INavigation> NavigationPath { get; }

-        public virtual IQuerySource QuerySource { get; }

-        public override string ToString();

-    }
-    public interface IQueryCompilationContextFactory {
 {
-        QueryCompilationContext Create(bool async);

-    }
-    public interface IQueryContextFactory {
 {
-        QueryContext Create();

-    }
-    public interface IQueryMethodProvider {
 {
-        MethodInfo DefaultIfEmptyShapedQueryMethod { get; }

-        MethodInfo FastQueryMethod { get; }

-        MethodInfo GetResultMethod { get; }

-        MethodInfo GroupByMethod { get; }

-        MethodInfo GroupJoinMethod { get; }

-        MethodInfo InjectParametersMethod { get; }

-        MethodInfo QueryMethod { get; }

-        MethodInfo ShapedQueryMethod { get; }

-    }
-    public interface IRelationalResultOperatorHandler : IResultOperatorHandler

-    public interface IResultOperatorHandler {
 {
-        Expression HandleResultOperator(EntityQueryModelVisitor entityQueryModelVisitor, ResultOperatorBase resultOperator, QueryModel queryModel);

-    }
-    public sealed class NotParameterizedAttribute : Attribute {
 {
-        public NotParameterizedAttribute();

-    }
-    public class QueryCompilationContext {
 {
-        public QueryCompilationContext(QueryCompilationContextDependencies dependencies, ILinqOperatorProvider linqOperatorProvider, bool trackQueryResults);

-        public virtual Type ContextType { get; }

-        public virtual bool IgnoreQueryFilters { get; }

-        public virtual bool IsAsyncQuery { get; set; }

-        public virtual bool IsIncludeQuery { get; }

-        public virtual bool IsQueryBufferRequired { get; private set; }

-        public virtual bool IsTrackingQuery { get; }

-        public virtual ILinqOperatorProvider LinqOperatorProvider { get; }

-        public virtual IDiagnosticsLogger<DbLoggerCategory.Query> Logger { get; }

-        public virtual IModel Model { get; }

-        public virtual IReadOnlyCollection<IQueryAnnotation> QueryAnnotations { get; }

-        public virtual QuerySourceMapping QuerySourceMapping { get; }

-        public virtual bool TrackQueryResults { get; }

-        public virtual void AddAnnotations(IEnumerable<IQueryAnnotation> annotations);

-        public virtual void AddOrUpdateMapping(IQuerySource querySource, IEntityType entityType);

-        public virtual void AddOrUpdateMapping(IQuerySource querySource, Expression expression);

-        public virtual void AddQuerySourceRequiringMaterialization(IQuerySource querySource);

-        public virtual void AddTrackableInclude(IQuerySource querySource, IReadOnlyList<INavigation> navigationPath);

-        public virtual void CloneAnnotations(QuerySourceMapping querySourceMapping, QueryModel queryModel);

-        public virtual EntityQueryModelVisitor CreateQueryModelVisitor();

-        public virtual EntityQueryModelVisitor CreateQueryModelVisitor(EntityQueryModelVisitor parentEntityQueryModelVisitor);

-        public virtual void DetermineQueryBufferRequirement(QueryModel queryModel);

-        public virtual IEntityType FindEntityType(IQuerySource querySource);

-        public virtual void FindQuerySourcesRequiringMaterialization(EntityQueryModelVisitor queryModelVisitor, QueryModel queryModel);

-        public virtual IReadOnlyList<IReadOnlyList<INavigation>> GetTrackableIncludes(IQuerySource querySource);

-        public virtual bool QuerySourceRequiresMaterialization(IQuerySource querySource);

-        public virtual void RegisterCorrelatedSubqueryMetadata(MainFromClause mainFromClause, bool trackingQuery, INavigation firstNavigation, INavigation collectionNavigation, IQuerySource parentQuerySource);

-        public virtual bool TryGetCorrelatedSubqueryMetadata(MainFromClause mainFromClause, out CorrelatedSubqueryMetadata correlatedSubqueryMetadata);

-        public virtual void UpdateMapping(QuerySourceMapping querySourceMapping);

-    }
-    public class QueryContext : IDisposable, IParameterValues {
 {
-        public QueryContext(QueryContextDependencies dependencies, Func<IQueryBuffer> queryBufferFactory);

-        public virtual CancellationToken CancellationToken { get; set; }

-        public virtual IConcurrencyDetector ConcurrencyDetector { get; }

-        public virtual DbContext Context { get; }

-        protected virtual QueryContextDependencies Dependencies { get; }

-        public virtual IReadOnlyDictionary<string, object> ParameterValues { get; }

-        public virtual IQueryBuffer QueryBuffer { get; }

-        public virtual IQueryProvider QueryProvider { get; }

-        public virtual IStateManager StateManager { get; }

-        public virtual void AddParameter(string name, object value);

-        public virtual void BeginTrackingQuery();

-        public virtual void Dispose();

-        public virtual object RemoveParameter(string name);

-        public virtual void SetParameter(string name, object value);

-        public virtual void StartTracking(object entity, EntityTrackingInfo entityTrackingInfo);

-    }
-    public sealed class QueryContextDependencies {
 {
-        public QueryContextDependencies(ICurrentDbContext currentContext, IConcurrencyDetector concurrencyDetector);

-        public IChangeDetector ChangeDetector { get; }

-        public IConcurrencyDetector ConcurrencyDetector { get; }

-        public ICurrentDbContext CurrentDbContext { get; }

-        public IQueryProvider QueryProvider { get; }

-        public IStateManager StateManager { get; }

-        public QueryContextDependencies With(IConcurrencyDetector concurrencyDetector);

-        public QueryContextDependencies With(ICurrentDbContext currentDbContext);

-    }
-    public abstract class QueryContextFactory : IQueryContextFactory {
 {
-        protected QueryContextFactory(QueryContextDependencies dependencies);

-        protected virtual QueryContextDependencies Dependencies { get; }

-        public abstract QueryContext Create();

-        protected virtual IQueryBuffer CreateQueryBuffer();

-    }
-    public class QueryMethodProvider : IQueryMethodProvider {
 {
-        public QueryMethodProvider();

-        public virtual MethodInfo DefaultIfEmptyShapedQueryMethod { get; }

-        public virtual MethodInfo FastQueryMethod { get; }

-        public virtual MethodInfo GetResultMethod { get; }

-        public virtual MethodInfo GroupByMethod { get; }

-        public virtual MethodInfo GroupJoinMethod { get; }

-        public virtual MethodInfo InjectParametersMethod { get; }

-        public virtual MethodInfo QueryMethod { get; }

-        public virtual MethodInfo ShapedQueryMethod { get; }

-    }
-    public class RelationalCompiledQueryCacheKeyGenerator : CompiledQueryCacheKeyGenerator {
 {
-        public RelationalCompiledQueryCacheKeyGenerator(CompiledQueryCacheKeyGeneratorDependencies dependencies, RelationalCompiledQueryCacheKeyGeneratorDependencies relationalDependencies);

-        protected virtual RelationalCompiledQueryCacheKeyGeneratorDependencies RelationalDependencies { get; }

-        public override object GenerateCacheKey(Expression query, bool async);

-        protected new RelationalCompiledQueryCacheKeyGenerator.RelationalCompiledQueryCacheKey GenerateCacheKeyCore(Expression query, bool async);

-        protected readonly struct RelationalCompiledQueryCacheKey {
 {
-            public RelationalCompiledQueryCacheKey(CompiledQueryCacheKeyGenerator.CompiledQueryCacheKey compiledQueryCacheKey, bool useRelationalNulls);

-            public override bool Equals(object obj);

-            public override int GetHashCode();

-        }
-    }
-    public sealed class RelationalCompiledQueryCacheKeyGeneratorDependencies {
 {
-        public RelationalCompiledQueryCacheKeyGeneratorDependencies(IDbContextOptions contextOptions);

-        public IDbContextOptions ContextOptions { get; }

-        public RelationalCompiledQueryCacheKeyGeneratorDependencies With(IDbContextOptions contextOptions);

-    }
-    public class RelationalQueryCompilationContext : QueryCompilationContext {
 {
-        public RelationalQueryCompilationContext(QueryCompilationContextDependencies dependencies, ILinqOperatorProvider linqOperatorProvider, IQueryMethodProvider queryMethodProvider, bool trackQueryResults);

-        public virtual bool IsLateralJoinSupported { get; }

-        public virtual int MaxTableAliasLength { get; }

-        public virtual IList<string> ParentQueryReferenceParameters { get; }

-        public virtual IQueryMethodProvider QueryMethodProvider { get; }

-        public override EntityQueryModelVisitor CreateQueryModelVisitor();

-        public override EntityQueryModelVisitor CreateQueryModelVisitor(EntityQueryModelVisitor parentEntityQueryModelVisitor);

-        public virtual string CreateUniqueTableAlias();

-        public virtual string CreateUniqueTableAlias(string currentAlias);

-        public virtual SelectExpression FindSelectExpression(IQuerySource querySource);

-    }
-    public sealed class RelationalQueryCompilationContextDependencies {
 {
-        public RelationalQueryCompilationContextDependencies(INodeTypeProviderFactory nodeTypeProviderFactory);

-        public INodeTypeProviderFactory NodeTypeProviderFactory { get; }

-        public RelationalQueryCompilationContextDependencies With(INodeTypeProviderFactory nodeTypeProviderFactory);

-    }
-    public class RelationalQueryCompilationContextFactory : QueryCompilationContextFactory {
 {
-        public RelationalQueryCompilationContextFactory(QueryCompilationContextDependencies dependencies, RelationalQueryCompilationContextDependencies relationalDependencies);

-        public override QueryCompilationContext Create(bool async);

-    }
-    public class RelationalQueryContext : QueryContext {
 {
-        public RelationalQueryContext(QueryContextDependencies dependencies, Func<IQueryBuffer> queryBufferFactory, IRelationalConnection connection, IExecutionStrategyFactory executionStrategyFactory);

-        public virtual IRelationalConnection Connection { get; }

-        public virtual IExecutionStrategyFactory ExecutionStrategyFactory { get; }

-    }
-    public class RelationalQueryContextFactory : QueryContextFactory {
 {
-        public RelationalQueryContextFactory(QueryContextDependencies dependencies, IRelationalConnection connection, IExecutionStrategyFactory executionStrategyFactory);

-        protected virtual IExecutionStrategyFactory ExecutionStrategyFactory { get; }

-        public override QueryContext Create();

-    }
-    public class RelationalQueryModelVisitor : EntityQueryModelVisitor {
 {
-        public RelationalQueryModelVisitor(EntityQueryModelVisitorDependencies dependencies, RelationalQueryModelVisitorDependencies relationalDependencies, RelationalQueryCompilationContext queryCompilationContext, RelationalQueryModelVisitor parentQueryModelVisitor);

-        public virtual bool CanBindToParentQueryModel { get; protected set; }

-        protected virtual IDbContextOptions ContextOptions { get; }

-        public virtual bool IsLiftable { get; }

-        public virtual RelationalQueryModelVisitor ParentQueryModelVisitor { get; }

-        public virtual ICollection<SelectExpression> Queries { get; }

-        protected virtual Dictionary<IQuerySource, SelectExpression> QueriesBySource { get; }

-        public virtual new RelationalQueryCompilationContext QueryCompilationContext { get; }

-        public virtual bool RequiresClientEval { get; set; }

-        public virtual bool RequiresClientFilter { get; set; }

-        public virtual bool RequiresClientJoin { get; set; }

-        public virtual bool RequiresClientOrderBy { get; set; }

-        public virtual bool RequiresClientProjection { get; set; }

-        public virtual bool RequiresClientResultOperator { get; set; }

-        public virtual bool RequiresClientSelectMany { get; set; }

-        public virtual bool RequiresStreamingGroupResultOperator { get; set; }

-        public virtual void AddQuery(IQuerySource querySource, SelectExpression selectExpression);

-        public virtual Expression BindLocalMethodCallExpression(MethodCallExpression methodCallExpression);

-        public virtual TResult BindMemberExpression<TResult>(MemberExpression memberExpression, Func<IProperty, IQuerySource, SelectExpression, TResult> memberBinder, bool bindSubQueries = false);

-        public virtual Expression BindMemberToOuterQueryParameter(MemberExpression memberExpression);

-        public override Expression BindMemberToValueBuffer(MemberExpression memberExpression, Expression expression);

-        public virtual TResult BindMethodCallExpression<TResult>(MethodCallExpression methodCallExpression, Func<IProperty, IQuerySource, SelectExpression, TResult> memberBinder, bool bindSubQueries = false);

-        public override Expression BindMethodCallToValueBuffer(MethodCallExpression methodCallExpression, Expression expression);

-        public virtual Expression BindMethodToOuterQueryParameter(MethodCallExpression methodCallExpression);

-        protected override bool CanOptimizeCorrelatedCollections();

-        protected override Expression CompileAdditionalFromClauseExpression(AdditionalFromClause additionalFromClause, QueryModel queryModel);

-        protected override Expression CompileGroupJoinInnerSequenceExpression(GroupJoinClause groupJoinClause, QueryModel queryModel);

-        protected override Expression CompileJoinClauseInnerSequenceExpression(JoinClause joinClause, QueryModel queryModel);

-        protected override Expression CompileMainFromClauseExpression(MainFromClause mainFromClause, QueryModel queryModel);

-        protected override Func<QueryContext, TResults> CreateExecutorLambda<TResults>();

-        public virtual void LiftInjectedParameters(RelationalQueryModelVisitor subQueryModelVisitor);

-        protected override void OnBeforeNavigationRewrite(QueryModel queryModel);

-        protected override void OptimizeQueryModel(QueryModel queryModel, bool asyncQuery);

-        public virtual void RegisterSubQueryVisitor(IQuerySource querySource, RelationalQueryModelVisitor queryModelVisitor);

-        protected override void RemoveOrderings(QueryModel queryModel);

-        public override bool ShouldApplyDefiningQuery(IEntityType entityType, IQuerySource querySource);

-        public virtual SelectExpression TryGetQuery(IQuerySource querySource);

-        public override void VisitAdditionalFromClause(AdditionalFromClause fromClause, QueryModel queryModel, int index);

-        public override void VisitGroupJoinClause(GroupJoinClause groupJoinClause, QueryModel queryModel, int index);

-        public override void VisitJoinClause(JoinClause joinClause, QueryModel queryModel, int index);

-        public override void VisitOrderByClause(OrderByClause orderByClause, QueryModel queryModel, int index);

-        public override void VisitQueryModel(QueryModel queryModel);

-        public override void VisitResultOperator(ResultOperatorBase resultOperator, QueryModel queryModel, int index);

-        public override void VisitSelectClause(SelectClause selectClause, QueryModel queryModel);

-        public virtual void VisitSubQueryModel(QueryModel queryModel);

-        public override void VisitWhereClause(WhereClause whereClause, QueryModel queryModel, int index);

-        protected virtual void WarnClientEval(QueryModel queryModel, object queryModelElement);

-    }
-    public sealed class RelationalQueryModelVisitorDependencies {
 {
-        public RelationalQueryModelVisitorDependencies(IRelationalResultOperatorHandler relationalResultOperatorHandler, ISqlTranslatingExpressionVisitorFactory sqlTranslatingExpressionVisitorFactory, ICompositePredicateExpressionVisitorFactory compositePredicateExpressionVisitorFactory, IConditionalRemovingExpressionVisitorFactory conditionalRemovingExpressionVisitorFactory, IDbContextOptions contextOptions);

-        public ICompositePredicateExpressionVisitorFactory CompositePredicateExpressionVisitorFactory { get; }

-        public IConditionalRemovingExpressionVisitorFactory ConditionalRemovingExpressionVisitorFactory { get; }

-        public IDbContextOptions ContextOptions { get; }

-        public IRelationalResultOperatorHandler RelationalResultOperatorHandler { get; }

-        public ISqlTranslatingExpressionVisitorFactory SqlTranslatingExpressionVisitorFactory { get; }

-        public RelationalQueryModelVisitorDependencies With(IDbContextOptions contextOptions);

-        public RelationalQueryModelVisitorDependencies With(ICompositePredicateExpressionVisitorFactory compositePredicateExpressionVisitorFactory);

-        public RelationalQueryModelVisitorDependencies With(IConditionalRemovingExpressionVisitorFactory conditionalRemovingExpressionVisitorFactory);

-        public RelationalQueryModelVisitorDependencies With(ISqlTranslatingExpressionVisitorFactory sqlTranslatingExpressionVisitorFactory);

-        public RelationalQueryModelVisitorDependencies With(IRelationalResultOperatorHandler relationalResultOperatorHandler);

-    }
-    public class RelationalQueryModelVisitorFactory : EntityQueryModelVisitorFactory {
 {
-        public RelationalQueryModelVisitorFactory(EntityQueryModelVisitorDependencies dependencies, RelationalQueryModelVisitorDependencies relationalDependencies);

-        protected virtual RelationalQueryModelVisitorDependencies RelationalDependencies { get; }

-        public override EntityQueryModelVisitor Create(QueryCompilationContext queryCompilationContext, EntityQueryModelVisitor parentEntityQueryModelVisitor);

-    }
-    public class ResultOperatorHandler : IResultOperatorHandler {
 {
-        public ResultOperatorHandler(ResultOperatorHandlerDependencies dependencies);

-        public static Expression CallWithPossibleCancellationToken(MethodInfo methodInfo, params Expression[] arguments);

-        public virtual Expression HandleResultOperator(EntityQueryModelVisitor entityQueryModelVisitor, ResultOperatorBase resultOperator, QueryModel queryModel);

-    }
-    public sealed class ResultOperatorHandlerDependencies {
 {
-        public ResultOperatorHandlerDependencies();

-    }
-}
```

