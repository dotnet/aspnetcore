// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Extensions.Validation;

/// <summary>
/// Contains validation information for a parameter.
/// </summary>
[Experimental("ASP0029", UrlFormat = "https://aka.ms/aspnet/analyzer/{0}")]
public abstract class ValidatableParameterInfo : IValidatableInfo
{
    private RequiredAttribute? _requiredAttribute;

    /// <summary>
    /// Creates a new instance of <see cref="ValidatableParameterInfo"/>.
    /// </summary>
    /// <param name="parameterType">The <see cref="Type"/> associated with the parameter.</param>
    /// <param name="name">The parameter name.</param>
    /// <param name="displayName">The display name for the parameter.</param>
    protected ValidatableParameterInfo(
        Type parameterType,
        string name,
        string displayName)
    {
        ParameterType = parameterType;
        Name = name;
        DisplayName = displayName;
    }

    /// <summary>
    /// Gets the parameter type.
    /// </summary>
    internal Type ParameterType { get; }

    /// <summary>
    /// Gets the parameter name.
    /// </summary>
    internal string Name { get; }

    /// <summary>
    /// Gets the display name for the parameter.
    /// </summary>
    internal string DisplayName { get; }

    /// <summary>
    /// Gets the validation attributes for this parameter.
    /// </summary>
    /// <returns>An array of validation attributes to apply to this parameter.</returns>
    protected abstract ValidationAttribute[] GetValidationAttributes();

    /// <inheritdoc />
    /// <remarks>
    /// If the parameter is a collection, each item in the collection will be validated.
    /// If the parameter is not a collection but has a validatable type, the single value will be validated.
    /// </remarks>
    public virtual async Task ValidateAsync(object? value, ValidateContext context, CancellationToken cancellationToken)
    {
        // Skip validation if value is null and parameter is optional
        if (value == null && ParameterType.IsNullable())
        {
            return;
        }

        context.ValidationContext.DisplayName = DisplayName;
        context.ValidationContext.MemberName = Name;

        var validationAttributes = GetValidationAttributes();

        if (_requiredAttribute is not null || validationAttributes.TryGetRequiredAttribute(out _requiredAttribute))
        {
            var result = _requiredAttribute.GetValidationResult(value, context.ValidationContext);

            if (result is not null && result != ValidationResult.Success && result.ErrorMessage is not null)
            {
                var key = string.IsNullOrEmpty(context.CurrentValidationPath) ? Name : $"{context.CurrentValidationPath}.{Name}";
                context.AddValidationError(Name, key, [result.ErrorMessage], null);
                return;
            }
        }

        // Validate against validation attributes
        for (var i = 0; i < validationAttributes.Length; i++)
        {
            var attribute = validationAttributes[i];
            try
            {
                var result = attribute.GetValidationResult(value, context.ValidationContext);
                if (result is not null && result != ValidationResult.Success && result.ErrorMessage is not null)
                {
                    var key = string.IsNullOrEmpty(context.CurrentValidationPath) ? Name : $"{context.CurrentValidationPath}.{Name}";
                    context.AddOrExtendValidationErrors(Name, key, [result.ErrorMessage], null);
                }
            }
            catch (Exception ex)
            {
                var key = string.IsNullOrEmpty(context.CurrentValidationPath) ? Name : $"{context.CurrentValidationPath}.{Name}";
                context.AddValidationError(Name, key, [ex.Message], null);
            }
        }

        // If the parameter is a collection, validate each item
        if (ParameterType.IsEnumerable() && value is IEnumerable enumerable)
        {
            var index = 0;
            var currentPrefix = context.CurrentValidationPath;

            foreach (var item in enumerable)
            {
                if (item != null)
                {
                    context.CurrentValidationPath = string.IsNullOrEmpty(currentPrefix)
                        ? $"{Name}[{index}]"
                        : $"{currentPrefix}.{Name}[{index}]";

                    if (context.ValidationOptions.TryGetValidatableTypeInfo(item.GetType(), out var validatableType))
                    {
                        await validatableType.ValidateAsync(item, context, cancellationToken);
                    }
                }
                index++;
            }

            context.CurrentValidationPath = currentPrefix;
        }
        // If not enumerable, validate the single value
        else if (value != null)
        {
            var valueType = value.GetType();
            if (context.ValidationOptions.TryGetValidatableTypeInfo(valueType, out var validatableType))
            {
                await validatableType.ValidateAsync(value, context, cancellationToken);
            }
        }
    }
}
