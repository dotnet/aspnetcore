# Microsoft.EntityFrameworkCore.Query.Sql

``` diff
-namespace Microsoft.EntityFrameworkCore.Query.Sql {
 {
-    public class DefaultQuerySqlGenerator : ThrowingExpressionVisitor, IQuerySqlGenerator, ISqlExpressionVisitor {
 {
-        protected DefaultQuerySqlGenerator(QuerySqlGeneratorDependencies dependencies, SelectExpression selectExpression);

-        protected virtual string AliasSeparator { get; }

-        protected virtual QuerySqlGeneratorDependencies Dependencies { get; }

-        public virtual bool IsCacheable { get; protected set; }

-        protected virtual IReadOnlyDictionary<string, object> ParameterValues { get; }

-        public virtual bool RequiresRuntimeProjectionRemapping { get; }

-        protected virtual SelectExpression SelectExpression { get; }

-        protected virtual string SingleLineCommentToken { get; }

-        protected virtual IRelationalCommandBuilder Sql { get; }

-        protected virtual ISqlGenerationHelper SqlGenerator { get; }

-        protected virtual string TypedFalseLiteral { get; }

-        protected virtual string TypedTrueLiteral { get; }

-        protected virtual Expression ApplyExplicitCastToBoolInProjectionOptimization(Expression expression);

-        protected override Exception CreateUnhandledItemException<T>(T unhandledItem, string visitMethod);

-        public virtual IRelationalValueBufferFactory CreateValueBufferFactory(IRelationalValueBufferFactoryFactory relationalValueBufferFactoryFactory, DbDataReader dataReader);

-        protected virtual IReadOnlyList<Expression> ExtractNonNullExpressionValues(IReadOnlyList<Expression> inExpressionValues);

-        protected virtual string GenerateBinaryOperator(ExpressionType op);

-        protected virtual void GenerateFromSql(string sql, Expression arguments, IReadOnlyDictionary<string, object> parameters);

-        protected virtual void GenerateFunctionCall(string functionName, IReadOnlyList<Expression> arguments, string schema = null);

-        protected virtual void GenerateHaving(Expression predicate);

-        protected virtual void GenerateIn(InExpression inExpression, bool negated = false);

-        protected virtual Expression GenerateIsNotNull(IsNullExpression isNotNullExpression);

-        protected virtual void GenerateLimitOffset(SelectExpression selectExpression);

-        protected virtual void GenerateList(IReadOnlyList<Expression> items, Action<IRelationalCommandBuilder> joinAction);

-        protected virtual void GenerateList(IReadOnlyList<Expression> items, Action<IRelationalCommandBuilder> joinAction = null, IReadOnlyList<RelationalTypeMapping> typeMappings = null);

-        protected virtual void GenerateList<T>(IReadOnlyList<T> items, Action<T> generationAction, Action<IRelationalCommandBuilder> joinAction);

-        protected virtual void GenerateList<T>(IReadOnlyList<T> items, Action<T> generationAction, Action<IRelationalCommandBuilder> joinAction = null, IReadOnlyList<RelationalTypeMapping> typeMappings = null);

-        protected virtual Expression GenerateNotIn(InExpression inExpression);

-        protected virtual string GenerateOperator(Expression expression);

-        protected virtual void GenerateOrderBy(IReadOnlyList<Ordering> orderings);

-        protected virtual void GenerateOrdering(Ordering ordering);

-        protected virtual void GeneratePredicate(Expression predicate);

-        protected virtual void GenerateProjection(Expression projection);

-        protected virtual void GeneratePseudoFromClause();

-        public virtual IRelationalCommand GenerateSql(IReadOnlyDictionary<string, object> parameterValues);

-        protected virtual void GenerateTagsHeaderComment();

-        protected virtual void GenerateTop(SelectExpression selectExpression);

-        public virtual IReadOnlyList<TypeMaterializationInfo> GetTypeMaterializationInfos();

-        protected virtual RelationalTypeMapping InferTypeMappingFromColumn(Expression expression);

-        protected virtual IReadOnlyList<Expression> ProcessInExpressionValues(IEnumerable<Expression> inExpressionValues);

-        protected virtual bool TryGenerateBinaryOperator(ExpressionType op, out string result);

-        public virtual Expression VisitAlias(AliasExpression aliasExpression);

-        protected override Expression VisitBinary(BinaryExpression binaryExpression);

-        public virtual Expression VisitCase(CaseExpression caseExpression);

-        public virtual Expression VisitColumn(ColumnExpression columnExpression);

-        public virtual Expression VisitColumnReference(ColumnReferenceExpression columnReferenceExpression);

-        protected override Expression VisitConditional(ConditionalExpression conditionalExpression);

-        protected override Expression VisitConstant(ConstantExpression constantExpression);

-        public virtual Expression VisitCrossJoin(CrossJoinExpression crossJoinExpression);

-        public virtual Expression VisitCrossJoinLateral(CrossJoinLateralExpression crossJoinLateralExpression);

-        public virtual Expression VisitExists(ExistsExpression existsExpression);

-        public virtual Expression VisitExplicitCast(ExplicitCastExpression explicitCastExpression);

-        public virtual Expression VisitFromSql(FromSqlExpression fromSqlExpression);

-        public virtual Expression VisitIn(InExpression inExpression);

-        public virtual Expression VisitInnerJoin(InnerJoinExpression innerJoinExpression);

-        public virtual Expression VisitIsNull(IsNullExpression isNullExpression);

-        public virtual Expression VisitLeftOuterJoin(LeftOuterJoinExpression leftOuterJoinExpression);

-        public virtual Expression VisitLike(LikeExpression likeExpression);

-        protected override Expression VisitParameter(ParameterExpression parameterExpression);

-        public virtual Expression VisitPropertyParameter(PropertyParameterExpression propertyParameterExpression);

-        public virtual Expression VisitSelect(SelectExpression selectExpression);

-        public virtual Expression VisitSqlFragment(SqlFragmentExpression sqlFragmentExpression);

-        public virtual Expression VisitSqlFunction(SqlFunctionExpression sqlFunctionExpression);

-        public virtual Expression VisitStringCompare(StringCompareExpression stringCompareExpression);

-        public virtual Expression VisitTable(TableExpression tableExpression);

-        protected override Expression VisitUnary(UnaryExpression expression);

-    }
-    public interface IQuerySqlGenerator {
 {
-        bool IsCacheable { get; }

-        IRelationalValueBufferFactory CreateValueBufferFactory(IRelationalValueBufferFactoryFactory relationalValueBufferFactoryFactory, DbDataReader dataReader);

-        IRelationalCommand GenerateSql(IReadOnlyDictionary<string, object> parameterValues);

-    }
-    public interface IQuerySqlGeneratorFactory {
 {
-        IQuerySqlGenerator CreateDefault(SelectExpression selectExpression);

-        IQuerySqlGenerator CreateFromSql(SelectExpression selectExpression, string sql, Expression arguments);

-    }
-    public interface ISqlExpressionVisitor {
 {
-        Expression VisitAlias(AliasExpression aliasExpression);

-        Expression VisitCase(CaseExpression caseExpression);

-        Expression VisitColumn(ColumnExpression columnExpression);

-        Expression VisitColumnReference(ColumnReferenceExpression columnReferenceExpression);

-        Expression VisitCrossJoin(CrossJoinExpression crossJoinExpression);

-        Expression VisitCrossJoinLateral(CrossJoinLateralExpression crossJoinLateralExpression);

-        Expression VisitExists(ExistsExpression existsExpression);

-        Expression VisitExplicitCast(ExplicitCastExpression explicitCastExpression);

-        Expression VisitFromSql(FromSqlExpression fromSqlExpression);

-        Expression VisitIn(InExpression inExpression);

-        Expression VisitInnerJoin(InnerJoinExpression innerJoinExpression);

-        Expression VisitIsNull(IsNullExpression isNullExpression);

-        Expression VisitLeftOuterJoin(LeftOuterJoinExpression leftOuterJoinExpression);

-        Expression VisitLike(LikeExpression likeExpression);

-        Expression VisitPropertyParameter(PropertyParameterExpression propertyParameterExpression);

-        Expression VisitSelect(SelectExpression selectExpression);

-        Expression VisitSqlFragment(SqlFragmentExpression sqlFragmentExpression);

-        Expression VisitSqlFunction(SqlFunctionExpression sqlFunctionExpression);

-        Expression VisitStringCompare(StringCompareExpression stringCompareExpression);

-        Expression VisitTable(TableExpression tableExpression);

-    }
-    public sealed class QuerySqlGeneratorDependencies {
 {
-        public QuerySqlGeneratorDependencies(IRelationalCommandBuilderFactory commandBuilderFactory, ISqlGenerationHelper sqlGenerationHelper, IParameterNameGeneratorFactory parameterNameGeneratorFactory, IRelationalTypeMapper relationalTypeMapper, IRelationalTypeMappingSource typeMappingSource, IDiagnosticsLogger<DbLoggerCategory.Query> logger);

-        public IRelationalCommandBuilderFactory CommandBuilderFactory { get; }

-        public IDiagnosticsLogger<DbLoggerCategory.Query> Logger { get; }

-        public IParameterNameGeneratorFactory ParameterNameGeneratorFactory { get; }

-        public IRelationalTypeMapper RelationalTypeMapper { get; }

-        public ISqlGenerationHelper SqlGenerationHelper { get; }

-        public IRelationalTypeMappingSource TypeMappingSource { get; }

-        public QuerySqlGeneratorDependencies With(IDiagnosticsLogger<DbLoggerCategory.Query> logger);

-        public QuerySqlGeneratorDependencies With(IParameterNameGeneratorFactory parameterNameGeneratorFactory);

-        public QuerySqlGeneratorDependencies With(IRelationalCommandBuilderFactory commandBuilderFactory);

-        public QuerySqlGeneratorDependencies With(IRelationalTypeMapper relationalTypeMapper);

-        public QuerySqlGeneratorDependencies With(IRelationalTypeMappingSource typeMappingSource);

-        public QuerySqlGeneratorDependencies With(ISqlGenerationHelper sqlGenerationHelper);

-    }
-    public abstract class QuerySqlGeneratorFactoryBase : IQuerySqlGeneratorFactory {
 {
-        protected QuerySqlGeneratorFactoryBase(QuerySqlGeneratorDependencies dependencies);

-        protected virtual QuerySqlGeneratorDependencies Dependencies { get; }

-        public abstract IQuerySqlGenerator CreateDefault(SelectExpression selectExpression);

-        public virtual IQuerySqlGenerator CreateFromSql(SelectExpression selectExpression, string sql, Expression arguments);

-    }
-}
```

