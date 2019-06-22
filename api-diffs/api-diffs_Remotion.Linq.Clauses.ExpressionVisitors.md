# Remotion.Linq.Clauses.ExpressionVisitors

``` diff
-namespace Remotion.Linq.Clauses.ExpressionVisitors {
 {
-    public sealed class AccessorFindingExpressionVisitor : RelinqExpressionVisitor {
 {
-        public static LambdaExpression FindAccessorLambda(Expression searchedExpression, Expression fullExpression, ParameterExpression inputParameter);

-        public override Expression Visit(Expression expression);

-        protected override MemberAssignment VisitMemberAssignment(MemberAssignment memberAssigment);

-        protected override MemberBinding VisitMemberBinding(MemberBinding memberBinding);

-        protected override Expression VisitNew(NewExpression expression);

-        protected override Expression VisitUnary(UnaryExpression expression);

-    }
-    public sealed class CloningExpressionVisitor : RelinqExpressionVisitor {
 {
-        public static Expression AdjustExpressionAfterCloning(Expression expression, QuerySourceMapping querySourceMapping);

-        protected internal override Expression VisitQuerySourceReference(QuerySourceReferenceExpression expression);

-        protected internal override Expression VisitSubQuery(SubQueryExpression expression);

-    }
-    public sealed class ReferenceReplacingExpressionVisitor : RelinqExpressionVisitor {
 {
-        public static Expression ReplaceClauseReferences(Expression expression, QuerySourceMapping querySourceMapping, bool throwOnUnmappedReferences);

-        protected internal override Expression VisitQuerySourceReference(QuerySourceReferenceExpression expression);

-        protected internal override Expression VisitSubQuery(SubQueryExpression expression);

-    }
-    public sealed class ReverseResolvingExpressionVisitor : RelinqExpressionVisitor {
 {
-        public static LambdaExpression ReverseResolve(Expression itemExpression, Expression resolvedExpression);

-        public static LambdaExpression ReverseResolveLambda(Expression itemExpression, LambdaExpression resolvedExpression, int parameterInsertionPosition);

-        protected internal override Expression VisitQuerySourceReference(QuerySourceReferenceExpression expression);

-    }
-}
```

