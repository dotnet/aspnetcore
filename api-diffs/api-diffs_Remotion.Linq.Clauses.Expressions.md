# Remotion.Linq.Clauses.Expressions

``` diff
-namespace Remotion.Linq.Clauses.Expressions {
 {
-    public interface IPartialEvaluationExceptionExpressionVisitor {
 {
-        Expression VisitPartialEvaluationException(PartialEvaluationExceptionExpression partialEvaluationExceptionExpression);

-    }
-    public interface IVBSpecificExpressionVisitor {
 {
-        Expression VisitVBStringComparison(VBStringComparisonExpression vbStringComparisonExpression);

-    }
-    public sealed class PartialEvaluationExceptionExpression : Expression {
 {
-        public PartialEvaluationExceptionExpression(Exception exception, Expression evaluatedExpression);

-        public override bool CanReduce { get; }

-        public Expression EvaluatedExpression { get; }

-        public Exception Exception { get; }

-        public override ExpressionType NodeType { get; }

-        public override Type Type { get; }

-        protected override Expression Accept(ExpressionVisitor visitor);

-        public override Expression Reduce();

-        public override string ToString();

-        protected override Expression VisitChildren(ExpressionVisitor visitor);

-    }
-    public sealed class QuerySourceReferenceExpression : Expression {
 {
-        public QuerySourceReferenceExpression(IQuerySource querySource);

-        public override ExpressionType NodeType { get; }

-        public IQuerySource ReferencedQuerySource { get; private set; }

-        public override Type Type { get; }

-        protected override Expression Accept(ExpressionVisitor visitor);

-        public override bool Equals(object obj);

-        public override int GetHashCode();

-        public override string ToString();

-    }
-    public sealed class SubQueryExpression : Expression {
 {
-        public SubQueryExpression(QueryModel queryModel);

-        public override ExpressionType NodeType { get; }

-        public QueryModel QueryModel { get; private set; }

-        public override Type Type { get; }

-        protected override Expression Accept(ExpressionVisitor visitor);

-        public override string ToString();

-    }
-    public sealed class VBStringComparisonExpression : Expression {
 {
-        public VBStringComparisonExpression(Expression comparison, bool textCompare);

-        public override bool CanReduce { get; }

-        public Expression Comparison { get; }

-        public override ExpressionType NodeType { get; }

-        public bool TextCompare { get; }

-        public override Type Type { get; }

-        protected override Expression Accept(ExpressionVisitor visitor);

-        public override Expression Reduce();

-        public override string ToString();

-        protected override Expression VisitChildren(ExpressionVisitor visitor);

-    }
-}
```

