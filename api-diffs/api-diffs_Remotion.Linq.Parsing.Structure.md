# Remotion.Linq.Parsing.Structure

``` diff
-namespace Remotion.Linq.Parsing.Structure {
 {
-    public sealed class ExpressionTreeParser {
 {
-        public ExpressionTreeParser(INodeTypeProvider nodeTypeProvider, IExpressionTreeProcessor processor);

-        public INodeTypeProvider NodeTypeProvider { get; }

-        public IExpressionTreeProcessor Processor { get; }

-        public static ExpressionTreeParser CreateDefault();

-        public static CompoundNodeTypeProvider CreateDefaultNodeTypeProvider();

-        public static CompoundExpressionTreeProcessor CreateDefaultProcessor(IExpressionTranformationProvider tranformationProvider, IEvaluatableExpressionFilter evaluatableExpressionFilter = null);

-        public MethodCallExpression GetQueryOperatorExpression(Expression expression);

-        public IExpressionNode ParseTree(Expression expressionTree);

-    }
-    public interface IExpressionTreeProcessor {
 {
-        Expression Process(Expression expressionTree);

-    }
-    public interface INodeTypeProvider {
 {
-        Type GetNodeType(MethodInfo method);

-        bool IsRegistered(MethodInfo method);

-    }
-    public interface IQueryParser {
 {
-        QueryModel GetParsedQuery(Expression expressionTreeRoot);

-    }
-    public sealed class MethodCallExpressionParser {
 {
-        public MethodCallExpressionParser(INodeTypeProvider nodeTypeProvider);

-        public IExpressionNode Parse(string associatedIdentifier, IExpressionNode source, IEnumerable<Expression> arguments, MethodCallExpression expressionToParse);

-    }
-    public sealed class QueryParser : IQueryParser {
 {
-        public QueryParser(ExpressionTreeParser expressionTreeParser);

-        public ExpressionTreeParser ExpressionTreeParser { get; }

-        public INodeTypeProvider NodeTypeProvider { get; }

-        public IExpressionTreeProcessor Processor { get; }

-        public static QueryParser CreateDefault();

-        public QueryModel GetParsedQuery(Expression expressionTreeRoot);

-    }
-}
```

