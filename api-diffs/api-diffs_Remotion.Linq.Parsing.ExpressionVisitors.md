# Remotion.Linq.Parsing.ExpressionVisitors

``` diff
-namespace Remotion.Linq.Parsing.ExpressionVisitors {
 {
-    public sealed class MultiReplacingExpressionVisitor : RelinqExpressionVisitor {
 {
-        public static Expression Replace(IDictionary<Expression, Expression> expressionMapping, Expression sourceTree);

-        public override Expression Visit(Expression expression);

-        protected internal override Expression VisitSubQuery(SubQueryExpression expression);

-    }
-    public sealed class PartialEvaluatingExpressionVisitor : RelinqExpressionVisitor {
 {
-        public static Expression EvaluateIndependentSubtrees(Expression expressionTree, IEvaluatableExpressionFilter evaluatableExpressionFilter);

-        public override Expression Visit(Expression expression);

-    }
-    public sealed class ReplacingExpressionVisitor : RelinqExpressionVisitor {
 {
-        public static Expression Replace(Expression replacedExpression, Expression replacementExpression, Expression sourceTree);

-        public override Expression Visit(Expression expression);

-        protected internal override Expression VisitSubQuery(SubQueryExpression expression);

-    }
-    public sealed class SubQueryFindingExpressionVisitor : RelinqExpressionVisitor {
 {
-        public static Expression Process(Expression expressionTree, INodeTypeProvider nodeTypeProvider);

-        public override Expression Visit(Expression expression);

-    }
-    public sealed class TransformingExpressionVisitor : RelinqExpressionVisitor {
 {
-        public static Expression Transform(Expression expression, IExpressionTranformationProvider tranformationProvider);

-        public override Expression Visit(Expression expression);

-    }
-    public sealed class TransparentIdentifierRemovingExpressionVisitor : RelinqExpressionVisitor {
 {
-        public static Expression ReplaceTransparentIdentifiers(Expression expression);

-        protected override Expression VisitMember(MemberExpression memberExpression);

-        protected internal override Expression VisitSubQuery(SubQueryExpression expression);

-    }
-}
```

