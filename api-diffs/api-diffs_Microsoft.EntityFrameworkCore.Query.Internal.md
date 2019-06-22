# Microsoft.EntityFrameworkCore.Query.Internal

``` diff
-namespace Microsoft.EntityFrameworkCore.Query.Internal {
 {
-    public readonly struct AnonymousObject {
 {
-        public static readonly ConstructorInfo AnonymousObjectCtor;

-        public static readonly MethodInfo GetValueMethodInfo;

-        public AnonymousObject(object[] values);

-        public override bool Equals(object obj);

-        public override int GetHashCode();

-        public object GetValue(int index);

-        public static bool IsGetValueExpression(MethodCallExpression methodCallExpression, out QuerySourceReferenceExpression querySourceReferenceExpression);

-        public static bool operator ==(AnonymousObject x, AnonymousObject y);

-        public static bool operator !=(AnonymousObject x, AnonymousObject y);

-    }
-    public class AsyncLinqOperatorProvider : ILinqOperatorProvider {
 {
-        public AsyncLinqOperatorProvider();

-        public virtual MethodInfo All { get; }

-        public virtual MethodInfo Any { get; }

-        public virtual MethodInfo Cast { get; }

-        public virtual MethodInfo Concat { get; }

-        public virtual MethodInfo Contains { get; }

-        public virtual MethodInfo Count { get; }

-        public virtual MethodInfo DefaultIfEmpty { get; }

-        public virtual MethodInfo DefaultIfEmptyArg { get; }

-        public virtual MethodInfo Distinct { get; }

-        public virtual MethodInfo Except { get; }

-        public virtual MethodInfo First { get; }

-        public virtual MethodInfo FirstOrDefault { get; }

-        public virtual MethodInfo GroupBy { get; }

-        public virtual MethodInfo GroupJoin { get; }

-        public virtual MethodInfo InterceptExceptions { get; }

-        public virtual MethodInfo Intersect { get; }

-        public virtual MethodInfo Join { get; }

-        public virtual MethodInfo Last { get; }

-        public virtual MethodInfo LastOrDefault { get; }

-        public virtual MethodInfo LongCount { get; }

-        public virtual MethodInfo OfType { get; }

-        public virtual MethodInfo OrderBy { get; }

-        public virtual MethodInfo Select { get; }

-        public static MethodInfo SelectAsyncMethod { get; }

-        public virtual MethodInfo SelectMany { get; }

-        public virtual MethodInfo Single { get; }

-        public virtual MethodInfo SingleOrDefault { get; }

-        public virtual MethodInfo Skip { get; }

-        public virtual MethodInfo Take { get; }

-        public virtual MethodInfo ThenBy { get; }

-        public virtual MethodInfo ToAsyncEnumerable { get; }

-        public virtual MethodInfo ToEnumerable { get; }

-        public virtual MethodInfo ToOrdered { get; }

-        public virtual MethodInfo ToQueryable { get; }

-        public virtual MethodInfo ToSequence { get; }

-        public virtual MethodInfo TrackEntities { get; }

-        public virtual MethodInfo TrackGroupedEntities { get; }

-        public virtual MethodInfo Union { get; }

-        public virtual MethodInfo Where { get; }

-        public virtual MethodInfo GetAggregateMethod(string methodName, Type elementType);

-        public virtual Type MakeSequenceType(Type elementType);

-    }
-    public class AsyncQueryingEnumerable<T> : IAsyncEnumerable<T> {
 {
-        public AsyncQueryingEnumerable(RelationalQueryContext relationalQueryContext, ShaperCommandContext shaperCommandContext, IShaper<T> shaper);

-        public virtual IAsyncEnumerator<T> GetEnumerator();

-    }
-    public class CompiledAsyncEnumerableQuery<TContext, TResult> : CompiledQueryBase<TContext, AsyncEnumerable<TResult>> where TContext : DbContext {
 {
-        public CompiledAsyncEnumerableQuery(LambdaExpression queryExpression);

-        protected override Func<QueryContext, AsyncEnumerable<TResult>> CreateCompiledQuery(IQueryCompiler queryCompiler, Expression expression);

-        public virtual AsyncEnumerable<TResult> Execute(TContext context);

-        public virtual AsyncEnumerable<TResult> Execute<TParam1, TParam2, TParam3, TParam4, TParam5>(TContext context, TParam1 param1, TParam2 param2, TParam3 param3, TParam4 param4, TParam5 param5);

-        public virtual AsyncEnumerable<TResult> Execute<TParam1, TParam2, TParam3, TParam4>(TContext context, TParam1 param1, TParam2 param2, TParam3 param3, TParam4 param4);

-        public virtual AsyncEnumerable<TResult> Execute<TParam1, TParam2, TParam3>(TContext context, TParam1 param1, TParam2 param2, TParam3 param3);

-        public virtual AsyncEnumerable<TResult> Execute<TParam1, TParam2>(TContext context, TParam1 param1, TParam2 param2);

-        public virtual AsyncEnumerable<TResult> Execute<TParam1>(TContext context, TParam1 param1);

-    }
-    public class CompiledAsyncTaskQuery<TContext, TResult> : CompiledQueryBase<TContext, Task<TResult>> where TContext : DbContext {
 {
-        public CompiledAsyncTaskQuery(LambdaExpression queryExpression);

-        protected override Func<QueryContext, Task<TResult>> CreateCompiledQuery(IQueryCompiler queryCompiler, Expression expression);

-        public virtual Task<TResult> ExecuteAsync(TContext context);

-        public virtual Task<TResult> ExecuteAsync(TContext context, CancellationToken cancellationToken);

-        public virtual Task<TResult> ExecuteAsync<TParam1, TParam2, TParam3, TParam4, TParam5>(TContext context, TParam1 param1, TParam2 param2, TParam3 param3, TParam4 param4, TParam5 param5);

-        public virtual Task<TResult> ExecuteAsync<TParam1, TParam2, TParam3, TParam4, TParam5>(TContext context, TParam1 param1, TParam2 param2, TParam3 param3, TParam4 param4, TParam5 param5, CancellationToken cancellationToken);

-        public virtual Task<TResult> ExecuteAsync<TParam1, TParam2, TParam3, TParam4>(TContext context, TParam1 param1, TParam2 param2, TParam3 param3, TParam4 param4);

-        public virtual Task<TResult> ExecuteAsync<TParam1, TParam2, TParam3, TParam4>(TContext context, TParam1 param1, TParam2 param2, TParam3 param3, TParam4 param4, CancellationToken cancellationToken);

-        public virtual Task<TResult> ExecuteAsync<TParam1, TParam2, TParam3>(TContext context, TParam1 param1, TParam2 param2, TParam3 param3);

-        public virtual Task<TResult> ExecuteAsync<TParam1, TParam2, TParam3>(TContext context, TParam1 param1, TParam2 param2, TParam3 param3, CancellationToken cancellationToken);

-        public virtual Task<TResult> ExecuteAsync<TParam1, TParam2>(TContext context, TParam1 param1, TParam2 param2);

-        public virtual Task<TResult> ExecuteAsync<TParam1, TParam2>(TContext context, TParam1 param1, TParam2 param2, CancellationToken cancellationToken);

-        public virtual Task<TResult> ExecuteAsync<TParam1>(TContext context, TParam1 param1);

-        public virtual Task<TResult> ExecuteAsync<TParam1>(TContext context, TParam1 param1, CancellationToken cancellationToken);

-    }
-    public class CompiledQuery<TContext, TResult> : CompiledQueryBase<TContext, TResult> where TContext : DbContext {
 {
-        public CompiledQuery(LambdaExpression queryExpression);

-        protected override Func<QueryContext, TResult> CreateCompiledQuery(IQueryCompiler queryCompiler, Expression expression);

-        public virtual TResult Execute(TContext context);

-        public virtual TResult Execute<TParam1, TParam2, TParam3, TParam4, TParam5>(TContext context, TParam1 param1, TParam2 param2, TParam3 param3, TParam4 param4, TParam5 param5);

-        public virtual TResult Execute<TParam1, TParam2, TParam3, TParam4>(TContext context, TParam1 param1, TParam2 param2, TParam3 param3, TParam4 param4);

-        public virtual TResult Execute<TParam1, TParam2, TParam3>(TContext context, TParam1 param1, TParam2 param2, TParam3 param3);

-        public virtual TResult Execute<TParam1, TParam2>(TContext context, TParam1 param1, TParam2 param2);

-        public virtual TResult Execute<TParam1>(TContext context, TParam1 param1);

-        public virtual TResult ExecuteAsync<TParam1>(TContext context, CancellationToken cancellationToken, TParam1 param1);

-    }
-    public abstract class CompiledQueryBase<TContext, TResult> where TContext : DbContext {
 {
-        protected CompiledQueryBase(LambdaExpression queryExpression);

-        protected abstract Func<QueryContext, TResult> CreateCompiledQuery(IQueryCompiler queryCompiler, Expression expression);

-        protected virtual TResult ExecuteCore(TContext context, params object[] parameters);

-        protected virtual TResult ExecuteCore(TContext context, CancellationToken cancellationToken, params object[] parameters);

-    }
-    public class CompiledQueryCache : ICompiledQueryCache {
 {
-        public const string CompiledQueryParameterPrefix = "__";

-        public CompiledQueryCache(IMemoryCache memoryCache);

-        public virtual Func<QueryContext, IAsyncEnumerable<TResult>> GetOrAddAsyncQuery<TResult>(object cacheKey, Func<Func<QueryContext, IAsyncEnumerable<TResult>>> compiler);

-        public virtual Func<QueryContext, TResult> GetOrAddQuery<TResult>(object cacheKey, Func<Func<QueryContext, TResult>> compiler);

-    }
-    public class CorrelatedSubqueryMetadata {
 {
-        public CorrelatedSubqueryMetadata(int index, bool trackingQuery, INavigation firstNavigation, INavigation collectionNavigation, IQuerySource parentQuerySource);

-        public virtual INavigation CollectionNavigation { get; }

-        public virtual INavigation FirstNavigation { get; }

-        public virtual int Index { get; }

-        public virtual IQuerySource ParentQuerySource { get; internal set; }

-        public virtual bool TrackingQuery { get; }

-    }
-    public class DefaultMethodInfoBasedNodeTypeRegistryFactory : MethodInfoBasedNodeTypeRegistryFactory {
 {
-        public DefaultMethodInfoBasedNodeTypeRegistryFactory();

-    }
-    public class DependentToPrincipalIncludeComparer<TKey> : IIncludeKeyComparer {
 {
-        public DependentToPrincipalIncludeComparer(TKey dependentKeyValue, IPrincipalKeyValueFactory<TKey> principalKeyValueFactory);

-        public virtual bool ShouldInclude(InternalEntityEntry internalEntityEntry);

-        public virtual bool ShouldInclude(in ValueBuffer valueBuffer);

-    }
-    public class EntityQueryable<TResult> : QueryableBase<TResult>, IAsyncEnumerable<TResult>, IDetachableContext, IListSource {
 {
-        public EntityQueryable(IAsyncQueryProvider provider);

-        public EntityQueryable(IAsyncQueryProvider provider, Expression expression);

-        bool System.ComponentModel.IListSource.ContainsListCollection { get; }

-        IDetachableContext Microsoft.EntityFrameworkCore.Query.Internal.IDetachableContext.DetachContext();

-        IAsyncEnumerator<TResult> System.Collections.Generic.IAsyncEnumerable<TResult>.GetEnumerator();

-        IList System.ComponentModel.IListSource.GetList();

-    }
-    public class EntityQueryProvider : IAsyncQueryProvider, IQueryProvider {
 {
-        public EntityQueryProvider(IQueryCompiler queryCompiler);

-        public virtual IQueryable CreateQuery(Expression expression);

-        public virtual IQueryable<TElement> CreateQuery<TElement>(Expression expression);

-        public virtual object Execute(Expression expression);

-        public virtual TResult Execute<TResult>(Expression expression);

-        public virtual IAsyncEnumerable<TResult> ExecuteAsync<TResult>(Expression expression);

-        public virtual Task<TResult> ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken);

-    }
-    public class EntityTrackingInfo {
 {
-        public EntityTrackingInfo(QueryCompilationContext queryCompilationContext, QuerySourceReferenceExpression querySourceReferenceExpression, IEntityType entityType);

-        public virtual bool IsEnumerableTarget { get; }

-        public virtual IQuerySource QuerySource { get; }

-        public virtual QuerySourceReferenceExpression QuerySourceReferenceExpression { get; }

-        public virtual void StartTracking(IStateManager stateManager, object entity, in ValueBuffer valueBuffer);

-    }
-    public class EntityTrackingInfoFactory : IEntityTrackingInfoFactory {
 {
-        public EntityTrackingInfoFactory();

-        public virtual EntityTrackingInfo Create(QueryCompilationContext queryCompilationContext, QuerySourceReferenceExpression querySourceReferenceExpression, IEntityType entityType);

-    }
-    public class EvaluatableExpressionFilter : EvaluatableExpressionFilterBase {
 {
-        public EvaluatableExpressionFilter();

-        public override bool IsEvaluatableMember(MemberExpression memberExpression);

-        public override bool IsEvaluatableMethodCall(MethodCallExpression methodCallExpression);

-    }
-    public class ExpressionEqualityComparer : IEqualityComparer<Expression> {
 {
-        public static ExpressionEqualityComparer Instance { get; }

-        public virtual bool Equals(Expression x, Expression y);

-        public virtual int GetHashCode(Expression obj);

-        public virtual bool SequenceEquals(IEnumerable<Expression> x, IEnumerable<Expression> y);

-    }
-    public class ExpressionPrinter : ExpressionVisitorBase, IExpressionPrinter {
 {
-        protected List<ExpressionPrinter.ConstantPrinterBase> ConstantPrinters;

-        public ExpressionPrinter();

-        protected ExpressionPrinter(List<ExpressionPrinter.ConstantPrinterBase> additionalConstantPrinters);

-        public virtual Nullable<int> CharacterLimit { get; set; }

-        public virtual bool GenerateUniqueQsreIds { get; set; }

-        public virtual bool PrintConnections { get; set; }

-        public virtual bool RemoveFormatting { get; set; }

-        public virtual IndentedStringBuilder StringBuilder { get; }

-        public virtual List<IQuerySource> VisitedQuerySources { get; }

-        protected virtual void Append(string message);

-        protected virtual void AppendLine(string message = "");

-        protected virtual string PostProcess(string queryPlan);

-        public virtual string Print(Expression expression, bool removeFormatting = false, Nullable<int> characterLimit = default(Nullable<int>), bool printConnections = true);

-        public virtual string PrintDebug(Expression expression, bool highlightNonreducibleNodes = true, bool reduceBeforePrinting = true, bool generateUniqueQsreIds = true, bool printConnections = true);

-        public override Expression Visit(Expression expression);

-        protected override Expression VisitBinary(BinaryExpression binaryExpression);

-        protected override Expression VisitBlock(BlockExpression blockExpression);

-        protected override Expression VisitConditional(ConditionalExpression conditionalExpression);

-        protected override Expression VisitConstant(ConstantExpression constantExpression);

-        protected override Expression VisitDefault(DefaultExpression defaultExpression);

-        protected override Expression VisitExtension(Expression extensionExpression);

-        protected override Expression VisitGoto(GotoExpression gotoExpression);

-        protected override Expression VisitIndex(IndexExpression indexExpression);

-        protected override Expression VisitLabel(LabelExpression labelExpression);

-        protected override Expression VisitLambda<T>(Expression<T> lambdaExpression);

-        protected override Expression VisitMember(MemberExpression memberExpression);

-        protected override Expression VisitMemberInit(MemberInitExpression memeberInitExpression);

-        protected override Expression VisitMethodCall(MethodCallExpression methodCallExpression);

-        protected override Expression VisitNew(NewExpression newExpression);

-        protected override Expression VisitNewArray(NewArrayExpression newArrayExpression);

-        protected override Expression VisitParameter(ParameterExpression parameterExpression);

-        protected override Expression VisitTry(TryExpression tryExpression);

-        protected override Expression VisitTypeBinary(TypeBinaryExpression typeBinaryExpression);

-        protected override Expression VisitUnary(UnaryExpression unaryExpression);

-        protected abstract class ConstantPrinterBase {
 {
-            protected ConstantPrinterBase();

-            protected virtual Action<IndentedStringBuilder, string> Append { get; }

-            protected virtual Action<IndentedStringBuilder, string> AppendLine { get; }

-            public abstract bool TryPrintConstant(ConstantExpression constantExpression, IndentedStringBuilder stringBuilder, bool removeFormatting);

-        }
-    }
-    public class Grouping<TKey, TElement> : IEnumerable, IEnumerable<TElement>, IGrouping<TKey, TElement> {
 {
-        public Grouping(TKey key);

-        public virtual TKey Key { get; }

-        public virtual void Add(TElement element);

-        public virtual IEnumerator<TElement> GetEnumerator();

-        IEnumerator System.Collections.IEnumerable.GetEnumerator();

-    }
-    public interface IAsyncEnumerableAccessor<out T> {
 {
-        IAsyncEnumerable<T> AsyncEnumerable { get; }

-    }
-    public interface IAsyncQueryProvider : IQueryProvider {
 {
-        IAsyncEnumerable<TResult> ExecuteAsync<TResult>(Expression expression);

-        Task<TResult> ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken);

-    }
-    public interface IAsyncRelatedEntitiesLoader : IDisposable {
 {
-        IAsyncEnumerable<EntityLoadInfo> Load(QueryContext queryContext, IIncludeKeyComparer keyComparer);

-    }
-    public interface IBufferable {
 {
-        void BufferAll();

-        Task BufferAllAsync(CancellationToken cancellationToken);

-    }
-    public interface ICompiledQueryCache {
 {
-        Func<QueryContext, IAsyncEnumerable<TResult>> GetOrAddAsyncQuery<TResult>(object cacheKey, Func<Func<QueryContext, IAsyncEnumerable<TResult>>> compiler);

-        Func<QueryContext, TResult> GetOrAddQuery<TResult>(object cacheKey, Func<Func<QueryContext, TResult>> compiler);

-    }
-    public interface IDetachableContext {
 {
-        IDetachableContext DetachContext();

-    }
-    public interface IEntityTrackingInfoFactory {
 {
-        EntityTrackingInfo Create(QueryCompilationContext queryCompilationContext, QuerySourceReferenceExpression querySourceReferenceExpression, IEntityType entityType);

-    }
-    public interface IExpressionPrinter {
 {
-        string Print(Expression expression, bool removeFormatting = false, Nullable<int> characterLimit = default(Nullable<int>), bool printConnections = true);

-        string PrintDebug(Expression expression, bool highlightNonreducibleNodes = true, bool reduceBeforePrinting = true, bool generateUniqueQsreIds = true, bool printConnections = true);

-    }
-    public interface IIncludeKeyComparer {
 {
-        bool ShouldInclude(InternalEntityEntry internalEntityEntry);

-        bool ShouldInclude(in ValueBuffer valueBuffer);

-    }
-    public interface ILinqOperatorProvider {
 {
-        MethodInfo All { get; }

-        MethodInfo Any { get; }

-        MethodInfo Cast { get; }

-        MethodInfo Concat { get; }

-        MethodInfo Contains { get; }

-        MethodInfo Count { get; }

-        MethodInfo DefaultIfEmpty { get; }

-        MethodInfo DefaultIfEmptyArg { get; }

-        MethodInfo Distinct { get; }

-        MethodInfo Except { get; }

-        MethodInfo First { get; }

-        MethodInfo FirstOrDefault { get; }

-        MethodInfo GroupBy { get; }

-        MethodInfo GroupJoin { get; }

-        MethodInfo InterceptExceptions { get; }

-        MethodInfo Intersect { get; }

-        MethodInfo Join { get; }

-        MethodInfo Last { get; }

-        MethodInfo LastOrDefault { get; }

-        MethodInfo LongCount { get; }

-        MethodInfo OfType { get; }

-        MethodInfo OrderBy { get; }

-        MethodInfo Select { get; }

-        MethodInfo SelectMany { get; }

-        MethodInfo Single { get; }

-        MethodInfo SingleOrDefault { get; }

-        MethodInfo Skip { get; }

-        MethodInfo Take { get; }

-        MethodInfo ThenBy { get; }

-        MethodInfo ToEnumerable { get; }

-        MethodInfo ToOrdered { get; }

-        MethodInfo ToQueryable { get; }

-        MethodInfo ToSequence { get; }

-        MethodInfo TrackEntities { get; }

-        MethodInfo TrackGroupedEntities { get; }

-        MethodInfo Union { get; }

-        MethodInfo Where { get; }

-        MethodInfo GetAggregateMethod(string methodName, Type elementType);

-        Type MakeSequenceType(Type elementType);

-    }
-    public class IncludeCompiler {
 {
-        public static readonly ParameterExpression CancellationTokenParameter;

-        public IncludeCompiler(QueryCompilationContext queryCompilationContext, IQuerySourceTracingExpressionVisitorFactory querySourceTracingExpressionVisitorFactory);

-        public virtual void CompileIncludes(QueryModel queryModel, bool trackingQuery, bool asyncQuery, bool shouldThrow);

-        public static bool IsIncludeMethod(MethodCallExpression methodCallExpression);

-        public virtual void LogIgnoredIncludes();

-        public virtual void RewriteCollectionQueries();

-        public virtual void RewriteCollectionQueries(QueryModel queryModel);

-    }
-    public interface INodeTypeProviderFactory {
 {
-        INodeTypeProvider Create();

-        void RegisterMethods(IEnumerable<MethodInfo> methods, Type nodeType);

-    }
-    public interface IParameterValues {
 {
-        IReadOnlyDictionary<string, object> ParameterValues { get; }

-        void AddParameter(string name, object value);

-        object RemoveParameter(string name);

-        void SetParameter(string name, object value);

-    }
-    public interface IQueryAnnotationExtractor {
 {
-        IReadOnlyCollection<IQueryAnnotation> ExtractQueryAnnotations(QueryModel queryModel);

-    }
-    public interface IQueryBuffer : IDisposable {
 {
-        TCollection CorrelateSubquery<TInner, TOut, TCollection>(int correlatedCollectionId, INavigation navigation, Func<INavigation, TCollection> resultCollectionFactory, in MaterializedAnonymousObject outerKey, bool tracking, Func<IEnumerable<Tuple<TInner, MaterializedAnonymousObject, MaterializedAnonymousObject>>> correlatedCollectionFactory, Func<MaterializedAnonymousObject, MaterializedAnonymousObject, bool> correlationPredicate) where TInner : TOut where TCollection : ICollection<TOut>;

-        Task<TCollection> CorrelateSubqueryAsync<TInner, TOut, TCollection>(int correlatedCollectionId, INavigation navigation, Func<INavigation, TCollection> resultCollectionFactory, MaterializedAnonymousObject outerKey, bool tracking, Func<IAsyncEnumerable<Tuple<TInner, MaterializedAnonymousObject, MaterializedAnonymousObject>>> correlatedCollectionFactory, Func<MaterializedAnonymousObject, MaterializedAnonymousObject, bool> correlationPredicate, CancellationToken cancellationToken) where TInner : TOut where TCollection : ICollection<TOut>;

-        object GetEntity(IKey key, EntityLoadInfo entityLoadInfo, bool queryStateManager, bool throwOnNullKey);

-        object GetPropertyValue(object entity, IProperty property);

-        void IncludeCollection<TEntity, TRelated, TElement>(int includeId, INavigation navigation, INavigation inverseNavigation, IEntityType targetEntityType, IClrCollectionAccessor clrCollectionAccessor, IClrPropertySetter inverseClrPropertySetter, bool tracking, TEntity instance, Func<IEnumerable<TRelated>> valuesFactory, Func<TEntity, TRelated, bool> joinPredicate) where TRelated : TElement;

-        Task IncludeCollectionAsync<TEntity, TRelated, TElement>(int includeId, INavigation navigation, INavigation inverseNavigation, IEntityType targetEntityType, IClrCollectionAccessor clrCollectionAccessor, IClrPropertySetter inverseClrPropertySetter, bool tracking, TEntity instance, Func<IAsyncEnumerable<TRelated>> valuesFactory, Func<TEntity, TRelated, bool> joinPredicate, CancellationToken cancellationToken) where TRelated : TElement;

-        void StartTracking(object entity, IEntityType entityType);

-        void StartTracking(object entity, EntityTrackingInfo entityTrackingInfo);

-    }
-    public interface IQueryCompiler {
 {
-        Func<QueryContext, IAsyncEnumerable<TResult>> CreateCompiledAsyncEnumerableQuery<TResult>(Expression query);

-        Func<QueryContext, Task<TResult>> CreateCompiledAsyncTaskQuery<TResult>(Expression query);

-        Func<QueryContext, TResult> CreateCompiledQuery<TResult>(Expression query);

-        TResult Execute<TResult>(Expression query);

-        IAsyncEnumerable<TResult> ExecuteAsync<TResult>(Expression query);

-        Task<TResult> ExecuteAsync<TResult>(Expression query, CancellationToken cancellationToken);

-    }
-    public interface IQueryModelGenerator {
 {
-        Expression ExtractParameters(IDiagnosticsLogger<DbLoggerCategory.Query> logger, Expression query, IParameterValues parameterValues, bool parameterize = true, bool generateContextAccessors = false);

-        QueryModel ParseQuery(Expression query);

-    }
-    public interface IQueryModelPrinter {
 {
-        string Print(QueryModel queryModel, bool removeFormatting = false, Nullable<int> characterLimit = default(Nullable<int>), bool generateUniqueQsreIds = false);

-        string PrintDebug(QueryModel queryModel, bool removeFormatting = false, Nullable<int> characterLimit = default(Nullable<int>), bool generateUniqueQsreIds = true);

-    }
-    public interface IQueryOptimizer {
 {
-        void Optimize(QueryCompilationContext queryCompilationContext, QueryModel queryModel);

-    }
-    public interface IRelatedEntitiesLoader : IDisposable {
 {
-        IEnumerable<EntityLoadInfo> Load(QueryContext queryContext, IIncludeKeyComparer keyComparer);

-    }
-    public interface IShaperCommandContextFactory {
 {
-        ShaperCommandContext Create(Func<IQuerySqlGenerator> sqlGeneratorFunc);

-    }
-    public interface IWeakReferenceIdentityMap {
 {
-        IKey Key { get; }

-        void Add(in ValueBuffer valueBuffer, object entity);

-        void CollectGarbage();

-        IIncludeKeyComparer CreateIncludeKeyComparer(INavigation navigation, InternalEntityEntry entry);

-        IIncludeKeyComparer CreateIncludeKeyComparer(INavigation navigation, in ValueBuffer valueBuffer);

-        WeakReference<object> TryGetEntity(in ValueBuffer valueBuffer, bool throwOnNullKey, out bool hasNullKey);

-    }
-    public class LinqOperatorProvider : ILinqOperatorProvider {
 {
-        public LinqOperatorProvider();

-        public virtual MethodInfo All { get; }

-        public virtual MethodInfo Any { get; }

-        public virtual MethodInfo Cast { get; }

-        public virtual MethodInfo Concat { get; }

-        public virtual MethodInfo Contains { get; }

-        public virtual MethodInfo Count { get; }

-        public virtual MethodInfo DefaultIfEmpty { get; }

-        public virtual MethodInfo DefaultIfEmptyArg { get; }

-        public virtual MethodInfo Distinct { get; }

-        public virtual MethodInfo Except { get; }

-        public virtual MethodInfo First { get; }

-        public virtual MethodInfo FirstOrDefault { get; }

-        public virtual MethodInfo GroupBy { get; }

-        public virtual MethodInfo GroupJoin { get; }

-        public virtual MethodInfo InterceptExceptions { get; }

-        public virtual MethodInfo Intersect { get; }

-        public virtual MethodInfo Join { get; }

-        public virtual MethodInfo Last { get; }

-        public virtual MethodInfo LastOrDefault { get; }

-        public virtual MethodInfo LongCount { get; }

-        public virtual MethodInfo OfType { get; }

-        public virtual MethodInfo OrderBy { get; }

-        public virtual MethodInfo Select { get; }

-        public virtual MethodInfo SelectMany { get; }

-        public virtual MethodInfo Single { get; }

-        public virtual MethodInfo SingleOrDefault { get; }

-        public virtual MethodInfo Skip { get; }

-        public virtual MethodInfo Take { get; }

-        public virtual MethodInfo ThenBy { get; }

-        public virtual MethodInfo ToEnumerable { get; }

-        public virtual MethodInfo ToOrdered { get; }

-        public virtual MethodInfo ToQueryable { get; }

-        public virtual MethodInfo ToSequence { get; }

-        public virtual MethodInfo TrackEntities { get; }

-        public virtual MethodInfo TrackGroupedEntities { get; }

-        public virtual MethodInfo Union { get; }

-        public virtual MethodInfo Where { get; }

-        public virtual MethodInfo GetAggregateMethod(string methodName, Type elementType);

-        public virtual Type MakeSequenceType(Type elementType);

-    }
-    public readonly struct MaterializedAnonymousObject {
 {
-        public static readonly ConstructorInfo AnonymousObjectCtor;

-        public static readonly MethodInfo GetValueMethodInfo;

-        public MaterializedAnonymousObject(object[] values);

-        public override bool Equals(object obj);

-        public override int GetHashCode();

-        public object GetValue(int index);

-        public bool IsDefault();

-        public static bool IsGetValueExpression(MethodCallExpression methodCallExpression, out QuerySourceReferenceExpression querySourceReferenceExpression);

-        public static bool operator ==(MaterializedAnonymousObject x, MaterializedAnonymousObject y);

-        public static bool operator !=(MaterializedAnonymousObject x, MaterializedAnonymousObject y);

-    }
-    public class MethodInfoBasedNodeTypeRegistryFactory : INodeTypeProviderFactory {
 {
-        public MethodInfoBasedNodeTypeRegistryFactory(MethodInfoBasedNodeTypeRegistry methodInfoBasedNodeTypeRegistry);

-        public virtual INodeTypeProvider Create();

-        public virtual void RegisterMethods(IEnumerable<MethodInfo> methods, Type nodeType);

-    }
-    public class NullAsyncQueryProvider : IAsyncQueryProvider, IQueryProvider {
 {
-        public static readonly IAsyncQueryProvider Instance;

-        IAsyncEnumerable<TResult1> Microsoft.EntityFrameworkCore.Query.Internal.IAsyncQueryProvider.ExecuteAsync<TResult1>(Expression expression);

-        Task<TResult1> Microsoft.EntityFrameworkCore.Query.Internal.IAsyncQueryProvider.ExecuteAsync<TResult1>(Expression expression, CancellationToken cancellationToken);

-        IQueryable System.Linq.IQueryProvider.CreateQuery(Expression expression);

-        IQueryable<TElement> System.Linq.IQueryProvider.CreateQuery<TElement>(Expression expression);

-        object System.Linq.IQueryProvider.Execute(Expression expression);

-        TResult1 System.Linq.IQueryProvider.Execute<TResult1>(Expression expression);

-    }
-    public class NullIncludeComparer : IIncludeKeyComparer {
 {
-        public NullIncludeComparer();

-        public virtual bool ShouldInclude(InternalEntityEntry internalEntityEntry);

-        public virtual bool ShouldInclude(in ValueBuffer valueBuffer);

-    }
-    public class PrincipalToDependentIncludeComparer<TKey> : IIncludeKeyComparer {
 {
-        public PrincipalToDependentIncludeComparer(TKey principalKeyValue, IDependentKeyValueFactory<TKey> dependentKeyValueFactory, IPrincipalKeyValueFactory<TKey> principalKeyValueFactory);

-        public virtual bool ShouldInclude(InternalEntityEntry internalEntityEntry);

-        public virtual bool ShouldInclude(in ValueBuffer valueBuffer);

-    }
-    public class QueryAnnotationExtractor : IQueryAnnotationExtractor {
 {
-        public QueryAnnotationExtractor();

-        public virtual IReadOnlyCollection<IQueryAnnotation> ExtractQueryAnnotations(QueryModel queryModel);

-    }
-    public class QueryBuffer : IDisposable, IQueryBuffer {
 {
-        public QueryBuffer(QueryContextDependencies dependencies);

-        public virtual TCollection CorrelateSubquery<TInner, TOut, TCollection>(int correlatedCollectionId, INavigation navigation, Func<INavigation, TCollection> resultCollectionFactory, in MaterializedAnonymousObject outerKey, bool tracking, Func<IEnumerable<Tuple<TInner, MaterializedAnonymousObject, MaterializedAnonymousObject>>> correlatedCollectionFactory, Func<MaterializedAnonymousObject, MaterializedAnonymousObject, bool> correlationPredicate) where TInner : TOut where TCollection : ICollection<TOut>;

-        public virtual Task<TCollection> CorrelateSubqueryAsync<TInner, TOut, TCollection>(int correlatedCollectionId, INavigation navigation, Func<INavigation, TCollection> resultCollectionFactory, MaterializedAnonymousObject outerKey, bool tracking, Func<IAsyncEnumerable<Tuple<TInner, MaterializedAnonymousObject, MaterializedAnonymousObject>>> correlatedCollectionFactory, Func<MaterializedAnonymousObject, MaterializedAnonymousObject, bool> correlationPredicate, CancellationToken cancellationToken) where TInner : TOut where TCollection : ICollection<TOut>;

-        public virtual object GetEntity(IKey key, EntityLoadInfo entityLoadInfo, bool queryStateManager, bool throwOnNullKey);

-        public virtual object GetPropertyValue(object entity, IProperty property);

-        public virtual void IncludeCollection<TEntity, TRelated, TElement>(int includeId, INavigation navigation, INavigation inverseNavigation, IEntityType targetEntityType, IClrCollectionAccessor clrCollectionAccessor, IClrPropertySetter inverseClrPropertySetter, bool tracking, TEntity entity, Func<IEnumerable<TRelated>> relatedEntitiesFactory, Func<TEntity, TRelated, bool> joinPredicate) where TRelated : TElement;

-        public virtual Task IncludeCollectionAsync<TEntity, TRelated, TElement>(int includeId, INavigation navigation, INavigation inverseNavigation, IEntityType targetEntityType, IClrCollectionAccessor clrCollectionAccessor, IClrPropertySetter inverseClrPropertySetter, bool tracking, TEntity entity, Func<IAsyncEnumerable<TRelated>> relatedEntitiesFactory, Func<TEntity, TRelated, bool> joinPredicate, CancellationToken cancellationToken) where TRelated : TElement;

-        public virtual void StartTracking(object entity, IEntityType entityType);

-        public virtual void StartTracking(object entity, EntityTrackingInfo entityTrackingInfo);

-        void System.IDisposable.Dispose();

-    }
-    public sealed class QueryCompilationContextDependencies {
 {
-        public QueryCompilationContextDependencies(IModel model, IDiagnosticsLogger<DbLoggerCategory.Query> logger, IEntityQueryModelVisitorFactory entityQueryModelVisitorFactory, IRequiresMaterializationExpressionVisitorFactory requiresMaterializationExpressionVisitorFactory, ICurrentDbContext currentContext);

-        public ICurrentDbContext CurrentContext { get; }

-        public IEntityQueryModelVisitorFactory EntityQueryModelVisitorFactory { get; }

-        public IDiagnosticsLogger<DbLoggerCategory.Query> Logger { get; }

-        public IModel Model { get; }

-        public IRequiresMaterializationExpressionVisitorFactory RequiresMaterializationExpressionVisitorFactory { get; }

-        public QueryCompilationContextDependencies With(IDiagnosticsLogger<DbLoggerCategory.Query> logger);

-        public QueryCompilationContextDependencies With(ICurrentDbContext currentContext);

-        public QueryCompilationContextDependencies With(IModel model);

-        public QueryCompilationContextDependencies With(IRequiresMaterializationExpressionVisitorFactory requiresMaterializationExpressionVisitorFactory);

-        public QueryCompilationContextDependencies With(IEntityQueryModelVisitorFactory entityQueryModelVisitorFactory);

-    }
-    public class QueryCompilationContextFactory : IQueryCompilationContextFactory {
 {
-        public QueryCompilationContextFactory(QueryCompilationContextDependencies dependencies);

-        protected virtual QueryCompilationContextDependencies Dependencies { get; }

-        protected virtual bool TrackQueryResults { get; }

-        public virtual QueryCompilationContext Create(bool async);

-    }
-    public class QueryCompiler : IQueryCompiler {
 {
-        public QueryCompiler(IQueryContextFactory queryContextFactory, ICompiledQueryCache compiledQueryCache, ICompiledQueryCacheKeyGenerator compiledQueryCacheKeyGenerator, IDatabase database, IDiagnosticsLogger<DbLoggerCategory.Query> logger, ICurrentDbContext currentContext, IQueryModelGenerator queryModelGenerator);

-        protected virtual IDatabase Database { get; }

-        protected virtual Func<QueryContext, IAsyncEnumerable<TResult>> CompileAsyncQuery<TResult>(Expression query);

-        public virtual Func<QueryContext, IAsyncEnumerable<TResult>> CreateCompiledAsyncEnumerableQuery<TResult>(Expression query);

-        public virtual Func<QueryContext, Task<TResult>> CreateCompiledAsyncTaskQuery<TResult>(Expression query);

-        public virtual Func<QueryContext, TResult> CreateCompiledQuery<TResult>(Expression query);

-        public virtual TResult Execute<TResult>(Expression query);

-        public virtual IAsyncEnumerable<TResult> ExecuteAsync<TResult>(Expression query);

-        public virtual Task<TResult> ExecuteAsync<TResult>(Expression query, CancellationToken cancellationToken);

-    }
-    public class QueryingEnumerable<T> : IEnumerable, IEnumerable<T> {
 {
-        public QueryingEnumerable(RelationalQueryContext relationalQueryContext, ShaperCommandContext shaperCommandContext, IShaper<T> shaper);

-        public virtual IEnumerator<T> GetEnumerator();

-        IEnumerator System.Collections.IEnumerable.GetEnumerator();

-    }
-    public static class QueryModelExtensions {
 {
-        public static int CountQuerySourceReferences(this QueryModel queryModel, IQuerySource querySource);

-        public static Expression GetOutputExpression(this QueryModel queryModel);

-        public static Dictionary<QueryModel, QueryModel> PopulateQueryModelMapping(this QueryModel queryModel, Dictionary<QueryModel, QueryModel> mapping);

-        public static string Print(this QueryModel queryModel, bool removeFormatting = false, Nullable<int> characterLimit = default(Nullable<int>));

-        public static string PrintDebug(this QueryModel queryModel, bool removeFormatting = false, Nullable<int> characterLimit = default(Nullable<int>), bool generateUniqueQsreIds = true);

-        public static QueryModel RecreateQueryModelFromMapping(this QueryModel queryModel, Dictionary<QueryModel, QueryModel> mapping);

-    }
-    public class QueryModelGenerator : IQueryModelGenerator {
 {
-        public QueryModelGenerator(INodeTypeProviderFactory nodeTypeProviderFactory, IEvaluatableExpressionFilter evaluatableExpressionFilter, ICurrentDbContext currentDbContext);

-        public virtual Expression ExtractParameters(IDiagnosticsLogger<DbLoggerCategory.Query> logger, Expression query, IParameterValues parameterValues, bool parameterize = true, bool generateContextAccessors = false);

-        public virtual QueryModel ParseQuery(Expression query);

-    }
-    public class QueryModelPrinter : IQueryModelPrinter {
 {
-        public QueryModelPrinter();

-        public virtual string Print(QueryModel queryModel, bool removeFormatting = false, Nullable<int> characterLimit = default(Nullable<int>), bool generateUniqueQsreIds = false);

-        public virtual string PrintDebug(QueryModel queryModel, bool removeFormatting = false, Nullable<int> characterLimit = default(Nullable<int>), bool generateUniqueQsreIds = true);

-    }
-    public class QueryOptimizer : SubQueryFromClauseFlattener, IQueryOptimizer {
 {
-        public QueryOptimizer();

-        protected override void FlattenSubQuery(SubQueryExpression subQueryExpression, IFromClause fromClause, QueryModel queryModel, int destinationIndex);

-        public virtual void Optimize(QueryCompilationContext queryCompilationContext, QueryModel queryModel);

-        public override void VisitGroupJoinClause(GroupJoinClause groupJoinClause, QueryModel queryModel, int index);

-        public override void VisitJoinClause(JoinClause joinClause, QueryModel queryModel, GroupJoinClause groupJoinClause);

-        public override void VisitJoinClause(JoinClause joinClause, QueryModel queryModel, int index);

-        public override void VisitResultOperator(ResultOperatorBase resultOperator, QueryModel queryModel, int index);

-    }
-    public static class QuerySourceExtensions {
 {
-        public static bool HasGeneratedItemName(this IQuerySource querySource);

-        public static GroupJoinClause TryGetFlattenedGroupJoinClause(this AdditionalFromClause additionalFromClause);

-    }
-    public class RelationalEvaluatableExpressionFilter : EvaluatableExpressionFilter {
 {
-        public RelationalEvaluatableExpressionFilter(IModel model);

-        public override bool IsEvaluatableMethodCall(MethodCallExpression methodCallExpression);

-    }
-    public class RelationalExpressionPrinter : ExpressionPrinter {
 {
-        public RelationalExpressionPrinter();

-    }
-    public class RelationalResultOperatorHandler : IRelationalResultOperatorHandler, IResultOperatorHandler {
 {
-        public RelationalResultOperatorHandler(IModel model, ISqlTranslatingExpressionVisitorFactory sqlTranslatingExpressionVisitorFactory, ISelectExpressionFactory selectExpressionFactory, IResultOperatorHandler resultOperatorHandler);

-        public virtual Expression HandleResultOperator(EntityQueryModelVisitor entityQueryModelVisitor, ResultOperatorBase resultOperator, QueryModel queryModel);

-        protected static void PrepareSelectExpressionForAggregate(SelectExpression selectExpression, QueryModel queryModel);

-    }
-    public class ShaperCommandContext {
 {
-        public ShaperCommandContext(IRelationalValueBufferFactoryFactory valueBufferFactoryFactory, Func<IQuerySqlGenerator> querySqlGeneratorFactory);

-        public virtual Func<IQuerySqlGenerator> QuerySqlGeneratorFactory { get; }

-        public virtual IRelationalValueBufferFactory ValueBufferFactory { get; }

-        public virtual IRelationalValueBufferFactoryFactory ValueBufferFactoryFactory { get; }

-        public virtual IRelationalCommand GetRelationalCommand(IReadOnlyDictionary<string, object> parameters);

-        public virtual void NotifyReaderCreated(DbDataReader dataReader);

-    }
-    public class ShaperCommandContextFactory : IShaperCommandContextFactory {
 {
-        public ShaperCommandContextFactory(IRelationalValueBufferFactoryFactory valueBufferFactoryFactory);

-        public virtual ShaperCommandContext Create(Func<IQuerySqlGenerator> sqlGeneratorFunc);

-    }
-    public sealed class WeakReferenceIdentityMap<TKey> : IWeakReferenceIdentityMap {
 {
-        public WeakReferenceIdentityMap(IKey key, IPrincipalKeyValueFactory<TKey> principalKeyValueFactory);

-        public IKey Key { get; }

-        public void Add(in ValueBuffer valueBuffer, object entity);

-        public void CollectGarbage();

-        public IIncludeKeyComparer CreateIncludeKeyComparer(INavigation navigation, InternalEntityEntry entry);

-        public IIncludeKeyComparer CreateIncludeKeyComparer(INavigation navigation, in ValueBuffer valueBuffer);

-        void Microsoft.EntityFrameworkCore.Query.Internal.IWeakReferenceIdentityMap.Add(in ValueBuffer valueBuffer, object entity);

-        IIncludeKeyComparer Microsoft.EntityFrameworkCore.Query.Internal.IWeakReferenceIdentityMap.CreateIncludeKeyComparer(INavigation navigation, in ValueBuffer valueBuffer);

-        WeakReference<object> Microsoft.EntityFrameworkCore.Query.Internal.IWeakReferenceIdentityMap.TryGetEntity(in ValueBuffer valueBuffer, bool throwOnNullKey, out bool hasNullKey);

-        public WeakReference<object> TryGetEntity(in ValueBuffer valueBuffer, bool throwOnNullKey, out bool hasNullKey);

-    }
-    public class WeakReferenceIdentityMapFactoryFactory : IdentityMapFactoryFactoryBase {
 {
-        public WeakReferenceIdentityMapFactoryFactory();

-        public virtual Func<IWeakReferenceIdentityMap> Create(IKey key);

-    }
-}
```

