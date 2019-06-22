# Microsoft.CodeAnalysis.Operations

``` diff
-namespace Microsoft.CodeAnalysis.Operations {
 {
-    public enum ArgumentKind {
 {
-        DefaultValue = 3,

-        Explicit = 1,

-        None = 0,

-        ParamArray = 2,

-    }
-    public enum BinaryOperatorKind {
 {
-        Add = 1,

-        And = 10,

-        Concatenate = 15,

-        ConditionalAnd = 13,

-        ConditionalOr = 14,

-        Divide = 4,

-        Equals = 16,

-        ExclusiveOr = 12,

-        GreaterThan = 23,

-        GreaterThanOrEqual = 22,

-        IntegerDivide = 5,

-        LeftShift = 8,

-        LessThan = 20,

-        LessThanOrEqual = 21,

-        Like = 24,

-        Multiply = 3,

-        None = 0,

-        NotEquals = 18,

-        ObjectValueEquals = 17,

-        ObjectValueNotEquals = 19,

-        Or = 11,

-        Power = 7,

-        Remainder = 6,

-        RightShift = 9,

-        Subtract = 2,

-    }
-    public enum BranchKind {
 {
-        Break = 2,

-        Continue = 1,

-        GoTo = 3,

-        None = 0,

-    }
-    public enum CaseKind {
 {
-        Default = 4,

-        None = 0,

-        Pattern = 5,

-        Range = 3,

-        Relational = 2,

-        SingleValue = 1,

-    }
-    public struct CommonConversion {
 {
-        public bool Exists { get; }

-        public bool IsIdentity { get; }

-        public bool IsNumeric { get; }

-        public bool IsReference { get; }

-        public bool IsUserDefined { get; }

-        public IMethodSymbol MethodSymbol { get; }

-    }
-    public interface IAddressOfOperation : IOperation {
 {
-        IOperation Reference { get; }

-    }
-    public interface IAnonymousFunctionOperation : IOperation {
 {
-        IBlockOperation Body { get; }

-        IMethodSymbol Symbol { get; }

-    }
-    public interface IAnonymousObjectCreationOperation : IOperation {
 {
-        ImmutableArray<IOperation> Initializers { get; }

-    }
-    public interface IArgumentOperation : IOperation {
 {
-        ArgumentKind ArgumentKind { get; }

-        CommonConversion InConversion { get; }

-        CommonConversion OutConversion { get; }

-        IParameterSymbol Parameter { get; }

-        IOperation Value { get; }

-    }
-    public interface IArrayCreationOperation : IOperation {
 {
-        ImmutableArray<IOperation> DimensionSizes { get; }

-        IArrayInitializerOperation Initializer { get; }

-    }
-    public interface IArrayElementReferenceOperation : IOperation {
 {
-        IOperation ArrayReference { get; }

-        ImmutableArray<IOperation> Indices { get; }

-    }
-    public interface IArrayInitializerOperation : IOperation {
 {
-        ImmutableArray<IOperation> ElementValues { get; }

-    }
-    public interface IAssignmentOperation : IOperation {
 {
-        IOperation Target { get; }

-        IOperation Value { get; }

-    }
-    public interface IAwaitOperation : IOperation {
 {
-        IOperation Operation { get; }

-    }
-    public interface IBinaryOperation : IOperation {
 {
-        bool IsChecked { get; }

-        bool IsCompareText { get; }

-        bool IsLifted { get; }

-        IOperation LeftOperand { get; }

-        BinaryOperatorKind OperatorKind { get; }

-        IMethodSymbol OperatorMethod { get; }

-        IOperation RightOperand { get; }

-    }
-    public interface IBlockOperation : IOperation {
 {
-        ImmutableArray<ILocalSymbol> Locals { get; }

-        ImmutableArray<IOperation> Operations { get; }

-    }
-    public interface IBranchOperation : IOperation {
 {
-        BranchKind BranchKind { get; }

-        ILabelSymbol Target { get; }

-    }
-    public interface ICaseClauseOperation : IOperation {
 {
-        CaseKind CaseKind { get; }

-    }
-    public interface ICatchClauseOperation : IOperation {
 {
-        IOperation ExceptionDeclarationOrExpression { get; }

-        ITypeSymbol ExceptionType { get; }

-        IOperation Filter { get; }

-        IBlockOperation Handler { get; }

-        ImmutableArray<ILocalSymbol> Locals { get; }

-    }
-    public interface ICoalesceOperation : IOperation {
 {
-        IOperation Value { get; }

-        IOperation WhenNull { get; }

-    }
-    public interface ICollectionElementInitializerOperation : IOperation {
 {
-        IMethodSymbol AddMethod { get; }

-        ImmutableArray<IOperation> Arguments { get; }

-        bool IsDynamic { get; }

-    }
-    public interface ICompoundAssignmentOperation : IAssignmentOperation, IOperation {
 {
-        CommonConversion InConversion { get; }

-        bool IsChecked { get; }

-        bool IsLifted { get; }

-        BinaryOperatorKind OperatorKind { get; }

-        IMethodSymbol OperatorMethod { get; }

-        CommonConversion OutConversion { get; }

-    }
-    public interface IConditionalAccessInstanceOperation : IOperation

-    public interface IConditionalAccessOperation : IOperation {
 {
-        IOperation Operation { get; }

-        IOperation WhenNotNull { get; }

-    }
-    public interface IConditionalOperation : IOperation {
 {
-        IOperation Condition { get; }

-        bool IsRef { get; }

-        IOperation WhenFalse { get; }

-        IOperation WhenTrue { get; }

-    }
-    public interface IConstantPatternOperation : IOperation, IPatternOperation {
 {
-        IOperation Value { get; }

-    }
-    public interface IConstructorBodyOperation : IMethodBodyBaseOperation, IOperation {
 {
-        IOperation Initializer { get; }

-        ImmutableArray<ILocalSymbol> Locals { get; }

-    }
-    public interface IConversionOperation : IOperation {
 {
-        CommonConversion Conversion { get; }

-        bool IsChecked { get; }

-        bool IsTryCast { get; }

-        IOperation Operand { get; }

-        IMethodSymbol OperatorMethod { get; }

-    }
-    public interface IDeclarationExpressionOperation : IOperation {
 {
-        IOperation Expression { get; }

-    }
-    public interface IDeclarationPatternOperation : IOperation, IPatternOperation {
 {
-        ISymbol DeclaredSymbol { get; }

-    }
-    public interface IDeconstructionAssignmentOperation : IAssignmentOperation, IOperation

-    public interface IDefaultCaseClauseOperation : ICaseClauseOperation, IOperation

-    public interface IDefaultValueOperation : IOperation

-    public interface IDelegateCreationOperation : IOperation {
 {
-        IOperation Target { get; }

-    }
-    public interface IDiscardOperation : IOperation {
 {
-        IDiscardSymbol DiscardSymbol { get; }

-    }
-    public interface IDynamicIndexerAccessOperation : IOperation {
 {
-        ImmutableArray<IOperation> Arguments { get; }

-        IOperation Operation { get; }

-    }
-    public interface IDynamicInvocationOperation : IOperation {
 {
-        ImmutableArray<IOperation> Arguments { get; }

-        IOperation Operation { get; }

-    }
-    public interface IDynamicMemberReferenceOperation : IOperation {
 {
-        ITypeSymbol ContainingType { get; }

-        IOperation Instance { get; }

-        string MemberName { get; }

-        ImmutableArray<ITypeSymbol> TypeArguments { get; }

-    }
-    public interface IDynamicObjectCreationOperation : IOperation {
 {
-        ImmutableArray<IOperation> Arguments { get; }

-        IObjectOrCollectionInitializerOperation Initializer { get; }

-    }
-    public interface IEmptyOperation : IOperation

-    public interface IEndOperation : IOperation

-    public interface IEventAssignmentOperation : IOperation {
 {
-        bool Adds { get; }

-        IEventReferenceOperation EventReference { get; }

-        IOperation HandlerValue { get; }

-    }
-    public interface IEventReferenceOperation : IMemberReferenceOperation, IOperation {
 {
-        IEventSymbol Event { get; }

-    }
-    public interface IExpressionStatementOperation : IOperation {
 {
-        IOperation Operation { get; }

-    }
-    public interface IFieldInitializerOperation : IOperation, ISymbolInitializerOperation {
 {
-        ImmutableArray<IFieldSymbol> InitializedFields { get; }

-    }
-    public interface IFieldReferenceOperation : IMemberReferenceOperation, IOperation {
 {
-        IFieldSymbol Field { get; }

-        bool IsDeclaration { get; }

-    }
-    public interface IForEachLoopOperation : ILoopOperation, IOperation {
 {
-        IOperation Collection { get; }

-        IOperation LoopControlVariable { get; }

-        ImmutableArray<IOperation> NextVariables { get; }

-    }
-    public interface IForLoopOperation : ILoopOperation, IOperation {
 {
-        ImmutableArray<IOperation> AtLoopBottom { get; }

-        ImmutableArray<IOperation> Before { get; }

-        IOperation Condition { get; }

-    }
-    public interface IForToLoopOperation : ILoopOperation, IOperation {
 {
-        IOperation InitialValue { get; }

-        IOperation LimitValue { get; }

-        IOperation LoopControlVariable { get; }

-        ImmutableArray<IOperation> NextVariables { get; }

-        IOperation StepValue { get; }

-    }
-    public interface IIncrementOrDecrementOperation : IOperation {
 {
-        bool IsChecked { get; }

-        bool IsLifted { get; }

-        bool IsPostfix { get; }

-        IMethodSymbol OperatorMethod { get; }

-        IOperation Target { get; }

-    }
-    public interface IInstanceReferenceOperation : IOperation

-    public interface IInterpolatedStringContentOperation : IOperation

-    public interface IInterpolatedStringOperation : IOperation {
 {
-        ImmutableArray<IInterpolatedStringContentOperation> Parts { get; }

-    }
-    public interface IInterpolatedStringTextOperation : IInterpolatedStringContentOperation, IOperation {
 {
-        IOperation Text { get; }

-    }
-    public interface IInterpolationOperation : IInterpolatedStringContentOperation, IOperation {
 {
-        IOperation Alignment { get; }

-        IOperation Expression { get; }

-        IOperation FormatString { get; }

-    }
-    public interface IInvalidOperation : IOperation

-    public interface IInvocationOperation : IOperation {
 {
-        ImmutableArray<IArgumentOperation> Arguments { get; }

-        IOperation Instance { get; }

-        bool IsVirtual { get; }

-        IMethodSymbol TargetMethod { get; }

-    }
-    public interface IIsPatternOperation : IOperation {
 {
-        IPatternOperation Pattern { get; }

-        IOperation Value { get; }

-    }
-    public interface IIsTypeOperation : IOperation {
 {
-        bool IsNegated { get; }

-        ITypeSymbol TypeOperand { get; }

-        IOperation ValueOperand { get; }

-    }
-    public interface ILabeledOperation : IOperation {
 {
-        ILabelSymbol Label { get; }

-        IOperation Operation { get; }

-    }
-    public interface ILiteralOperation : IOperation

-    public interface ILocalFunctionOperation : IOperation {
 {
-        IBlockOperation Body { get; }

-        IBlockOperation IgnoredBody { get; }

-        IMethodSymbol Symbol { get; }

-    }
-    public interface ILocalReferenceOperation : IOperation {
 {
-        bool IsDeclaration { get; }

-        ILocalSymbol Local { get; }

-    }
-    public interface ILockOperation : IOperation {
 {
-        IOperation Body { get; }

-        IOperation LockedValue { get; }

-    }
-    public interface ILoopOperation : IOperation {
 {
-        IOperation Body { get; }

-        ImmutableArray<ILocalSymbol> Locals { get; }

-        LoopKind LoopKind { get; }

-    }
-    public interface IMemberInitializerOperation : IOperation {
 {
-        IOperation InitializedMember { get; }

-        IObjectOrCollectionInitializerOperation Initializer { get; }

-    }
-    public interface IMemberReferenceOperation : IOperation {
 {
-        IOperation Instance { get; }

-        ISymbol Member { get; }

-    }
-    public interface IMethodBodyBaseOperation : IOperation {
 {
-        IBlockOperation BlockBody { get; }

-        IBlockOperation ExpressionBody { get; }

-    }
-    public interface IMethodBodyOperation : IMethodBodyBaseOperation, IOperation

-    public interface IMethodReferenceOperation : IMemberReferenceOperation, IOperation {
 {
-        bool IsVirtual { get; }

-        IMethodSymbol Method { get; }

-    }
-    public interface INameOfOperation : IOperation {
 {
-        IOperation Argument { get; }

-    }
-    public interface IObjectCreationOperation : IOperation {
 {
-        ImmutableArray<IArgumentOperation> Arguments { get; }

-        IMethodSymbol Constructor { get; }

-        IObjectOrCollectionInitializerOperation Initializer { get; }

-    }
-    public interface IObjectOrCollectionInitializerOperation : IOperation {
 {
-        ImmutableArray<IOperation> Initializers { get; }

-    }
-    public interface IOmittedArgumentOperation : IOperation

-    public interface IParameterInitializerOperation : IOperation, ISymbolInitializerOperation {
 {
-        IParameterSymbol Parameter { get; }

-    }
-    public interface IParameterReferenceOperation : IOperation {
 {
-        IParameterSymbol Parameter { get; }

-    }
-    public interface IParenthesizedOperation : IOperation {
 {
-        IOperation Operand { get; }

-    }
-    public interface IPatternCaseClauseOperation : ICaseClauseOperation, IOperation {
 {
-        IOperation Guard { get; }

-        ILabelSymbol Label { get; }

-        IPatternOperation Pattern { get; }

-    }
-    public interface IPatternOperation : IOperation

-    public interface IPropertyInitializerOperation : IOperation, ISymbolInitializerOperation {
 {
-        ImmutableArray<IPropertySymbol> InitializedProperties { get; }

-    }
-    public interface IPropertyReferenceOperation : IMemberReferenceOperation, IOperation {
 {
-        ImmutableArray<IArgumentOperation> Arguments { get; }

-        IPropertySymbol Property { get; }

-    }
-    public interface IRaiseEventOperation : IOperation {
 {
-        ImmutableArray<IArgumentOperation> Arguments { get; }

-        IEventReferenceOperation EventReference { get; }

-    }
-    public interface IRangeCaseClauseOperation : ICaseClauseOperation, IOperation {
 {
-        IOperation MaximumValue { get; }

-        IOperation MinimumValue { get; }

-    }
-    public interface IRelationalCaseClauseOperation : ICaseClauseOperation, IOperation {
 {
-        BinaryOperatorKind Relation { get; }

-        IOperation Value { get; }

-    }
-    public interface IReturnOperation : IOperation {
 {
-        IOperation ReturnedValue { get; }

-    }
-    public interface ISimpleAssignmentOperation : IAssignmentOperation, IOperation {
 {
-        bool IsRef { get; }

-    }
-    public interface ISingleValueCaseClauseOperation : ICaseClauseOperation, IOperation {
 {
-        IOperation Value { get; }

-    }
-    public interface ISizeOfOperation : IOperation {
 {
-        ITypeSymbol TypeOperand { get; }

-    }
-    public interface IStopOperation : IOperation

-    public interface ISwitchCaseOperation : IOperation {
 {
-        ImmutableArray<IOperation> Body { get; }

-        ImmutableArray<ICaseClauseOperation> Clauses { get; }

-    }
-    public interface ISwitchOperation : IOperation {
 {
-        ImmutableArray<ISwitchCaseOperation> Cases { get; }

-        IOperation Value { get; }

-    }
-    public interface ISymbolInitializerOperation : IOperation {
 {
-        ImmutableArray<ILocalSymbol> Locals { get; }

-        IOperation Value { get; }

-    }
-    public interface IThrowOperation : IOperation {
 {
-        IOperation Exception { get; }

-    }
-    public interface ITranslatedQueryOperation : IOperation {
 {
-        IOperation Operation { get; }

-    }
-    public interface ITryOperation : IOperation {
 {
-        IBlockOperation Body { get; }

-        ImmutableArray<ICatchClauseOperation> Catches { get; }

-        IBlockOperation Finally { get; }

-    }
-    public interface ITupleBinaryOperation : IOperation {
 {
-        IOperation LeftOperand { get; }

-        BinaryOperatorKind OperatorKind { get; }

-        IOperation RightOperand { get; }

-    }
-    public interface ITupleOperation : IOperation {
 {
-        ImmutableArray<IOperation> Elements { get; }

-        ITypeSymbol NaturalType { get; }

-    }
-    public interface ITypeOfOperation : IOperation {
 {
-        ITypeSymbol TypeOperand { get; }

-    }
-    public interface ITypeParameterObjectCreationOperation : IOperation {
 {
-        IObjectOrCollectionInitializerOperation Initializer { get; }

-    }
-    public interface IUnaryOperation : IOperation {
 {
-        bool IsChecked { get; }

-        bool IsLifted { get; }

-        IOperation Operand { get; }

-        UnaryOperatorKind OperatorKind { get; }

-        IMethodSymbol OperatorMethod { get; }

-    }
-    public interface IUsingOperation : IOperation {
 {
-        IOperation Body { get; }

-        IOperation Resources { get; }

-    }
-    public interface IVariableDeclarationGroupOperation : IOperation {
 {
-        ImmutableArray<IVariableDeclarationOperation> Declarations { get; }

-    }
-    public interface IVariableDeclarationOperation : IOperation {
 {
-        ImmutableArray<IVariableDeclaratorOperation> Declarators { get; }

-        IVariableInitializerOperation Initializer { get; }

-    }
-    public interface IVariableDeclaratorOperation : IOperation {
 {
-        ImmutableArray<IOperation> IgnoredArguments { get; }

-        IVariableInitializerOperation Initializer { get; }

-        ILocalSymbol Symbol { get; }

-    }
-    public interface IVariableInitializerOperation : IOperation, ISymbolInitializerOperation

-    public interface IWhileLoopOperation : ILoopOperation, IOperation {
 {
-        IOperation Condition { get; }

-        bool ConditionIsTop { get; }

-        bool ConditionIsUntil { get; }

-        IOperation IgnoredCondition { get; }

-    }
-    public enum LoopKind {
 {
-        For = 2,

-        ForEach = 4,

-        ForTo = 3,

-        None = 0,

-        While = 1,

-    }
-    public static class OperationExtensions {
 {
-        public static IEnumerable<IOperation> Descendants(this IOperation operation);

-        public static IEnumerable<IOperation> DescendantsAndSelf(this IOperation operation);

-        public static string GetArgumentName(this IDynamicIndexerAccessOperation dynamicOperation, int index);

-        public static string GetArgumentName(this IDynamicInvocationOperation dynamicOperation, int index);

-        public static string GetArgumentName(this IDynamicObjectCreationOperation dynamicOperation, int index);

-        public static Nullable<RefKind> GetArgumentRefKind(this IDynamicIndexerAccessOperation dynamicOperation, int index);

-        public static Nullable<RefKind> GetArgumentRefKind(this IDynamicInvocationOperation dynamicOperation, int index);

-        public static Nullable<RefKind> GetArgumentRefKind(this IDynamicObjectCreationOperation dynamicOperation, int index);

-        public static ImmutableArray<ILocalSymbol> GetDeclaredVariables(this IVariableDeclarationGroupOperation declarationGroup);

-        public static ImmutableArray<ILocalSymbol> GetDeclaredVariables(this IVariableDeclarationOperation declaration);

-        public static IVariableInitializerOperation GetVariableInitializer(this IVariableDeclaratorOperation declarationOperation);

-    }
-    public abstract class OperationVisitor {
 {
-        protected OperationVisitor();

-        public virtual void DefaultVisit(IOperation operation);

-        public virtual void Visit(IOperation operation);

-        public virtual void VisitAddressOf(IAddressOfOperation operation);

-        public virtual void VisitAnonymousFunction(IAnonymousFunctionOperation operation);

-        public virtual void VisitAnonymousObjectCreation(IAnonymousObjectCreationOperation operation);

-        public virtual void VisitArgument(IArgumentOperation operation);

-        public virtual void VisitArrayCreation(IArrayCreationOperation operation);

-        public virtual void VisitArrayElementReference(IArrayElementReferenceOperation operation);

-        public virtual void VisitArrayInitializer(IArrayInitializerOperation operation);

-        public virtual void VisitAwait(IAwaitOperation operation);

-        public virtual void VisitBinaryOperator(IBinaryOperation operation);

-        public virtual void VisitBlock(IBlockOperation operation);

-        public virtual void VisitBranch(IBranchOperation operation);

-        public virtual void VisitCatchClause(ICatchClauseOperation operation);

-        public virtual void VisitCoalesce(ICoalesceOperation operation);

-        public virtual void VisitCollectionElementInitializer(ICollectionElementInitializerOperation operation);

-        public virtual void VisitCompoundAssignment(ICompoundAssignmentOperation operation);

-        public virtual void VisitConditional(IConditionalOperation operation);

-        public virtual void VisitConditionalAccess(IConditionalAccessOperation operation);

-        public virtual void VisitConditionalAccessInstance(IConditionalAccessInstanceOperation operation);

-        public virtual void VisitConstantPattern(IConstantPatternOperation operation);

-        public virtual void VisitConstructorBodyOperation(IConstructorBodyOperation operation);

-        public virtual void VisitConversion(IConversionOperation operation);

-        public virtual void VisitDeclarationExpression(IDeclarationExpressionOperation operation);

-        public virtual void VisitDeclarationPattern(IDeclarationPatternOperation operation);

-        public virtual void VisitDeconstructionAssignment(IDeconstructionAssignmentOperation operation);

-        public virtual void VisitDefaultCaseClause(IDefaultCaseClauseOperation operation);

-        public virtual void VisitDefaultValue(IDefaultValueOperation operation);

-        public virtual void VisitDelegateCreation(IDelegateCreationOperation operation);

-        public virtual void VisitDiscardOperation(IDiscardOperation operation);

-        public virtual void VisitDynamicIndexerAccess(IDynamicIndexerAccessOperation operation);

-        public virtual void VisitDynamicInvocation(IDynamicInvocationOperation operation);

-        public virtual void VisitDynamicMemberReference(IDynamicMemberReferenceOperation operation);

-        public virtual void VisitDynamicObjectCreation(IDynamicObjectCreationOperation operation);

-        public virtual void VisitEmpty(IEmptyOperation operation);

-        public virtual void VisitEnd(IEndOperation operation);

-        public virtual void VisitEventAssignment(IEventAssignmentOperation operation);

-        public virtual void VisitEventReference(IEventReferenceOperation operation);

-        public virtual void VisitExpressionStatement(IExpressionStatementOperation operation);

-        public virtual void VisitFieldInitializer(IFieldInitializerOperation operation);

-        public virtual void VisitFieldReference(IFieldReferenceOperation operation);

-        public virtual void VisitForEachLoop(IForEachLoopOperation operation);

-        public virtual void VisitForLoop(IForLoopOperation operation);

-        public virtual void VisitForToLoop(IForToLoopOperation operation);

-        public virtual void VisitIncrementOrDecrement(IIncrementOrDecrementOperation operation);

-        public virtual void VisitInstanceReference(IInstanceReferenceOperation operation);

-        public virtual void VisitInterpolatedString(IInterpolatedStringOperation operation);

-        public virtual void VisitInterpolatedStringText(IInterpolatedStringTextOperation operation);

-        public virtual void VisitInterpolation(IInterpolationOperation operation);

-        public virtual void VisitInvalid(IInvalidOperation operation);

-        public virtual void VisitInvocation(IInvocationOperation operation);

-        public virtual void VisitIsPattern(IIsPatternOperation operation);

-        public virtual void VisitIsType(IIsTypeOperation operation);

-        public virtual void VisitLabeled(ILabeledOperation operation);

-        public virtual void VisitLiteral(ILiteralOperation operation);

-        public virtual void VisitLocalFunction(ILocalFunctionOperation operation);

-        public virtual void VisitLocalReference(ILocalReferenceOperation operation);

-        public virtual void VisitLock(ILockOperation operation);

-        public virtual void VisitMemberInitializer(IMemberInitializerOperation operation);

-        public virtual void VisitMethodBodyOperation(IMethodBodyOperation operation);

-        public virtual void VisitMethodReference(IMethodReferenceOperation operation);

-        public virtual void VisitNameOf(INameOfOperation operation);

-        public virtual void VisitObjectCreation(IObjectCreationOperation operation);

-        public virtual void VisitObjectOrCollectionInitializer(IObjectOrCollectionInitializerOperation operation);

-        public virtual void VisitOmittedArgument(IOmittedArgumentOperation operation);

-        public virtual void VisitParameterInitializer(IParameterInitializerOperation operation);

-        public virtual void VisitParameterReference(IParameterReferenceOperation operation);

-        public virtual void VisitParenthesized(IParenthesizedOperation operation);

-        public virtual void VisitPatternCaseClause(IPatternCaseClauseOperation operation);

-        public virtual void VisitPropertyInitializer(IPropertyInitializerOperation operation);

-        public virtual void VisitPropertyReference(IPropertyReferenceOperation operation);

-        public virtual void VisitRaiseEvent(IRaiseEventOperation operation);

-        public virtual void VisitRangeCaseClause(IRangeCaseClauseOperation operation);

-        public virtual void VisitRelationalCaseClause(IRelationalCaseClauseOperation operation);

-        public virtual void VisitReturn(IReturnOperation operation);

-        public virtual void VisitSimpleAssignment(ISimpleAssignmentOperation operation);

-        public virtual void VisitSingleValueCaseClause(ISingleValueCaseClauseOperation operation);

-        public virtual void VisitSizeOf(ISizeOfOperation operation);

-        public virtual void VisitStop(IStopOperation operation);

-        public virtual void VisitSwitch(ISwitchOperation operation);

-        public virtual void VisitSwitchCase(ISwitchCaseOperation operation);

-        public virtual void VisitThrow(IThrowOperation operation);

-        public virtual void VisitTranslatedQuery(ITranslatedQueryOperation operation);

-        public virtual void VisitTry(ITryOperation operation);

-        public virtual void VisitTuple(ITupleOperation operation);

-        public virtual void VisitTupleBinaryOperator(ITupleBinaryOperation operation);

-        public virtual void VisitTypeOf(ITypeOfOperation operation);

-        public virtual void VisitTypeParameterObjectCreation(ITypeParameterObjectCreationOperation operation);

-        public virtual void VisitUnaryOperator(IUnaryOperation operation);

-        public virtual void VisitUsing(IUsingOperation operation);

-        public virtual void VisitVariableDeclaration(IVariableDeclarationOperation operation);

-        public virtual void VisitVariableDeclarationGroup(IVariableDeclarationGroupOperation operation);

-        public virtual void VisitVariableDeclarator(IVariableDeclaratorOperation operation);

-        public virtual void VisitVariableInitializer(IVariableInitializerOperation operation);

-        public virtual void VisitWhileLoop(IWhileLoopOperation operation);

-    }
-    public abstract class OperationVisitor<TArgument, TResult> {
 {
-        protected OperationVisitor();

-        public virtual TResult DefaultVisit(IOperation operation, TArgument argument);

-        public virtual TResult Visit(IOperation operation, TArgument argument);

-        public virtual TResult VisitAddressOf(IAddressOfOperation operation, TArgument argument);

-        public virtual TResult VisitAnonymousFunction(IAnonymousFunctionOperation operation, TArgument argument);

-        public virtual TResult VisitAnonymousObjectCreation(IAnonymousObjectCreationOperation operation, TArgument argument);

-        public virtual TResult VisitArgument(IArgumentOperation operation, TArgument argument);

-        public virtual TResult VisitArrayCreation(IArrayCreationOperation operation, TArgument argument);

-        public virtual TResult VisitArrayElementReference(IArrayElementReferenceOperation operation, TArgument argument);

-        public virtual TResult VisitArrayInitializer(IArrayInitializerOperation operation, TArgument argument);

-        public virtual TResult VisitAwait(IAwaitOperation operation, TArgument argument);

-        public virtual TResult VisitBinaryOperator(IBinaryOperation operation, TArgument argument);

-        public virtual TResult VisitBlock(IBlockOperation operation, TArgument argument);

-        public virtual TResult VisitBranch(IBranchOperation operation, TArgument argument);

-        public virtual TResult VisitCatchClause(ICatchClauseOperation operation, TArgument argument);

-        public virtual TResult VisitCoalesce(ICoalesceOperation operation, TArgument argument);

-        public virtual TResult VisitCollectionElementInitializer(ICollectionElementInitializerOperation operation, TArgument argument);

-        public virtual TResult VisitCompoundAssignment(ICompoundAssignmentOperation operation, TArgument argument);

-        public virtual TResult VisitConditional(IConditionalOperation operation, TArgument argument);

-        public virtual TResult VisitConditionalAccess(IConditionalAccessOperation operation, TArgument argument);

-        public virtual TResult VisitConditionalAccessInstance(IConditionalAccessInstanceOperation operation, TArgument argument);

-        public virtual TResult VisitConstantPattern(IConstantPatternOperation operation, TArgument argument);

-        public virtual TResult VisitConstructorBodyOperation(IConstructorBodyOperation operation, TArgument argument);

-        public virtual TResult VisitConversion(IConversionOperation operation, TArgument argument);

-        public virtual TResult VisitDeclarationExpression(IDeclarationExpressionOperation operation, TArgument argument);

-        public virtual TResult VisitDeclarationPattern(IDeclarationPatternOperation operation, TArgument argument);

-        public virtual TResult VisitDeconstructionAssignment(IDeconstructionAssignmentOperation operation, TArgument argument);

-        public virtual TResult VisitDefaultCaseClause(IDefaultCaseClauseOperation operation, TArgument argument);

-        public virtual TResult VisitDefaultValue(IDefaultValueOperation operation, TArgument argument);

-        public virtual TResult VisitDelegateCreation(IDelegateCreationOperation operation, TArgument argument);

-        public virtual TResult VisitDiscardOperation(IDiscardOperation operation, TArgument argument);

-        public virtual TResult VisitDynamicIndexerAccess(IDynamicIndexerAccessOperation operation, TArgument argument);

-        public virtual TResult VisitDynamicInvocation(IDynamicInvocationOperation operation, TArgument argument);

-        public virtual TResult VisitDynamicMemberReference(IDynamicMemberReferenceOperation operation, TArgument argument);

-        public virtual TResult VisitDynamicObjectCreation(IDynamicObjectCreationOperation operation, TArgument argument);

-        public virtual TResult VisitEmpty(IEmptyOperation operation, TArgument argument);

-        public virtual TResult VisitEnd(IEndOperation operation, TArgument argument);

-        public virtual TResult VisitEventAssignment(IEventAssignmentOperation operation, TArgument argument);

-        public virtual TResult VisitEventReference(IEventReferenceOperation operation, TArgument argument);

-        public virtual TResult VisitExpressionStatement(IExpressionStatementOperation operation, TArgument argument);

-        public virtual TResult VisitFieldInitializer(IFieldInitializerOperation operation, TArgument argument);

-        public virtual TResult VisitFieldReference(IFieldReferenceOperation operation, TArgument argument);

-        public virtual TResult VisitForEachLoop(IForEachLoopOperation operation, TArgument argument);

-        public virtual TResult VisitForLoop(IForLoopOperation operation, TArgument argument);

-        public virtual TResult VisitForToLoop(IForToLoopOperation operation, TArgument argument);

-        public virtual TResult VisitIncrementOrDecrement(IIncrementOrDecrementOperation operation, TArgument argument);

-        public virtual TResult VisitInstanceReference(IInstanceReferenceOperation operation, TArgument argument);

-        public virtual TResult VisitInterpolatedString(IInterpolatedStringOperation operation, TArgument argument);

-        public virtual TResult VisitInterpolatedStringText(IInterpolatedStringTextOperation operation, TArgument argument);

-        public virtual TResult VisitInterpolation(IInterpolationOperation operation, TArgument argument);

-        public virtual TResult VisitInvalid(IInvalidOperation operation, TArgument argument);

-        public virtual TResult VisitInvocation(IInvocationOperation operation, TArgument argument);

-        public virtual TResult VisitIsPattern(IIsPatternOperation operation, TArgument argument);

-        public virtual TResult VisitIsType(IIsTypeOperation operation, TArgument argument);

-        public virtual TResult VisitLabeled(ILabeledOperation operation, TArgument argument);

-        public virtual TResult VisitLiteral(ILiteralOperation operation, TArgument argument);

-        public virtual TResult VisitLocalFunction(ILocalFunctionOperation operation, TArgument argument);

-        public virtual TResult VisitLocalReference(ILocalReferenceOperation operation, TArgument argument);

-        public virtual TResult VisitLock(ILockOperation operation, TArgument argument);

-        public virtual TResult VisitMemberInitializer(IMemberInitializerOperation operation, TArgument argument);

-        public virtual TResult VisitMethodBodyOperation(IMethodBodyOperation operation, TArgument argument);

-        public virtual TResult VisitMethodReference(IMethodReferenceOperation operation, TArgument argument);

-        public virtual TResult VisitNameOf(INameOfOperation operation, TArgument argument);

-        public virtual TResult VisitObjectCreation(IObjectCreationOperation operation, TArgument argument);

-        public virtual TResult VisitObjectOrCollectionInitializer(IObjectOrCollectionInitializerOperation operation, TArgument argument);

-        public virtual TResult VisitOmittedArgument(IOmittedArgumentOperation operation, TArgument argument);

-        public virtual TResult VisitParameterInitializer(IParameterInitializerOperation operation, TArgument argument);

-        public virtual TResult VisitParameterReference(IParameterReferenceOperation operation, TArgument argument);

-        public virtual TResult VisitParenthesized(IParenthesizedOperation operation, TArgument argument);

-        public virtual TResult VisitPatternCaseClause(IPatternCaseClauseOperation operation, TArgument argument);

-        public virtual TResult VisitPropertyInitializer(IPropertyInitializerOperation operation, TArgument argument);

-        public virtual TResult VisitPropertyReference(IPropertyReferenceOperation operation, TArgument argument);

-        public virtual TResult VisitRaiseEvent(IRaiseEventOperation operation, TArgument argument);

-        public virtual TResult VisitRangeCaseClause(IRangeCaseClauseOperation operation, TArgument argument);

-        public virtual TResult VisitRelationalCaseClause(IRelationalCaseClauseOperation operation, TArgument argument);

-        public virtual TResult VisitReturn(IReturnOperation operation, TArgument argument);

-        public virtual TResult VisitSimpleAssignment(ISimpleAssignmentOperation operation, TArgument argument);

-        public virtual TResult VisitSingleValueCaseClause(ISingleValueCaseClauseOperation operation, TArgument argument);

-        public virtual TResult VisitSizeOf(ISizeOfOperation operation, TArgument argument);

-        public virtual TResult VisitStop(IStopOperation operation, TArgument argument);

-        public virtual TResult VisitSwitch(ISwitchOperation operation, TArgument argument);

-        public virtual TResult VisitSwitchCase(ISwitchCaseOperation operation, TArgument argument);

-        public virtual TResult VisitThrow(IThrowOperation operation, TArgument argument);

-        public virtual TResult VisitTranslatedQuery(ITranslatedQueryOperation operation, TArgument argument);

-        public virtual TResult VisitTry(ITryOperation operation, TArgument argument);

-        public virtual TResult VisitTuple(ITupleOperation operation, TArgument argument);

-        public virtual TResult VisitTupleBinaryOperator(ITupleBinaryOperation operation, TArgument argument);

-        public virtual TResult VisitTypeOf(ITypeOfOperation operation, TArgument argument);

-        public virtual TResult VisitTypeParameterObjectCreation(ITypeParameterObjectCreationOperation operation, TArgument argument);

-        public virtual TResult VisitUnaryOperator(IUnaryOperation operation, TArgument argument);

-        public virtual TResult VisitUsing(IUsingOperation operation, TArgument argument);

-        public virtual TResult VisitVariableDeclaration(IVariableDeclarationOperation operation, TArgument argument);

-        public virtual TResult VisitVariableDeclarationGroup(IVariableDeclarationGroupOperation operation, TArgument argument);

-        public virtual TResult VisitVariableDeclarator(IVariableDeclaratorOperation operation, TArgument argument);

-        public virtual TResult VisitVariableInitializer(IVariableInitializerOperation operation, TArgument argument);

-        public virtual TResult VisitWhileLoop(IWhileLoopOperation operation, TArgument argument);

-    }
-    public abstract class OperationWalker : OperationVisitor {
 {
-        protected OperationWalker();

-        public override void DefaultVisit(IOperation operation);

-        public override void Visit(IOperation operation);

-    }
-    public enum UnaryOperatorKind {
 {
-        BitwiseNegation = 1,

-        False = 6,

-        Minus = 4,

-        None = 0,

-        Not = 2,

-        Plus = 3,

-        True = 5,

-    }
-}
```

