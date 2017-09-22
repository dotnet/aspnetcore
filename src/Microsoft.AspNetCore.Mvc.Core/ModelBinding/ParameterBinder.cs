// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Internal;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace Microsoft.AspNetCore.Mvc.ModelBinding
{
    /// <summary>
    /// Binds and validates models specified by a <see cref="ParameterDescriptor"/>.
    /// </summary>
    public class ParameterBinder
    {
        private readonly IModelMetadataProvider _modelMetadataProvider;
        private readonly IModelBinderFactory _modelBinderFactory;
        private readonly IObjectModelValidator _validatorForBackCompatOnly;
        private readonly IModelValidatorProvider _validatorProvider;
        private readonly ValidatorCache _validatorCache;

        /// <summary>
        /// Initializes a new instance of <see cref="ParameterBinder"/>.
        /// </summary>
        /// <param name="modelMetadataProvider">The <see cref="IModelMetadataProvider"/>.</param>
        /// <param name="modelBinderFactory">The <see cref="IModelBinderFactory"/>.</param>
        /// <param name="validatorProvider">The <see cref="IModelValidatorProvider"/>.</param>
        public ParameterBinder(
            IModelMetadataProvider modelMetadataProvider,
            IModelBinderFactory modelBinderFactory,
            IModelValidatorProvider validatorProvider)
            : this(modelMetadataProvider, modelBinderFactory, validatorProvider, null)
        {
            if (validatorProvider == null)
            {
                throw new ArgumentNullException(nameof(validatorProvider));
            }
        }

        /// <summary>
        /// Initializes a new instance of <see cref="ParameterBinder"/>.
        /// </summary>
        /// <param name="modelMetadataProvider">The <see cref="IModelMetadataProvider"/>.</param>
        /// <param name="modelBinderFactory">The <see cref="IModelBinderFactory"/>.</param>
        /// <param name="validator">The <see cref="IObjectModelValidator"/>.</param>
        [Obsolete("This constructor is obsolete and will be removed in a future version. The recommended alternative is the overload that takes a " + nameof(IModelValidatorProvider) + " instead of a " + nameof(IObjectModelValidator) + ".")]
        public ParameterBinder(
            IModelMetadataProvider modelMetadataProvider,
            IModelBinderFactory modelBinderFactory,
            IObjectModelValidator validator)
            : this(modelMetadataProvider, modelBinderFactory, null, validator)
        {
            // Note: When this obsolete constructor overload is removed, also remember
            // to remove the validatorForBackCompatOnly field.

            if (validator == null)
            {
                throw new ArgumentNullException(nameof(validator));
            }
        }

        private ParameterBinder(
            IModelMetadataProvider modelMetadataProvider,
            IModelBinderFactory modelBinderFactory,
            IModelValidatorProvider validatorProvider,
            IObjectModelValidator validatorForBackCompatOnly)
        {
            if (modelMetadataProvider == null)
            {
                throw new ArgumentNullException(nameof(modelMetadataProvider));
            }

            if (modelBinderFactory == null)
            {
                throw new ArgumentNullException(nameof(modelBinderFactory));
            }

            _modelMetadataProvider = modelMetadataProvider;
            _modelBinderFactory = modelBinderFactory;
            _validatorProvider = validatorProvider;
            _validatorForBackCompatOnly = validatorForBackCompatOnly;
            _validatorCache = new ValidatorCache();
        }

        /// <summary>
        /// Initializes and binds a model specified by <paramref name="parameter"/>.
        /// </summary>
        /// <param name="actionContext">The <see cref="ActionContext"/>.</param>
        /// <param name="valueProvider">The <see cref="IValueProvider"/>.</param>
        /// <param name="parameter">The <see cref="ParameterDescriptor"/></param>
        /// <returns>The result of model binding.</returns>
        public Task<ModelBindingResult> BindModelAsync(
            ActionContext actionContext,
            IValueProvider valueProvider,
            ParameterDescriptor parameter)
        {
            return BindModelAsync(actionContext, valueProvider, parameter, value: null);
        }

        /// <summary>
        /// Binds a model specified by <paramref name="parameter"/> using <paramref name="value"/> as the initial value.
        /// </summary>
        /// <param name="actionContext">The <see cref="ActionContext"/>.</param>
        /// <param name="valueProvider">The <see cref="IValueProvider"/>.</param>
        /// <param name="parameter">The <see cref="ParameterDescriptor"/></param>
        /// <param name="value">The initial model value.</param>
        /// <returns>The result of model binding.</returns>
        public virtual Task<ModelBindingResult> BindModelAsync(
            ActionContext actionContext,
            IValueProvider valueProvider,
            ParameterDescriptor parameter,
            object value)
        {
            if (actionContext == null)
            {
                throw new ArgumentNullException(nameof(actionContext));
            }

            if (valueProvider == null)
            {
                throw new ArgumentNullException(nameof(valueProvider));
            }

            if (parameter == null)
            {
                throw new ArgumentNullException(nameof(parameter));
            }

            var metadata = _modelMetadataProvider.GetMetadataForType(parameter.ParameterType);
            var binder = _modelBinderFactory.CreateBinder(new ModelBinderFactoryContext
            {
                BindingInfo = parameter.BindingInfo,
                Metadata = metadata,
                CacheToken = parameter,
            });

            return BindModelAsync(
                actionContext,
                binder,
                valueProvider,
                parameter,
                metadata,
                value);
        }

        /// <summary>
        /// Binds a model specified by <paramref name="parameter"/> using <paramref name="value"/> as the initial value.
        /// </summary>
        /// <param name="actionContext">The <see cref="ActionContext"/>.</param>
        /// <param name="modelBinder">The <see cref="IModelBinder"/>.</param>
        /// <param name="valueProvider">The <see cref="IValueProvider"/>.</param>
        /// <param name="parameter">The <see cref="ParameterDescriptor"/></param>
        /// <param name="metadata">The <see cref="ModelMetadata"/>.</param>
        /// <param name="value">The initial model value.</param>
        /// <returns>The result of model binding.</returns>
        public virtual async Task<ModelBindingResult> BindModelAsync(
            ActionContext actionContext,
            IModelBinder modelBinder,
            IValueProvider valueProvider,
            ParameterDescriptor parameter,
            ModelMetadata metadata,
            object value)
        {
            if (actionContext == null)
            {
                throw new ArgumentNullException(nameof(actionContext));
            }

            if (modelBinder == null)
            {
                throw new ArgumentNullException(nameof(modelBinder));
            }

            if (valueProvider == null)
            {
                throw new ArgumentNullException(nameof(valueProvider));
            }

            if (parameter == null)
            {
                throw new ArgumentNullException(nameof(parameter));
            }

            if (metadata == null)
            {
                throw new ArgumentNullException(nameof(metadata));
            }

            if (parameter.BindingInfo?.RequestPredicate?.Invoke(actionContext) == false)
            {
                return ModelBindingResult.Failed();
            }

            var modelBindingContext = DefaultModelBindingContext.CreateBindingContext(
                actionContext,
                valueProvider,
                metadata,
                parameter.BindingInfo,
                parameter.Name);
            modelBindingContext.Model = value;

            var parameterModelName = parameter.BindingInfo?.BinderModelName ?? metadata.BinderModelName;
            if (parameterModelName != null)
            {
                // The name was set explicitly, always use that as the prefix.
                modelBindingContext.ModelName = parameterModelName;
            }
            else if (modelBindingContext.ValueProvider.ContainsPrefix(parameter.Name))
            {
                // We have a match for the parameter name, use that as that prefix.
                modelBindingContext.ModelName = parameter.Name;
            }
            else
            {
                // No match, fallback to empty string as the prefix.
                modelBindingContext.ModelName = string.Empty;
            }

            await modelBinder.BindModelAsync(modelBindingContext);

            var modelBindingResult = modelBindingContext.Result;

            if (_validatorForBackCompatOnly != null)
            {
                // Since we don't have access to an IModelValidatorProvider, fall back
                // on back-compatibility logic. In this scenario, top-level validation
                // attributes will be ignored like they were historically.
                if (modelBindingResult.IsModelSet)
                {
                    _validatorForBackCompatOnly.Validate(
                        actionContext,
                        modelBindingContext.ValidationState,
                        modelBindingContext.ModelName,
                        modelBindingResult.Model);
                }
            }
            else
            {
                EnforceBindRequiredAndValidate(
                    actionContext,
                    metadata,
                    modelBindingContext,
                    modelBindingResult);
            }

            return modelBindingResult;
        }

        private void EnforceBindRequiredAndValidate(ActionContext actionContext, ModelMetadata metadata, ModelBindingContext modelBindingContext, ModelBindingResult modelBindingResult)
        {
            if (!modelBindingResult.IsModelSet && metadata.IsBindingRequired)
            {
                // Enforce BindingBehavior.Required (e.g., [BindRequired])
                var modelName = modelBindingContext.FieldName;
                var message = metadata.ModelBindingMessageProvider.MissingBindRequiredValueAccessor(modelName);
                actionContext.ModelState.TryAddModelError(modelName, message);
            }
            else if (modelBindingResult.IsModelSet || metadata.IsRequired)
            {
                // Enforce any other validation rules
                var visitor = new ValidationVisitor(
                    actionContext,
                    _validatorProvider,
                    _validatorCache,
                    _modelMetadataProvider,
                    modelBindingContext.ValidationState);

                visitor.Validate(
                    metadata,
                    modelBindingContext.ModelName,
                    modelBindingResult.Model,
                    alwaysValidateAtTopLevel: metadata.IsRequired);
            }
        }
    }
}
