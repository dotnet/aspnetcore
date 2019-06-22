# Microsoft.EntityFrameworkCore.Query.Expressions

``` diff
-namespace Microsoft.EntityFrameworkCore.Query.Expressions {
 {
-    public class AliasExpression : Expression {
 {
-        public AliasExpression(string alias, Expression expression);

-        public virtual string Alias { get; }

-        public virtual Expression Expression { get; }

-        public override ExpressionType NodeType { get; }

-        public override Type Type { get; }

-        protected override Expression Accept(ExpressionVisitor visitor);

-        public override bool Equals(object obj);

-        public override int GetHashCode();

-        public override string ToString();

-        protected override Expression VisitChildren(ExpressionVisitor visitor);

-    }
-    public class CaseExpression : Expression, IPrintable {
 {
-        public CaseExpression(params CaseWhenClause[] whenClauses);

-        public CaseExpression(IReadOnlyList<CaseWhenClause> whenClauses, Expression elseResult);

-        public CaseExpression(Expression operand, params CaseWhenClause[] whenClauses);

-        public CaseExpression(Expression operand, IReadOnlyList<CaseWhenClause> whenClauses, Expression elseResult);

-        public virtual Expression ElseResult { get; }

-        public override ExpressionType NodeType { get; }

-        public virtual Expression Operand { get; }

-        public override Type Type { get; }

-        public virtual IReadOnlyList<CaseWhenClause> WhenClauses { get; }

-        protected override Expression Accept(ExpressionVisitor visitor);

-        public override bool Equals(object obj);

-        public override int GetHashCode();

-        void Microsoft.EntityFrameworkCore.Query.Expressions.Internal.IPrintable.Print(ExpressionPrinter expressionPrinter);

-        public override string ToString();

-        protected override Expression VisitChildren(ExpressionVisitor visitor);

-    }
-    public class CaseWhenClause {
 {
-        public CaseWhenClause(Expression test, Expression result);

-        public virtual Expression Result { get; }

-        public virtual Expression Test { get; }

-        public override bool Equals(object obj);

-        public override int GetHashCode();

-        public override string ToString();

-    }
-    public class ColumnExpression : Expression {
 {
-        public ColumnExpression(string name, IProperty property, TableExpressionBase tableExpression);

-        public virtual string Name { get; }

-        public override ExpressionType NodeType { get; }

-        public virtual IProperty Property { get; }

-        public virtual TableExpressionBase Table { get; }

-        public override Type Type { get; }

-        protected override Expression Accept(ExpressionVisitor visitor);

-        public override bool Equals(object obj);

-        public override int GetHashCode();

-        public override string ToString();

-        protected override Expression VisitChildren(ExpressionVisitor visitor);

-    }
-    public class ColumnReferenceExpression : Expression {
 {
-        public ColumnReferenceExpression(AliasExpression aliasExpression, TableExpressionBase tableExpression);

-        public ColumnReferenceExpression(ColumnExpression columnExpression, TableExpressionBase tableExpression);

-        public ColumnReferenceExpression(ColumnReferenceExpression columnReferenceExpression, TableExpressionBase tableExpression);

-        public virtual Expression Expression { get; }

-        public virtual string Name { get; }

-        public override ExpressionType NodeType { get; }

-        public virtual TableExpressionBase Table { get; }

-        public override Type Type { get; }

-        protected override Expression Accept(ExpressionVisitor visitor);

-        public override bool Equals(object obj);

-        public override int GetHashCode();

-        public override string ToString();

-        protected override Expression VisitChildren(ExpressionVisitor visitor);

-    }
-    public class CrossJoinExpression : JoinExpressionBase {
 {
-        public CrossJoinExpression(TableExpressionBase tableExpression);

-        protected override Expression Accept(ExpressionVisitor visitor);

-        public override bool Equals(object obj);

-        public override int GetHashCode();

-        public override string ToString();

-    }
-    public class CrossJoinLateralExpression : JoinExpressionBase {
 {
-        public CrossJoinLateralExpression(TableExpressionBase tableExpression);

-        protected override Expression Accept(ExpressionVisitor visitor);

-        public override bool Equals(object obj);

-        public override int GetHashCode();

-        public override string ToString();

-    }
-    public class DiscriminatorPredicateExpression : Expression, IPrintable {
 {
-        public DiscriminatorPredicateExpression(Expression predicate, IQuerySource querySource);

-        public override bool CanReduce { get; }

-        public override ExpressionType NodeType { get; }

-        public virtual IQuerySource QuerySource { get; }

-        public override Type Type { get; }

-        public override bool Equals(object obj);

-        public override int GetHashCode();

-        void Microsoft.EntityFrameworkCore.Query.Expressions.Internal.IPrintable.Print(ExpressionPrinter expressionPrinter);

-        public override Expression Reduce();

-        public override string ToString();

-        protected override Expression VisitChildren(ExpressionVisitor visitor);

-    }
-    public class ExistsExpression : Expression {
 {
-        public ExistsExpression(SelectExpression subquery);

-        public override ExpressionType NodeType { get; }

-        public virtual SelectExpression Subquery { get; }

-        public override Type Type { get; }

-        protected override Expression Accept(ExpressionVisitor visitor);

-        public override bool Equals(object obj);

-        public override int GetHashCode();

-        protected override Expression VisitChildren(ExpressionVisitor visitor);

-    }
-    public class ExplicitCastExpression : Expression {
 {
-        public ExplicitCastExpression(Expression operand, Type type);

-        public override ExpressionType NodeType { get; }

-        public virtual Expression Operand { get; }

-        public override Type Type { get; }

-        protected override Expression Accept(ExpressionVisitor visitor);

-        public override bool Equals(object obj);

-        public override int GetHashCode();

-        public override string ToString();

-        protected override Expression VisitChildren(ExpressionVisitor visitor);

-    }
-    public class FromSqlExpression : TableExpressionBase {
 {
-        public FromSqlExpression(string sql, Expression arguments, string alias, IQuerySource querySource);

-        public virtual Expression Arguments { get; }

-        public virtual string Sql { get; }

-        protected override Expression Accept(ExpressionVisitor visitor);

-        public override bool Equals(object obj);

-        public override int GetHashCode();

-        public override string ToString();

-    }
-    public class InExpression : Expression {
 {
-        public InExpression(Expression operand, SelectExpression subQuery);

-        public InExpression(Expression operand, IReadOnlyList<Expression> values);

-        public override ExpressionType NodeType { get; }

-        public virtual Expression Operand { get; }

-        public virtual SelectExpression SubQuery { get; }

-        public override Type Type { get; }

-        public virtual IReadOnlyList<Expression> Values { get; }

-        protected override Expression Accept(ExpressionVisitor visitor);

-        public override bool Equals(object obj);

-        public override int GetHashCode();

-        public override string ToString();

-        protected override Expression VisitChildren(ExpressionVisitor visitor);

-    }
-    public class InnerJoinExpression : PredicateJoinExpressionBase {
 {
-        public InnerJoinExpression(TableExpressionBase tableExpression);

-        protected override Expression Accept(ExpressionVisitor visitor);

-        public override bool Equals(object obj);

-        public override int GetHashCode();

-        public override string ToString();

-    }
-    public interface ISelectExpressionFactory {
 {
-        SelectExpression Create(RelationalQueryCompilationContext queryCompilationContext);

-        SelectExpression Create(RelationalQueryCompilationContext queryCompilationContext, string alias);

-    }
-    public class IsNullExpression : Expression {
 {
-        public IsNullExpression(Expression operand);

-        public override ExpressionType NodeType { get; }

-        public virtual Expression Operand { get; }

-        public override Type Type { get; }

-        protected override Expression Accept(ExpressionVisitor visitor);

-        public override bool Equals(object obj);

-        public override int GetHashCode();

-        public override string ToString();

-        protected override Expression VisitChildren(ExpressionVisitor visitor);

-    }
-    public abstract class JoinExpressionBase : TableExpressionBase {
 {
-        protected JoinExpressionBase(TableExpressionBase tableExpression);

-        public virtual TableExpressionBase TableExpression { get; }

-        protected override Expression VisitChildren(ExpressionVisitor visitor);

-    }
-    public class LeftOuterJoinExpression : PredicateJoinExpressionBase {
 {
-        public LeftOuterJoinExpression(TableExpressionBase tableExpression);

-        protected override Expression Accept(ExpressionVisitor visitor);

-        public override bool Equals(object obj);

-        public override int GetHashCode();

-        public override string ToString();

-    }
-    public class LikeExpression : Expression {
 {
-        public LikeExpression(Expression match, Expression pattern);

-        public LikeExpression(Expression match, Expression pattern, Expression escapeChar);

-        public virtual Expression EscapeChar { get; }

-        public virtual Expression Match { get; }

-        public override ExpressionType NodeType { get; }

-        public virtual Expression Pattern { get; }

-        public override Type Type { get; }

-        protected override Expression Accept(ExpressionVisitor visitor);

-        public override bool Equals(object obj);

-        public override int GetHashCode();

-        public override string ToString();

-        protected override Expression VisitChildren(ExpressionVisitor visitor);

-    }
-    public class NullableExpression : Expression {
 {
-        public NullableExpression(Expression operand);

-        public override bool CanReduce { get; }

-        public override ExpressionType NodeType { get; }

-        public virtual Expression Operand { get; }

-        public override Type Type { get; }

-        public override bool Equals(object obj);

-        public override int GetHashCode();

-        public override Expression Reduce();

-        protected override Expression VisitChildren(ExpressionVisitor visitor);

-    }
-    public class NullCompensatedExpression : Expression {
 {
-        public NullCompensatedExpression(Expression operand);

-        public override bool CanReduce { get; }

-        public override ExpressionType NodeType { get; }

-        public virtual Expression Operand { get; }

-        public override Type Type { get; }

-        public override bool Equals(object obj);

-        public override int GetHashCode();

-        public override Expression Reduce();

-        protected override Expression VisitChildren(ExpressionVisitor visitor);

-    }
-    public abstract class PredicateJoinExpressionBase : JoinExpressionBase {
 {
-        protected PredicateJoinExpressionBase(TableExpressionBase tableExpression);

-        public virtual Expression Predicate { get; set; }

-        protected override Expression VisitChildren(ExpressionVisitor visitor);

-    }
-    public class PropertyParameterExpression : Expression {
 {
-        public PropertyParameterExpression(string name, IProperty property);

-        public virtual string Name { get; }

-        public override ExpressionType NodeType { get; }

-        public virtual IProperty Property { get; }

-        public virtual string PropertyParameterName { get; }

-        public override Type Type { get; }

-        protected override Expression Accept(ExpressionVisitor visitor);

-        public override bool Equals(object obj);

-        public override int GetHashCode();

-        public override string ToString();

-        protected override Expression VisitChildren(ExpressionVisitor visitor);

-    }
-    public class SelectExpression : TableExpressionBase {
 {
-        public SelectExpression(SelectExpressionDependencies dependencies, RelationalQueryCompilationContext queryCompilationContext);

-        public SelectExpression(SelectExpressionDependencies dependencies, RelationalQueryCompilationContext queryCompilationContext, string alias);

-        protected virtual SelectExpressionDependencies Dependencies { get; }

-        public virtual IReadOnlyList<Expression> GroupBy { get; }

-        public virtual Expression Having { get; set; }

-        public virtual bool IsDistinct { get; set; }

-        public virtual bool IsProjectStar { get; set; }

-        public virtual Expression Limit { get; set; }

-        public virtual Expression Offset { get; set; }

-        public virtual IReadOnlyList<Ordering> OrderBy { get; }

-        public virtual Expression Predicate { get; set; }

-        public virtual IReadOnlyList<Expression> Projection { get; }

-        public virtual TableExpressionBase ProjectStarTable { get; set; }

-        public virtual IReadOnlyList<TableExpressionBase> Tables { get; }

-        public virtual IReadOnlyCollection<string> Tags { get; }

-        public override Type Type { get; }

-        protected override Expression Accept(ExpressionVisitor visitor);

-        public virtual JoinExpressionBase AddCrossJoin(TableExpressionBase tableExpression, IEnumerable<Expression> projection);

-        public virtual JoinExpressionBase AddCrossJoinLateral(TableExpressionBase tableExpression, IEnumerable<Expression> projection);

-        public virtual PredicateJoinExpressionBase AddInnerJoin(TableExpressionBase tableExpression);

-        public virtual PredicateJoinExpressionBase AddInnerJoin(TableExpressionBase tableExpression, IEnumerable<Expression> projection, Expression innerPredicate);

-        public virtual PredicateJoinExpressionBase AddLeftOuterJoin(TableExpressionBase tableExpression);

-        public virtual PredicateJoinExpressionBase AddLeftOuterJoin(TableExpressionBase tableExpression, IEnumerable<Expression> projection);

-        public virtual void AddTable(TableExpressionBase tableExpression);

-        public virtual void AddToGroupBy(Expression[] groupingExpressions);

-        public virtual Ordering AddToOrderBy(Ordering ordering);

-        public virtual void AddToPredicate(Expression predicate);

-        public virtual int AddToProjection(IProperty property, IQuerySource querySource);

-        public virtual int AddToProjection(Expression expression, bool resetProjectStar = true);

-        public virtual Expression BindProperty(IProperty property, IQuerySource querySource);

-        public virtual Expression BindSubqueryProjectionIndex(int projectionIndex, IQuerySource querySource);

-        public virtual void Clear();

-        public virtual void ClearOrderBy();

-        public virtual void ClearProjection();

-        public virtual void ClearTables();

-        public virtual SelectExpression Clone(string alias = null);

-        public virtual IQuerySqlGenerator CreateDefaultQuerySqlGenerator();

-        public virtual IQuerySqlGenerator CreateFromSqlQuerySqlGenerator(string sql, Expression arguments);

-        public virtual void ExplodeStarProjection();

-        public virtual IEnumerable<TypeMaterializationInfo> GetMappedProjectionTypes();

-        public virtual Expression GetProjectionForMemberInfo(MemberInfo memberInfo);

-        public virtual int GetProjectionIndex(IProperty property, IQuerySource querySource);

-        public virtual IEnumerable<Type> GetProjectionTypes();

-        public virtual TableExpressionBase GetTableForQuerySource(IQuerySource querySource);

-        public override bool HandlesQuerySource(IQuerySource querySource);

-        public virtual bool IsCorrelated();

-        public virtual bool IsIdentityQuery();

-        public virtual void LiftOrderBy();

-        public virtual void PrependToOrderBy(IEnumerable<Ordering> orderings);

-        public virtual SelectExpression PushDownSubquery();

-        public virtual void RemoveRangeFromProjection(int index);

-        public virtual void RemoveTable(TableExpressionBase tableExpression);

-        public virtual void ReplaceOrderBy(IEnumerable<Ordering> orderings);

-        public virtual void ReplaceProjection(IEnumerable<Expression> expressions);

-        public virtual void SetProjectionExpression(Expression expression);

-        public virtual void SetProjectionForMemberInfo(MemberInfo memberInfo, Expression projection);

-        public override string ToString();

-        protected override Expression VisitChildren(ExpressionVisitor visitor);

-    }
-    public sealed class SelectExpressionDependencies {
 {
-        public SelectExpressionDependencies(IQuerySqlGeneratorFactory querySqlGeneratorFactory, IRelationalTypeMappingSource typeMappingSource);

-        public IQuerySqlGeneratorFactory QuerySqlGeneratorFactory { get; }

-        public IRelationalTypeMappingSource TypeMappingSource { get; }

-        public SelectExpressionDependencies With(IQuerySqlGeneratorFactory querySqlGeneratorFactory);

-        public SelectExpressionDependencies With(IRelationalTypeMappingSource typeMappingSource);

-    }
-    public class SelectExpressionFactory : ISelectExpressionFactory {
 {
-        public SelectExpressionFactory(SelectExpressionDependencies dependencies);

-        protected virtual SelectExpressionDependencies Dependencies { get; }

-        public virtual SelectExpression Create(RelationalQueryCompilationContext queryCompilationContext);

-        public virtual SelectExpression Create(RelationalQueryCompilationContext queryCompilationContext, string alias);

-    }
-    public class SqlFragmentExpression : Expression {
 {
-        public SqlFragmentExpression(string sql);

-        public override ExpressionType NodeType { get; }

-        public virtual string Sql { get; }

-        public override Type Type { get; }

-        protected override Expression Accept(ExpressionVisitor visitor);

-        public override bool Equals(object obj);

-        public override int GetHashCode();

-        protected override Expression VisitChildren(ExpressionVisitor visitor);

-    }
-    public class SqlFunctionExpression : Expression {
 {
-        public SqlFunctionExpression(Expression instance, string functionName, Type returnType, bool niladic);

-        public SqlFunctionExpression(Expression instance, string functionName, Type returnType, IEnumerable<Expression> arguments);

-        public SqlFunctionExpression(Expression instance, string functionName, Type returnType, IEnumerable<Expression> arguments, RelationalTypeMapping resultTypeMapping = null, RelationalTypeMapping instanceTypeMapping = null, IEnumerable<RelationalTypeMapping> argumentTypeMappings = null);

-        public SqlFunctionExpression(string functionName, Type returnType);

-        public SqlFunctionExpression(string functionName, Type returnType, bool niladic);

-        public SqlFunctionExpression(string functionName, Type returnType, IEnumerable<Expression> arguments);

-        public SqlFunctionExpression(string functionName, Type returnType, string schema, IEnumerable<Expression> arguments);

-        public SqlFunctionExpression(string functionName, Type returnType, string schema, IEnumerable<Expression> arguments, RelationalTypeMapping resultTypeMapping = null, IEnumerable<RelationalTypeMapping> argumentTypeMappings = null);

-        public virtual IReadOnlyList<Expression> Arguments { get; }

-        public virtual IReadOnlyList<RelationalTypeMapping> ArgumentTypeMappings { get; }

-        public virtual string FunctionName { get; }

-        public virtual Expression Instance { get; }

-        public virtual RelationalTypeMapping InstanceTypeMapping { get; }

-        public virtual bool IsNiladic { get; }

-        public override ExpressionType NodeType { get; }

-        public virtual RelationalTypeMapping ResultTypeMapping { get; }

-        public virtual string Schema { get; }

-        public override Type Type { get; }

-        protected override Expression Accept(ExpressionVisitor visitor);

-        public override bool Equals(object obj);

-        public override int GetHashCode();

-        public override string ToString();

-        protected override Expression VisitChildren(ExpressionVisitor visitor);

-    }
-    public class StringCompareExpression : Expression {
 {
-        public StringCompareExpression(ExpressionType op, Expression left, Expression right);

-        public virtual Expression Left { get; }

-        public override ExpressionType NodeType { get; }

-        public virtual ExpressionType Operator { get; }

-        public virtual Expression Right { get; }

-        public override Type Type { get; }

-        protected override Expression Accept(ExpressionVisitor visitor);

-        public override bool Equals(object obj);

-        public override int GetHashCode();

-        protected override Expression VisitChildren(ExpressionVisitor visitor);

-    }
-    public class TableExpression : TableExpressionBase {
 {
-        public TableExpression(string table, string schema, string alias, IQuerySource querySource);

-        public virtual string Schema { get; }

-        public virtual string Table { get; }

-        protected override Expression Accept(ExpressionVisitor visitor);

-        public override bool Equals(object obj);

-        public override int GetHashCode();

-        public override string ToString();

-    }
-    public abstract class TableExpressionBase : Expression {
 {
-        protected TableExpressionBase(IQuerySource querySource, string alias);

-        public virtual string Alias { get; set; }

-        public override ExpressionType NodeType { get; }

-        public virtual IQuerySource QuerySource { get; set; }

-        public override Type Type { get; }

-        public virtual bool HandlesQuerySource(IQuerySource querySource);

-        protected virtual IQuerySource PreProcessQuerySource(IQuerySource querySource);

-        protected override Expression VisitChildren(ExpressionVisitor visitor);

-    }
-}
```

