// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http.Validation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

[assembly: MetadataUpdateHandler(typeof(Microsoft.AspNetCore.Components.Forms.EditContextDataAnnotationsExtensions))]

namespace Microsoft.AspNetCore.Components.Forms;

/// <summary>
/// Extension methods to add DataAnnotations validation to an <see cref="EditContext"/>.
/// </summary>
public static partial class EditContextDataAnnotationsExtensions
{
    /// <summary>
    /// Adds DataAnnotations validation support to the <see cref="EditContext"/>.
    /// </summary>
    /// <param name="editContext">The <see cref="EditContext"/>.</param>
    [Obsolete("Use " + nameof(EnableDataAnnotationsValidation) + " instead.")]
    public static EditContext AddDataAnnotationsValidation(this EditContext editContext)
    {
        EnableDataAnnotationsValidation(editContext);
        return editContext;
    }

    /// <summary>
    /// Enables DataAnnotations validation support for the <see cref="EditContext"/>.
    /// </summary>
    /// <param name="editContext">The <see cref="EditContext"/>.</param>
    /// <returns>A disposable object whose disposal will remove DataAnnotations validation support from the <see cref="EditContext"/>.</returns>
    [Obsolete("This API is obsolete and may be removed in future versions. Use the overload that accepts an IServiceProvider instead.")]
    public static IDisposable EnableDataAnnotationsValidation(this EditContext editContext)
    {
        return new DataAnnotationsEventSubscriptions(editContext, null!);
    }
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

        public DataAnnotationsEventSubscriptions(EditContext editContext, IServiceProvider serviceProvider)
        {
            _editContext = editContext ?? throw new ArgumentNullException(nameof(editContext));
            _serviceProvider = serviceProvider;
            _messages = new ValidationMessageStore(_editContext);

            _editContext.OnFieldChanged += OnFieldChanged;
            _editContext.OnValidationRequested += OnValidationRequested;

            if (MetadataUpdater.IsSupported)
            {
                OnClearCache += ClearCache;
            }
        }

        // TODO(OR): Should this also use ValidatablePropertyInfo.ValidateAsync?
        [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Model types are expected to be defined in assemblies that do not get trimmed.")]
        private void OnFieldChanged(object? sender, FieldChangedEventArgs eventArgs)
        {
            var fieldIdentifier = eventArgs.FieldIdentifier;
            if (TryGetValidatableProperty(fieldIdentifier, out var propertyInfo))
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
        }

        [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Model types are expected to be defined in assemblies that do not get trimmed.")]
        private void OnValidationRequested(object? sender, ValidationRequestedEventArgs e)
        {
            var validationContext = new ValidationContext(_editContext.Model, _serviceProvider, items: null);

            if (!TryValidateTypeInfo(validationContext))
            {
                ValidateWithDefaultValidator(validationContext);
            }

            _editContext.NotifyValidationStateChanged();
        }

        [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Model types are expected to be defined in assemblies that do not get trimmed.")]
        private void ValidateWithDefaultValidator(ValidationContext validationContext)
        {
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
        private bool TryValidateTypeInfo(ValidationContext validationContext)
        {
            var options = _serviceProvider?.GetService<IOptions<ValidationOptions>>()?.Value;

            if (options == null || !options.TryGetValidatableTypeInfo(_editContext.Model.GetType(), out var typeInfo))
            {
                return false;
            }

            var validateContext = new ValidateContext
            {
                ValidationOptions = options,
                ValidationContext = validationContext,
            };

            var containerMapping = new Dictionary<string, object?>();

            validateContext.OnValidationError += (key, _, container) => containerMapping[key] = container;

            var validationTask = typeInfo.ValidateAsync(_editContext.Model, validateContext, CancellationToken.None);

            if (!validationTask.IsCompleted)
            {
                throw new InvalidOperationException("Async validation is not supported");
            }

            var validationErrors = validateContext.ValidationErrors;

            // Transfer results to the ValidationMessageStore
            _messages.Clear();

            if (validationErrors is not null && validationErrors.Count > 0)
            {
                foreach (var (fieldKey, messages) in validationErrors)
                {
                    // Reverse mapping based on storing references during validation.
                    // With this approach, we could skip iterating over ValidateContext.ValidationErrors and pass the errors
                    // directly to ValidationMessageStore in the OnValidationError handler.
                    var fieldContainer = containerMapping[fieldKey] ?? _editContext.Model;

                    // Alternative: Reverse mapping based on object graph walk.
                    //var fieldContainer = GetFieldContainer(_editContext.Model, fieldKey);

                    var lastDotIndex = fieldKey.LastIndexOf('.');
                    var fieldName = lastDotIndex >= 0 ? fieldKey[(lastDotIndex + 1)..] : fieldKey;

                    _messages.Add(new FieldIdentifier(fieldContainer, fieldName), messages);
                }
            }

            return true;
        }
#pragma warning restore ASP0029 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

        // TODO(OR): Replace this with a more robust implementation or a different approach. E.g. collect references during the validation process itself.
        [UnconditionalSuppressMessage("Trimming", "IL2075", Justification = "Model types are expected to be defined in assemblies that do not get trimmed.")]
        private static object GetFieldContainer(object obj, string fieldKey)
        {
            // The method does not check all possible null access and index bound errors as the path is constructed internally and assumed to be correct.
            var dotSegments = fieldKey.Split('.')[..^1];
            var currentObject = obj;

            for (int i = 0; i < dotSegments.Length; i++)
            {
                string segment = dotSegments[i];

                if (currentObject == null)
                {
                    string traversedPath = string.Join(".", dotSegments.Take(i));
                    throw new ArgumentException($"Cannot access segment '{segment}' because the path '{traversedPath}' resolved to null.");
                }

                Match match = _pathSegmentRegex.Match(segment);
                if (!match.Success)
                {
                    throw new ArgumentException($"Invalid path segment: '{segment}'.");
                }

                string propertyName = match.Groups[1].Value;
                string? indexStr = match.Groups[2].Success ? match.Groups[2].Value : null;

                Type currentType = currentObject.GetType();
                PropertyInfo propertyInfo = currentType!.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance)!;
                object propertyValue = propertyInfo!.GetValue(currentObject)!;

                if (indexStr == null) // Simple property access
                {
                    currentObject = propertyValue;
                }
                else // Indexed access
                {
                    if (!int.TryParse(indexStr, out int index))
                    {
                        throw new ArgumentException($"Invalid index '{indexStr}' in segment '{segment}'.");
                    }

                    if (propertyValue is Array array)
                    {
                        currentObject = array.GetValue(index)!;
                    }
                    else if (propertyValue is IList list)
                    {
                        currentObject = list[index]!;
                    }
                    else if (propertyValue is IEnumerable enumerable)
                    {
                        currentObject = enumerable.Cast<object>().ElementAt(index);
                    }
                    else
                    {
                        throw new ArgumentException($"Property '{propertyName}' is not an array, list, or enumerable. Cannot access by index in segment '{segment}'.");
                    }
                }

            }
            return currentObject!;
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

        private static readonly Regex _pathSegmentRegex = PathSegmentRegexGen();

        // Regex to parse "PropertyName" or "PropertyName[index]"
        [GeneratedRegex(@"^([a-zA-Z_]\w*)(?:\[(\d+)\])?$", RegexOptions.Compiled)]
        private static partial Regex PathSegmentRegexGen();
    }
}
