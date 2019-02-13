// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq.Expressions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ViewFeatures.Internal;

namespace Microsoft.AspNetCore.Mvc.ViewFeatures
{
    /// <summary>
    /// A default implementation of <see cref="IModelMetadataProvider"/>.
    /// </summary>
    public class ModelExpressionProvider : IModelExpressionProvider
    {
        private readonly IModelMetadataProvider _modelMetadataProvider;
        private readonly ExpressionTextCache _expressionTextCache;

        /// <summary>
        /// Creates a  new <see cref="ModelExpressionProvider"/>.
        /// </summary>
        /// <param name="modelMetadataProvider">The <see cref="IModelMetadataProvider"/>.</param>
        /// <param name="expressionTextCache">The <see cref="ExpressionTextCache"/>.</param>
        public ModelExpressionProvider(
            IModelMetadataProvider modelMetadataProvider,
            ExpressionTextCache expressionTextCache)
        {
            if (modelMetadataProvider == null)
            {
                throw new ArgumentNullException(nameof(modelMetadataProvider));
            }

            if (expressionTextCache == null)
            {
                throw new ArgumentNullException(nameof(expressionTextCache));
            }

            _modelMetadataProvider = modelMetadataProvider;
            _expressionTextCache = expressionTextCache;
        }

        /// <inheritdoc />
        public ModelExpression CreateModelExpression<TModel, TValue>(
            ViewDataDictionary<TModel> viewData,
            Expression<Func<TModel, TValue>> expression)
        {
            if (viewData == null)
            {
                throw new ArgumentNullException(nameof(viewData));
            }

            if (expression == null)
            {
                throw new ArgumentNullException(nameof(expression));
            }

            var name = ExpressionHelper.GetExpressionText(expression, _expressionTextCache);
            var modelExplorer = ExpressionMetadataProvider.FromLambdaExpression(expression, viewData, _modelMetadataProvider);
            if (modelExplorer == null)
            {
                throw new InvalidOperationException(
                    Resources.FormatCreateModelExpression_NullModelMetadata(nameof(IModelMetadataProvider), name));
            }

            return new ModelExpression(name, modelExplorer);
        }
    }
}
