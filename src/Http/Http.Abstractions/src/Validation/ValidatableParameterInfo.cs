// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Linq;

namespace Microsoft.AspNetCore.Http.Validation;

/// <summary>
/// Contains validation information for a parameter.
/// </summary>
public abstract class ValidatableParameterInfo
{
    private ValidationAttribute? _requiredAttribute;
    /// <summary>
    /// Creates a new instance of <see cref="ValidatableParameterInfo"/>.
    /// </summary>
    /// <param name="parameterType">The <see cref="Type"/> associated with the parameter.</param>
    /// <param name="name">The parameter name.</param>
    /// <param name="displayName">The display name for the parameter.</param>
    /// <param name="isNullable">Whether the parameter is optional.</param>
    /// <param name="isRequired"></param>
    /// <param name="isEnumerable">Whether the parameter is enumerable.</param>
    public ValidatableParameterInfo(
        Type parameterType,
        string name,
        string displayName,
        bool isNullable,
        bool isRequired,
        bool isEnumerable)
    {
        ParameterType = parameterType;
        Name = name;
        DisplayName = displayName;
        IsNullable = isNullable;
        IsRequired = isRequired;
        IsEnumerable = isEnumerable;
    }

    /// <summary>
    /// Gets the parameter type.
    /// </summary>
    public Type ParameterType { get; }

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
    public bool IsNullable { get; }

    /// <summary>
    /// Gets whether the parameter is annotated with the <see cref="RequiredAttribute"/>.
    /// </summary>
    public bool IsRequired { get; }

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
    /// <param name="context">The context for the validation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <remarks>
    /// If the parameter is a collection, each item in the collection will be validated.
    /// If the parameter is not a collection but has a validatable type, the single value will be validated.
    /// </remarks>
    public virtual Task Validate(object? value, ValidatableContext context)
    {
        Debug.Assert(context.ValidationContext is not null);

        // Skip validation if value is null and parameter is optional
        if (value == null && IsNullable && !IsRequired)
        {
            return Task.CompletedTask;
        }

        context.ValidationContext.DisplayName = DisplayName;
        context.ValidationContext.MemberName = Name;

        var validationAttributes = GetValidationAttributes();

        if (IsRequired)
        {
            _requiredAttribute ??= validationAttributes.OfType<RequiredAttribute>()
                .FirstOrDefault();
            Debug.Assert(_requiredAttribute is not null, "RequiredAttribute should be present if IsRequired is true");
            var result = _requiredAttribute.GetValidationResult(value, context.ValidationContext);

            if (result is not null && result != ValidationResult.Success)
            {
                var key = string.IsNullOrEmpty(context.Prefix) ? Name : $"{context.Prefix}.{Name}";
                context.AddValidationError(key, [result.ErrorMessage!]);
                return Task.CompletedTask;
            }
        }

        // Validate against validation attributes
        for (var i = 0; i < validationAttributes.Length; i++)
        {
            var attribute = validationAttributes[i];
            try
            {
                var result = attribute.GetValidationResult(value, context.ValidationContext);
                if (result is not null && result != ValidationResult.Success)
                {
                    var key = string.IsNullOrEmpty(context.Prefix) ? Name : $"{context.Prefix}.{Name}";
                    context.AddOrExtendValidationErrors(key, [result!.ErrorMessage!]);
                }
            }
            catch (Exception ex)
            {
                var key = string.IsNullOrEmpty(context.Prefix) ? Name : $"{context.Prefix}.{Name}";
                context.AddValidationError(key, [ex.Message]);
            }
        }

        // If the parameter is a collection, validate each item
        if (IsEnumerable && value is IEnumerable enumerable)
        {
            var index = 0;
            foreach (var item in enumerable)
            {
                if (item != null)
                {
                    var itemPrefix = string.IsNullOrEmpty(context.Prefix)
                        ? $"{Name}[{index}]"
                        : $"{context.Prefix}.{Name}[{index}]";

                    if (context.ValidationOptions.TryGetValidatableTypeInfo(item.GetType(), out var validatableType))
                    {
                        validatableType.Validate(item, context);
                    }
                }
                index++;
            }
        }
        // If not enumerable, validate the single value
        else if (value != null)
        {
            var valueType = value.GetType();
            if (context.ValidationOptions.TryGetValidatableTypeInfo(valueType, out var validatableType))
            {
                validatableType.Validate(value, context);
            }
        }

        return Task.CompletedTask;
    }
}
