# Remotion.Linq.Parsing

``` diff
-namespace Remotion.Linq.Parsing {
 {
-    public abstract class ParserException : Exception

-    public abstract class RelinqExpressionVisitor : ExpressionVisitor {
 {
-        protected RelinqExpressionVisitor();

-        public static IEnumerable<Expression> AdjustArgumentsForNewExpression(IList<Expression> arguments, IList<MemberInfo> members);

-        protected override Expression VisitNew(NewExpression expression);

-        protected internal virtual Expression VisitQuerySourceReference(QuerySourceReferenceExpression expression);

-        protected internal virtual Expression VisitSubQuery(SubQueryExpression expression);

-    }
-    public abstract class ThrowingExpressionVisitor : RelinqExpressionVisitor {
 {
-        protected ThrowingExpressionVisitor();

-        protected Expression BaseVisitBinary(BinaryExpression expression);

-        protected Expression BaseVisitBlock(BlockExpression expression);

-        protected CatchBlock BaseVisitCatchBlock(CatchBlock expression);

-        protected Expression BaseVisitConditional(ConditionalExpression arg);

-        protected Expression BaseVisitConstant(ConstantExpression expression);

-        protected Expression BaseVisitDebugInfo(DebugInfoExpression expression);

-        protected Expression BaseVisitDefault(DefaultExpression expression);

-        protected ElementInit BaseVisitElementInit(ElementInit elementInit);

-        protected Expression BaseVisitExtension(Expression expression);

-        protected Expression BaseVisitGoto(GotoExpression expression);

-        protected Expression BaseVisitIndex(IndexExpression expression);

-        protected Expression BaseVisitInvocation(InvocationExpression expression);

-        protected Expression BaseVisitLabel(LabelExpression expression);

-        protected LabelTarget BaseVisitLabelTarget(LabelTarget expression);

-        protected Expression BaseVisitLambda<T>(Expression<T> expression);

-        protected Expression BaseVisitListInit(ListInitExpression expression);

-        protected Expression BaseVisitLoop(LoopExpression expression);

-        protected Expression BaseVisitMember(MemberExpression expression);

-        protected MemberAssignment BaseVisitMemberAssignment(MemberAssignment memberAssigment);

-        protected MemberBinding BaseVisitMemberBinding(MemberBinding expression);

-        protected Expression BaseVisitMemberInit(MemberInitExpression expression);

-        protected MemberListBinding BaseVisitMemberListBinding(MemberListBinding listBinding);

-        protected MemberMemberBinding BaseVisitMemberMemberBinding(MemberMemberBinding binding);

-        protected Expression BaseVisitMethodCall(MethodCallExpression expression);

-        protected Expression BaseVisitNew(NewExpression expression);

-        protected Expression BaseVisitNewArray(NewArrayExpression expression);

-        protected Expression BaseVisitParameter(ParameterExpression expression);

-        protected Expression BaseVisitQuerySourceReference(QuerySourceReferenceExpression expression);

-        protected Expression BaseVisitRuntimeVariables(RuntimeVariablesExpression expression);

-        protected Expression BaseVisitSubQuery(SubQueryExpression expression);

-        protected Expression BaseVisitSwitch(SwitchExpression expression);

-        protected SwitchCase BaseVisitSwitchCase(SwitchCase expression);

-        protected Expression BaseVisitTry(TryExpression expression);

-        protected Expression BaseVisitTypeBinary(TypeBinaryExpression expression);

-        protected Expression BaseVisitUnary(UnaryExpression expression);

-        protected abstract Exception CreateUnhandledItemException<T>(T unhandledItem, string visitMethod);

-        public override Expression Visit(Expression expression);

-        protected override Expression VisitBinary(BinaryExpression expression);

-        protected override Expression VisitBlock(BlockExpression expression);

-        protected override CatchBlock VisitCatchBlock(CatchBlock expression);

-        protected override Expression VisitConditional(ConditionalExpression expression);

-        protected override Expression VisitConstant(ConstantExpression expression);

-        protected override Expression VisitDebugInfo(DebugInfoExpression expression);

-        protected override Expression VisitDefault(DefaultExpression expression);

-        protected override ElementInit VisitElementInit(ElementInit elementInit);

-        protected override Expression VisitExtension(Expression expression);

-        protected override Expression VisitGoto(GotoExpression expression);

-        protected override Expression VisitIndex(IndexExpression expression);

-        protected override Expression VisitInvocation(InvocationExpression expression);

-        protected override Expression VisitLabel(LabelExpression expression);

-        protected override LabelTarget VisitLabelTarget(LabelTarget expression);

-        protected override Expression VisitLambda<T>(Expression<T> expression);

-        protected override Expression VisitListInit(ListInitExpression expression);

-        protected override Expression VisitLoop(LoopExpression expression);

-        protected override Expression VisitMember(MemberExpression expression);

-        protected override MemberAssignment VisitMemberAssignment(MemberAssignment memberAssigment);

-        protected override MemberBinding VisitMemberBinding(MemberBinding expression);

-        protected override Expression VisitMemberInit(MemberInitExpression expression);

-        protected override MemberListBinding VisitMemberListBinding(MemberListBinding listBinding);

-        protected override MemberMemberBinding VisitMemberMemberBinding(MemberMemberBinding binding);

-        protected override Expression VisitMethodCall(MethodCallExpression expression);

-        protected override Expression VisitNew(NewExpression expression);

-        protected override Expression VisitNewArray(NewArrayExpression expression);

-        protected override Expression VisitParameter(ParameterExpression expression);

-        protected internal override Expression VisitQuerySourceReference(QuerySourceReferenceExpression expression);

-        protected override Expression VisitRuntimeVariables(RuntimeVariablesExpression expression);

-        protected internal override Expression VisitSubQuery(SubQueryExpression expression);

-        protected override Expression VisitSwitch(SwitchExpression expression);

-        protected override SwitchCase VisitSwitchCase(SwitchCase expression);

-        protected override Expression VisitTry(TryExpression expression);

-        protected override Expression VisitTypeBinary(TypeBinaryExpression expression);

-        protected override Expression VisitUnary(UnaryExpression expression);

-        protected virtual TResult VisitUnhandledItem<TItem, TResult>(TItem unhandledItem, string visitMethod, Func<TItem, TResult> baseBehavior) where TItem : TResult;

-        protected virtual Expression VisitUnknownStandardExpression(Expression expression, string visitMethod, Func<Expression, Expression> baseBehavior);

-    }
-    public static class TupleExpressionBuilder {
 {
-        public static Expression AggregateExpressionsIntoTuple(IEnumerable<Expression> expressions);

-        public static IEnumerable<Expression> GetExpressionsFromTuple(Expression tupleExpression);

-    }
-}
```

