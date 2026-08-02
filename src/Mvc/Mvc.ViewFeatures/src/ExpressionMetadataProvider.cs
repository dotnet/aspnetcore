// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Microsoft.AspNetCore.Mvc.ViewFeatures;

internal static class ExpressionMetadataProvider
{
    public static ModelExplorer FromLambdaExpression<TModel, TResult>(
        Expression<Func<TModel, TResult>> expression,
        ViewDataDictionary<TModel> viewData,
        IModelMetadataProvider metadataProvider)
    {
        ArgumentNullException.ThrowIfNull(expression);
        ArgumentNullException.ThrowIfNull(viewData);

        string propertyName = null;
        Type containerType = null;
        var legalExpression = false;

        // Need to verify the expression is valid; it needs to at least end in something
        // that we can convert to a meaningful string for model binding purposes

        switch (expression.Body.NodeType)
        {
            case ExpressionType.ArrayIndex:
                // ArrayIndex always means a single-dimensional indexer;
                // multi-dimensional indexer is a method call to Get().
                legalExpression = true;
                break;

            case ExpressionType.Call:
                // Only legal method call is a single argument indexer/DefaultMember call
                legalExpression = ExpressionHelper.IsSingleArgumentIndexer(expression.Body);
                break;

            case ExpressionType.MemberAccess:
                // Property/field access is always legal
                var memberExpression = (MemberExpression)expression.Body;
                propertyName = memberExpression.Member is PropertyInfo ? memberExpression.Member.Name : null;
                if (string.Equals(propertyName, "Model", StringComparison.Ordinal) &&
                    memberExpression.Type == typeof(TModel) &&
                    memberExpression.Expression.NodeType == ExpressionType.Constant)
                {
                    // Special case the Model property in RazorPage<TModel>. (m => Model) should behave identically
                    // to (m => m). But do the more complicated thing for (m => m.Model) since that is a slightly
                    // different beast.)
                    return FromModel(viewData, metadataProvider);
                }

                // memberExpression.Expression can be null when this is a static field or property.
                //
                // This can be the case if the expression is like (m => Person.Name) where Name is a static field
                // or property on the Person type.
                containerType = memberExpression.Expression?.Type;

                legalExpression = true;
                break;

            case ExpressionType.Parameter:
                // Parameter expression means "model => model", so we delegate to FromModel
                return FromModel(viewData, metadataProvider);
        }

        if (!legalExpression)
        {
            throw new InvalidOperationException(Resources.TemplateHelpers_TemplateLimitations);
        }

        object modelAccessor(object container)
        {
            var model = (TModel)container;
            var cachedFunc = CachedExpressionCompiler.Process(expression);
            if (cachedFunc != null)
            {
                return cachedFunc(model);
            }

            var func = expression.Compile();
            try
            {
                return func(model);
            }
            catch (NullReferenceException)
            {
                return null;
            }
        }

        ModelMetadata metadata = null;
        if (containerType != null && propertyName != null)
        {
            // Ex:
            //    m => m.Color (simple property access)
            //    m => m.Color.Red (nested property access)
            //    m => m.Widgets[0].Size (expression ending with property-access)
            metadata = metadataProvider.GetMetadataForType(containerType).Properties[propertyName];
        }

        if (metadata == null)
        {
            // Ex:
            //    m => 5 (arbitrary expression)
            //    m => foo (arbitrary expression)
            //    m => m.Widgets[0] (expression ending with non-property-access)
            //
            // This can also happen for any case where we cannot retrieve a model metadata.
            // This will happen for:
            // - fields
            // - statics
            // - non-visibility (internal/private)
            metadata = metadataProvider.GetMetadataForType(typeof(TResult));
            Debug.Assert(metadata != null);
        }

        return viewData.ModelExplorer.GetExplorerForExpression(metadata, modelAccessor);
    }

    /// <summary>
    /// Gets <see cref="ModelExplorer"/> for named <paramref name="expression"/> in given
    /// <paramref name="viewData"/>.
    /// </summary>
    /// <param name="expression">Expression name, relative to <c>viewData.Model</c>.</param>
    /// <param name="viewData">
    /// The <see cref="ViewDataDictionary"/> that may contain the <paramref name="expression"/> value.
    /// </param>
    /// <param name="metadataProvider">The <see cref="IModelMetadataProvider"/>.</param>
    /// <returns>
    /// <see cref="ModelExplorer"/> for named <paramref name="expression"/> in given <paramref name="viewData"/>.
    /// </returns>
    public static ModelExplorer FromStringExpression(
        string expression,
        ViewDataDictionary viewData,
        IModelMetadataProvider metadataProvider)
    {
        ArgumentNullException.ThrowIfNull(viewData);

        var viewDataInfo = ViewDataEvaluator.Eval(viewData, expression);
        if (viewDataInfo == null)
        {
            // Try getting a property from ModelMetadata if we couldn't find an answer in ViewData
            var propertyExplorer = viewData.ModelExplorer.GetExplorerForProperty(expression);
            if (propertyExplorer != null)
            {
                return propertyExplorer;
            }
        }

        if (viewDataInfo != null)
        {
            if (viewDataInfo.Container == viewData &&
                viewDataInfo.Value == viewData.Model &&
                string.IsNullOrEmpty(expression))
            {
                // Nothing for empty expression in ViewData and ViewDataEvaluator just returned the model. Handle
                // using FromModel() for its object special case.
                return FromModel(viewData, metadataProvider);
            }

            ModelExplorer containerExplorer = viewData.ModelExplorer;
            if (viewDataInfo.Container != null)
            {
                containerExplorer = metadataProvider.GetModelExplorerForType(
                    viewDataInfo.Container.GetType(),
                    viewDataInfo.Container);
            }

            if (viewDataInfo.PropertyInfo != null)
            {
                // We've identified a property access, which provides us with accurate metadata.
                var containerMetadata = metadataProvider.GetMetadataForType(viewDataInfo.Container.GetType());
                var propertyMetadata = containerMetadata.Properties[viewDataInfo.PropertyInfo.Name];

                Func<object, object> modelAccessor = (ignore) => viewDataInfo.Value;
                return containerExplorer.GetExplorerForExpression(propertyMetadata, modelAccessor);
            }
            else if (viewDataInfo.Value != null)
            {
                // We have a value, even though we may not know where it came from.
                var valueMetadata = metadataProvider.GetMetadataForType(viewDataInfo.Value.GetType());
                return containerExplorer.GetExplorerForExpression(valueMetadata, viewDataInfo.Value);
            }
        }

        // Treat the expression as string if we don't find anything better.
        var stringMetadata = metadataProvider.GetMetadataForType(typeof(string));
        return viewData.ModelExplorer.GetExplorerForExpression(stringMetadata, modelAccessor: null);
    }

    private static ModelExplorer FromModel(
        ViewDataDictionary viewData,
        IModelMetadataProvider metadataProvider)
    {
        ArgumentNullException.ThrowIfNull(viewData);

        if (viewData.ModelMetadata.ModelType == typeof(object))
        {
            // Use common simple type rather than object so e.g. Editor() at least generates a TextBox.
            var model = viewData.Model == null ? null : Convert.ToString(viewData.Model, CultureInfo.CurrentCulture);
            return metadataProvider.GetModelExplorerForType(typeof(string), model);
        }
        else
        {
            return viewData.ModelExplorer;
        }
    }
}
