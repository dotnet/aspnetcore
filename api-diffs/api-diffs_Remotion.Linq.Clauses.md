# Remotion.Linq.Clauses

``` diff
-namespace Remotion.Linq.Clauses {
 {
-    public sealed class AdditionalFromClause : FromClauseBase, IBodyClause, IClause {
 {
-        public AdditionalFromClause(string itemName, Type itemType, Expression fromExpression);

-        public void Accept(IQueryModelVisitor visitor, QueryModel queryModel, int index);

-        public AdditionalFromClause Clone(CloneContext cloneContext);

-        IBodyClause Remotion.Linq.Clauses.IBodyClause.Clone(CloneContext cloneContext);

-    }
-    public sealed class CloneContext {
 {
-        public CloneContext(QuerySourceMapping querySourceMapping);

-        public QuerySourceMapping QuerySourceMapping { get; private set; }

-    }
-    public abstract class FromClauseBase : IClause, IFromClause, IQuerySource {
 {
-        public Expression FromExpression { get; set; }

-        public string ItemName { get; set; }

-        public Type ItemType { get; set; }

-        public virtual void CopyFromSource(IFromClause source);

-        public override string ToString();

-        public virtual void TransformExpressions(Func<Expression, Expression> transformation);

-    }
-    public sealed class GroupJoinClause : IBodyClause, IClause, IQuerySource {
 {
-        public GroupJoinClause(string itemName, Type itemType, JoinClause joinClause);

-        public string ItemName { get; set; }

-        public Type ItemType { get; set; }

-        public JoinClause JoinClause { get; set; }

-        public void Accept(IQueryModelVisitor visitor, QueryModel queryModel, int index);

-        public GroupJoinClause Clone(CloneContext cloneContext);

-        IBodyClause Remotion.Linq.Clauses.IBodyClause.Clone(CloneContext cloneContext);

-        public override string ToString();

-        public void TransformExpressions(Func<Expression, Expression> transformation);

-    }
-    public interface IBodyClause : IClause {
 {
-        void Accept(IQueryModelVisitor visitor, QueryModel queryModel, int index);

-        IBodyClause Clone(CloneContext cloneContext);

-    }
-    public interface IClause {
 {
-        void TransformExpressions(Func<Expression, Expression> transformation);

-    }
-    public interface IFromClause : IClause, IQuerySource {
 {
-        Expression FromExpression { get; }

-        void CopyFromSource(IFromClause source);

-    }
-    public interface IQuerySource {
 {
-        string ItemName { get; }

-        Type ItemType { get; }

-    }
-    public sealed class JoinClause : IBodyClause, IClause, IQuerySource {
 {
-        public JoinClause(string itemName, Type itemType, Expression innerSequence, Expression outerKeySelector, Expression innerKeySelector);

-        public Expression InnerKeySelector { get; set; }

-        public Expression InnerSequence { get; set; }

-        public string ItemName { get; set; }

-        public Type ItemType { get; set; }

-        public Expression OuterKeySelector { get; set; }

-        public void Accept(IQueryModelVisitor visitor, QueryModel queryModel, GroupJoinClause groupJoinClause);

-        public void Accept(IQueryModelVisitor visitor, QueryModel queryModel, int index);

-        public JoinClause Clone(CloneContext cloneContext);

-        IBodyClause Remotion.Linq.Clauses.IBodyClause.Clone(CloneContext cloneContext);

-        public override string ToString();

-        public void TransformExpressions(Func<Expression, Expression> transformation);

-    }
-    public sealed class MainFromClause : FromClauseBase {
 {
-        public MainFromClause(string itemName, Type itemType, Expression fromExpression);

-        public void Accept(IQueryModelVisitor visitor, QueryModel queryModel);

-        public MainFromClause Clone(CloneContext cloneContext);

-    }
-    public sealed class OrderByClause : IBodyClause, IClause {
 {
-        public OrderByClause();

-        public ObservableCollection<Ordering> Orderings { get; private set; }

-        public void Accept(IQueryModelVisitor visitor, QueryModel queryModel, int index);

-        public OrderByClause Clone(CloneContext cloneContext);

-        IBodyClause Remotion.Linq.Clauses.IBodyClause.Clone(CloneContext cloneContext);

-        public override string ToString();

-        public void TransformExpressions(Func<Expression, Expression> transformation);

-    }
-    public sealed class Ordering {
 {
-        public Ordering(Expression expression, OrderingDirection direction);

-        public Expression Expression { get; set; }

-        public OrderingDirection OrderingDirection { get; set; }

-        public void Accept(IQueryModelVisitor visitor, QueryModel queryModel, OrderByClause orderByClause, int index);

-        public Ordering Clone(CloneContext cloneContext);

-        public override string ToString();

-        public void TransformExpressions(Func<Expression, Expression> transformation);

-    }
-    public enum OrderingDirection {
 {
-        Asc = 0,

-        Desc = 1,

-    }
-    public sealed class QuerySourceMapping {
 {
-        public QuerySourceMapping();

-        public void AddMapping(IQuerySource querySource, Expression expression);

-        public bool ContainsMapping(IQuerySource querySource);

-        public Expression GetExpression(IQuerySource querySource);

-        public void RemoveMapping(IQuerySource querySource);

-        public void ReplaceMapping(IQuerySource querySource, Expression expression);

-    }
-    public abstract class ResultOperatorBase {
 {
-        protected ResultOperatorBase();

-        public void Accept(IQueryModelVisitor visitor, QueryModel queryModel, int index);

-        protected void CheckSequenceItemType(StreamedSequenceInfo inputInfo, Type expectedItemType);

-        public abstract ResultOperatorBase Clone(CloneContext cloneContext);

-        public abstract IStreamedData ExecuteInMemory(IStreamedData input);

-        protected T GetConstantValueFromExpression<T>(string expressionName, Expression expression);

-        public abstract IStreamedDataInfo GetOutputDataInfo(IStreamedDataInfo inputInfo);

-        protected object InvokeExecuteMethod(MethodInfo method, object input);

-        public abstract void TransformExpressions(Func<Expression, Expression> transformation);

-    }
-    public sealed class SelectClause : IClause {
 {
-        public SelectClause(Expression selector);

-        public Expression Selector { get; set; }

-        public void Accept(IQueryModelVisitor visitor, QueryModel queryModel);

-        public SelectClause Clone(CloneContext cloneContext);

-        public StreamedSequenceInfo GetOutputDataInfo();

-        public override string ToString();

-        public void TransformExpressions(Func<Expression, Expression> transformation);

-    }
-    public sealed class WhereClause : IBodyClause, IClause {
 {
-        public WhereClause(Expression predicate);

-        public Expression Predicate { get; set; }

-        public void Accept(IQueryModelVisitor visitor, QueryModel queryModel, int index);

-        public WhereClause Clone(CloneContext cloneContext);

-        IBodyClause Remotion.Linq.Clauses.IBodyClause.Clone(CloneContext cloneContext);

-        public override string ToString();

-        public void TransformExpressions(Func<Expression, Expression> transformation);

-    }
-}
```

