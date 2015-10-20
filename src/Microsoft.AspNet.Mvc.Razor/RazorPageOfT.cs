// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq.Expressions;
using Microsoft.AspNet.Mvc.ModelBinding;
using Microsoft.AspNet.Mvc.Razor.Internal;
using Microsoft.AspNet.Mvc.Rendering;
using Microsoft.AspNet.Mvc.ViewFeatures;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNet.Mvc.Razor
{
    /// <summary>
    /// Represents the properties and methods that are needed in order to render a view that uses Razor syntax.
    /// </summary>
    /// <typeparam name="TModel">The type of the view data model.</typeparam>
    public abstract class RazorPage<TModel> : RazorPage
    {
        private IModelMetadataProvider _provider;

        /// <summary>
        /// Gets the Model property of the <see cref="ViewData"/> property.
        /// </summary>
        public TModel Model
        {
            get
            {
                return ViewData == null ? default(TModel) : ViewData.Model;
            }
        }

        /// <summary>
        /// Gets or sets the dictionary for view data.
        /// </summary>
        [RazorInject]
        public ViewDataDictionary<TModel> ViewData { get; set; }

        /// <summary>
        /// Returns a <see cref="ModelExpression"/> instance describing the given <paramref name="expression"/>.
        /// </summary>
        /// <typeparam name="TValue">The type of the <paramref name="expression"/> result.</typeparam>
        /// <param name="expression">An expression to be evaluated against the current model.</param>
        /// <returns>A new <see cref="ModelExpression"/> instance describing the given <paramref name="expression"/>.
        /// </returns>
        /// <remarks>
        /// Compiler normally infers <typeparamref name="TValue"/> from the given <paramref name="expression"/>.
        /// </remarks>
        public ModelExpression CreateModelExpression<TValue>(Expression<Func<TModel, TValue>> expression)
        {
            if (expression == null)
            {
                throw new ArgumentNullException(nameof(expression));
            }

            if (_provider == null)
            {
                _provider = Context.RequestServices.GetRequiredService<IModelMetadataProvider>();
            }

            var name = ExpressionHelper.GetExpressionText(expression);
            var modelExplorer = ExpressionMetadataProvider.FromLambdaExpression(expression, ViewData, _provider);
            if (modelExplorer == null)
            {
                throw new InvalidOperationException(
                    Resources.FormatRazorPage_NullModelMetadata(nameof(IModelMetadataProvider), name));
            }

            return new ModelExpression(name, modelExplorer);
        }
    }
}
