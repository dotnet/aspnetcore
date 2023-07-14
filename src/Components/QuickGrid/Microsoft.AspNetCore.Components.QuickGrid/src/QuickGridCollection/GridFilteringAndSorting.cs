// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.QuickGrid.QuickGridCollection.Infrastructure;
using System.Linq;
using System.Linq.Expressions;

namespace Microsoft.AspNetCore.Components.QuickGrid.QuickGridCollection;

/// <summary>
/// The <see cref="GridFilteringAndSorting{TGridItem}"/> structure is a generic type that takes a type parameter <c>TGridItem</c> which is the same typeparam as <see cref="QuickGridC{TGridItem}"/>.
/// It is used by a <see cref="QuickGridC{TGridItem}"/> component via the <see cref="QuickGridC{TGridItem}.FilterSortChanged"/> property.
/// which means it can be used to trigger an event when the grid's filters or order change.
/// <para>
/// The user can then use this instance to filter and sort the grid data using <c>Linq</c>, <c>Entity Framework Linq</c> or <c>Simple.Client.OData</c>.
/// </para>
/// <para>
/// <example>
/// Example of usage in a Blazor component:
/// <code>
/// <Grid TGridItem="Item" Items="Items" FilterOrOrderChange="OnFilterOrOrderChange">
/// </Grid>
/// @code {
///     private async Task OnFilterOrOrderChange(GridFilteringAndSorting&lt;Item&gt; filtersAndOrder)
///    {
///         // Using Linq
///         Items = filtersAndOrder.ApplyFilterAndSortExpressionsForLinq(myData.AsQueryable()).ToList();
///
///         // Using Entity Framework Linq
///         Items = filtersAndOrder.ApplyFilterAndSortExpressions(dbContext.Set&lt;Item&gt;()).ToList();
///
///        // Using Simple.Client.OData
///        var client = new ODataClient("https://my-service.com/odata");
///        var queryableOdata = client.For&lt;Item&gt;();
///        if (filtersAndOrder.HasFilterExpressions())
///           queryableOdata.Filter(filtersAndOrder.CombineFilterExpressions()!);
///        if(filtersAndOrder.HasSortExpressions())
///        {
///            foreach (var value in filtersAndOrder.SortExpressions)
///            {
///                (var sort, var exp) = value;
///                if (sort == SortedLinq.OrderBy)
///                    queryableOdata.OrderBy(exp);
///                if (sort == SortedLinq.OrderByDescending)
///                    queryableOdata.OrderByDescending(exp);
///                if (sort == SortedLinq.ThenBy)
///                    queryableOdata.ThenBy(exp);
///                if (sort == SortedLinq.ThenByDescending)
///                   queryableOdata.ThenByDescending(exp);
///           }
///       }
///       Items = await queryableOdata.FindEntriesAsync();
///   }
/// }    
/// </code>
/// </example>
/// </para>
/// </summary>
/// <typeparam name="TGridItem">The type of grid item.</typeparam> 
public struct GridFilteringAndSorting<TGridItem>
{
    /// <summary>
    /// Nullable array of expressions that take a TGridItem and return a boolean value.
    /// These expressions can be used to filter grid items.
    /// For example,
    /// <code>
    /// var queryable = myData.AsQueryable().Where(gridFilteringAndSorting.FilterExpressions[0])
    /// </code>
    /// </summary>
    public Expression<Func<TGridItem, bool>>[]? FilterExpressions { get; internal set; }

    /// <summary>
    /// Nullable array of tuples containing an element of type SortedLinq and an expression that takes a TGridItem and returns a value of type object.
    /// These tuples can be used to sort grid items.
    /// The first element of the tuple indicates the sorting method to use (OrderBy, OrderByDescending, ThenBy or ThenByDescending) and the second element is the sorting expression.
    /// For example, to sort items in ascending order, the following code can be used:
    /// <code>
    /// var queryable = myData.AsQueryable();
    /// if (gridFilteringAndSorting.SortExpressions[0].Item1 == SortedLinq.OrderBy)
    ///    queryable = queryable.OrderBy(gridFilteringAndSorting.SortExpressions[0].Item2);
    /// </code>
    /// </summary>
    public (SortedLinq, Expression<Func<TGridItem, object?>>)[]? SortExpressions { get; internal set; }

    /// <summary>
    /// Checks if the sorting tuple array <see cref="GridFilteringAndSorting{TGridItem}.SortExpressions"/> is non-null and non-empty.
    /// </summary>
    /// <returns>Returns <c>true</c> if the sorting tuple array is non-null and non-empty, otherwise <c>false</c>.</returns>    
    public readonly bool HasSortExpressions()
    {
        if (SortExpressions != null && SortExpressions.Length != 0)
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    /// <summary>
    /// Checks if the filtering expression array <see cref="GridFilteringAndSorting{TGridItem}.FilterExpressions"/> is non-null and non-empty.
    /// </summary>
    public readonly bool HasFilterExpressions()
    {
        if (FilterExpressions != null && FilterExpressions.Length != 0)
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    /// <summary>
    /// Applies the filtering expressions <see cref="GridFilteringAndSorting{TGridItem}.FilterExpressions"/> to the provided IQueryable using the AndAlso (logical AND) operator.
    /// </summary>
    /// <param name="queryable">The <see cref="IQueryable"/> to filter.</param>
    /// <param name="useDefaultValueForNull">
    /// Indicates whether nullable objects should be treated as having a default value when generating expressions.
    /// <para>If this value is <c>true</c>, the generated expressions will replace <c>null</c> values with the default value of the nullable type.</para>
    /// If this value is <c>false</c>, the generated expressions will treat <c>null</c> values as valid values.
    /// </param>
    /// <param name="ignoreCaseInStringComparison">
    /// Indicates whether string comparisons should be case-insensitive when generating expressions.
    /// <para>If this value is <c>true</c>, the generated expressions will convert strings to lowercase before comparing them, thus making the comparison case-insensitive.</para>
    /// If this value is <c>false</c>, the generated expressions will perform case-sensitive comparisons.
    /// </param>
    /// <param name="operatorType">The operator to use to combine expressions.</param>
    /// <returns>Returns a filtered <see cref="IQueryable"/> or <c>null</c> if no filtering expression is defined.</returns>
    public readonly IQueryable<TGridItem>? ApplyFilterExpressionsForLinq(
        IQueryable<TGridItem> queryable,
        bool useDefaultValueForNull = false,
        bool ignoreCaseInStringComparison = true,
        FilterOperator operatorType = FilterOperator.AndAlso)
    {
        var expression = CombineFilterExpressionsForLinq(useDefaultValueForNull, ignoreCaseInStringComparison, operatorType);
        if (expression != null)
        {
            return queryable.Where(expression);
        }

        return null;
    }

    /// <summary>
    /// Applies the filtering expressions <see cref="GridFilteringAndSorting{TGridItem}.FilterExpressions"/> to the provided IQueryable using the AndAlso (logical AND) operator.
    /// </summary>
    /// <param name="queryable">The <see cref="IQueryable"/> to filter.</param>  
    /// <param name="operatorType">The operator to use to combine expressions.</param>
    /// <returns>Returns a filtered <see cref="IQueryable"/> or <c>null</c> if no filtering expression is defined.</returns>
    public readonly IQueryable<TGridItem>? ApplyFilterExpressions(
        IQueryable<TGridItem> queryable,
        FilterOperator operatorType = FilterOperator.AndAlso)
    {
        var expression = CombineFilterExpressions(operatorType);
        if (expression != null)
        {
            return queryable.Where(expression);
        }

        return null;
    }

    /// <summary>
    /// Applies the sorting tuples <see cref="GridFilteringAndSorting{TGridItem}.SortExpressions"/> to the provided <see cref="IQueryable"/>.
    /// </summary>
    /// <param name="queryable">The <see cref="IQueryable"/> to sort.</param>
    /// <returns>Returns a sorted <see cref="IOrderedQueryable"/> or <c>null</c> if no sorting tuple is defined.</returns>
    public readonly IOrderedQueryable<TGridItem>? ApplySortExpressions(IQueryable<TGridItem> queryable)
    {
        var query = queryable.Order();
        if (HasSortExpressions())
        {
            foreach (var value in SortExpressions!)
            {
                (var sort, var exp) = value;
                if (sort == SortedLinq.OrderBy)
                {
                    query = queryable.OrderBy(exp);
                }

                if (sort == SortedLinq.OrderByDescending)
                {
                    query = queryable.OrderByDescending(exp);
                }

                if (sort == SortedLinq.ThenBy)
                {
                    query = query.ThenBy(exp);
                }

                if (sort == SortedLinq.ThenByDescending)
                {
                    query = query.ThenByDescending(exp);
                }
            }
            return query;
        }
        return null;
    }

    /// <summary>
    /// Applies the filtering expressions <see cref="GridFilteringAndSorting{TGridItem}.FilterExpressions"/> and sorting tuples <see cref="GridFilteringAndSorting{TGridItem}.SortExpressions"/> to the provided <see cref="IQueryable"/>.
    /// </summary>
    /// <param name="queryable">The <see cref="IQueryable"/> to filter and sort.</param>
    /// <param name="useDefaultValueForNull">
    /// Indicates whether nullable objects should be treated as having a default value when generating expressions.
    /// <para>If this value is <c>true</c>, the generated expressions will replace <c>null</c> values with the default value of the nullable type.</para>
    /// If this value is <c>false</c>, the generated expressions will treat <c>null</c> values as valid values.
    /// </param>
    /// <param name="ignoreCaseInStringComparison">
    /// Indicates whether string comparisons should be case-insensitive when generating expressions.
    /// <para>If this value is <c>true</c>, the generated expressions will convert strings to lowercase before comparing them, thus making the comparison case-insensitive.</para>
    /// If this value is <c>false</c>, the generated expressions will perform case-sensitive comparisons.
    /// </param>
    /// <param name="operatorType">The operator to use to combine expressions.</param>
    /// <returns>Returns a filtered and sorted <see cref="IQueryable"/>.</returns>
    public readonly IQueryable<TGridItem> ApplyFilterAndSortExpressionsForLinq(
        IQueryable<TGridItem> queryable,
        bool useDefaultValueForNull = false,
        bool ignoreCaseInStringComparison = true,
        FilterOperator operatorType = FilterOperator.AndAlso)
    {
        var q = ApplyFilterExpressionsForLinq(queryable, useDefaultValueForNull, ignoreCaseInStringComparison, operatorType);
        if (q != null)
        {
            queryable = q;
        }

        var oq = ApplySortExpressions(queryable);
        if (oq != null)
        {
            queryable = oq;
        }

        return queryable;
    }
    /// <summary>
    /// Applies the filtering expressions <see cref="GridFilteringAndSorting{TGridItem}.FilterExpressions"/> and sorting tuples <see cref="GridFilteringAndSorting{TGridItem}.SortExpressions"/> to the provided <see cref="IQueryable"/>.
    /// </summary>
    /// <param name="queryable">The <see cref="IQueryable"/> to filter and sort.</param>        
    /// <param name="operatorType">The operator to use to combine expressions.</param>
    /// <returns>Returns a filtered and sorted <see cref="IQueryable"/>.</returns>
    public readonly IQueryable<TGridItem> ApplyFilterAndSortExpressions(
        IQueryable<TGridItem> queryable,
        FilterOperator operatorType = FilterOperator.AndAlso)
    {
        var q = ApplyFilterExpressions(queryable, operatorType);
        if (q != null)
        {
            queryable = q;
        }

        var oq = ApplySortExpressions(queryable);
        if (oq != null)
        {
            queryable = oq;
        }

        return queryable;
    }
    /// <summary>
    /// Combines the filtering expressions <see cref="GridFilteringAndSorting{TGridItem}.FilterExpressions"/> using the AndAlso (logical AND) operator.
    /// </summary>
    /// <param name="operatorType">The operator to use to combine expressions.</param>
    public readonly Expression<Func<TGridItem, bool>>? CombineFilterExpressions(FilterOperator operatorType = FilterOperator.AndAlso)
    {
        if (HasFilterExpressions())
        {
            var expressionTypet = Enum.Parse<ExpressionType>(operatorType.ToString(), true);
            return CombineExpressions(expressionTypet, FilterExpressions!);
        }
        else
        {
            return null;
        }
    }

    /// <summary>
    /// Combines the filtering expressions <see cref="GridFilteringAndSorting{TGridItem}.FilterExpressions"/> using the AndAlso (logical AND) operator.
    /// Adds a null check for nullable types
    /// </summary>
    /// <param name="useDefaultValueForNull">
    /// Indicates whether nullable objects should be treated as having a default value when generating expressions.
    /// <para>If this value is <c>true</c>, the generated expressions will replace <c>null</c> values with the default value of the nullable type.</para>
    /// If this value is <c>false</c>, the generated expressions will treat <c>null</c> values as valid values.
    /// </param>
    /// <param name="ignoreCaseInStringComparison">
    /// Indicates whether string comparisons should be case-insensitive when generating expressions.
    /// <para>If this value is <c>true</c>, the generated expressions will convert strings to lowercase before comparing them, thus making the comparison case-insensitive.</para>
    /// If this value is <c>false</c>, the generated expressions will perform case-sensitive comparisons.
    /// </param>
    /// <param name="operatorType">The operator to use to combine expressions.</param>
    /// <returns>Returns a combined expression or <c>null</c> if no filtering expression is defined.</returns>
    public readonly Expression<Func<TGridItem, bool>>? CombineFilterExpressionsForLinq(
        bool useDefaultValueForNull = false,
        bool ignoreCaseInStringComparison = true,
        FilterOperator operatorType = FilterOperator.AndAlso)
    {
        if (HasFilterExpressions())
        {
            var expressions = FilterExpressions!.Select(e => NullableCheckAndOptionStringCompare(e, useDefaultValueForNull, ignoreCaseInStringComparison));
            var expressionTypet = Enum.Parse<ExpressionType>(operatorType.ToString(), true);
            return CombineExpressions(expressionTypet, expressions!);
        }
        else
        {
            return null;
        }
    }

    private static Expression<Func<TGridItem, bool>> NullableCheckAndOptionStringCompare(Expression<Func<TGridItem, bool>> e, bool useDefaultValueForNull = false, bool ignoreCaseInStringComparison = true)
    {
        return (Expression<Func<TGridItem, bool>>)(new NullableAndStringComparisonExpressionVisitor(useDefaultValueForNull, ignoreCaseInStringComparison)).Visit(e);
    }

    /// <summary>
    /// Combines the expressions using the specified operator.
    /// </summary>
    /// <typeparam name="T">The return type of the expressions.</typeparam>
    /// <param name="expressionType">The operator to use to combine the expressions.</param>
    /// <param name="expressions">The expressions to combine.</param>
    /// <returns>Returns a combined expression.</returns>
    private static Expression<Func<TGridItem, T>> CombineExpressions<T>(ExpressionType expressionType, IEnumerable<Expression<Func<TGridItem, T>>> expressions)
    {
        var parameter = Expression.Parameter(typeof(TGridItem), "x");
        var replacedExpressions = expressions.Select(e => ((Expression<Func<TGridItem, T>>)ParameterReplacer.Replace(e, e.Parameters[0], parameter)).Body);
        var expression = expressionType switch
        {
            ExpressionType.And => replacedExpressions.Aggregate(Expression.And),
            ExpressionType.AndAlso => replacedExpressions.Aggregate(Expression.AndAlso),
            ExpressionType.AndAssign => replacedExpressions.Aggregate(Expression.AndAssign),
            ExpressionType.Or => replacedExpressions.Aggregate(Expression.Or),
            ExpressionType.OrElse => replacedExpressions.Aggregate(Expression.OrElse),
            ExpressionType.OrAssign => replacedExpressions.Aggregate(Expression.OrAssign),
            _ => throw new NotImplementedException(),
        };
        return Expression.Lambda<Func<TGridItem, T>>(expression, parameter);
    }
}
