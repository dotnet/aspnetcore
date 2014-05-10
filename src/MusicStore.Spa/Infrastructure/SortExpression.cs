using System;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.AspNet.Mvc.Rendering.Expressions;

namespace MusicStore.Infrastructure
{
    public static class SortExpression
    {
        private const string SORT_DIRECTION_DESC = " DESC";

        public static IQueryable<TModel> SortBy<TModel, TProperty>(this IQueryable<TModel> query, string sortExpression, Expression<Func<TModel, TProperty>> defaultSortExpression, SortDirection defaultSortDirection = SortDirection.Ascending) where TModel : class
        {
            return SortBy(query, sortExpression ?? Create(defaultSortExpression, defaultSortDirection));
        }

        public static string Create<TModel, TProperty>(Expression<Func<TModel, TProperty>> expression, SortDirection sortDirection = SortDirection.Ascending) where TModel : class
        {
            var expressionText = ExpressionHelper.GetExpressionText(expression);
            // TODO: Validate the expression depth, etc.

            var sortExpression = expressionText;

            if (sortDirection == SortDirection.Descending)
            {
                sortExpression += SORT_DIRECTION_DESC;
            }

            return sortExpression;
        }

        public static IQueryable<T> SortBy<T>(this IQueryable<T> source, string sortExpression) where T : class
        {
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }

            if (String.IsNullOrWhiteSpace(sortExpression))
            {
                return source;
            }

            sortExpression = sortExpression.Trim();
            var isDescending = false;

            // DataSource control passes the sort parameter with a direction
            // if the direction is descending
            if (sortExpression.EndsWith(SORT_DIRECTION_DESC, StringComparison.OrdinalIgnoreCase))
            {
                isDescending = true;
                var descIndex = sortExpression.Length - SORT_DIRECTION_DESC.Length;
                sortExpression = sortExpression.Substring(0, descIndex).Trim();
            }

            if (string.IsNullOrEmpty(sortExpression))
            {
                return source;
            }

            ParameterExpression parameter = Expression.Parameter(source.ElementType, String.Empty);
            
            // Build up the property expression, e.g.: (m => m.Foo.Bar)
            var sortExpressionParts = sortExpression.Split('.');
            Expression propertyExpression = parameter;
            foreach (var property in sortExpressionParts)
            {
                propertyExpression = Expression.Property(propertyExpression, property);
            }

            LambdaExpression lambda = Expression.Lambda(propertyExpression, parameter);

            var methodName = (isDescending) ? "OrderByDescending" : "OrderBy";

            Expression methodCallExpression = Expression.Call(
                typeof(Queryable),
                methodName,
                new [] { source.ElementType, propertyExpression.Type },
                source.Expression,
                Expression.Quote(lambda));

            return (IQueryable<T>)source.Provider.CreateQuery(methodCallExpression);
        }
    }
}