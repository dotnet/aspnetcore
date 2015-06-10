// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
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
        public void Validate(
            [NotNull] ModelValidationContext modelValidationContext,
            [NotNull] ModelValidationNode validationNode)
        {
            var validationContext = new ValidationContext()
            {
                ModelValidationContext = modelValidationContext,
                Visited = new HashSet<object>(ReferenceEqualityComparer.Instance),
                ValidationNode = validationNode
            };

            ValidateNonVisitedNodeAndChildren(
                validationNode.Key,
                validationContext,
                validators: null);
        }

        private bool ValidateNonVisitedNodeAndChildren(
            string modelKey,
            ValidationContext validationContext,
            IList<IModelValidator> validators)
        {
            // Recursion guard to avoid stack overflows
            RuntimeHelpers.EnsureSufficientExecutionStack();

            var modelValidationContext = validationContext.ModelValidationContext;
            var modelExplorer = modelValidationContext.ModelExplorer;
            var modelState = modelValidationContext.ModelState;
            var currentValidationNode = validationContext.ValidationNode;
            if (currentValidationNode.SuppressValidation)
            {
                // Short circuit if the node is marked to be suppressed.
                // If there are any sub entries which were model bound, they need to be marked as skipped,
                // Otherwise they will remain as unvalidated and the model state would be Invalid.
                MarkChildNodesAsSkipped(modelKey, modelExplorer.Metadata, validationContext);

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
                // instead we just pass them over.
                validators = GetValidators(modelValidationContext.ValidatorProvider, modelExplorer.Metadata);
            }

            // We don't need to recursively traverse the graph if there are no child nodes.
            if (currentValidationNode.ChildNodes.Count == 0 && !currentValidationNode.ValidateAllProperties)
            {
                return ShallowValidate(modelKey, modelExplorer, validationContext, validators);
            }

            // We don't need to recursively traverse the graph for types that shouldn't be validated
            var modelType = modelExplorer.ModelType;
            if (IsTypeExcludedFromValidation(_excludeFilters, modelType))
            {
                var result = ShallowValidate(modelKey, modelExplorer, validationContext, validators);

                // If there are any sub entries which were model bound, they need to be marked as skipped,
                // Otherwise they will remain as unvalidated and the model state would be Invalid.
                MarkChildNodesAsSkipped(modelKey, modelExplorer.Metadata, validationContext);
                return result;
            }

            // Check to avoid infinite recursion. This can happen with cycles in an object graph.
            // Note that this is only applicable in case the model is pre-existing (like in case of TryUpdateModel).
            if (validationContext.Visited.Contains(modelExplorer.Model))
            {
                return true;
            }

            validationContext.Visited.Add(modelExplorer.Model);
            isValid = ValidateChildNodes(modelKey, modelExplorer, validationContext);
            if (isValid)
            {
                // Don't bother to validate this node if children failed.
                isValid = ShallowValidate(modelKey, modelExplorer, validationContext, validators);
            }

            // Pop the object so that it can be validated again in a different path
            validationContext.Visited.Remove(modelExplorer.Model);
            return isValid;
        }

        private void MarkChildNodesAsSkipped(string currentModelKey, ModelMetadata metadata, ValidationContext validationContext)
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

            // At this point we just want to mark all sub-entries present in the model state as skipped.
            var entries = modelState.FindKeysWithPrefix(currentModelKey);
            foreach (var entry in entries)
            {
                entry.Value.ValidationState = ModelValidationState.Skipped;
            }
        }

        private IList<IModelValidator> GetValidators(IModelValidatorProvider provider, ModelMetadata metadata)
        {
            var validatorProviderContext = new ModelValidatorProviderContext(metadata);
            provider.GetValidators(validatorProviderContext);

            return validatorProviderContext
                .Validators
                .OrderBy(v => v, ValidatorOrderComparer.Instance)
                .ToList();
        }

        private bool ValidateChildNodes(
            string currentModelKey,
            ModelExplorer modelExplorer,
            ValidationContext validationContext)
        {
            var isValid = true;
            ExpandValidationNode(validationContext, modelExplorer);

            IList<IModelValidator> validators = null;
            var elementMetadata = modelExplorer.Metadata.ElementMetadata;
            if (elementMetadata != null)
            {
                validators = GetValidators(validationContext.ModelValidationContext.ValidatorProvider, elementMetadata);
            }

            foreach (var childNode in validationContext.ValidationNode.ChildNodes)
            {
                var childModelExplorer = childNode.ModelMetadata.MetadataKind == Metadata.ModelMetadataKind.Type ?
                    _modelMetadataProvider.GetModelExplorerForType(childNode.ModelMetadata.ModelType, childNode.Model) :
                    modelExplorer.GetExplorerForProperty(childNode.ModelMetadata.PropertyName);

                var propertyValidationContext = new ValidationContext()
                {
                    ModelValidationContext = ModelValidationContext.GetChildValidationContext(
                        validationContext.ModelValidationContext,
                        childModelExplorer),
                    Visited = validationContext.Visited,
                    ValidationNode = childNode
                };

                if (!ValidateNonVisitedNodeAndChildren(
                        childNode.Key,
                        propertyValidationContext,
                        validators))
                {
                    isValid = false;
                }
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
                            var errorKey = ModelNames.CreatePropertyModelName(modelKey, error.MemberName);
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

            // Add an entry only if there was an entry which was added by a model binder.
            // This prevents adding spurious entries.
            if (modelState.ContainsKey(modelKey) && isValid)
            {
                validationContext.ModelValidationContext.ModelState.MarkFieldValid(modelKey);
            }

            return isValid;
        }

        private bool IsTypeExcludedFromValidation(IList<IExcludeTypeValidationFilter> filters, Type type)
        {
            return filters.Any(filter => filter.IsTypeExcluded(type));
        }

        private void ExpandValidationNode(ValidationContext context, ModelExplorer modelExplorer)
        {
            var validationNode = context.ValidationNode;
            if (validationNode.ChildNodes.Count != 0 ||
                !validationNode.ValidateAllProperties ||
                validationNode.Model == null)
            {
                return;
            }

            var elementMetadata = modelExplorer.Metadata.ElementMetadata;
            if (elementMetadata == null)
            {
                foreach (var property in validationNode.ModelMetadata.Properties)
                {
                    var propertyExplorer = modelExplorer.GetExplorerForProperty(property.PropertyName);
                    var propertyBindingName = property.BinderModelName ?? property.PropertyName;
                    var childKey = ModelNames.CreatePropertyModelName(validationNode.Key, propertyBindingName);
                    var childNode = new ModelValidationNode(childKey, property, propertyExplorer.Model)
                    {
                        ValidateAllProperties = true
                    };
                    validationNode.ChildNodes.Add(childNode);
                }
            }
            else
            {
                var enumerableModel = (IEnumerable)modelExplorer.Model;

                // An integer index is incorrect in scenarios where there is a custom index provided by the user.
                // However those scenarios are supported by createing a ModelValidationNode with the right keys.
                var index = 0;
                foreach (var element in enumerableModel)
                {
                    var elementExplorer = new ModelExplorer(_modelMetadataProvider, elementMetadata, element);
                    var elementKey = ModelNames.CreateIndexModelName(validationNode.Key, index);
                    var childNode = new ModelValidationNode(elementKey, elementMetadata, elementExplorer.Model)
                    {
                        ValidateAllProperties = true
                    };

                    validationNode.ChildNodes.Add(childNode);
                    index++;
                }
            }
        }

        private class ValidationContext
        {
            public ModelValidationContext ModelValidationContext { get; set; }

            public HashSet<object> Visited { get; set; }

            public ModelValidationNode ValidationNode { get; set; }
        }

        // Sorts validators based on whether or not they are 'required'. We want to run
        // 'required' validators first so that we get the best possible error message.
        private class ValidatorOrderComparer : IComparer<IModelValidator>
        {
            public static readonly ValidatorOrderComparer Instance = new ValidatorOrderComparer();

            public int Compare(IModelValidator x, IModelValidator y)
            {
                var xScore = x.IsRequired ? 0 : 1;
                var yScore = y.IsRequired ? 0 : 1;
                return xScore.CompareTo(yScore);
            }
        }
    }
}
