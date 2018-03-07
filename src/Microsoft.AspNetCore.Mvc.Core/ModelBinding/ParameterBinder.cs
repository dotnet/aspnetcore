// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Internal;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Microsoft.AspNetCore.Mvc.ModelBinding
{
    /// <summary>
    /// Binds and validates models specified by a <see cref="ParameterDescriptor"/>.
    /// </summary>
    public class ParameterBinder
    {
        private readonly IModelMetadataProvider _modelMetadataProvider;
        private readonly IModelBinderFactory _modelBinderFactory;
        private readonly IObjectModelValidator _objectModelValidator;

        /// <summary>
        /// <para>This constructor is obsolete and will be removed in a future version. The recommended alternative
        /// is the overload that also takes an <see cref="ILoggerFactory"/>.</para>
        /// <para>Initializes a new instance of <see cref="ParameterBinder"/>.</para>
        /// </summary>
        /// <param name="modelMetadataProvider">The <see cref="IModelMetadataProvider"/>.</param>
        /// <param name="modelBinderFactory">The <see cref="IModelBinderFactory"/>.</param>
        /// <param name="validator">The <see cref="IObjectModelValidator"/>.</param>
        [Obsolete("This constructor is obsolete and will be removed in a future version. The recommended alternative"
            + " is the overload that also takes an " + nameof(ILoggerFactory) + ".")]
        public ParameterBinder(
            IModelMetadataProvider modelMetadataProvider,
            IModelBinderFactory modelBinderFactory,
            IObjectModelValidator validator)
            : this(modelMetadataProvider, modelBinderFactory, validator, NullLoggerFactory.Instance)
        {
        }

        /// <summary>
        /// Initializes a new instance of <see cref="ParameterBinder"/>.
        /// </summary>
        /// <param name="modelMetadataProvider">The <see cref="IModelMetadataProvider"/>.</param>
        /// <param name="modelBinderFactory">The <see cref="IModelBinderFactory"/>.</param>
        /// <param name="validator">The <see cref="IObjectModelValidator"/>.</param>
        /// <param name="loggerFactory">The <see cref="ILoggerFactory"/>.</param>
        public ParameterBinder(
            IModelMetadataProvider modelMetadataProvider,
            IModelBinderFactory modelBinderFactory,
            IObjectModelValidator validator,
            ILoggerFactory loggerFactory)
        {
            if (modelMetadataProvider == null)
            {
                throw new ArgumentNullException(nameof(modelMetadataProvider));
            }

            if (modelBinderFactory == null)
            {
                throw new ArgumentNullException(nameof(modelBinderFactory));
            }

            if (validator == null)
            {
                throw new ArgumentNullException(nameof(validator));
            }

            if (loggerFactory == null)
            {
                throw new ArgumentNullException(nameof(loggerFactory));
            }

            _modelMetadataProvider = modelMetadataProvider;
            _modelBinderFactory = modelBinderFactory;
            _objectModelValidator = validator;
            Logger = loggerFactory.CreateLogger(GetType());
        }

        /// <summary>
        /// The <see cref="ILogger"/> used for logging in this binder.
        /// </summary>
        protected ILogger Logger { get; }

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

            Logger.AttemptingToBindParameterOrProperty(parameter, modelBindingContext);

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

            Logger.DoneAttemptingToBindParameterOrProperty(parameter, modelBindingContext);

            var modelBindingResult = modelBindingContext.Result;

            var baseObjectValidator = _objectModelValidator as ObjectModelValidator;
            if (baseObjectValidator == null)
            {
                // For legacy implementations (which directly implemented IObjectModelValidator), fall back to the
                // back-compatibility logic. In this scenario, top-level validation attributes will be ignored like
                // they were historically.
                if (modelBindingResult.IsModelSet)
                {
                    _objectModelValidator.Validate(
                        actionContext,
                        modelBindingContext.ValidationState,
                        modelBindingContext.ModelName,
                        modelBindingResult.Model);
                }
            }
            else
            {
                Logger.AttemptingToValidateParameterOrProperty(parameter, modelBindingContext);

                EnforceBindRequiredAndValidate(
                    baseObjectValidator,
                    actionContext,
                    metadata,
                    modelBindingContext,
                    modelBindingResult);

                Logger.DoneAttemptingToValidateParameterOrProperty(parameter, modelBindingContext);
            }

            return modelBindingResult;
        }

        private void EnforceBindRequiredAndValidate(
            ObjectModelValidator baseObjectValidator,
            ActionContext actionContext,
            ModelMetadata metadata,
            ModelBindingContext modelBindingContext,
            ModelBindingResult modelBindingResult)
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
                baseObjectValidator.Validate(
                    actionContext,
                    modelBindingContext.ValidationState,
                    modelBindingContext.ModelName,
                    modelBindingResult.Model,
                    metadata);
            }
        }
    }
}
