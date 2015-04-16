// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc.ModelBinding.Internal;
using Microsoft.AspNet.Mvc.ModelBinding.Validation;

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    public class MutableObjectModelBinder : IModelBinder
    {
        public virtual async Task<ModelBindingResult> BindModelAsync(ModelBindingContext bindingContext)
        {
            ModelBindingHelper.ValidateBindingContext(bindingContext);
            if (!CanBindType(bindingContext.ModelMetadata))
            {
                return null;
            }

            var mutableObjectBinderContext = new MutableObjectBinderContext()
            {
                ModelBindingContext = bindingContext,
                PropertyMetadata = GetMetadataForProperties(bindingContext),
            };

            if (!(await CanCreateModel(mutableObjectBinderContext)))
            {
                return null;
            }

            EnsureModel(bindingContext);
            var result = await CreateAndPopulateDto(bindingContext, mutableObjectBinderContext.PropertyMetadata);

            // post-processing, e.g. property setters and hooking up validation
            ProcessDto(bindingContext, (ComplexModelDto)result.Model);
            return new ModelBindingResult(
                bindingContext.Model,
                bindingContext.ModelName,
                isModelSet: true);
        }

        protected virtual bool CanUpdateProperty(ModelMetadata propertyMetadata)
        {
            return CanUpdatePropertyInternal(propertyMetadata);
        }

        internal async Task<bool> CanCreateModel(MutableObjectBinderContext context)
        {
            var bindingContext = context.ModelBindingContext;

            var isTopLevelObject = bindingContext.ModelMetadata.ContainerType == null;
            var hasExplicitAlias = bindingContext.BinderModelName != null;

            // If we get here the model is a complex object which was not directly bound by any previous model binder,
            // so we want to decide if we want to continue binding. This is important to get right to avoid infinite
            // recursion.
            //
            // First, we want to make sure this object is allowed to come from a value provider source as this binder
            // will always include value provider data. For instance if the model is marked with [FromBody], then we
            // can just skip it. A greedy source cannot be a value provider.
            //
            // If the model isn't marked with ANY binding source, then we assume it's ok also.
            //
            // We skip this check if it is a top level object because we want to always evaluate
            // the creation of top level object (this is also required for ModelBinderAttribute to work.)
            var bindingSource = bindingContext.BindingSource;
            if (!isTopLevelObject &&
                bindingSource != null &&
                bindingSource.IsGreedy)
            {
                return false;
            }

            // Create the object if :
            // 1. It is a top level model with an explicit user supplied prefix.
            //    In this case since it will never fallback to empty prefix, we need to create the model here.
            if (isTopLevelObject && hasExplicitAlias)
            {
                return true;
            }

            // 2. It is a top level object and there is no model name ( Fallback to empty prefix case ).
            //    This is necessary as we do not want to depend on a value provider to contain an empty prefix.
            if (isTopLevelObject && bindingContext.ModelName == string.Empty)
            {
                return true;
            }

            // 3. Any of the model properties can be bound using a value provider.
            if (await CanValueBindAnyModelProperties(context))
            {
                return true;
            }

            return false;
        }

        private async Task<bool> CanValueBindAnyModelProperties(MutableObjectBinderContext context)
        {
            // We want to check to see if any of the properties of the model can be bound using the value providers,
            // because that's all that MutableObjectModelBinder can handle.
            //
            // However, because a property might specify a custom binding source ([FromForm]), it's not correct
            // for us to just try bindingContext.ValueProvider.ContainsPrefixAsync(bindingContext.ModelName),
            // because that may include ALL value providers - that would lead us to mistakenly create the model
            // when the data is coming from a source we should use (ex: value found in query string, but the
            // model has [FromForm]).
            //
            // To do this we need to enumerate the properties, and see which of them provide a binding source
            // through metadata, then we decide what to do.
            //
            //      If a property has a binding source, and it's a greedy source, then it's not
            //      allowed to come from a value provider, so we skip it.
            //
            //      If a property has a binding source, and it's a non-greedy source, then we'll filter the
            //      the value providers to just that source, and see if we can find a matching prefix
            //      (see CanBindValue).
            //
            //      If a property does not have a binding source, then it's fair game for any value provider.
            //
            // If any property meets the above conditions and has a value from valueproviders, then we'll
            // create the model and try to bind it. OR if ALL properties of the model have a greedy source,
            // then we go ahead and create it.
            //
            var isAnyPropertyEnabledForValueProviderBasedBinding = false;
            foreach (var propertyMetadata in context.PropertyMetadata)
            {
                // This check will skip properties which are marked explicitly using a non value binder.
                var bindingSource = propertyMetadata.BindingSource;
                if (bindingSource == null || !bindingSource.IsGreedy)
                {
                    isAnyPropertyEnabledForValueProviderBasedBinding = true;

                    var propertyModelName = ModelBindingHelper.CreatePropertyModelName(
                        context.ModelBindingContext.ModelName,
                        propertyMetadata.BinderModelName ?? propertyMetadata.PropertyName);

                    var propertyModelBindingContext = ModelBindingContext.GetChildModelBindingContext(
                        context.ModelBindingContext,
                        propertyModelName,
                        propertyMetadata);

                    // If any property can return a true value.
                    if (await CanBindValue(propertyModelBindingContext))
                    {
                        return true;
                    }
                }
            }

            if (!isAnyPropertyEnabledForValueProviderBasedBinding)
            {
                // Either there are no properties or all the properties are marked as
                // a non value provider based marker.
                // This would be the case when the model has all its properties annotated with
                // a IBinderMetadata. We want to be able to create such a model.
                return true;
            }

            return false;
        }

        private async Task<bool> CanBindValue(ModelBindingContext bindingContext)
        {
            var valueProvider = bindingContext.ValueProvider;

            var bindingSource = bindingContext.BindingSource;
            if (bindingSource != null && !bindingSource.IsGreedy)
            {
                var rootValueProvider = bindingContext.OperationBindingContext.ValueProvider as IBindingSourceValueProvider;
                if (rootValueProvider != null)
                {
                    valueProvider = rootValueProvider.Filter(bindingSource);
                }
            }

            if (await valueProvider.ContainsPrefixAsync(bindingContext.ModelName))
            {
                return true;
            }

            return false;
        }

        private static bool CanBindType(ModelMetadata modelMetadata)
        {
            // Simple types cannot use this binder
            if (!modelMetadata.IsComplexType)
            {
                return false;
            }

            if (modelMetadata.ModelType == typeof(ComplexModelDto))
            {
                // forbidden type - will cause a stack overflow if we try binding this type
                return false;
            }

            return true;
        }

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

        private async Task<ModelBindingResult> CreateAndPopulateDto(
            ModelBindingContext bindingContext,
            IEnumerable<ModelMetadata> propertyMetadatas)
        {
            // create a DTO and call into the DTO binder
            var dto = new ComplexModelDto(bindingContext.ModelMetadata, propertyMetadatas);

            var metadataProvider = bindingContext.OperationBindingContext.MetadataProvider;
            var dtoMetadata = metadataProvider.GetMetadataForType(typeof(ComplexModelDto));

            var childContext = ModelBindingContext.GetChildModelBindingContext(
                bindingContext,
                bindingContext.ModelName,
                dtoMetadata);

            childContext.Model = dto;

            return await bindingContext.OperationBindingContext.ModelBinder.BindModelAsync(childContext);
        }

        protected virtual object CreateModel(ModelBindingContext bindingContext)
        {
            // If the Activator throws an exception, we want to propagate it back up the call stack, since the
            // application developer should know that this was an invalid type to try to bind to.
            return Activator.CreateInstance(bindingContext.ModelType);
        }

        protected virtual void EnsureModel(ModelBindingContext bindingContext)
        {
            if (bindingContext.Model == null)
            {
                bindingContext.Model = CreateModel(bindingContext);
            }
        }

        protected virtual IEnumerable<ModelMetadata> GetMetadataForProperties(ModelBindingContext bindingContext)
        {
            var validationInfo = GetPropertyValidationInfo(bindingContext);
            var newPropertyFilter = GetPropertyFilter();
            return bindingContext.ModelMetadata.Properties
                                 .Where(propertyMetadata =>
                                    newPropertyFilter(bindingContext, propertyMetadata.PropertyName) &&
                                    (validationInfo.RequiredProperties.Contains(propertyMetadata.PropertyName) ||
                                    !validationInfo.SkipProperties.Contains(propertyMetadata.PropertyName)) &&
                                    CanUpdateProperty(propertyMetadata));
        }

        private static Func<ModelBindingContext, string, bool> GetPropertyFilter()
        {
            return (ModelBindingContext context, string propertyName) =>
            {
                var modelMetadataPredicate = context.ModelMetadata.PropertyBindingPredicateProvider?.PropertyFilter;

                return
                    context.PropertyFilter(context, propertyName) &&
                    (modelMetadataPredicate == null || modelMetadataPredicate(context, propertyName));
            };
        }

        private static bool TryGetPropertyDefaultValue(PropertyInfo propertyInfo, out object value)
        {
            var attribute = propertyInfo.GetCustomAttribute<DefaultValueAttribute>();
            if (attribute == null)
            {
                value = null;
                return false;
            }
            else
            {
                value = attribute.Value;
                return true;
            }
        }

        internal static PropertyValidationInfo GetPropertyValidationInfo(ModelBindingContext bindingContext)
        {
            var validationInfo = new PropertyValidationInfo();

            foreach (var propertyMetadata in bindingContext.ModelMetadata.Properties)
            {
                var propertyName = propertyMetadata.PropertyName;

                if (!propertyMetadata.IsBindingAllowed)
                {
                    // Nothing to do here if binding is not allowed.
                    validationInfo.SkipProperties.Add(propertyName);
                    continue;
                }

                if (propertyMetadata.IsBindingRequired)
                {
                    validationInfo.RequiredProperties.Add(propertyName);
                }

                var validatorProviderContext = new ModelValidatorProviderContext(propertyMetadata);
                bindingContext.OperationBindingContext.ValidatorProvider.GetValidators(validatorProviderContext);

                var requiredValidator = validatorProviderContext.Validators
                    .FirstOrDefault(v => v != null && v.IsRequired);
                if (requiredValidator != null)
                {
                    validationInfo.RequiredValidators[propertyName] = requiredValidator;
                    validationInfo.RequiredProperties.Add(propertyName);
                }
            }

            return validationInfo;
        }

        internal void ProcessDto(ModelBindingContext bindingContext, ComplexModelDto dto)
        {
            var metadataProvider = bindingContext.OperationBindingContext.MetadataProvider;
            var modelExplorer = metadataProvider.GetModelExplorerForType(bindingContext.ModelType, bindingContext.Model);

            var validationInfo = GetPropertyValidationInfo(bindingContext);

            // Eliminate provided properties from requiredProperties; leaving just *missing* required properties.
            var boundProperties = dto.Results.Where(p => p.Value.IsModelSet).Select(p => p.Key.PropertyName);
            validationInfo.RequiredProperties.ExceptWith(boundProperties);

            foreach (var missingRequiredProperty in validationInfo.RequiredProperties)
            {
                var addedError = false;

                // We want to provide the 'null' value, not the value of model.Property,
                // so avoiding modelExplorer.GetProperty here which would call the actual getter on the
                // model. This avoids issues with value types, or properties with pre-initialized values.
                var propertyExplorer = modelExplorer.GetExplorerForProperty(missingRequiredProperty, model: null);

                var propertyName = propertyExplorer.Metadata.BinderModelName ?? missingRequiredProperty;
                var modelStateKey = ModelBindingHelper.CreatePropertyModelName(
                    bindingContext.ModelName,
                    propertyName);

                // Execute validator (if any) to get custom error message.
                IModelValidator validator;
                if (validationInfo.RequiredValidators.TryGetValue(missingRequiredProperty, out validator))
                {
                    addedError = RunValidator(validator, bindingContext, propertyExplorer, modelStateKey);
                }

                // Fall back to default message if BindingBehaviorAttribute required this property or validator
                // (oddly) succeeded.
                if (!addedError)
                {
                    bindingContext.ModelState.TryAddModelError(
                        modelStateKey,
                        Resources.FormatMissingRequiredMember(propertyName));
                }
            }

            // For each property that ComplexModelDtoModelBinder attempted to bind, call the setter, recording
            // exceptions as necessary.
            foreach (var entry in dto.Results)
            {
                var dtoResult = entry.Value;
                if (dtoResult != null)
                {
                    var propertyMetadata = entry.Key;
                    IModelValidator requiredValidator;
                    validationInfo.RequiredValidators.TryGetValue(
                        propertyMetadata.PropertyName,
                        out requiredValidator);

                    SetProperty(bindingContext, modelExplorer, propertyMetadata, dtoResult, requiredValidator);
                }
            }
        }

        protected virtual void SetProperty(
            ModelBindingContext bindingContext,
            ModelExplorer modelExplorer,
            ModelMetadata propertyMetadata,
            ModelBindingResult dtoResult,
            IModelValidator requiredValidator)
        {
            var bindingFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase;
            var property = bindingContext.ModelType.GetProperty(
                propertyMetadata.PropertyName,
                bindingFlags);

            if (property == null || !property.CanWrite)
            {
                // nothing to do
                return;
            }

            object value;
            var hasDefaultValue = false;
            if (dtoResult.IsModelSet)
            {
                value = dtoResult.Model;
            }
            else
            {
                hasDefaultValue = TryGetPropertyDefaultValue(property, out value);
            }

            // 'Required' validators need to run first so that we can provide useful error messages if
            // the property setters throw, e.g. if we're setting entity keys to null.
            if (value == null)
            {
                var modelStateKey = dtoResult.Key;
                var validationState = bindingContext.ModelState.GetFieldValidationState(modelStateKey);
                if (validationState == ModelValidationState.Unvalidated)
                {
                    if (requiredValidator != null)
                    {
                        var propertyExplorer = modelExplorer.GetExplorerForExpression(propertyMetadata, model: null);
                        var validationContext = new ModelValidationContext(bindingContext, propertyExplorer);
                        foreach (var validationResult in requiredValidator.Validate(validationContext))
                        {
                            bindingContext.ModelState.TryAddModelError(modelStateKey, validationResult.Message);
                        }
                    }
                }
            }

            if (!dtoResult.IsModelSet && !hasDefaultValue)
            {
                // If we don't have a value, don't set it on the model and trounce a pre-initialized
                // value.
                return;
            }

            if (value != null || property.PropertyType.AllowsNullValue())
            {
                try
                {
                    propertyMetadata.PropertySetter(bindingContext.Model, value);
                }
                catch (Exception ex)
                {
                    // don't display a duplicate error message if a binding error has already occurred for this field
                    var targetInvocationException = ex as TargetInvocationException;
                    if (targetInvocationException != null &&
                        targetInvocationException.InnerException != null)
                    {
                        ex = targetInvocationException.InnerException;
                    }
                    var modelStateKey = dtoResult.Key;
                    var validationState = bindingContext.ModelState.GetFieldValidationState(modelStateKey);
                    if (validationState == ModelValidationState.Unvalidated)
                    {
                        bindingContext.ModelState.AddModelError(modelStateKey, ex);
                    }
                }
            }
            else
            {
                // trying to set a non-nullable value type to null, need to make sure there's a message
                var modelStateKey = dtoResult.Key;
                var validationState = bindingContext.ModelState.GetFieldValidationState(modelStateKey);
                if (validationState == ModelValidationState.Unvalidated)
                {
                    var errorMessage = Resources.ModelBinderConfig_ValueRequired;
                    bindingContext.ModelState.TryAddModelError(modelStateKey, errorMessage);
                }
            }
        }

        // Returns true if validator execution adds a model error.
        private static bool RunValidator(
            IModelValidator validator,
            ModelBindingContext bindingContext,
            ModelExplorer propertyExplorer,
            string modelStateKey)
        {
            var validationContext = new ModelValidationContext(bindingContext, propertyExplorer);

            var addedError = false;
            foreach (var validationResult in validator.Validate(validationContext))
            {
                bindingContext.ModelState.TryAddModelError(modelStateKey, validationResult.Message);
                addedError = true;
            }

            if (!addedError)
            {
                bindingContext.ModelState.MarkFieldValid(modelStateKey);
            }

            return addedError;
        }

        internal sealed class PropertyValidationInfo
        {
            public PropertyValidationInfo()
            {
                RequiredProperties = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                RequiredValidators = new Dictionary<string, IModelValidator>(StringComparer.OrdinalIgnoreCase);
                SkipProperties = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            }

            public HashSet<string> RequiredProperties { get; private set; }

            public Dictionary<string, IModelValidator> RequiredValidators { get; private set; }

            public HashSet<string> SkipProperties { get; private set; }
        }
    }
}
