// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc.ModelBinding.Internal;
using Microsoft.Framework.DependencyInjection;

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    public class MutableObjectModelBinder : IModelBinder
    {
        public virtual async Task<bool> BindModelAsync(ModelBindingContext bindingContext)
        {
            ModelBindingHelper.ValidateBindingContext(bindingContext);
            if (!CanBindType(bindingContext.ModelType))
            {
                return false;
            }

            var mutableObjectBinderContext = new MutableObjectBinderContext()
            {
                ModelBindingContext = bindingContext,
                PropertyMetadata = GetMetadataForProperties(bindingContext),
            };

            if (!(await CanCreateModel(mutableObjectBinderContext)))
            {
                return false;
            }

            EnsureModel(bindingContext);
            var dto = await CreateAndPopulateDto(bindingContext, mutableObjectBinderContext.PropertyMetadata);

            // post-processing, e.g. property setters and hooking up validation
            ProcessDto(bindingContext, dto);
            // complex models require full validation
            bindingContext.ValidationNode.ValidateAllProperties = true;
            return true;
        }

        protected virtual bool CanUpdateProperty(ModelMetadata propertyMetadata)
        {
            return CanUpdatePropertyInternal(propertyMetadata);
        }

        internal async Task<bool> CanCreateModel(MutableObjectBinderContext context)
        {
            var bindingContext = context.ModelBindingContext;
            var isTopLevelObject = bindingContext.ModelMetadata.ContainerType == null;
            var hasExplicitAlias = bindingContext.ModelMetadata.BinderModelName != null;

            // The fact that this has reached here,
            // it is a complex object which was not directly bound by any previous model binders.
            // Check if this was supposed to be handled by a non value provider based binder.
            // if it was then it should be not be bound using mutable object binder.
            // This check would prevent it from recursing in if a model contains a property of its own type.
            // We skip this check if it is a top level object because we want to always evaluate
            // the creation of top level object (this is also required for ModelBinderAttribute to work.)
            if (!isTopLevelObject &&
                bindingContext.ModelMetadata.BinderMetadata != null &&
                !(bindingContext.ModelMetadata.BinderMetadata is IValueProviderMetadata))
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

            // 3. The model name is not prefixed and a value provider can directly provide a value for the model name.
            //    The fact that it is not prefixed means that the containsPrefixAsync call checks for the exact
            //    model name instead of doing a prefix match.
            if (!bindingContext.ModelName.Contains(".") &&
                await bindingContext.ValueProvider.ContainsPrefixAsync(bindingContext.ModelName))
            {
                return true;
            }

            // 4. Any of the model properties can be bound using a value provider.
            if (await CanValueBindAnyModelProperties(context))
            {
                return true;
            }

            return false;
        }

        private async Task<bool> CanValueBindAnyModelProperties(MutableObjectBinderContext context)
        {
            // We need to enumerate the non marked properties and properties marked with IValueProviderMetadata
            // instead of checking bindingContext.ValueProvider.ContainsPrefixAsync(bindingContext.ModelName)
            // because there can be a case where a value provider might be willing to provide a marked property,
            // which might never be bound.
            // For example if person.Name is marked with FromQuery, and FormValueProvider has a key person.Name,
            // and the QueryValueProvider does not, we do not want to create Person.
            var isAnyPropertyEnabledForValueProviderBasedBinding = false;
            foreach (var propertyMetadata in context.PropertyMetadata)
            {
                // This check will skip properties which are marked explicitly using a non value binder.
                if (propertyMetadata.BinderMetadata == null ||
                    propertyMetadata.BinderMetadata is IValueProviderMetadata)
                {
                    isAnyPropertyEnabledForValueProviderBasedBinding = true;

                    // If any property can return a true value.
                    if (await CanBindValue(context.ModelBindingContext, propertyMetadata))
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

        private async Task<bool> CanBindValue(ModelBindingContext bindingContext, ModelMetadata metadata)
        {
            var valueProvider = bindingContext.ValueProvider;
            var valueProviderMetadata = metadata.BinderMetadata as IValueProviderMetadata;
            if (valueProviderMetadata != null)
            {
                // if there is a binder metadata and since the property can be bound using a value provider.
                var metadataAwareValueProvider =
                    bindingContext.OperationBindingContext.ValueProvider as IMetadataAwareValueProvider;
                if (metadataAwareValueProvider != null)
                {
                    valueProvider = metadataAwareValueProvider.Filter(valueProviderMetadata);
                }
            }

            var propertyModelName = ModelBindingHelper.CreatePropertyModelName(bindingContext.ModelName,
                                                                               metadata.PropertyName);

            if (await valueProvider.ContainsPrefixAsync(propertyModelName))
            {
                return true;
            }

            return false;
        }

        private static bool CanBindType(Type modelType)
        {
            // Simple types cannot use this binder
            var isComplexType = !TypeHelper.HasStringConverter(modelType);
            if (!isComplexType)
            {
                return false;
            }

            if (modelType == typeof(ComplexModelDto))
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

        private async Task<ComplexModelDto> CreateAndPopulateDto(ModelBindingContext bindingContext,
                                                     IEnumerable<ModelMetadata> propertyMetadatas)
        {
            // create a DTO and call into the DTO binder
            var originalDto = new ComplexModelDto(bindingContext.ModelMetadata, propertyMetadatas);
            var complexModelDtoMetadata =
                bindingContext.OperationBindingContext.MetadataProvider.GetMetadataForType(() => originalDto,
                                                                                   typeof(ComplexModelDto));
            var dtoBindingContext =
                new ModelBindingContext(bindingContext, bindingContext.ModelName, complexModelDtoMetadata);

            await bindingContext.OperationBindingContext.ModelBinder.BindModelAsync(dtoBindingContext);
            return (ComplexModelDto)dtoBindingContext.Model;
        }

        protected virtual object CreateModel(ModelBindingContext bindingContext)
        {
            // If the Activator throws an exception, we want to propagate it back up the call stack, since the
            // application developer should know that this was an invalid type to try to bind to.
            return Activator.CreateInstance(bindingContext.ModelType);
        }

        // Called when the property setter null check failed, allows us to add our own error message to ModelState.
        internal static EventHandler<ModelValidatedEventArgs> CreateNullCheckFailedHandler(ModelMetadata modelMetadata,
                                                                                           object incomingValue)
        {
            return (sender, e) =>
            {
                var validationNode = (ModelValidationNode)sender;
                var modelState = e.ValidationContext.ModelState;
                var validationState = modelState.GetFieldValidationState(validationNode.ModelStateKey);

                if (validationState == ModelValidationState.Unvalidated)
                {
                    // TODO: https://github.com/aspnet/Mvc/issues/450 Revive ModelBinderConfig
                    // var errorMessage =  ModelBinderConfig.ValueRequiredErrorMessageProvider(e.ValidationContext,
                    //                                                                            modelMetadata,
                    //                                                                            incomingValue);
                    var errorMessage = Resources.ModelBinderConfig_ValueRequired;
                    if (errorMessage != null)
                    {
                        modelState.TryAddModelError(validationNode.ModelStateKey, errorMessage);
                    }
                }
            };
        }

        protected virtual void EnsureModel(ModelBindingContext bindingContext)
        {
            if (bindingContext.Model == null)
            {
                bindingContext.ModelMetadata.Model = CreateModel(bindingContext);
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

        private static object GetPropertyDefaultValue(PropertyInfo propertyInfo)
        {
            var attr = propertyInfo.GetCustomAttribute<DefaultValueAttribute>();
            return (attr != null) ? attr.Value : null;
        }

        internal static PropertyValidationInfo GetPropertyValidationInfo(ModelBindingContext bindingContext)
        {
            var validationInfo = new PropertyValidationInfo();
            var modelTypeInfo = bindingContext.ModelType.GetTypeInfo();
            var typeAttribute = modelTypeInfo.GetCustomAttribute<BindingBehaviorAttribute>();
            var properties = bindingContext.ModelType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach (var property in properties)
            {
                var propertyName = property.Name;
                var propertyMetadata = bindingContext.PropertyMetadata[propertyName];
                var requiredValidator = bindingContext.OperationBindingContext
                                                      .ValidatorProvider
                                                      .GetValidators(propertyMetadata)
                                                      .FirstOrDefault(v => v != null && v.IsRequired);
                if (requiredValidator != null)
                {
                    validationInfo.RequiredValidators[propertyName] = requiredValidator;
                }

                var propertyAttribute = property.GetCustomAttribute<BindingBehaviorAttribute>();
                var bindingBehaviorAttribute = propertyAttribute ?? typeAttribute;
                if (bindingBehaviorAttribute != null)
                {
                    switch (bindingBehaviorAttribute.Behavior)
                    {
                        case BindingBehavior.Required:
                            validationInfo.RequiredProperties.Add(propertyName);
                            break;

                        case BindingBehavior.Never:
                            validationInfo.SkipProperties.Add(propertyName);
                            break;
                    }
                }
                else if (requiredValidator != null)
                {
                    validationInfo.RequiredProperties.Add(propertyName);
                }
            }

            return validationInfo;
        }

        internal void ProcessDto(ModelBindingContext bindingContext, ComplexModelDto dto)
        {
            var validationInfo = GetPropertyValidationInfo(bindingContext);

            // Eliminate provided properties from requiredProperties; leaving just *missing* required properties.
            validationInfo.RequiredProperties.ExceptWith(dto.Results.Select(r => r.Key.PropertyName));

            foreach (var missingRequiredProperty in validationInfo.RequiredProperties)
            {
                var addedError = false;
                var modelStateKey = ModelBindingHelper.CreatePropertyModelName(
                    bindingContext.ValidationNode.ModelStateKey, missingRequiredProperty);

                // Update Model as SetProperty() would: Place null value where validator will check for non-null. This
                // ensures a failure result from a required validator (if any) even for a non-nullable property.
                // (Otherwise, propertyMetadata.Model is likely already null.)
                var propertyMetadata = bindingContext.PropertyMetadata[missingRequiredProperty];
                propertyMetadata.Model = null;

                // Execute validator (if any) to get custom error message.
                IModelValidator validator;
                if (validationInfo.RequiredValidators.TryGetValue(missingRequiredProperty, out validator))
                {
                    addedError = RunValidator(validator, bindingContext, propertyMetadata, modelStateKey);
                }

                // Fall back to default message if BindingBehaviorAttribute required this property or validator
                // (oddly) succeeded.
                if (!addedError)
                {
                    bindingContext.ModelState.TryAddModelError(
                        modelStateKey,
                        Resources.FormatMissingRequiredMember(missingRequiredProperty));
                }
            }

            // for each property that was bound, call the setter, recording exceptions as necessary
            foreach (var entry in dto.Results)
            {
                var propertyMetadata = entry.Key;
                var dtoResult = entry.Value;
                if (dtoResult != null)
                {
                    IModelValidator requiredValidator;
                    validationInfo.RequiredValidators.TryGetValue(propertyMetadata.PropertyName,
                                                                  out requiredValidator);
                    SetProperty(bindingContext, propertyMetadata, dtoResult, requiredValidator);
                    bindingContext.ValidationNode.ChildNodes.Add(dtoResult.ValidationNode);
                }
            }
        }

        protected virtual void SetProperty(ModelBindingContext bindingContext,
                                           ModelMetadata propertyMetadata,
                                           ComplexModelDtoResult dtoResult,
                                           IModelValidator requiredValidator)
        {
            var bindingFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase;
            var property = bindingContext.ModelType
                                         .GetProperty(propertyMetadata.PropertyName, bindingFlags);

            if (property == null || !property.CanWrite)
            {
                // nothing to do
                return;
            }

            var value = dtoResult.Model ?? GetPropertyDefaultValue(property);
            propertyMetadata.Model = value;

            // 'Required' validators need to run first so that we can provide useful error messages if
            // the property setters throw, e.g. if we're setting entity keys to null.
            if (value == null)
            {
                var modelStateKey = dtoResult.ValidationNode.ModelStateKey;
                var validationState = bindingContext.ModelState.GetFieldValidationState(modelStateKey);
                if (validationState == ModelValidationState.Unvalidated)
                {
                    if (requiredValidator != null)
                    {
                        var validationContext = new ModelValidationContext(bindingContext, propertyMetadata);
                        foreach (var validationResult in requiredValidator.Validate(validationContext))
                        {
                            bindingContext.ModelState.TryAddModelError(modelStateKey, validationResult.Message);
                        }
                    }
                }
            }

            if (value != null || property.PropertyType.AllowsNullValue())
            {
                try
                {
                    property.SetValue(bindingContext.Model, value);
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
                    var modelStateKey = dtoResult.ValidationNode.ModelStateKey;
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
                var modelStateKey = dtoResult.ValidationNode.ModelStateKey;
                var validationState = bindingContext.ModelState.GetFieldValidationState(modelStateKey);
                if (validationState == ModelValidationState.Unvalidated)
                {
                    dtoResult.ValidationNode.Validated += CreateNullCheckFailedHandler(propertyMetadata, value);
                }
            }
        }

        // Returns true if validator execution adds a model error.
        private static bool RunValidator(IModelValidator validator,
                                         ModelBindingContext bindingContext,
                                         ModelMetadata propertyMetadata,
                                         string modelStateKey)
        {
            var validationContext = new ModelValidationContext(bindingContext, propertyMetadata);

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
