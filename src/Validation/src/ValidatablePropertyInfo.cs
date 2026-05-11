// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Extensions.Validation;

/// <summary>
/// Contains validation information for a member of a type.
/// </summary>
[Experimental("ASP0029", UrlFormat = "https://aka.ms/aspnet/analyzer/{0}")]
public abstract class ValidatablePropertyInfo : IValidatableInfo
{
    private RequiredAttribute? _requiredAttribute;

    /// <summary>
    /// Creates a new instance of <see cref="ValidatablePropertyInfo"/>.
    /// </summary>
    /// <param name="declaringType">The <see cref="Type"/> that declares the property.</param>
    /// <param name="propertyType">The <see cref="Type"/> of the property.</param>
    /// <param name="name">The property name.</param>
    /// <param name="displayName">The literal display name for the property (sourced from
    /// <see cref="DisplayAttribute.Name"/> when no <see cref="DisplayAttribute.ResourceType"/> is set,
    /// or from <see cref="System.ComponentModel.DisplayNameAttribute.DisplayName"/>).</param>
    /// <param name="displayResourceAccessor">An accessor that resolves the localized display name
    /// from a static resource property when the property is decorated with
    /// <c>[Display(Name = ..., ResourceType = ...)]</c>; <see langword="null"/> otherwise.</param>
    protected ValidatablePropertyInfo(
        [param: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
        Type declaringType,
        Type propertyType,
        string name,
        string? displayName,
        Func<string?>? displayResourceAccessor = null)
    {
        DeclaringType = declaringType;
        PropertyType = propertyType;
        Name = name;
        DisplayName = displayName;
        DisplayResourceAccessor = displayResourceAccessor;
    }

    /// <summary>
    /// Gets the type that declares the property.
    /// </summary>
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
    internal Type DeclaringType { get; }

    /// <summary>
    /// Gets the property type.
    /// </summary>
    internal Type PropertyType { get; }

    /// <summary>
    /// Gets the property name.
    /// </summary>
    internal string Name { get; }

    /// <summary>
    /// Gets the literal display name for the property, sourced from
    /// <see cref="DisplayAttribute.Name"/> (when no <see cref="DisplayAttribute.ResourceType"/> is set)
    /// or from <see cref="System.ComponentModel.DisplayNameAttribute.DisplayName"/>.
    /// </summary>
    /// <remarks>
    /// When <see cref="DisplayAttribute.ResourceType"/> is set, the resolved display name is
    /// produced by invoking <see cref="DisplayResourceAccessor"/> instead.
    /// </remarks>
    internal string? DisplayName { get; }

    /// <summary>
    /// Gets the accessor that resolves the localized display name from a static resource property
    /// (e.g. <c>Resources.MyProperty</c>) when the property is decorated with
    /// <c>[Display(Name = ..., ResourceType = ...)]</c>. Returns <see langword="null"/> for
    /// properties without resource-based display names.
    /// </summary>
    internal Func<string?>? DisplayResourceAccessor { get; }

    /// <summary>
    /// Gets the validation attributes for this property.
    /// </summary>
    /// <returns>An array of validation attributes to apply to this property.</returns>
    protected abstract ValidationAttribute[] GetValidationAttributes();

    /// <inheritdoc />
    public virtual async Task ValidateAsync(object? value, ValidateContext context, CancellationToken cancellationToken)
    {
        var property = DeclaringType.GetProperty(Name) ?? throw new InvalidOperationException($"Property '{Name}' not found on type '{DeclaringType.Name}'.");
        var propertyValue = property.GetValue(value);
        var validationAttributes = GetValidationAttributes();

        // Calculate and save the current path
        var originalPrefix = context.CurrentValidationPath;
        if (string.IsNullOrEmpty(originalPrefix))
        {
            context.CurrentValidationPath = Name;
        }
        else
        {
            context.CurrentValidationPath = $"{originalPrefix}.{Name}";
        }

        var localizer = context.ValidationOptions.Localizer;
        var displayName = LocalizationHelper.ResolveDisplayName(
            memberName: Name,
            DisplayName,
            DisplayResourceAccessor,
            DeclaringType,
            localizer
        );

        context.ValidationContext.DisplayName = displayName;
        context.ValidationContext.MemberName = Name;

        // Check required attribute first
        if (_requiredAttribute is not null || validationAttributes.TryGetRequiredAttribute(out _requiredAttribute))
        {
            var result = _requiredAttribute.GetValidationResult(propertyValue, context.ValidationContext);

            if (result is not null && result != ValidationResult.Success)
            {
                var errorMessage = LocalizationHelper.ResolveAttributeErrorMessage(
                    memberName: Name,
                    displayName,
                    declaringType: DeclaringType,
                    attribute: _requiredAttribute,
                    result,
                    localizer
                );

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
                        var errorMessage = LocalizationHelper.ResolveAttributeErrorMessage(
                            memberName: Name,
                            displayName,
                            declaringType: DeclaringType,
                            attribute,
                            result,
                            localizer
                        );

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
