// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc.Core;
using Microsoft.AspNet.Mvc.ModelBinding.Validation;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    /// <summary>
    /// <see cref="IModelBinder"/> implementation for binding complex values.
    /// </summary>
    public class MutableObjectModelBinder : IModelBinder
    {
        private static readonly MethodInfo CallPropertyAddRangeOpenGenericMethod =
            typeof(MutableObjectModelBinder).GetTypeInfo().GetDeclaredMethod(nameof(CallPropertyAddRange));

        /// <inheritdoc />
        public Task<ModelBindingResult> BindModelAsync([NotNull] ModelBindingContext bindingContext)
        {
            ModelBindingHelper.ValidateBindingContext(bindingContext);
            if (!CanBindType(bindingContext.ModelMetadata))
            {
                return ModelBindingResult.NoResultAsync;
            }

            var mutableObjectBinderContext = new MutableObjectBinderContext()
            {
                ModelBindingContext = bindingContext,
                PropertyMetadata = GetMetadataForProperties(bindingContext).ToArray(),
            };

            if (!(CanCreateModel(mutableObjectBinderContext)))
            {
                return ModelBindingResult.NoResultAsync;
            }

            return BindModelCoreAsync(bindingContext, mutableObjectBinderContext);
        }

        private async Task<ModelBindingResult> BindModelCoreAsync(
            ModelBindingContext bindingContext,
            MutableObjectBinderContext mutableObjectBinderContext)
        {
            // Create model first (if necessary) to avoid reporting errors about properties when activation fails.
            var model = GetModel(bindingContext);

            var results = await BindPropertiesAsync(bindingContext, mutableObjectBinderContext.PropertyMetadata);

            // Post-processing e.g. property setters and hooking up validation.
            bindingContext.Model = model;
            ProcessResults(bindingContext, results);

            return ModelBindingResult.Success(bindingContext.ModelName, model);
        }

        /// <summary>
        /// Gets an indication whether a property with the given <paramref name="propertyMetadata"/> can be updated.
        /// </summary>
        /// <param name="propertyMetadata"><see cref="ModelMetadata"/> for the property of interest.</param>
        /// <returns><c>true</c> if the property can be updated; <c>false</c> otherwise.</returns>
        /// <remarks>Should return <c>true</c> only for properties <see cref="SetProperty"/> can update.</remarks>
        protected virtual bool CanUpdateProperty([NotNull] ModelMetadata propertyMetadata)
        {
            return CanUpdatePropertyInternal(propertyMetadata);
        }

        internal bool CanCreateModel(MutableObjectBinderContext context)
        {
            var bindingContext = context.ModelBindingContext;
            var isTopLevelObject = bindingContext.IsTopLevelObject;

            // If we get here the model is a complex object which was not directly bound by any previous model binder,
            // so we want to decide if we want to continue binding. This is important to get right to avoid infinite
            // recursion.
            //
            // First, we want to make sure this object is allowed to come from a value provider source as this binder
            // will always include value provider data. For instance if the model is marked with [FromBody], then we
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

            // 2. If it is top level object and there are no properties to bind
            if (isTopLevelObject && context.PropertyMetadata != null && context.PropertyMetadata.Count == 0)
            {
                return true;
            }

            // 3. Any of the model properties can be bound using a value provider.
            if (CanValueBindAnyModelProperties(context))
            {
                return true;
            }

            return false;
        }

        private bool CanValueBindAnyModelProperties(MutableObjectBinderContext context)
        {
            // If there are no properties on the model, there is nothing to bind. We are here means this is not a top
            // level object. So we return false.
            if (context.PropertyMetadata == null || context.PropertyMetadata.Count == 0)
            {
                return false;
            }

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

                    var fieldName = propertyMetadata.BinderModelName ?? propertyMetadata.PropertyName;
                    var modelName = ModelNames.CreatePropertyModelName(
                        context.ModelBindingContext.ModelName,
                        fieldName);

                    var propertyModelBindingContext = ModelBindingContext.CreateChildBindingContext(
                        context.ModelBindingContext,
                        propertyMetadata,
                        fieldName: fieldName,
                        modelName: modelName,
                        model: null);

                    // If any property can return a true value.
                    if (CanBindValue(propertyModelBindingContext))
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

        private bool CanBindValue(ModelBindingContext bindingContext)
        {
            var valueProvider = bindingContext.ValueProvider;

            var bindingSource = bindingContext.BindingSource;
            if (bindingSource != null && !bindingSource.IsGreedy)
            {
                var rootValueProvider =
                    bindingContext.OperationBindingContext.ValueProvider as IBindingSourceValueProvider;
                if (rootValueProvider != null)
                {
                    valueProvider = rootValueProvider.Filter(bindingSource);
                    if (valueProvider == null)
                    {
                        // Unable to find a value provider for this binding source. Binding will fail.
                        return false;
                    }
                }
            }

            if (valueProvider.ContainsPrefix(bindingContext.ModelName))
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

            if (modelMetadata.IsEnumerableType)
            {
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

        // Returned dictionary contains entries corresponding to properties against which binding was attempted. If
        // binding failed, the entry's value will have IsModelSet == false. Binding is attempted for all elements of
        // propertyMetadatas.
        private async Task<IDictionary<ModelMetadata, ModelBindingResult>> BindPropertiesAsync(
            ModelBindingContext bindingContext,
            IEnumerable<ModelMetadata> propertyMetadatas)
        {
            var results = new Dictionary<ModelMetadata, ModelBindingResult>();
            foreach (var propertyMetadata in propertyMetadatas)
            {
                // ModelBindingContext.Model property values may be non-null when invoked via TryUpdateModel(). Pass
                // complex (including collection) values down so that binding system does not unnecessarily recreate
                // instances or overwrite inner properties that are not bound. No need for this with simple values
                // because they will be overwritten if binding succeeds. Arrays are never reused because they cannot
                // be resized.
                object model = null;
                if (propertyMetadata.PropertyGetter != null &&
                    propertyMetadata.IsComplexType &&
                    !propertyMetadata.ModelType.IsArray)
                {
                    model = propertyMetadata.PropertyGetter(bindingContext.Model);
                }

                var fieldName = propertyMetadata.BinderModelName ?? propertyMetadata.PropertyName;
                var modelName = ModelNames.CreatePropertyModelName(bindingContext.ModelName, fieldName);

                var propertyContext = ModelBindingContext.CreateChildBindingContext(
                    bindingContext,
                    propertyMetadata,
                    fieldName: fieldName,
                    modelName: modelName,
                    model: model);

                var result = await bindingContext.OperationBindingContext.ModelBinder.BindModelAsync(propertyContext);
                if (result == ModelBindingResult.NoResult)
                {
                    // Could not bind. Let ProcessResult() know explicitly.
                    result = ModelBindingResult.Failed(propertyContext.ModelName);
                }

                results[propertyMetadata] = result;
            }

            return results;
        }

        /// <summary>
        /// Creates suitable <see cref="object"/> for given <paramref name="bindingContext"/>.
        /// </summary>
        /// <param name="bindingContext">The <see cref="ModelBindingContext"/>.</param>
        /// <returns>An <see cref="object"/> compatible with <see cref="ModelBindingContext.ModelType"/>.</returns>
        protected virtual object CreateModel([NotNull] ModelBindingContext bindingContext)
        {
            // If the Activator throws an exception, we want to propagate it back up the call stack, since the
            // application developer should know that this was an invalid type to try to bind to.
            return Activator.CreateInstance(bindingContext.ModelType);
        }

        /// <summary>
        /// Get <see cref="ModelBindingContext.Model"/> if that property is not <c>null</c>. Otherwise activate a
        /// new instance of <see cref="ModelBindingContext.ModelType"/>.
        /// </summary>
        /// <param name="bindingContext">The <see cref="ModelBindingContext"/>.</param>
        protected virtual object GetModel([NotNull] ModelBindingContext bindingContext)
        {
            if (bindingContext.Model != null)
            {
                return bindingContext.Model;
            }

            return CreateModel(bindingContext);
        }

        /// <summary>
        /// Gets the collection of <see cref="ModelMetadata"/> for properties this binder should update.
        /// </summary>
        /// <param name="bindingContext">The <see cref="ModelBindingContext"/>.</param>
        /// <returns>Collection of <see cref="ModelMetadata"/> for properties this binder should update.</returns>
        protected virtual IEnumerable<ModelMetadata> GetMetadataForProperties(
            [NotNull] ModelBindingContext bindingContext)
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
                    (context.PropertyFilter == null || context.PropertyFilter(context, propertyName)) &&
                    (modelMetadataPredicate == null || modelMetadataPredicate(context, propertyName));
            };
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
            }

            return validationInfo;
        }

        // Internal for testing.
        internal void ProcessResults(
            ModelBindingContext bindingContext,
            IDictionary<ModelMetadata, ModelBindingResult> results)
        {
            var metadataProvider = bindingContext.OperationBindingContext.MetadataProvider;
            var modelExplorer =
                metadataProvider.GetModelExplorerForType(bindingContext.ModelType, bindingContext.Model);
            var validationInfo = GetPropertyValidationInfo(bindingContext);

            // Eliminate provided properties from RequiredProperties; leaving just *missing* required properties.
            var boundProperties = results.Where(p => p.Value.IsModelSet).Select(p => p.Key.PropertyName);
            validationInfo.RequiredProperties.ExceptWith(boundProperties);

            foreach (var missingRequiredProperty in validationInfo.RequiredProperties)
            {
                var propertyExplorer = modelExplorer.GetExplorerForProperty(missingRequiredProperty);
                var propertyName = propertyExplorer.Metadata.BinderModelName ?? missingRequiredProperty;
                var modelStateKey = ModelNames.CreatePropertyModelName(bindingContext.ModelName, propertyName);

                bindingContext.ModelState.TryAddModelError(
                    modelStateKey,
                    Resources.FormatModelBinding_MissingBindRequiredMember(propertyName));
            }

            // For each property that BindPropertiesAsync() attempted to bind, call the setter, recording
            // exceptions as necessary.
            foreach (var entry in results)
            {
                if (entry.Value != ModelBindingResult.NoResult)
                {
                    var result = entry.Value;
                    var propertyMetadata = entry.Key;
                    SetProperty(bindingContext, modelExplorer, propertyMetadata, result);
                }
            }
        }

        /// <summary>
        /// Updates a property in the current <see cref="ModelBindingContext.Model"/>.
        /// </summary>
        /// <param name="bindingContext">The <see cref="ModelBindingContext"/>.</param>
        /// <param name="modelExplorer">
        /// The <see cref="ModelExplorer"/> for the model containing property to set.
        /// </param>
        /// <param name="propertyMetadata">The <see cref="ModelMetadata"/> for the property to set.</param>
        /// <param name="result">The <see cref="ModelBindingResult"/> for the property's new value.</param>
        /// <remarks>Should succeed in all cases that <see cref="CanUpdateProperty"/> returns <c>true</c>.</remarks>
        protected virtual void SetProperty(
            [NotNull] ModelBindingContext bindingContext,
            [NotNull] ModelExplorer modelExplorer,
            [NotNull] ModelMetadata propertyMetadata,
            [NotNull] ModelBindingResult result)
        {
            var bindingFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase;
            var property = bindingContext.ModelType.GetProperty(propertyMetadata.PropertyName, bindingFlags);

            if (property == null)
            {
                // Nothing to do if property does not exist.
                return;
            }

            if (!result.IsModelSet)
            {
                // If we don't have a value, don't set it on the model and trounce a pre-initialized value.
                return;
            }

            if (!property.CanWrite)
            {
                // Try to handle as a collection if property exists but is not settable.
                AddToProperty(bindingContext, modelExplorer, property, result);
                return;
            }

            var value = result.Model;
            try
            {
                propertyMetadata.PropertySetter(bindingContext.Model, value);
            }
            catch (Exception exception)
            {
                AddModelError(exception, bindingContext, result);
            }
        }

        private void AddToProperty(
            ModelBindingContext bindingContext,
            ModelExplorer modelExplorer,
            PropertyInfo property,
            ModelBindingResult result)
        {
            var propertyExplorer = modelExplorer.GetExplorerForProperty(property.Name);

            var target = propertyExplorer.Model;
            var source = result.Model;
            if (target == null || source == null)
            {
                // Cannot copy to or from a null collection.
                return;
            }

            if (target == source)
            {
                // Added to the target collection in BindPropertiesAsync().
                return;
            }

            // Determine T if this is an ICollection<T> property. No need for a T[] case because CanUpdateProperty()
            // ensures property is either settable or not an array. Underlying assumption is that CanUpdateProperty()
            // and SetProperty() are overridden together.
            if (!propertyExplorer.Metadata.IsCollectionType)
            {
                // Not a collection model.
                return;
            }

            var propertyAddRange = CallPropertyAddRangeOpenGenericMethod.MakeGenericMethod(
                propertyExplorer.Metadata.ElementMetadata.ModelType);
            try
            {
                propertyAddRange.Invoke(obj: null, parameters: new[] { target, source });
            }
            catch (Exception exception)
            {
                AddModelError(exception, bindingContext, result);
            }
        }

        // Called via reflection.
        private static void CallPropertyAddRange<TElement>(object target, object source)
        {
            var targetCollection = (ICollection<TElement>)target;
            var sourceCollection = source as IEnumerable<TElement>;
            if (sourceCollection != null && !targetCollection.IsReadOnly)
            {
                targetCollection.Clear();
                foreach (var item in sourceCollection)
                {
                    targetCollection.Add(item);
                }
            }
        }

        private static void AddModelError(
            Exception exception,
            ModelBindingContext bindingContext,
            ModelBindingResult result)
        {
            var targetInvocationException = exception as TargetInvocationException;
            if (targetInvocationException != null && targetInvocationException.InnerException != null)
            {
                exception = targetInvocationException.InnerException;
            }

            // Do not add an error message if a binding error has already occurred for this property.
            var modelState = bindingContext.ModelState;
            var modelStateKey = result.Key;
            var validationState = modelState.GetFieldValidationState(modelStateKey);
            if (validationState == ModelValidationState.Unvalidated)
            {
                modelState.AddModelError(modelStateKey, exception);
            }
        }

        internal sealed class PropertyValidationInfo
        {
            public PropertyValidationInfo()
            {
                RequiredProperties = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                SkipProperties = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            }

            public HashSet<string> RequiredProperties { get; private set; }

            public HashSet<string> SkipProperties { get; private set; }
        }
    }
}
