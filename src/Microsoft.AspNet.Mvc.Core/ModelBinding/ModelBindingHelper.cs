// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc.Core;
using Microsoft.AspNet.Mvc.Formatters;
using Microsoft.AspNet.Mvc.ModelBinding.Validation;

namespace Microsoft.AspNet.Mvc.ModelBinding
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
        /// <param name="actionContext">The <see cref="ActionContext"/> for the current executing request.</param>
        /// <param name="metadataProvider">The provider used for reading metadata for the model type.</param>
        /// <param name="modelBinder">The <see cref="IModelBinder"/> used for binding.</param>
        /// <param name="valueProvider">The <see cref="IValueProvider"/> used for looking up values.</param>
        /// <param name="inputFormatters">
        /// The set of <see cref="IInputFormatter"/> instances for deserializing the body.
        /// </param>
        /// <param name="objectModelValidator">The <see cref="IObjectModelValidator"/> used for validating the
        /// bound values.</param>
        /// <param name="validatorProvider">The <see cref="IModelValidatorProvider"/> used for executing validation
        /// on the model instance.</param>
        /// <returns>A <see cref="Task"/> that on completion returns <c>true</c> if the update is successful</returns>
        public static Task<bool> TryUpdateModelAsync<TModel>(
            TModel model,
            string prefix,
            ActionContext actionContext,
            IModelMetadataProvider metadataProvider,
            IModelBinder modelBinder,
            IValueProvider valueProvider,
            IList<IInputFormatter> inputFormatters,
            IObjectModelValidator objectModelValidator,
            IModelValidatorProvider validatorProvider)
            where TModel : class
        {
            if (model == null)
            {
                throw new ArgumentNullException(nameof(model));
            }

            if (prefix == null)
            {
                throw new ArgumentNullException(nameof(prefix));
            }

            if (actionContext == null)
            {
                throw new ArgumentNullException(nameof(actionContext));
            }

            if (metadataProvider == null)
            {
                throw new ArgumentNullException(nameof(metadataProvider));
            }

            if (modelBinder == null)
            {
                throw new ArgumentNullException(nameof(modelBinder));
            }

            if (valueProvider == null)
            {
                throw new ArgumentNullException(nameof(valueProvider));
            }

            if (inputFormatters == null)
            {
                throw new ArgumentNullException(nameof(inputFormatters));
            }

            if (objectModelValidator == null)
            {
                throw new ArgumentNullException(nameof(objectModelValidator));
            }

            if (validatorProvider == null)
            {
                throw new ArgumentNullException(nameof(validatorProvider));
            }

            // Includes everything by default.
            return TryUpdateModelAsync(
                model,
                prefix,
                actionContext,
                metadataProvider,
                modelBinder,
                valueProvider,
                inputFormatters,
                objectModelValidator,
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
        /// <param name="actionContext">The <see cref="ActionContext"/> for the current executing request.</param>
        /// <param name="metadataProvider">The provider used for reading metadata for the model type.</param>
        /// <param name="modelBinder">The <see cref="IModelBinder"/> used for binding.</param>
        /// <param name="valueProvider">The <see cref="IValueProvider"/> used for looking up values.</param>
        /// <param name="inputFormatters">
        /// The set of <see cref="IInputFormatter"/> instances for deserializing the body.
        /// </param>
        /// <param name="objectModelValidator">The <see cref="IObjectModelValidator"/> used for validating the
        /// bound values.</param>
        /// <param name="validatorProvider">The <see cref="IModelValidatorProvider"/> used for executing validation
        /// on the model
        /// instance.</param>
        /// <param name="includeExpressions">Expression(s) which represent top level properties
        /// which need to be included for the current model.</param>
        /// <returns>A <see cref="Task"/> that on completion returns <c>true</c> if the update is successful</returns>
        public static Task<bool> TryUpdateModelAsync<TModel>(
            TModel model,
            string prefix,
            ActionContext actionContext,
            IModelMetadataProvider metadataProvider,
            IModelBinder modelBinder,
            IValueProvider valueProvider,
            IList<IInputFormatter> inputFormatters,
            IObjectModelValidator objectModelValidator,
            IModelValidatorProvider validatorProvider,
            params Expression<Func<TModel, object>>[] includeExpressions)
           where TModel : class
        {
            if (model == null)
            {
                throw new ArgumentNullException(nameof(model));
            }

            if (prefix == null)
            {
                throw new ArgumentNullException(nameof(prefix));
            }

            if (actionContext == null)
            {
                throw new ArgumentNullException(nameof(actionContext));
            }

            if (metadataProvider == null)
            {
                throw new ArgumentNullException(nameof(metadataProvider));
            }

            if (modelBinder == null)
            {
                throw new ArgumentNullException(nameof(modelBinder));
            }

            if (valueProvider == null)
            {
                throw new ArgumentNullException(nameof(valueProvider));
            }

            if (inputFormatters == null)
            {
                throw new ArgumentNullException(nameof(inputFormatters));
            }

            if (objectModelValidator == null)
            {
                throw new ArgumentNullException(nameof(objectModelValidator));
            }

            if (validatorProvider == null)
            {
                throw new ArgumentNullException(nameof(validatorProvider));
            }

            if (includeExpressions == null)
            {
                throw new ArgumentNullException(nameof(includeExpressions));
            }

            var includeExpression = GetIncludePredicateExpression(prefix, includeExpressions);
            var predicate = includeExpression.Compile();

            return TryUpdateModelAsync(
               model,
               prefix,
               actionContext,
               metadataProvider,
               modelBinder,
               valueProvider,
               inputFormatters,
               objectModelValidator,
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
        /// <param name="actionContext">The <see cref="ActionContext"/> for the current executing request.</param>
        /// <param name="metadataProvider">The provider used for reading metadata for the model type.</param>
        /// <param name="modelBinder">The <see cref="IModelBinder"/> used for binding.</param>
        /// <param name="valueProvider">The <see cref="IValueProvider"/> used for looking up values.</param>
        /// <param name="inputFormatters">
        /// The set of <see cref="IInputFormatter"/> instances for deserializing the body.
        /// </param>
        /// <param name="objectModelValidator">The <see cref="IObjectModelValidator"/> used for validating the
        /// bound values.</param>
        /// <param name="validatorProvider">The <see cref="IModelValidatorProvider"/> used for executing validation
        /// on the model instance.</param>
        /// <param name="predicate">A predicate which can be used to
        /// filter properties(for inclusion/exclusion) at runtime.</param>
        /// <returns>A <see cref="Task"/> that on completion returns <c>true</c> if the update is successful</returns>
        public static Task<bool> TryUpdateModelAsync<TModel>(
            TModel model,
            string prefix,
            ActionContext actionContext,
            IModelMetadataProvider metadataProvider,
            IModelBinder modelBinder,
            IValueProvider valueProvider,
            IList<IInputFormatter> inputFormatters,
            IObjectModelValidator objectModelValidator,
            IModelValidatorProvider validatorProvider,
            Func<ModelBindingContext, string, bool> predicate)
            where TModel : class
        {
            if (model == null)
            {
                throw new ArgumentNullException(nameof(model));
            }

            if (prefix == null)
            {
                throw new ArgumentNullException(nameof(prefix));
            }

            if (actionContext == null)
            {
                throw new ArgumentNullException(nameof(actionContext));
            }

            if (metadataProvider == null)
            {
                throw new ArgumentNullException(nameof(metadataProvider));
            }

            if (modelBinder == null)
            {
                throw new ArgumentNullException(nameof(modelBinder));
            }

            if (valueProvider == null)
            {
                throw new ArgumentNullException(nameof(valueProvider));
            }

            if (inputFormatters == null)
            {
                throw new ArgumentNullException(nameof(inputFormatters));
            }

            if (objectModelValidator == null)
            {
                throw new ArgumentNullException(nameof(objectModelValidator));
            }

            if (validatorProvider == null)
            {
                throw new ArgumentNullException(nameof(validatorProvider));
            }

            if (predicate == null)
            {
                throw new ArgumentNullException(nameof(predicate));
            }

            return TryUpdateModelAsync(
               model,
               typeof(TModel),
               prefix,
               actionContext,
               metadataProvider,
               modelBinder,
               valueProvider,
               inputFormatters,
               objectModelValidator,
               validatorProvider,
               predicate: predicate);
        }

        /// <summary>
        /// Updates the specified <paramref name="model"/> instance using the specified <paramref name="modelBinder"/>
        /// and the specified <paramref name="valueProvider"/> and executes validation using the specified
        /// <paramref name="validatorProvider"/>.
        /// </summary>
        /// <param name="model">The model instance to update and validate.</param>
        /// <param name="modelType">The type of model instance to update and validate.</param>
        /// <param name="prefix">The prefix to use when looking up values in the <paramref name="valueProvider"/>.
        /// </param>
        /// <param name="actionContext">The <see cref="ActionContext"/> for the current executing request.</param>
        /// <param name="metadataProvider">The provider used for reading metadata for the model type.</param>
        /// <param name="modelBinder">The <see cref="IModelBinder"/> used for binding.</param>
        /// <param name="valueProvider">The <see cref="IValueProvider"/> used for looking up values.</param>
        /// <param name="inputFormatters">
        /// The set of <see cref="IInputFormatter"/> instances for deserializing the body.
        /// </param>
        /// <param name="objectModelValidator">The <see cref="IObjectModelValidator"/> used for validating the
        /// bound values.</param>
        /// <param name="validatorProvider">The <see cref="IModelValidatorProvider"/> used for executing validation
        /// on the model instance.</param>
        /// <returns>A <see cref="Task"/> that on completion returns <c>true</c> if the update is successful</returns>
        public static Task<bool> TryUpdateModelAsync(
                object model,
                Type modelType,
                string prefix,
                ActionContext actionContext,
                IModelMetadataProvider metadataProvider,
                IModelBinder modelBinder,
                IValueProvider valueProvider,
                IList<IInputFormatter> inputFormatters,
                IObjectModelValidator objectModelValidator,
                IModelValidatorProvider validatorProvider)
        {
            if (model == null)
            {
                throw new ArgumentNullException(nameof(model));
            }

            if (modelType == null)
            {
                throw new ArgumentNullException(nameof(modelType));
            }

            if (prefix == null)
            {
                throw new ArgumentNullException(nameof(prefix));
            }

            if (actionContext == null)
            {
                throw new ArgumentNullException(nameof(actionContext));
            }

            if (metadataProvider == null)
            {
                throw new ArgumentNullException(nameof(metadataProvider));
            }

            if (modelBinder == null)
            {
                throw new ArgumentNullException(nameof(modelBinder));
            }

            if (valueProvider == null)
            {
                throw new ArgumentNullException(nameof(valueProvider));
            }

            if (inputFormatters == null)
            {
                throw new ArgumentNullException(nameof(inputFormatters));
            }

            if (objectModelValidator == null)
            {
                throw new ArgumentNullException(nameof(objectModelValidator));
            }

            if (validatorProvider == null)
            {
                throw new ArgumentNullException(nameof(validatorProvider));
            }

            // Includes everything by default.
            return TryUpdateModelAsync(
                model,
                modelType,
                prefix,
                actionContext,
                metadataProvider,
                modelBinder,
                valueProvider,
                inputFormatters,
                objectModelValidator,
                validatorProvider,
                predicate: (context, propertyName) => true);
        }

        /// <summary>
        /// Updates the specified <paramref name="model"/> instance using the specified <paramref name="modelBinder"/>
        /// and the specified <paramref name="valueProvider"/> and executes validation using the specified
        /// <paramref name="validatorProvider"/>.
        /// </summary>
        /// <param name="model">The model instance to update and validate.</param>
        /// <param name="modelType">The type of model instance to update and validate.</param>
        /// <param name="prefix">The prefix to use when looking up values in the <paramref name="valueProvider"/>.
        /// </param>
        /// <param name="actionContext">The <see cref="ActionContext"/> for the current executing request.</param>
        /// <param name="metadataProvider">The provider used for reading metadata for the model type.</param>
        /// <param name="modelBinder">The <see cref="IModelBinder"/> used for binding.</param>
        /// <param name="valueProvider">The <see cref="IValueProvider"/> used for looking up values.</param>
        /// <param name="inputFormatters">
        /// The set of <see cref="IInputFormatter"/> instances for deserializing the body.
        /// </param>
        /// <param name="objectModelValidator">The <see cref="IObjectModelValidator"/> used for validating the
        /// bound values.</param>
        /// <param name="validatorProvider">The <see cref="IModelValidatorProvider"/> used for executing validation
        /// on the model instance.</param>
        /// <param name="predicate">A predicate which can be used to
        /// filter properties(for inclusion/exclusion) at runtime.</param>
        /// <returns>A <see cref="Task"/> that on completion returns <c>true</c> if the update is successful</returns>
        public static async Task<bool> TryUpdateModelAsync(
               object model,
               Type modelType,
               string prefix,
               ActionContext actionContext,
               IModelMetadataProvider metadataProvider,
               IModelBinder modelBinder,
               IValueProvider valueProvider,
               IList<IInputFormatter> inputFormatters,
               IObjectModelValidator objectModelValidator,
               IModelValidatorProvider validatorProvider,
               Func<ModelBindingContext, string, bool> predicate)
        {
            if (model == null)
            {
                throw new ArgumentNullException(nameof(model));
            }

            if (modelType == null)
            {
                throw new ArgumentNullException(nameof(modelType));
            }

            if (prefix == null)
            {
                throw new ArgumentNullException(nameof(prefix));
            }

            if (actionContext == null)
            {
                throw new ArgumentNullException(nameof(actionContext));
            }

            if (metadataProvider == null)
            {
                throw new ArgumentNullException(nameof(metadataProvider));
            }

            if (modelBinder == null)
            {
                throw new ArgumentNullException(nameof(modelBinder));
            }

            if (valueProvider == null)
            {
                throw new ArgumentNullException(nameof(valueProvider));
            }

            if (inputFormatters == null)
            {
                throw new ArgumentNullException(nameof(inputFormatters));
            }

            if (objectModelValidator == null)
            {
                throw new ArgumentNullException(nameof(objectModelValidator));
            }

            if (validatorProvider == null)
            {
                throw new ArgumentNullException(nameof(validatorProvider));
            }

            if (predicate == null)
            {
                throw new ArgumentNullException(nameof(predicate));
            }

            if (!modelType.IsAssignableFrom(model.GetType()))
            {
                var message = Resources.FormatModelType_WrongType(
                    model.GetType().FullName,
                    modelType.FullName);
                throw new ArgumentException(message, nameof(modelType));
            }

            var modelMetadata = metadataProvider.GetMetadataForType(modelType);

            // Clear ModelStateDictionary entries for the model so that it will be re-validated.
            var modelState = actionContext.ModelState;
            ClearValidationStateForModel(modelType, modelState, metadataProvider, prefix);

            var operationBindingContext = new OperationBindingContext
            {
                InputFormatters = inputFormatters,
                ModelBinder = modelBinder,
                ValidatorProvider = validatorProvider,
                MetadataProvider = metadataProvider,
                ActionContext = actionContext,
                ValueProvider = valueProvider,
            };

            var modelBindingContext = ModelBindingContext.CreateBindingContext(
                operationBindingContext,
                modelMetadata,
                bindingInfo: null,
                modelName: prefix ?? string.Empty);
            modelBindingContext.Model = model;
            modelBindingContext.PropertyFilter = predicate;

            var modelBindingResult = await modelBinder.BindModelAsync(modelBindingContext);
            if (modelBindingResult.IsModelSet)
            {
                objectModelValidator.Validate(
                    operationBindingContext.ActionContext,
                    operationBindingContext.ValidatorProvider,
                    modelBindingContext.ValidationState,
                    modelBindingResult.Key,
                    modelBindingResult.Model);

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
            var property = ModelNames.CreatePropertyModelName(prefix, propertyName);

            return
             (context, modelPropertyName) =>
                 property.Equals(ModelNames.CreatePropertyModelName(context.ModelName, modelPropertyName),
                 StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Clears <see cref="ModelStateDictionary"/> entries for <see cref="ModelMetadata"/>.
        /// </summary>
        /// <param name="modelMetadata">The <see cref="ModelMetadata"/>.</param>
        /// <param name="modelKey">The entry to clear. </param>
        /// <param name="modelMetadataProvider">The <see cref="IModelMetadataProvider"/>.</param>
        public static void ClearValidationStateForModel(
            Type modelType,
            ModelStateDictionary modelstate,
            IModelMetadataProvider metadataProvider,
            string modelKey)
        {
            if (modelType == null)
            {
                throw new ArgumentNullException(nameof(modelType));
            }

            if (modelstate == null)
            {
                throw new ArgumentNullException(nameof(modelstate));
            }

            if (metadataProvider == null)
            {
                throw new ArgumentNullException(nameof(metadataProvider));
            }

            // If modelkey is empty, we need to iterate through properties (obtained from ModelMetadata) and
            // clear validation state for all entries in ModelStateDictionary that start with each property name.
            // If modelkey is non-empty, clear validation state for all entries in ModelStateDictionary
            // that start with modelKey
            if (string.IsNullOrEmpty(modelKey))
            {
                var modelMetadata = metadataProvider.GetMetadataForType(modelType);
                var elementMetadata = modelMetadata.ElementMetadata;
                if (elementMetadata != null)
                {
                    modelMetadata = elementMetadata;
                }

                foreach (var property in modelMetadata.Properties)
                {
                    var childKey = property.BinderModelName ?? property.PropertyName;
                    modelstate.ClearValidationState(childKey);
                }
            }
            else
            {
                modelstate.ClearValidationState(modelKey);
            }
        }

        internal static void ValidateBindingContext(ModelBindingContext bindingContext)
        {
            if (bindingContext == null)
            {
                throw new ArgumentNullException(nameof(bindingContext));
            }

            if (bindingContext.ModelMetadata == null)
            {
                throw new ArgumentException(Resources.ModelBinderUtil_ModelMetadataCannotBeNull, nameof(bindingContext));
            }
        }

        internal static void ValidateBindingContext(
            ModelBindingContext bindingContext,
            Type requiredType,
            bool allowNullModel)
        {
            ValidateBindingContext(bindingContext);

            if (bindingContext.ModelType != requiredType)
            {
                var message = Resources.FormatModelBinderUtil_ModelTypeIsWrong(bindingContext.ModelType, requiredType);
                throw new ArgumentException(message, nameof(bindingContext));
            }

            if (!allowNullModel && bindingContext.Model == null)
            {
                var message = Resources.FormatModelBinderUtil_ModelCannotBeNull(requiredType);
                throw new ArgumentException(message, nameof(bindingContext));
            }

            if (bindingContext.Model != null &&
                !bindingContext.ModelType.GetTypeInfo().IsAssignableFrom(requiredType.GetTypeInfo()))
            {
                var message = Resources.FormatModelBinderUtil_ModelInstanceIsWrong(
                    bindingContext.Model.GetType(),
                    requiredType);
                throw new ArgumentException(message, nameof(bindingContext));
            }
        }

        internal static TModel CastOrDefault<TModel>(object model)
        {
            return (model is TModel) ? (TModel)model : default(TModel);
        }

        public static object ConvertValuesToCollectionType<T>(Type modelType, IList<T> values)
        {
            // There's a limited set of collection types we can support here.
            //
            // For the simple cases - choose a T[] or List<T> if the destination type supports
            // it.
            //
            // For more complex cases, if the destination type is a class and implements ICollection<T>
            // then activate it and add the values.
            //
            // Otherwise just give up.
            if (typeof(List<T>).IsAssignableFrom(modelType))
            {
                return new List<T>(values);
            }
            else if (typeof(T[]).IsAssignableFrom(modelType))
            {
                return values.ToArray();
            }
            else if (
                modelType.GetTypeInfo().IsClass &&
                !modelType.GetTypeInfo().IsAbstract &&
                typeof(ICollection<T>).IsAssignableFrom(modelType))
            {
                var result = (ICollection<T>)Activator.CreateInstance(modelType);
                foreach (var value in values)
                {
                    result.Add(value);
                }

                return result;
            }
            else if (typeof(IEnumerable<T>).IsAssignableFrom(modelType))
            {
                return values;
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Converts the provided <paramref name="value"/> to a value of <see cref="Type"/> <typeparamref name="T"/>
        /// using the <see cref="CultureInfo.InvariantCulture"/>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Type"/> for conversion.</typeparam>
        /// <param name="value">The value to convert."/></param>
        /// <returns>
        /// The converted value or the default value of <typeparamref name="T"/> if the value could not be converted.
        /// </returns>
        public static T ConvertTo<T>(object value)
        {
            return ConvertTo<T>(value, culture: null);
        }

        /// <summary>
        /// Converts the provided <paramref name="value"/> to a value of <see cref="Type"/> <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Type"/> for conversion.</typeparam>
        /// <param name="value">The value to convert."/></param>
        /// <param name="culture">The <see cref="CultureInfo"/> for conversion.</param>
        /// <returns>
        /// The converted value or the default value of <typeparamref name="T"/> if the value could not be converted.
        /// </returns>
        public static T ConvertTo<T>(object value, CultureInfo culture)
        {
            var converted = ConvertTo(value, typeof(T), culture);
            return converted == null ? default(T) : (T)converted;
        }

        /// <summary>
        /// Converts the provided <paramref name="value"/> to a value of <see cref="Type"/> <param name="type"/>
        /// using the <see cref="CultureInfo.InvariantCulture"/>.
        /// </summary>
        /// <param name="value">The value to convert."/></param>
        /// <param name="type">The <see cref="Type"/> for conversion.</param>
        /// <returns>
        /// The converted value or <c>null</c> if the value could not be converted.
        /// </returns>
        public static object ConvertTo(object value, Type type)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            return ConvertTo(value, type, culture: null);
        }

        /// <summary>
        /// Converts the provided <paramref name="value"/> to a value of <see cref="Type"/> <paramref name="type"/>.
        /// </summary>
        /// <param name="value">The value to convert."/></param>
        /// <param name="type">The <see cref="Type"/> for conversion.</param>
        /// <param name="culture">The <see cref="CultureInfo"/> for conversion.</param>
        /// <returns>
        /// The converted value or <c>null</c> if the value could not be converted.
        /// </returns>
        public static object ConvertTo(object value, Type type, CultureInfo culture)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            if (value == null)
            {
                // For value types, treat null values as though they were the default value for the type.
                return type.GetTypeInfo().IsValueType ? Activator.CreateInstance(type) : null;
            }

            if (value.GetType().IsAssignableFrom(type))
            {
                return value;
            }

            var cultureToUse = culture ?? CultureInfo.InvariantCulture;
            return UnwrapPossibleArrayType(value, type, cultureToUse);
        }

        private static object UnwrapPossibleArrayType(object value, Type destinationType, CultureInfo culture)
        {
            // array conversion results in four cases, as below
            var valueAsArray = value as Array;
            if (destinationType.IsArray)
            {
                var destinationElementType = destinationType.GetElementType();
                if (valueAsArray != null)
                {
                    // case 1: both destination + source type are arrays, so convert each element
                    var converted = (IList)Array.CreateInstance(destinationElementType, valueAsArray.Length);
                    for (var i = 0; i < valueAsArray.Length; i++)
                    {
                        converted[i] = ConvertSimpleType(valueAsArray.GetValue(i), destinationElementType, culture);
                    }
                    return converted;
                }
                else
                {
                    // case 2: destination type is array but source is single element, so wrap element in
                    // array + convert
                    var element = ConvertSimpleType(value, destinationElementType, culture);
                    var converted = (IList)Array.CreateInstance(destinationElementType, 1);
                    converted[0] = element;
                    return converted;
                }
            }
            else if (valueAsArray != null)
            {
                // case 3: destination type is single element but source is array, so extract first element + convert
                if (valueAsArray.Length > 0)
                {
                    value = valueAsArray.GetValue(0);
                    return ConvertSimpleType(value, destinationType, culture);
                }
                else
                {
                    // case 3(a): source is empty array, so can't perform conversion
                    return null;
                }
            }

            // case 4: both destination + source type are single elements, so convert
            return ConvertSimpleType(value, destinationType, culture);
        }

        private static object ConvertSimpleType(object value, Type destinationType, CultureInfo culture)
        {
            if (value == null || value.GetType().IsAssignableFrom(destinationType))
            {
                return value;
            }

            // In case of a Nullable object, we try again with its underlying type.
            destinationType = UnwrapNullableType(destinationType);

            // if this is a user-input value but the user didn't type anything, return no value
            var valueAsString = value as string;
            if (valueAsString != null && string.IsNullOrWhiteSpace(valueAsString))
            {
                return null;
            }

            var converter = TypeDescriptor.GetConverter(destinationType);
            var canConvertFrom = converter.CanConvertFrom(value.GetType());
            if (!canConvertFrom)
            {
                converter = TypeDescriptor.GetConverter(value.GetType());
            }
            if (!(canConvertFrom || converter.CanConvertTo(destinationType)))
            {
                // EnumConverter cannot convert integer, so we verify manually
                if (destinationType.GetTypeInfo().IsEnum &&
                    (value is int ||
                    value is uint ||
                    value is long ||
                    value is ulong ||
                    value is short ||
                    value is ushort ||
                    value is byte ||
                    value is sbyte))
                {
                    return Enum.ToObject(destinationType, value);
                }

                throw new InvalidOperationException(
                    Resources.FormatValueProviderResult_NoConverterExists(value.GetType(), destinationType));
            }

            try
            {
                return canConvertFrom
                    ? converter.ConvertFrom(null, culture, value)
                    : converter.ConvertTo(null, culture, value, destinationType);
            }
            catch (FormatException)
            {
                throw;
            }
            catch (Exception ex)
            {
                if (ex.InnerException == null)
                {
                    throw;
                }
                else
                {
                    // TypeConverter throws System.Exception wrapping the FormatException,
                    // so we throw the inner exception.
                    ExceptionDispatchInfo.Capture(ex.InnerException).Throw();

                    // This code is never reached because the previous line will always throw.
                    throw;
                }
            }
        }

        private static Type UnwrapNullableType(Type destinationType)
        {
            return Nullable.GetUnderlyingType(destinationType) ?? destinationType;
        }
    }
}
