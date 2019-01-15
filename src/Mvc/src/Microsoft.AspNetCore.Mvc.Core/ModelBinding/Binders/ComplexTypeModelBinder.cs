// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Core;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Microsoft.AspNetCore.Mvc.ModelBinding.Binders
{
    /// <summary>
    /// <see cref="IModelBinder"/> implementation for binding complex types.
    /// </summary>
    public class ComplexTypeModelBinder : IModelBinder
    {
        private readonly IDictionary<ModelMetadata, IModelBinder> _propertyBinders;
        private readonly ILogger _logger;
        private Func<object> _modelCreator;

        /// <summary>
        /// <para>This constructor is obsolete and will be removed in a future version. The recommended alternative
        /// is the overload that also takes an <see cref="ILoggerFactory"/>.</para>
        /// <para>Creates a new <see cref="ComplexTypeModelBinder"/>.</para>
        /// </summary>
        /// <param name="propertyBinders">
        /// The <see cref="IDictionary{TKey, TValue}"/> of binders to use for binding properties.
        /// </param>
        [Obsolete("This constructor is obsolete and will be removed in a future version. The recommended alternative"
            + " is the overload that also takes an " + nameof(ILoggerFactory) + ".")]
        public ComplexTypeModelBinder(IDictionary<ModelMetadata, IModelBinder> propertyBinders)
            : this(propertyBinders, NullLoggerFactory.Instance)
        {
        }

        /// <summary>
        /// Creates a new <see cref="ComplexTypeModelBinder"/>.
        /// </summary>
        /// <param name="propertyBinders">
        /// The <see cref="IDictionary{TKey, TValue}"/> of binders to use for binding properties.
        /// </param>
        /// <param name="loggerFactory">The <see cref="ILoggerFactory"/>.</param>
        public ComplexTypeModelBinder(
            IDictionary<ModelMetadata, IModelBinder> propertyBinders,
            ILoggerFactory loggerFactory)
            : this(propertyBinders, loggerFactory, allowValidatingTopLevelNodes: true)
        {
        }

        /// <summary>
        /// Creates a new <see cref="ComplexTypeModelBinder"/>.
        /// </summary>
        /// <param name="propertyBinders">
        /// The <see cref="IDictionary{TKey, TValue}"/> of binders to use for binding properties.
        /// </param>
        /// <param name="loggerFactory">The <see cref="ILoggerFactory"/>.</param>
        /// <param name="allowValidatingTopLevelNodes">
        /// Indication that validation of top-level models is enabled. If <see langword="true"/> and
        /// <see cref="ModelMetadata.IsBindingRequired"/> is <see langword="true"/> for a top-level model, the binder
        /// adds a <see cref="ModelStateDictionary"/> error when the model is not bound.
        /// </param>
        /// <remarks>The <paramref name="allowValidatingTopLevelNodes"/> parameter is currently ignored.</remarks>
        public ComplexTypeModelBinder(
            IDictionary<ModelMetadata, IModelBinder> propertyBinders,
            ILoggerFactory loggerFactory,
            bool allowValidatingTopLevelNodes)
        {
            if (propertyBinders == null)
            {
                throw new ArgumentNullException(nameof(propertyBinders));
            }

            if (loggerFactory == null)
            {
                throw new ArgumentNullException(nameof(loggerFactory));
            }

            _propertyBinders = propertyBinders;
            _logger = loggerFactory.CreateLogger<ComplexTypeModelBinder>();
        }

        public Task BindModelAsync(ModelBindingContext bindingContext)
        {
            if (bindingContext == null)
            {
                throw new ArgumentNullException(nameof(bindingContext));
            }

            _logger.AttemptingToBindModel(bindingContext);

            if (!CanCreateModel(bindingContext))
            {
                return Task.CompletedTask;
            }

            // Perf: separated to avoid allocating a state machine when we don't
            // need to go async.
            return BindModelCoreAsync(bindingContext);
        }

        private async Task BindModelCoreAsync(ModelBindingContext bindingContext)
        {
            // Create model first (if necessary) to avoid reporting errors about properties when activation fails.
            if (bindingContext.Model == null)
            {
                bindingContext.Model = CreateModel(bindingContext);
            }

            var modelMetadata = bindingContext.ModelMetadata;
            var attemptedPropertyBinding = false;
            for (var i = 0; i < modelMetadata.Properties.Count; i++)
            {
                var property = modelMetadata.Properties[i];
                if (!CanBindProperty(bindingContext, property))
                {
                    continue;
                }

                // Pass complex (including collection) values down so that binding system does not unnecessarily
                // recreate instances or overwrite inner properties that are not bound. No need for this with simple
                // values because they will be overwritten if binding succeeds. Arrays are never reused because they
                // cannot be resized.
                object propertyModel = null;
                if (property.PropertyGetter != null &&
                    property.IsComplexType &&
                    !property.ModelType.IsArray)
                {
                    propertyModel = property.PropertyGetter(bindingContext.Model);
                }

                var fieldName = property.BinderModelName ?? property.PropertyName;
                var modelName = ModelNames.CreatePropertyModelName(bindingContext.ModelName, fieldName);

                ModelBindingResult result;
                using (bindingContext.EnterNestedScope(
                    modelMetadata: property,
                    fieldName: fieldName,
                    modelName: modelName,
                    model: propertyModel))
                {
                    await BindProperty(bindingContext);
                    result = bindingContext.Result;
                }

                if (result.IsModelSet)
                {
                    attemptedPropertyBinding = true;
                    SetProperty(bindingContext, modelName, property, result);
                }
                else if (property.IsBindingRequired)
                {
                    attemptedPropertyBinding = true;
                    var message = property.ModelBindingMessageProvider.MissingBindRequiredValueAccessor(fieldName);
                    bindingContext.ModelState.TryAddModelError(modelName, message);
                }
            }

            // Have we created a top-level model despite an inability to bind anything in said model and a lack of
            // other IsBindingRequired errors? Does that violate [BindRequired] on the model? This case occurs when
            // 1. The top-level model has no public settable properties.
            // 2. All properties in a [BindRequired] model have [BindNever] or are otherwise excluded from binding.
            // 3. No data exists for any property.
            if (!attemptedPropertyBinding &&
                bindingContext.IsTopLevelObject &&
                modelMetadata.IsBindingRequired)
            {
                var messageProvider = modelMetadata.ModelBindingMessageProvider;
                var message = messageProvider.MissingBindRequiredValueAccessor(bindingContext.FieldName);
                bindingContext.ModelState.TryAddModelError(bindingContext.ModelName, message);
            }

            bindingContext.Result = ModelBindingResult.Success(bindingContext.Model);
            _logger.DoneAttemptingToBindModel(bindingContext);
        }

        /// <summary>
        /// Gets a value indicating whether or not the model property identified by <paramref name="propertyMetadata"/>
        /// can be bound.
        /// </summary>
        /// <param name="bindingContext">The <see cref="ModelBindingContext"/> for the container model.</param>
        /// <param name="propertyMetadata">The <see cref="ModelMetadata"/> for the model property.</param>
        /// <returns><c>true</c> if the model property can be bound, otherwise <c>false</c>.</returns>
        protected virtual bool CanBindProperty(ModelBindingContext bindingContext, ModelMetadata propertyMetadata)
        {
            var metadataProviderFilter = bindingContext.ModelMetadata.PropertyFilterProvider?.PropertyFilter;
            if (metadataProviderFilter?.Invoke(propertyMetadata) == false)
            {
                return false;
            }

            if (bindingContext.PropertyFilter?.Invoke(propertyMetadata) == false)
            {
                return false;
            }

            if (!propertyMetadata.IsBindingAllowed)
            {
                return false;
            }

            if (!CanUpdatePropertyInternal(propertyMetadata))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Attempts to bind a property of the model.
        /// </summary>
        /// <param name="bindingContext">The <see cref="ModelBindingContext"/> for the model property.</param>
        /// <returns>
        /// A <see cref="Task"/> that when completed will set <see cref="ModelBindingContext.Result"/> to the
        /// result of model binding.
        /// </returns>
        protected virtual Task BindProperty(ModelBindingContext bindingContext)
        {
            var binder = _propertyBinders[bindingContext.ModelMetadata];
            return binder.BindModelAsync(bindingContext);
        }

        internal bool CanCreateModel(ModelBindingContext bindingContext)
        {
            var isTopLevelObject = bindingContext.IsTopLevelObject;

            // If we get here the model is a complex object which was not directly bound by any previous model binder,
            // so we want to decide if we want to continue binding. This is important to get right to avoid infinite
            // recursion.
            //
            // First, we want to make sure this object is allowed to come from a value provider source as this binder
            // will only include value provider data. For instance if the model is marked with [FromBody], then we
            // can just skip it. A greedy source cannot be a value provider.
            //
            // If the model isn't marked with ANY binding source, then we assume it's OK also.
            //
            // We skip this check if it is a top level object because we want to always evaluate
            // the creation of top level object (this is also required for ModelBinderAttribute to work.)
            var bindingSource = bindingContext.BindingSource;
            if (!isTopLevelObject && bindingSource != null && bindingSource.IsGreedy)
            {
                return false;
            }

            // Create the object if:
            // 1. It is a top level model.
            if (isTopLevelObject)
            {
                return true;
            }

            // 2. Any of the model properties can be bound.
            if (CanBindAnyModelProperties(bindingContext))
            {
                return true;
            }

            return false;
        }

        private bool CanBindAnyModelProperties(ModelBindingContext bindingContext)
        {
            // If there are no properties on the model, there is nothing to bind. We are here means this is not a top
            // level object. So we return false.
            if (bindingContext.ModelMetadata.Properties.Count == 0)
            {
                _logger.NoPublicSettableProperties(bindingContext);
                return false;
            }

            // We want to check to see if any of the properties of the model can be bound using the value providers or
            // a greedy binder.
            //
            // Because a property might specify a custom binding source ([FromForm]), it's not correct
            // for us to just try bindingContext.ValueProvider.ContainsPrefixAsync(bindingContext.ModelName);
            // that may include other value providers - that would lead us to mistakenly create the model
            // when the data is coming from a source we should use (ex: value found in query string, but the
            // model has [FromForm]).
            //
            // To do this we need to enumerate the properties, and see which of them provide a binding source
            // through metadata, then we decide what to do.
            //
            //      If a property has a binding source, and it's a greedy source, then it's always bound.
            //
            //      If a property has a binding source, and it's a non-greedy source, then we'll filter the
            //      the value providers to just that source, and see if we can find a matching prefix
            //      (see CanBindValue).
            //
            //      If a property does not have a binding source, then it's fair game for any value provider.
            //
            // Bottom line, if any property meets the above conditions and has a value from ValueProviders, then we'll
            // create the model and try to bind it. Of, if ANY properties of the model have a greedy source,
            // then we go ahead and create it.
            //
            for (var i = 0; i < bindingContext.ModelMetadata.Properties.Count; i++)
            {
                var propertyMetadata = bindingContext.ModelMetadata.Properties[i];
                if (!CanBindProperty(bindingContext, propertyMetadata))
                {
                    continue;
                }

                // If any property can be bound from a greedy binding source, then success.
                var bindingSource = propertyMetadata.BindingSource;
                if (bindingSource != null && bindingSource.IsGreedy)
                {
                    return true;
                }

                // Otherwise, check whether the (perhaps filtered) value providers have a match.
                var fieldName = propertyMetadata.BinderModelName ?? propertyMetadata.PropertyName;
                var modelName = ModelNames.CreatePropertyModelName(bindingContext.ModelName, fieldName);
                using (bindingContext.EnterNestedScope(
                    modelMetadata: propertyMetadata,
                    fieldName: fieldName,
                    modelName: modelName,
                    model: null))
                {
                    // If any property can be bound from a value provider, then success.
                    if (bindingContext.ValueProvider.ContainsPrefix(bindingContext.ModelName))
                    {
                        return true;
                    }
                }
            }

            _logger.CannotBindToComplexType(bindingContext);

            return false;
        }

        // Internal for tests
        internal static bool CanUpdatePropertyInternal(ModelMetadata propertyMetadata)
        {
            return !propertyMetadata.IsReadOnly || CanUpdateReadOnlyProperty(propertyMetadata.ModelType);
        }

        private static bool CanUpdateReadOnlyProperty(Type propertyType)
        {
            // Value types have copy-by-value semantics, which prevents us from updating
            // properties that are marked readonly.
            if (propertyType.GetTypeInfo().IsValueType)
            {
                return false;
            }

            // Arrays are strange beasts since their contents are mutable but their sizes aren't.
            // Therefore we shouldn't even try to update these. Further reading:
            // http://blogs.msdn.com/ericlippert/archive/2008/09/22/arrays-considered-somewhat-harmful.aspx
            if (propertyType.IsArray)
            {
                return false;
            }

            // Special-case known immutable reference types
            if (propertyType == typeof(string))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Creates suitable <see cref="object"/> for given <paramref name="bindingContext"/>.
        /// </summary>
        /// <param name="bindingContext">The <see cref="ModelBindingContext"/>.</param>
        /// <returns>An <see cref="object"/> compatible with <see cref="ModelBindingContext.ModelType"/>.</returns>
        protected virtual object CreateModel(ModelBindingContext bindingContext)
        {
            if (bindingContext == null)
            {
                throw new ArgumentNullException(nameof(bindingContext));
            }

            // If model creator throws an exception, we want to propagate it back up the call stack, since the
            // application developer should know that this was an invalid type to try to bind to.
            if (_modelCreator == null)
            {
                // The following check causes the ComplexTypeModelBinder to NOT participate in binding structs as
                // reflection does not provide information about the implicit parameterless constructor for a struct.
                // This binder would eventually fail to construct an instance of the struct as the Linq's NewExpression
                // compile fails to construct it.
                var modelTypeInfo = bindingContext.ModelType.GetTypeInfo();
                if (modelTypeInfo.IsAbstract || modelTypeInfo.GetConstructor(Type.EmptyTypes) == null)
                {
                    var metadata = bindingContext.ModelMetadata;
                    switch (metadata.MetadataKind)
                    {
                        case ModelMetadataKind.Parameter:
                            throw new InvalidOperationException(
                                Resources.FormatComplexTypeModelBinder_NoParameterlessConstructor_ForParameter(
                                    modelTypeInfo.FullName,
                                    metadata.ParameterName));
                        case ModelMetadataKind.Property:
                            throw new InvalidOperationException(
                                Resources.FormatComplexTypeModelBinder_NoParameterlessConstructor_ForProperty(
                                    modelTypeInfo.FullName,
                                    metadata.PropertyName,
                                    bindingContext.ModelMetadata.ContainerType.FullName));
                        case ModelMetadataKind.Type:
                            throw new InvalidOperationException(
                                Resources.FormatComplexTypeModelBinder_NoParameterlessConstructor_ForType(
                                    modelTypeInfo.FullName));
                    }
                }

                _modelCreator = Expression
                    .Lambda<Func<object>>(Expression.New(bindingContext.ModelType))
                    .Compile();
            }

            return _modelCreator();
        }

        /// <summary>
        /// Updates a property in the current <see cref="ModelBindingContext.Model"/>.
        /// </summary>
        /// <param name="bindingContext">The <see cref="ModelBindingContext"/>.</param>
        /// <param name="modelName">The model name.</param>
        /// <param name="propertyMetadata">The <see cref="ModelMetadata"/> for the property to set.</param>
        /// <param name="result">The <see cref="ModelBindingResult"/> for the property's new value.</param>
        protected virtual void SetProperty(
            ModelBindingContext bindingContext,
            string modelName,
            ModelMetadata propertyMetadata,
            ModelBindingResult result)
        {
            if (bindingContext == null)
            {
                throw new ArgumentNullException(nameof(bindingContext));
            }

            if (modelName == null)
            {
                throw new ArgumentNullException(nameof(modelName));
            }

            if (propertyMetadata == null)
            {
                throw new ArgumentNullException(nameof(propertyMetadata));
            }

            if (!result.IsModelSet)
            {
                // If we don't have a value, don't set it on the model and trounce a pre-initialized value.
                return;
            }

            if (propertyMetadata.IsReadOnly)
            {
                // The property should have already been set when we called BindPropertyAsync, so there's
                // nothing to do here.
                return;
            }

            var value = result.Model;
            try
            {
                propertyMetadata.PropertySetter(bindingContext.Model, value);
            }
            catch (Exception exception)
            {
                AddModelError(exception, modelName, bindingContext);
            }
        }

        private static void AddModelError(
            Exception exception,
            string modelName,
            ModelBindingContext bindingContext)
        {
            var targetInvocationException = exception as TargetInvocationException;
            if (targetInvocationException?.InnerException != null)
            {
                exception = targetInvocationException.InnerException;
            }

            // Do not add an error message if a binding error has already occurred for this property.
            var modelState = bindingContext.ModelState;
            var validationState = modelState.GetFieldValidationState(modelName);
            if (validationState == ModelValidationState.Unvalidated)
            {
                modelState.AddModelError(modelName, exception, bindingContext.ModelMetadata);
            }
        }
    }
}
