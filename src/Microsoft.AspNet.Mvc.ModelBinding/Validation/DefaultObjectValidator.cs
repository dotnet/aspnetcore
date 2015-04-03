// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.AspNet.Mvc.ModelBinding.Internal;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Mvc.ModelBinding.Validation
{
    /// <summary>
    /// Recursively validate an object.
    /// </summary>
    public class DefaultObjectValidator : IObjectModelValidator
    {
        private readonly IList<IExcludeTypeValidationFilter> _excludeFilters;
        private readonly IModelMetadataProvider _modelMetadataProvider;

        /// <summary>
        /// Initializes a new instance of <see cref="DefaultObjectValidator"/>.
        /// </summary>
        /// <param name="excludeFilters"><see cref="IExcludeTypeValidationFilter"/>s that determine
        /// types to exclude from validation.</param>
        /// <param name="modelMetadataProvider">The <see cref="IModelMetadataProvider"/>.</param>
        public DefaultObjectValidator(
            [NotNull] IList<IExcludeTypeValidationFilter> excludeFilters,
            [NotNull] IModelMetadataProvider modelMetadataProvider)
        {
            _modelMetadataProvider = modelMetadataProvider;
            _excludeFilters = excludeFilters;
        }

        /// <inheritdoc />
        public void Validate([NotNull] ModelValidationContext modelValidationContext)
        {
            var validationContext = new ValidationContext()
            {
                ModelValidationContext = modelValidationContext,
                Visited = new HashSet<object>(ReferenceEqualityComparer.Instance),
            };

            ValidateNonVisitedNodeAndChildren(
                modelValidationContext.RootPrefix,
                validationContext,
                validators: null);
        }

        private bool ValidateNonVisitedNodeAndChildren(
            string modelKey,
            ValidationContext validationContext,
            IList<IModelValidator> validators)
        {
            var modelValidationContext = validationContext.ModelValidationContext;
            var modelExplorer = modelValidationContext.ModelExplorer;

            // Recursion guard to avoid stack overflows
            RuntimeHelpers.EnsureSufficientExecutionStack();

            var modelState = modelValidationContext.ModelState;

            var bindingSource = modelValidationContext.BindingSource;
            if (bindingSource != null && !bindingSource.IsFromRequest)
            {
                // Short circuit if the metadata represents something that was not bound using request data.
                // For example model bound using [FromServices]. Treat such objects as skipped.
                var validationState = modelState.GetFieldValidationState(modelKey);
                if (validationState == ModelValidationState.Unvalidated)
                {
                    modelValidationContext.ModelState.MarkFieldSkipped(modelKey);
                }

                // For validation purposes this model is valid.
                return true;
            }

            if (modelState.HasReachedMaxErrors)
            {
                // Short circuit if max errors have been recorded. In which case we treat this as invalid.
                return false;
            }

            var isValid = true;
            if (validators == null)
            {
                // The validators are not null in the case of validating an array. Since the validators are
                // the same for all the elements of the array, we do not do GetValidators for each element,
                // instead we just pass them over. See ValidateElements function.
                var validatorProvider = modelValidationContext.ValidatorProvider;
                var validatorProviderContext = new ModelValidatorProviderContext(modelExplorer.Metadata);
                validatorProvider.GetValidators(validatorProviderContext);

                validators = validatorProviderContext.Validators;
            }

            // We don't need to recursively traverse the graph for null values
            if (modelExplorer.Model == null)
            {
                return ShallowValidate(modelKey, modelExplorer, validationContext, validators);
            }

            // We don't need to recursively traverse the graph for types that shouldn't be validated
            var modelType = modelExplorer.Model.GetType();
            if (IsTypeExcludedFromValidation(_excludeFilters, modelType))
            {
                var result = ShallowValidate(modelKey, modelExplorer, validationContext, validators);
                MarkPropertiesAsSkipped(modelKey, modelExplorer.Metadata, validationContext);
                return result;
            }

            // Check to avoid infinite recursion. This can happen with cycles in an object graph.
            if (validationContext.Visited.Contains(modelExplorer.Model))
            {
                return true;
            }

            validationContext.Visited.Add(modelExplorer.Model);

            // Validate the children first - depth-first traversal
            var enumerableModel = modelExplorer.Model as IEnumerable;
            if (enumerableModel == null)
            {
                isValid = ValidateProperties(modelKey, modelExplorer, validationContext);
            }
            else
            {
                isValid = ValidateElements(modelKey, enumerableModel, validationContext);
            }

            if (isValid)
            {
                // Don't bother to validate this node if children failed.
                isValid = ShallowValidate(modelKey, modelExplorer, validationContext, validators);
            }

            // Pop the object so that it can be validated again in a different path
            validationContext.Visited.Remove(modelExplorer.Model);
            return isValid;
        }

        private void MarkPropertiesAsSkipped(string currentModelKey, ModelMetadata metadata, ValidationContext validationContext)
        {
            var modelState = validationContext.ModelValidationContext.ModelState;
            var fieldValidationState = modelState.GetFieldValidationState(currentModelKey);

            // Since shallow validation is done, if the modelvalidation state is still marked as unvalidated,
            // it is because some properties in the subtree are marked as unvalidated. Mark all such properties
            // as skipped. Models which have their subtrees as Valid or Invalid do not need to be marked as skipped.
            if (fieldValidationState != ModelValidationState.Unvalidated)
            {
                return;
            }

            foreach (var childMetadata in metadata.Properties)
            {
                var childKey = ModelBindingHelper.CreatePropertyModelName(currentModelKey, childMetadata.PropertyName);
                var validationState = modelState.GetFieldValidationState(childKey);

                if (validationState == ModelValidationState.Unvalidated)
                {
                    validationContext.ModelValidationContext.ModelState.MarkFieldSkipped(childKey);
                }
            }
        }

        private bool ValidateProperties(
            string currentModelKey,
            ModelExplorer modelExplorer,
            ValidationContext validationContext)
        {
            var isValid = true;

            foreach (var property in modelExplorer.Metadata.Properties)
            {
                var propertyExplorer = modelExplorer.GetExplorerForProperty(property.PropertyName);
                var propertyMetadata = propertyExplorer.Metadata;
                var propertyValidationContext = new ValidationContext()
                {
                    ModelValidationContext = ModelValidationContext.GetChildValidationContext(
                        validationContext.ModelValidationContext,
                        propertyExplorer),
                    Visited = validationContext.Visited
                };

                var propertyBindingName = propertyMetadata.BinderModelName ?? propertyMetadata.PropertyName;
                var childKey = ModelBindingHelper.CreatePropertyModelName(currentModelKey, propertyBindingName);

                if (!ValidateNonVisitedNodeAndChildren(
                    childKey,
                    propertyValidationContext,
                    validators: null))
                {
                    isValid = false;
                }
            }

            return isValid;
        }

        private bool ValidateElements(string currentKey, IEnumerable model, ValidationContext validationContext)
        {
            var elementType = GetElementType(model.GetType());
            var elementMetadata = _modelMetadataProvider.GetMetadataForType(elementType);

            var validatorProvider = validationContext.ModelValidationContext.ValidatorProvider;
            var validatorProviderContext = new ModelValidatorProviderContext(elementMetadata);
            validatorProvider.GetValidators(validatorProviderContext);

            var validators = validatorProviderContext.Validators;

            // If there are no validators or the object is null we bail out quickly
            // when there are large arrays of null, this will save a significant amount of processing
            // with minimal impact to other scenarios.
            var anyValidatorsDefined = validators.Any();
            var index = 0;
            var isValid = true;
            foreach (var element in model)
            {
                // If the element is non null, the recursive calls might find more validators.
                // If it's null, then a shallow validation will be performed.
                if (element != null || anyValidatorsDefined)
                {
                    var elementExplorer = new ModelExplorer(_modelMetadataProvider, elementMetadata, element);
                    var elementKey = ModelBindingHelper.CreateIndexModelName(currentKey, index);
                    var elementValidationContext = new ValidationContext()
                    {
                        ModelValidationContext = ModelValidationContext.GetChildValidationContext(
                            validationContext.ModelValidationContext,
                            elementExplorer),
                        Visited = validationContext.Visited
                    };

                    if (!ValidateNonVisitedNodeAndChildren(elementKey, elementValidationContext, validators))
                    {
                        isValid = false;
                    }
                }

                index++;
            }

            return isValid;
        }

        // Validates a single node (not including children)
        // Returns true if validation passes successfully
        private static bool ShallowValidate(
            string modelKey,
            ModelExplorer modelExplorer,
            ValidationContext validationContext,
            IList<IModelValidator> validators)
        {
            var isValid = true;

            var modelState = validationContext.ModelValidationContext.ModelState;
            var fieldValidationState = modelState.GetFieldValidationState(modelKey);
            if (fieldValidationState == ModelValidationState.Invalid)
            {
                // Even if we have no validators it's possible that model binding may have added a
                // validation error (conversion error, missing data). We want to still run
                // validators even if that's the case.
                isValid = false;
            }

            // When the are no validators we bail quickly. This saves a GetEnumerator allocation.
            // In a large array (tens of thousands or more) scenario it's very significant.
            if (validators == null || validators.Count > 0)
            {
                var modelValidationContext = ModelValidationContext.GetChildValidationContext(
                    validationContext.ModelValidationContext,
                    modelExplorer);

                var modelValidationState = modelState.GetValidationState(modelKey);

                // If either the model or its properties are unvalidated, validate them now.
                if (modelValidationState == ModelValidationState.Unvalidated ||
                    fieldValidationState == ModelValidationState.Unvalidated)
                {
                    foreach (var validator in validators)
                    {
                        foreach (var error in validator.Validate(modelValidationContext))
                        {
                            var errorKey = ModelBindingHelper.CreatePropertyModelName(modelKey, error.MemberName);
                            if (!modelState.TryAddModelError(errorKey, error.Message) &&
                                modelState.GetFieldValidationState(errorKey) == ModelValidationState.Unvalidated)
                            {

                                // If we are not able to add a model error 
                                // for instance when the max error count is reached, mark the model as skipped. 
                                modelState.MarkFieldSkipped(errorKey);
                            }

                            isValid = false;
                        }
                    }
                }
            }

            if (isValid)
            {
                validationContext.ModelValidationContext.ModelState.MarkFieldValid(modelKey);
            }

            return isValid;
        }

        private bool IsTypeExcludedFromValidation(IList<IExcludeTypeValidationFilter> filters, Type type)
        {
            return filters.Any(filter => filter.IsTypeExcluded(type));
        }

        private static Type GetElementType(Type type)
        {
            Debug.Assert(typeof(IEnumerable).IsAssignableFrom(type));
            if (type.IsArray)
            {
                return type.GetElementType();
            }

            foreach (var implementedInterface in type.GetInterfaces())
            {
                if (implementedInterface.IsGenericType() &&
                    implementedInterface.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                {
                    return implementedInterface.GetGenericArguments()[0];
                }
            }

            return typeof(object);
        }

        private class ValidationContext
        {
            public ModelValidationContext ModelValidationContext { get; set; }

            public HashSet<object> Visited { get; set; }
        }
    }
}
