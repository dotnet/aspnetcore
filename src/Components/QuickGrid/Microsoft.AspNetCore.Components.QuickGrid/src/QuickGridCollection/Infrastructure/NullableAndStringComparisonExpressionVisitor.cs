// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq.Expressions;

namespace Microsoft.AspNetCore.Components.QuickGrid.QuickGridCollection.Infrastructure;

/// <summary>
/// A custom expression visitor that handles expressions with Nullable values and case-insensitive string comparisons.
/// </summary>
internal class NullableAndStringComparisonExpressionVisitor : ExpressionVisitor
{
    private readonly bool useDefaultValueForNull;
    private readonly bool ignoreCaseInStringComparison;

    /// <summary>
    /// Creates a new instance of the NullableAndStringComparisonExpressionVisitor class.
    /// </summary>
    /// <param name="useDefaultValueForNull">Indicates whether nullable objects should be treated as having a default value when visiting expressions.</param>
    /// <param name="ignoreCaseInStringComparison">Indicates whether string comparisons should be case-insensitive when visiting expressions.</param>
    internal NullableAndStringComparisonExpressionVisitor(bool useDefaultValueForNull = false, bool ignoreCaseInStringComparison = true)
    {
        this.useDefaultValueForNull = useDefaultValueForNull;
        this.ignoreCaseInStringComparison = ignoreCaseInStringComparison;
    }

    /// <summary>
    /// Visits the method call nodes of the expression tree.
    /// </summary>
    /// <param name="node">The method call node to visit.</param>
    /// <returns>The modified method call node, if necessary, or the original node if no modification is necessary.</returns>
    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        if (node.Object != null && node.Object.Type == typeof(string))
        {
            var expressionBody = node.Object;
            var arguments = node.Arguments;

            var toLowerMethod = typeof(string).GetMethod("ToLower", Type.EmptyTypes);
            if (ignoreCaseInStringComparison)
            {
                expressionBody = Expression.Call(node.Object, toLowerMethod!);
                arguments = Array.AsReadOnly(new[] { (Expression)Expression.Call(node.Arguments[0], toLowerMethod!) });
            }

            if (useDefaultValueForNull)
            {
                Expression instance = Expression.Coalesce(node.Object, Expression.Constant(string.Empty));
                if (ignoreCaseInStringComparison)
                {
                    instance = Expression.Call(instance, toLowerMethod!);
                }

                var methodCall = Expression.Call(instance, node.Method, arguments);
                return methodCall;
            }
            else
            {
                var conditionalExpression = Expression.Condition(Expression.Equal(node.Object, Expression.Constant(null)),
                                                                 Expression.Constant(false),
                                                                 Expression.Call(expressionBody, node.Method, arguments));
                return conditionalExpression;
            }
        }

        return base.VisitMethodCall(node);
    }

    /// <summary>
    /// Visits the binary nodes of the expression tree.
    /// </summary>
    /// <param name="node">The binary node to visit.</param>
    /// <returns>The modified binary node, if necessary, or the original node if no modification is necessary.</returns>    
    protected override Expression VisitBinary(BinaryExpression node)
    {
        if (node.Left is UnaryExpression unaryLeft
            && unaryLeft.Operand is MemberExpression memberExpression
            && node.Right is UnaryExpression unaryRight
            && unaryRight.Operand is ConstantExpression constant
            )
        {
            Expression property = memberExpression;
            var objvalue = constant.Value;
            if (memberExpression.Type == typeof(string) && objvalue != null && ignoreCaseInStringComparison)
            {
                objvalue = ((string)objvalue).ToLower(System.Globalization.CultureInfo.InvariantCulture);
            }

            var toLowerMethod = typeof(string).GetMethod("ToLower", Type.EmptyTypes);
            Expression constantExpression = Expression.Constant(objvalue, memberExpression.Type);

            if (useDefaultValueForNull)
            {
                Expression getValueOrDefaultExpression;
                if (memberExpression.Type == typeof(string))
                {
                    getValueOrDefaultExpression = Expression.Coalesce(property, Expression.Constant(string.Empty));
                    if (ignoreCaseInStringComparison)
                    {
                        getValueOrDefaultExpression = Expression.Call(getValueOrDefaultExpression, toLowerMethod!);
                    }
                }
                else if (Nullable.GetUnderlyingType(memberExpression.Type) != null)
                {
                    getValueOrDefaultExpression = Expression.Default(property.Type);
                }
                else
                {
                    return Expression.MakeBinary(node.NodeType, memberExpression, constantExpression);
                }

                Expression getValueOrDefaultExpressionConvert = Expression.Convert(getValueOrDefaultExpression, memberExpression.Type);

                var comparisonWhithgetValueOrDefault = Expression.MakeBinary(node.NodeType, getValueOrDefaultExpressionConvert, constantExpression);
                return comparisonWhithgetValueOrDefault;
            }
            Expression comparison = Expression.MakeBinary(node.NodeType, property, constantExpression);
            if (ignoreCaseInStringComparison && memberExpression.Type == typeof(string))
            {
                comparison = Expression.MakeBinary(node.NodeType, Expression.Call(property, toLowerMethod!), constantExpression);
            }
            if (Nullable.GetUnderlyingType(memberExpression.Type) == null)
            {
                return comparison;
            }

            var condition = Expression.Condition(
                Expression.Equal(property, Expression.Constant(null)),
                Expression.Constant(false),
                comparison
                );
            return condition;
        }

        return base.VisitBinary(node);
    }
}
