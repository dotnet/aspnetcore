# Remotion.Linq.Parsing.ExpressionVisitors.TreeEvaluation

``` diff
-namespace Remotion.Linq.Parsing.ExpressionVisitors.TreeEvaluation {
 {
-    public abstract class EvaluatableExpressionFilterBase : IEvaluatableExpressionFilter {
 {
-        protected EvaluatableExpressionFilterBase();

-        public virtual bool IsEvaluatableBinary(BinaryExpression node);

-        public virtual bool IsEvaluatableBlock(BlockExpression node);

-        public virtual bool IsEvaluatableCatchBlock(CatchBlock node);

-        public virtual bool IsEvaluatableConditional(ConditionalExpression node);

-        public virtual bool IsEvaluatableConstant(ConstantExpression node);

-        public virtual bool IsEvaluatableDebugInfo(DebugInfoExpression node);

-        public virtual bool IsEvaluatableDefault(DefaultExpression node);

-        public virtual bool IsEvaluatableElementInit(ElementInit node);

-        public virtual bool IsEvaluatableGoto(GotoExpression node);

-        public virtual bool IsEvaluatableIndex(IndexExpression node);

-        public virtual bool IsEvaluatableInvocation(InvocationExpression node);

-        public virtual bool IsEvaluatableLabel(LabelExpression node);

-        public virtual bool IsEvaluatableLabelTarget(LabelTarget node);

-        public virtual bool IsEvaluatableLambda(LambdaExpression node);

-        public virtual bool IsEvaluatableListInit(ListInitExpression node);

-        public virtual bool IsEvaluatableLoop(LoopExpression node);

-        public virtual bool IsEvaluatableMember(MemberExpression node);

-        public virtual bool IsEvaluatableMemberAssignment(MemberAssignment node);

-        public virtual bool IsEvaluatableMemberInit(MemberInitExpression node);

-        public virtual bool IsEvaluatableMemberListBinding(MemberListBinding node);

-        public virtual bool IsEvaluatableMemberMemberBinding(MemberMemberBinding node);

-        public virtual bool IsEvaluatableMethodCall(MethodCallExpression node);

-        public virtual bool IsEvaluatableNew(NewExpression node);

-        public virtual bool IsEvaluatableNewArray(NewArrayExpression node);

-        public virtual bool IsEvaluatableSwitch(SwitchExpression node);

-        public virtual bool IsEvaluatableSwitchCase(SwitchCase node);

-        public virtual bool IsEvaluatableTry(TryExpression node);

-        public virtual bool IsEvaluatableTypeBinary(TypeBinaryExpression node);

-        public virtual bool IsEvaluatableUnary(UnaryExpression node);

-    }
-    public sealed class EvaluatableTreeFindingExpressionVisitor : RelinqExpressionVisitor, IPartialEvaluationExceptionExpressionVisitor {
 {
-        public static PartialEvaluationInfo Analyze(Expression expressionTree, IEvaluatableExpressionFilter evaluatableExpressionFilter);

-        public override Expression Visit(Expression expression);

-        protected override Expression VisitBinary(BinaryExpression expression);

-        protected override Expression VisitBlock(BlockExpression expression);

-        protected override CatchBlock VisitCatchBlock(CatchBlock node);

-        protected override Expression VisitConditional(ConditionalExpression expression);

-        protected override Expression VisitConstant(ConstantExpression expression);

-        protected override Expression VisitDebugInfo(DebugInfoExpression expression);

-        protected override Expression VisitDefault(DefaultExpression expression);

-        protected override ElementInit VisitElementInit(ElementInit node);

-        protected override Expression VisitGoto(GotoExpression expression);

-        protected override Expression VisitIndex(IndexExpression expression);

-        protected override Expression VisitInvocation(InvocationExpression expression);

-        protected override Expression VisitLabel(LabelExpression expression);

-        protected override LabelTarget VisitLabelTarget(LabelTarget node);

-        protected override Expression VisitLambda<T>(Expression<T> expression);

-        protected override Expression VisitListInit(ListInitExpression expression);

-        protected override Expression VisitLoop(LoopExpression expression);

-        protected override Expression VisitMember(MemberExpression expression);

-        protected override MemberAssignment VisitMemberAssignment(MemberAssignment node);

-        protected override Expression VisitMemberInit(MemberInitExpression expression);

-        protected override MemberListBinding VisitMemberListBinding(MemberListBinding node);

-        protected override MemberMemberBinding VisitMemberMemberBinding(MemberMemberBinding node);

-        protected override Expression VisitMethodCall(MethodCallExpression expression);

-        protected override Expression VisitNew(NewExpression expression);

-        protected override Expression VisitNewArray(NewArrayExpression expression);

-        protected override Expression VisitParameter(ParameterExpression expression);

-        public Expression VisitPartialEvaluationException(PartialEvaluationExceptionExpression partialEvaluationExceptionExpression);

-        protected override Expression VisitSwitch(SwitchExpression expression);

-        protected override SwitchCase VisitSwitchCase(SwitchCase node);

-        protected override Expression VisitTry(TryExpression expression);

-        protected override Expression VisitTypeBinary(TypeBinaryExpression expression);

-        protected override Expression VisitUnary(UnaryExpression expression);

-    }
-    public interface IEvaluatableExpressionFilter {
 {
-        bool IsEvaluatableBinary(BinaryExpression node);

-        bool IsEvaluatableBlock(BlockExpression node);

-        bool IsEvaluatableCatchBlock(CatchBlock node);

-        bool IsEvaluatableConditional(ConditionalExpression node);

-        bool IsEvaluatableConstant(ConstantExpression node);

-        bool IsEvaluatableDebugInfo(DebugInfoExpression node);

-        bool IsEvaluatableDefault(DefaultExpression node);

-        bool IsEvaluatableElementInit(ElementInit node);

-        bool IsEvaluatableGoto(GotoExpression node);

-        bool IsEvaluatableIndex(IndexExpression node);

-        bool IsEvaluatableInvocation(InvocationExpression node);

-        bool IsEvaluatableLabel(LabelExpression node);

-        bool IsEvaluatableLabelTarget(LabelTarget node);

-        bool IsEvaluatableLambda(LambdaExpression node);

-        bool IsEvaluatableListInit(ListInitExpression node);

-        bool IsEvaluatableLoop(LoopExpression node);

-        bool IsEvaluatableMember(MemberExpression node);

-        bool IsEvaluatableMemberAssignment(MemberAssignment node);

-        bool IsEvaluatableMemberInit(MemberInitExpression node);

-        bool IsEvaluatableMemberListBinding(MemberListBinding node);

-        bool IsEvaluatableMemberMemberBinding(MemberMemberBinding node);

-        bool IsEvaluatableMethodCall(MethodCallExpression node);

-        bool IsEvaluatableNew(NewExpression node);

-        bool IsEvaluatableNewArray(NewArrayExpression node);

-        bool IsEvaluatableSwitch(SwitchExpression node);

-        bool IsEvaluatableSwitchCase(SwitchCase node);

-        bool IsEvaluatableTry(TryExpression node);

-        bool IsEvaluatableTypeBinary(TypeBinaryExpression node);

-        bool IsEvaluatableUnary(UnaryExpression node);

-    }
-    public class PartialEvaluationInfo {
 {
-        public PartialEvaluationInfo();

-        public int Count { get; }

-        public void AddEvaluatableExpression(Expression expression);

-        public bool IsEvaluatableExpression(Expression expression);

-    }
-}
```

