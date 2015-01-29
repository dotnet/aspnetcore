// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Mvc.Core;
using Microsoft.AspNet.Mvc.ModelBinding;

namespace Microsoft.AspNet.Mvc
{
    public static class ModelBindingHelper
    {
        /// <summary>
        /// Updates the specified <paramref name="model"/> instance using the specified <paramref name="modelBinder"/>
        /// and the specified <paramref name="valueProvider"/> and executes validation using the specified
        /// <paramref name="validatorProvider"/>.
        /// </summary>
        /// <typeparam name="TModel">The type of the model object.</typeparam>
        /// <param name="model">The model instance to update and validate.</param>
        /// <param name="prefix">The prefix to use when looking up values in the <paramref name="valueProvider"/>.
        /// </param>
        /// <param name="httpContext">The <see cref="HttpContext"/> for the current executing request.</param>
        /// <param name="modelState">The <see cref="ModelStateDictionary"/> used for maintaining state and
        /// results of model-binding validation.</param>
        /// <param name="metadataProvider">The provider used for reading metadata for the model type.</param>
        /// <param name="modelBinder">The <see cref="IModelBinder"/> used for binding.</param>
        /// <param name="valueProvider">The <see cref="IValueProvider"/> used for looking up values.</param>
        /// <param name="validatorProvider">The <see cref="IModelValidatorProvider"/> used for executing validation
        /// on the model instance.</param>
        /// <returns>A <see cref="Task"/> that on completion returns <c>true</c> if the update is successful</returns>
        public static Task<bool> TryUpdateModelAsync<TModel>(
                [NotNull] TModel model,
                [NotNull] string prefix,
                [NotNull] HttpContext httpContext,
                [NotNull] ModelStateDictionary modelState,
                [NotNull] IModelMetadataProvider metadataProvider,
                [NotNull] IModelBinder modelBinder,
                [NotNull] IValueProvider valueProvider,
                [NotNull] IModelValidatorProvider validatorProvider)
            where TModel : class
        {
            // Includes everything by default.
            return TryUpdateModelAsync(
                model,
                prefix,
                httpContext,
                modelState,
                metadataProvider,
                modelBinder,
                valueProvider,
                validatorProvider,
                predicate: (context, propertyName) => true);
        }

        /// <summary>
        /// Updates the specified <paramref name="model"/> instance using the specified <paramref name="modelBinder"/>
        /// and the specified <paramref name="valueProvider"/> and executes validation using the specified
        /// <paramref name="validatorProvider"/>.
        /// </summary>
        /// <typeparam name="TModel">The type of the model object.</typeparam>
        /// <param name="model">The model instance to update and validate.</param>
        /// <param name="prefix">The prefix to use when looking up values in the <paramref name="valueProvider"/>.
        /// </param>
        /// <param name="httpContext">The <see cref="HttpContext"/> for the current executing request.</param>
        /// <param name="modelState">The <see cref="ModelStateDictionary"/> used for maintaining state and
        /// results of model-binding validation.</param>
        /// <param name="metadataProvider">The provider used for reading metadata for the model type.</param>
        /// <param name="modelBinder">The <see cref="IModelBinder"/> used for binding.</param>
        /// <param name="valueProvider">The <see cref="IValueProvider"/> used for looking up values.</param>
        /// <param name="validatorProvider">The <see cref="IModelValidatorProvider"/> used for executing validation
        /// on the model
        /// instance.</param>
        /// <param name="includeExpressions">Expression(s) which represent top level properties
        /// which need to be included for the current model.</param>
        /// <returns>A <see cref="Task"/> that on completion returns <c>true</c> if the update is successful</returns>
        public static Task<bool> TryUpdateModelAsync<TModel>(
               [NotNull] TModel model,
               [NotNull] string prefix,
               [NotNull] HttpContext httpContext,
               [NotNull] ModelStateDictionary modelState,
               [NotNull] IModelMetadataProvider metadataProvider,
               [NotNull] IModelBinder modelBinder,
               [NotNull] IValueProvider valueProvider,
               [NotNull] IModelValidatorProvider validatorProvider,
               [NotNull] params Expression<Func<TModel, object>>[] includeExpressions)
           where TModel : class
        {
            var includeExpression = GetIncludePredicateExpression(prefix, includeExpressions);
            var predicate = includeExpression.Compile();

            return TryUpdateModelAsync(
               model,
               prefix,
               httpContext,
               modelState,
               metadataProvider,
               modelBinder,
               valueProvider,
               validatorProvider,
               predicate: predicate);
        }

        /// <summary>
        /// Updates the specified <paramref name="model"/> instance using the specified <paramref name="modelBinder"/>
        /// and the specified <paramref name="valueProvider"/> and executes validation using the specified
        /// <paramref name="validatorProvider"/>.
        /// </summary>
        /// <typeparam name="TModel">The type of the model object.</typeparam>
        /// <param name="model">The model instance to update and validate.</param>
        /// <param name="prefix">The prefix to use when looking up values in the <paramref name="valueProvider"/>.
        /// </param>
        /// <param name="httpContext">The <see cref="HttpContext"/> for the current executing request.</param>
        /// <param name="modelState">The <see cref="ModelStateDictionary"/> used for maintaining state and
        /// results of model-binding validation.</param>
        /// <param name="metadataProvider">The provider used for reading metadata for the model type.</param>
        /// <param name="modelBinder">The <see cref="IModelBinder"/> used for binding.</param>
        /// <param name="valueProvider">The <see cref="IValueProvider"/> used for looking up values.</param>
        /// <param name="validatorProvider">The <see cref="IModelValidatorProvider"/> used for executing validation
        /// on the model instance.</param>
        /// <param name="predicate">A predicate which can be used to
        /// filter properties(for inclusion/exclusion) at runtime.</param>
        /// <returns>A <see cref="Task"/> that on completion returns <c>true</c> if the update is successful</returns>
        public static async Task<bool> TryUpdateModelAsync<TModel>(
               [NotNull] TModel model,
               [NotNull] string prefix,
               [NotNull] HttpContext httpContext,
               [NotNull] ModelStateDictionary modelState,
               [NotNull] IModelMetadataProvider metadataProvider,
               [NotNull] IModelBinder modelBinder,
               [NotNull] IValueProvider valueProvider,
               [NotNull] IModelValidatorProvider validatorProvider,
               [NotNull] Func<ModelBindingContext, string, bool> predicate)
           where TModel : class
        {
            var modelMetadata = metadataProvider.GetMetadataForType(
                modelAccessor: null,
                modelType: typeof(TModel));
            var operationBindingContext = new OperationBindingContext
            {
                ModelBinder = modelBinder,
                ValidatorProvider = validatorProvider,
                MetadataProvider = metadataProvider,
                HttpContext = httpContext
            };

            var modelBindingContext = new ModelBindingContext
            {
                ModelMetadata = modelMetadata,
                ModelName = prefix,
                Model = model,
                ModelState = modelState,
                ValueProvider = valueProvider,
                FallbackToEmptyPrefix = true,
                OperationBindingContext = operationBindingContext,
                PropertyFilter = predicate
            };

            if (await modelBinder.BindModelAsync(modelBindingContext))
            {
                return modelState.IsValid;
            }

            return false;
        }

        // Internal for tests
        internal static string GetPropertyName(Expression expression)
        {
            if (expression.NodeType == ExpressionType.Convert ||
                expression.NodeType == ExpressionType.ConvertChecked)
            {
                // For Boxed Value Types
                expression = ((UnaryExpression)expression).Operand;
            }

            if (expression.NodeType != ExpressionType.MemberAccess)
            {
                throw new InvalidOperationException(Resources.FormatInvalid_IncludePropertyExpression(
                        expression.NodeType));
            }

            var memberExpression = (MemberExpression)expression;
            var memberInfo = memberExpression.Member as PropertyInfo;
            if (memberInfo != null)
            {
                if (memberExpression.Expression.NodeType != ExpressionType.Parameter)
                {
                    // Chained expressions and non parameter based expressions are not supported.
                    throw new InvalidOperationException(
                    Resources.FormatInvalid_IncludePropertyExpression(expression.NodeType));
                }

                return memberInfo.Name;
            }
            else
            {
                // Fields are also not supported.
                throw new InvalidOperationException(Resources.FormatInvalid_IncludePropertyExpression(
                    expression.NodeType));
            }
        }

        /// <summary>
        /// Creates an expression for a predicate to limit the set of properties used in model binding.
        /// </summary>
        /// <typeparam name="TModel">The model type.</typeparam>
        /// <param name="prefix">The model prefix.</param>
        /// <param name="expressions">Expressions identifying the properties to allow for binding.</param>
        /// <returns>An expression which can be used with <see cref="IPropertyBindingPredicateProvider"/>.</returns>
        public static Expression<Func<ModelBindingContext, string, bool>> GetIncludePredicateExpression<TModel>(
            string prefix, 
            Expression<Func<TModel, object>>[] expressions)
        {
            if (expressions.Length == 0)
            {
                // If nothing is included explcitly, treat everything as included.
                return (context, propertyName) => true;
            }

            var firstExpression = GetPredicateExpression(prefix, expressions[0]);
            var orWrapperExpression = firstExpression.Body;
            foreach (var expression in expressions.Skip(1))
            {
                var predicate = GetPredicateExpression(prefix, expression);
                orWrapperExpression = Expression.OrElse(orWrapperExpression,
                                                        Expression.Invoke(predicate, firstExpression.Parameters));
            }

            return Expression.Lambda<Func<ModelBindingContext, string, bool>>(
                orWrapperExpression, firstExpression.Parameters);
        }

        private static Expression<Func<ModelBindingContext, string, bool>> GetPredicateExpression<TModel>
            (string prefix, Expression<Func<TModel, object>> expression)
        {
            var propertyName = GetPropertyName(expression.Body);
            var property = CreatePropertyModelName(prefix, propertyName);

            return
             (context, modelPropertyName) =>
                 property.Equals(CreatePropertyModelName(context.ModelName, modelPropertyName),
                 StringComparison.OrdinalIgnoreCase);
        }

        public static string CreatePropertyModelName(string prefix, string propertyName)
        {
            if (string.IsNullOrEmpty(prefix))
            {
                return propertyName ?? string.Empty;
            }
            else if (string.IsNullOrEmpty(propertyName))
            {
                return prefix ?? string.Empty;
            }
            else
            {
                return prefix + "." + propertyName;
            }
        }
    }
}