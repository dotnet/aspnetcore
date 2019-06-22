# Remotion.Linq.Parsing.ExpressionVisitors.Transformation.PredefinedTransformations

``` diff
-namespace Remotion.Linq.Parsing.ExpressionVisitors.Transformation.PredefinedTransformations {
 {
-    public class AttributeEvaluatingExpressionTransformer : IExpressionTransformer<Expression> {
 {
-        public AttributeEvaluatingExpressionTransformer();

-        public ExpressionType[] SupportedExpressionTypes { get; }

-        public Expression Transform(Expression expression);

-        public interface IMethodCallExpressionTransformerAttribute {
 {
-            IExpressionTransformer<MethodCallExpression> GetExpressionTransformer(MethodCallExpression expression);

-        }
-    }
-    public class DictionaryEntryNewExpressionTransformer : MemberAddingNewExpressionTransformerBase {
 {
-        public DictionaryEntryNewExpressionTransformer();

-        protected override bool CanAddMembers(Type instantiatedType, ReadOnlyCollection<Expression> arguments);

-        protected override MemberInfo[] GetMembers(ConstructorInfo constructorInfo, ReadOnlyCollection<Expression> arguments);

-    }
-    public class InvocationOfLambdaExpressionTransformer : IExpressionTransformer<InvocationExpression> {
 {
-        public InvocationOfLambdaExpressionTransformer();

-        public ExpressionType[] SupportedExpressionTypes { get; }

-        public Expression Transform(InvocationExpression expression);

-    }
-    public class KeyValuePairNewExpressionTransformer : MemberAddingNewExpressionTransformerBase {
 {
-        public KeyValuePairNewExpressionTransformer();

-        protected override bool CanAddMembers(Type instantiatedType, ReadOnlyCollection<Expression> arguments);

-        protected override MemberInfo[] GetMembers(ConstructorInfo constructorInfo, ReadOnlyCollection<Expression> arguments);

-    }
-    public abstract class MemberAddingNewExpressionTransformerBase : IExpressionTransformer<NewExpression> {
 {
-        protected MemberAddingNewExpressionTransformerBase();

-        public ExpressionType[] SupportedExpressionTypes { get; }

-        protected abstract bool CanAddMembers(Type instantiatedType, ReadOnlyCollection<Expression> arguments);

-        protected MemberInfo GetMemberForNewExpression(Type instantiatedType, string propertyName);

-        protected abstract MemberInfo[] GetMembers(ConstructorInfo constructorInfo, ReadOnlyCollection<Expression> arguments);

-        public Expression Transform(NewExpression expression);

-    }
-    public class MethodCallExpressionTransformerAttribute : Attribute, AttributeEvaluatingExpressionTransformer.IMethodCallExpressionTransformerAttribute {
 {
-        public MethodCallExpressionTransformerAttribute(Type transformerType);

-        public Type TransformerType { get; }

-        public IExpressionTransformer<MethodCallExpression> GetExpressionTransformer(MethodCallExpression expression);

-    }
-    public class NullableValueTransformer : IExpressionTransformer<MemberExpression> {
 {
-        public NullableValueTransformer();

-        public ExpressionType[] SupportedExpressionTypes { get; }

-        public Expression Transform(MemberExpression expression);

-    }
-    public class TupleNewExpressionTransformer : MemberAddingNewExpressionTransformerBase {
 {
-        public TupleNewExpressionTransformer();

-        protected override bool CanAddMembers(Type instantiatedType, ReadOnlyCollection<Expression> arguments);

-        protected override MemberInfo[] GetMembers(ConstructorInfo constructorInfo, ReadOnlyCollection<Expression> arguments);

-    }
-    public class VBCompareStringExpressionTransformer : IExpressionTransformer<BinaryExpression> {
 {
-        public VBCompareStringExpressionTransformer();

-        public ExpressionType[] SupportedExpressionTypes { get; }

-        public Expression Transform(BinaryExpression expression);

-    }
-    public class VBInformationIsNothingExpressionTransformer : IExpressionTransformer<MethodCallExpression> {
 {
-        public VBInformationIsNothingExpressionTransformer();

-        public ExpressionType[] SupportedExpressionTypes { get; }

-        public Expression Transform(MethodCallExpression expression);

-    }
-}
```

