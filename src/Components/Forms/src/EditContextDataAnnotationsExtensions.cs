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
        private readonly IValidatableTypeInfo? _validatorTypeInfo;
#pragma warning restore ASP0029 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

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

            if (MetadataUpdater.IsSupported)
            {
                OnClearCache += ClearCache;
            }
        }

        [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Model types are expected to be defined in assemblies that do not get trimmed.")]
        private void OnFieldChanged(object? sender, FieldChangedEventArgs eventArgs)
        {
            var fieldIdentifier = eventArgs.FieldIdentifier;
            var modelType = fieldIdentifier.Model.GetType();

#pragma warning disable ASP0029 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
            if (_validationOptions is not null &&
                _validationOptions.TryGetValidatableTypeInfo(modelType, out var typeInfo) &&
                typeInfo.TryFindProperty(fieldIdentifier.FieldName, _validationOptions, out var validatablePropertyInfo))
            {
                _editContext.RegisterAsyncFieldValidator(
                    fieldIdentifier,
                    token => ValidateFieldWithValidatableInfoAsync(fieldIdentifier, validatablePropertyInfo, token));
            }
#pragma warning restore ASP0029 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
            else if (TryGetValidatableProperty(fieldIdentifier, out var propertyInfo))
            {
                _editContext.RegisterAsyncFieldValidator(
                    fieldIdentifier,
                    token => ValidateFieldWithValidatorAsync(fieldIdentifier, propertyInfo, token));
            }
        }

        private void OnValidationRequested(object? sender, ValidationRequestedEventArgs e)
        {
            if (e.IsAsync)
            {
                // ValidateAsync invokes the registered validator and awaits the resulting task.
                e.AddAsyncValidator(ValidateFormAndNotifyAsync);
            }
            else
            {
                // Validate runs the form validation fully synchronously.
                ValidateFormAndNotify();
            }
        }

#pragma warning disable ASP0029 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
        private async Task ValidateFormAndNotifyAsync(CancellationToken cancellationToken)
        {
            if (_validatorTypeInfo is not null)
            {
                await ValidateFormWithValidatableInfoAsync(_validatorTypeInfo, cancellationToken);
            }
            else
            {
                await ValidateFormWithValidatorAsync(cancellationToken);
            }

            _editContext.NotifyValidationStateChanged();
        }

        private void ValidateFormAndNotify()
        {
            if (_validatorTypeInfo is not null)
            {
                ValidateFormWithValidatableInfo(_validatorTypeInfo);
            }
            else
            {
                ValidateFormWithValidator();
            }

            _editContext.NotifyValidationStateChanged();
        }
#pragma warning restore ASP0029 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

        [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Model types are expected to be defined in assemblies that do not get trimmed.")]
        private async Task ValidateFormWithValidatorAsync(CancellationToken cancellationToken)
        {
            var validationContext = new ValidationContext(_editContext.Model, _serviceProvider, items: null);
            var validationResults = new List<ValidationResult>();

            // Clear stale messages so the form shows neutral state; faulted state is signalled via EditContext.IsValidationFaulted.
            _messages.Clear();

            await Validator.TryValidateObjectAsync(_editContext.Model, validationContext, validationResults, validateAllProperties: true, cancellationToken);

            AddValidatorResults(validationResults);
        }

        [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Model types are expected to be defined in assemblies that do not get trimmed.")]
        private void ValidateFormWithValidator()
        {
            var validationContext = new ValidationContext(_editContext.Model, _serviceProvider, items: null);
            var validationResults = new List<ValidationResult>();

            // Clear stale messages so the form shows neutral state; faulted state is signalled via EditContext.IsValidationFaulted.
            _messages.Clear();

            Validator.TryValidateObject(_editContext.Model, validationContext, validationResults, validateAllProperties: true);

            AddValidatorResults(validationResults);
        }

        // Transfers Validator results to the ValidationMessageStore, keyed by member name (or the
        // model-level field when a result carries no member names). Shared by the synchronous and
        // asynchronous form validation paths.
        private void AddValidatorResults(List<ValidationResult> validationResults)
        {
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
        private async Task ValidateFormWithValidatableInfoAsync(IValidatableTypeInfo validatableInfo, CancellationToken cancellationToken)
        {
            var validateContext = CreateFormValidateContext();

            // Clear stale messages so the form shows neutral state; faulted state is signalled via EditContext.IsValidationFaulted.
            _messages.Clear();

            await validatableInfo.ValidateAsync(_editContext.Model, validateContext, cancellationToken);

            AddValidateContextErrors(validateContext);
        }

        [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Model types are expected to be defined in assemblies that do not get trimmed.")]
        private void ValidateFormWithValidatableInfo(IValidatableTypeInfo validatableInfo)
        {
            var validateContext = CreateFormValidateContext();

            // Clear stale messages so the form shows neutral state; faulted state is signalled via EditContext.IsValidationFaulted.
            _messages.Clear();

            validatableInfo.Validate(_editContext.Model, validateContext);

            AddValidateContextErrors(validateContext);
        }

        [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Model types are expected to be defined in assemblies that do not get trimmed.")]
        private ValidateContext CreateFormValidateContext()
        {
            return new ValidateContext
            {
                ValidationOptions = _validationOptions!,
                ServiceProvider = _serviceProvider,
            };
        }

        // Transfers ValidateContext errors to the ValidationMessageStore. Each reported error carries the
        // container and member name needed to build the FieldIdentifier directly. Shared by the synchronous
        // and asynchronous form validation paths.
        private void AddValidateContextErrors(ValidateContext validateContext)
        {
            if (validateContext.ValidationErrors is { Count: > 0 } validationErrors)
            {
                foreach (var (_, error) in validationErrors)
                {
                    foreach (var errorContext in error)
                    {
                        var fieldIdentifier = new FieldIdentifier(errorContext.Container ?? _editContext.Model, errorContext.Name);
                        _messages.Add(fieldIdentifier, errorContext.ErrorMessage);
                    }
                }
            }
        }

        [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Model types are expected to be defined in assemblies that do not get trimmed.")]
        private async Task ValidateFieldWithValidatorAsync(FieldIdentifier fieldIdentifier, PropertyInfo propertyInfo, CancellationToken cancellationToken)
        {
            var propertyValue = propertyInfo.GetValue(fieldIdentifier.Model);
            var validationContext = new ValidationContext(fieldIdentifier.Model, _serviceProvider, items: null)
            {
                MemberName = propertyInfo.Name
            };
            var results = new List<ValidationResult>();

            // Clear stale messages so the field shows neutral state; faulted state is signalled via EditContext.IsValidationFaulted.
            _messages.Clear(fieldIdentifier);

            try
            {
                await Validator.TryValidatePropertyAsync(propertyValue, validationContext, results, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                // Cancelled (field re-edited or form submitting); the superseding RegisterAsyncFieldValidator
                // call already notified with the cleared messages.
                return;
            }

            foreach (var result in CollectionsMarshal.AsSpan(results))
            {
                _messages.Add(fieldIdentifier, result.ErrorMessage!);
            }

            // Notify even when messages are unchanged: the change may be the completion of async validation.
            _editContext.NotifyValidationStateChanged();
        }

        [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Model types are expected to be defined in assemblies that do not get trimmed.")]
        private async Task ValidateFieldWithValidatableInfoAsync(
            FieldIdentifier fieldIdentifier,
            IValidatablePropertyInfo validatableInfo,
            CancellationToken cancellationToken)
        {
            var validateContext = new ValidateContext
            {
                ValidationOptions = _validationOptions!,
                ServiceProvider = _serviceProvider,
            };

            // Clear stale messages so the field shows neutral state; faulted state is signalled via EditContext.IsValidationFaulted.
            _messages.Clear(fieldIdentifier);

            try
            {
                await validatableInfo.ValidateAsync(fieldIdentifier.Model, validateContext, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                // Cancelled (field re-edited or form submitting); the superseding RegisterAsyncFieldValidator
                // call already notified with the cleared messages.
                return;
            }

            if (validateContext.ValidationErrors is { Count: > 0 } validationErrors)
            {
                foreach (var (_, error) in validationErrors)
                {
                    foreach (var errorContext in error)
                    {
                        _messages.Add(fieldIdentifier, errorContext.ErrorMessage);
                    }
                }
            }

            _editContext.NotifyValidationStateChanged();
        }

        public void Dispose()
        {
            _messages.Clear();
            _editContext.OnFieldChanged -= OnFieldChanged;
            _editContext.OnValidationRequested -= OnValidationRequested;
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
                propertyInfo = cacheKey.ModelType.GetProperty(
                    cacheKey.FieldName,
                    BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

                if (propertyInfo is null)
                {
                    propertyInfo = cacheKey.ModelType.GetProperty(
                        cacheKey.FieldName,
                        BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy);
                }

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
