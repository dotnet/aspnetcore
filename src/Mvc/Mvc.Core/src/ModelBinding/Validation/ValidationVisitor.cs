// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Mvc.Core;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;

namespace Microsoft.AspNetCore.Mvc.ModelBinding.Validation
{
    /// <summary>
    /// A visitor implementation that interprets <see cref="ValidationStateDictionary"/> to traverse
    /// a model object graph and perform validation.
    /// </summary>
    public class ValidationVisitor
    {
        private readonly ValidationStack _currentPath;
        private int? _maxValidationDepth;

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

            Context = actionContext;
            ValidatorProvider = validatorProvider;
            Cache = validatorCache;

            MetadataProvider = metadataProvider;
            ValidationState = validationState;

            ModelState = actionContext.ModelState;
            _currentPath = new ValidationStack();
        }

        protected IModelValidatorProvider ValidatorProvider { get; }
        protected IModelMetadataProvider MetadataProvider { get; }

        protected ValidatorCache Cache { get; }
        protected ActionContext Context { get; }
        protected ModelStateDictionary ModelState { get; }
        protected ValidationStateDictionary ValidationState { get; }

        protected object Container { get; set; }
        protected string Key { get; set; }
        protected object Model { get; set; }
        protected ModelMetadata Metadata { get; set; }
        protected IValidationStrategy Strategy { get; set; }

        /// <summary>
        /// Gets or sets the maximum depth to constrain the validation visitor when validating.
        /// <para>
        /// <see cref="ValidationVisitor"/> traverses the object graph of the model being validated. For models
        /// that are very deep or are infinitely recursive, validation may result in stack overflow.
        /// </para>
        /// <para>
        /// When not <see langword="null"/>, <see cref="Visit(ModelMetadata, string, object)"/> will throw if
        /// current traversal depth exceeds the specified value.
        /// </para>
        /// </summary>
        public int? MaxValidationDepth
        {
            get => _maxValidationDepth;
            set
            {
                if (value != null && value <= 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(value));
                }

                _maxValidationDepth = value;
            }
        }

        /// <summary>
        /// Indicates whether validation of a complex type should be performed if validation fails for any of its children. The default behavior is false.
        /// </summary>
        public bool ValidateComplexTypesIfChildValidationFails { get; set; }

        /// <summary>
        ///  Gets or sets a value that determines if <see cref="ValidationVisitor"/> can short circuit validation when a model
        ///  does not have any associated validators.
        /// </summary>
        /// <value>The default value is <see langword="true"/>.</value>
        /// <remarks>This property is currently ignored.</remarks>
        [Obsolete("This property is deprecated and is no longer used by the runtime.")]
        public bool AllowShortCircuitingValidationWhenNoValidatorsArePresent { get; set; } = true;

        /// <summary>
        /// Validates a object.
        /// </summary>
        /// <param name="metadata">The <see cref="ModelMetadata"/> associated with the model.</param>
        /// <param name="key">The model prefix key.</param>
        /// <param name="model">The model object.</param>
        /// <returns><c>true</c> if the object is valid, otherwise <c>false</c>.</returns>
        public bool Validate(ModelMetadata metadata, string key, object model)
        {
            return Validate(metadata, key, model, alwaysValidateAtTopLevel: false);
        }

        /// <summary>
        /// Validates a object.
        /// </summary>
        /// <param name="metadata">The <see cref="ModelMetadata"/> associated with the model.</param>
        /// <param name="key">The model prefix key.</param>
        /// <param name="model">The model object.</param>
        /// <param name="alwaysValidateAtTopLevel">If <c>true</c>, applies validation rules even if the top-level value is <c>null</c>.</param>
        /// <returns><c>true</c> if the object is valid, otherwise <c>false</c>.</returns>
        public virtual bool Validate(ModelMetadata metadata, string key, object model, bool alwaysValidateAtTopLevel)
        {
            if (model == null && key != null && !alwaysValidateAtTopLevel)
            {
                var entry = ModelState[key];

                // Rationale: We might see the same model state key for two different objects and want to preserve any
                // known invalidity.
                if (entry != null && entry.ValidationState != ModelValidationState.Invalid)
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
            var state = ModelState.GetValidationState(Key);

            // Rationale: we might see the same model state key used for two different objects.
            // We want to run validation unless it's already known that this key is invalid.
            if (state != ModelValidationState.Invalid)
            {
                var validators = Cache.GetValidators(Metadata, ValidatorProvider);

                var count = validators.Count;
                if (count > 0)
                {
                    var context = new ModelValidationContext(
                        Context,
                        Metadata,
                        MetadataProvider,
                        Container,
                        Model);

                    var results = new List<ModelValidationResult>();
                    for (var i = 0; i < count; i++)
                    {
                        results.AddRange(validators[i].Validate(context));
                    }

                    var resultsCount = results.Count;
                    for (var i = 0; i < resultsCount; i++)
                    {
                        var result = results[i];
                        var key = ModelNames.CreatePropertyModelName(Key, result.MemberName);

                        // It's OK for key to be the empty string here. This can happen when a top
                        // level object implements IValidatableObject.
                        ModelState.TryAddModelError(key, result.Message);
                    }
                }
            }

            state = ModelState.GetFieldValidationState(Key);
            if (state == ModelValidationState.Invalid)
            {
                return false;
            }
            else
            {
                // If the field has an entry in ModelState, then record it as valid. Don't create
                // extra entries if they don't exist already.
                var entry = ModelState[Key];
                if (entry != null)
                {
                    entry.ValidationState = ModelValidationState.Valid;
                }

                return true;
            }
        }

        protected virtual bool Visit(ModelMetadata metadata, string key, object model)
        {
            RuntimeHelpers.EnsureSufficientExecutionStack();

            if (model != null && !_currentPath.Push(model))
            {
                // This is a cycle, bail.
                return true;
            }

            if (MaxValidationDepth != null && _currentPath.Count > MaxValidationDepth)
            {
                // Non cyclic but too deep an object graph.

                // Pop the current model to make ValidationStack.Dispose happy
                _currentPath.Pop(model);

                string message;
                switch (metadata.MetadataKind)
                {
                    case ModelMetadataKind.Property:
                        message = Resources.FormatValidationVisitor_ExceededMaxPropertyDepth(nameof(ValidationVisitor), MaxValidationDepth, metadata.Name, metadata.ContainerType);
                        break;

                    default:
                        // Since the minimum depth is never 0, MetadataKind can never be Parameter. Consequently we only special case MetadataKind.Property.
                        message = Resources.FormatValidationVisitor_ExceededMaxDepth(nameof(ValidationVisitor), MaxValidationDepth, metadata.ModelType);
                        break;
                }

                message += " " + Resources.FormatValidationVisitor_ExceededMaxDepthFix(nameof(MvcOptions), nameof(MvcOptions.MaxValidationDepth));
                throw new InvalidOperationException(message)
                {
                    HelpLink = "https://aka.ms/AA21ue1",
                };
            }

            var entry = GetValidationEntry(model);
            key = entry?.Key ?? key ?? string.Empty;
            metadata = entry?.Metadata ?? metadata;
            var strategy = entry?.Strategy;

            if (ModelState.HasReachedMaxErrors)
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
            // If the metadata indicates that no validators exist AND the aggregate state for the key says that the model graph
            // is not invalid (i.e. is one of Unvalidated, Valid, or Skipped) we can safely mark the graph as valid.
            else if (metadata.HasValidators == false &&
                ModelState.GetFieldValidationState(key) != ModelValidationState.Invalid)
            {
                // No validators will be created for this graph of objects. Mark it as valid if it wasn't previously validated.
                var entries = ModelState.FindKeysWithPrefix(key);
                foreach (var item in entries)
                {
                    if (item.Value.ValidationState == ModelValidationState.Unvalidated)
                    {
                        item.Value.ValidationState = ModelValidationState.Valid;
                    }
                }

                _currentPath.Pop(model);
                return true;
            }

            using (StateManager.Recurse(this, key ?? string.Empty, metadata, model, strategy))
            {
                if (Metadata.IsEnumerableType)
                {
                    return VisitComplexType(DefaultCollectionValidationStrategy.Instance);
                }

                if (Metadata.IsComplexType)
                {
                    return VisitComplexType(DefaultComplexObjectValidationStrategy.Instance);
                }

                return VisitSimpleType();
            }
        }

        // Covers everything VisitSimpleType does not i.e. both enumerations and complex types.
        protected virtual bool VisitComplexType(IValidationStrategy defaultStrategy)
        {
            var isValid = true;

            if (Model != null && Metadata.ValidateChildren)
            {
                var strategy = Strategy ?? defaultStrategy;
                isValid = VisitChildren(strategy);
            }
            else if (Model != null)
            {
                // Suppress validation for the entries matching this prefix. This will temporarily set
                // the current node to 'skipped' but we're going to visit it right away, so subsequent
                // code will set it to 'valid' or 'invalid'
                SuppressValidation(Key);
            }

            // Double-checking HasReachedMaxErrors just in case this model has no properties.
            // If validation has failed for any children, only validate the parent if ValidateComplexTypesIfChildValidationFails is true.
            if ((isValid || ValidateComplexTypesIfChildValidationFails) && !ModelState.HasReachedMaxErrors)
            {
                isValid &= ValidateNode();
            }

            return isValid;
        }

        protected virtual bool VisitSimpleType()
        {
            if (ModelState.HasReachedMaxErrors)
            {
                SuppressValidation(Key);
                return false;
            }

            return ValidateNode();
        }

        protected virtual bool VisitChildren(IValidationStrategy strategy)
        {
            var isValid = true;
            var enumerator = strategy.GetChildren(Metadata, Key, Model);
            var parentEntry = new ValidationEntry(Metadata, Key, Model);

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

        protected virtual void SuppressValidation(string key)
        {
            if (key == null)
            {
                // If the key is null, that means that we shouldn't expect any entries in ModelState for
                // this value, so there's nothing to do.
                return;
            }

            var entries = ModelState.FindKeysWithPrefix(key);
            foreach (var entry in entries)
            {
                if (entry.Value.ValidationState != ModelValidationState.Invalid)
                {
                    entry.Value.ValidationState = ModelValidationState.Skipped;
                }
            }
        }

        protected virtual ValidationStateEntry GetValidationEntry(object model)
        {
            if (model == null || ValidationState == null)
            {
                return null;
            }

            ValidationState.TryGetValue(model, out var entry);
            return entry;
        }

        protected readonly struct StateManager : IDisposable
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

                visitor.Container = visitor.Model;
                visitor.Key = key;
                visitor.Metadata = metadata;
                visitor.Model = model;
                visitor.Strategy = strategy;

                return recursifier;
            }

            public StateManager(ValidationVisitor visitor, object newModel)
            {
                _visitor = visitor;
                _newModel = newModel;

                _container = _visitor.Container;
                _key = _visitor.Key;
                _metadata = _visitor.Metadata;
                _model = _visitor.Model;
                _strategy = _visitor.Strategy;
            }

            public void Dispose()
            {
                _visitor.Container = _container;
                _visitor.Key = _key;
                _visitor.Metadata = _metadata;
                _visitor.Model = _model;
                _visitor.Strategy = _strategy;

                _visitor._currentPath.Pop(_newModel);
            }
        }
    }
}
