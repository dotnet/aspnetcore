// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Mvc.Internal;
using Microsoft.AspNetCore.Mvc.ModelBinding.Internal;

namespace Microsoft.AspNetCore.Mvc.ModelBinding.Validation
{
    /// <summary>
    /// A visitor implementation that interprets <see cref="ValidationStateDictionary"/> to traverse
    /// a model object graph and perform validation.
    /// </summary>
    public class ValidationVisitor
    {
        private readonly IModelValidatorProvider _validatorProvider;
        private readonly IModelMetadataProvider _metadataProvider;
        private readonly ValidatorCache _validatorCache;
        private readonly ActionContext _actionContext;
        private readonly ModelStateDictionary _modelState;
        private readonly ValidationStateDictionary _validationState;
        private readonly ValidationStack _currentPath;

        private object _container;
        private string _key;
        private object _model;
        private ModelMetadata _metadata;
        private IValidationStrategy _strategy;

        /// <summary>
        /// Creates a new <see cref="ValidationVisitor"/>.
        /// </summary>
        /// <param name="actionContext">The <see cref="ActionContext"/> associated with the current request.</param>
        /// <param name="validatorProvider">The <see cref="IModelValidatorProvider"/>.</param>
        /// <param name="validatorCache">The <see cref="ValidatorCache"/> that provides a list of <see cref="IModelValidator"/>s.</param>
        /// <param name="metadataProvider">The provider used for reading metadata for the model type.</param>
        /// <param name="validationState">The <see cref="ValidationStateDictionary"/>.</param>
        public ValidationVisitor(
            ActionContext actionContext,
            IModelValidatorProvider validatorProvider,
            ValidatorCache validatorCache,
            IModelMetadataProvider metadataProvider,
            ValidationStateDictionary validationState)
        {
            if (actionContext == null)
            {
                throw new ArgumentNullException(nameof(actionContext));
            }

            if (validatorProvider == null)
            {
                throw new ArgumentNullException(nameof(validatorProvider));
            }

            if (validatorCache == null)
            {
                throw new ArgumentNullException(nameof(validatorCache));
            }

            _actionContext = actionContext;
            _validatorProvider = validatorProvider;
            _validatorCache = validatorCache;

            _metadataProvider = metadataProvider;
            _validationState = validationState;

            _modelState = actionContext.ModelState;
            _currentPath = new ValidationStack();
        }

        /// <summary>
        /// Validates a object.
        /// </summary>
        /// <param name="metadata">The <see cref="ModelMetadata"/> associated with the model.</param>
        /// <param name="key">The model prefix key.</param>
        /// <param name="model">The model object.</param>
        /// <returns><c>true</c> if the object is valid, otherwise <c>false</c>.</returns>
        public bool Validate(ModelMetadata metadata, string key, object model)
        {
            if (model == null && key != null)
            {
                var entry = _modelState[key];
                if (entry != null && entry.ValidationState != ModelValidationState.Valid)
                {
                    entry.ValidationState = ModelValidationState.Valid;
                }

                return true;
            }

            return Visit(metadata, key, model);
        }

        /// <summary>
        /// Validates a single node in a model object graph.
        /// </summary>
        /// <returns><c>true</c> if the node is valid, otherwise <c>false</c>.</returns>
        protected virtual bool ValidateNode()
        {
            var state = _modelState.GetValidationState(_key);

            // Rationale: we might see the same model state key used for two different objects.
            // We want to run validation unless it's already known that this key is invalid.
            if (state != ModelValidationState.Invalid)
            {
                var validators = _validatorCache.GetValidators(_metadata, _validatorProvider);

                var count = validators.Count;
                if (count > 0)
                {
                    var context = new ModelValidationContext(
                        _actionContext,
                        _metadata,
                        _metadataProvider,
                        _container,
                        _model);

                    var results = new List<ModelValidationResult>();
                    for (var i = 0; i < count; i++)
                    {
                        results.AddRange(validators[i].Validate(context));
                    }

                    var resultsCount = results.Count;
                    for (var i = 0; i < resultsCount; i++)
                    {
                        var result = results[i];
                        var key = ModelNames.CreatePropertyModelName(_key, result.MemberName);
                        _modelState.TryAddModelError(key, result.Message);
                    }
                }
            }

            state = _modelState.GetFieldValidationState(_key);
            if (state == ModelValidationState.Invalid)
            {
                return false;
            }
            else
            {
                // If the field has an entry in ModelState, then record it as valid. Don't create
                // extra entries if they don't exist already.
                var entry = _modelState[_key];
                if (entry != null)
                {
                    entry.ValidationState = ModelValidationState.Valid;
                }

                return true;
            }
        }

        private bool Visit(ModelMetadata metadata, string key, object model)
        {
            RuntimeHelpers.EnsureSufficientExecutionStack();

            if (model != null && !_currentPath.Push(model))
            {
                // This is a cycle, bail.
                return true;
            }

            var entry = GetValidationEntry(model);
            key = entry?.Key ?? key ?? string.Empty;
            metadata = entry?.Metadata ?? metadata;
            var strategy = entry?.Strategy;

            if (_modelState.HasReachedMaxErrors)
            {
                SuppressValidation(key);
                return false;
            }
            else if (entry != null && entry.SuppressValidation)
            {
                // Use the key on the entry, because we might not have entries in model state.
                SuppressValidation(entry.Key);
                _currentPath.Pop(model);
                return true;
            }

            using (StateManager.Recurse(this, key ?? string.Empty, metadata, model, strategy))
            {
                if (_metadata.IsEnumerableType)
                {
                    return VisitComplexType(DefaultCollectionValidationStrategy.Instance);
                }

                if (_metadata.IsComplexType)
                {
                    return VisitComplexType(DefaultComplexObjectValidationStrategy.Instance);
                }

                return VisitSimpleType();
            }
        }

        // Covers everything VisitSimpleType does not i.e. both enumerations and complex types.
        private bool VisitComplexType(IValidationStrategy defaultStrategy)
        {
            var isValid = true;

            if (_model != null && _metadata.ValidateChildren)
            {
                var strategy = _strategy ?? defaultStrategy;
                isValid = VisitChildren(strategy);
            }
            else if (_model != null)
            {
                // Suppress validation for the entries matching this prefix. This will temporarily set
                // the current node to 'skipped' but we're going to visit it right away, so subsequent
                // code will set it to 'valid' or 'invalid'
                SuppressValidation(_key);
            }

            // Double-checking HasReachedMaxErrors just in case this model has no properties.
            if (isValid && !_modelState.HasReachedMaxErrors)
            {
                isValid &= ValidateNode();
            }

            return isValid;
        }

        private bool VisitSimpleType()
        {
            if (_modelState.HasReachedMaxErrors)
            {
                SuppressValidation(_key);
                return false;
            }

            return ValidateNode();
        }

        private bool VisitChildren(IValidationStrategy strategy)
        {
            var isValid = true;
            var enumerator = strategy.GetChildren(_metadata, _key, _model);
            var parentEntry = new ValidationEntry(_metadata, _key, _model);

            while (enumerator.MoveNext())
            {
                var entry = enumerator.Current;
                var metadata = entry.Metadata;
                var key = entry.Key;
                if (metadata.PropertyValidationFilter?.ShouldValidateEntry(entry, parentEntry) == false)
                {
                    SuppressValidation(key);
                    continue;
                }

                isValid &= Visit(metadata, key, entry.Model);
            }

            return isValid;
        }

        private void SuppressValidation(string key)
        {
            if (key == null)
            {
                // If the key is null, that means that we shouldn't expect any entries in ModelState for
                // this value, so there's nothing to do.
                return;
            }

            var entries = _modelState.FindKeysWithPrefix(key);
            foreach (var entry in entries)
            {
                entry.Value.ValidationState = ModelValidationState.Skipped;
            }
        }

        private ValidationStateEntry GetValidationEntry(object model)
        {
            if (model == null || _validationState == null)
            {
                return null;
            }

            ValidationStateEntry entry;
            _validationState.TryGetValue(model, out entry);
            return entry;
        }

        private struct StateManager : IDisposable
        {
            private readonly ValidationVisitor _visitor;
            private readonly object _container;
            private readonly string _key;
            private readonly ModelMetadata _metadata;
            private readonly object _model;
            private readonly object _newModel;
            private readonly IValidationStrategy _strategy;

            public static StateManager Recurse(
                ValidationVisitor visitor,
                string key,
                ModelMetadata metadata,
                object model,
                IValidationStrategy strategy)
            {
                var recursifier = new StateManager(visitor, model);

                visitor._container = visitor._model;
                visitor._key = key;
                visitor._metadata = metadata;
                visitor._model = model;
                visitor._strategy = strategy;

                return recursifier;
            }

            public StateManager(ValidationVisitor visitor, object newModel)
            {
                _visitor = visitor;
                _newModel = newModel;

                _container = _visitor._container;
                _key = _visitor._key;
                _metadata = _visitor._metadata;
                _model = _visitor._model;
                _strategy = _visitor._strategy;
            }

            public void Dispose()
            {
                _visitor._container = _container;
                _visitor._key = _key;
                _visitor._metadata = _metadata;
                _visitor._model = _model;
                _visitor._strategy = _strategy;

                _visitor._currentPath.Pop(_newModel);
            }
        }
    }
}
