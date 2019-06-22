# Remotion.Linq

``` diff
-namespace Remotion.Linq {
 {
-    public sealed class DefaultQueryProvider : QueryProviderBase {
 {
-        public DefaultQueryProvider(Type queryableType, IQueryParser queryParser, IQueryExecutor executor);

-        public Type QueryableType { get; }

-        public override IQueryable<T> CreateQuery<T>(Expression expression);

-    }
-    public interface IQueryExecutor {
 {
-        IEnumerable<T> ExecuteCollection<T>(QueryModel queryModel);

-        T ExecuteScalar<T>(QueryModel queryModel);

-        T ExecuteSingle<T>(QueryModel queryModel, bool returnDefaultWhenEmpty);

-    }
-    public interface IQueryModelVisitor {
 {
-        void VisitAdditionalFromClause(AdditionalFromClause fromClause, QueryModel queryModel, int index);

-        void VisitGroupJoinClause(GroupJoinClause joinClause, QueryModel queryModel, int index);

-        void VisitJoinClause(JoinClause joinClause, QueryModel queryModel, GroupJoinClause groupJoinClause);

-        void VisitJoinClause(JoinClause joinClause, QueryModel queryModel, int index);

-        void VisitMainFromClause(MainFromClause fromClause, QueryModel queryModel);

-        void VisitOrderByClause(OrderByClause orderByClause, QueryModel queryModel, int index);

-        void VisitOrdering(Ordering ordering, QueryModel queryModel, OrderByClause orderByClause, int index);

-        void VisitQueryModel(QueryModel queryModel);

-        void VisitResultOperator(ResultOperatorBase resultOperator, QueryModel queryModel, int index);

-        void VisitSelectClause(SelectClause selectClause, QueryModel queryModel);

-        void VisitWhereClause(WhereClause whereClause, QueryModel queryModel, int index);

-    }
-    public abstract class QueryableBase<T> : IEnumerable, IEnumerable<T>, IOrderedQueryable, IOrderedQueryable<T>, IQueryable, IQueryable<T> {
 {
-        protected QueryableBase(IQueryParser queryParser, IQueryExecutor executor);

-        protected QueryableBase(IQueryProvider provider);

-        protected QueryableBase(IQueryProvider provider, Expression expression);

-        public Type ElementType { get; }

-        public Expression Expression { get; private set; }

-        public IQueryProvider Provider { get; }

-        public IEnumerator<T> GetEnumerator();

-        IEnumerator System.Collections.IEnumerable.GetEnumerator();

-    }
-    public sealed class QueryModel {
 {
-        public QueryModel(MainFromClause mainFromClause, SelectClause selectClause);

-        public ObservableCollection<IBodyClause> BodyClauses { get; private set; }

-        public MainFromClause MainFromClause { get; set; }

-        public ObservableCollection<ResultOperatorBase> ResultOperators { get; private set; }

-        public Type ResultTypeOverride { get; set; }

-        public SelectClause SelectClause { get; set; }

-        public void Accept(IQueryModelVisitor visitor);

-        public QueryModel Clone();

-        public QueryModel Clone(QuerySourceMapping querySourceMapping);

-        public QueryModel ConvertToSubQuery(string itemName);

-        public IStreamedData Execute(IQueryExecutor executor);

-        public string GetNewName(string prefix);

-        public IStreamedDataInfo GetOutputDataInfo();

-        public Type GetResultType();

-        public UniqueIdentifierGenerator GetUniqueIdentfierGenerator();

-        public bool IsIdentityQuery();

-        public override string ToString();

-        public void TransformExpressions(Func<Expression, Expression> transformation);

-    }
-    public sealed class QueryModelBuilder {
 {
-        public QueryModelBuilder();

-        public ReadOnlyCollection<IBodyClause> BodyClauses { get; }

-        public MainFromClause MainFromClause { get; private set; }

-        public ReadOnlyCollection<ResultOperatorBase> ResultOperators { get; }

-        public SelectClause SelectClause { get; private set; }

-        public void AddClause(IClause clause);

-        public void AddResultOperator(ResultOperatorBase resultOperator);

-        public QueryModel Build();

-    }
-    public abstract class QueryModelVisitorBase : IQueryModelVisitor {
 {
-        protected QueryModelVisitorBase();

-        public virtual void VisitAdditionalFromClause(AdditionalFromClause fromClause, QueryModel queryModel, int index);

-        protected virtual void VisitBodyClauses(ObservableCollection<IBodyClause> bodyClauses, QueryModel queryModel);

-        public virtual void VisitGroupJoinClause(GroupJoinClause groupJoinClause, QueryModel queryModel, int index);

-        public virtual void VisitJoinClause(JoinClause joinClause, QueryModel queryModel, GroupJoinClause groupJoinClause);

-        public virtual void VisitJoinClause(JoinClause joinClause, QueryModel queryModel, int index);

-        public virtual void VisitMainFromClause(MainFromClause fromClause, QueryModel queryModel);

-        public virtual void VisitOrderByClause(OrderByClause orderByClause, QueryModel queryModel, int index);

-        public virtual void VisitOrdering(Ordering ordering, QueryModel queryModel, OrderByClause orderByClause, int index);

-        protected virtual void VisitOrderings(ObservableCollection<Ordering> orderings, QueryModel queryModel, OrderByClause orderByClause);

-        public virtual void VisitQueryModel(QueryModel queryModel);

-        public virtual void VisitResultOperator(ResultOperatorBase resultOperator, QueryModel queryModel, int index);

-        protected virtual void VisitResultOperators(ObservableCollection<ResultOperatorBase> resultOperators, QueryModel queryModel);

-        public virtual void VisitSelectClause(SelectClause selectClause, QueryModel queryModel);

-        public virtual void VisitWhereClause(WhereClause whereClause, QueryModel queryModel, int index);

-    }
-    public abstract class QueryProviderBase : IQueryProvider {
 {
-        protected QueryProviderBase(IQueryParser queryParser, IQueryExecutor executor);

-        public IQueryExecutor Executor { get; }

-        public ExpressionTreeParser ExpressionTreeParser { get; }

-        public IQueryParser QueryParser { get; }

-        public IQueryable CreateQuery(Expression expression);

-        public abstract IQueryable<T> CreateQuery<T>(Expression expression);

-        public virtual IStreamedData Execute(Expression expression);

-        public virtual QueryModel GenerateQueryModel(Expression expression);

-        object System.Linq.IQueryProvider.Execute(Expression expression);

-        TResult System.Linq.IQueryProvider.Execute<TResult>(Expression expression);

-    }
-    public sealed class UniqueIdentifierGenerator {
 {
-        public UniqueIdentifierGenerator();

-        public void AddKnownIdentifier(string identifier);

-        public string GetUniqueIdentifier(string prefix);

-        public void Reset();

-    }
-}
```

