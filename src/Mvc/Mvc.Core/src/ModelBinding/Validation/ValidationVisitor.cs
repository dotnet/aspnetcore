// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Diagnostics;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Mvc.Core;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;

namespace Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

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
        ValidationStateDictionary? validationState)
    {
        ArgumentNullException.ThrowIfNull(actionContext);
        ArgumentNullException.ThrowIfNull(validatorProvider);
        ArgumentNullException.ThrowIfNull(validatorCache);

        Context = actionContext;
        ValidatorProvider = validatorProvider;
        Cache = validatorCache;

        MetadataProvider = metadataProvider;
        ValidationState = validationState;

        ModelState = actionContext.ModelState;
        _currentPath = new ValidationStack();
    }

    /// <summary>
    /// The model validator provider.
    /// </summary>
    protected IModelValidatorProvider ValidatorProvider { get; }

    /// <summary>
    /// The model metadata provider.
    /// </summary>
    protected IModelMetadataProvider MetadataProvider { get; }

    /// <summary>
    /// The validator cache.
    /// </summary>
    protected ValidatorCache Cache { get; }

    /// <summary>
    /// The action context.
    /// </summary>
    protected ActionContext Context { get; }

    /// <summary>
    /// The model state.
    /// </summary>
    protected ModelStateDictionary ModelState { get; }

    /// <summary>
    /// The validation state.
    /// </summary>
    protected ValidationStateDictionary? ValidationState { get; }

    /// <summary>
    /// The container.
    /// </summary>
    protected object? Container { get; set; }

    /// <summary>
    /// The key.
    /// </summary>
    protected string? Key { get; set; }

    /// <summary>
    /// The model.
    /// </summary>
    protected object? Model { get; set; }

    /// <summary>
    /// The model metadata.
    /// </summary>
    protected ModelMetadata? Metadata { get; set; }

    /// <summary>
    /// The validation strategy.
    /// </summary>
    protected IValidationStrategy? Strategy { get; set; }

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
    public virtual bool Validate(ModelMetadata? metadata, string? key, object? model, bool alwaysValidateAtTopLevel)
        => Validate(metadata, key, model, alwaysValidateAtTopLevel, container: null);

    /// <summary>
    /// Validates a object.
    /// </summary>
    /// <param name="metadata">The <see cref="ModelMetadata"/> associated with the model.</param>
    /// <param name="key">The model prefix key.</param>
    /// <param name="model">The model object.</param>
    /// <param name="alwaysValidateAtTopLevel">If <c>true</c>, applies validation rules even if the top-level value is <c>null</c>.</param>
    /// <param name="container">The model container.</param>
    /// <returns><c>true</c> if the object is valid, otherwise <c>false</c>.</returns>
    public virtual bool Validate(ModelMetadata? metadata, string? key, object? model, bool alwaysValidateAtTopLevel, object? container)
    {
        if (container != null && metadata!.MetadataKind != ModelMetadataKind.Property)
        {
            throw new ArgumentException(Resources.FormatValidationVisitor_ContainerCannotBeSpecified(metadata.MetadataKind));
        }

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

        // Container is non-null only when validation top-level properties. Start off by treating "container" as the "Model" instance.
        // Invoking StateManager.Recurse later in this invocation will result in it being correctly used as the container instance during the
        // validation of "model".
        Model = container;
        return Visit(metadata!, key, model);
    }

    /// <summary>
    /// Validates a single node in a model object graph.
    /// </summary>
    /// <returns><c>true</c> if the node is valid, otherwise <c>false</c>.</returns>
    protected virtual bool ValidateNode()
    {
        Debug.Assert(Key != null);
        Debug.Assert(Metadata != null);
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
                    Metadata!,
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

    /// <summary>
    /// Validate something in a model.
    /// </summary>
    /// <param name="metadata">The model metadata.</param>
    /// <param name="key">The key to validate.</param>
    /// <param name="model">The model to validate.</param>
    /// <see langword="true"/> if the specified model key is valid, otherwise <see langword="false"/>.
    /// <returns>Whether the the specified model key is valid.</returns>
    protected virtual bool Visit(ModelMetadata metadata, string? key, object? model)
    {
        RuntimeHelpers.EnsureSufficientExecutionStack();

        if (model != null && !_currentPath.Push(model))
        {
            // This is a cycle, bail.
            return true;
        }

        bool result;
        try
        {
            // Throws InvalidOperationException if the object graph is too deep
            result = VisitImplementation(ref metadata, ref key, model);
        }
        finally
        {
            _currentPath.Pop(model);
        }
        return result;
    }

    private bool VisitImplementation(ref ModelMetadata metadata, ref string? key, object? model)
    {
        if (MaxValidationDepth != null && _currentPath.Count > MaxValidationDepth)
        {
            // Non cyclic but too deep an object graph.

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
            return true;
        }
        // If the metadata indicates that no validators exist AND the aggregate state for the key says that the model graph
        // is not invalid (i.e. is one of Unvalidated, Valid, or Skipped) we can safely mark the graph as valid.
        else if (metadata.HasValidators == false &&
            ModelState.GetFieldValidationState(key) != ModelValidationState.Invalid)
        {
            if (metadata.BoundConstructor != null)
            {
                metadata.ThrowIfRecordTypeHasValidationOnProperties();
            }

            // No validators will be created for this graph of objects. Mark it as valid if it wasn't previously validated.
            var entries = ModelState.FindKeysWithPrefix(key);
            foreach (var item in entries)
            {
                if (item.Value.ValidationState == ModelValidationState.Unvalidated)
                {
                    item.Value.ValidationState = ModelValidationState.Valid;
                }
            }

            return true;
        }

        using (StateManager.Recurse(this, key ?? string.Empty, metadata, model, strategy!))
        {
            if (Metadata!.IsEnumerableType)
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

    /// <summary>
    /// Validate complex types, this covers everything VisitSimpleType does not i.e. both enumerations and complex types.
    /// </summary>
    /// <param name="defaultStrategy">The default validation strategy to use.</param>
    /// <returns><see langword="true" /> if valid, otherwise <see langword="false" />.</returns>
    protected virtual bool VisitComplexType(IValidationStrategy defaultStrategy)
    {
        var isValid = true;

        if (Model != null && Metadata!.ValidateChildren)
        {
            var strategy = Strategy ?? defaultStrategy;
            isValid = VisitChildren(strategy);
        }
        else if (Model != null)
        {
            // Suppress validation for the entries matching this prefix. This will temporarily set
            // the current node to 'skipped' but we're going to visit it right away, so subsequent
            // code will set it to 'valid' or 'invalid'
            SuppressValidation(Key!);
        }

        // Double-checking HasReachedMaxErrors just in case this model has no properties.
        // If validation has failed for any children, only validate the parent if ValidateComplexTypesIfChildValidationFails is true.
        if ((isValid || ValidateComplexTypesIfChildValidationFails) && !ModelState.HasReachedMaxErrors)
        {
            isValid &= ValidateNode();
        }

        return isValid;
    }

    /// <summary>
    /// Validate a simple type.
    /// </summary>
    /// <returns>True if valid.</returns>
    protected virtual bool VisitSimpleType()
    {
        if (ModelState.HasReachedMaxErrors)
        {
            SuppressValidation(Key!);
            return false;
        }

        return ValidateNode();
    }

    /// <summary>
    /// Validate all the child nodes using the specified strategy.
    /// </summary>
    /// <param name="strategy">The validation strategy.</param>
    /// <returns><see langword="true" /> if all children are valid, otherwise <see langword="false" />.</returns>
    protected virtual bool VisitChildren(IValidationStrategy strategy)
    {
        Debug.Assert(Metadata is not null && Key is not null && Model is not null);

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

    /// <summary>
    /// Supress validation for a given key.
    /// </summary>
    /// <param name="key">The key to supress.</param>
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

    /// <summary>
    /// Get the validation entry for the model.
    /// </summary>
    /// <param name="model">The model.</param>
    /// <returns>The validation state entry for the model.</returns>
    protected virtual ValidationStateEntry? GetValidationEntry(object? model)
    {
        if (model == null || ValidationState == null)
        {
            return null;
        }

        ValidationState.TryGetValue(model, out var entry);
        return entry;
    }

    /// <summary>
    /// State manager used for by <see cref="ValidationVisitor"/>.
    /// </summary>
    protected readonly struct StateManager : IDisposable
    {
        private readonly ValidationVisitor _visitor;
        private readonly object? _container;
        private readonly string _key;
        private readonly ModelMetadata _metadata;
        private readonly object _model;
        private readonly IValidationStrategy _strategy;

        /// <summary>
        /// Set up a state manager from a visitor.
        /// </summary>
        /// <param name="visitor">The visitor.</param>
        /// <param name="key">The key.</param>
        /// <param name="metadata">The metadata.</param>
        /// <param name="model">The model.</param>
        /// <param name="strategy">The strategy.</param>
        /// <returns>A StateManager setup for recursion.</returns>
        public static StateManager Recurse(
            ValidationVisitor visitor,
            string key,
            ModelMetadata metadata,
            object? model,
            IValidationStrategy strategy)
        {
            var recursifier = new StateManager(visitor, null);

            visitor.Container = visitor.Model;
            visitor.Key = key;
            visitor.Metadata = metadata;
            visitor.Model = model;
            visitor.Strategy = strategy;

            return recursifier;
        }

        /// <summary>
        /// Initialize a new <see cref="StateManager"/>.
        /// </summary>
        /// <param name="visitor">The visitor.</param>
        /// <param name="newModel">The model to validate.</param>
        public StateManager(ValidationVisitor visitor, object? newModel)
        {
            _visitor = visitor;

            _container = _visitor.Container;
            _key = _visitor.Key!;
            _metadata = _visitor.Metadata!;
            _model = _visitor.Model!;
            _strategy = _visitor.Strategy!;
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            _visitor.Container = _container;
            _visitor.Key = _key;
            _visitor.Metadata = _metadata;
            _visitor.Model = _model;
            _visitor.Strategy = _strategy;
        }
    }
}
