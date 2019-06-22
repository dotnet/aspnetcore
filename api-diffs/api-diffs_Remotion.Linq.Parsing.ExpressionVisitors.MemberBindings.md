# Remotion.Linq.Parsing.ExpressionVisitors.MemberBindings

``` diff
-namespace Remotion.Linq.Parsing.ExpressionVisitors.MemberBindings {
 {
-    public class FieldInfoBinding : MemberBinding {
 {
-        public FieldInfoBinding(FieldInfo boundMember, Expression associatedExpression);

-        public override bool MatchesReadAccess(MemberInfo member);

-    }
-    public abstract class MemberBinding {
 {
-        public MemberBinding(MemberInfo boundMember, Expression associatedExpression);

-        public Expression AssociatedExpression { get; }

-        public MemberInfo BoundMember { get; }

-        public static MemberBinding Bind(MemberInfo boundMember, Expression associatedExpression);

-        public abstract bool MatchesReadAccess(MemberInfo member);

-    }
-    public class MethodInfoBinding : MemberBinding {
 {
-        public MethodInfoBinding(MethodInfo boundMember, Expression associatedExpression);

-        public override bool MatchesReadAccess(MemberInfo readMember);

-    }
-    public class PropertyInfoBinding : MemberBinding {
 {
-        public PropertyInfoBinding(PropertyInfo boundMember, Expression associatedExpression);

-        public override bool MatchesReadAccess(MemberInfo member);

-    }
-}
```

