// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Extensions.Validation;

/// <summary>
/// Represents the context for validating a validatable object.
/// </summary>
[Experimental("ASP0029", UrlFormat = "https://aka.ms/aspnet/analyzer/{0}")]
public sealed class ValidateContext
{
    private ConcurrentDictionary<string, IEnumerable<string>>? _validationErrors;

    /// <summary>
    /// Initializes a new instance of <see cref="ValidateContext"/>.
    /// </summary>
    public ValidateContext()
    {
    }

    internal ValidateContext(ValidateContext original, ValidateContextMutableState state)
    {
        CurrentDepth = state.Depth;
        CurrentValidationPath = state.Path;

        if (original.OnValidationError?.GetInvocationList() is { } onValidationErrorDelegates)
        {
            foreach (var onValidationErrorDelegate in onValidationErrorDelegates)
            {
                OnValidationError += context => ((Action<ValidationErrorContext>)onValidationErrorDelegate).Invoke(context);
            }
        }
    }

    /// <summary>
    /// Gets or sets the validation context used for validating objects that implement <see cref="IValidatableObject"/> or have <see cref="ValidationAttribute"/>.
    /// This context provides access to service provider and other validation metadata.
    /// </summary>
    /// <remarks>
    /// This property should be set by the consumer of the validatable info
    /// interface to provide the necessary context for validation. The object should be initialized
    /// with the current object being validated, the display name, and the service provider to support
    /// the complete set of validation scenarios.
    /// </remarks>
    /// <example>
    /// <code language="csharp">
    /// var validationContext = new ValidationContext(objectToValidate, serviceProvider, items);
    /// var validationOptions = serviceProvider.GetService&lt;IOptions&lt;ValidationOptions&gt;&gt;()?.Value;
    /// var validateContext = new ValidateContext
    /// {
    ///     ValidationContext = validationContext,
    ///     ValidationOptions = validationOptions
    /// };
    /// </code>
    /// </example>
    public required ValidationContext ValidationContext { get; set; }

    /// <summary>
    /// Gets or sets the prefix used to identify the current object being validated in a complex object graph.
    /// </summary>
    /// <remarks>
    /// This prefix is used to build property paths in validation error messages (for example, "Customer.Address.Street").
    /// </remarks>
    public string CurrentValidationPath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the validation options that control validation behavior,
    /// including validation depth limits and resolver registration.
    /// </summary>
    public required ValidationOptions ValidationOptions { get; set; }

    /// <summary>
    /// Gets the dictionary of validation errors collected during validation.
    /// </summary>
    /// <remarks>
    /// Keys are property names or paths, and values are collection of error messages.
    /// There are no guarantees whether or not this dictionary is lazy. Usages should treat null and empty dictionary the same.
    /// </remarks>
    public IReadOnlyDictionary<string, IEnumerable<string>>? ValidationErrors
        => _validationErrors;

    /// <summary>
    /// Gets or sets the current depth in the validation hierarchy.
    /// </summary>
    /// <remarks>
    /// This value is used to prevent stack overflows from circular references.
    /// </remarks>
    public int CurrentDepth { get; set; }

    /// <summary>
    /// Optional event raised when a validation error is reported.
    /// Note that this event may be raised concurrently from different threads.
    /// </summary>
    public event Action<ValidationErrorContext>? OnValidationError;

    internal ValidateContext CopyWithState(ValidateContextMutableState state)
    {
        return new ValidateContext(this, state)
        {
            ValidationOptions = this.ValidationOptions,
            ValidationContext = CloneValidationContextWithMutableState(state),
        };
    }

    private ValidateContextMutableState CaptureMutableState()
        => new ValidateContextMutableState()
        {
            Depth = CurrentDepth,
            Path = CurrentValidationPath,
            DisplayName = ValidationContext.DisplayName,
            MemberName = ValidationContext.MemberName,
        };

    /// <summary>
    /// Adds a validation error to <see cref="ValidationErrors"/> and raises the <see cref="OnValidationError"/> event.
    /// </summary>
    /// <param name="validationErrorContext"></param>
    public void AddValidationError(ValidationErrorContext validationErrorContext)
    {
        AddValidationErrorSuppressEvent(validationErrorContext.Path, validationErrorContext.Errors);

        OnValidationError?.Invoke(validationErrorContext);
    }

    private bool MergeErrorsFromClonedContexts(List<ValidateContext>? clonedContexts)
    {
        if (clonedContexts is null)
        {
            return false;
        }

        bool hasErrors = false;
        foreach (var clonedContext in clonedContexts)
        {
            if (clonedContext.ValidationErrors is null)
            {
                continue;
            }

            foreach (var validationError in clonedContext.ValidationErrors)
            {
                hasErrors = true;

                // Event is cloned and was already raised when the error got added to the cloned context.
                // We could avoid cloning the event so that cloned context never have event subscribers.
                // However, that will mean we need to store more information that are needed by
                // the event in the dictionary.
                // Note that the dictionary is a public API.
                // Maybe it actually makes sense to re-consider the public API shape and if the additional
                // information are needed?
                AddValidationErrorSuppressEvent(validationError.Key, validationError.Value);
            }
        }

        return hasErrors;
    }

    private void AddValidationErrorSuppressEvent(string path, IEnumerable<string> errors)
    {
        var validationErrors = _validationErrors;
        if (validationErrors is null)
        {
            var newDictionary = new ConcurrentDictionary<string, IEnumerable<string>>();
            validationErrors = Interlocked.CompareExchange(ref _validationErrors, newDictionary, null) ?? newDictionary;
        }

        var existingErrors = (ConcurrentQueue<string>)validationErrors.GetOrAdd(path, static _ => new ConcurrentQueue<string>());
        foreach (var error in errors)
        {
            existingErrors.Enqueue(error);
        }
    }

    internal string? ResolveAttributeErrorMessage(
        string memberName,
        string displayName,
        Type? declaringType,
        ValidationAttribute attribute,
        ValidationResult result)
    {
        if (ValidationOptions.Localizer is null || attribute.ErrorMessageResourceType is not null)
        {
            return result.ErrorMessage;
        }

        var context = new ErrorMessageLocalizationContext
        {
            MemberName = memberName,
            DisplayName = displayName,
            DeclaringType = declaringType,
            Attribute = attribute,
        };

        return ValidationOptions.Localizer.ResolveErrorMessage(context) ?? result.ErrorMessage;
    }

    private ValidationContext CloneValidationContextWithMutableState(ValidateContextMutableState state)
    {
        var original = ValidationContext;
        return new ValidationContext(
            original.ObjectInstance,
            state.DisplayName,
            original,
            original.Items)
        {
            MemberName = state.MemberName,
        };
    }

    internal AsyncValidationTracker TrackAsyncValidations()
        => new AsyncValidationTracker(this);

    internal struct AsyncValidationTracker
    {
        private readonly ValidateContext _originalContext;
        private readonly ValidateContextMutableState _originalState;

        private bool _nextNeedsClone;
        private ValidateContext _currentContext;
        private List<ValidateContext>? _clonedContexts;
        private List<Task>? _pendingTasks;

        public AsyncValidationTracker(ValidateContext context)
        {
            _originalContext = context;
            _currentContext = context;
            _originalState = context.CaptureMutableState();
        }

        // Reuses the context while validations complete synchronously; clones only after one goes async,
        // so two concurrently-running validations never share a context.
        public ValidateContext NextContext()
        {
            if (_nextNeedsClone)
            {
                _currentContext = _originalContext.CopyWithState(_originalState);
                (_clonedContexts ??= []).Add(_currentContext);
                _nextNeedsClone = false;
            }

            return _currentContext;
        }

        public void Track(Task validationTask)
        {
            if (validationTask.IsCompletedSuccessfully)
            {
                return; // synchronous: keep using the same context
            }

            _nextNeedsClone = true; // the next item must get its own clone
            (_pendingTasks ??= []).Add(validationTask);
        }

        // Stays fully synchronous when nothing was tracked; otherwise awaits all and merges clone errors back.
        public readonly Task<bool> CompleteAsync()
            => _pendingTasks is null ? Task.FromResult(false) : AwaitAndMergeAsync(_pendingTasks, _clonedContexts, _originalContext);

        private static async Task<bool> AwaitAndMergeAsync(List<Task> pendingTasks, List<ValidateContext>? clonedContexts, ValidateContext originalContext)
        {
            await Task.WhenAll(pendingTasks);
            return originalContext.MergeErrorsFromClonedContexts(clonedContexts);
        }
    }

}
