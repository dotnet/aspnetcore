# Remotion.Linq.Parsing.ExpressionVisitors.Transformation

``` diff
-namespace Remotion.Linq.Parsing.ExpressionVisitors.Transformation {
 {
-    public delegate Expression ExpressionTransformation(Expression expression);

-    public class ExpressionTransformerRegistry : IExpressionTranformationProvider {
 {
-        public ExpressionTransformerRegistry();

-        public int RegisteredTransformerCount { get; }

-        public static ExpressionTransformerRegistry CreateDefault();

-        public ExpressionTransformation[] GetAllTransformations(ExpressionType expressionType);

-        public IEnumerable<ExpressionTransformation> GetTransformations(Expression expression);

-        public void Register<T>(IExpressionTransformer<T> transformer) where T : Expression;

-    }
-    public interface IExpressionTranformationProvider {
 {
-        IEnumerable<ExpressionTransformation> GetTransformations(Expression expression);

-    }
-    public interface IExpressionTransformer<T> where T : Expression {
 {
-        ExpressionType[] SupportedExpressionTypes { get; }

-        Expression Transform(T expression);

-    }
-}
```

