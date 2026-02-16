// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Microsoft.Extensions.Validation.Localization;

namespace Microsoft.Extensions.Validation;

/// <summary>
/// Contains validation information for a member of a type.
/// </summary>
[Experimental("ASP0029", UrlFormat = "https://aka.ms/aspnet/analyzer/{0}")]
public abstract class ValidatablePropertyInfo : IValidatableInfo
{
    private readonly PropertyInfo _propertyInfo;
    private RequiredAttribute? _requiredAttribute;

    /// <summary>
    /// Creates a new instance of <see cref="ValidatablePropertyInfo"/>.
    /// </summary>
    protected ValidatablePropertyInfo(
        [param: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
        Type declaringType,
        Type propertyType,
        string name)
    {
        DeclaringType = declaringType;
        PropertyType = propertyType;
        Name = name;

        _propertyInfo = DeclaringType.GetProperty(Name, PropertyType)
            ?? throw new InvalidOperationException($"Property '{Name}' not found on type '{DeclaringType.Name}'.");
    }

    /// <summary>
    /// Gets the member type.
    /// </summary>
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
    internal Type DeclaringType { get; }

    /// <summary>
    /// Gets the member type.
    /// </summary>
    internal Type PropertyType { get; }

    /// <summary>
    /// Gets the member name.
    /// </summary>
    internal string Name { get; }

    /// <summary>
    /// Gets the validation attributes for this member.
    /// </summary>
    /// <returns>An array of validation attributes to apply to this member.</returns>
    protected abstract ValidationAttribute[] GetValidationAttributes();

    /// <summary>
    /// Gets the <see cref="DisplayAttribute"/> for this member, if one exists.
    /// </summary>
    /// <returns>The <see cref="DisplayAttribute"/> applied to this member, or <see langword="null"/>.</returns>
    protected abstract DisplayAttribute? GetDisplayAttribute();

    /// <inheritdoc />
    public virtual async Task ValidateAsync(object? value, ValidateContext context, CancellationToken cancellationToken)
    {
        var displayName = LocalizationHelper.ResolveDisplayName(GetDisplayAttribute(), declaringType: DeclaringType, memberName: Name, context);

        context.ValidationContext.DisplayName = displayName;
        context.ValidationContext.MemberName = Name;

        // Calculate and save the current path
        // TODO: Support property key customization (e.g. using JsonPropertyName)
        var originalPrefix = context.CurrentValidationPath;
        if (string.IsNullOrEmpty(originalPrefix))
        {
            context.CurrentValidationPath = Name;
        }
        else
        {
            context.CurrentValidationPath = $"{originalPrefix}.{Name}";
        }

        var errorMessageProvider = context.ErrorMessageProvider ?? context.ValidationOptions.ErrorMessageProvider;
        var propertyValue = _propertyInfo.GetValue(value);
        var validationAttributes = GetValidationAttributes();

        // Check required attribute first
        if (_requiredAttribute is not null || validationAttributes.TryGetRequiredAttribute(out _requiredAttribute))
        {
            var result = _requiredAttribute.GetValidationResult(propertyValue, context.ValidationContext);

            if (result is not null && result != ValidationResult.Success)
            {
                var customMessage = LocalizationHelper.TryResolveErrorMessage(_requiredAttribute, DeclaringType, displayName, Name, errorMessageProvider, context.ValidationContext);
                var errorMessage = customMessage ?? result.ErrorMessage;

                if (errorMessage is not null)
                {
                    context.AddValidationError(Name, context.CurrentValidationPath, [errorMessage], value);
                }

                context.CurrentValidationPath = originalPrefix; // Restore prefix
                return;
            }
        }

        // Validate any other attributes
        ValidateValue(propertyValue, Name, context.CurrentValidationPath, validationAttributes, value);

        // Check if we've reached the maximum depth before validating complex properties
        if (context.CurrentDepth >= context.ValidationOptions.MaxDepth)
        {
            throw new InvalidOperationException(
                $"Maximum validation depth of {context.ValidationOptions.MaxDepth} exceeded at '{context.CurrentValidationPath}' in '{DeclaringType.Name}.{Name}'. " +
                "This is likely caused by a circular reference in the object graph. " +
                "Consider increasing the MaxDepth in ValidationOptions if deeper validation is required.");
        }

        // Increment depth counter
        context.CurrentDepth++;

        try
        {
            // Handle enumerable values
            if (PropertyType.IsEnumerable() && propertyValue is System.Collections.IEnumerable enumerable)
            {
                var index = 0;
                var currentPrefix = context.CurrentValidationPath;

                foreach (var item in enumerable)
                {
                    context.CurrentValidationPath = $"{currentPrefix}[{index}]";

                    if (item != null)
                    {
                        var itemType = item.GetType();
                        if (context.ValidationOptions.TryGetValidatableTypeInfo(itemType, out var validatableType))
                        {
                            await validatableType.ValidateAsync(item, context, cancellationToken);
                        }
                    }

                    index++;
                }

                // Restore prefix to the property name before validating the next item
                context.CurrentValidationPath = currentPrefix;
            }
            else if (propertyValue != null)
            {
                // Validate as a complex object
                var valueType = propertyValue.GetType();
                if (context.ValidationOptions.TryGetValidatableTypeInfo(valueType, out var validatableType))
                {
                    await validatableType.ValidateAsync(propertyValue, context, cancellationToken);
                }
            }
        }
        finally
        {
            // Always decrement the depth counter and restore prefix
            context.CurrentDepth--;
            context.CurrentValidationPath = originalPrefix;
        }

        void ValidateValue(object? val, string name, string errorPrefix, ValidationAttribute[] validationAttributes, object? container)
        {
            for (var i = 0; i < validationAttributes.Length; i++)
            {
                var attribute = validationAttributes[i];
                try
                {
                    var result = attribute.GetValidationResult(val, context.ValidationContext);
                    if (result is not null && result != ValidationResult.Success)
                    {
                        var customMessage = LocalizationHelper.TryResolveErrorMessage(attribute, DeclaringType, displayName, Name, errorMessageProvider, context.ValidationContext);
                        var errorMessage = customMessage ?? result.ErrorMessage;

                        if (errorMessage is not null)
                        {
                            var key = errorPrefix.TrimStart('.');
                            context.AddOrExtendValidationErrors(name, key, [errorMessage], container);
                        }
                    }
                }
                catch (Exception ex)
                {
                    var key = errorPrefix.TrimStart('.');
                    context.AddOrExtendValidationErrors(name, key, [ex.Message], container);
                }
            }
        }
    }
}
