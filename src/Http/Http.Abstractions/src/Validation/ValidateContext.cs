// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Http.Validation;

/// <summary>
/// Represents the context for validating a validatable object.
/// </summary>
[Experimental("ASP0029", UrlFormat = "https://aka.ms/aspnet/analyzer/{0}")]
public sealed class ValidateContext
{
    private JsonNamingPolicy? _dictionaryKeyPolicy;
    
    /// <summary>
    /// Gets or sets the validation context used for validating objects that implement <see cref="IValidatableObject"/> or have <see cref="ValidationAttribute"/>.
    /// This context provides access to service provider and other validation metadata.
    /// </summary>
    /// <remarks>
    /// This property should be set by the consumer of the <see cref="IValidatableInfo"/>
    /// interface to provide the necessary context for validation. The object should be initialized
    /// with the current object being validated, the display name, and the service provider to support
    /// the complete set of validation scenarios.
    /// </remarks>
    /// <example>
    /// <code>
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
    /// This is used to build property paths in validation error messages (e.g., "Customer.Address.Street").
    /// </summary>
    public string CurrentValidationPath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the validation options that control validation behavior,
    /// including validation depth limits and resolver registration.
    /// </summary>
    public required ValidationOptions ValidationOptions { get; set; }

    /// <summary>
    /// Gets or sets the dictionary of validation errors collected during validation.
    /// Keys are property names or paths, and values are arrays of error messages.
    /// In the default implementation, this dictionary is initialized when the first error is added.
    /// </summary>
    public Dictionary<string, string[]>? ValidationErrors { get; set; }

    /// <summary>
    /// Gets or sets the current depth in the validation hierarchy.
    /// This is used to prevent stack overflows from circular references.
    /// </summary>
    public int CurrentDepth { get; set; }
    
    /// <summary>
    /// Gets the DictionaryKeyPolicy from JsonSerializerOptions if available in the service provider.
    /// </summary>
    private JsonNamingPolicy? DictionaryKeyPolicy
    {
        get
        {
            if (_dictionaryKeyPolicy is null && ValidationContext.GetService(typeof(IOptions<JsonOptions>)) is IOptions<JsonOptions> jsonOptions)
            {
                _dictionaryKeyPolicy = jsonOptions.Value.SerializerOptions.DictionaryKeyPolicy;
            }

            return _dictionaryKeyPolicy;
        }
    }

    internal void AddValidationError(string key, string[] error)
    {
        ValidationErrors ??= [];

        var transformedKey = TransformKey(key);
        ValidationErrors[transformedKey] = error;
    }

    internal void AddOrExtendValidationErrors(string key, string[] errors)
    {
        ValidationErrors ??= [];

        var transformedKey = TransformKey(key);
        if (ValidationErrors.TryGetValue(transformedKey, out var existingErrors))
        {
            var newErrors = new string[existingErrors.Length + errors.Length];
            existingErrors.CopyTo(newErrors, 0);
            errors.CopyTo(newErrors, existingErrors.Length);
            ValidationErrors[transformedKey] = newErrors;
        }
        else
        {
            ValidationErrors[transformedKey] = errors;
        }
    }

    internal void AddOrExtendValidationError(string key, string error)
    {
        ValidationErrors ??= [];

        var transformedKey = TransformKey(key);
        if (ValidationErrors.TryGetValue(transformedKey, out var existingErrors) && !existingErrors.Contains(error))
        {
            ValidationErrors[transformedKey] = [.. existingErrors, error];
        }
        else
        {
            ValidationErrors[transformedKey] = [error];
        }
    }
    
    private string TransformKey(string key)
    {
        return DictionaryKeyPolicy?.ConvertName(key) ?? key;
    }
}
