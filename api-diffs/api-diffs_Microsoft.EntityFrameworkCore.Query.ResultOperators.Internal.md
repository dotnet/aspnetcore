# Microsoft.EntityFrameworkCore.Query.ResultOperators.Internal

``` diff
-namespace Microsoft.EntityFrameworkCore.Query.ResultOperators.Internal {
 {
-    public class FromSqlExpressionNode : ResultOperatorExpressionNodeBase {
 {
-        public static readonly IReadOnlyCollection<MethodInfo> SupportedMethods;

-        public FromSqlExpressionNode(MethodCallExpressionParseInfo parseInfo, ConstantExpression sql, Expression arguments);

-        protected override ResultOperatorBase CreateResultOperator(ClauseGenerationContext clauseGenerationContext);

-        public override Expression Resolve(ParameterExpression inputParameter, Expression expressionToBeResolved, ClauseGenerationContext clauseGenerationContext);

-    }
-    public class FromSqlResultOperator : SequenceTypePreservingResultOperatorBase, ICloneableQueryAnnotation, IQueryAnnotation {
 {
-        public FromSqlResultOperator(string sql, Expression arguments);

-        public virtual Expression Arguments { get; }

-        public virtual QueryModel QueryModel { get; set; }

-        public virtual IQuerySource QuerySource { get; set; }

-        public virtual string Sql { get; }

-        public override ResultOperatorBase Clone(CloneContext cloneContext);

-        public override StreamedSequence ExecuteInMemory<T>(StreamedSequence input);

-        ICloneableQueryAnnotation Microsoft.EntityFrameworkCore.Query.ResultOperators.ICloneableQueryAnnotation.Clone(IQuerySource querySource, QueryModel queryModel);

-        public override string ToString();

-        public override void TransformExpressions(Func<Expression, Expression> transformation);

-    }
-    public class IgnoreQueryFiltersExpressionNode : ResultOperatorExpressionNodeBase {
 {
-        public static readonly IReadOnlyCollection<MethodInfo> SupportedMethods;

-        public IgnoreQueryFiltersExpressionNode(MethodCallExpressionParseInfo parseInfo);

-        protected override ResultOperatorBase CreateResultOperator(ClauseGenerationContext clauseGenerationContext);

-        public override Expression Resolve(ParameterExpression inputParameter, Expression expressionToBeResolved, ClauseGenerationContext clauseGenerationContext);

-    }
-    public class IgnoreQueryFiltersResultOperator : SequenceTypePreservingResultOperatorBase, IQueryAnnotation {
 {
-        public IgnoreQueryFiltersResultOperator();

-        public virtual QueryModel QueryModel { get; set; }

-        public virtual IQuerySource QuerySource { get; set; }

-        public override ResultOperatorBase Clone(CloneContext cloneContext);

-        public override StreamedSequence ExecuteInMemory<T>(StreamedSequence input);

-        public override string ToString();

-        public override void TransformExpressions(Func<Expression, Expression> transformation);

-    }
-    public class IncludeExpressionNode : IncludeExpressionNodeBase {
 {
-        public static readonly IReadOnlyCollection<MethodInfo> SupportedMethods;

-        public IncludeExpressionNode(MethodCallExpressionParseInfo parseInfo, LambdaExpression navigationPropertyPathLambda);

-        protected override ResultOperatorBase CreateResultOperator(ClauseGenerationContext clauseGenerationContext);

-    }
-    public abstract class IncludeExpressionNodeBase : ResultOperatorExpressionNodeBase {
 {
-        protected IncludeExpressionNodeBase(MethodCallExpressionParseInfo parseInfo, LambdaExpression navigationPropertyPathLambda);

-        protected virtual LambdaExpression NavigationPropertyPathLambda { get; }

-        protected virtual Type SourceEntityType { get; }

-        public override Expression Resolve(ParameterExpression inputParameter, Expression expressionToBeResolved, ClauseGenerationContext clauseGenerationContext);

-    }
-    public class IncludeResultOperator : SequenceTypePreservingResultOperatorBase, IQueryAnnotation {
 {
-        public IncludeResultOperator(INavigation[] navigationPath, Expression pathFromQuerySource, bool implicitLoad = false);

-        public IncludeResultOperator(IEnumerable<string> navigationPropertyPaths, Expression pathFromQuerySource);

-        public virtual bool IsImplicitLoad { get; }

-        public virtual IReadOnlyList<INavigation[]> NavigationPaths { get; }

-        public virtual IReadOnlyList<string> NavigationPropertyPaths { get; }

-        public virtual Expression PathFromQuerySource { get; set; }

-        public virtual QueryModel QueryModel { get; set; }

-        public virtual IQuerySource QuerySource { get; set; }

-        public virtual void AppendToNavigationPath(IReadOnlyList<PropertyInfo> propertyInfos);

-        public override ResultOperatorBase Clone(CloneContext cloneContext);

-        public virtual string DisplayString();

-        public override StreamedSequence ExecuteInMemory<T>(StreamedSequence input);

-        public override string ToString();

-        public override void TransformExpressions(Func<Expression, Expression> transformation);

-    }
-    public class StringIncludeExpressionNode : ResultOperatorExpressionNodeBase {
 {
-        public static readonly IReadOnlyCollection<MethodInfo> SupportedMethods;

-        public StringIncludeExpressionNode(MethodCallExpressionParseInfo parseInfo, ConstantExpression navigationPropertyPath);

-        protected override ResultOperatorBase CreateResultOperator(ClauseGenerationContext clauseGenerationContext);

-        public override Expression Resolve(ParameterExpression inputParameter, Expression expressionToBeResolved, ClauseGenerationContext clauseGenerationContext);

-    }
-    public class TagExpressionNode : ResultOperatorExpressionNodeBase {
 {
-        public static readonly IReadOnlyCollection<MethodInfo> SupportedMethods;

-        public TagExpressionNode(MethodCallExpressionParseInfo parseInfo, ConstantExpression tagExpression);

-        protected override ResultOperatorBase CreateResultOperator(ClauseGenerationContext clauseGenerationContext);

-        public override Expression Resolve(ParameterExpression inputParameter, Expression expressionToBeResolved, ClauseGenerationContext clauseGenerationContext);

-    }
-    public class TagResultOperator : SequenceTypePreservingResultOperatorBase, IQueryAnnotation {
 {
-        public TagResultOperator(string tag);

-        public virtual QueryModel QueryModel { get; set; }

-        public virtual IQuerySource QuerySource { get; set; }

-        public virtual string Tag { get; }

-        public override ResultOperatorBase Clone(CloneContext cloneContext);

-        public override StreamedSequence ExecuteInMemory<T>(StreamedSequence input);

-        public override string ToString();

-        public override void TransformExpressions(Func<Expression, Expression> transformation);

-    }
-    public class ThenIncludeExpressionNode : IncludeExpressionNodeBase {
 {
-        public static readonly IReadOnlyCollection<MethodInfo> SupportedMethods;

-        public ThenIncludeExpressionNode(MethodCallExpressionParseInfo parseInfo, LambdaExpression navigationPropertyPathLambda);

-        protected override void ApplyNodeSpecificSemantics(QueryModel queryModel, ClauseGenerationContext clauseGenerationContext);

-        protected override ResultOperatorBase CreateResultOperator(ClauseGenerationContext clauseGenerationContext);

-    }
-    public class TrackingExpressionNode : ResultOperatorExpressionNodeBase {
 {
-        public static readonly IReadOnlyCollection<MethodInfo> SupportedMethods;

-        public TrackingExpressionNode(MethodCallExpressionParseInfo parseInfo);

-        protected override ResultOperatorBase CreateResultOperator(ClauseGenerationContext clauseGenerationContext);

-        public override Expression Resolve(ParameterExpression inputParameter, Expression expressionToBeResolved, ClauseGenerationContext clauseGenerationContext);

-    }
-    public class TrackingResultOperator : SequenceTypePreservingResultOperatorBase, IQueryAnnotation {
 {
-        public TrackingResultOperator(bool tracking);

-        public virtual bool IsTracking { get; }

-        public virtual QueryModel QueryModel { get; set; }

-        public virtual IQuerySource QuerySource { get; set; }

-        public override ResultOperatorBase Clone(CloneContext cloneContext);

-        public override StreamedSequence ExecuteInMemory<T>(StreamedSequence input);

-        public override string ToString();

-        public override void TransformExpressions(Func<Expression, Expression> transformation);

-    }
-}
```

