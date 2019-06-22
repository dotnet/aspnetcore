# Remotion.Linq.Parsing.Structure.ExpressionTreeProcessors

``` diff
-namespace Remotion.Linq.Parsing.Structure.ExpressionTreeProcessors {
 {
-    public sealed class CompoundExpressionTreeProcessor : IExpressionTreeProcessor {
 {
-        public CompoundExpressionTreeProcessor(IEnumerable<IExpressionTreeProcessor> innerProcessors);

-        public IList<IExpressionTreeProcessor> InnerProcessors { get; }

-        public Expression Process(Expression expressionTree);

-    }
-    public sealed class NullExpressionTreeProcessor : IExpressionTreeProcessor {
 {
-        public NullExpressionTreeProcessor();

-        public Expression Process(Expression expressionTree);

-    }
-    public sealed class PartialEvaluatingExpressionTreeProcessor : IExpressionTreeProcessor {
 {
-        public PartialEvaluatingExpressionTreeProcessor(IEvaluatableExpressionFilter filter);

-        public IEvaluatableExpressionFilter Filter { get; }

-        public Expression Process(Expression expressionTree);

-    }
-    public sealed class TransformingExpressionTreeProcessor : IExpressionTreeProcessor {
 {
-        public TransformingExpressionTreeProcessor(IExpressionTranformationProvider provider);

-        public IExpressionTranformationProvider Provider { get; }

-        public Expression Process(Expression expressionTree);

-    }
-}
```

