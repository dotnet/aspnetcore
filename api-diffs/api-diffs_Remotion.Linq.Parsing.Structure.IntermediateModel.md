# Remotion.Linq.Parsing.Structure.IntermediateModel

``` diff
-namespace Remotion.Linq.Parsing.Structure.IntermediateModel {
 {
-    public sealed class AggregateExpressionNode : ResultOperatorExpressionNodeBase {
 {
-        public AggregateExpressionNode(MethodCallExpressionParseInfo parseInfo, LambdaExpression func);

-        public LambdaExpression Func { get; }

-        protected override ResultOperatorBase CreateResultOperator(ClauseGenerationContext clauseGenerationContext);

-        public LambdaExpression GetResolvedFunc(ClauseGenerationContext clauseGenerationContext);

-        public static IEnumerable<MethodInfo> GetSupportedMethods();

-        public override Expression Resolve(ParameterExpression inputParameter, Expression expressionToBeResolved, ClauseGenerationContext clauseGenerationContext);

-    }
-    public sealed class AggregateFromSeedExpressionNode : ResultOperatorExpressionNodeBase {
 {
-        public AggregateFromSeedExpressionNode(MethodCallExpressionParseInfo parseInfo, Expression seed, LambdaExpression func, LambdaExpression optionalResultSelector);

-        public LambdaExpression Func { get; }

-        public LambdaExpression OptionalResultSelector { get; }

-        public Expression Seed { get; }

-        protected override ResultOperatorBase CreateResultOperator(ClauseGenerationContext clauseGenerationContext);

-        public LambdaExpression GetResolvedFunc(ClauseGenerationContext clauseGenerationContext);

-        public static IEnumerable<MethodInfo> GetSupportedMethods();

-        public override Expression Resolve(ParameterExpression inputParameter, Expression expressionToBeResolved, ClauseGenerationContext clauseGenerationContext);

-    }
-    public sealed class AllExpressionNode : ResultOperatorExpressionNodeBase {
 {
-        public AllExpressionNode(MethodCallExpressionParseInfo parseInfo, LambdaExpression predicate);

-        public LambdaExpression Predicate { get; }

-        protected override ResultOperatorBase CreateResultOperator(ClauseGenerationContext clauseGenerationContext);

-        public Expression GetResolvedPredicate(ClauseGenerationContext clauseGenerationContext);

-        public static IEnumerable<MethodInfo> GetSupportedMethods();

-        public override Expression Resolve(ParameterExpression inputParameter, Expression expressionToBeResolved, ClauseGenerationContext clauseGenerationContext);

-    }
-    public sealed class AnyExpressionNode : ResultOperatorExpressionNodeBase {
 {
-        public AnyExpressionNode(MethodCallExpressionParseInfo parseInfo, LambdaExpression optionalPredicate);

-        protected override ResultOperatorBase CreateResultOperator(ClauseGenerationContext clauseGenerationContext);

-        public static IEnumerable<MethodInfo> GetSupportedMethods();

-        public override Expression Resolve(ParameterExpression inputParameter, Expression expressionToBeResolved, ClauseGenerationContext clauseGenerationContext);

-    }
-    public sealed class AsQueryableExpressionNode : ResultOperatorExpressionNodeBase {
 {
-        public AsQueryableExpressionNode(MethodCallExpressionParseInfo parseInfo);

-        protected override ResultOperatorBase CreateResultOperator(ClauseGenerationContext clauseGenerationContext);

-        public static IEnumerable<MethodInfo> GetSupportedMethods();

-        public override Expression Resolve(ParameterExpression inputParameter, Expression expressionToBeResolved, ClauseGenerationContext clauseGenerationContext);

-    }
-    public sealed class AverageExpressionNode : ResultOperatorExpressionNodeBase {
 {
-        public AverageExpressionNode(MethodCallExpressionParseInfo parseInfo, LambdaExpression optionalSelector);

-        protected override ResultOperatorBase CreateResultOperator(ClauseGenerationContext clauseGenerationContext);

-        public static IEnumerable<MethodInfo> GetSupportedMethods();

-        public override Expression Resolve(ParameterExpression inputParameter, Expression expressionToBeResolved, ClauseGenerationContext clauseGenerationContext);

-    }
-    public sealed class CastExpressionNode : ResultOperatorExpressionNodeBase {
 {
-        public CastExpressionNode(MethodCallExpressionParseInfo parseInfo);

-        public Type CastItemType { get; }

-        protected override ResultOperatorBase CreateResultOperator(ClauseGenerationContext clauseGenerationContext);

-        public static IEnumerable<MethodInfo> GetSupportedMethods();

-        public override Expression Resolve(ParameterExpression inputParameter, Expression expressionToBeResolved, ClauseGenerationContext clauseGenerationContext);

-    }
-    public struct ClauseGenerationContext {
 {
-        public ClauseGenerationContext(INodeTypeProvider nodeTypeProvider);

-        public int Count { get; }

-        public INodeTypeProvider NodeTypeProvider { get; }

-        public void AddContextInfo(IExpressionNode node, object contextInfo);

-        public object GetContextInfo(IExpressionNode node);

-    }
-    public sealed class ConcatExpressionNode : QuerySourceSetOperationExpressionNodeBase {
 {
-        public ConcatExpressionNode(MethodCallExpressionParseInfo parseInfo, Expression source2);

-        protected override ResultOperatorBase CreateSpecificResultOperator();

-        public static IEnumerable<MethodInfo> GetSupportedMethods();

-    }
-    public sealed class ContainsExpressionNode : ResultOperatorExpressionNodeBase {
 {
-        public ContainsExpressionNode(MethodCallExpressionParseInfo parseInfo, Expression item);

-        public Expression Item { get; }

-        protected override ResultOperatorBase CreateResultOperator(ClauseGenerationContext clauseGenerationContext);

-        public static IEnumerable<NameBasedRegistrationInfo> GetSupportedMethodNames();

-        public static IEnumerable<MethodInfo> GetSupportedMethods();

-        public override Expression Resolve(ParameterExpression inputParameter, Expression expressionToBeResolved, ClauseGenerationContext clauseGenerationContext);

-    }
-    public sealed class CountExpressionNode : ResultOperatorExpressionNodeBase {
 {
-        public CountExpressionNode(MethodCallExpressionParseInfo parseInfo, LambdaExpression optionalPredicate);

-        protected override ResultOperatorBase CreateResultOperator(ClauseGenerationContext clauseGenerationContext);

-        public static IEnumerable<MethodInfo> GetSupportedMethods();

-        public override Expression Resolve(ParameterExpression inputParameter, Expression expressionToBeResolved, ClauseGenerationContext clauseGenerationContext);

-    }
-    public sealed class DefaultIfEmptyExpressionNode : ResultOperatorExpressionNodeBase {
 {
-        public DefaultIfEmptyExpressionNode(MethodCallExpressionParseInfo parseInfo, Expression optionalDefaultValue);

-        public Expression OptionalDefaultValue { get; }

-        protected override ResultOperatorBase CreateResultOperator(ClauseGenerationContext clauseGenerationContext);

-        public static IEnumerable<MethodInfo> GetSupportedMethods();

-        public override Expression Resolve(ParameterExpression inputParameter, Expression expressionToBeResolved, ClauseGenerationContext clauseGenerationContext);

-    }
-    public sealed class DistinctExpressionNode : ResultOperatorExpressionNodeBase {
 {
-        public DistinctExpressionNode(MethodCallExpressionParseInfo parseInfo);

-        protected override ResultOperatorBase CreateResultOperator(ClauseGenerationContext clauseGenerationContext);

-        public static IEnumerable<MethodInfo> GetSupportedMethods();

-        public override Expression Resolve(ParameterExpression inputParameter, Expression expressionToBeResolved, ClauseGenerationContext clauseGenerationContext);

-    }
-    public sealed class ExceptExpressionNode : ResultOperatorExpressionNodeBase {
 {
-        public ExceptExpressionNode(MethodCallExpressionParseInfo parseInfo, Expression source2);

-        public Expression Source2 { get; }

-        protected override ResultOperatorBase CreateResultOperator(ClauseGenerationContext clauseGenerationContext);

-        public static IEnumerable<MethodInfo> GetSupportedMethods();

-        public override Expression Resolve(ParameterExpression inputParameter, Expression expressionToBeResolved, ClauseGenerationContext clauseGenerationContext);

-    }
-    public sealed class ExpressionNodeInstantiationException : Exception

-    public sealed class ExpressionResolver {
 {
-        public ExpressionResolver(IExpressionNode currentNode);

-        public IExpressionNode CurrentNode { get; }

-        public Expression GetResolvedExpression(Expression unresolvedExpression, ParameterExpression parameterToBeResolved, ClauseGenerationContext clauseGenerationContext);

-    }
-    public sealed class FirstExpressionNode : ResultOperatorExpressionNodeBase {
 {
-        public FirstExpressionNode(MethodCallExpressionParseInfo parseInfo, LambdaExpression optionalPredicate);

-        protected override ResultOperatorBase CreateResultOperator(ClauseGenerationContext clauseGenerationContext);

-        public static IEnumerable<MethodInfo> GetSupportedMethods();

-        public override Expression Resolve(ParameterExpression inputParameter, Expression expressionToBeResolved, ClauseGenerationContext clauseGenerationContext);

-    }
-    public sealed class GroupByExpressionNode : ResultOperatorExpressionNodeBase, IExpressionNode, IQuerySourceExpressionNode {
 {
-        public GroupByExpressionNode(MethodCallExpressionParseInfo parseInfo, LambdaExpression keySelector, LambdaExpression optionalElementSelector);

-        public LambdaExpression KeySelector { get; }

-        public LambdaExpression OptionalElementSelector { get; }

-        protected override ResultOperatorBase CreateResultOperator(ClauseGenerationContext clauseGenerationContext);

-        public Expression GetResolvedKeySelector(ClauseGenerationContext clauseGenerationContext);

-        public Expression GetResolvedOptionalElementSelector(ClauseGenerationContext clauseGenerationContext);

-        public static IEnumerable<MethodInfo> GetSupportedMethods();

-        public override Expression Resolve(ParameterExpression inputParameter, Expression expressionToBeResolved, ClauseGenerationContext clauseGenerationContext);

-    }
-    public sealed class GroupByWithResultSelectorExpressionNode : IExpressionNode, IQuerySourceExpressionNode {
 {
-        public GroupByWithResultSelectorExpressionNode(MethodCallExpressionParseInfo parseInfo, LambdaExpression keySelector, LambdaExpression elementSelectorOrResultSelector, LambdaExpression resultSelectorOrNull);

-        public string AssociatedIdentifier { get; }

-        public Expression Selector { get; }

-        public IExpressionNode Source { get; }

-        public QueryModel Apply(QueryModel queryModel, ClauseGenerationContext clauseGenerationContext);

-        public static IEnumerable<MethodInfo> GetSupportedMethods();

-        public Expression Resolve(ParameterExpression inputParameter, Expression expressionToBeResolved, ClauseGenerationContext clauseGenerationContext);

-    }
-    public sealed class GroupJoinExpressionNode : MethodCallExpressionNodeBase, IExpressionNode, IQuerySourceExpressionNode {
 {
-        public GroupJoinExpressionNode(MethodCallExpressionParseInfo parseInfo, Expression innerSequence, LambdaExpression outerKeySelector, LambdaExpression innerKeySelector, LambdaExpression resultSelector);

-        public LambdaExpression InnerKeySelector { get; }

-        public Expression InnerSequence { get; }

-        public JoinExpressionNode JoinExpressionNode { get; }

-        public LambdaExpression OuterKeySelector { get; }

-        public MethodCallExpression ParsedExpression { get; }

-        public LambdaExpression ResultSelector { get; }

-        protected override void ApplyNodeSpecificSemantics(QueryModel queryModel, ClauseGenerationContext clauseGenerationContext);

-        public Expression GetResolvedResultSelector(ClauseGenerationContext clauseGenerationContext);

-        public static IEnumerable<MethodInfo> GetSupportedMethods();

-        public override Expression Resolve(ParameterExpression inputParameter, Expression expressionToBeResolved, ClauseGenerationContext clauseGenerationContext);

-    }
-    public interface IExpressionNode {
 {
-        string AssociatedIdentifier { get; }

-        IExpressionNode Source { get; }

-        QueryModel Apply(QueryModel queryModel, ClauseGenerationContext clauseGenerationContext);

-        Expression Resolve(ParameterExpression inputParameter, Expression expressionToBeResolved, ClauseGenerationContext clauseGenerationContext);

-    }
-    public sealed class IntersectExpressionNode : ResultOperatorExpressionNodeBase {
 {
-        public IntersectExpressionNode(MethodCallExpressionParseInfo parseInfo, Expression source2);

-        public Expression Source2 { get; }

-        protected override ResultOperatorBase CreateResultOperator(ClauseGenerationContext clauseGenerationContext);

-        public static IEnumerable<MethodInfo> GetSupportedMethods();

-        public override Expression Resolve(ParameterExpression inputParameter, Expression expressionToBeResolved, ClauseGenerationContext clauseGenerationContext);

-    }
-    public interface IQuerySourceExpressionNode : IExpressionNode

-    public sealed class JoinExpressionNode : MethodCallExpressionNodeBase, IExpressionNode, IQuerySourceExpressionNode {
 {
-        public JoinExpressionNode(MethodCallExpressionParseInfo parseInfo, Expression innerSequence, LambdaExpression outerKeySelector, LambdaExpression innerKeySelector, LambdaExpression resultSelector);

-        public LambdaExpression InnerKeySelector { get; }

-        public Expression InnerSequence { get; }

-        public LambdaExpression OuterKeySelector { get; }

-        public LambdaExpression ResultSelector { get; }

-        protected override void ApplyNodeSpecificSemantics(QueryModel queryModel, ClauseGenerationContext clauseGenerationContext);

-        public JoinClause CreateJoinClause(ClauseGenerationContext clauseGenerationContext);

-        public Expression GetResolvedInnerKeySelector(ClauseGenerationContext clauseGenerationContext);

-        public Expression GetResolvedOuterKeySelector(ClauseGenerationContext clauseGenerationContext);

-        public Expression GetResolvedResultSelector(ClauseGenerationContext clauseGenerationContext);

-        public static IEnumerable<MethodInfo> GetSupportedMethods();

-        public override Expression Resolve(ParameterExpression inputParameter, Expression expressionToBeResolved, ClauseGenerationContext clauseGenerationContext);

-    }
-    public sealed class LastExpressionNode : ResultOperatorExpressionNodeBase {
 {
-        public LastExpressionNode(MethodCallExpressionParseInfo parseInfo, LambdaExpression optionalPredicate);

-        protected override ResultOperatorBase CreateResultOperator(ClauseGenerationContext clauseGenerationContext);

-        public static IEnumerable<MethodInfo> GetSupportedMethods();

-        public override Expression Resolve(ParameterExpression inputParameter, Expression expressionToBeResolved, ClauseGenerationContext clauseGenerationContext);

-    }
-    public sealed class LongCountExpressionNode : ResultOperatorExpressionNodeBase {
 {
-        public LongCountExpressionNode(MethodCallExpressionParseInfo parseInfo, LambdaExpression optionalPredicate);

-        protected override ResultOperatorBase CreateResultOperator(ClauseGenerationContext clauseGenerationContext);

-        public static IEnumerable<MethodInfo> GetSupportedMethods();

-        public override Expression Resolve(ParameterExpression inputParameter, Expression expressionToBeResolved, ClauseGenerationContext clauseGenerationContext);

-    }
-    public sealed class MainSourceExpressionNode : IExpressionNode, IQuerySourceExpressionNode {
 {
-        public MainSourceExpressionNode(string associatedIdentifier, Expression expression);

-        public string AssociatedIdentifier { get; }

-        public Expression ParsedExpression { get; }

-        public Type QuerySourceElementType { get; }

-        public Type QuerySourceType { get; }

-        public IExpressionNode Source { get; }

-        public QueryModel Apply(QueryModel queryModel, ClauseGenerationContext clauseGenerationContext);

-        public Expression Resolve(ParameterExpression inputParameter, Expression expressionToBeResolved, ClauseGenerationContext clauseGenerationContext);

-    }
-    public sealed class MaxExpressionNode : ResultOperatorExpressionNodeBase {
 {
-        public MaxExpressionNode(MethodCallExpressionParseInfo parseInfo, LambdaExpression optionalSelector);

-        protected override ResultOperatorBase CreateResultOperator(ClauseGenerationContext clauseGenerationContext);

-        public static IEnumerable<MethodInfo> GetSupportedMethods();

-        public override Expression Resolve(ParameterExpression inputParameter, Expression expressionToBeResolved, ClauseGenerationContext clauseGenerationContext);

-    }
-    public abstract class MethodCallExpressionNodeBase : IExpressionNode {
 {
-        protected MethodCallExpressionNodeBase(MethodCallExpressionParseInfo parseInfo);

-        public string AssociatedIdentifier { get; }

-        public Type NodeResultType { get; }

-        public IExpressionNode Source { get; }

-        public QueryModel Apply(QueryModel queryModel, ClauseGenerationContext clauseGenerationContext);

-        protected abstract void ApplyNodeSpecificSemantics(QueryModel queryModel, ClauseGenerationContext clauseGenerationContext);

-        protected NotSupportedException CreateOutputParameterNotSupportedException();

-        protected NotSupportedException CreateResolveNotSupportedException();

-        public abstract Expression Resolve(ParameterExpression inputParameter, Expression expressionToBeResolved, ClauseGenerationContext clauseGenerationContext);

-        protected virtual void SetResultTypeOverride(QueryModel queryModel);

-        protected virtual QueryModel WrapQueryModelAfterEndOfQuery(QueryModel queryModel, ClauseGenerationContext clauseGenerationContext);

-    }
-    public static class MethodCallExpressionNodeFactory {
 {
-        public static IExpressionNode CreateExpressionNode(Type nodeType, MethodCallExpressionParseInfo parseInfo, object[] additionalConstructorParameters);

-    }
-    public struct MethodCallExpressionParseInfo {
 {
-        public MethodCallExpressionParseInfo(string associatedIdentifier, IExpressionNode source, MethodCallExpression parsedExpression);

-        public string AssociatedIdentifier { get; }

-        public MethodCallExpression ParsedExpression { get; }

-        public IExpressionNode Source { get; }

-    }
-    public sealed class MinExpressionNode : ResultOperatorExpressionNodeBase {
 {
-        public MinExpressionNode(MethodCallExpressionParseInfo parseInfo, LambdaExpression optionalSelector);

-        protected override ResultOperatorBase CreateResultOperator(ClauseGenerationContext clauseGenerationContext);

-        public static IEnumerable<MethodInfo> GetSupportedMethods();

-        public override Expression Resolve(ParameterExpression inputParameter, Expression expressionToBeResolved, ClauseGenerationContext clauseGenerationContext);

-    }
-    public sealed class OfTypeExpressionNode : ResultOperatorExpressionNodeBase {
 {
-        public OfTypeExpressionNode(MethodCallExpressionParseInfo parseInfo);

-        public Type SearchedItemType { get; }

-        protected override ResultOperatorBase CreateResultOperator(ClauseGenerationContext clauseGenerationContext);

-        public static IEnumerable<MethodInfo> GetSupportedMethods();

-        public override Expression Resolve(ParameterExpression inputParameter, Expression expressionToBeResolved, ClauseGenerationContext clauseGenerationContext);

-    }
-    public sealed class OrderByDescendingExpressionNode : MethodCallExpressionNodeBase {
 {
-        public OrderByDescendingExpressionNode(MethodCallExpressionParseInfo parseInfo, LambdaExpression keySelector);

-        public LambdaExpression KeySelector { get; }

-        protected override void ApplyNodeSpecificSemantics(QueryModel queryModel, ClauseGenerationContext clauseGenerationContext);

-        public Expression GetResolvedKeySelector(ClauseGenerationContext clauseGenerationContext);

-        public static IEnumerable<MethodInfo> GetSupportedMethods();

-        public override Expression Resolve(ParameterExpression inputParameter, Expression expressionToBeResolved, ClauseGenerationContext clauseGenerationContext);

-    }
-    public sealed class OrderByExpressionNode : MethodCallExpressionNodeBase {
 {
-        public OrderByExpressionNode(MethodCallExpressionParseInfo parseInfo, LambdaExpression keySelector);

-        public LambdaExpression KeySelector { get; }

-        protected override void ApplyNodeSpecificSemantics(QueryModel queryModel, ClauseGenerationContext clauseGenerationContext);

-        public Expression GetResolvedKeySelector(ClauseGenerationContext clauseGenerationContext);

-        public static IEnumerable<MethodInfo> GetSupportedMethods();

-        public override Expression Resolve(ParameterExpression inputParameter, Expression expressionToBeResolved, ClauseGenerationContext clauseGenerationContext);

-    }
-    public static class QuerySourceExpressionNodeUtility {
 {
-        public static IQuerySource GetQuerySourceForNode(IQuerySourceExpressionNode node, ClauseGenerationContext context);

-        public static Expression ReplaceParameterWithReference(IQuerySourceExpressionNode referencedNode, ParameterExpression parameterToReplace, Expression expression, ClauseGenerationContext context);

-    }
-    public abstract class QuerySourceSetOperationExpressionNodeBase : ResultOperatorExpressionNodeBase, IExpressionNode, IQuerySourceExpressionNode {
 {
-        protected QuerySourceSetOperationExpressionNodeBase(MethodCallExpressionParseInfo parseInfo, Expression source2);

-        public Type ItemType { get; }

-        public Expression Source2 { get; }

-        protected sealed override ResultOperatorBase CreateResultOperator(ClauseGenerationContext clauseGenerationContext);

-        protected abstract ResultOperatorBase CreateSpecificResultOperator();

-        public sealed override Expression Resolve(ParameterExpression inputParameter, Expression expressionToBeResolved, ClauseGenerationContext clauseGenerationContext);

-    }
-    public sealed class ResolvedExpressionCache<T> where T : Expression {
 {
-        public ResolvedExpressionCache(IExpressionNode currentNode);

-        public T GetOrCreate(Func<ExpressionResolver, T> generator);

-    }
-    public abstract class ResultOperatorExpressionNodeBase : MethodCallExpressionNodeBase {
 {
-        protected ResultOperatorExpressionNodeBase(MethodCallExpressionParseInfo parseInfo, LambdaExpression optionalPredicate, LambdaExpression optionalSelector);

-        public MethodCallExpression ParsedExpression { get; }

-        protected override void ApplyNodeSpecificSemantics(QueryModel queryModel, ClauseGenerationContext clauseGenerationContext);

-        protected abstract ResultOperatorBase CreateResultOperator(ClauseGenerationContext clauseGenerationContext);

-        protected sealed override QueryModel WrapQueryModelAfterEndOfQuery(QueryModel queryModel, ClauseGenerationContext clauseGenerationContext);

-    }
-    public sealed class ReverseExpressionNode : ResultOperatorExpressionNodeBase {
 {
-        public ReverseExpressionNode(MethodCallExpressionParseInfo parseInfo);

-        protected override ResultOperatorBase CreateResultOperator(ClauseGenerationContext clauseGenerationContext);

-        public static IEnumerable<MethodInfo> GetSupportedMethods();

-        public override Expression Resolve(ParameterExpression inputParameter, Expression expressionToBeResolved, ClauseGenerationContext clauseGenerationContext);

-    }
-    public sealed class SelectExpressionNode : MethodCallExpressionNodeBase {
 {
-        public SelectExpressionNode(MethodCallExpressionParseInfo parseInfo, LambdaExpression selector);

-        public LambdaExpression Selector { get; }

-        protected override void ApplyNodeSpecificSemantics(QueryModel queryModel, ClauseGenerationContext clauseGenerationContext);

-        public Expression GetResolvedSelector(ClauseGenerationContext clauseGenerationContext);

-        public static IEnumerable<MethodInfo> GetSupportedMethods();

-        public override Expression Resolve(ParameterExpression inputParameter, Expression expressionToBeResolved, ClauseGenerationContext clauseGenerationContext);

-    }
-    public sealed class SelectManyExpressionNode : MethodCallExpressionNodeBase, IExpressionNode, IQuerySourceExpressionNode {
 {
-        public SelectManyExpressionNode(MethodCallExpressionParseInfo parseInfo, LambdaExpression collectionSelector, LambdaExpression resultSelector);

-        public LambdaExpression CollectionSelector { get; }

-        public LambdaExpression ResultSelector { get; }

-        protected override void ApplyNodeSpecificSemantics(QueryModel queryModel, ClauseGenerationContext clauseGenerationContext);

-        public Expression GetResolvedCollectionSelector(ClauseGenerationContext clauseGenerationContext);

-        public Expression GetResolvedResultSelector(ClauseGenerationContext clauseGenerationContext);

-        public static IEnumerable<MethodInfo> GetSupportedMethods();

-        public override Expression Resolve(ParameterExpression inputParameter, Expression expressionToBeResolved, ClauseGenerationContext clauseGenerationContext);

-    }
-    public sealed class SingleExpressionNode : ResultOperatorExpressionNodeBase {
 {
-        public SingleExpressionNode(MethodCallExpressionParseInfo parseInfo, LambdaExpression optionalPredicate);

-        protected override ResultOperatorBase CreateResultOperator(ClauseGenerationContext clauseGenerationContext);

-        public static IEnumerable<MethodInfo> GetSupportedMethods();

-        public override Expression Resolve(ParameterExpression inputParameter, Expression expressionToBeResolved, ClauseGenerationContext clauseGenerationContext);

-    }
-    public sealed class SkipExpressionNode : ResultOperatorExpressionNodeBase {
 {
-        public SkipExpressionNode(MethodCallExpressionParseInfo parseInfo, Expression count);

-        public Expression Count { get; }

-        protected override ResultOperatorBase CreateResultOperator(ClauseGenerationContext clauseGenerationContext);

-        public static IEnumerable<MethodInfo> GetSupportedMethods();

-        public override Expression Resolve(ParameterExpression inputParameter, Expression expressionToBeResolved, ClauseGenerationContext clauseGenerationContext);

-    }
-    public sealed class SumExpressionNode : ResultOperatorExpressionNodeBase {
 {
-        public SumExpressionNode(MethodCallExpressionParseInfo parseInfo, LambdaExpression optionalSelector);

-        protected override ResultOperatorBase CreateResultOperator(ClauseGenerationContext clauseGenerationContext);

-        public static IEnumerable<MethodInfo> GetSupportedMethods();

-        public override Expression Resolve(ParameterExpression inputParameter, Expression expressionToBeResolved, ClauseGenerationContext clauseGenerationContext);

-    }
-    public sealed class TakeExpressionNode : ResultOperatorExpressionNodeBase {
 {
-        public TakeExpressionNode(MethodCallExpressionParseInfo parseInfo, Expression count);

-        public Expression Count { get; }

-        protected override ResultOperatorBase CreateResultOperator(ClauseGenerationContext clauseGenerationContext);

-        public static IEnumerable<MethodInfo> GetSupportedMethods();

-        public override Expression Resolve(ParameterExpression inputParameter, Expression expressionToBeResolved, ClauseGenerationContext clauseGenerationContext);

-    }
-    public sealed class ThenByDescendingExpressionNode : MethodCallExpressionNodeBase {
 {
-        public ThenByDescendingExpressionNode(MethodCallExpressionParseInfo parseInfo, LambdaExpression keySelector);

-        public LambdaExpression KeySelector { get; }

-        protected override void ApplyNodeSpecificSemantics(QueryModel queryModel, ClauseGenerationContext clauseGenerationContext);

-        public Expression GetResolvedKeySelector(ClauseGenerationContext clauseGenerationContext);

-        public static IEnumerable<MethodInfo> GetSupportedMethods();

-        public override Expression Resolve(ParameterExpression inputParameter, Expression expressionToBeResolved, ClauseGenerationContext clauseGenerationContext);

-    }
-    public sealed class ThenByExpressionNode : MethodCallExpressionNodeBase {
 {
-        public ThenByExpressionNode(MethodCallExpressionParseInfo parseInfo, LambdaExpression keySelector);

-        public LambdaExpression KeySelector { get; }

-        protected override void ApplyNodeSpecificSemantics(QueryModel queryModel, ClauseGenerationContext clauseGenerationContext);

-        public Expression GetResolvedKeySelector(ClauseGenerationContext clauseGenerationContext);

-        public static IEnumerable<MethodInfo> GetSupportedMethods();

-        public override Expression Resolve(ParameterExpression inputParameter, Expression expressionToBeResolved, ClauseGenerationContext clauseGenerationContext);

-    }
-    public sealed class UnionExpressionNode : QuerySourceSetOperationExpressionNodeBase {
 {
-        public UnionExpressionNode(MethodCallExpressionParseInfo parseInfo, Expression source2);

-        protected override ResultOperatorBase CreateSpecificResultOperator();

-        public static IEnumerable<MethodInfo> GetSupportedMethods();

-    }
-    public sealed class WhereExpressionNode : MethodCallExpressionNodeBase {
 {
-        public WhereExpressionNode(MethodCallExpressionParseInfo parseInfo, LambdaExpression predicate);

-        public LambdaExpression Predicate { get; }

-        protected override void ApplyNodeSpecificSemantics(QueryModel queryModel, ClauseGenerationContext clauseGenerationContext);

-        public Expression GetResolvedPredicate(ClauseGenerationContext clauseGenerationContext);

-        public static IEnumerable<MethodInfo> GetSupportedMethods();

-        public override Expression Resolve(ParameterExpression inputParameter, Expression expressionToBeResolved, ClauseGenerationContext clauseGenerationContext);

-    }
-}
```

