// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Microsoft.AspNetCore.Http.Validation;

/// <summary>
/// Contains validation information for a parameter.
/// </summary>
public abstract class ValidatableParameterInfo
{
    /// <summary>
    /// Creates a new instance of <see cref="ValidatableParameterInfo"/>.
    /// </summary>
    /// <param name="name">The parameter name.</param>
    /// <param name="displayName">The display name for the parameter.</param>
    /// <param name="isOptional">Whether the parameter is optional.</param>
    /// <param name="hasValidatableType">Whether the parameter type is validatable.</param>
    /// <param name="isEnumerable">Whether the parameter is enumerable.</param>
    public ValidatableParameterInfo(
        string name,
        string displayName,
        bool isOptional,
        bool hasValidatableType,
        bool isEnumerable)
    {
        Name = name;
        DisplayName = displayName;
        IsOptional = isOptional;
        HasValidatableType = hasValidatableType;
        IsEnumerable = isEnumerable;
    }

    /// <summary>
    /// Gets the parameter name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the display name for the parameter.
    /// </summary>
    public string DisplayName { get; }

    /// <summary>
    /// Gets whether the parameter is optional.
    /// </summary>
    public bool IsOptional { get; }

    /// <summary>
    /// Gets whether the parameter type is validatable.
    /// </summary>
    public bool HasValidatableType { get; }

    /// <summary>
    /// Gets whether the parameter is enumerable.
    /// </summary>
    public bool IsEnumerable { get; }

    /// <summary>
    /// Gets the validation attributes for this parameter.
    /// </summary>
    /// <returns>An array of validation attributes to apply to this parameter.</returns>
    protected abstract ValidationAttribute[] GetValidationAttributes();

    /// <summary>
    /// Validates the parameter value.
    /// </summary>
    /// <param name="value">The value to validate.</param>
    /// <param name="prefix">The prefix to use for validation errors.</param>
    /// <param name="validationErrors">The dictionary to add validation errors to.</param>
    /// <param name="validatableTypeInfoResolver">The resolver to use for validatable types.</param>
    /// <param name="serviceProvider">The service provider to use for validation context.</param>
    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "<Pending>")]
    public Task Validate(object? value, string prefix, Dictionary<string, string[]> validationErrors, IValidatableInfoResolver validatableTypeInfoResolver, IServiceProvider serviceProvider)
    {
        // Skip validation if value is null and parameter is optional
        if (value == null && IsOptional)
        {
            return Task.CompletedTask;
        }

        // Validate against validation attributes
        foreach (var attribute in GetValidationAttributes())
        {
            try
            {
                var validationContext = new ValidationContext(value ?? new object(), serviceProvider, null)
                {
                    DisplayName = DisplayName,
                    MemberName = Name
                };

                var result = attribute.GetValidationResult(value, validationContext);
                if (result != ValidationResult.Success)
                {
                    var key = string.IsNullOrEmpty(prefix) ? Name : $"{prefix}.{Name}";
                    if (validationErrors.TryGetValue(key, out var existing))
                    {
                        validationErrors[key] = existing.Concat(new[] { result!.ErrorMessage! }).ToArray();
                    }
                    else
                    {
                        validationErrors[key] = new[] { result!.ErrorMessage! };
                    }
                }
            }
            catch (Exception ex)
            {
                var key = string.IsNullOrEmpty(prefix) ? Name : $"{prefix}.{Name}";
                validationErrors[key] = new[] { ex.Message };
            }
        }

        // If the parameter is a collection, validate each item
        if (IsEnumerable && value is IEnumerable enumerable && HasValidatableType)
        {
            var index = 0;
            foreach (var item in enumerable)
            {
                if (item != null)
                {
                    var itemPrefix = string.IsNullOrEmpty(prefix)
                        ? $"{Name}[{index}]"
                        : $"{prefix}.{Name}[{index}]";

                    var validatableType = validatableTypeInfoResolver.GetValidatableTypeInfo(item.GetType());
                    validatableType?.Validate(item, itemPrefix, validationErrors, validatableTypeInfoResolver, serviceProvider);
                }
                index++;
            }
        }
        // If not enumerable but has a validatable type, validate the single value
        else if (HasValidatableType && value != null)
        {
            var valueType = value.GetType();
            var validatableType = validatableTypeInfoResolver.GetValidatableTypeInfo(valueType);
            validatableType?.Validate(value, prefix, validationErrors, validatableTypeInfoResolver, serviceProvider);
        }

        return Task.CompletedTask;
    }
}
