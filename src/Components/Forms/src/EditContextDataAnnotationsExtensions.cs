// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Validation;

[assembly: MetadataUpdateHandler(typeof(Microsoft.AspNetCore.Components.Forms.EditContextDataAnnotationsExtensions))]

namespace Microsoft.AspNetCore.Components.Forms;

/// <summary>
/// Extension methods to add DataAnnotations validation to an <see cref="EditContext"/>.
/// </summary>
public static partial class EditContextDataAnnotationsExtensions
{
    /// <summary>
    /// Enables DataAnnotations validation support for the <see cref="EditContext"/>.
    /// </summary>
    /// <param name="editContext">The <see cref="EditContext"/>.</param>
    /// <param name="serviceProvider">The <see cref="IServiceProvider"/> to be used in the <see cref="ValidationContext"/>.</param>
    /// <returns>A disposable object whose disposal will remove DataAnnotations validation support from the <see cref="EditContext"/>.</returns>
    public static IDisposable EnableDataAnnotationsValidation(this EditContext editContext, IServiceProvider serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);
        return new DataAnnotationsEventSubscriptions(editContext, serviceProvider);
    }

    private static event Action? OnClearCache;

#pragma warning disable IDE0051 // Remove unused private members
    private static void ClearCache(Type[]? _)
    {
        OnClearCache?.Invoke();
    }
#pragma warning restore IDE0051 // Remove unused private members

    private sealed partial class DataAnnotationsEventSubscriptions : IDisposable
    {
        private static readonly ConcurrentDictionary<(Type ModelType, string FieldName), PropertyInfo?> _propertyInfoCache = new();

        private readonly EditContext _editContext;
        private readonly IServiceProvider? _serviceProvider;
        private readonly ValidationMessageStore _messages;
        private readonly ValidationOptions? _validationOptions;
#pragma warning disable ASP0029 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
        private readonly IValidatableInfo? _validatorTypeInfo;
#pragma warning restore ASP0029 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
        private readonly Dictionary<string, FieldIdentifier> _validationPathToFieldIdentifierMapping = new();

        [UnconditionalSuppressMessage("Trimming", "IL2066", Justification = "Model types are expected to be defined in assemblies that do not get trimmed.")]
        public DataAnnotationsEventSubscriptions(EditContext editContext, IServiceProvider serviceProvider)
        {
            _editContext = editContext ?? throw new ArgumentNullException(nameof(editContext));
            _serviceProvider = serviceProvider;
            _messages = new ValidationMessageStore(_editContext);
            _validationOptions = _serviceProvider?.GetService<IOptions<ValidationOptions>>()?.Value;
#pragma warning disable ASP0029 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
            _validatorTypeInfo = _validationOptions != null && _validationOptions.TryGetValidatableTypeInfo(_editContext.Model.GetType(), out var typeInfo)
                ? typeInfo
                : null;
#pragma warning restore ASP0029 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
            _editContext.OnFieldChanged += OnFieldChanged;
            _editContext.OnValidationRequested += OnValidationRequested;
            _editContext.OnValidationRequestedAsync += OnValidationRequestedAsync;

            if (MetadataUpdater.IsSupported)
            {
                OnClearCache += ClearCache;
            }
        }

        [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Model types are expected to be defined in assemblies that do not get trimmed.")]
        private void OnFieldChanged(object? sender, FieldChangedEventArgs eventArgs)
        {
            var fieldIdentifier = eventArgs.FieldIdentifier;

#pragma warning disable ASP0029 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
            if (_validatorTypeInfo is ValidatableTypeInfo typeInfo &&
                typeInfo.GetProperty(fieldIdentifier.FieldName) is ValidatablePropertyInfo validatablePropertyInfo)
            {
                RunAsyncFieldValidation(fieldIdentifier, validatablePropertyInfo);
            }
#pragma warning restore ASP0029 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
            else if (TryGetValidatableProperty(fieldIdentifier, out var propertyInfo))
            {
                ValidateField(fieldIdentifier, propertyInfo);
            }
        }

        private void OnValidationRequested(object? sender, ValidationRequestedEventArgs e)
        {
            if (_validatorTypeInfo is not null)
            {
                // The IValidatableInfo path runs through OnValidationRequestedAsync.
                // EditContext.Validate() drains async handlers synchronously and throws if any
                // are truly async, preserving the behavior where sync IValidatableInfo validators
                // run during Validate().
                return;
            }

            ValidateForm();
            _editContext.NotifyValidationStateChanged();
        }

        private async Task OnValidationRequestedAsync(object sender, ValidationRequestedEventArgs e)
        {
            if (_validatorTypeInfo is null)
            {
                // The async path requires IValidatableInfo for the model.
                return;
            }

            await ValidateFormAsync(_validatorTypeInfo);
            _editContext.NotifyValidationStateChanged();
        }

        [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Model types are expected to be defined in assemblies that do not get trimmed.")]
        private void ValidateForm()
        {
            var validationContext = new ValidationContext(_editContext.Model, _serviceProvider, items: null);
            var validationResults = new List<ValidationResult>();
            Validator.TryValidateObject(_editContext.Model, validationContext, validationResults, true);

            // Transfer results to the ValidationMessageStore
            _messages.Clear();
            foreach (var validationResult in validationResults)
            {
                if (validationResult == null)
                {
                    continue;
                }

                var hasMemberNames = false;
                foreach (var memberName in validationResult.MemberNames)
                {
                    hasMemberNames = true;
                    _messages.Add(_editContext.Field(memberName), validationResult.ErrorMessage!);
                }

                if (!hasMemberNames)
                {
                    _messages.Add(new FieldIdentifier(_editContext.Model, fieldName: string.Empty), validationResult.ErrorMessage!);
                }
            }
        }

#pragma warning disable ASP0029 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
        [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Model types are expected to be defined in assemblies that do not get trimmed.")]
        private async Task ValidateFormAsync(IValidatableInfo validatableInfo)
        {
            var validationContext = new ValidationContext(_editContext.Model, _serviceProvider, items: null);
            var validateContext = new ValidateContext
            {
                ValidationOptions = _validationOptions!,
                ValidationContext = validationContext,
            };
            try
            {
                validateContext.OnValidationError += AddMapping;

                await validatableInfo.ValidateAsync(_editContext.Model, validateContext, CancellationToken.None);

                // Transfer results to the ValidationMessageStore
                _messages.Clear();

                if (validateContext.ValidationErrors is { Count: > 0 } validationErrors)
                {
                    foreach (var (fieldKey, messages) in validationErrors)
                    {
                        var fieldIdentifier = _validationPathToFieldIdentifierMapping[fieldKey];
                        _messages.Add(fieldIdentifier, messages);
                    }
                }
            }
            finally
            {
                validateContext.OnValidationError -= AddMapping;
                _validationPathToFieldIdentifierMapping.Clear();
            }
        }

        [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Model types are expected to be defined in assemblies that do not get trimmed.")]
        private void ValidateField(FieldIdentifier fieldIdentifier, PropertyInfo propertyInfo)
        {
            var propertyValue = propertyInfo.GetValue(fieldIdentifier.Model);
            var validationContext = new ValidationContext(fieldIdentifier.Model, _serviceProvider, items: null)
            {
                MemberName = propertyInfo.Name
            };
            var results = new List<ValidationResult>();

            Validator.TryValidateProperty(propertyValue, validationContext, results);
            _messages.Clear(fieldIdentifier);
            foreach (var result in CollectionsMarshal.AsSpan(results))
            {
                _messages.Add(fieldIdentifier, result.ErrorMessage!);
            }

            // We have to notify even if there were no messages before and are still no messages now,
            // because the "state" that changed might be the completion of some async validation task
            _editContext.NotifyValidationStateChanged();
        }

        private void RunAsyncFieldValidation(FieldIdentifier fieldIdentifier, ValidatablePropertyInfo validatableInfo)
        {
            // Clear stale messages immediately so the field shows neutral state
            // while async validation is in progress.
            _messages.Clear(fieldIdentifier);

            var cts = new CancellationTokenSource();
            var task = ValidateFieldAsync(fieldIdentifier, validatableInfo, cts.Token);

            if (task.IsCompleted)
            {
                // Sync-only validators - task completed immediately, results already in store.
                cts.Dispose();
            }
            else
            {
                // Has async validators - register for tracking (pending/faulted state).
                _editContext.AddValidationTask(fieldIdentifier, task, cts);
            }
        }

        [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Model types are expected to be defined in assemblies that do not get trimmed.")]
        private async Task ValidateFieldAsync(
            FieldIdentifier fieldIdentifier,
            ValidatablePropertyInfo validatableInfo,
            CancellationToken cancellationToken)
        {
            var validationContext = new ValidationContext(fieldIdentifier.Model, _serviceProvider, items: null);
            var validateContext = new ValidateContext
            {
                ValidationOptions = _validationOptions!,
                ValidationContext = validationContext,
            };

            try
            {
                await validatableInfo.ValidateAsync(fieldIdentifier.Model, validateContext, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                // Task was cancelled (user re-edited field or form is submitting).
                // Clear stale messages so the field shows a neutral state, not old results.
                _messages.Clear(fieldIdentifier);
                _editContext.NotifyValidationStateChanged();
                return;
            }

            // Transfer results to the ValidationMessageStore
            _messages.Clear(fieldIdentifier);

            if (validateContext.ValidationErrors is { Count: > 0 } validationErrors)
            {
                foreach (var (_, messages) in validationErrors)
                {
                    _messages.Add(fieldIdentifier, messages);
                }
            }

            _editContext.NotifyValidationStateChanged();
        }

        private void AddMapping(ValidationErrorContext context)
        {
            _validationPathToFieldIdentifierMapping[context.Path] =
                new FieldIdentifier(context.Container ?? _editContext.Model, context.Name);
        }

        public void Dispose()
        {
            _messages.Clear();
            _editContext.OnFieldChanged -= OnFieldChanged;
            _editContext.OnValidationRequested -= OnValidationRequested;
            _editContext.OnValidationRequestedAsync -= OnValidationRequestedAsync;
            _editContext.NotifyValidationStateChanged();

            if (MetadataUpdater.IsSupported)
            {
                OnClearCache -= ClearCache;
            }
        }

        [UnconditionalSuppressMessage("Trimming", "IL2080", Justification = "Model types are expected to be defined in assemblies that do not get trimmed.")]
        private static bool TryGetValidatableProperty(in FieldIdentifier fieldIdentifier, [NotNullWhen(true)] out PropertyInfo? propertyInfo)
        {
            var cacheKey = (ModelType: fieldIdentifier.Model.GetType(), fieldIdentifier.FieldName);
            if (!_propertyInfoCache.TryGetValue(cacheKey, out propertyInfo))
            {
                // DataAnnotations only validates public properties, so that's all we'll look for
                // If we can't find it, cache 'null' so we don't have to try again next time
                propertyInfo = cacheKey.ModelType.GetProperty(cacheKey.FieldName);

                // No need to lock, because it doesn't matter if we write the same value twice
                _propertyInfoCache[cacheKey] = propertyInfo;
            }

            return propertyInfo != null;
        }

        internal void ClearCache()
        {
            _propertyInfoCache.Clear();
        }
    }
}
